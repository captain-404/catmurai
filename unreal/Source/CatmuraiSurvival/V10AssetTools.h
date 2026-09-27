#pragma once
#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "V10AssetTools.generated.h"
class USkeletalMesh;
class UAnimSequence;
UCLASS()
class CATMURAISURVIVAL_API UV10AssetTools : public UBlueprintFunctionLibrary {
 GENERATED_BODY()
public:
 UFUNCTION(BlueprintCallable) static bool ConfigureMesh(USkeletalMesh* Mesh);
 UFUNCTION(BlueprintCallable) static bool LockGrip(UAnimSequence* Sequence,USkeletalMesh* Mesh);
 UFUNCTION(BlueprintCallable) static bool ValidateAssets(USkeletalMesh* Mesh,const TArray<UAnimSequence*>& Clips);
};
