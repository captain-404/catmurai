#include "V10Anim.h"
#include "Survival.h"
#include "Animation/AnimSequence.h"
#include "Animation/AnimationPoseData.h"
#include "Components/SkeletalMeshComponent.h"
#include "Engine/SkeletalMesh.h"
#include "GameFramework/CharacterMovementComponent.h"

FAnimInstanceProxy* UV10AnimInstance::CreateAnimInstanceProxy(){return new FV10AnimProxy(this);}
void UV10AnimInstance::DestroyAnimInstanceProxy(FAnimInstanceProxy* Proxy){delete Proxy;}
void FV10AnimProxy::PreUpdate(UAnimInstance* Instance,float Delta){
 FAnimInstanceProxy::PreUpdate(Instance,Delta);
 auto* P=Cast<ACatmuraiCharacter>(Instance->TryGetPawnOwner());if(!P)return;
 Step=FMath::Min(Delta,.05f);
 const FVector V=P->GetVelocity();
 LocalAcceleration=P->GetActorQuat().UnrotateVector((V-PreviousVelocity)/FMath::Max(Delta,.001f));PreviousVelocity=V;
 UAnimSequence* Desired=P->CurrentAnimation;
 if(Desired!=Clip||P->AnimationSerial!=P->ConsumedAnimationSerial){
  PreviousPose=LastPose;Clip=Desired;Time=0;BlendTime=0;BlendDuration=P->AnimationBlend;
  P->ConsumedAnimationSerial=P->AnimationSerial;
 }
 Loop=P->bAnimationLoop;
 Time+=Delta*P->AnimationRate;BlendTime+=Delta;
 AirFall=P->bAirBlend?P->JumpFallAnimation.Get():nullptr;
 AirAlpha=FMath::Clamp((153.f-V.Z)/536.f,0.f,1.f);
}
bool FV10AnimProxy::Evaluate(FPoseContext& Output){
 Output.ResetToRefPose();if(!Clip)return true;
 auto Sample=[](UAnimSequence* Seq,float T,bool Looping,FPoseContext& Pose){
  const float Length=Seq->GetPlayLength();
  FAnimationPoseData Data(Pose);
  Seq->GetAnimationPose(Data,FAnimExtractContext(Looping?FMath::Fmod(T,FMath::Max(.001f,Length)):FMath::Min(T,Length),false));
 };
 Sample(Clip,Time,Loop,Output);
 if(AirFall){
  FPoseContext Fall(this);Fall.ResetToRefPose();Sample(AirFall,Time,true,Fall);
  for(FCompactPoseBoneIndex I:Output.Pose.ForEachBoneIndex())Output.Pose[I].Blend(Output.Pose[I],Fall.Pose[I],AirAlpha);
 }
 const float Weight=FMath::Clamp(BlendTime/FMath::Max(.001f,BlendDuration),0.f,1.f);
 if(PreviousPose.Num()==Output.Pose.GetNumBones()&&Weight<1){
  for(FCompactPoseBoneIndex I:Output.Pose.ForEachBoneIndex())Output.Pose[I].Blend(PreviousPose[I.GetInt()],Output.Pose[I],Weight);
 }
 LastPose.SetNum(Output.Pose.GetNumBones());
 const auto& Bones=Output.Pose.GetBoneContainer();
 const auto& Ref=Bones.GetReferenceSkeleton();
 for(FCompactPoseBoneIndex I:Output.Pose.ForEachBoneIndex()){
  const int32 MeshIndex=Bones.MakeMeshPoseIndex(I).GetInt();
  const FName Bone=Ref.GetBoneName(MeshIndex);const FString Name=Bone.ToString();
  FTransform& Transform=Output.Pose[I];
  // Non-negotiable grip constraint: local wrist and weapon rotations remain at bind.
  // Translation is intentionally untouched, preserving the supplied 3.5 cm grip offset.
  if(Bone==TEXT("hand_r")||Bone==TEXT("weapon_r"))Transform.SetRotation(Ref.GetRefBonePose()[MeshIndex].GetRotation());
  LastPose[I.GetInt()]=Transform; // Store the authored pose, never accumulate spring offsets.
  float Stiffness=0,Drag=0,Limit=0,Gravity=0,Influence=.5f;
  if(Name.StartsWith(TEXT("cape_"))){Stiffness=.07f;Drag=.10f;Limit=55;Gravity=-6;}
  else if(Name.StartsWith(TEXT("tail_"))&&Name!=TEXT("tail_05")&&Name!=TEXT("tail_06")){Stiffness=.25f;Drag=.20f;Limit=20;Gravity=-1;Influence=.2f;if(Name==TEXT("tail_04"))Limit*=.4f;}
  else if(Name.StartsWith(TEXT("ear_"))){Stiffness=.6f;Drag=.35f;Limit=6;}
  else if(Name.StartsWith(TEXT("ribbon_"))){Stiffness=.05f;Drag=.06f;Limit=70;Gravity=-4;Influence=.6f;}
  if(Stiffness>0){
   FVector& A=SpringAngles.FindOrAdd(Bone);FVector& V=SpringVelocities.FindOrAdd(Bone);
   const FVector Force=FVector(-LocalAcceleration.Y,LocalAcceleration.X,Gravity*100)*(.001f*Influence);
   // Bounded 60 Hz substeps give consistent damped secondary motion across frame rates.
   float Remaining=Step;
   while(Remaining>0){const float Dt=FMath::Min(Remaining,1.f/60);const float Scale=Dt*60;
    V+=(Force-A*Stiffness)*Scale;V*=FMath::Pow(1.f-Drag,Scale);A=(A+V*Scale).GetClampedToMaxSize(Limit);Remaining-=Dt;}
   Transform.SetRotation((Transform.GetRotation()*FRotator(A.Y,A.Z,A.X).Quaternion()).GetNormalized());
  }
 }
 return true;
}
