#pragma once
#include "CoreMinimal.h"
#include "Animation/AnimInstance.h"
#include "Animation/AnimInstanceProxy.h"
#include "V10Anim.generated.h"

// Native equivalent of the handoff state graph; animation never drives the capsule.
UCLASS(Transient, Blueprintable)
class CATMURAISURVIVAL_API UV10AnimInstance : public UAnimInstance {
 GENERATED_BODY()
public:
 virtual FAnimInstanceProxy* CreateAnimInstanceProxy() override;
 virtual void DestroyAnimInstanceProxy(FAnimInstanceProxy* Proxy) override;
};

struct FV10AnimProxy : public FAnimInstanceProxy {
 FV10AnimProxy(UAnimInstance* Instance):FAnimInstanceProxy(Instance){}
 virtual void PreUpdate(UAnimInstance* Instance,float Delta) override;
 virtual bool Evaluate(FPoseContext& Output) override;
 UAnimSequence* Clip=nullptr;
 UAnimSequence* AirFall=nullptr;
 float Time=0,BlendTime=0,BlendDuration=.12f,AirAlpha=0;
 bool Loop=true;
 TArray<FTransform> PreviousPose,LastPose;
 TMap<FName,FVector> SpringAngles,SpringVelocities;
 FVector PreviousVelocity=FVector::ZeroVector;
 float Step=0;
 FVector LocalAcceleration=FVector::ZeroVector;
};
