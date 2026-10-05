#include "Survival.h"
#include "ReleaseCombat.h"
#include "ReleaseRun.h"
#include "ReleaseFrontend.h"
#include "ReleaseAudio.h"
#include "MoonfallArena.h"
#include "SpellPresentation.h"
#include "Engine/Texture2D.h"
#include "Components/StaticMeshComponent.h"
#include "Components/LightComponent.h"
#include "Components/DirectionalLightComponent.h"
#include "Components/SkyLightComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Engine/StaticMeshActor.h"
#include "Engine/DirectionalLight.h"
#include "Engine/SkyLight.h"
#include "Engine/ExponentialHeightFog.h"
#include "Components/ExponentialHeightFogComponent.h"
#include "Engine/PostProcessVolume.h"
#include "EngineUtils.h"
#include "Kismet/GameplayStatics.h"
#include "Engine/Canvas.h"
#include "Engine/Engine.h"
#include "Engine/Font.h"
#include "Misc/Paths.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "Misc/CommandLine.h"
#include "Misc/Parse.h"
#include "HAL/PlatformMisc.h"

ASurvivalGameMode::ASurvivalGameMode(){PrimaryActorTick.bCanEverTick=true;PrimaryActorTick.bTickEvenWhenPaused=FParse::Param(FCommandLine::Get(),TEXT("ReleaseReview"));DefaultPawnClass=ACatmuraiCharacter::StaticClass();HUDClass=AReleaseHUD::StaticClass();PlayerControllerClass=AReleasePlayerController::StaticClass();Combat=CreateDefaultSubobject<UReleaseCombatComponent>(TEXT("RunCombat"));RunDirector=CreateDefaultSubobject<UReleaseRunComponent>(TEXT("RunDirector"));}
ACatmuraiCharacter* ASurvivalGameMode::Player()const{return Cast<ACatmuraiCharacter>(UGameplayStatics::GetPlayerCharacter(this,0));}
int32 ASurvivalGameMode::ActiveCount()const{int32 Count=0;for(auto E:EnemyPool)if(E&&E->Active)++Count;return Count;}
void ASurvivalGameMode::EnsureDefaults(){if(Tuning)return;Tuning=NewObject<USurvivalTuning>(this);Tuning->RunDuration=480;
 FEnemySpec Rat;Tuning->Enemies.Add(Rat);FEnemySpec Sushi;Sushi.Name=TEXT("Lantern Stalker");Sushi.Archetype=EEnemyArchetype::Runner;Sushi.Health=22;Sushi.Speed=290;Sushi.Damage=6;Sushi.Scale=.65f;Sushi.Color=FLinearColor(.95,.55,.2);Sushi.Threat=.8f;Tuning->Enemies.Add(Sushi);FEnemySpec Crab;Crab.Name=TEXT("Basalt Brute");Crab.Archetype=EEnemyArchetype::Brute;Crab.Health=125;Crab.Speed=110;Crab.Damage=16;Crab.Scale=1.5;Crab.XP=5;Crab.Threat=3;Crab.Color=FLinearColor(.25,.55,.7);Tuning->Enemies.Add(Crab);FEnemySpec Shaman;Shaman.Name=TEXT("Hex Caller");Shaman.Archetype=EEnemyArchetype::Shaman;Shaman.Health=65;Shaman.Speed=135;Shaman.Damage=12;Shaman.Scale=1.15;Shaman.Threat=2;Shaman.XP=4;Shaman.Color=FLinearColor(.55f,.12f,.8f);Tuning->Enemies.Add(Shaman);
 auto Add=[this](FName Name,const TCHAR* Description,EUpgradeKind Kind,float Amount,ERarity Rarity){FUpgradeSpec U;U.Name=Name;U.Description=Description;U.Kind=Kind;U.Amount=Amount;U.Rarity=Rarity;Tuning->Upgrades.Add(U);};
 Add(TEXT("Anime Budget Increase"),TEXT("+25% damage"),EUpgradeKind::Damage,.25,ERarity::Rare);
 Add(TEXT("Samurai Footwork"),TEXT("+12% movement speed"),EUpgradeKind::Speed,.12,ERarity::Common);
 Add(TEXT("Keen Claws"),TEXT("+8% critical chance"),EUpgradeKind::Critical,.08,ERarity::Rare);
 Add(TEXT("Sweeping Katana"),TEXT("+20% slash reach"),EUpgradeKind::Area,.2,ERarity::Epic);
 Add(TEXT("Catnip Rush"),TEXT("15% shorter ability cooldowns"),EUpgradeKind::Haste,.15,ERarity::Rare);
 Add(TEXT("Chonky Resolve"),TEXT("+30 maximum health and heal 40"),EUpgradeKind::Vitality,30,ERarity::Common);
 Add(TEXT("Echoing Crescent"),TEXT("+1 flying slash projectile"),EUpgradeKind::Projectiles,1,ERarity::Epic);
 Add(TEXT("Paw Power"),TEXT("+30% knockback"),EUpgradeKind::Knockback,.3,ERarity::Common);
 Add(TEXT("Cinder Claws"),TEXT("Ignite struck enemies. Frost + fire erupts as steam."),EUpgradeKind::Ember,1,ERarity::Rare);
 Add(TEXT("Winter's Fang"),TEXT("Slow and freeze foes. Storm shatters frozen enemies."),EUpgradeKind::Frost,1,ERarity::Rare);
 Add(TEXT("Tempest Thread"),TEXT("Chain lightning between foes. Fire creates explosive arcs."),EUpgradeKind::Storm,1,ERarity::Epic);
 Add(TEXT("Spirit Blades"),TEXT("Orbiting blades cut nearby foes. Rank 3 evolves the orbit."),EUpgradeKind::Orbit,1,ERarity::Epic);
 Add(TEXT("Nine Lives"),TEXT("Restore health with each defeated enemy."),EUpgradeKind::Siphon,1,ERarity::Rare);
 Add(TEXT("Lucky Whiskers"),TEXT("Gain more XP from every foe."),EUpgradeKind::Fortune,1,ERarity::Common);
}
void ASurvivalGameMode::BeginPlay(){Super::BeginPlay();EnsureDefaults();SpellArt=LoadObject<USpellPresentation>(nullptr,TEXT("/Game/Spells/DA_SpellPresentation.DA_SpellPresentation"));BuildArena();if(auto* P=Player()){P->SetActorLocation(FVector(0,0,100));if(Tuning->Abilities.Num())P->Abilities->Abilities=Tuning->Abilities;}FReleaseAudio::StartMusic(this);FReleaseFrontend::ApplySettings(this,FReleaseFrontend::LoadProfile(this));
 const bool Legacy=FParse::Param(FCommandLine::Get(),TEXT("MoonfallReview"))||FParse::Param(FCommandLine::Get(),TEXT("SpellReview"))||FParse::Param(FCommandLine::Get(),TEXT("BleepReview"))||FParse::Param(FCommandLine::Get(),TEXT("CatmuraiV10Test"))||FParse::Param(FCommandLine::Get(),TEXT("CatmuraiSmokeTest"));
 if(Legacy){Combat->SetComponentTickEnabled(false);RunDirector->SetComponentTickEnabled(false);RunDirector=nullptr;}else{RunDirector->ResetRun();SetRunState(ERunState::Title);}
 UE_LOG(LogTemp,Display,TEXT("CATMURAI: Release systems ready"));}
