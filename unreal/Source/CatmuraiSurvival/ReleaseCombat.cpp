#include "ReleaseCombat.h"
#include "ReleaseAudio.h"
#include "SpellPresentation.h"
#include "Components/SceneComponent.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/StaticMesh.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "Kismet/GameplayStatics.h"
#include "Engine/World.h"

static ASurvivalGameMode* CombatRun(const UObject* Context){return Cast<ASurvivalGameMode>(UGameplayStatics::GetGameMode(Context));}
UReleaseCombatComponent::UReleaseCombatComponent(){PrimaryComponentTick.bCanEverTick=true;}
void UReleaseCombatComponent::AddRank(EUpgradeKind Kind){
 int32* Rank=nullptr;
 switch(Kind){case EUpgradeKind::Ember:Rank=&EmberRank;break;case EUpgradeKind::Frost:Rank=&FrostRank;break;case EUpgradeKind::Storm:Rank=&StormRank;break;case EUpgradeKind::Orbit:Rank=&OrbitRank;break;case EUpgradeKind::Siphon:Rank=&SiphonRank;break;case EUpgradeKind::Fortune:Rank=&FortuneRank;break;default:break;}
 if(Rank)*Rank=FMath::Clamp(*Rank+1,0,5);
}
FString UReleaseCombatComponent::SynergyName() const{
 if(EmberRank&&FrostRank&&StormRank)return TEXT("TEMPEST NOVA");
 if(EmberRank&&FrostRank)return TEXT("STEAM BLOOM");
 if(FrostRank&&StormRank)return TEXT("THUNDER SHATTER");
 if(EmberRank&&StormRank)return TEXT("EXPLOSIVE ARCS");
 if(OrbitRank>=3)return TEXT("SPIRIT BLADE EVOLUTION");
 return TEXT("Discover an elemental pairing");
}
void UReleaseCombatComponent::ResetCombat(){
 for(auto V:Blades)if(V.IsValid())V->Destroy();Blades.Empty();
 for(auto B:Bolts)if(B.IsValid())B->Destroy();Bolts.Empty();
 if(auto* G=CombatRun(this))for(auto E:G->EnemyPool)if(E){E->FrostRemaining=E->BurnRemaining=E->BurnTickTimer=E->BurnDamage=0;E->ShockStacks=0;}
 EmberRank=FrostRank=StormRank=OrbitRank=SiphonRank=FortuneRank=SynergyTriggers=0;
 ProcCooldown=StormCooldown=OrbitAge=OrbitHitTimer=HealCueCooldown=0;UltimateCharge=0;bInProc=false;
}
void UReleaseCombatComponent::EndPlay(const EEndPlayReason::Type Reason){ResetCombat();Super::EndPlay(Reason);}
void UReleaseCombatComponent::OnEnemyDefeated(ASurvivalEnemy* Enemy){
 auto* G=CombatRun(this);if(!G||G->State!=ERunState::Playing)return;UltimateCharge=FMath::Min(100.f,UltimateCharge+(Enemy&&Enemy->Elite?25.f:3.f));if(SiphonRank<=0)return;
 if(auto* P=G->Player()){const float Before=P->Health->Current;P->Health->Heal(float(SiphonRank)*1.5f);
  if(P->Health->Current>Before&&HealCueCooldown<=0){P->CombatCue(TEXT("Heal"),P->GetActorLocation(),false);HealCueCooldown=.5f;}}
}
void UReleaseCombatComponent::Burst(ASurvivalEnemy* Source,float Radius,float Damage,FName Art,int32 HitLimit){
 auto* G=CombatRun(this);if(!G||!Source||G->State!=ERunState::Playing)return;
 const FVector Origin=Source->GetActorLocation();ASpellEffect::Spawn(this,Art,Origin+FVector(0,0,-35),Radius*2,.4f,0,120,.8f);
 int32 Hits=0;for(auto E:G->EnemyPool){if(G->State!=ERunState::Playing)break;if(E&&E->Active&&FVector::DistSquared2D(E->GetActorLocation(),Origin)<=Radius*Radius){E->Hit(FMath::Clamp(Damage,1.f,1000.f),(E->GetActorLocation()-Origin).GetSafeNormal2D(),110,false);if(++Hits>=HitLimit)break;}}
}
void UReleaseCombatComponent::Chain(ASurvivalEnemy* Source,float Damage,int32 Count){
 auto* G=CombatRun(this);if(!G||!Source)return;TSet<ASurvivalEnemy*> Visited;Visited.Add(Source);FVector Previous=Source->GetActorLocation();
 for(int32 Hop=0;Hop<Count&&G->State==ERunState::Playing;++Hop){
  ASurvivalEnemy* Next=nullptr;float Best=FMath::Square(440.f);
  for(auto E:G->EnemyPool)if(E&&E->Active&&!Visited.Contains(E)){const float D=FVector::DistSquared2D(E->GetActorLocation(),Previous);if(D<Best){Best=D;Next=E;}}
  if(!Next)break;Visited.Add(Next);const FVector To=Next->GetActorLocation();
  const FVector Mid=(Previous+To)*.5f+FVector(0,0,-25);ASpellEffect::Spawn(this,TEXT("spell_shadow_shuriken_trio"),Mid,FMath::Clamp(FVector::Dist2D(Previous,To),80.f,300.f),.16f,(To-Previous).Rotation().Yaw,90,.8f);
  Next->ShockStacks=FMath::Min(3,Next->ShockStacks+1);Next->Hit(FMath::Clamp(Damage,1.f,1000.f),FVector::ZeroVector,0,false);Previous=To;Damage*=.8f;
 }
}
void UReleaseCombatComponent::ApplyInfusion(ASurvivalEnemy* E,float HitDamage){
 auto* G=CombatRun(this);if(bInProc||!G||G->State!=ERunState::Playing||!E||!E->Active||!FMath::IsFinite(HitDamage)||HitDamage<=0)return;
 // Secondary damage never feeds this method. Cooldowns cap synergy work across an entire multi-hit volley.
 const bool WasBurning=E->BurnRemaining>0,WasFrozen=E->FrostRemaining>0;
 if(EmberRank){E->BurnRemaining=3.f+.4f*EmberRank;E->BurnDamage=FMath::Max(E->BurnDamage,FMath::Clamp(HitDamage*(.1f+.035f*EmberRank),1.f,120.f));if(E->BurnTickTimer<=0)E->BurnTickTimer=.5f;}
 if(FrostRank){E->FrostRemaining=FMath::Max(E->FrostRemaining,FrostRank>=3?4.f:3.f);}
 if(StormRank){E->ShockStacks=FMath::Min(3,E->ShockStacks+1);}
 TGuardValue<bool> Guard(bInProc,true);
 if(StormRank&&StormCooldown<=0){StormCooldown=.35f;Chain(E,HitDamage*(.3f+.12f*StormRank),FMath::Min(5,1+StormRank));}
 if(ProcCooldown>0||G->State!=ERunState::Playing)return;
 if(EmberRank&&FrostRank&&(WasBurning||WasFrozen)){
  ProcCooldown=.65f;++SynergyTriggers;E->BurnRemaining=0;E->BurnDamage=0;
  Burst(E,210.f+10*FrostRank,HitDamage*.75f,TEXT("spell_sakura_bloom"),12);
 }else if(FrostRank&&StormRank&&WasFrozen&&E->ShockStacks>=2){
  ProcCooldown=.65f;++SynergyTriggers;E->FrostRemaining=0;E->ShockStacks=0;
  Burst(E,240,HitDamage*(.8f+.08f*FrostRank),TEXT("spell_glacial_paw"),12);
 }else if(EmberRank&&StormRank&&WasBurning&&E->ShockStacks>=2){
  ProcCooldown=.65f;++SynergyTriggers;E->ShockStacks=0;
  Burst(E,190,HitDamage*.65f,TEXT("spell_shadow_bomb"),10);
 }
}
void UReleaseCombatComponent::TickComponent(float Delta,ELevelTick TickType,FActorComponentTickFunction* Function){
 Super::TickComponent(Delta,TickType,Function);auto* G=CombatRun(this);auto* P=G?G->Player():nullptr;
 if(!G||!P||G->State!=ERunState::Playing||Delta<=0)return;
 ProcCooldown=FMath::Max(0.f,ProcCooldown-Delta);StormCooldown=FMath::Max(0.f,StormCooldown-Delta);HealCueCooldown=FMath::Max(0.f,HealCueCooldown-Delta);
 Bolts.RemoveAll([](const TWeakObjectPtr<AEnemyBolt>& B){return !B.IsValid();});
 for(auto E:G->EnemyPool){if(G->State!=ERunState::Playing)return;if(!E||!E->Active)continue;
  E->FrostRemaining=FMath::Max(0.f,E->FrostRemaining-Delta);
  if(E->BurnRemaining>0){const float Elapsed=FMath::Min(Delta,E->BurnRemaining);E->BurnRemaining=FMath::Max(0.f,E->BurnRemaining-Delta);E->BurnTickTimer-=Elapsed;
   if(E->BurnTickTimer<=0){E->BurnTickTimer+=.5f;E->Hit(E->BurnDamage,FVector::ZeroVector,0,false);ASpellEffect::Spawn(this,TEXT("spell_ember_potion"),E->GetActorLocation()+FVector(0,0,-20),90,.22f,0,70,.6f);}
   if(E->BurnRemaining<=0)E->BurnDamage=0;
  }
 }
 if(OrbitRank<=0||G->State!=ERunState::Playing)return;
 OrbitAge+=Delta;OrbitHitTimer-=Delta;const bool Contact=OrbitHitTimer<=0;if(Contact)OrbitHitTimer=.3f;
 const int32 Count=OrbitRank>=3?5:3;const float Radius=OrbitRank>=3?225.f:175.f;
 Blades.SetNum(Count);
 for(int32 i=0;i<Count;++i){const float Angle=OrbitAge*(OrbitRank>=3?2.1f:1.6f)+2*PI*i/Count;
  const FVector Location=P->GetActorLocation()+FVector(FMath::Cos(Angle)*Radius,FMath::Sin(Angle)*Radius,-25);
  if(!Blades[i].IsValid()){auto* V=ASpellEffect::Spawn(this,TEXT("spell_spirit_bell"),Location,OrbitRank>=3?105:80,3600,0,220,.8f);Blades[i]=V;if(V)V->Age=500;} // Start the long-lived orbit sprite inside its steady opacity envelope.
  if(auto* V=Blades[i].Get())V->SetActorLocation(Location);
  if(Contact){int32 Hits=0;for(auto E:G->EnemyPool){if(G->State!=ERunState::Playing)return;if(E&&E->Active&&FVector::DistSquared2D(E->GetActorLocation(),Location)<FMath::Square(OrbitRank>=3?85.f:65.f)){
    const float Damage=(6.f+OrbitRank*3.f)*P->DamageMultiplier;E->Hit(Damage,(E->GetActorLocation()-P->GetActorLocation()).GetSafeNormal2D(),40,false);ApplyInfusion(E,Damage);if(++Hits>=3)break;
   }}}
 }
}
void UReleaseCombatComponent::FireEnemyBolt(ASurvivalEnemy* E,FVector Direction,float Telegraph){
 auto* G=CombatRun(this);if(!G||G->State!=ERunState::Playing||!E||!E->Active)return;
 Bolts.RemoveAll([](const TWeakObjectPtr<AEnemyBolt>& B){return !B.IsValid();});
 const int32 Count=E->Elite?5:1;
 for(int32 i=0;i<Count&&Bolts.Num()<48;++i){auto* B=GetWorld()->SpawnActor<AEnemyBolt>(E->GetActorLocation()+FVector(0,0,-20),FRotator::ZeroRotator);if(!B)continue;
  B->Direction=Direction.GetSafeNormal2D().RotateAngleAxis((i-(Count-1)*.5f)*16,FVector::UpVector);B->Damage=E->Spec.Damage*(E->Elite?1.5f:1.f);B->Speed=E->Elite?520:430;B->Charge=FMath::Max(.35f,Telegraph);Bolts.Add(B);
 }
}
AEnemyBolt::AEnemyBolt(){
 PrimaryActorTick.bCanEverTick=true;
 // The persistent core remains readable if optional spell art is unavailable or its budget is full.
 auto* Core=CreateDefaultSubobject<UStaticMeshComponent>(TEXT("BoltCore"));SetRootComponent(Core);
 Core->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,TEXT("/Engine/BasicShapes/Sphere.Sphere")));Core->SetRelativeScale3D(FVector(.22f));
 Core->SetCollisionEnabled(ECollisionEnabled::NoCollision);Core->SetCastShadow(false);
 if(auto* Material=LoadObject<UMaterialInterface>(nullptr,TEXT("/Game/Materials/M_Prototype.M_Prototype"))){Core->SetMaterial(0,Material);if(auto* M=Core->CreateAndSetMaterialInstanceDynamic(0))M->SetVectorParameterValue(TEXT("Color"),FLinearColor(1,.08f,.4f));}
}
void AEnemyBolt::Tick(float Delta){
 Super::Tick(Delta);auto* G=CombatRun(this);auto* P=G?G->Player():nullptr;if(!G||!P||G->State!=ERunState::Playing)return;
 if(!bVisualAttempted){bVisualAttempted=true;Visual=ASpellEffect::Spawn(this,TEXT("spell_infernal_moon"),GetActorLocation(),95,Life,Direction.Rotation().Yaw,90,.85f);if(Visual)Visual->AttachToActor(this,FAttachmentTransformRules::KeepWorldTransform);}
 if(Charge>0){Charge=FMath::Max(0.f,Charge-Delta);Life-=Delta;if(Life<=0)Destroy();return;}
 const FVector Before=GetActorLocation(),After=Before+Direction*Speed*Delta;SetActorLocation(After);Life-=Delta;
 const FVector PlayerFlat(P->GetActorLocation().X,P->GetActorLocation().Y,After.Z);
 if(FMath::Abs(P->GetActorLocation().Z-After.Z)<110&&FMath::PointDistToSegment(PlayerFlat,Before,After)<48){P->ApplyIncomingHit(Damage);Destroy();return;}
 if(Life<=0||FMath::Abs(After.X)>2450||FMath::Abs(After.Y)>2450)Destroy();
}
void AEnemyBolt::EndPlay(const EEndPlayReason::Type Reason){if(IsValid(Visual))Visual->Destroy();Super::EndPlay(Reason);}

void UReleaseCombatComponent::CastUltimate(){
 auto* G=CombatRun(this);auto* P=G?G->Player():nullptr;if(!G||!P||G->State!=ERunState::Playing||UltimateCharge<100)return;
 FReleaseAudio::PlayCue(this,TEXT("Ultimate"));UltimateCharge=0;TGuardValue<bool> Guard(bInProc,true);const FVector Origin=P->GetActorLocation();
 ASpellEffect::Spawn(this,TEXT("spell_infernal_moon"),Origin+FVector(0,0,-40),1300,1.1f,0,90,1.f);
 ASpellEffect::Spawn(this,TEXT("spell_solar_pounce"),Origin+FVector(0,0,-30),850,.8f,0,-160,.9f);
 // A capped burst, with no infusion recursion or damage to the caster.
 int32 Hits=0;for(auto E:G->EnemyPool){if(G->State!=ERunState::Playing)break;if(E&&E->Active&&FVector::DistSquared2D(E->GetActorLocation(),Origin)<=FMath::Square(650.f)){
  E->Hit(250,(E->GetActorLocation()-Origin).GetSafeNormal2D(),550,true);if(++Hits>=80)break;
 }}
}


