#include "Survival.h"
#include "SpellPresentation.h"
#include "Components/StaticMeshComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "Engine/Texture2D.h"
#include "UnrealClient.h"
#include "Misc/Paths.h"
#include "Misc/App.h"
#include "Misc/CommandLine.h"
#include "Misc/Parse.h"
#include "HAL/FileManager.h"
#include "HAL/PlatformMisc.h"

void ASurvivalGameMode::TickSpellReview(float Delta){
 auto* P=Player();if(!P)return;
 static float Age=0;static int Stage=0;static bool OK=true;
 Age+=State==ERunState::Playing?Delta:float(FApp::GetDeltaTime());
 auto Check=[&](bool Pass,const TCHAR* Name){OK&=Pass;UE_LOG(LogTemp,Display,TEXT("SPELL_CHECK %s %s"),Name,Pass?TEXT("PASS"):TEXT("FAIL"));};
 const FString Dir=FPaths::ProjectSavedDir()/TEXT("SpellReview");
 auto Shot=[&](const TCHAR* Name){IFileManager::Get().MakeDirectory(*Dir,true);FScreenshotRequest::RequestScreenshot(Dir/Name,false,false);};
 auto Reset=[&](){P->SetActorLocation(FVector(0,0,77));P->SetActorRotation(FRotator::ZeroRotator);P->GetCharacterMovement()->StopMovementImmediately();P->GetCharacterMovement()->SetMovementMode(MOVE_Walking);P->DashTime=0;P->SlashTime=0;P->Abilities->PendingSlash=-1;};
 if(Stage==0){
  PrimaryActorTick.bTickEvenWhenPaused=true;P->Abilities->AutoAttack=false;
  Check(SpellArt&&SpellArt->Textures.Num()==15&&SpellArt->EffectMaterial&&SpellArt->IconMaterial,TEXT("complete_presentation_library"));
  if(!SpellArt){FPlatformMisc::RequestExitWithStatus(false,1);return;}
  for(const auto& Pair:SpellArt->Textures)Check(Pair.Value!=nullptr&&Pair.Value->GetSizeX()<=512&&Pair.Value->GetSizeY()<=512,*Pair.Key.ToString());
  Check(SpellArt->BlackKeys.FindRef(TEXT("spell_frost_dash"))==1&&SpellArt->BlackKeys.FindRef(TEXT("spell_shadow_claw_crescent"))==0,TEXT("RGB_and_alpha_background_modes"));
  for(auto E:EnemyPool){E->Active=false;E->SetActorHiddenInGame(true);}EnemyPool.Empty();Reset();
  for(int i=0;i<6;++i){auto* E=GetWorld()->SpawnActor<ASurvivalEnemy>();EnemyPool.Add(E);E->Activate(Tuning->Enemies[i%3],FVector(130+i*170,(i%2?1:-1)*75,55),false);E->Health->Maximum=E->Health->Current=1500;}
  auto* FX=ASpellEffect::Spawn(this,TEXT("spell_sakura_bloom"),FVector(0,0,15),200,.3f);
  Check(FX&&FX->Dynamic&&FX->Sprite->GetCollisionEnabled()==ECollisionEnabled::NoCollision,TEXT("effect_material_and_no_gameplay_collision"));
  if(FX){FX->Tick(.1f);Check(FX->CurrentFade>0&&FX->GetActorScale3D().X>1,TEXT("effect_animated_scale_and_alpha"));FX->Tick(.3f);Check(FX->IsActorBeingDestroyed(),TEXT("effect_lifetime_cleanup"));}
  Stage=1;Age=0;
 }else if(Stage==1&&Age>2){Shot(TEXT("01-skill-icons.png"));P->Abilities->SlashRemaining=0;P->Attack();Stage=2;Age=0;}
 else if(Stage==2&&Age>.42f){Check(EnemyPool[0]->Health->Current<1500,TEXT("katana_hit_timed_to_animation"));Shot(TEXT("02-phantom-claw.png"));Stage=3;Age=0;}
 else if(Stage==3&&Age>.6f){P->DashCooldown=0;P->Dash();Stage=4;Age=0;}
 else if(Stage==4&&Age>.1f){Check(P->DashTime>0,TEXT("dash_and_trail_active"));Shot(TEXT("03-frost-dash.png"));Stage=5;Age=0;}
 else if(Stage==5&&Age>.6f){Reset();P->Abilities->WaveRemaining=0;P->Special();Stage=6;Age=0;}
 else if(Stage==6&&Age>.2f){Check(P->Abilities->WaveRemaining>0,TEXT("crescent_projectile_cast"));Shot(TEXT("04-shadow-crescent.png"));Stage=7;Age=0;}
 else if(Stage==7&&Age>.6f){
  auto* FX=ASpellEffect::Spawn(this,TEXT("spell_frost_dash"),FVector(0,0,15),100,1);
  if(FX)FX->Tick(.1f);AddXP(8);Check(State==ERunState::Choosing&&Choices.Num()==3,TEXT("upgrade_choices"));
  if(FX){float Fade=FX->CurrentFade;FX->Tick(.2f);Check(FX->CurrentFade==Fade,TEXT("effects_freeze_during_upgrade_pause"));}
  Stage=8;Age=0;
 }else if(Stage==8&&Age>.5f){Shot(TEXT("05-upgrade-icons.png"));Stage=9;Age=0;}
 else if(Stage==9&&Age>.3f){Choose(0);Check(State==ERunState::Playing&&Ranks.Num()==1,TEXT("upgrade_and_resume"));Stage=10;Age=0;}
 else if(Stage==10&&Age>.4f){
  Shot(TEXT("06-upgrade-bloom.png"));
  auto* B=GetWorld()->SpawnActor<ACombatProjectile>(FVector(0,0,100),FRotator::ZeroRotator);B->Direction=FVector(0,1,0);B->Life=.7f;B->Tick(.01f);
  Check(B->Visual&&B->Visual->GetAttachParentActor()==B,TEXT("projectile_visual_attached"));
  if(B->Visual){ASpellEffect* V=B->Visual;B->Destroy();Check(V->IsActorBeingDestroyed(),TEXT("projectile_destroy_cleans_visual"));}else B->Destroy();
  for(int i=0;i<100;++i)ASpellEffect::Spawn(this,TEXT("spell_sakura_bloom"),FVector(0,0,15),10,.05f);
  Check(SpellEffects.Num()<=64,TEXT("dense_wave_effect_budget"));Stage=11;Age=0;
 }else if(Stage==11&&Age>1.5f){
  SpellEffects.RemoveAll([](const TWeakObjectPtr<ASpellEffect>& E){return !E.IsValid();});
  Check(SpellEffects.Num()==0,TEXT("all_transient_effects_reclaimed"));
  if(FParse::Param(FCommandLine::Get(),TEXT("SpellStress"))){
   Reset();P->Health->Current=P->Health->Maximum=100000;P->Abilities->AutoAttack=true;P->ExtraProjectiles=7;
   for(auto E:EnemyPool){E->Active=false;E->SetActorHiddenInGame(true);}
   for(int i=0;i<80;++i){ASurvivalEnemy* E=i<EnemyPool.Num()?EnemyPool[i].Get():GetWorld()->SpawnActor<ASurvivalEnemy>();if(i>=EnemyPool.Num())EnemyPool.Add(E);
    FEnemySpec Spec=Tuning->Enemies[i%3];Spec.Health=100000;const float Angle=i*2*PI/80;E->Activate(Spec,FVector(FMath::Cos(Angle)*600,FMath::Sin(Angle)*600,55),false);
   }
   Stage=12;Age=0;return;
  }
  UE_LOG(LogTemp,Display,TEXT("SPELL_RUNTIME_%s"),OK?TEXT("PASS"):TEXT("FAIL"));FPlatformMisc::RequestExitWithStatus(false,OK?0:1);
 }else if(Stage==12){
  static TArray<float> Frames;static int PeakEffects=0;static float DashTimer=0;static bool Captured=false;
  if(Age>2)Frames.Add(Delta*1000);PeakEffects=FMath::Max(PeakEffects,SpellEffects.Num());
  for(auto E:EnemyPool)E->Simulate(FMath::Min(.05f,Delta),P);
  DashTimer+=Delta;if(DashTimer>.8f){DashTimer=0;P->SetActorLocation(FVector(0,0,77));P->SetActorRotation(FRotator(0,Age*45,0));P->DashCooldown=0;P->Dash();}
  if(!Captured&&Age>8){Shot(TEXT("07-dense-wave.png"));Captured=true;}
  if(Age>14){
   Check(ActiveCount()==80&&State==ERunState::Playing,TEXT("80_enemy_combat_stress"));
   Check(PeakEffects<=64,TEXT("stress_effect_budget"));
   Frames.Sort();float Sum=0;for(float F:Frames)Sum+=F;
   UE_LOG(LogTemp,Display,TEXT("SPELL_STRESS frames=%d mean_ms=%.2f p95_ms=%.2f peak_effects=%d enemies=%d"),Frames.Num(),Sum/FMath::Max(1,Frames.Num()),Frames.IsEmpty()?0:Frames[FMath::Min(Frames.Num()-1,int32(Frames.Num()*.95f))],PeakEffects,ActiveCount());
   UE_LOG(LogTemp,Display,TEXT("SPELL_RUNTIME_%s"),OK?TEXT("PASS"):TEXT("FAIL"));FPlatformMisc::RequestExitWithStatus(false,OK?0:1);
  }
 }
}
