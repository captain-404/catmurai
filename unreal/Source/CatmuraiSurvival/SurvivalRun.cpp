#include "Survival.h"
#include "Components/StaticMeshComponent.h"
#include "Components/LightComponent.h"
#include "Components/SkyLightComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Engine/StaticMeshActor.h"
#include "Engine/DirectionalLight.h"
#include "Engine/SkyLight.h"
#include "Kismet/GameplayStatics.h"
#include "Engine/Canvas.h"
#include "Engine/Engine.h"
#include "Engine/Font.h"
#include "Misc/Paths.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "Misc/CommandLine.h"
#include "Misc/Parse.h"
#include "HAL/PlatformMisc.h"

ASurvivalGameMode::ASurvivalGameMode(){PrimaryActorTick.bCanEverTick=true;DefaultPawnClass=ACatmuraiCharacter::StaticClass();HUDClass=ASurvivalHUD::StaticClass();}
ACatmuraiCharacter* ASurvivalGameMode::Player()const{return Cast<ACatmuraiCharacter>(UGameplayStatics::GetPlayerCharacter(this,0));}
int32 ASurvivalGameMode::ActiveCount()const{int32 Count=0;for(auto E:EnemyPool)if(E&&E->Active)++Count;return Count;}
void ASurvivalGameMode::EnsureDefaults(){if(Tuning)return;Tuning=NewObject<USurvivalTuning>(this);
 FEnemySpec Rat;Tuning->Enemies.Add(Rat);FEnemySpec Sushi;Sushi.Name=TEXT("Angry Sushi");Sushi.Health=22;Sushi.Speed=290;Sushi.Damage=6;Sushi.Scale=.65f;Sushi.Color=FLinearColor(.95,.55,.2);Sushi.Threat=.8f;Tuning->Enemies.Add(Sushi);FEnemySpec Crab;Crab.Name=TEXT("Barrel Crab");Crab.Health=125;Crab.Speed=110;Crab.Damage=16;Crab.Scale=1.5;Crab.XP=5;Crab.Threat=3;Crab.Color=FLinearColor(.25,.55,.7);Tuning->Enemies.Add(Crab);
 auto Add=[this](FName Name,const TCHAR* Description,EUpgradeKind Kind,float Amount,ERarity Rarity){FUpgradeSpec U;U.Name=Name;U.Description=Description;U.Kind=Kind;U.Amount=Amount;U.Rarity=Rarity;Tuning->Upgrades.Add(U);};
 Add(TEXT("Anime Budget Increase"),TEXT("+25% damage"),EUpgradeKind::Damage,.25,ERarity::Rare);
 Add(TEXT("Samurai Footwork"),TEXT("+12% movement speed"),EUpgradeKind::Speed,.12,ERarity::Common);
 Add(TEXT("Keen Claws"),TEXT("+8% critical chance"),EUpgradeKind::Critical,.08,ERarity::Rare);
 Add(TEXT("Sweeping Katana"),TEXT("+20% slash reach"),EUpgradeKind::Area,.2,ERarity::Epic);
 Add(TEXT("Catnip Rush"),TEXT("15% shorter ability cooldowns"),EUpgradeKind::Haste,.15,ERarity::Rare);
 Add(TEXT("Chonky Resolve"),TEXT("+30 maximum health and heal 40"),EUpgradeKind::Vitality,30,ERarity::Common);
 Add(TEXT("Echoing Crescent"),TEXT("+1 flying slash projectile"),EUpgradeKind::Projectiles,1,ERarity::Epic);
 Add(TEXT("Paw Power"),TEXT("+30% knockback"),EUpgradeKind::Knockback,.3,ERarity::Common);
}
void ASurvivalGameMode::BeginPlay(){Super::BeginPlay();EnsureDefaults();BuildArena();if(auto* P=Player()){P->SetActorLocation(FVector(0,0,100));if(Tuning->Abilities.Num())P->Abilities->Abilities=Tuning->Abilities;}UE_LOG(LogTemp,Display,TEXT("CATMURAI: Survival vertical slice started"));}
void ASurvivalGameMode::BuildArena(){
 UStaticMesh* Cube=LoadObject<UStaticMesh>(nullptr,TEXT("/Engine/BasicShapes/Cube.Cube"));
 auto Box=[this,Cube](FVector Position,FVector Scale,FLinearColor Color){auto* A=GetWorld()->SpawnActor<AStaticMeshActor>(Position,FRotator::ZeroRotator);A->GetStaticMeshComponent()->SetMobility(EComponentMobility::Movable);A->GetStaticMeshComponent()->SetStaticMesh(Cube);A->SetActorScale3D(Scale);A->GetStaticMeshComponent()->SetCollisionProfileName(TEXT("BlockAll"));if(auto* M=LoadObject<UMaterialInterface>(nullptr,TEXT("/Game/Materials/M_Prototype.M_Prototype")))A->GetStaticMeshComponent()->SetMaterial(0,M);if(auto* M=A->GetStaticMeshComponent()->CreateAndSetMaterialInstanceDynamic(0))M->SetVectorParameterValue(TEXT("Color"),Color);return A;};
 Box(FVector(0,0,-30),FVector(48,48,.6),FLinearColor(.11,.08,.17));
 for(int32 Side=-1;Side<=1;Side+=2){Box(FVector(Side*2380,0,80),FVector(.4,48,2.2),FLinearColor(.27,.12,.32));Box(FVector(0,Side*2380,80),FVector(48,.4,2.2),FLinearColor(.27,.12,.32));}
 for(int32 i=0;i<8;i++){const float A=i*PI/4;const FVector Center(FMath::Cos(A)*2670,FMath::Sin(A)*2670,0);Box(Center+FVector(0,0,170),FVector(.42,.42,3.4),FLinearColor(.34,.035,.055));Box(Center+FVector(0,0,350),FVector(3.5,.7,.3),FLinearColor(.18,.07,.25));}
 auto* Sun=GetWorld()->SpawnActor<ADirectionalLight>(FVector(0,0,800),FRotator(-50,-35,0));Sun->GetLightComponent()->SetIntensity(3);Sun->GetLightComponent()->SetLightColor(FLinearColor(.75,.68,1));
 auto* Sky=GetWorld()->SpawnActor<ASkyLight>();Sky->GetLightComponent()->SetMobility(EComponentMobility::Movable);Sky->GetLightComponent()->SetIntensity(.8);Sky->GetLightComponent()->bRealTimeCapture=true;
}
void ASurvivalGameMode::SpawnEnemy(bool IsElite){auto* P=Player();if(!P||Tuning->Enemies.IsEmpty()||ActiveCount()>=Tuning->PopulationCap)return;
 ASurvivalEnemy* E=nullptr;for(auto Candidate:EnemyPool)if(!Candidate->Active){E=Candidate;break;}if(!E){E=GetWorld()->SpawnActor<ASurvivalEnemy>();EnemyPool.Add(E);}if(!E)return;
 const int32 Available=FMath::Min(Tuning->Enemies.Num(),1+int32(RunTime/20));const int32 Index=IsElite?Tuning->Enemies.Num()-1:FMath::RandRange(0,Available-1);FEnemySpec Spec=Tuning->Enemies[Index];Spec.Health*=1+RunTime/160;
 const float Angle=FMath::FRand()*2*PI;FVector Location=P->GetActorLocation()+FVector(FMath::Cos(Angle),FMath::Sin(Angle),0)*1300;Location.X=FMath::Clamp(Location.X,-2200.f,2200.f);Location.Y=FMath::Clamp(Location.Y,-2200.f,2200.f);Location.Z=55;
 // At corners choose the opposite inner edge, never materialize on the player.
 if(FVector::Dist2D(Location,P->GetActorLocation())<750)Location=FVector(-FMath::Sign(P->GetActorLocation().X)*1800,-FMath::Sign(P->GetActorLocation().Y)*1800,55);
 E->Activate(Spec,Location,IsElite);DirectorBudget=FMath::Max(0.f,DirectorBudget-Spec.Threat);
}
void ASurvivalGameMode::Tick(float Delta){Super::Tick(Delta);if(State!=ERunState::Playing)return;auto* P=Player();if(!P)return;RunTime+=Delta;DirectorTimer-=Delta;DirectorBudget=FMath::Min(12.f,DirectorBudget+Delta*(1.5f+RunTime/35+Level*.15f));
 if(DirectorTimer<=0){DirectorTimer=.4f;if(DirectorBudget>=3&&ActiveCount()<Tuning->PopulationCap)SpawnEnemy();}
 if(RunTime>=Tuning->RunDuration&&!EliteSpawned){if(ActiveCount()>=Tuning->PopulationCap){for(auto E:EnemyPool)if(E->Active){E->Active=false;E->SetActorHiddenInGame(true);break;}}SpawnEnemy(true);EliteSpawned=true;P->CombatCue(TEXT("EliteIntro"),P->GetActorLocation(),false);}
 AITimer+=Delta;if(AITimer>=.05f){const float Step=FMath::Min(.1f,AITimer);AITimer=0;for(auto E:EnemyPool){if(E)E->Simulate(Step,P);if(State!=ERunState::Playing)break;}for(auto Orb:OrbPool){Orb->Simulate(Step,P);if(State!=ERunState::Playing)break;}}
 for(auto& N:Numbers){N.Life-=Delta;N.Position.Z+=Delta*60;}Numbers.RemoveAll([](const FCombatNumber& N){return N.Life<=0;});
 // Opt-in runtime smoke exercises actual world actors; never runs in normal gameplay.
 if(FParse::Param(FCommandLine::Get(),TEXT("CatmuraiSmokeTest"))&&RunTime>3&&RunTime<10){
 if(EnemyPool.IsEmpty()){UE_LOG(LogTemp,Error,TEXT("CATMURAI_SMOKE_FAIL: no enemies"));FPlatformMisc::RequestExitWithStatus(false,1);return;}bool OK=true;const FVector Before=P->GetActorLocation();P->Dash();OK&=P->DashTime>0;P->Tick(.05f);OK&=FVector::Dist2D(Before,P->GetActorLocation())>40;P->SetActorLocation(Before);
 auto* E=EnemyPool[0].Get();E->Activate(Tuning->Enemies[0],Before+P->GetActorForwardVector()*100,false);P->Abilities->SlashRemaining=0;const float HP=E->Health->Current;P->Abilities->Katana();OK&=E->Health->Current<HP;
 P->Abilities->WaveRemaining=0;E->Activate(Tuning->Enemies[0],Before+P->GetActorForwardVector()*500,false);P->Abilities->FlyingSlash();OK&=P->Abilities->WaveRemaining>0;auto* Probe=GetWorld()->SpawnActor<ACombatProjectile>(Before,FRotator::ZeroRotator);Probe->Direction=P->GetActorForwardVector();const float WaveHP=E->Health->Current;Probe->Tick(.5f);OK&=E->Health->Current<WaveHP;Probe->Destroy();
 E->Hit(10000,FVector::ForwardVector,0,false);OK&=Kills>0&&OrbPool.Num()>0;
 auto* Pickup=OrbPool[0].Get();Pickup->Activate(Before,8);Pickup->Simulate(.05f,P);OK&=!Pickup->Active;OK&=State==ERunState::Choosing&&Choices.Num()==3;Choose(0);OK&=State==ERunState::Playing&&Level==2;
 SetRunState(ERunState::Defeat);OK&=State==ERunState::Defeat;SetRunState(ERunState::Playing);E->Activate(Tuning->Enemies[2],Before+FVector(400,0,0),true);E->Hit(100000,FVector::ForwardVector,0,true);OK&=State==ERunState::Victory;
 UE_LOG(LogTemp,Display,TEXT("CATMURAI_SMOKE_%s: spawn, dash, katana, projectile, death, XP, choices, resume, defeat, elite victory"),OK?TEXT("PASS"):TEXT("FAIL"));FPlatformMisc::RequestExitWithStatus(false,OK?0:1);
 }
}
void ASurvivalGameMode::EnemyDefeated(ASurvivalEnemy* Enemy){++Kills;AXPOrb* Orb=nullptr;for(auto O:OrbPool)if(!O->Active){Orb=O;break;}if(!Orb&&OrbPool.Num()<300){Orb=GetWorld()->SpawnActor<AXPOrb>();OrbPool.Add(Orb);}if(Orb)Orb->Activate(Enemy->GetActorLocation(),Enemy->Spec.XP);else AddXP(Enemy->Spec.XP);if(auto* P=Player())P->CombatCue(TEXT("EnemyDeath"),Enemy->GetActorLocation(),false);if(Enemy->Elite)SetRunState(ERunState::Victory);}
void ASurvivalGameMode::SetRunState(ERunState NewState){State=NewState;UGameplayStatics::SetGamePaused(this,State!=ERunState::Playing);if(auto* P=Player()){P->GetCharacterMovement()->StopMovementImmediately();if(NewState==ERunState::Defeat)P->CombatCue(TEXT("Defeat"),P->GetActorLocation(),false);}}
void ASurvivalGameMode::TogglePause(){if(State==ERunState::Playing)SetRunState(ERunState::Paused);else if(State==ERunState::Paused)SetRunState(ERunState::Playing);}
void ASurvivalGameMode::AddXP(int32 Amount){if(Amount<=0||State==ERunState::Victory||State==ERunState::Defeat)return;XP+=Amount;if(State==ERunState::Playing&&XP>=5+Level*3){XP-=5+Level*3;++Level;OfferUpgrades();}}
void ASurvivalGameMode::OfferUpgrades(){Choices.Reset();TArray<FUpgradeSpec> Eligible;for(const auto& U:Tuning->Upgrades)if(Ranks.FindRef(U.Kind)<U.MaxRank)Eligible.Add(U);while(Choices.Num()<3&&Eligible.Num()){const int32 Index=FMath::RandRange(0,Eligible.Num()-1);Choices.Add(Eligible[Index]);Eligible.RemoveAt(Index);}if(Choices.IsEmpty()){if(auto* P=Player())P->Health->Heal(20);return;}SetRunState(ERunState::Choosing);if(auto* P=Player())P->CombatCue(TEXT("LevelUp"),P->GetActorLocation(),false);}
void ASurvivalGameMode::Choose(int32 Index){if(State!=ERunState::Choosing||!Choices.IsValidIndex(Index))return;ApplyUpgrade(Choices[Index]);Choices.Reset();SetRunState(ERunState::Playing);if(XP>=5+Level*3){XP-=5+Level*3;++Level;OfferUpgrades();}}
void ASurvivalGameMode::ApplyUpgrade(const FUpgradeSpec& U){auto* P=Player();if(!P)return;++Ranks.FindOrAdd(U.Kind);switch(U.Kind){case EUpgradeKind::Damage:P->DamageMultiplier+=U.Amount;break;case EUpgradeKind::Speed:P->GetCharacterMovement()->MaxWalkSpeed*=1+U.Amount;break;case EUpgradeKind::Critical:P->CritChance=FMath::Min(.8f,P->CritChance+U.Amount);break;case EUpgradeKind::Area:P->AreaMultiplier+=U.Amount;break;case EUpgradeKind::Haste:P->CooldownMultiplier=FMath::Max(.25f,P->CooldownMultiplier*(1-U.Amount));break;case EUpgradeKind::Vitality:P->Health->Maximum+=U.Amount;P->Health->Heal(40);break;case EUpgradeKind::Projectiles:P->ExtraProjectiles=FMath::Min(7,P->ExtraProjectiles+1);break;case EUpgradeKind::Knockback:P->Knockback*=1+U.Amount;break;}}
void ASurvivalHUD::DrawHUD(){Super::DrawHUD();auto* G=Cast<ASurvivalGameMode>(UGameplayStatics::GetGameMode(this));if(!Canvas||!G||!G->Player())return;auto* P=G->Player();const float W=Canvas->SizeX,H=Canvas->SizeY;
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
 Card(20,20,450,182,Crimson);
 Text(TEXT("CATMURAI"),Parchment,36,24,1.15f,true);
 Text(TEXT("MOONFALL / SURVIVAL"),Lilac,36,65,.9f);
 Rect(Crimson,36,88,410,2);
 Rect(Ink,36,98,410,16);Rect(Crimson,36,98,410*FMath::Clamp(P->Health->Current/P->Health->Maximum,0.f,1.f),16);
 Text(FString::Printf(TEXT("HP %.0f / %.0f    Level %d   XP %d / %d"),P->Health->Current,P->Health->Maximum,G->Level,G->XP,5+G->Level*3),Parchment,36,121);
 Rect(Ink,36,143,410,6);Rect(Violet,36,143,410*FMath::Clamp(float(G->XP)/(5+G->Level*3),0.f,1.f),6);
 Text(FString::Printf(TEXT("%02d:%02d  |  Enemies %d  |  Defeated %d"),int(G->RunTime)/60,int(G->RunTime)%60,G->ActiveCount(),G->Kills),Muted,36,162);
 Card(20,612,450,42,Violet);
 Text(FString::Printf(TEXT("Dash %.1fs   Slash %.1fs   Flying Slash %.1fs"),P->DashCooldown,P->Abilities->SlashRemaining,P->Abilities->WaveRemaining),Lilac,36,624);
 Rect(Ink,20,668,1240,32);
 Text(TEXT("WASD move  |  Space dash  |  Auto-slash + flying slash  |  LMB slash  |  Q wave  |  Esc pause"),Parchment,32,676,.95f);
 Card(844,20,416,72,Violet);Text(TEXT("THE HUNT"),Parchment,860,27,.7f,true);
 Text(G->EliteSpawned?TEXT("Defeat the Barrel Crab Daimyo"):TEXT("Survive 3 minutes to summon the elite"),Lilac,860,64,.95f);
 for(const auto& N:G->Numbers){const FVector S=Project(N.Position);if(S.Z>0)DrawText(FString::Printf(TEXT("%.0f%s"),N.Value,N.Critical?TEXT("!"):TEXT("")),N.Critical?Lilac:Parchment,S.X,S.Y,nullptr,(N.Critical?1.5f:1.f)*Scale);}
 if(G->State!=ERunState::Playing){FLinearColor Veil=Ink;Veil.A=.92f;DrawRect(Veil,0,0,W,H);
  if(G->State==ERunState::Choosing){
   Text(TEXT("A NEW TECHNIQUE"),Parchment,320,166,1.1f,true);Text(TEXT("Choose 1, 2 or 3"),Lilac,320,211);
   for(int32 i=0;i<G->Choices.Num();i++){const auto& U=G->Choices[i];const float Y=246+i*86;const FLinearColor Accent=U.Rarity==ERarity::Epic?Violet:Crimson;Card(320,Y,640,74,Accent);Text(FString::Printf(TEXT("[%d] %s"),i+1,*U.Name.ToString()),Parchment,336,Y+4,.7f,true);Text(U.Description,Muted,336,Y+44);}
  }else{
   Card(280,252,720,192,G->State==ERunState::Defeat?Crimson:Violet);
   Text(G->State==ERunState::Victory?TEXT("VICTORY!"):G->State==ERunState::Defeat?TEXT("DEFEAT"):TEXT("PAUSED"),Parchment,310,270,1.4f,true);
   Text(G->State==ERunState::Victory?TEXT("The barrel has been defeated."):G->State==ERunState::Defeat?TEXT("Even samurai cats need practice."):TEXT("The shadows can wait."),Muted,310,338,1.1f);
   Text(G->State==ERunState::Paused?TEXT("Press Escape to resume"):TEXT("Press R to begin a fresh run"),Lilac,310,389,1.1f);
  }
 }
}



