#include "Survival.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/StaticMesh.h"
#include "Engine/World.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "GameFramework/SpringArmComponent.h"
#include "GameFramework/PlayerController.h"
#include "UnrealClient.h"
#include "Misc/Paths.h"
#include "Misc/CommandLine.h"
#include "Misc/Parse.h"
#include "HAL/FileManager.h"
#include "HAL/PlatformMisc.h"
void ASurvivalGameMode::TickBleepReview(float Delta){
 auto* P=Player();if(!P)return;
 const bool bCombatReview=FParse::Param(FCommandLine::Get(),TEXT("CombatReview"));
 static float Age=0;static int Stage=0;static bool OK=true;
 Age+=Delta;P->Abilities->AutoAttack=false;
 auto Check=[&](bool Pass,const TCHAR* Name=TEXT("legacy")){OK&=Pass;UE_LOG(LogTemp,Display,TEXT("BLEEP_CHECK %s %s"),Name,Pass?TEXT("PASS"):TEXT("FAIL"));};
 if(Stage==0){
  for(auto E:EnemyPool){E->Active=false;E->SetActorHiddenInGame(true);}
  EnemyPool.Empty();
  for(int i=0;i<4;++i){auto* E=GetWorld()->SpawnActor<ASurvivalEnemy>();EnemyPool.Add(E);E->Activate(Tuning->Enemies[i%Tuning->Enemies.Num()],FVector(0,(i-1.5f)*220,55),i==3);
   E->SetActorRotation(FRotator(0,0,0));Check(E->BleepMesh&&E->Body->GetStaticMesh()==E->BleepMesh,TEXT("mesh_available"));Check(E->Body->GetStaticMesh()&&E->Body->GetStaticMesh()->GetNumLODs()==3,TEXT("three_authored_lods"));
   UE_LOG(LogTemp,Display,TEXT("BLEEP_GROUND index=%d bottom=%.2f lods=%d"),i,E->Body->Bounds.Origin.Z-E->Body->Bounds.BoxExtent.Z,E->Body->GetStaticMesh()->GetNumLODs());Check(FMath::Abs(E->Body->Bounds.Origin.Z-E->Body->Bounds.BoxExtent.Z)<3,TEXT("bind_pose_grounding"));
  }
  P->SetActorLocation(FVector(350,0,77));P->CameraBoom->TargetArmLength=1150;P->CameraBoom->SetRelativeRotation(FRotator(-18,180,0));P->CameraBoom->bEnableCameraLag=false;
  if(auto* PC=Cast<APlayerController>(P->GetController()))if(PC->GetHUD())PC->GetHUD()->bShowHUD=bCombatReview;
  if(bCombatReview)for(auto E:EnemyPool){E->Windup=.2f;E->Health->Current*=.6f;}Stage=1;Age=0;
 }else if(Stage==1&&Age>3){
  const FString Dir=FPaths::ProjectSavedDir()/TEXT("BleepReview");IFileManager::Get().MakeDirectory(*Dir,true);
  FScreenshotRequest::RequestScreenshot(Dir/(bCombatReview?TEXT("bCombatReview-feedback.png"):TEXT("bleep-enemies.png")),false,false);Stage=2;Age=0;
 }else if(Stage==2&&Age>.5f){
  auto* E=EnemyPool[0].Get();E->Windup=0;const FVector Before=E->GetActorLocation();E->Simulate(.1f,P);Check(FVector::Dist2D(Before,E->GetActorLocation())>1);
  E->Hit(100000,FVector::ForwardVector,0,false);Check(!E->Active);E->Activate(Tuning->Enemies[0],FVector(0,0,55),false);Check(E->Active&&E->Body->GetStaticMesh()==E->BleepMesh);
  if(bCombatReview){
   auto Probe=[&](bool Pass,const TCHAR* Name){Check(Pass);UE_LOG(LogTemp,Display,TEXT("COMBAT_CHECK %s %s"),Name,Pass?TEXT("PASS"):TEXT("FAIL"));};
   FEnemySpec Spec=Tuning->Enemies[0];const FVector Start(0,0,55);
   P->SetActorLocation(FVector(90,0,77));P->ImpactTime=0;P->DashTime=0;P->Health->Current=120;
   E->Activate(Spec,Start,false);E->AttackTimer=0;E->Simulate(.05f,P);Probe(E->Windup>0,TEXT("warning_before_damage"));
   const float HP=P->Health->Current;P->SetActorLocation(FVector(500,0,77));for(int i=0;i<12;++i)E->Simulate(.05f,P);
   Probe(P->Health->Current==HP&&E->AttackTimer>0,TEXT("dodge_causes_miss_and_recovery"));
   E->Activate(Spec,Start,false);P->SetActorLocation(FVector(90,0,77));E->AttackTimer=0;E->Simulate(.05f,P);const FVector Locked=E->GetActorLocation();E->Simulate(.1f,P);
   Probe(E->GetActorLocation().Equals(Locked,.01f),TEXT("attack_position_committed"));
   E->Hit(1,FVector(-1,0,0),300,false);E->Simulate(.05f,P);
   Probe(E->GetActorLocation().X<-10&&E->Windup==0&&E->AttackTimer>0,TEXT("close_knockback_interrupts"));
   E->Activate(Spec,Start,false);E->AttackTimer=0;P->ImpactTime=0;for(int i=0;i<12;++i)E->Simulate(.05f,P);
   Probe(P->Health->Current<HP,TEXT("unavoided_attack_hits"));
   const float After=P->Health->Current;P->ImpactTime=0;P->DashTime=.2f;E->AttackTimer=0;E->Windup=.01f;E->Simulate(.05f,P);
   Probe(P->Health->Current==After,TEXT("dash_avoids_strike"));P->DashTime=0;
   E->Activate(Spec,Start,false);E->Windup=.01f;P->ImpactTime=0;P->SetActorLocation(FVector(90,0,300));E->Simulate(.05f,P);
   Probe(P->Health->Current==After,TEXT("high_jump_avoids_strike"));
   // Compare real movement with and without repeated close-range attacks.
   auto Travel=[&](bool Automatic,bool Manual){
    P->SetActorLocation(FVector(0,0,77));P->SetActorRotation(FRotator::ZeroRotator);
    auto* Move=P->GetCharacterMovement();Move->StopMovementImmediately();Move->SetMovementMode(MOVE_Walking);
    P->SlashTime=0;P->DashTime=0;P->Abilities->SlashRemaining=0;P->Abilities->PendingSlash=-1;P->Abilities->AutoAttack=Automatic;
    E->Activate(Spec,FVector(100,0,55),false);E->Health->Current=E->Health->Maximum=100000;
    if(Manual)P->Attack();
    const FVector Begin=P->GetActorLocation();int SlashingFrames=0;
    for(int Frame=0;Frame<90;++Frame){
     E->SetActorLocation(P->GetActorLocation()+FVector(100,0,0));
     P->Abilities->TickComponent(1.f/60,LEVELTICK_All,nullptr);
     P->Forward(1);P->Tick(1.f/60);Move->TickComponent(1.f/60,LEVELTICK_All,nullptr);
     if(P->SlashTime>0)++SlashingFrames;
    }
    P->Forward(0);P->Abilities->AutoAttack=false;
    if(Automatic)Probe(SlashingFrames>70,TEXT("continuous_close_attack_exercised"));
    return FVector::Dist2D(Begin,P->GetActorLocation());
   };
   const float FreeTravel=Travel(false,false),ManualTravel=Travel(false,true),AutoTravel=Travel(true,false);
   UE_LOG(LogTemp,Display,TEXT("COMBAT_TRAVEL free=%.1f manual=%.1f automatic=%.1f"),FreeTravel,ManualTravel,AutoTravel);
   Probe(FreeTravel>400&&ManualTravel>=FreeTravel*.95f&&AutoTravel>=FreeTravel*.95f,TEXT("full_speed_during_attacks"));
   P->GetCharacterMovement()->SetMovementMode(MOVE_Walking);P->DashCooldown=0;P->DashTime=0;P->PlaySlashAnimation();P->Abilities->PendingSlash=.2f;P->Dash();
   Probe(P->DashTime>0&&P->SlashTime==0&&P->Abilities->PendingSlash<0,TEXT("dash_cancels_attack_without_delayed_hit"));
   P->DashTime=0;P->SetActorLocation(FVector(0,0,77));P->GetCharacterMovement()->StopMovementImmediately();P->GetCharacterMovement()->SetMovementMode(MOVE_Walking);
   Stage=3;Age=0;return;
  }
  UE_LOG(LogTemp,Display,TEXT("BLEEP_RUNTIME_%s"),OK?TEXT("PASS"):TEXT("FAIL"));FPlatformMisc::RequestExitWithStatus(false,OK?0:1);
 } else if(Stage==3){
  static int Direction=0;static bool AimOK=true;
  const float Forward=Direction==0?1.f:Direction==2?-1.f:0.f,Right=Direction==1?1.f:Direction==3?-1.f:0.f;
  P->Forward(Forward);P->Right(Right);P->Abilities->AutoAttack=true;
  auto* E=EnemyPool[0].Get();E->SetActorLocation(P->GetActorLocation()-P->MoveDirection*100);
  if(Age>.35f&&Age<.35f+Delta*1.1f){P->SlashTime=0;P->Abilities->SlashRemaining=0;const FVector Facing=P->GetActorForwardVector();P->Abilities->Katana();AimOK&=Facing.Equals(P->GetActorForwardVector(),.001f);}
  if(Age>.6f){const bool Facing=FVector::DotProduct(P->GetActorForwardVector(),P->MoveDirection.GetSafeNormal2D())>.99f;
   Check(Facing);UE_LOG(LogTemp,Display,TEXT("FACING direction=%d %s"),Direction,Facing?TEXT("PASS"):TEXT("FAIL"));Age=0;++Direction;
   if(Direction==4){Check(AimOK);UE_LOG(LogTemp,Display,TEXT("COMBAT_RUNTIME_%s"),OK?TEXT("PASS"):TEXT("FAIL"));FPlatformMisc::RequestExitWithStatus(false,OK?0:1);}
  }
 }
}
