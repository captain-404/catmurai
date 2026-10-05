#include "ReleaseFrontend.h"
#include "SpellPresentation.h"
#include "ReleaseCombat.h"
#include "ReleaseRun.h"
#include "ReleaseAudio.h"
#include "Kismet/KismetSystemLibrary.h"
#include "Kismet/GameplayStatics.h"
#include "Engine/Canvas.h"
#include "Engine/Engine.h"
#include "Engine/Font.h"
#include "Engine/GameInstance.h"
#include "GameFramework/GameUserSettings.h"
#include "Components/InputComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Materials/MaterialInstanceDynamic.h"


#include "Misc/CommandLine.h"
#include "Misc/Parse.h"
#include "Misc/Paths.h"

namespace {
 ASurvivalGameMode* Game(const UObject* C){return Cast<ASurvivalGameMode>(UGameplayStatics::GetGameMode(C));}
 AReleasePlayerController* Controller(const UObject* C){return Cast<AReleasePlayerController>(UGameplayStatics::GetPlayerController(C,0));}
 bool ReviewSession(){for(const TCHAR* Flag:{TEXT("ReleaseReview"),TEXT("MoonfallReview"),TEXT("SpellReview"),TEXT("BleepReview"),TEXT("CatmuraiSmokeTest"),TEXT("CatmuraiV10Test")})if(FParse::Param(FCommandLine::Get(),Flag))return true;return false;}
 FString Slot(){return ReviewSession()?TEXT("CatmuraiReleaseReviewProfile"):TEXT("CatmuraiReleaseProfile");}
 void Sanitize(UReleaseSaveGame* P){
  P->ShardCurrency=FMath::Clamp(P->ShardCurrency,0,100000000);P->DamageRank=FMath::Clamp(P->DamageRank,0,5);P->HealthRank=FMath::Clamp(P->HealthRank,0,5);P->FortuneRank=FMath::Clamp(P->FortuneRank,0,5);
  P->TotalRuns=FMath::Clamp(P->TotalRuns,0,10000000);P->BestKills=FMath::Clamp(P->BestKills,0,10000000);P->Victories=FMath::Clamp(P->Victories,0,P->TotalRuns);
  P->VolumeMusic=FMath::IsFinite(P->VolumeMusic)?FMath::Clamp(P->VolumeMusic,0.f,1.f):.65f;P->VolumeSFX=FMath::IsFinite(P->VolumeSFX)?FMath::Clamp(P->VolumeSFX,0.f,1.f):.8f;P->Quality=FMath::Clamp(P->Quality,0,3);
 }
 const FLinearColor Ink(.025f,.018f,.042f),Panel(.055f,.035f,.08f,.96f),Cream(.96f,.86f,.66f),Gold(.72f,.48f,.21f),Lilac(.66f,.47f,.88f),Muted(.49f,.44f,.55f),Red(.75f,.08f,.21f);
 const TCHAR* QualityNames[]={TEXT("LOW"),TEXT("MEDIUM"),TEXT("HIGH"),TEXT("EPIC")};
 bool Menu(ERunState S){return S!=ERunState::Playing;}
}
UReleaseSaveGame* FReleaseFrontend::LoadProfile(UObject* Context){
 if(auto* PC=Controller(Context);PC&&PC->Profile)return PC->Profile;
 auto* P=Cast<UReleaseSaveGame>(UGameplayStatics::LoadGameFromSlot(Slot(),0));
 if(!P)P=Cast<UReleaseSaveGame>(UGameplayStatics::CreateSaveGameObject(UReleaseSaveGame::StaticClass()));
 if(P){Sanitize(P);if(auto* PC=Controller(Context))PC->Profile=P;}return P;
}
bool FReleaseFrontend::SaveProfile(UObject* Context,UReleaseSaveGame* P){if(!P)return false;Sanitize(P);const bool OK=UGameplayStatics::SaveGameToSlot(P,Slot(),0);if(auto* PC=Controller(Context))PC->bProfileSaveSucceeded=OK;if(!OK)UE_LOG(LogTemp,Warning,TEXT("CATMURAI_PROFILE_SAVE_FAILED"));return OK;}
void FReleaseFrontend::ApplySettings(UObject* Context,UReleaseSaveGame* P){
 if(!P)return;Sanitize(P);
 if(!ReviewSession())if(auto* S=UGameUserSettings::GetGameUserSettings()){S->SetOverallScalabilityLevel(P->Quality);S->SetFullscreenMode(P->bFullScreen?EWindowMode::WindowedFullscreen:EWindowMode::Windowed);S->ApplySettings(false);}
 FReleaseAudio::SetMix(P->VolumeMusic,P->VolumeSFX);
}
void FReleaseFrontend::ApplyProfile(ASurvivalGameMode* G){
 if(!G)return;auto* P=LoadProfile(G);auto* Cat=G->Player();if(!P||!Cat)return;
 Cat->DamageMultiplier*=1+.05f*P->DamageRank;Cat->Health->Maximum+=10*P->HealthRank;Cat->Health->Current=Cat->Health->Maximum;
 if(G->Combat){G->Combat->FortuneRank=FMath::Clamp(G->Combat->FortuneRank+P->FortuneRank,0,5);G->Ranks.Add(EUpgradeKind::Fortune,G->Combat->FortuneRank);}
}
void FReleaseFrontend::RecordRun(ASurvivalGameMode* G){
 if(!G||G->bTerminalRecorded||(G->State!=ERunState::Victory&&G->State!=ERunState::Defeat))return;G->bTerminalRecorded=true;
 if(auto* P=LoadProfile(G)){++P->TotalRuns;P->BestKills=FMath::Max(P->BestKills,G->Kills);const bool Won=G->State==ERunState::Victory;if(Won)++P->Victories;
  P->ShardCurrency+=FMath::Max(1,G->Kills/5)+(Won?75:10);SaveProfile(G,P);}
}
int32 FReleaseFrontend::PurchaseCost(int32 Rank){return 40+FMath::Clamp(Rank,0,5)*40;}
bool FReleaseFrontend::Purchase(UObject* Context,int32 Upgrade){
 auto* P=LoadProfile(Context);if(!P)return false;int32* Rank=Upgrade==0?&P->DamageRank:Upgrade==1?&P->HealthRank:Upgrade==2?&P->FortuneRank:nullptr;
 if(!Rank||*Rank>=5||P->ShardCurrency<PurchaseCost(*Rank))return false;const int32 Cost=PurchaseCost(*Rank);P->ShardCurrency-=Cost;++*Rank;
 if(!SaveProfile(Context,P)){--*Rank;P->ShardCurrency+=Cost;return false;}return true;
}
AReleasePlayerController::AReleasePlayerController(){PrimaryActorTick.bCanEverTick=true;PrimaryActorTick.bTickEvenWhenPaused=true;bShouldPerformFullTickWhenPaused=true;}
void AReleasePlayerController::BeginPlay(){Super::BeginPlay();Profile=FReleaseFrontend::LoadProfile(this);RefreshInputMode();}
void AReleasePlayerController::SetupInputComponent(){
 Super::SetupInputComponent();
 auto Bind=[&](FKey Key,void(AReleasePlayerController::*Method)()){auto& B=InputComponent->BindKey(Key,IE_Pressed,this,Method);B.bExecuteWhenPaused=true;B.bConsumeInput=true;};
 Bind(EKeys::Enter,&AReleasePlayerController::ActivateFocus);Bind(EKeys::Escape,&AReleasePlayerController::Back);Bind(EKeys::Gamepad_Special_Right,&AReleasePlayerController::Back);Bind(EKeys::Gamepad_FaceButton_Bottom,&AReleasePlayerController::ActivateFocus);Bind(EKeys::Gamepad_FaceButton_Right,&AReleasePlayerController::MenuCancel);
 Bind(EKeys::Up,&AReleasePlayerController::MenuUp);Bind(EKeys::Down,&AReleasePlayerController::MenuDown);Bind(EKeys::Left,&AReleasePlayerController::MenuLeft);Bind(EKeys::Right,&AReleasePlayerController::MenuRight);Bind(EKeys::Gamepad_DPad_Up,&AReleasePlayerController::MenuUp);Bind(EKeys::Gamepad_DPad_Down,&AReleasePlayerController::MenuDown);Bind(EKeys::Gamepad_DPad_Left,&AReleasePlayerController::MenuLeft);Bind(EKeys::Gamepad_DPad_Right,&AReleasePlayerController::MenuRight);
 Bind(EKeys::LeftMouseButton,&AReleasePlayerController::Click);Bind(EKeys::E,&AReleasePlayerController::Ultimate);Bind(EKeys::Gamepad_FaceButton_Top,&AReleasePlayerController::Ultimate);Bind(EKeys::Tab,&AReleasePlayerController::ToggleAuto);Bind(EKeys::M,&AReleasePlayerController::OpenOptions);
}
void AReleasePlayerController::PlayerTick(float Delta){Super::PlayerTick(Delta);if(auto* G=Game(this);G&&(!bInputInitialized||LastInputState!=G->State))RefreshInputMode();}
void AReleasePlayerController::RefreshInputMode(){auto* G=Game(this);if(!G)return;LastInputState=G->State;bInputInitialized=true;const bool InMenu=Menu(G->State);bShowMouseCursor=InMenu;ResetIgnoreMoveInput();ResetIgnoreLookInput();SetIgnoreMoveInput(InMenu);SetIgnoreLookInput(InMenu);
 if(InMenu){FInputModeGameAndUI Mode;Mode.SetHideCursorDuringCapture(false);Mode.SetLockMouseToViewportBehavior(EMouseLockMode::DoNotLock);SetInputMode(Mode);}else{SetInputMode(FInputModeGameOnly());}
 // The controller stack runs before pawn bindings. Consume menu-only keys conditionally.
 if(InputComponent)for(auto& Binding:InputComponent->KeyBindings){if(Binding.Chord.Key==EKeys::LeftMouseButton||Binding.Chord.Key==EKeys::Gamepad_FaceButton_Bottom||Binding.Chord.Key==EKeys::Gamepad_FaceButton_Right)Binding.bConsumeInput=InMenu;}
}
void AReleasePlayerController::Navigate(int32 Direction){if(auto* G=Game(this);G&&Menu(G->State))if(auto* H=Cast<AReleaseHUD>(GetHUD()))H->Navigate(Direction);}
void AReleasePlayerController::MenuUp(){Navigate(-1);}void AReleasePlayerController::MenuDown(){Navigate(1);}
void AReleasePlayerController::MenuLeft(){if(auto* H=Cast<AReleaseHUD>(GetHUD()))H->Adjust(-1);}void AReleasePlayerController::MenuRight(){if(auto* H=Cast<AReleaseHUD>(GetHUD()))H->Adjust(1);}
void AReleasePlayerController::ActivateFocus(){if(auto* G=Game(this);G&&Menu(G->State))if(auto* H=Cast<AReleaseHUD>(GetHUD()))H->ActivateFocus();}
void AReleasePlayerController::Click(){auto* G=Game(this);if(!G||!Menu(G->State))return;float X,Y;if(GetMousePosition(X,Y))if(auto* H=Cast<AReleaseHUD>(GetHUD()))H->Click(X,Y);}
void AReleasePlayerController::Back(){auto* G=Game(this);if(!G)return;if(G->State==ERunState::Playing||G->State==ERunState::Paused)G->TogglePause();else if(G->State==ERunState::Settings)G->SetRunState(SettingsReturn);else if(G->State==ERunState::Armory)G->SetRunState(ERunState::Title);else if(G->State==ERunState::Victory||G->State==ERunState::Defeat)G->ReturnToTitle();RefreshInputMode();}
void AReleasePlayerController::Ultimate(){if(auto* G=Game(this);G&&G->State==ERunState::Playing&&G->Combat)G->Combat->CastUltimate();}
void AReleasePlayerController::MenuCancel(){if(auto* G=Game(this);G&&Menu(G->State))Back();}
void AReleasePlayerController::ToggleAuto(){if(auto* G=Game(this);G&&G->State==ERunState::Playing&&G->Player())G->Player()->Abilities->AutoAttack=!G->Player()->Abilities->AutoAttack;}
void AReleasePlayerController::OpenOptions(){if(auto* G=Game(this);G&&(G->State==ERunState::Playing||G->State==ERunState::Paused||G->State==ERunState::Title)){SettingsReturn=G->State==ERunState::Title?ERunState::Title:ERunState::Paused;G->SetRunState(ERunState::Settings);RefreshInputMode();}}
void AReleaseHUD::Navigate(int32 Direction){if(!FocusRects.IsEmpty())Focus=(Focus+Direction+FocusRects.Num())%FocusRects.Num();}
void AReleaseHUD::Adjust(int32 Direction){if(!FocusRects.IsValidIndex(Focus))return;auto R=FocusRects[Focus];if(R.bSlider){auto* P=FReleaseFrontend::LoadProfile(this);const float V=R.Action==TEXT("Music")?P->VolumeMusic:P->VolumeSFX;Execute(R.Action,R.Value,FMath::Clamp(V+Direction*.1f,0.f,1.f));}else if(R.Action==TEXT("Quality"))Execute(R.Action,Direction);else if(R.Action==TEXT("Loadout")){SelectedLoadout=(SelectedLoadout+Direction+3)%3;}}
void AReleaseHUD::ActivateFocus(){if(FocusRects.IsValidIndex(Focus)){auto R=FocusRects[Focus];if(R.bSlider)Adjust(1);else Execute(R.Action,R.Value);}}
void AReleaseHUD::Click(float X,float Y){FVector2D P=(FVector2D(X,Y)-UIOrigin)/UIScale;for(int32 I=FocusRects.Num()-1;I>=0;--I){const auto& R=FocusRects[I];if(P.X>=R.Position.X&&P.Y>=R.Position.Y&&P.X<=R.Position.X+R.Size.X&&P.Y<=R.Position.Y+R.Size.Y){Focus=I;Execute(R.Action,R.Value,R.bSlider?FMath::Clamp((P.X-R.Position.X)/R.Size.X,0.f,1.f):-1);return;}}}
void AReleaseHUD::Execute(FName Action,int32 Value,float Fraction){auto* G=Game(this);auto* PC=Controller(this);if(!G)return;auto* Profile=FReleaseFrontend::LoadProfile(this);
 if(Action==TEXT("Start"))G->StartReleaseRun(SelectedLoadout);
 else if(Action==TEXT("Title"))G->ReturnToTitle();else if(Action==TEXT("Resume"))G->SetRunState(ERunState::Playing);
 else if(Action==TEXT("Armory"))G->SetRunState(ERunState::Armory);else if(Action==TEXT("Settings")){if(PC)PC->OpenOptions();}
 else if(Action==TEXT("Back")){if(PC)PC->Back();}else if(Action==TEXT("Quit"))UKismetSystemLibrary::QuitGame(this,PC,EQuitPreference::Quit,false);
 else if(Action==TEXT("Choose"))G->Choose(Value);else if(Action==TEXT("Loadout"))SelectedLoadout=Value;
 else if(Action==TEXT("Buy"))FReleaseFrontend::Purchase(this,Value);
 else if(Profile){if(Action==TEXT("Music"))Profile->VolumeMusic=Fraction;else if(Action==TEXT("SFX"))Profile->VolumeSFX=Fraction;else if(Action==TEXT("Quality"))Profile->Quality=(Profile->Quality+(Value==0?1:Value)+4)%4;else if(Action==TEXT("Fullscreen"))Profile->bFullScreen=!Profile->bFullScreen;FReleaseFrontend::ApplySettings(this,Profile);FReleaseFrontend::SaveProfile(this,Profile);}
 if(PC)PC->RefreshInputMode();
}




