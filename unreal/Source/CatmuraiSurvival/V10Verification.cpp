#include "Survival.h"
#include "Animation/AnimSequence.h"
#include "Components/SkeletalMeshComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "GameFramework/SpringArmComponent.h"
#include "GameFramework/PlayerController.h"
#include "Engine/SkeletalMesh.h"
#include "Engine/World.h"
#include "UnrealClient.h"
#include "Misc/Paths.h"
#include "Misc/CommandLine.h"
#include "Misc/Parse.h"
#include "HAL/FileManager.h"
#include "HAL/PlatformMisc.h"

void ASurvivalGameMode::TickV10Verification(float Delta){
 auto* P=Player();if(!P)return;
 auto* M=P->GetCharacterMovement();
 static int32 Stage=0;static float Age=0,Peak=0,MaxBlink=0;static bool OK=true;static FVector Start;
 static ASurvivalEnemy* Front=nullptr;static ASurvivalEnemy* Back=nullptr;
 const bool Before=FParse::Param(FCommandLine::Get(),TEXT("CatmuraiBeforeV10"));
 Age+=Delta;MaxBlink=FMath::Max(MaxBlink,P->GetMesh()->GetMorphTarget(TEXT("Blink")));
 auto Check=[&](bool Pass,const TCHAR* Name){OK&=Pass;UE_LOG(LogTemp,Display,TEXT("V10_CHECK %s %s"),Name,Pass?TEXT("PASS"):TEXT("FAIL"));};
 auto Shot=[&](const TCHAR* Name){const FString Dir=FPaths::ProjectSavedDir()/TEXT("V10Review");IFileManager::Get().MakeDirectory(*Dir,true);FScreenshotRequest::RequestScreenshot(Dir/Name,false,false);};
 auto Next=[&](){++Stage;Age=0;};
 P->Abilities->AutoAttack=false;P->Health->Current=120;
 if(Before&&Stage>0){if(Age>.3f)FPlatformMisc::RequestExitWithStatus(false,0);return;}
 if(Stage==0){
  if(Before&&Age<=Delta*1.1f){P->GetMesh()->SetSkeletalMesh(LoadObject<USkeletalMesh>(nullptr,TEXT("/Game/Characters/CatmuraiV2/SK_Catmurai_Drawn.SK_Catmurai_Drawn")));P->GetMesh()->PlayAnimation(LoadObject<UAnimSequence>(nullptr,TEXT("/Game/Characters/CatmuraiV2/A_Catmurai_Idle.A_Catmurai_Idle")),true);}P->GetMesh()->SetForcedLOD(1);P->CameraBoom->TargetArmLength=540;P->CameraBoom->SetRelativeRotation(FRotator(-20,-45,0));P->CameraBoom->bEnableCameraLag=false;
  if(auto* PC=Cast<APlayerController>(P->GetController()))if(PC->GetHUD())PC->GetHUD()->bShowHUD=false;
  if(Age>1){Check(P->CurrentAnimation==P->IdleAnimation,TEXT("idle"));Shot(Before?TEXT("00-before-v9.png"):TEXT("01-idle.png"));Next();}}
 else if(Stage==1){P->Forward(1);if(Age>.6f){Check(P->GetVelocity().Size2D()>300,TEXT("run_speed"));Shot(TEXT("02-run.png"));P->Forward(0);M->StopMovementImmediately();P->PressJump();Start=P->GetActorLocation();Peak=Start.Z;Next();}}
 else if(Stage==2){Peak=FMath::Max(Peak,float(P->GetActorLocation().Z));if(Age>.16f){Check(M->IsFalling()&&M->Velocity.Z>0,TEXT("jump_rise"));Shot(TEXT("03-jump-rise.png"));Next();}}
 else if(Stage==3){Peak=FMath::Max(Peak,float(P->GetActorLocation().Z));if(Age>.19f){Check(Peak-Start.Z>145&&Peak-Start.Z<180,TEXT("jump_height"));P->ReleaseJump();P->PressJump();Check(P->AirJumpsUsed==1&&M->Velocity.Z>900,TEXT("double_jump"));P->PressJump();Check(P->AirJumpsUsed==1,TEXT("third_jump_rejected"));Next();}}
 else if(Stage==4){if(Age>.2f){Shot(TEXT("04-double-jump-flip.png"));P->ReleaseJump();Next();}}
 else if(Stage==5){if(Age>.5f){if(M->IsMovingOnGround()){Check(P->AirJumpsUsed==0&&P->AirDashesUsed==0,TEXT("landing_resets"));Shot(TEXT("05-land.png"));Next();}else if(Age>3){Check(false,TEXT("landing_timeout"));Next();}}}
 else if(Stage==6){if(Age>.6f){Start=P->GetActorLocation();P->Dash();Check(P->DashTime>0&&P->DashCooldown>.59f,TEXT("dash_start"));Next();}}
 else if(Stage==7){if(Age>.09f&&Age<.09f+Delta*1.5f)Shot(TEXT("06-dash.png"));if(Age>.23f){const float Distance=FVector::Dist2D(Start,P->GetActorLocation());UE_LOG(LogTemp,Display,TEXT("V10_DASH_DISTANCE %.2f"),Distance);Check(Distance>540&&Distance<690,TEXT("dash_distance"));Next();}}
 else if(Stage==8){if(Age>.5f){P->PressJump();Next();}}
 else if(Stage==9){if(Age>.15f){P->Dash();Check(P->AirDashesUsed==1,TEXT("air_dash"));Start=P->GetActorLocation();Next();}}
 else if(Stage==10){if(Age>.1f){Check(FMath::Abs(P->GetActorLocation().Z-Start.Z)<8,TEXT("air_dash_holds_height"));P->ReleaseJump();P->PressJump();Check(P->DashTime==0&&M->Velocity.Z>900,TEXT("jump_cancels_dash"));Next();}}
 else if(Stage==11){if(Age>.65f){P->DashCooldown=0;P->Dash();Check(P->DashTime==0&&P->AirDashesUsed==1,TEXT("second_air_dash_rejected"));P->ReleaseJump();Next();}}
 else if(Stage==12){if(M->IsMovingOnGround()&&Age>.3f){M->StopMovementImmediately();P->SetActorLocation(FVector(0,0,77));P->SetActorRotation(FRotator(0,135,0));P->SlashTime=0;P->Abilities->SlashRemaining=0;
   FEnemySpec Spec;Spec.Health=100;Front=GetWorld()->SpawnActor<ASurvivalEnemy>();Back=GetWorld()->SpawnActor<ASurvivalEnemy>();EnemyPool.Add(Front);EnemyPool.Add(Back);
   Front->Activate(Spec,P->GetActorLocation()+P->GetActorForwardVector()*180,false);Back->Activate(Spec,P->GetActorLocation()-P->GetActorForwardVector()*180,false);
   // Manual attack preserves facing; only automatic attacks aim toward a target.
   P->Abilities->Katana();Check(Front->Health->Current==100,TEXT("slash_no_instant_damage"));Next();}}
 else if(Stage==13){if(Age>.2f){Check(Front->Health->Current==100,TEXT("slash_windup"));Next();}}
 else if(Stage==14){if(Age>.16f){Check(Front->Health->Current<100&&Back->Health->Current==100,TEXT("slash_delayed_arc_hit"));Front->SetActorHiddenInGame(true);Back->SetActorHiddenInGame(true);Shot(TEXT("07-slash.png"));Next();}}
 else if(Stage==15){if(Age>.7f){const auto& Ref=P->GetMesh()->GetSkeletalMeshAsset()->GetRefSkeleton();const auto& Pose=P->GetMesh()->GetBoneSpaceTransforms();
   for(FName Bone:{FName(TEXT("hand_r")),FName(TEXT("weapon_r"))}){int32 I=Ref.FindBoneIndex(Bone);Check(I>=0&&Pose.IsValidIndex(I)&&Pose[I].GetRotation().AngularDistance(Ref.GetRefBonePose()[I].GetRotation())<.001f,*Bone.ToString());}
   Check(P->GetMesh()->GetMorphTarget(TEXT("MouthOpen"))>0,TEXT("face_morph"));
   P->SetActorLocation(FVector(0,0,200));M->SetMovementMode(MOVE_Falling);P->bJumpInProgress=false;P->CoyoteRemaining=.1f;P->AirJumpsUsed=0;P->PressJump();
   Check(P->AirJumpsUsed==0&&M->Velocity.Z>1000,TEXT("coyote_jump"));P->ReleaseJump();Next();}}
 else if(Stage==16){if(Age>.3f){Check(M->GravityScale>4.9f,TEXT("short_hop_gravity"));
   P->SetActorLocation(FVector(0,0,95));M->SetMovementMode(MOVE_Falling);M->Velocity=FVector(0,0,-100);P->AirJumpsUsed=1;P->CoyoteRemaining=0;P->PressJump();Next();}}
 else if(Stage==17){if(Age>.1f){Check(M->IsFalling()&&M->Velocity.Z>700&&P->AirJumpsUsed==0,TEXT("buffered_landing_jump"));P->ReleaseJump();Next();}}
 else if(Stage==18){if(M->IsMovingOnGround()&&Age>.3f){P->DashCooldown=0;P->Dash();Next();}}
 else if(Stage==19){if(Age>.15f){P->PressJump();Check(P->AirJumpsUsed==0&&P->DashTime==0&&M->Velocity.Z>1000,TEXT("ground_dash_jump_cancel"));
  P->ReleaseJump();Next();}}
 else if(Stage==20){if(M->IsMovingOnGround()&&Age>.3f){P->SetActorLocation(FVector(0,0,250));M->SetMovementMode(MOVE_Falling);M->Velocity=FVector::ZeroVector;P->bJumpInProgress=false;P->AirTime=0;P->CoyoteRemaining=.12f;Next();}}
 else if(Stage==21){if(Age>.14f){Check(P->bAirBlend,TEXT("ledge_fall_animation"));P->PressJump();Check(P->AirJumpsUsed==1&&M->Velocity.Z>900,TEXT("ledge_double_jump"));P->ReleaseJump();Next();}}
 else if(Stage==22){if(M->IsMovingOnGround()&&Age>.3f){P->SetActorLocation(FVector(0,0,77));P->SetActorRotation(FRotator::ZeroRotator);P->SetClip(P->IdleAnimation,true,.12f,true);Next();}}
 else if(Stage>=23&&Stage<=26){P->GetMesh()->SetForcedLOD(Stage-22);if(Age>.35f){Shot(*FString::Printf(TEXT("08-lod-%d.png"),Stage-23));Next();}}
 else if(Stage==27&&Age>.3f){Check(MaxBlink>.5f,TEXT("blink_animation"));
  P->SlashTime=0;P->Abilities->SlashRemaining=0;P->CooldownMultiplier=.85f;P->Attack();Check(P->SlashTime>.80f&&P->SlashTime<.82f&&P->Abilities->PendingSlash>.28f&&P->Abilities->PendingSlash<.29f,TEXT("haste_scales_slash_timing"));
  UE_LOG(LogTemp,Display,TEXT("CATMURAI_V10_RUNTIME_%s"),OK?TEXT("PASS"):TEXT("FAIL"));FPlatformMisc::RequestExitWithStatus(false,OK?0:1);}
 if(Age>6){Check(false,TEXT("stage_timeout"));FPlatformMisc::RequestExitWithStatus(false,1);}
}
