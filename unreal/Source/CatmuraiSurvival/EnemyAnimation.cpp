#include "Survival.h"
#include "Components/StaticMeshComponent.h"
#include "Kismet/GameplayStatics.h"

void ASurvivalEnemy::Tick(float Delta){
 Super::Tick(Delta);
 auto* G=Cast<ASurvivalGameMode>(UGameplayStatics::GetGameMode(this));
 if(!G||G->State!=ERunState::Playing)return;
 // Preserve responsive attacks up close; distant presentation can update at 30 Hz.
 if(auto* P=G->Player()){
  const float Distance=FVector::DistSquared2D(GetActorLocation(),P->GetActorLocation());
  SetActorTickInterval(Active&&Distance>FMath::Square(1100.f)?1.f/30.f:0.f);
  if(BleepMesh)Body->SetForcedLodModel(Elite||Distance<FMath::Square(450.f)?1:Distance<FMath::Square(1200.f)?2:3);
 }
 AnimatePresentation(Delta);
}

void ASurvivalEnemy::AnimatePresentation(float Delta){
 if(!Active&&DeathRemaining<=0){SetActorHiddenInGame(true);SetActorTickEnabled(false);return;}
 AnimationClock+=Delta;
 HitReaction=FMath::Max(0.f,HitReaction-Delta);StrikeVisual=FMath::Max(0.f,StrikeVisual-Delta);
 const float ActorScale=FMath::Max(.01f,GetActorScale3D().Z);
 FVector Offset(0,0,BleepMesh?-55.f/ActorScale:0.f);FVector Scale=FVector::OneVector;
 float Lean=0,Roll=0;
 if(!Active){
  DeathRemaining=FMath::Max(0.f,DeathRemaining-Delta);AnimationPose=EEnemyPose::Death;
  const float T=1-DeathRemaining/.32f;const float Shrink=FMath::Max(.01f,1-T*T);
  Scale=FVector(Shrink*(1+.3f*T),Shrink*(1+.3f*T),Shrink);
  Offset.Z+=FMath::Sin(T*PI)*28/ActorScale;Roll=T*65;
  if(DeathRemaining<=0){SetActorHiddenInGame(true);SetActorTickEnabled(false);}
 }else if(HitReaction>0){
  AnimationPose=EEnemyPose::Hurt;const float T=HitReaction/.18f;
  Scale=FVector(1+.12f*T,1+.12f*T,1-.17f*T);Lean=-16*T;
  Offset.Y=FMath::Sin(AnimationClock*70)*5*T/ActorScale;
 }else if(Windup>0){
  AnimationPose=EEnemyPose::Windup;const float Duration=Elite?.85f:Spec.Scale>1.2f?.65f:.45f;
  const float T=1-FMath::Clamp(Windup/Duration,0.f,1.f);
  Scale=FVector(1+.15f*T,1+.15f*T,1-.22f*T);Lean=-12*T;
 }else if(StrikeVisual>0){
  AnimationPose=EEnemyPose::Strike;const float T=StrikeVisual/.18f;
  Offset.X=FMath::Sin(T*PI)*36/ActorScale;Scale=FVector(.92f,.92f,1.12f);Lean=20*T;
 }else if(bPursuing){
  AnimationPose=EEnemyPose::Run;const float Phase=AnimationClock*(Spec.Speed/170.f)*10+PosePhase;
  const float Step=FMath::Abs(FMath::Sin(Phase));Offset.Z+=Step*(Elite?8:12)/ActorScale;
  const float Squash=FMath::Sin(Phase*2)*.045f;
  Scale=FVector(1-Squash*.5f,1-Squash*.5f,1+Squash);Lean=8;Roll=FMath::Sin(Phase)*4;
 }else{
  AnimationPose=EEnemyPose::Idle;const float Breath=FMath::Sin(AnimationClock*2.2f+PosePhase)*.025f;
  Scale=FVector(1-Breath*.4f,1-Breath*.4f,1+Breath);
 }
 Body->SetRelativeLocation(Offset);Body->SetRelativeScale3D(Scale);
 Body->SetRelativeRotation(BleepMesh?FRotator(Roll,-90,Lean):FRotator(Lean,0,Roll));
}
