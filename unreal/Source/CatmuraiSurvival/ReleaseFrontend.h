#pragma once
#include "CoreMinimal.h"
#include "Survival.h"
#include "GameFramework/SaveGame.h"
#include "GameFramework/PlayerController.h"
#include "ReleaseFrontend.generated.h"

UCLASS() class CATMURAISURVIVAL_API UReleaseSaveGame : public USaveGame {
 GENERATED_BODY()
public:
 UPROPERTY() int32 ShardCurrency=0;
 UPROPERTY() int32 DamageRank=0;
 UPROPERTY() int32 HealthRank=0;
 UPROPERTY() int32 FortuneRank=0;
 UPROPERTY() int32 TotalRuns=0;
 UPROPERTY() int32 BestKills=0;
 UPROPERTY() int32 Victories=0;
 UPROPERTY() float VolumeMusic=.65f;
 UPROPERTY() float VolumeSFX=.8f;
 UPROPERTY() int32 Quality=2;
 UPROPERTY() bool bFullScreen=false;
};
struct CATMURAISURVIVAL_API FReleaseFrontend {
 static UReleaseSaveGame* LoadProfile(UObject* Context);
 static bool SaveProfile(UObject* Context,UReleaseSaveGame* Profile);
 static void ApplyProfile(ASurvivalGameMode* Game);
 static void RecordRun(ASurvivalGameMode* Game);
 static void ApplySettings(UObject* Context,UReleaseSaveGame* Profile);
 static int32 PurchaseCost(int32 Rank);
 static bool Purchase(UObject* Context,int32 Upgrade);
};
struct FReleaseFocusRect {FVector2D Position,Size;FName Action;int32 Value=0;bool bSlider=false;};
UCLASS() class CATMURAISURVIVAL_API AReleaseHUD : public ASurvivalHUD {
 GENERATED_BODY()
public:
 virtual void DrawHUD() override;
 TArray<FReleaseFocusRect> FocusRects;
 int32 Focus=0;
 int32 SelectedLoadout=0;
 float UIScale=1;FVector2D UIOrigin=FVector2D::ZeroVector;
 UPROPERTY(Transient) TObjectPtr<UTexture2D> TitleArt;
 void Navigate(int32 Direction);
 void Adjust(int32 Direction);
 void ActivateFocus();
 void Click(float X,float Y);
 void Execute(FName Action,int32 Value=0,float Fraction=-1);
private:
 FVector2D LastMouse=FVector2D(-1,-1);
 ERunState LastDrawState=ERunState::Playing;
};
UCLASS() class CATMURAISURVIVAL_API AReleasePlayerController : public APlayerController {
 GENERATED_BODY()
public:
 AReleasePlayerController();
 virtual void BeginPlay() override;
 virtual void SetupInputComponent() override;
 virtual void PlayerTick(float Delta) override;
 UPROPERTY(Transient) TObjectPtr<UReleaseSaveGame> Profile;
 bool bProfileSaveSucceeded=true;
 ERunState SettingsReturn=ERunState::Title;
 void RefreshInputMode();
 void Navigate(int32 Direction);void ActivateFocus();void Back();
 void MenuUp();void MenuDown();void MenuLeft();void MenuRight();void Click();
 void Ultimate();void MenuCancel();void ToggleAuto();void OpenOptions();
private:
 ERunState LastInputState=ERunState::Playing;
 bool bInputInitialized=false;
};