void ASurvivalGameMode::BuildArena(){
 auto* Shrine=GetWorld()->SpawnActor<AMoonfallArena>();Shrine->BuildShrine();
 // Opaque sky sphere supplies a moonlit horizon without layered fog overdraw.
 auto* SkyDome=GetWorld()->SpawnActor<AStaticMeshActor>();auto* SkyMesh=SkyDome->GetStaticMeshComponent();SkyMesh->SetMobility(EComponentMobility::Movable);SkyMesh->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,TEXT("/Engine/BasicShapes/Sphere.Sphere")));SkyMesh->SetMaterial(0,LoadObject<UMaterialInterface>(nullptr,TEXT("/Game/Moonfall/Materials/M_ShrineSky.M_ShrineSky")));SkyMesh->SetCollisionEnabled(ECollisionEnabled::NoCollision);SkyMesh->SetCastShadow(false);SkyDome->SetActorScale3D(FVector(400));
 auto* CloudSea=GetWorld()->SpawnActor<AStaticMeshActor>(FVector(0,0,-2400),FRotator::ZeroRotator);auto* Clouds=CloudSea->GetStaticMeshComponent();Clouds->SetMobility(EComponentMobility::Movable);Clouds->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,TEXT("/Engine/BasicShapes/Plane.Plane")));Clouds->SetMaterial(0,LoadObject<UMaterialInterface>(nullptr,TEXT("/Game/Release/Environment/M_CloudSea.M_CloudSea")));Clouds->SetCollisionEnabled(ECollisionEnabled::NoCollision);Clouds->SetCastShadow(false);CloudSea->SetActorScale3D(FVector(400,400,1));
 auto* Fog=GetWorld()->SpawnActor<AExponentialHeightFog>(FVector(0,0,-650),FRotator::ZeroRotator);Fog->GetComponent()->FogDensity=.025f;Fog->GetComponent()->FogHeightFalloff=.22f;Fog->GetComponent()->FogInscatteringLuminance=FLinearColor(.075f,.027f,.16f);Fog->GetComponent()->StartDistance=1200;
 auto* Grade=GetWorld()->SpawnActor<APostProcessVolume>();Grade->bUnbound=true;Grade->Settings.bOverride_BloomIntensity=true;Grade->Settings.BloomIntensity=.45f;Grade->Settings.bOverride_VignetteIntensity=true;Grade->Settings.VignetteIntensity=.25f;
 auto* Sun=GetWorld()->SpawnActor<ADirectionalLight>(FVector(0,0,800),FRotator(-50,-35,0));Sun->GetLightComponent()->SetMobility(EComponentMobility::Movable);Sun->GetLightComponent()->SetIntensity(1.5f);Cast<UDirectionalLightComponent>(Sun->GetLightComponent())->SetForwardShadingPriority(1);Sun->GetLightComponent()->SetLightColor(FLinearColor(.75,.68,1));
 // Soft opposing light keeps v10's dark clothing and sword hand readable.
 auto* Fill=GetWorld()->SpawnActor<ADirectionalLight>(FVector(0,0,700),FRotator(-35,145,0));Fill->GetLightComponent()->SetMobility(EComponentMobility::Movable);Fill->GetLightComponent()->SetIntensity(.8f);Fill->GetLightComponent()->SetLightColor(FLinearColor(.85f,.9f,1.f));Fill->GetLightComponent()->SetCastShadows(false);
 auto* Sky=GetWorld()->SpawnActor<ASkyLight>();Sky->GetLightComponent()->SetMobility(EComponentMobility::Movable);Sky->GetLightComponent()->SetIntensity(.8);Sky->GetLightComponent()->bRealTimeCapture=false;Sky->GetLightComponent()->RecaptureSky();
}
void ASurvivalGameMode::SpawnEnemy(bool IsElite){auto* P=Player();if(!P||Tuning->Enemies.IsEmpty()||ActiveCount()>=Tuning->PopulationCap)return;
 ASurvivalEnemy* E=nullptr;for(auto Candidate:EnemyPool)if(Candidate->CanReuse()){E=Candidate;break;}if(!E){E=GetWorld()->SpawnActor<ASurvivalEnemy>();EnemyPool.Add(E);}if(!E)return;
 const int32 Available=FMath::Min(Tuning->Enemies.Num(),1+int32(RunTime/20));const int32 Index=IsElite?Tuning->Enemies.Num()-1:FMath::RandRange(0,Available-1);FEnemySpec Spec=Tuning->Enemies[Index];Spec.Health*=1+RunTime/160;
 const float Angle=FMath::FRand()*2*PI;FVector Location=P->GetActorLocation()+FVector(FMath::Cos(Angle),FMath::Sin(Angle),0)*1300;Location.X=FMath::Clamp(Location.X,-2200.f,2200.f);Location.Y=FMath::Clamp(Location.Y,-2200.f,2200.f);Location.Z=55;
 // At corners choose the opposite inner edge, never materialize on the player.
 if(FVector::Dist2D(Location,P->GetActorLocation())<750)Location=FVector(-FMath::Sign(P->GetActorLocation().X)*1800,-FMath::Sign(P->GetActorLocation().Y)*1800,55);
 E->Activate(Spec,Location,IsElite);DirectorBudget=FMath::Max(0.f,DirectorBudget-Spec.Threat);
}
void ASurvivalGameMode::Tick(float Delta){Super::Tick(Delta);if(FParse::Param(FCommandLine::Get(),TEXT("ReleaseReview"))){TickReleaseReview(Delta);return;}if(FParse::Param(FCommandLine::Get(),TEXT("MoonfallReview"))){TickMoonfallReview(Delta);return;}if(FParse::Param(FCommandLine::Get(),TEXT("SpellReview"))){TickSpellReview(Delta);return;}if(FParse::Param(FCommandLine::Get(),TEXT("BleepReview"))){TickBleepReview(Delta);return;}if(FParse::Param(FCommandLine::Get(),TEXT("CatmuraiV10Test"))){TickV10Verification(Delta);return;}if(State!=ERunState::Playing)return;auto* P=Player();if(!P)return;RunTime+=Delta;DirectorTimer-=Delta;DirectorBudget=FMath::Min(12.f,DirectorBudget+Delta*(1.5f+RunTime/35+Level*.15f));
 if(DirectorTimer<=0){DirectorTimer=.4f;if(DirectorBudget>=3&&ActiveCount()<Tuning->PopulationCap)SpawnEnemy();}
 if(!RunDirector&&RunTime>=Tuning->RunDuration&&!EliteSpawned){if(ActiveCount()>=Tuning->PopulationCap){for(auto E:EnemyPool)if(E->Active){E->Active=false;E->SetActorHiddenInGame(true);break;}}SpawnEnemy(true);EliteSpawned=true;P->CombatCue(TEXT("EliteIntro"),P->GetActorLocation(),false);}
 AITimer+=Delta;if(AITimer>=.05f){const float Step=FMath::Min(.1f,AITimer);AITimer=0;for(auto E:EnemyPool){if(E)E->Simulate(Step,P);if(State!=ERunState::Playing)break;}for(auto Orb:OrbPool){Orb->Simulate(Step,P);if(State!=ERunState::Playing)break;}}
 for(auto& N:Numbers){N.Life-=Delta;N.Position.Z+=Delta*60;}Numbers.RemoveAll([](const FCombatNumber& N){return N.Life<=0;});
 // Opt-in runtime smoke exercises actual world actors; never runs in normal gameplay.
 if(FParse::Param(FCommandLine::Get(),TEXT("CatmuraiSmokeTest"))&&RunTime>3&&RunTime<10){
 if(EnemyPool.IsEmpty()){UE_LOG(LogTemp,Error,TEXT("CATMURAI_SMOKE_FAIL: no enemies"));FPlatformMisc::RequestExitWithStatus(false,1);return;}bool OK=true;const FVector Before=P->GetActorLocation();P->Dash();OK&=P->DashTime>0;P->Tick(.01f);P->GetCharacterMovement()->TickComponent(.05f,LEVELTICK_All,nullptr);OK&=FVector::Dist2D(Before,P->GetActorLocation())>40;P->DashTime=0;P->GetCharacterMovement()->SetMovementMode(MOVE_Walking);P->SetActorLocation(Before);
 auto* E=EnemyPool[0].Get();E->Activate(Tuning->Enemies[0],Before+P->GetActorForwardVector()*100,false);P->Abilities->SlashRemaining=0;const float HP=E->Health->Current;P->Abilities->Katana();OK&=E->Health->Current==HP;P->Abilities->TickComponent(.34f,LEVELTICK_All,nullptr);OK&=E->Health->Current<HP;
 P->Abilities->WaveRemaining=0;E->Activate(Tuning->Enemies[0],Before+P->GetActorForwardVector()*500,false);P->Abilities->FlyingSlash();OK&=P->Abilities->WaveRemaining>0;auto* Probe=GetWorld()->SpawnActor<ACombatProjectile>(Before,FRotator::ZeroRotator);Probe->Direction=P->GetActorForwardVector();const float WaveHP=E->Health->Current;Probe->Tick(.5f);OK&=E->Health->Current<WaveHP;Probe->Destroy();
 E->Hit(10000,FVector::ForwardVector,0,false);OK&=Kills>0&&OrbPool.Num()>0;
 auto* Pickup=OrbPool[0].Get();Pickup->Activate(Before,8);Pickup->Simulate(.05f,P);OK&=!Pickup->Active;OK&=State==ERunState::Choosing&&Choices.Num()==3;Choose(0);OK&=State==ERunState::Playing&&Level==2;
 SetRunState(ERunState::Defeat);OK&=State==ERunState::Defeat;SetRunState(ERunState::Playing);E->Activate(Tuning->Enemies[2],Before+FVector(400,0,0),true);E->Hit(100000,FVector::ForwardVector,0,true);OK&=State==ERunState::Victory;
 UE_LOG(LogTemp,Display,TEXT("CATMURAI_SMOKE_%s: spawn, dash, katana, projectile, death, XP, choices, resume, defeat, elite victory"),OK?TEXT("PASS"):TEXT("FAIL"));FPlatformMisc::RequestExitWithStatus(false,OK?0:1);
 }
}
void ASurvivalGameMode::EnemyDefeated(ASurvivalEnemy* Enemy){++Kills;if(Combat&&Combat->IsComponentTickEnabled())Combat->OnEnemyDefeated(Enemy);AXPOrb* Orb=nullptr;for(auto O:OrbPool)if(!O->Active){Orb=O;break;}if(!Orb&&OrbPool.Num()<300){Orb=GetWorld()->SpawnActor<AXPOrb>();OrbPool.Add(Orb);}const int RewardXP=Enemy->Spec.XP+(Combat&&Combat->IsComponentTickEnabled()?Combat->FortuneRank:0);if(Orb)Orb->Activate(Enemy->GetActorLocation(),RewardXP);else AddXP(RewardXP);if(auto* P=Player())P->CombatCue(TEXT("EnemyDeath"),Enemy->GetActorLocation(),false);if(RunDirector)RunDirector->OnEnemyDefeated(Enemy);else if(Enemy->Elite)SetRunState(ERunState::Victory);}
void ASurvivalGameMode::SetRunState(ERunState NewState){if(NewState==ERunState::Victory||NewState==ERunState::Defeat){for(auto E:EnemyPool)if(E&&!E->Active){E->DeathRemaining=0;E->SetActorHiddenInGame(true);E->SetActorTickEnabled(false);}}State=NewState;if((State==ERunState::Victory||State==ERunState::Defeat)&&!bTerminalRecorded&&RunDirector){FReleaseFrontend::RecordRun(this);}if(auto* C=Cast<AReleasePlayerController>(UGameplayStatics::GetPlayerController(this,0)))C->RefreshInputMode();UGameplayStatics::SetGamePaused(this,State!=ERunState::Playing);if(auto* P=Player()){P->GetCharacterMovement()->StopMovementImmediately();if(NewState==ERunState::Victory)P->CombatCue(TEXT("Victory"),P->GetActorLocation(),false);if(NewState==ERunState::Defeat)P->CombatCue(TEXT("Defeat"),P->GetActorLocation(),false);}}
void ASurvivalGameMode::TogglePause(){if(State==ERunState::Playing)SetRunState(ERunState::Paused);else if(State==ERunState::Paused)SetRunState(ERunState::Playing);}
void ASurvivalGameMode::AddXP(int32 Amount){if(Amount<=0||State==ERunState::Victory||State==ERunState::Defeat)return;if(RunDirector)RunDirector->OnPickupXP(Amount);XP+=Amount;if(State==ERunState::Playing&&XP>=5+Level*3){XP-=5+Level*3;++Level;OfferUpgrades();}}
void ASurvivalGameMode::OfferUpgrades(){Choices.Reset();TArray<FUpgradeSpec> Eligible;for(const auto& U:Tuning->Upgrades)if(Ranks.FindRef(U.Kind)<U.MaxRank)Eligible.Add(U);while(Choices.Num()<3&&Eligible.Num()){const int32 Index=FMath::RandRange(0,Eligible.Num()-1);Choices.Add(Eligible[Index]);Eligible.RemoveAt(Index);}if(Choices.IsEmpty()){if(auto* P=Player())P->Health->Heal(20);return;}SetRunState(ERunState::Choosing);if(auto* P=Player())P->CombatCue(TEXT("LevelUp"),P->GetActorLocation(),false);}
void ASurvivalGameMode::Choose(int32 Index){if(State!=ERunState::Choosing||!Choices.IsValidIndex(Index))return;const bool Heal=Choices[Index].Kind==EUpgradeKind::Vitality;ApplyUpgrade(Choices[Index]);Choices.Reset();SetRunState(ERunState::Playing);if(auto* P=Player())P->CombatCue(Heal?TEXT("Heal"):TEXT("Upgrade"),P->GetActorLocation(),false);if(XP>=5+Level*3){XP-=5+Level*3;++Level;OfferUpgrades();}}
void ASurvivalGameMode::ApplyUpgrade(const FUpgradeSpec& U){auto* P=Player();if(!P)return;++Ranks.FindOrAdd(U.Kind);switch(U.Kind){case EUpgradeKind::Damage:P->DamageMultiplier+=U.Amount;break;case EUpgradeKind::Speed:P->BaseMoveSpeed*=1+U.Amount;break;case EUpgradeKind::Critical:P->CritChance=FMath::Min(.8f,P->CritChance+U.Amount);break;case EUpgradeKind::Area:P->AreaMultiplier+=U.Amount;break;case EUpgradeKind::Haste:P->CooldownMultiplier=FMath::Max(.25f,P->CooldownMultiplier*(1-U.Amount));break;case EUpgradeKind::Vitality:P->Health->Maximum+=U.Amount;P->Health->Heal(40);break;case EUpgradeKind::Projectiles:P->ExtraProjectiles=FMath::Min(7,P->ExtraProjectiles+1);break;case EUpgradeKind::Knockback:P->Knockback*=1+U.Amount;break;default:if(Combat)Combat->AddRank(U.Kind);break;}FReleaseAudio::PlayCue(this,TEXT("Upgrade"));}
void ASurvivalHUD::DrawHUD(){Super::DrawHUD();auto* G=Cast<ASurvivalGameMode>(UGameplayStatics::GetGameMode(this));if(!Canvas||!G||!G->Player()||G->bReviewHideHUD)return;auto* P=G->Player();const float W=Canvas->SizeX,H=Canvas->SizeY;
 // Palette sampled by eye from the supplied Path of Shadows reference.
 const FLinearColor Ink=FLinearColor::FromSRGBColor(FColor(15,9,20));
 const FLinearColor Panel=FLinearColor::FromSRGBColor(FColor(31,16,35));
 const FLinearColor Parchment=FLinearColor::FromSRGBColor(FColor(247,216,163));
 const FLinearColor Crimson=FLinearColor::FromSRGBColor(FColor(205,38,78));
 const FLinearColor Violet=FLinearColor::FromSRGBColor(FColor(177,77,237));
 const FLinearColor Lilac=FLinearColor::FromSRGBColor(FColor(221,174,246));
 const FLinearColor Muted=FLinearColor::FromSRGBColor(FColor(195,173,177));
 if(!BrushFont){
  BrushFont=NewObject<UFont>(this);BrushFont->FontCacheType=EFontCacheType::Runtime;
  BrushFont->LegacyFontSize=24;
  BrushFont->GetMutableInternalCompositeFont().DefaultTypeface.Fonts.Emplace(FName("Regular"),FPaths::ProjectContentDir()/TEXT("UI/Fonts/PermanentMarker-Regular.ttf"),EFontHinting::Default,EFontLoadingPolicy::LazyLoad);
 }
 // Design coordinates keep the typography and cards together at smaller resolutions.
 const float Scale=FMath::Min(W/1280.f,H/720.f),OX=(W-1280*Scale)*.5f,OY=(H-720*Scale)*.5f;
 auto Rect=[&](FLinearColor C,float X,float Y,float Width,float Height){DrawRect(C,OX+X*Scale,OY+Y*Scale,Width*Scale,Height*Scale);};
 auto Text=[&](const FString& S,FLinearColor C,float X,float Y,float Size=1.f,bool Brush=false){DrawText(S,C,OX+X*Scale,OY+Y*Scale,Brush?BrushFont.Get():GEngine->GetSmallFont(),Size*Scale);};
 auto Card=[&](float X,float Y,float Width,float Height,FLinearColor Accent){Rect(Accent,X,Y,Width,Height);Rect(Panel,X+1,Y+1,Width-2,Height-2);Rect(Accent,X,Y,4,Height);};
 // Screen-space telegraphs remain visible in packaged builds and follow the arena floor.
 for(auto E:G->EnemyPool)if(E&&E->Active){
  if(E->Windup>0){
   const float Duration=E->Elite?.85f:E->Spec.Scale>1.2f?.65f:.45f;
   const float Progress=1-FMath::Clamp(E->Windup/Duration,0.f,1.f);
   for(int Ring=0;Ring<2;++Ring){const float Radius=125*E->Spec.Scale*(Ring?Progress:1);
    FVector Last=FVector::ZeroVector;for(int i=0;i<=48;++i){const float A=2*PI*i/48;FVector Point=E->GetActorLocation()+FVector(FMath::Cos(A)*Radius,FMath::Sin(A)*Radius,0);Point.Z=5;const FVector Screen=Project(Point);
     if(i&&Last.Z>0&&Screen.Z>0)DrawLine(Last.X,Last.Y,Screen.X,Screen.Y,Ring?Parchment:Crimson,2*Scale);Last=Screen;}
   }
  }
  if(E->Elite||E->Health->Current<E->Health->Maximum){
   const FVector Top=Project(E->Body->Bounds.Origin+FVector(0,0,E->Body->Bounds.BoxExtent.Z+18));
   if(Top.Z>0){const float BW=(E->Elite?110.f:48.f)*Scale;DrawRect(Ink,Top.X-BW/2,Top.Y,BW,6*Scale);DrawRect(E->Elite?Violet:Crimson,Top.X-BW/2,Top.Y,BW*FMath::Clamp(E->Health->Current/E->Health->Maximum,0.f,1.f),6*Scale);}
  }
 }
 Card(20,20,450,182,Crimson);
 Text(TEXT("CATMURAI"),Parchment,36,24,1.15f,true);
 Text(TEXT("MOONFALL / SURVIVAL"),Lilac,36,65,.9f);
 Rect(Crimson,36,88,410,2);
 Rect(Ink,36,98,410,16);Rect(Crimson,36,98,410*FMath::Clamp(P->Health->Current/P->Health->Maximum,0.f,1.f),16);
 Text(FString::Printf(TEXT("HP %.0f / %.0f    Level %d   XP %d / %d"),P->Health->Current,P->Health->Maximum,G->Level,G->XP,5+G->Level*3),Parchment,36,121);
 Rect(Ink,36,143,410,6);Rect(Violet,36,143,410*FMath::Clamp(float(G->XP)/(5+G->Level*3),0.f,1.f),6);
 Text(FString::Printf(TEXT("%02d:%02d  |  Enemies %d  |  Defeated %d"),int(G->RunTime)/60,int(G->RunTime)%60,G->ActiveCount(),G->Kills),Muted,36,162);
 auto Icon=[&](FName Name,float X,float Y,float Size,float Alpha=1.f){
  if(!G->SpellArt||!G->SpellArt->IconMaterial)return;
  auto* T=G->SpellArt->Texture(Name);if(!T)return;
  auto& M=SpellIcons.FindOrAdd(Name);if(!M){M=UMaterialInstanceDynamic::Create(G->SpellArt->IconMaterial,this);M->SetTextureParameterValue(TEXT("SpellTexture"),T);M->SetScalarParameterValue(TEXT("BlackKey"),G->SpellArt->BlackKeys.FindRef(Name));}
  M->SetScalarParameterValue(TEXT("Fade"),Alpha);DrawMaterialSimple(M,OX+X*Scale,OY+Y*Scale,Size*Scale,Size*Scale);
 };
 auto Skill=[&](FName Art,const TCHAR* Name,const TCHAR* Key,float Remaining,float Total,float X){
  const bool Ready=Remaining<=0;Card(X,578,244,76,Ready?Violet:Muted);
  Icon(Art,X+6,584,62,Ready?1.f:.5f);
  Text(Name,Ready?Parchment:Muted,X+78,589,.95f);
  Text(FString::Printf(TEXT("%s  |  %s"),Key,Ready?TEXT("READY"):*FString::Printf(TEXT("%.1fs"),Remaining)),Lilac,X+78,614,.9f);
  Rect(Ink,X+78,638,152,4);Rect(Ready?Violet:Crimson,X+78,638,152*(1-FMath::Clamp(Remaining/FMath::Max(.01f,Total),0.f,1.f)),4);
 };
 float SlashTotal=.95f,WaveTotal=3.5f;for(const auto& S:P->Abilities->Abilities){if(S.Kind==EAbilityKind::Katana)SlashTotal=FMath::Max(.12f,S.Cooldown*P->CooldownMultiplier);else if(S.Kind==EAbilityKind::FlyingSlash)WaveTotal=FMath::Max(.2f,S.Cooldown*P->CooldownMultiplier);}
 Skill(TEXT("spell_phantom_claw_rend"),TEXT("PHANTOM CLAW"),TEXT("LMB / Auto"),P->Abilities->SlashRemaining,SlashTotal,20);
 Skill(TEXT("spell_shadow_claw_crescent"),TEXT("SHADOW CRESCENT"),TEXT("Q / Auto"),P->Abilities->WaveRemaining,WaveTotal,276);
 Skill(TEXT("spell_frost_dash"),TEXT("FROST DASH"),TEXT("Shift"),P->DashCooldown,.6f,532);
 Rect(Ink,20,668,1240,32);
 Text(TEXT("WASD move  |  Space jump / double jump | Shift dash  |  Auto-slash + flying slash  |  LMB slash  |  Q wave  |  Esc pause"),Parchment,32,676,.95f);
 Card(844,20,416,72,Violet);Text(TEXT("THE HUNT"),Parchment,860,27,.7f,true);
 Text(G->EliteSpawned?TEXT("Defeat the Bleep overlord"):TEXT("Survive 3 minutes to summon the elite"),Lilac,860,64,.95f);
 for(const auto& N:G->Numbers){const FVector S=Project(N.Position);if(S.Z>0)DrawText(FString::Printf(TEXT("%.0f%s"),N.Value,N.Critical?TEXT("!"):TEXT("")),N.Critical?Lilac:Parchment,S.X,S.Y,nullptr,(N.Critical?1.5f:1.f)*Scale);}
 if(G->State!=ERunState::Playing){FLinearColor Veil=Ink;Veil.A=.92f;DrawRect(Veil,0,0,W,H);
  if(G->State==ERunState::Choosing){
   Text(TEXT("A NEW TECHNIQUE"),Parchment,320,166,1.1f,true);Text(TEXT("Choose 1, 2 or 3"),Lilac,320,211);
   for(int32 i=0;i<G->Choices.Num();i++){const auto& U=G->Choices[i];const float Y=246+i*86;const FLinearColor Accent=U.Rarity==ERarity::Epic?Violet:Crimson;Card(320,Y,640,74,Accent);Icon(USpellPresentation::UpgradeIcon(uint8(U.Kind)),326,Y+5,64);Text(FString::Printf(TEXT("[%d] %s"),i+1,*U.Name.ToString()),Parchment,402,Y+5,.65f,true);Text(U.Description,Muted,402,Y+44);
    const TCHAR* Rarity=U.Rarity==ERarity::Legendary?TEXT("LEGENDARY"):U.Rarity==ERarity::Epic?TEXT("EPIC"):U.Rarity==ERarity::Rare?TEXT("RARE"):TEXT("COMMON");
    Text(FString::Printf(TEXT("%s  %d/%d"),Rarity,G->Ranks.FindRef(U.Kind),U.MaxRank),Accent,806,Y+49,.75f);}
  }else{
   Card(280,252,720,192,G->State==ERunState::Defeat?Crimson:Violet);
   Text(G->State==ERunState::Victory?TEXT("VICTORY!"):G->State==ERunState::Defeat?TEXT("DEFEAT"):TEXT("PAUSED"),Parchment,310,270,1.4f,true);
   Text(G->State==ERunState::Victory?TEXT("The Bleep overlord has fallen."):G->State==ERunState::Defeat?TEXT("Even samurai cats need practice."):TEXT("The shadows can wait."),Muted,310,338,1.1f);
   Text(G->State==ERunState::Paused?TEXT("Press Escape to resume"):TEXT("Press R to begin a fresh run"),Lilac,310,389,1.1f);
  }
 }
}




