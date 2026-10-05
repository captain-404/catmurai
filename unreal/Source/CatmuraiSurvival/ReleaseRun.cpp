#include "ReleaseRun.h"
#include "Survival.h"
#include "SpellPresentation.h"
#include "Components/StaticMeshComponent.h"
#include "Components/HierarchicalInstancedStaticMeshComponent.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "Engine/StaticMesh.h"
#include "Engine/World.h"
#include "GameFramework/CharacterMovementComponent.h"

AShrineBeacon::AShrineBeacon(){
 RootComponent=CreateDefaultSubobject<USceneComponent>(TEXT("BeaconRoot"));
 PrimaryActorTick.bCanEverTick=false;
 Disc=CreateDefaultSubobject<UStaticMeshComponent>(TEXT("AttunementCircle"));Disc->SetupAttachment(RootComponent);Disc->SetMobility(EComponentMobility::Movable);Disc->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,TEXT("/Engine/BasicShapes/Plane.Plane")));Disc->SetCollisionEnabled(ECollisionEnabled::NoCollision);Disc->SetCastShadow(false);Disc->SetRelativeScale3D(FVector(3.4,3.4,1));
 Crystal=CreateDefaultSubobject<UStaticMeshComponent>(TEXT("ShrineCrystal"));Crystal->SetupAttachment(RootComponent);Crystal->SetMobility(EComponentMobility::Movable);Crystal->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,TEXT("/Engine/BasicShapes/Cone.Cone")));Crystal->SetCollisionEnabled(ECollisionEnabled::NoCollision);Crystal->SetCastShadow(false);
 // Absolute scale prevents the wafer-thin ground marker from flattening the crystal.
 Crystal->SetAbsolute(false,false,true);Crystal->SetRelativeScale3D(FVector(.26,.26,.65));Crystal->SetRelativeLocation(FVector(0,0,65));
 if(auto* Base=LoadObject<UMaterialInterface>(nullptr,TEXT("/Game/Moonfall/Materials/M_MoonGlow.M_MoonGlow"))){Disc->SetMaterial(0,Base);Crystal->SetMaterial(0,Base);}
}
void AShrineBeacon::BeginPlay(){Super::BeginPlay();if(auto* Base=Crystal->GetMaterial(0)){Glow=UMaterialInstanceDynamic::Create(Base,this);Crystal->SetMaterial(0,Glow);}if(auto* Art=LoadObject<USpellPresentation>(nullptr,TEXT("/Game/Spells/DA_SpellPresentation.DA_SpellPresentation"));Art&&Art->EffectMaterial){auto* Marker=UMaterialInstanceDynamic::Create(Art->EffectMaterial,this);Disc->SetMaterial(0,Marker);}SetAttuned(false);}
void AShrineBeacon::SetAttuned(bool Value){if(Glow)Glow->SetVectorParameterValue(TEXT("Color"),Value?FLinearColor(.07f,.8f,.35f):FLinearColor(.08f,.18f,.42f));if(auto* Marker=Cast<UMaterialInstanceDynamic>(Disc->GetMaterial(0)))if(auto* Art=LoadObject<USpellPresentation>(nullptr,TEXT("/Game/Spells/DA_SpellPresentation.DA_SpellPresentation"))){const FName Key=Value?TEXT("spell_verdant_lotus_paw"):TEXT("spell_guardian_eye_ward");Marker->SetTextureParameterValue(TEXT("SpellTexture"),Art->Texture(Key));Marker->SetScalarParameterValue(TEXT("BlackKey"),Art->BlackKeys.FindRef(Key));Marker->SetScalarParameterValue(TEXT("Fade"),Value?.4f:.25f);}}
ARunTelegraph::ARunTelegraph(){
 PrimaryActorTick.bCanEverTick=false;auto* Ring=CreateDefaultSubobject<UHierarchicalInstancedStaticMeshComponent>(TEXT("DangerRing"));SetRootComponent(Ring);Ring->SetMobility(EComponentMobility::Movable);Ring->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,TEXT("/Engine/BasicShapes/Cube.Cube")));Ring->SetCollisionEnabled(ECollisionEnabled::NoCollision);Ring->SetCastShadow(false);
 if(auto* Base=LoadObject<UMaterialInterface>(nullptr,TEXT("/Game/Moonfall/Materials/M_LanternGlow.M_LanternGlow")))Ring->SetMaterial(0,Base);
 for(int i=0;i<40;++i){float A=2*PI*i/40;Ring->AddInstance(FTransform(FRotator(0,FMath::RadiansToDegrees(A),0),FVector(FMath::Cos(A)*100,FMath::Sin(A)*100,0),FVector(.035,.14,.02)));}
}
UReleaseRunComponent::UReleaseRunComponent(){PrimaryComponentTick.bCanEverTick=true;}
ASurvivalGameMode* UReleaseRunComponent::Mode()const{return Cast<ASurvivalGameMode>(GetOwner());}
void UReleaseRunComponent::ResetRun(){
 for(auto& H:Hazards)if(H.Visual.IsValid())H.Visual->Destroy();Hazards.Reset();for(auto B:Beacons)if(IsValid(B))B->Destroy();Beacons.Reset();
 for(auto Boss:{CurrentBoss,FinalBoss})if(Boss.IsValid()&&Boss->Active){Boss->Active=false;Boss->SetActorHiddenInGame(true);}
 CurrentBoss.Reset();FinalBoss.Reset();ObjectiveKind=0;BeaconIndex=0;Chapter=0;ObjectivesCompleted=0;BossPhase=0;ObjectiveProgress=0;ObjectiveGoal=40;HazardsDodged=0;HazardHits=0;bFinalBossActive=false;bRewarding=false;BossAttackTimer=3.5f;PhaseTime=0;
 const FVector Sites[]={FVector(-1200,-850,3),FVector(1250,-550,3),FVector(0,1300,3)};
 for(auto Site:Sites)Beacons.Add(GetWorld()->SpawnActor<AShrineBeacon>(Site,FRotator::ZeroRotator));
 PhaseTitle=TEXT("I / AWAKEN THE SHRINE");NextObjective();
 UE_LOG(LogTemp,Display,TEXT("RELEASE_RUN_RESET: 3 beacons, 150/300/finale bosses, objective rotation"));
}
void UReleaseRunComponent::NextObjective(){
 for(auto B:Beacons)if(B)if(auto* Marker=Cast<UMaterialInstanceDynamic>(B->Disc->GetMaterial(0)))Marker->SetScalarParameterValue(TEXT("Fade"),.2f);ObjectiveProgress=0;TargetLocation=FVector::ZeroVector;
 ObjectiveKind=ObjectivesCompleted%3;
 if(ObjectiveKind==0){ObjectiveGoal=40+Chapter*10;ObjectiveTitle=TEXT("Clear the courtyard");ObjectiveDescription=FString::Printf(TEXT("Defeat %.0f enemies to restore the shrine."),ObjectiveGoal);}
 else if(ObjectiveKind==1){ObjectiveGoal=6;BeaconIndex=(ObjectivesCompleted/3)%3;if(Beacons.IsValidIndex(BeaconIndex)&&IsValid(Beacons[BeaconIndex]))Beacons[BeaconIndex]->SetAttuned(false);if(Beacons.IsValidIndex(BeaconIndex)&&Beacons[BeaconIndex])if(auto* Marker=Cast<UMaterialInstanceDynamic>(Beacons[BeaconIndex]->Disc->GetMaterial(0)))Marker->SetScalarParameterValue(TEXT("Fade"),.75f);TargetLocation=Beacons.IsValidIndex(BeaconIndex)&&IsValid(Beacons[BeaconIndex])?Beacons[BeaconIndex]->GetActorLocation():FVector(-1200,-850,3);ObjectiveTitle=TEXT("Attune a moon shrine");ObjectiveDescription=TEXT("Stand in the marked blue circle for 6 seconds. Progress holds while you evade.");}
 else{ObjectiveGoal=20+Chapter*10;ObjectiveTitle=TEXT("Gather scattered spirits");ObjectiveDescription=FString::Printf(TEXT("Collect %.0f XP from fallen enemies."),ObjectiveGoal);}
}
void UReleaseRunComponent::RewardComplete(){
 auto* G=Mode();if(!G||bRewarding||G->State!=ERunState::Playing)return;bRewarding=true;
 if(ObjectiveKind==1&&Beacons.IsValidIndex(BeaconIndex)&&IsValid(Beacons[BeaconIndex]))Beacons[BeaconIndex]->SetAttuned(true);
 ++ObjectivesCompleted;NextObjective();if(auto* P=G->Player()){P->Health->Heal(28);P->CombatCue(TEXT("Heal"),P->GetActorLocation(),false);}
 // Reward XP cannot count toward the next pickup objective and grants only one upgrade choice at a time.
 G->AddXP(5+Chapter*3);bRewarding=false;
 UE_LOG(LogTemp,Display,TEXT("RELEASE_OBJECTIVE_COMPLETE: completed=%d heal=28"),ObjectivesCompleted);
}
void UReleaseRunComponent::OnPickupXP(int32 Amount){auto* G=Mode();if(!G||bRewarding||Amount<=0||ObjectiveKind!=2||G->State!=ERunState::Playing)return;ObjectiveProgress=FMath::Min(ObjectiveGoal,ObjectiveProgress+Amount);if(ObjectiveProgress>=ObjectiveGoal)RewardComplete();}
bool UReleaseRunComponent::IsFinalBoss(const ASurvivalEnemy* Enemy)const{return Enemy&&FinalBoss.Get()==Enemy&&bFinalBossActive;}
void UReleaseRunComponent::OnEnemyDefeated(ASurvivalEnemy* Enemy){
 auto* G=Mode();if(!G||!Enemy)return;
 if(IsFinalBoss(Enemy)){bFinalBossActive=false;PhaseTitle=TEXT("THE MOON IS RESTORED");for(auto& H:Hazards)if(H.Visual.IsValid())H.Visual->Destroy();Hazards.Reset();G->SetRunState(ERunState::Victory);UE_LOG(LogTemp,Display,TEXT("RELEASE_FINAL_BOSS_DEFEATED"));return;}
 if(CurrentBoss.Get()==Enemy){CurrentBoss.Reset();for(int32 i=Hazards.Num()-1;i>=0;--i)if(Hazards[i].Source.Get()==Enemy){if(Hazards[i].Visual.IsValid())Hazards[i].Visual->Destroy();Hazards.RemoveAtSwap(i);}if(auto* P=G->Player())P->Health->Heal(40);}
 if(ObjectiveKind==0&&G->State==ERunState::Playing){ObjectiveProgress=FMath::Min(ObjectiveGoal,ObjectiveProgress+1);if(ObjectiveProgress>=ObjectiveGoal)RewardComplete();}
}
bool UReleaseRunComponent::SpawnBoss(int32 Phase){
 auto* G=Mode();auto* P=G?G->Player():nullptr;if(!G||!P||!G->Tuning||G->Tuning->Enemies.IsEmpty())return false;
 // Delay the next chapter champion while an earlier one lives; never silently retire an undefeated boss.
 if(CurrentBoss.IsValid()&&CurrentBoss->Active)return false;
 if(G->ActiveCount()>=G->Tuning->PopulationCap){ASurvivalEnemy* Retire=nullptr;float Farthest=-1;for(auto E:G->EnemyPool)if(E&&E->Active&&!E->Elite){float Distance=FVector::DistSquared2D(E->GetActorLocation(),P->GetActorLocation());if(Distance>Farthest){Farthest=Distance;Retire=E;}}if(!Retire)return false;Retire->Active=false;Retire->SetActorHiddenInGame(true);} 
 auto* Boss=GetWorld()->SpawnActor<ASurvivalEnemy>();if(!Boss)return false;
 FEnemySpec Spec=G->Tuning->Enemies.Last();Spec.Name=Phase==1?TEXT("Frost Warden"):Phase==2?TEXT("Shade Ronin"):TEXT("Moon Eater");Spec.Archetype=Phase==2?EEnemyArchetype::Shaman:EEnemyArchetype::Brute;Spec.Scale=Phase==1?1.1f:Phase==2?1.35f:1.55f;Spec.Health=Phase==1?1600:Phase==2?2600:4200;Spec.Color=Phase==1?FLinearColor(.2f,.75f,1.f):Phase==2?FLinearColor(.95f,.1f,.4f):FLinearColor(1.f,.35f,.06f);Spec.Damage=Phase==3?22:16;Spec.Speed=Phase==3?110:130;Spec.XP=Phase==3?50:20;
 // Choose the farthest valid approach position; clamping a fixed offset at a corner could spawn on the player.
 FVector Spawn;float BestDistance=-1;
 for(int i=0;i<8;++i){float A=2*PI*i/8;FVector Candidate=P->GetActorLocation()+FVector(FMath::Cos(A)*1100,FMath::Sin(A)*1100,0);Candidate.X=FMath::Clamp(Candidate.X,-1900.f,1900.f);Candidate.Y=FMath::Clamp(Candidate.Y,-1900.f,1900.f);Candidate.Z=55;float Distance=FVector::DistSquared2D(P->GetActorLocation(),Candidate);if(Distance>BestDistance){BestDistance=Distance;Spawn=Candidate;}}
 Boss->Activate(Spec,Spawn,true);Boss->Health->Maximum=Boss->Health->Current=Spec.Health;G->EnemyPool.Add(Boss);CurrentBoss=Boss;BossPhase=Phase;BossAttackTimer=2.5f;
 if(Phase==3){FinalBoss=Boss;bFinalBossActive=true;G->EliteSpawned=true;PhaseTitle=TEXT("FINALE / THE MOON EATER");}
 P->CombatCue(TEXT("EliteIntro"),Spawn,false);UE_LOG(LogTemp,Display,TEXT("RELEASE_BOSS_SPAWN: phase=%d health=%.0f"),Phase,Boss->Health->Maximum);return true;
}
void UReleaseRunComponent::QueueHazard(FVector Location,float Radius,float Damage){
 if(Hazards.Num()>=12)return;FRunHazard H;Location.X=FMath::Clamp(Location.X,-2150.f,2150.f);Location.Y=FMath::Clamp(Location.Y,-2150.f,2150.f);Location.Z=7;H.Location=Location;H.Radius=Radius;H.Damage=Damage;H.Source=CurrentBoss;
 auto* Ring=GetWorld()->SpawnActor<ARunTelegraph>(Location,FRotator::ZeroRotator);if(Ring){Ring->SetActorScale3D(FVector(Radius/100,Radius/100,1));H.Visual=Ring;Hazards.Add(H);}
}
void UReleaseRunComponent::TickComponent(float Delta,ELevelTick TickType,FActorComponentTickFunction* Function){
 Super::TickComponent(Delta,TickType,Function);if(!FMath::IsFinite(Delta)||Delta<=0)return;auto* G=Mode();auto* P=G?G->Player():nullptr;if(!G||!P||G->State!=ERunState::Playing)return;
 int32 NewChapter=G->RunTime>=300?2:G->RunTime>=150?1:0;
 if(NewChapter!=Chapter){Chapter=NewChapter;PhaseTitle=Chapter==1?TEXT("II / SHADOWS AT THE GATES"):TEXT("III / THE MOONFALL SIEGE");}
 float Finale=G->Tuning?G->Tuning->RunDuration:480;
 if(BossPhase==0&&G->RunTime>=150)SpawnBoss(1);else if(BossPhase==1&&G->RunTime>=300)SpawnBoss(2);else if(BossPhase==2&&G->RunTime>=Finale)SpawnBoss(3);
 if(ObjectiveKind==1&&P->GetCharacterMovement()->IsMovingOnGround()&&FVector::Dist2D(P->GetActorLocation(),TargetLocation)<155){ObjectiveProgress=FMath::Min(ObjectiveGoal,ObjectiveProgress+Delta);if(ObjectiveProgress>=ObjectiveGoal){RewardComplete();if(G->State!=ERunState::Playing)return;}}
 for(int32 i=Hazards.Num()-1;i>=0;--i){auto& H=Hazards[i];H.Remaining-=Delta;
  if(!H.Source.IsValid()||!H.Source->Active){if(H.Visual.IsValid())H.Visual->Destroy();Hazards.RemoveAtSwap(i);continue;}
  if(H.Remaining<=0){const bool Hit=FVector::Dist2D(P->GetActorLocation(),H.Location)<=H.Radius&&P->GetActorLocation().Z<190;
   if(Hit){const float Before=P->Health->Current;P->ApplyIncomingHit(H.Damage);if(P->Health->Current<Before)++HazardHits;else ++HazardsDodged;}else ++HazardsDodged;
   ASpellEffect::Spawn(this,BossPhase==1?TEXT("spell_glacial_paw"):TEXT("spell_infernal_moon"),H.Location,H.Radius*2,.4f,0,40,.75f);
   if(H.Visual.IsValid())H.Visual->Destroy();Hazards.RemoveAtSwap(i);if(G->State!=ERunState::Playing)return;
  }
 }
 if(CurrentBoss.IsValid()&&CurrentBoss->Active){BossAttackTimer-=Delta;if(BossAttackTimer<=0){BossAttackTimer=BossPhase==3?3.2f:4.5f;FVector Target=P->GetActorLocation();QueueHazard(Target,BossPhase==3?280:230,BossPhase==3?24:18);if(BossPhase>=2){QueueHazard(Target+FVector(480,0,0),200,16);QueueHazard(Target-FVector(480,0,0),200,16);}}}
}






