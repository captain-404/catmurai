#pragma once
#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "MoonfallArena.generated.h"
class UHierarchicalInstancedStaticMeshComponent;
class UMaterialInterface;
UCLASS() class CATMURAISURVIVAL_API AMoonfallArena : public AActor {
 GENERATED_BODY()
public:
 AMoonfallArena();
 UFUNCTION(BlueprintCallable) void BuildShrine();
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) int32 InstanceComponentCount=0;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) int32 DecorativeInstanceCount=0;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) int32 SmallLightCount=0;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) float WalkableHalfExtent=2250.f;
 UPROPERTY(VisibleAnywhere,BlueprintReadOnly) bool bBuilt=false;
private:
 UPROPERTY() TArray<TObjectPtr<UHierarchicalInstancedStaticMeshComponent>> Batches;
 UHierarchicalInstancedStaticMeshComponent* Batch(const TCHAR* Name,const TCHAR* Mesh,const TCHAR* Material,FLinearColor Fallback,bool Shadows=true);
 void Put(UHierarchicalInstancedStaticMeshComponent* Batch,FVector Position,FVector Scale,FRotator Rotation=FRotator::ZeroRotator);
};
