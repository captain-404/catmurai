#pragma once
#include "CoreMinimal.h"
#include "Engine/DataAsset.h"
#include "Components/ActorComponent.h"
#include "GameFramework/Character.h"
#include "GameFramework/GameModeBase.h"
#include "GameFramework/HUD.h"
#include "Survival.generated.h"

class UStaticMeshComponent;
class USpringArmComponent;
class UCameraComponent;
class ASurvivalEnemy;
class AXPOrb;
class ACatmuraiCharacter;

UENUM(BlueprintType) enum class ERunState : uint8 { Playing, Choosing, Paused, Victory, Defeat };
UENUM(BlueprintType) enum class EAbilityKind : uint8 { Katana, FlyingSlash };
UENUM(BlueprintType) enum class EUpgradeKind : uint8 { Damage, Speed, Critical, Area, Haste, Vitality, Projectiles, Knockback };
UENUM(BlueprintType) enum class ERarity : uint8 { Common, Rare, Epic, Legendary };
USTRUCT(BlueprintType) struct FAbilitySpec {
 GENERATED_BODY()
 UPROPERTY(EditAnywhere,BlueprintReadWrite) EAbilityKind Kind=EAbilityKind::Katana;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) float Damage=24;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) float Cooldown=0.8f;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) float Range=260;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) float Duration=2;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) int32 Level=1;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) int32 ProjectileCount=1;
};
USTRUCT(BlueprintType) struct FEnemySpec {
 GENERATED_BODY()
 UPROPERTY(EditAnywhere,BlueprintReadWrite) FName Name="Ronin Rat";
 UPROPERTY(EditAnywhere,BlueprintReadWrite) float Health=45;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) float Speed=170;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) float Damage=9;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) float Scale=1;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) float Threat=1;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) int32 XP=2;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) FLinearColor Color=FLinearColor(0.7f,0.2f,0.3f);
};
USTRUCT(BlueprintType) struct FUpgradeSpec {
 GENERATED_BODY()
 UPROPERTY(EditAnywhere,BlueprintReadWrite) FName Name;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) FString Description;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) EUpgradeKind Kind=EUpgradeKind::Damage;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) ERarity Rarity=ERarity::Common;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) float Amount=0.2f;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) int32 MaxRank=5;
};
UCLASS(BlueprintType) class CATMURAISURVIVAL_API USurvivalTuning : public UDataAsset {
 GENERATED_BODY()
public:
 UPROPERTY(EditAnywhere,BlueprintReadOnly) TArray<FAbilitySpec> Abilities;
 UPROPERTY(EditAnywhere,BlueprintReadOnly) TArray<FEnemySpec> Enemies;
 UPROPERTY(EditAnywhere,BlueprintReadOnly) TArray<FUpgradeSpec> Upgrades;
 UPROPERTY(EditAnywhere,BlueprintReadOnly,meta=(ClampMin="20")) float RunDuration=180;
 UPROPERTY(EditAnywhere,BlueprintReadOnly,meta=(ClampMin="1",ClampMax="300")) int32 PopulationCap=80;
};
UCLASS(ClassGroup=(Catmurai),meta=(BlueprintSpawnableComponent)) class CATMURAISURVIVAL_API UHealthComponent : public UActorComponent {
 GENERATED_BODY()
public:
 UPROPERTY(EditAnywhere,BlueprintReadOnly) float Maximum=120;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) float Current=120;
 UFUNCTION(BlueprintCallable) bool Hurt(float Amount);
 UFUNCTION(BlueprintCallable) void Heal(float Amount);
};
UCLASS(ClassGroup=(Catmurai),meta=(BlueprintSpawnableComponent)) class CATMURAISURVIVAL_API UAbilityComponent : public UActorComponent {
 GENERATED_BODY()
public:
 UAbilityComponent();
 virtual void TickComponent(float Delta,ELevelTick TickType,FActorComponentTickFunction* Function) override;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) TArray<FAbilitySpec> Abilities;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) bool AutoAttack=true;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) float SlashRemaining=0;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) float WaveRemaining=0;
 UFUNCTION(BlueprintCallable) void Katana();
 UFUNCTION(BlueprintCallable) void FlyingSlash();
};
UCLASS() class CATMURAISURVIVAL_API ACatmuraiCharacter : public ACharacter {
 GENERATED_BODY()
public:
 ACatmuraiCharacter();
 virtual void BeginPlay() override;
 virtual void Tick(float Delta) override;
 virtual void SetupPlayerInputComponent(UInputComponent* Input) override;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) TObjectPtr<UHealthComponent> Health;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) TObjectPtr<UAbilityComponent> Abilities;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) TObjectPtr<USceneComponent> Presentation;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) TObjectPtr<USceneComponent> SwordPivot;
 UPROPERTY() TObjectPtr<class UAnimSequence> IdleAnimation;
 UPROPERTY() TObjectPtr<class UAnimSequence> RunAnimation;
 UPROPERTY() TObjectPtr<class UAnimSequence> SlashAnimation;
 UPROPERTY(Transient) TObjectPtr<class UAnimSequence> CurrentAnimation;
 float SlashVisualTime=0;
 void PlaySlashAnimation();
 UPROPERTY(VisibleAnywhere) TObjectPtr<USpringArmComponent> CameraBoom;
 UPROPERTY(VisibleAnywhere) TObjectPtr<UCameraComponent> Camera;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) float DamageMultiplier=1;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) float AreaMultiplier=1;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) float CooldownMultiplier=1;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) float CritChance=0.08f;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) float CritMultiplier=2;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) float Knockback=220;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) float DashCooldown=0;
 float DashTime=0,SlashTime=0,ImpactTime=0; FVector DashDirection;
 int32 ExtraProjectiles=0;
 UFUNCTION(BlueprintCallable) void Dash();
 UFUNCTION(BlueprintCallable) void ApplyIncomingHit(float Amount);
 UFUNCTION(BlueprintImplementableEvent) void CombatCue(FName Cue,FVector Location,bool Critical);
 void Forward(float Value); void Right(float Value); void Attack(); void Special();
 void Choice1(); void Choice2(); void Choice3(); void PauseRun(); void Retry();
};
UCLASS() class CATMURAISURVIVAL_API ASurvivalEnemy : public AActor {
 GENERATED_BODY()
public:
 ASurvivalEnemy();
 UPROPERTY(VisibleAnywhere) TObjectPtr<UStaticMeshComponent> Body;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) TObjectPtr<UHealthComponent> Health;
 UPROPERTY(EditAnywhere,BlueprintReadWrite) FEnemySpec Spec;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) bool Active=false;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) bool Elite=false;
 FVector Push; float AttackTimer=0,Windup=0;
 void Activate(const FEnemySpec& InSpec,FVector Location,bool IsElite);
 void Simulate(float Delta,ACatmuraiCharacter* Player);
 void Hit(float Damage,FVector Direction,float Force,bool Critical);
};
UCLASS() class CATMURAISURVIVAL_API AXPOrb : public AActor {
 GENERATED_BODY()
public:
 AXPOrb();
 int32 Value=2; bool Active=false;
 void Activate(FVector Location,int32 InValue);
 void Simulate(float Delta,ACatmuraiCharacter* Player);
};
UCLASS() class CATMURAISURVIVAL_API ACombatProjectile : public AActor {
 GENERATED_BODY()
public:
 ACombatProjectile();
 virtual void Tick(float Delta) override;
 FVector Direction=FVector::ForwardVector; float Damage=18; float Life=2;
 TSet<TWeakObjectPtr<ASurvivalEnemy>> Struck;
};
USTRUCT() struct FCombatNumber {
 GENERATED_BODY()
 FVector Position; float Value=0; float Life=0.65f; bool Critical=false;
};
UCLASS() class CATMURAISURVIVAL_API ASurvivalGameMode : public AGameModeBase {
 GENERATED_BODY()
public:
 ASurvivalGameMode();
 virtual void BeginPlay() override;
 virtual void Tick(float Delta) override;
 UPROPERTY(EditDefaultsOnly,BlueprintReadOnly) TObjectPtr<USurvivalTuning> Tuning;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) ERunState State=ERunState::Playing;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) float RunTime=0;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) int32 Level=1;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) int32 XP=0;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) int32 Kills=0;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) TArray<FUpgradeSpec> Choices;
 UPROPERTY() TArray<TObjectPtr<ASurvivalEnemy>> EnemyPool;
 UPROPERTY() TArray<TObjectPtr<AXPOrb>> OrbPool;
 TArray<FCombatNumber> Numbers;
 TMap<EUpgradeKind,int32> Ranks;
 float DirectorBudget=0,DirectorTimer=0,AITimer=0; bool EliteSpawned=false;
 UFUNCTION(BlueprintCallable) void AddXP(int32 Amount);
 UFUNCTION(BlueprintCallable) void Choose(int32 Index);
 UFUNCTION(BlueprintCallable) void SetRunState(ERunState NewState);
 UFUNCTION(BlueprintCallable) void TogglePause();
 void OfferUpgrades(); void BuildArena(); void EnsureDefaults(); void SpawnEnemy(bool Elite=false);
 void EnemyDefeated(ASurvivalEnemy* Enemy); void ApplyUpgrade(const FUpgradeSpec& Upgrade);
 ACatmuraiCharacter* Player() const;
 int32 ActiveCount() const;
};
UCLASS() class CATMURAISURVIVAL_API ASurvivalHUD : public AHUD {
 GENERATED_BODY()
public:
 UPROPERTY(Transient) TObjectPtr<class UFont> BrushFont;
 virtual void DrawHUD() override;
};

