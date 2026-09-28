#include "Survival.h"
#include "V10Anim.h"
#include "Animation/AnimSequence.h"
#include "Components/SkeletalMeshComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Kismet/GameplayStatics.h"
#include "Camera/CameraComponent.h"

void ACatmuraiCharacter::SetClip(UAnimSequence* Clip,bool Loop,float Blend,bool Restart){
 if(!Clip)return;
 if(CurrentAnimation!=Clip||Restart){CurrentAnimation=Clip;AnimationAge=0;AnimationBlend=Blend;++AnimationSerial;}
 bAnimationLoop=Loop;bAirBlend=false;AnimationRate=1;
}
void ACatmuraiCharacter::BeginPlay(){
 Super::BeginPlay();GetMesh()->SetAnimInstanceClass(UV10AnimInstance::StaticClass());
 GetMesh()->VisibilityBasedAnimTickOption=EVisibilityBasedAnimTickOption::AlwaysTickPoseAndRefreshBones;
 SetClip(IdleAnimation,true,.12f);BlinkWait=FMath::FRandRange(2.2f,5.5f);
 UE_LOG(LogTemp,Display,TEXT("CATMURAI_V10: %s; native animation graph, grip lock, springs and face enabled"),*GetNameSafe(GetMesh()->GetSkeletalMeshAsset()));
}
void ACatmuraiCharacter::PlaySlashAnimation(){ActiveSlashDuration=FMath::Max(.12f,.95f*CooldownMultiplier);SlashVisualTime=ActiveSlashDuration;SlashTime=ActiveSlashDuration;SetClip(SlashAnimation,false,.06f,true);}
void ACatmuraiCharacter::PressJump(){
 auto* G=Cast<ASurvivalGameMode>(UGameplayStatics::GetGameMode(this));if(!G||G->State!=ERunState::Playing)return;
 bJumpHeld=true;JumpBuffer=.12f;TryBufferedJump(true);
}
void ACatmuraiCharacter::ReleaseJump(){bJumpHeld=false;}
void ACatmuraiCharacter::TryBufferedJump(bool Fresh){
 auto* M=GetCharacterMovement();const bool Ground=M->IsMovingOnGround();
 const bool First=(Ground||CoyoteRemaining>0||(DashTime>0&&bDashStartedGrounded))&&!bJumpInProgress;
 if(JumpBuffer<=0||(!First&&!(Fresh&&!Ground&&AirJumpsUsed<1)))return;
 if(!First)++AirJumpsUsed;
 FVector Horizontal(M->Velocity.X,M->Velocity.Y,0);
 if(!First)Horizontal=FMath::Lerp(Horizontal,MoveDirection.GetClampedToMaxSize(1)*BaseMoveSpeed,.6f);
 if(DashTime>0){DashTime=0;Horizontal=DashDirection*BaseMoveSpeed*1.3f;}
 M->SetMovementMode(MOVE_Falling);M->Velocity=Horizontal;
 M->Velocity.Z=FMath::Sqrt(2.f*3061.f*168.f*(First?1.f:.9f));
 M->GravityScale=3061.f/980.f;JumpBuffer=0;CoyoteRemaining=0;bJumpInProgress=true;
 SetClip(First?JumpStartAnimation.Get():JumpFlipAnimation.Get(),false,First?.05f:.04f,true);
 UE_LOG(LogTemp,Display,TEXT("V10_MOVE %s vz=%.2f"),First?TEXT("Jump"):TEXT("DoubleJump"),M->Velocity.Z);
}
void ACatmuraiCharacter::Landed(const FHitResult& Hit){
 Super::Landed(Hit);AirJumpsUsed=0;AirDashesUsed=0;bJumpInProgress=false;AirTime=0;CoyoteRemaining=.12f;
 if(DashTime<=0&&SlashTime<=0)SetClip(JumpLandAnimation,false,.04f,true);
}
void ACatmuraiCharacter::Dash(){
 auto* G=Cast<ASurvivalGameMode>(UGameplayStatics::GetGameMode(this));auto* M=GetCharacterMovement();
 if(!G||G->State!=ERunState::Playing||DashCooldown>0||DashTime>0||(!M->IsMovingOnGround()&&AirDashesUsed>=1))return;
 if(!M->IsMovingOnGround())++AirDashesUsed;
 // Dash cancels the slash, including a strike whose windup has not completed.
 SlashTime=0;SlashVisualTime=0;Abilities->PendingSlash=-1;
 bDashStartedGrounded=M->IsMovingOnGround();DashTime=.2f;DashCooldown=.6f;DashDirection=MoveDirection.GetSafeNormal2D();
 if(DashDirection.IsNearlyZero())DashDirection=GetActorForwardVector();SetActorRotation(DashDirection.Rotation());
 M->Velocity=DashDirection*3060;M->GravityScale=0;SetClip(DashAnimation,false,.03f,true);CombatCue(TEXT("Dash"),GetActorLocation(),false);
}
void ACatmuraiCharacter::Forward(float Value){
 auto* G=Cast<ASurvivalGameMode>(UGameplayStatics::GetGameMode(this));if(!G||G->State!=ERunState::Playing)return;
 ForwardInput=Value;AddMovementInput(FRotator(0,Camera->GetComponentRotation().Yaw,0).Vector(),Value);
}
void ACatmuraiCharacter::Right(float Value){
 auto* G=Cast<ASurvivalGameMode>(UGameplayStatics::GetGameMode(this));if(!G||G->State!=ERunState::Playing)return;
 RightInput=Value;AddMovementInput(FRotator(0,Camera->GetComponentRotation().Yaw+90,0).Vector(),Value);
}
void ACatmuraiCharacter::Tick(float Delta){
 Super::Tick(Delta);auto* G=Cast<ASurvivalGameMode>(UGameplayStatics::GetGameMode(this));if(!G||G->State!=ERunState::Playing)return;
 auto* M=GetCharacterMovement();const bool Ground=M->IsMovingOnGround();
 MoveDirection=(FRotator(0,Camera->GetComponentRotation().Yaw,0).Vector()*ForwardInput+FRotator(0,Camera->GetComponentRotation().Yaw+90,0).Vector()*RightInput).GetClampedToMaxSize(1);
 DashCooldown=FMath::Max(0.f,DashCooldown-Delta);ImpactTime=FMath::Max(0.f,ImpactTime-Delta);
 if(Ground&&!bJumpInProgress){CoyoteRemaining=.12f;AirTime=0;}else{CoyoteRemaining=FMath::Max(0.f,CoyoteRemaining-Delta);AirTime+=Delta;}
 TryBufferedJump(false);JumpBuffer=FMath::Max(0.f,JumpBuffer-Delta);
 SlashTime=FMath::Max(0.f,SlashTime-Delta);SlashVisualTime=SlashTime;
 M->bOrientRotationToMovement=DashTime<=0;
 M->MaxWalkSpeed=BaseMoveSpeed*(Ground?1.f:.85f);
 M->MaxAcceleration=2449;M->BrakingDecelerationWalking=3367;M->BrakingDecelerationFalling=3367;
 M->GroundFriction=0;M->BrakingFrictionFactor=0;
 if(DashTime>0){
  // MOVE_Flying provides a swept, collision-aware constant-speed burst with no gravity.
  if(M->MovementMode!=MOVE_Flying){M->SetMovementMode(MOVE_Flying);}
  M->MaxFlySpeed=3060;M->BrakingDecelerationFlying=0;M->Velocity=DashDirection*3060;M->GravityScale=0;
  DashTime=FMath::Max(0.f,DashTime-Delta);
  if(DashTime<=0){M->SetMovementMode(MOVE_Falling);M->Velocity=DashDirection*BaseMoveSpeed;}
 }else M->GravityScale=(3061.f/980.f)*((M->Velocity.Z<0||(bJumpInProgress&&!bJumpHeld))?1.6f:1.f);
 AnimationAge+=Delta;
 const float Speed=GetVelocity().Size2D();const bool AnimGround=Ground||(!bJumpInProgress&&AirTime<.12f);
 bool Hold=false;
 if(SlashAnimation&&CurrentAnimation==SlashAnimation&&SlashTime>0)Hold=true;
 else if(JumpStartAnimation&&CurrentAnimation==JumpStartAnimation&&AnimationAge<JumpStartAnimation->GetPlayLength()*.85f)Hold=true;
 else if(JumpFlipAnimation&&CurrentAnimation==JumpFlipAnimation&&AnimationAge<JumpFlipAnimation->GetPlayLength()*.92f&&!(Ground&&AnimationAge>JumpFlipAnimation->GetPlayLength()*.5f))Hold=true;
 else if(DashAnimation&&CurrentAnimation==DashAnimation&&AnimationAge<DashAnimation->GetPlayLength()*((!AnimGround||Speed>36.7f)?.55f:.85f))Hold=true;
 else if(JumpLandAnimation&&CurrentAnimation==JumpLandAnimation&&Ground&&AnimationAge<JumpLandAnimation->GetPlayLength()*(Speed>36.7f?.3f:.92f))Hold=true;
 if(!Hold){
  if(!AnimGround){SetClip(JumpRiseAnimation,true,.10f);bAirBlend=true;}
  else if((CurrentAnimation==JumpRiseAnimation||CurrentAnimation==JumpFlipAnimation||CurrentAnimation==JumpStartAnimation)&&Ground)SetClip(JumpLandAnimation,false,.04f);
  else SetClip(Speed>36.7f?RunAnimation.Get():IdleAnimation.Get(),true,.12f);
 }
 if(CurrentAnimation==RunAnimation)AnimationRate=FMath::Clamp(Speed/130.f,.8f,4.f);
 else if(SlashAnimation&&CurrentAnimation==SlashAnimation)AnimationRate=SlashAnimation->GetPlayLength()/ActiveSlashDuration;
 FaceTime+=Delta;float Blink=0;
 if(BlinkPhase>=0){BlinkPhase+=Delta;Blink=BlinkPhase<.06f?BlinkPhase/.06f:BlinkPhase<.11f?1.f:FMath::Max(0.f,1.f-(BlinkPhase-.11f)/.11f);
  if(BlinkPhase>=.22f){BlinkPhase=-1;BlinkWait=bDoubleBlink?.1f:FMath::FRandRange(2.2f,5.5f);bDoubleBlink=false;}}
 else if((BlinkWait-=Delta)<=0){BlinkPhase=0;bDoubleBlink=FMath::FRand()<.2f;}
 const float Mouth=SlashTime>0?.9f:(Speed>36.7f?.6f*(.5f+.5f*FMath::Sin(FaceTime*2*PI/.6f)):.28f*(.5f+.5f*FMath::Sin(FaceTime*2*PI/3.4f)));
 GetMesh()->SetMorphTarget(TEXT("Blink"),Blink);GetMesh()->SetMorphTarget(TEXT("MouthOpen"),Mouth);
 FVector L=GetActorLocation();L.X=FMath::Clamp(L.X,-2250.f,2250.f);L.Y=FMath::Clamp(L.Y,-2250.f,2250.f);if(L.Z<-100)L.Z=100;SetActorLocation(L);
}
