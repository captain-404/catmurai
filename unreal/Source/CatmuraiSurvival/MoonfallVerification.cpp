#include "Survival.h"
#include "MoonfallArena.h"
#include "EngineUtils.h"
#include "Engine/World.h"
#include "SceneInterface.h"
#include "Components/LightComponentBase.h"
#include "Materials/Material.h"
#include "Components/HierarchicalInstancedStaticMeshComponent.h"
#include "Components/StaticMeshComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "GameFramework/SpringArmComponent.h"
#include "GameFramework/PlayerController.h"
#include "UnrealClient.h"
#include "Misc/Paths.h"
#include "Misc/CommandLine.h"
#include "Misc/Parse.h"
#include "HAL/FileManager.h"
#include "HAL/PlatformMisc.h"

void ASurvivalGameMode::TickMoonfallReview(float Delta){
 auto* P=Player();if(!P)return;
 static float Age=0;static int Stage=0;static bool OK=true;
 static TArray<float> FrameTimes;static int PeakEffects=0;static bool Captured=false;
 int Round=1;FParse::Value(FCommandLine::Get(),TEXT("ReviewRound="),Round);
 const FString Dir=FPaths::ProjectSavedDir()/FString::Printf(TEXT("MoonfallReview/round-%d"),Round);
 Age+=Delta;
 auto Check=[&](bool Pass,const TCHAR* Name){OK&=Pass;UE_LOG(LogTemp,Display,TEXT("MOONFALL_CHECK %s %s"),Name,Pass?TEXT("PASS"):TEXT("FAIL"));};
 auto Shot=[&](const TCHAR* Name){IFileManager::Get().MakeDirectory(*Dir,true);FScreenshotRequest::RequestScreenshot(Dir/Name,false,false);};
 if(Stage==0){
  PrimaryActorTick.bTickEvenWhenPaused=true;
  AMoonfallArena* Arena=nullptr;for(TActorIterator<AMoonfallArena> It(GetWorld());It;++It)Arena=*It;
  Check(Arena&&Arena->bBuilt&&Arena->InstanceComponentCount<=12&&Arena->DecorativeInstanceCount>500&&Arena->SmallLightCount<=8,TEXT("instanced_shrine_budget"));
  bool Materials=true;if(Arena){TArray<UHierarchicalInstancedStaticMeshComponent*> Meshes;Arena->GetComponents(Meshes);for(auto M:Meshes)Materials&=M->GetMaterial(0)&&M->GetMaterial(0)->GetPathName().StartsWith(TEXT("/Game/Moonfall/"))&&M->GetMaterial(0)->GetMaterial()->GetUsageByFlag(MATUSAGE_InstancedStaticMeshes);}
  Check(Materials,TEXT("shrine_materials_loaded"));
  Check(LoadObject<UMaterial>(nullptr,TEXT("/Game/Moonfall/Materials/M_ShrineSky.M_ShrineSky"))!=nullptr,TEXT("moonlit_sky_material"));
  bool Floor=true;for(int X=-4;X<=4;X++)for(int Y=-4;Y<=4;Y++){FHitResult Hit;Floor&=GetWorld()->LineTraceSingleByChannel(Hit,FVector(X*550,Y*550,80),FVector(X*550,Y*550,-80),ECC_Visibility)&&FMath::Abs(Hit.ImpactPoint.Z)<1;}
  Check(Floor,TEXT("81_walkable_floor_samples"));
  bool Walls=true;for(int Axis=0;Axis<2;Axis++)for(int Side=-1;Side<=1;Side+=2){FHitResult Hit;FVector Start(0,0,70),End(0,0,70);Start[Axis]=Side*2200;End[Axis]=Side*2600;Walls&=GetWorld()->LineTraceSingleByChannel(Hit,Start,End,ECC_Visibility);}
  Check(Walls,TEXT("four_safety_boundaries"));
  P->Abilities->AutoAttack=false;P->SetActorLocation(FVector(0,0,77));P->SetActorRotation(FRotator::ZeroRotator);
  P->GetCharacterMovement()->StopMovementImmediately();P->Health->Maximum=P->Health->Current=100000;
  for(auto E:EnemyPool){E->Destroy();}EnemyPool.Empty();
  for(int i=0;i<6;++i){auto* E=GetWorld()->SpawnActor<ASurvivalEnemy>();EnemyPool.Add(E);E->Activate(Tuning->Enemies[i%3],FVector(180+i*180,(i%2?1:-1)*240,55),i==5);E->Health->Maximum=E->Health->Current=100000;}
  auto* E=EnemyPool[0].Get();const FVector Anchor=E->GetActorLocation();const FVector IdleScale=E->Body->GetRelativeScale3D();E->AnimatePresentation(.1f);
  Check(E->AnimationPose==EEnemyPose::Idle&&!E->Body->GetRelativeScale3D().Equals(IdleScale,.0001f),TEXT("idle_breathing"));
  E->bPursuing=true;E->AnimatePresentation(.1f);Check(E->AnimationPose==EEnemyPose::Run&&E->GetActorLocation().Equals(Anchor),TEXT("stride_animates_visual_only"));
  E->bPursuing=false;E->Windup=.1f;E->AnimatePresentation(.05f);Check(E->AnimationPose==EEnemyPose::Windup&&E->Body->GetRelativeScale3D().Z<1,TEXT("windup_crouch"));
  E->Windup=0;E->StrikeVisual=.18f;E->AnimatePresentation(.04f);Check(E->AnimationPose==EEnemyPose::Strike,TEXT("strike_lunge"));
  E->Hit(1,FVector::ForwardVector,100,false);E->AnimatePresentation(.04f);Check(E->AnimationPose==EEnemyPose::Hurt,TEXT("hit_recoil"));
  E->Hit(1000000,FVector::ForwardVector,0,false);Check(!E->Active&&!E->CanReuse(),TEXT("defeat_visual_reserves_pool_slot"));E->AnimatePresentation(.1f);
  Check(E->AnimationPose==EEnemyPose::Death&&E->Body->GetRelativeScale3D().Z<1,TEXT("defeat_shrink"));E->AnimatePresentation(.3f);Check(E->CanReuse()&&E->IsHidden(),TEXT("death_finishes_then_pool_reuses"));
  E->Activate(Tuning->Enemies[0],Anchor,false);E->Health->Maximum=E->Health->Current=100000;
  Check(E->Body->GetRelativeScale3D().Equals(FVector::OneVector)&&E->HitReaction==0&&E->DeathRemaining==0,TEXT("pooled_pose_reset"));
  const float Before=E->AnimationClock;State=ERunState::Paused;E->Tick(.1f);Check(E->AnimationClock==Before,TEXT("animation_respects_pause"));State=ERunState::Playing;
  E->Active=false;E->DeathRemaining=.32f;SetRunState(ERunState::Victory);Check(E->IsHidden()&&E->DeathRemaining==0,TEXT("terminal_death_cleanup"));SetRunState(ERunState::Playing);E->Activate(Tuning->Enemies[0],Anchor,false);E->Health->Maximum=E->Health->Current=100000;
  E->SetActorLocation(FVector(1600,0,55));E->Tick(.04f);Check(E->GetActorTickInterval()>.03f&&E->Body->ForcedLodModel==3,TEXT("distant_animation_and_mesh_lod"));E->SetActorLocation(Anchor);E->Tick(.04f);Check(E->GetActorTickInterval()==0&&E->Body->ForcedLodModel==1,TEXT("near_attack_full_rate"));
  Stage=1;Age=0;
 }else if(Stage==1&&Age>2){
  P->CameraBoom->TargetArmLength=1100;P->CameraBoom->SetRelativeRotation(FRotator(-40,-45,0));P->CameraBoom->bEnableCameraLag=false;
  for(int i=0;i<EnemyPool.Num();++i){auto* E=EnemyPool[i].Get();E->bPursuing=i%3==0;E->Windup=i%3==1?.25f:0;E->StrikeVisual=i%3==2?.18f:0;}
  Shot(TEXT("01-enemy-poses.png"));Stage=2;Age=0;
 }else if(Stage==2&&Age>1){
  bReviewHideHUD=true;P->CameraBoom->TargetArmLength=7200;P->CameraBoom->SetRelativeRotation(FRotator(-48,-45,0));
  P->CameraBoom->TargetArmLength=9600;P->CameraBoom->SetRelativeRotation(FRotator(-38,-45,0));Stage=25;Age=0;
 }else if(Stage==25&&Age>.5f){Shot(TEXT("02-shrine-overview.png"));Stage=3;Age=0;
 }else if(Stage==3&&Age>.5f){
  bReviewHideHUD=false;
  P->CameraBoom->TargetArmLength=1250;P->CameraBoom->SetRelativeRotation(FRotator(-57,-45,0));
  P->Abilities->AutoAttack=true;P->ExtraProjectiles=7;
  for(int i=0;i<80;++i){ASurvivalEnemy* E=i<EnemyPool.Num()?EnemyPool[i].Get():GetWorld()->SpawnActor<ASurvivalEnemy>();if(i>=EnemyPool.Num())EnemyPool.Add(E);
   FEnemySpec Spec=Tuning->Enemies[i%3];Spec.Health=100000;const float A=i*2*PI/80;E->Activate(Spec,FVector(FMath::Cos(A)*600,FMath::Sin(A)*600,55),false);
  }
  Stage=4;Age=0;
 }else if(Stage==4){
  static float Step=0,DashTimer=0;
  Step+=Delta;if(Step>=.05f){const float DT=FMath::Min(.1f,Step);Step=0;for(auto E:EnemyPool)E->Simulate(DT,P);}
  DashTimer+=Delta;if(DashTimer>.9f){DashTimer=0;P->SetActorLocation(FVector(0,0,77));P->SetActorRotation(FRotator(0,Age*40,0));P->DashCooldown=0;P->Dash();}
  if(Age>2)FrameTimes.Add(Delta*1000);PeakEffects=FMath::Max(PeakEffects,SpellEffects.Num());
  if(!Captured&&Age>6){Shot(TEXT("03-dense-combat.png"));Captured=true;}
  if(Age>10){
   UE_LOG(LogTemp,Display,TEXT("MOONFALL_LIGHTING unbuilt_objects=%u unbuilt_reflections=%u"),GetWorld()->NumLightingUnbuiltObjects,GetWorld()->NumUnbuiltReflectionCaptures);
   bool DynamicLights=true;int LightCount=0;for(TActorIterator<AActor> It(GetWorld());It;++It){TArray<ULightComponentBase*> Lights;It->GetComponents(Lights);for(auto L:Lights){DynamicLights&=L->Mobility==EComponentMobility::Movable;++LightCount;}}
   Check(DynamicLights&&LightCount==11,TEXT("runtime_lights_do_not_require_baking"));
   GetWorld()->Scene->DumpUnbuiltLightInteractions(*GLog);
   Check(GetWorld()->NumLightingUnbuiltObjects==0&&GetWorld()->NumUnbuiltReflectionCaptures==0,TEXT("no_unbuilt_lighting_or_reflections"));
   Check(ActiveCount()==80&&State==ERunState::Playing,TEXT("80_animated_enemies"));Check(PeakEffects>0&&PeakEffects<=64,TEXT("spell_effect_budget"));Check(FrameTimes.Num()>100,TEXT("performance_samples_present"));
   FrameTimes.Sort();float Sum=0;for(float T:FrameTimes)Sum+=T;
   UE_LOG(LogTemp,Display,TEXT("MOONFALL_METRICS round=%d frames=%d mean_ms=%.2f p95_ms=%.2f peak_effects=%d enemies=%d"),Round,FrameTimes.Num(),Sum/FMath::Max(1,FrameTimes.Num()),FrameTimes.IsEmpty()?0:FrameTimes[FMath::Min(FrameTimes.Num()-1,int32(FrameTimes.Num()*.95f))],PeakEffects,ActiveCount());
   Stage=5;Age=0;P->Abilities->AutoAttack=false;P->SetActorLocation(FVector(1800,0,77));P->CameraBoom->TargetArmLength=2000;P->CameraBoom->SetRelativeRotation(FRotator(-35,-70,0));
  }
 }else if(Stage==5&&Age>.5f){Shot(TEXT("04-shrine-edge.png"));Stage=6;Age=0;
 }else if(Stage==6&&Age>.5f){
  OfferUpgrades();Check(State==ERunState::Choosing&&Choices.Num()==3&&GetWorld()->NumLightingUnbuiltObjects==0,TEXT("upgrade_menu_no_lighting_warning"));Shot(TEXT("05-upgrade-lighting.png"));Stage=7;Age=0;
 }else if(Stage==7){Choose(0);Check(State==ERunState::Playing,TEXT("upgrade_resumes_after_lighting_check"));Stage=8;Age=0;
 }else if(Stage==8&&Age>.5f){UE_LOG(LogTemp,Display,TEXT("MOONFALL_ROUND_%d_%s"),Round,OK?TEXT("PASS"):TEXT("FAIL"));FPlatformMisc::RequestExitWithStatus(false,OK?0:1);}
}
