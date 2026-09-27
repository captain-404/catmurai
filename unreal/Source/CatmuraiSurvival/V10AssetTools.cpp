#include "V10AssetTools.h"
#include "Engine/SkeletalMesh.h"
#include "Animation/Skeleton.h"
#include "Animation/AnimSequence.h"
#include "Animation/AnimData/IAnimationDataModel.h"
#include "Animation/AnimData/IAnimationDataController.h"
#include "Animation/MorphTarget.h"
#include "Rendering/SkeletalMeshModel.h"

bool UV10AssetTools::ConfigureMesh(USkeletalMesh* Mesh){
#if WITH_EDITOR
 if(!Mesh||Mesh->GetLODNum()!=4)return false;
 const float Screens[]={.6f,.3f,.1f,.02f};
 for(int32 I=0;I<4;++I)Mesh->GetLODInfo(I)->ScreenSize.Default=Screens[I];
 auto* Skeleton=Mesh->GetSkeleton();
 Skeleton->SetBoneTranslationRetargetingMode(Skeleton->GetReferenceSkeleton().FindBoneIndex(TEXT("weapon_r")),EBoneTranslationRetargetingMode::Animation);
 Skeleton->MarkPackageDirty();Mesh->MarkPackageDirty();Mesh->PostEditChange();return true;
#else
 return false;
#endif
}
bool UV10AssetTools::LockGrip(UAnimSequence* Sequence,USkeletalMesh* Mesh){
#if WITH_EDITOR
 if(!Sequence||!Mesh)return false;
 const auto* Model=Sequence->GetDataModel();auto& Controller=Sequence->GetController();
 Controller.OpenBracket(FText::FromString(TEXT("Preserve v10 bind grip")),false);
 for(FName Bone:{FName(TEXT("hand_r")),FName(TEXT("weapon_r"))}){
  const int32 Index=Mesh->GetRefSkeleton().FindBoneIndex(Bone);if(Index==INDEX_NONE)return false;
  TArray<FTransform> Keys;Model->GetBoneTrackTransforms(Bone,Keys);if(Keys.IsEmpty())return false;
  TArray<FVector> Positions,Scales;TArray<FQuat> Rotations;
  float MaxAngle=0;
  for(const auto& Key:Keys){Positions.Add(Key.GetTranslation());Scales.Add(Key.GetScale3D());
   const FQuat Bind=Mesh->GetRefSkeleton().GetRefBonePose()[Index].GetRotation();
   MaxAngle=FMath::Max(MaxAngle,float(FMath::RadiansToDegrees(Key.GetRotation().AngularDistance(Bind))));Rotations.Add(Bind);}
  UE_LOG(LogTemp,Display,TEXT("V10_GRIP %s %s original_max_rotation=%.4f translation=%s"),*Sequence->GetName(),*Bone.ToString(),MaxAngle,*Positions[0].ToString());
  if(!Controller.SetBoneTrackKeys(Bone,Positions,Rotations,Scales,false))return false;
 }
 Controller.CloseBracket(false);Sequence->MarkPackageDirty();return true;
#else
 return false;
#endif
}
bool UV10AssetTools::ValidateAssets(USkeletalMesh* Mesh,const TArray<UAnimSequence*>& Clips){
#if WITH_EDITOR
 if(!Mesh||Mesh->GetLODNum()!=4||Clips.Num()!=9)return false;
 bool OK=true;
 OK&=Mesh->FindMorphTarget(TEXT("Blink"))!=nullptr&&Mesh->FindMorphTarget(TEXT("MouthOpen"))!=nullptr;
 auto* Model=Mesh->GetImportedModel();
 for(int32 I=0;I<Model->LODModels.Num();++I){
  const auto& Lod=Model->LODModels[I];
  UE_LOG(LogTemp,Display,TEXT("V10_LOD %d vertices=%d triangles=%d sections=%d"),I,Lod.NumVertices,Lod.IndexBuffer.Num()/3,Lod.Sections.Num());
  FBox BodyBounds(ForceInit);
  for(const auto& Section:Lod.Sections){
   const FString Slot=Mesh->GetMaterials()[Section.MaterialIndex].ImportedMaterialSlotName.ToString();
   if(!Slot.Contains(TEXT("Katana")))for(const auto& Vertex:Section.SoftVertices)BodyBounds+=FVector(Vertex.Position);
  }
  const float BodyHeight=BodyBounds.GetSize().Z;
  UE_LOG(LogTemp,Display,TEXT("V10_BODY_HEIGHT lod=%d height=%.3f"),I,BodyHeight);
  OK&=BodyHeight>145&&BodyHeight<155;
  OK&=Lod.Sections.Num()>=4;
  if(I>0)OK&=Lod.IndexBuffer.Num()<Model->LODModels[I-1].IndexBuffer.Num();
 }
 for(auto* Clip:Clips){
  if(!Clip||Clip->GetSkeleton()!=Mesh->GetSkeleton()||Clip->GetPlayLength()<=0){OK=false;continue;}
  for(FName Bone:{FName(TEXT("hand_r")),FName(TEXT("weapon_r"))}){
   const int32 Index=Mesh->GetRefSkeleton().FindBoneIndex(Bone);if(Index==INDEX_NONE){OK=false;continue;}
   TArray<FTransform> Keys;Clip->GetDataModel()->GetBoneTrackTransforms(Bone,Keys);OK&=!Keys.IsEmpty();
   for(const auto& Key:Keys)OK&=Key.GetRotation().AngularDistance(Mesh->GetRefSkeleton().GetRefBonePose()[Index].GetRotation())<.001;
  }
 }
 UE_LOG(LogTemp,Display,TEXT("CATMURAI_V10_ASSET_%s height=%.3f bones=%d"),OK?TEXT("PASS"):TEXT("FAIL"),Mesh->GetBounds().BoxExtent.Z*2,Mesh->GetRefSkeleton().GetNum());return OK;
#else
 return false;
#endif
}