void ASurvivalGameMode::StartReleaseRun(int32 Loadout){
 UGameplayStatics::SetGamePaused(this,false);State=ERunState::Playing;bTerminalRecorded=false;RunTime=0;Level=1;XP=0;Kills=0;EliteSpawned=false;Ranks.Empty();Choices.Empty();Numbers.Empty();DirectorBudget=3;DirectorTimer=0;AITimer=0;
 for(TActorIterator<ACombatProjectile> It(GetWorld());It;++It)It->Destroy();
 for(auto E:EnemyPool)if(E){E->Active=false;E->DeathRemaining=0;E->SetActorHiddenInGame(true);E->SetActorTickEnabled(false);}for(auto O:OrbPool)if(O){O->Active=false;O->SetActorHiddenInGame(true);}for(auto Effect:SpellEffects)if(Effect.IsValid())Effect->Destroy();SpellEffects.Empty();
 if(auto* P=Player()){P->Health->Maximum=P->Health->Current=120;P->DamageMultiplier=1;P->AreaMultiplier=1;P->CooldownMultiplier=1;P->BaseMoveSpeed=367;P->CritChance=.08f;P->Knockback=220;P->ExtraProjectiles=0;P->JumpBuffer=P->CoyoteRemaining=P->AirTime=P->ImpactTime=P->SlashVisualTime=0;P->AirJumpsUsed=P->AirDashesUsed=0;P->bJumpHeld=P->bJumpInProgress=P->bAirBlend=false;P->DashTime=0;P->SlashTime=0;P->DashCooldown=0;P->Abilities->PendingSlash=-1;P->Abilities->SlashRemaining=P->Abilities->WaveRemaining=0;P->Abilities->AutoAttack=true;P->ForwardInput=P->RightInput=0;P->SetActorLocation(FVector(0,0,100));P->GetCharacterMovement()->StopMovementImmediately();P->GetCharacterMovement()->SetMovementMode(MOVE_Walking);}
 if(Combat){Combat->SetComponentTickEnabled(true);Combat->ResetCombat();if(Loadout==1){Combat->AddRank(EUpgradeKind::Ember);Ranks.Add(EUpgradeKind::Ember,1);}else if(Loadout==2){Combat->AddRank(EUpgradeKind::Frost);Ranks.Add(EUpgradeKind::Frost,1);}else{Combat->AddRank(EUpgradeKind::Orbit);Ranks.Add(EUpgradeKind::Orbit,1);}}
 if(RunDirector){RunDirector->ResetRun();RunDirector->SetComponentTickEnabled(true);}FReleaseFrontend::ApplyProfile(this);SetRunState(ERunState::Playing);FReleaseAudio::PlayCue(this,TEXT("Start"));
}
void ASurvivalGameMode::ReturnToTitle(){if(Combat)Combat->ResetCombat();if(RunDirector)RunDirector->ResetRun();SetRunState(ERunState::Title);}
