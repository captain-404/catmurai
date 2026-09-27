#include "Survival.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/StaticMesh.h"
#include "Engine/World.h"
#include "GameFramework/SpringArmComponent.h"
#include "GameFramework/PlayerController.h"
#include "UnrealClient.h"
#include "Misc/Paths.h"
#include "HAL/FileManager.h"
#include "HAL/PlatformMisc.h"
void ASurvivalGameMode::TickBleepReview(float Delta){
 auto* P=Player();if(!P)return;
 static float Age=0;static int Stage=0;static bool OK=true;
 Age+=Delta;P->Abilities->AutoAttack=false;
 auto Check=[&](bool Pass){OK&=Pass;};
 if(Stage==0){
  for(auto E:EnemyPool){E->Active=false;E->SetActorHiddenInGame(true);}
  EnemyPool.Empty();
  for(int i=0;i<4;++i){auto* E=GetWorld()->SpawnActor<ASurvivalEnemy>();EnemyPool.Add(E);E->Activate(Tuning->Enemies[i%Tuning->Enemies.Num()],FVector(0,(i-1.5f)*220,55),i==3);
   E->SetActorRotation(FRotator(0,0,0));Check(E->BleepMesh&&E->Body->GetStaticMesh()==E->BleepMesh);Check(E->Body->GetStaticMesh()&&E->Body->GetStaticMesh()->GetNumLODs()==3);
   Check(FMath::Abs(E->Body->Bounds.Origin.Z-E->Body->Bounds.BoxExtent.Z)<3);
  }
  P->SetActorLocation(FVector(350,0,77));P->CameraBoom->TargetArmLength=1150;P->CameraBoom->SetRelativeRotation(FRotator(-18,180,0));P->CameraBoom->bEnableCameraLag=false;
  if(auto* PC=Cast<APlayerController>(P->GetController()))if(PC->GetHUD())PC->GetHUD()->bShowHUD=false;
  Stage=1;Age=0;
 }else if(Stage==1&&Age>3){
  const FString Dir=FPaths::ProjectSavedDir()/TEXT("BleepReview");IFileManager::Get().MakeDirectory(*Dir,true);
  FScreenshotRequest::RequestScreenshot(Dir/TEXT("bleep-enemies.png"),false,false);Stage=2;Age=0;
 }else if(Stage==2&&Age>.5f){
  auto* E=EnemyPool[0].Get();const FVector Before=E->GetActorLocation();E->Simulate(.1f,P);Check(FVector::Dist2D(Before,E->GetActorLocation())>1);
  E->Hit(100000,FVector::ForwardVector,0,false);Check(!E->Active);E->Activate(Tuning->Enemies[0],FVector(0,0,55),false);Check(E->Active&&E->Body->GetStaticMesh()==E->BleepMesh);
  UE_LOG(LogTemp,Display,TEXT("BLEEP_RUNTIME_%s"),OK?TEXT("PASS"):TEXT("FAIL"));FPlatformMisc::RequestExitWithStatus(false,OK?0:1);
 }
}
