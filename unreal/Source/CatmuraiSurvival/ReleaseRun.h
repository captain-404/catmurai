#pragma once
#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "GameFramework/Actor.h"
#include "ReleaseRun.generated.h"
class ASurvivalEnemy;
class ASurvivalGameMode;
class UStaticMeshComponent;
class UMaterialInstanceDynamic;
UCLASS() class CATMURAISURVIVAL_API AShrineBeacon : public AActor {
 GENERATED_BODY()
public:
 AShrineBeacon();
 virtual void BeginPlay() override;
 void SetAttuned(bool Value);
 UPROPERTY() TObjectPtr<UStaticMeshComponent> Disc;
 UPROPERTY() TObjectPtr<UStaticMeshComponent> Crystal;
 UPROPERTY() TObjectPtr<UMaterialInstanceDynamic> Glow;
};
UCLASS() class CATMURAISURVIVAL_API ARunTelegraph : public AActor {
 GENERATED_BODY()
public: ARunTelegraph();
};
USTRUCT() struct FRunHazard {
 GENERATED_BODY()
 FVector Location=FVector::ZeroVector;
 float Radius=240,Remaining=1.2f,Damage=18;
 TWeakObjectPtr<ARunTelegraph> Visual;
 TWeakObjectPtr<ASurvivalEnemy> Source;
};
UCLASS(ClassGroup=(Catmurai),meta=(BlueprintSpawnableComponent)) class CATMURAISURVIVAL_API UReleaseRunComponent : public UActorComponent {
 GENERATED_BODY()
public:
 UReleaseRunComponent();
 virtual void TickComponent(float Delta,ELevelTick TickType,FActorComponentTickFunction* Function) override;
 UFUNCTION(BlueprintCallable) void ResetRun();
 void OnEnemyDefeated(ASurvivalEnemy* Enemy);
 void OnPickupXP(int32 Amount);
 bool IsFinalBoss(const ASurvivalEnemy* Enemy) const;
 void RewardComplete();
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) FString ObjectiveTitle;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) FString ObjectiveDescription;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) FString PhaseTitle;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) FVector TargetLocation=FVector::ZeroVector;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) float ObjectiveProgress=0;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) float ObjectiveGoal=40;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) int32 ObjectivesCompleted=0;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) int32 BossPhase=0;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) bool bFinalBossActive=false;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) int32 HazardsDodged=0;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) int32 HazardHits=0;
 UPROPERTY() TArray<TObjectPtr<AShrineBeacon>> Beacons;
 TArray<FRunHazard> Hazards;
private:
 int32 ObjectiveKind=0,BeaconIndex=0,Chapter=0;
 bool bRewarding=false;
 float BossAttackTimer=3.5f,PhaseTime=0;
 TWeakObjectPtr<ASurvivalEnemy> CurrentBoss,FinalBoss;
 void NextObjective();
 bool SpawnBoss(int32 Phase);
 void QueueHazard(FVector Location,float Radius,float Damage);
 ASurvivalGameMode* Mode() const;
};


