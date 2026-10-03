#pragma once
#include "CoreMinimal.h"
#include "Engine/DataAsset.h"
#include "GameFramework/Actor.h"
#include "SpellPresentation.generated.h"

class UTexture2D;
class UStaticMeshComponent;
class UMaterialInterface;
class UMaterialInstanceDynamic;

// Presentation is replaceable; combat never depends on a particular art asset.
UCLASS(BlueprintType)
class CATMURAISURVIVAL_API USpellPresentation : public UDataAsset {
 GENERATED_BODY()
public:
 UPROPERTY(EditAnywhere,BlueprintReadOnly) TMap<FName,TObjectPtr<UTexture2D>> Textures;
 UPROPERTY(EditAnywhere,BlueprintReadOnly) TObjectPtr<UMaterialInterface> EffectMaterial;
 UPROPERTY(EditAnywhere,BlueprintReadOnly) TObjectPtr<UMaterialInterface> IconMaterial;
 UPROPERTY(EditAnywhere,BlueprintReadOnly) TMap<FName,float> BlackKeys;
 UTexture2D* Texture(FName Name) const;
 static FName UpgradeIcon(uint8 Kind);
};

UCLASS()
class CATMURAISURVIVAL_API ASpellEffect : public AActor {
 GENERATED_BODY()
public:
 ASpellEffect();
 virtual void Tick(float Delta) override;
 UPROPERTY(VisibleAnywhere) TObjectPtr<UStaticMeshComponent> Sprite;
 UPROPERTY(Transient) TObjectPtr<UMaterialInstanceDynamic> Dynamic;
 float Age=0,Duration=.3f,Diameter=160,Spin=0,Opacity=.9f;
 float CurrentFade=0;
 static ASpellEffect* Spawn(UObject* Context,FName Art,FVector Location,float Diameter,float Duration,float Yaw=0,float Spin=0,float Opacity=.9f);
};
