#pragma once
#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "GameFramework/Actor.h"
#include "Survival.h"
#include "ReleaseCombat.generated.h"

// Combat state belongs to the run; damage never depends on presentation assets.
UCLASS(ClassGroup=(Catmurai))
class CATMURAISURVIVAL_API UReleaseCombatComponent : public UActorComponent {
 GENERATED_BODY()
public:
 UReleaseCombatComponent();
 virtual void TickComponent(float Delta,ELevelTick TickType,FActorComponentTickFunction* Function) override;
 virtual void EndPlay(const EEndPlayReason::Type Reason) override;
 void AddRank(EUpgradeKind Kind);
 void ApplyInfusion(ASurvivalEnemy* Enemy,float HitDamage);
 void ResetCombat();
 void CastUltimate();
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) float UltimateCharge=0;
 void OnEnemyDefeated(ASurvivalEnemy* Enemy=nullptr);
 FString SynergyName() const;
 void FireEnemyBolt(ASurvivalEnemy* Enemy,FVector Direction,float Telegraph=.75f);
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) int32 EmberRank=0;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) int32 FrostRank=0;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) int32 StormRank=0;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) int32 OrbitRank=0;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) int32 SiphonRank=0;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) int32 FortuneRank=0;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) int32 SynergyTriggers=0;
private:
 bool bInProc=false;
 float ProcCooldown=0,StormCooldown=0,OrbitAge=0,OrbitHitTimer=0,HealCueCooldown=0;
 TArray<TWeakObjectPtr<ASpellEffect>> Blades;
 TArray<TWeakObjectPtr<class AEnemyBolt>> Bolts;
 void Burst(ASurvivalEnemy* Source,float Radius,float Damage,FName Art,int32 HitLimit);
 void Chain(ASurvivalEnemy* Source,float Damage,int32 Count);
};

// Telegraph + swept hit test; actor and its visual have explicit run-relative lifetimes.
UCLASS()
class CATMURAISURVIVAL_API AEnemyBolt : public AActor {
 GENERATED_BODY()
public:
 AEnemyBolt();
 virtual void Tick(float Delta) override;
 virtual void EndPlay(const EEndPlayReason::Type Reason) override;
 FVector Direction=FVector::ForwardVector;
 float Damage=9,Speed=460,Life=5,Charge=.75f;
 UPROPERTY(Transient) TObjectPtr<ASpellEffect> Visual;
 bool bVisualAttempted=false;
};

