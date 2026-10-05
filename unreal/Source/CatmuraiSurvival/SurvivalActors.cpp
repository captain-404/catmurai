#include "Survival.h"
#include "SpellPresentation.h"
#include "ReleaseCombat.h"
#include "Components/StaticMeshComponent.h"
#include "Components/SkeletalMeshComponent.h"
#include "Engine/SkeletalMesh.h"
#include "Animation/AnimSequence.h"
#include "Components/CapsuleComponent.h"
#include "Components/InputComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "GameFramework/SpringArmComponent.h"
#include "Camera/CameraComponent.h"
#include "Kismet/GameplayStatics.h"
#include "Engine/StaticMeshActor.h"
#include "Engine/DirectionalLight.h"
#include "Engine/SkyLight.h"
#include "Components/LightComponent.h"
#include "Components/SkyLightComponent.h"
#include "Engine/Canvas.h"
#include "Engine/Engine.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "DrawDebugHelpers.h"
#include "Misc/CommandLine.h"
#include "Misc/Parse.h"
#include "HAL/PlatformMisc.h"

static ASurvivalGameMode* Run(const UObject* Object){return Cast<ASurvivalGameMode>(UGameplayStatics::GetGameMode(Object));}
static UStaticMesh* Shape(const TCHAR* Name){return LoadObject<UStaticMesh>(nullptr,*FString::Printf(TEXT("/Engine/BasicShapes/%s.%s"),Name,Name));}
static void Tint(UStaticMeshComponent* Mesh,FLinearColor Color){
 if(auto* Material=LoadObject<UMaterialInterface>(nullptr,TEXT("/Game/Materials/M_Prototype.M_Prototype"))){Mesh->SetMaterial(0,Material);}
 if(auto* Dynamic=Mesh->CreateAndSetMaterialInstanceDynamic(0))Dynamic->SetVectorParameterValue(TEXT("Color"),Color);
}
bool UHealthComponent::Hurt(float Amount){if(!FMath::IsFinite(Amount)||Amount<=0||Current<=0)return false;Current=FMath::Max(0.f,Current-Amount);return Current<=0;}
void UHealthComponent::Heal(float Amount){if(FMath::IsFinite(Amount)&&Amount>0)Current=FMath::Min(Maximum,Current+Amount);}

ACatmuraiCharacter::ACatmuraiCharacter(){
 PrimaryActorTick.bCanEverTick=true;GetCapsuleComponent()->InitCapsuleSize(34,75);bUseControllerRotationYaw=false;
 GetCharacterMovement()->MaxWalkSpeed=367;GetCharacterMovement()->bOrientRotationToMovement=true;GetCharacterMovement()->RotationRate=FRotator(0,720,0);
 GetCharacterMovement()->GravityScale=3061.f/980.f;GetCharacterMovement()->AirControl=1;GetCharacterMovement()->JumpZVelocity=FMath::Sqrt(2.f*3061.f*168.f);JumpMaxCount=2;
 Health=CreateDefaultSubobject<UHealthComponent>(TEXT("Health"));Abilities=CreateDefaultSubobject<UAbilityComponent>(TEXT("Abilities"));
 Presentation=CreateDefaultSubobject<USceneComponent>(TEXT("ReplaceablePresentation"));Presentation->SetupAttachment(RootComponent);
 // The handoff's drawn variant includes the katana weighted to weapon_r.
 GetMesh()->SetupAttachment(Presentation);
 GetMesh()->SetSkeletalMesh(LoadObject<USkeletalMesh>(nullptr,TEXT("/Game/Characters/CatmuraiV10/SK_Catmurai_v10.SK_Catmurai_v10")));
 GetMesh()->SetRelativeLocation(FVector(0,0,-75));
 // Imported v10 visual forward is +Y; align it with gameplay +X.
 GetMesh()->SetRelativeRotation(FRotator(0,-90,0));
 GetMesh()->SetCollisionEnabled(ECollisionEnabled::NoCollision);
 IdleAnimation=LoadObject<UAnimSequence>(nullptr,TEXT("/Game/Characters/CatmuraiV10/A_Catmurai_Idle.A_Catmurai_Idle"));
 RunAnimation=LoadObject<UAnimSequence>(nullptr,TEXT("/Game/Characters/CatmuraiV10/A_Catmurai_Run.A_Catmurai_Run"));
 SlashAnimation=LoadObject<UAnimSequence>(nullptr,TEXT("/Game/Characters/CatmuraiV10/A_Catmurai_Slash.A_Catmurai_Slash"));
 JumpStartAnimation=LoadObject<UAnimSequence>(nullptr,TEXT("/Game/Characters/CatmuraiV10/A_Catmurai_JumpStart.A_Catmurai_JumpStart"));
 JumpRiseAnimation=LoadObject<UAnimSequence>(nullptr,TEXT("/Game/Characters/CatmuraiV10/A_Catmurai_JumpRise.A_Catmurai_JumpRise"));
 JumpFallAnimation=LoadObject<UAnimSequence>(nullptr,TEXT("/Game/Characters/CatmuraiV10/A_Catmurai_JumpFall.A_Catmurai_JumpFall"));
 JumpLandAnimation=LoadObject<UAnimSequence>(nullptr,TEXT("/Game/Characters/CatmuraiV10/A_Catmurai_JumpLand.A_Catmurai_JumpLand"));
 JumpFlipAnimation=LoadObject<UAnimSequence>(nullptr,TEXT("/Game/Characters/CatmuraiV10/A_Catmurai_JumpFlip.A_Catmurai_JumpFlip"));
 DashAnimation=LoadObject<UAnimSequence>(nullptr,TEXT("/Game/Characters/CatmuraiV10/A_Catmurai_Dash.A_Catmurai_Dash"));
 SwordPivot=CreateDefaultSubobject<USceneComponent>(TEXT("SwordPivot"));SwordPivot->SetupAttachment(GetMesh(),TEXT("weapon_r"));
 CameraBoom=CreateDefaultSubobject<USpringArmComponent>(TEXT("CameraBoom"));CameraBoom->SetupAttachment(RootComponent);CameraBoom->TargetArmLength=1900;CameraBoom->SetRelativeRotation(FRotator(-57,-45,0));CameraBoom->bInheritPitch=false;CameraBoom->bInheritYaw=false;CameraBoom->bInheritRoll=false;CameraBoom->bDoCollisionTest=false;CameraBoom->bEnableCameraLag=true;CameraBoom->CameraLagSpeed=12;
 Camera=CreateDefaultSubobject<UCameraComponent>(TEXT("Camera"));Camera->SetupAttachment(CameraBoom);Camera->FieldOfView=65;
}


void ACatmuraiCharacter::SetupPlayerInputComponent(UInputComponent* Input){
 Super::SetupPlayerInputComponent(Input);Input->BindAxis(TEXT("MoveForward"),this,&ACatmuraiCharacter::Forward);Input->BindAxis(TEXT("MoveRight"),this,&ACatmuraiCharacter::Right);
 Input->BindKey(EKeys::LeftMouseButton,IE_Pressed,this,&ACatmuraiCharacter::Attack);Input->BindKey(EKeys::Q,IE_Pressed,this,&ACatmuraiCharacter::Special);Input->BindKey(EKeys::SpaceBar,IE_Pressed,this,&ACatmuraiCharacter::PressJump);
 Input->BindKey(EKeys::SpaceBar,IE_Released,this,&ACatmuraiCharacter::ReleaseJump);
 Input->BindKey(EKeys::Gamepad_FaceButton_Bottom,IE_Pressed,this,&ACatmuraiCharacter::PressJump);
 Input->BindKey(EKeys::Gamepad_FaceButton_Bottom,IE_Released,this,&ACatmuraiCharacter::ReleaseJump);
 Input->BindKey(EKeys::LeftShift,IE_Pressed,this,&ACatmuraiCharacter::Dash);
 Input->BindKey(EKeys::RightShift,IE_Pressed,this,&ACatmuraiCharacter::Dash);
 Input->BindKey(EKeys::Gamepad_FaceButton_Right,IE_Pressed,this,&ACatmuraiCharacter::Dash);
 Input->BindKey(EKeys::Gamepad_FaceButton_Left,IE_Pressed,this,&ACatmuraiCharacter::Attack);
 Input->BindKey(EKeys::J,IE_Pressed,this,&ACatmuraiCharacter::Attack);
 Input->BindKey(EKeys::One,IE_Pressed,this,&ACatmuraiCharacter::Choice1).bExecuteWhenPaused=true;Input->BindKey(EKeys::Two,IE_Pressed,this,&ACatmuraiCharacter::Choice2).bExecuteWhenPaused=true;Input->BindKey(EKeys::Three,IE_Pressed,this,&ACatmuraiCharacter::Choice3).bExecuteWhenPaused=true;Input->BindKey(EKeys::R,IE_Pressed,this,&ACatmuraiCharacter::Retry).bExecuteWhenPaused=true;
}


void ACatmuraiCharacter::Attack(){const bool Automatic=Abilities->AutoAttack;Abilities->AutoAttack=false;Abilities->Katana();Abilities->AutoAttack=Automatic;}void ACatmuraiCharacter::Special(){Abilities->FlyingSlash();}
void ACatmuraiCharacter::Choice1(){if(auto* G=Run(this))G->Choose(0);}void ACatmuraiCharacter::Choice2(){if(auto* G=Run(this))G->Choose(1);}void ACatmuraiCharacter::Choice3(){if(auto* G=Run(this))G->Choose(2);}
void ACatmuraiCharacter::PauseRun(){if(auto* G=Run(this))G->TogglePause();}
void ACatmuraiCharacter::Retry(){if(auto* G=Run(this);G&&(G->State==ERunState::Defeat||G->State==ERunState::Victory)){UGameplayStatics::SetGamePaused(this,false);UGameplayStatics::OpenLevel(this,FName(*GetWorld()->GetMapName()),false,TEXT("game=/Script/CatmuraiSurvival.SurvivalGameMode"));}}

void ACatmuraiCharacter::ApplyIncomingHit(float Amount){auto* G=Run(this);if(!G||G->State!=ERunState::Playing||!FMath::IsFinite(Amount)||Amount<=0||Health->Current<=0||DashTime>0||ImpactTime>0)return;ImpactTime=.3f;CombatCue(TEXT("Hurt"),GetActorLocation(),false);if(Health->Hurt(Amount))G->SetRunState(ERunState::Defeat);}


UAbilityComponent::UAbilityComponent(){PrimaryComponentTick.bCanEverTick=true;FAbilitySpec Slash;Abilities.Add(Slash);FAbilitySpec Wave;Wave.Kind=EAbilityKind::FlyingSlash;Wave.Cooldown=3.5f;Wave.Damage=18;Wave.Range=1400;Abilities.Add(Wave);}
void UAbilityComponent::TickComponent(float Delta,ELevelTick TickType,FActorComponentTickFunction* Function){Super::TickComponent(Delta,TickType,Function);auto* G=Run(this);if(!G||G->State!=ERunState::Playing)return;SlashRemaining=FMath::Max(0.f,SlashRemaining-Delta);WaveRemaining=FMath::Max(0.f,WaveRemaining-Delta);if(PendingSlash>=0){PendingSlash-=Delta;if(PendingSlash<=0){PendingSlash=-1;ApplySlashHit();}}if(AutoAttack){Katana();FlyingSlash();}}
void UAbilityComponent::Katana(){auto* P=Cast<ACatmuraiCharacter>(GetOwner());auto* G=Run(this);if(!P||!G||G->State!=ERunState::Playing||SlashRemaining>0||P->SlashTime>0||P->DashTime>0)return;const FAbilitySpec* Spec=Abilities.FindByPredicate([](const FAbilitySpec& S){return S.Kind==EAbilityKind::Katana;});if(!Spec)return;
 ASurvivalEnemy* Nearest=nullptr;float Best=Spec->Range*P->AreaMultiplier;for(auto E:G->EnemyPool)if(E->Active){const float D=FVector::Dist2D(E->GetActorLocation(),P->GetActorLocation());if(D<Best){Best=D;Nearest=E;}}if(AutoAttack&&!Nearest)return;if(AutoAttack&&Nearest&&FMath::IsNearlyZero(P->ForwardInput)&&FMath::IsNearlyZero(P->RightInput)&&P->GetVelocity().SizeSquared2D()<1.f)P->SetActorRotation(FRotator(0,(Nearest->GetActorLocation()-P->GetActorLocation()).Rotation().Yaw,0));
 SlashRemaining=FMath::Max(.12f,Spec->Cooldown*P->CooldownMultiplier);P->PlaySlashAnimation();PendingSlash=P->ActiveSlashDuration*.35f;
}
void UAbilityComponent::ApplySlashHit(){
 auto* P=Cast<ACatmuraiCharacter>(GetOwner());auto* G=Run(this);if(!P||!G||G->State!=ERunState::Playing)return;
 const FAbilitySpec* Spec=Abilities.FindByPredicate([](const FAbilitySpec& S){return S.Kind==EAbilityKind::Katana;});if(!Spec)return;
 const FVector Origin=P->GetActorLocation();const FVector Forward=P->GetActorForwardVector();
 for(auto E:G->EnemyPool)if(E->Active){const FVector Offset=E->GetActorLocation()-Origin;if(Offset.Size2D()<=Spec->Range*P->AreaMultiplier&&FVector::DotProduct(Forward,Offset.GetSafeNormal2D())>FMath::Cos(FMath::DegreesToRadians(55.f))){const bool Critical=FMath::FRand()<P->CritChance;const float Damage=Spec->Damage*P->DamageMultiplier*(Critical?P->CritMultiplier:1);const float Before=E->Health->Current;E->Hit(Damage,Offset.GetSafeNormal2D(),P->Knockback,Critical);if(G->Combat&&E->Health->Current<Before)G->Combat->ApplyInfusion(E,Damage);}}
 P->CombatCue(TEXT("Katana"),Origin,false);
}
void UAbilityComponent::FlyingSlash(){auto* P=Cast<ACatmuraiCharacter>(GetOwner());auto* G=Run(this);if(!P||!G||G->State!=ERunState::Playing||WaveRemaining>0)return;const FAbilitySpec* S=Abilities.FindByPredicate([](const FAbilitySpec& A){return A.Kind==EAbilityKind::FlyingSlash;});if(!S)return;
 ASurvivalEnemy* Nearest=nullptr;float Best=S->Range;for(auto E:G->EnemyPool)if(E->Active){float D=FVector::Dist2D(E->GetActorLocation(),P->GetActorLocation());if(D<Best){Best=D;Nearest=E;}}if(!Nearest)return;WaveRemaining=FMath::Max(.2f,S->Cooldown*P->CooldownMultiplier);const FVector Aim=(Nearest->GetActorLocation()-P->GetActorLocation()).GetSafeNormal2D();const int32 Count=FMath::Clamp(S->ProjectileCount+P->ExtraProjectiles,1,8);
 for(int32 i=0;i<Count;i++){auto* B=GetWorld()->SpawnActor<ACombatProjectile>(P->GetActorLocation(),FRotator::ZeroRotator);if(B){B->Direction=Aim.RotateAngleAxis((i-(Count-1)*.5f)*12,FVector::UpVector);B->Damage=S->Damage*P->DamageMultiplier;B->Life=S->Duration;}}P->CombatCue(TEXT("FlyingSlash"),P->GetActorLocation(),false);
}
ASurvivalEnemy::ASurvivalEnemy(){
 PrimaryActorTick.bCanEverTick=true;PrimaryActorTick.bStartWithTickEnabled=false;
 SetRootComponent(CreateDefaultSubobject<USceneComponent>(TEXT("EnemyRoot")));
 Body=CreateDefaultSubobject<UStaticMeshComponent>(TEXT("ReplaceableEnemy"));Body->SetupAttachment(RootComponent);
 BleepMesh=LoadObject<UStaticMesh>(nullptr,TEXT("/Game/Characters/BleepFinal/SM_Bleep_Final.SM_Bleep_Final"));
 Body->SetStaticMesh(BleepMesh?BleepMesh.Get():Shape(TEXT("Sphere")));
 Body->SetCollisionEnabled(ECollisionEnabled::NoCollision);
 Health=CreateDefaultSubobject<UHealthComponent>(TEXT("Health"));
}
void ASurvivalEnemy::Activate(const FEnemySpec& InSpec,FVector Location,bool IsElite){
 Spec=InSpec;Elite=IsElite;Active=true;Push=FVector::ZeroVector;AttackTimer=1;Windup=0;FrostRemaining=BurnRemaining=BurnTickTimer=BurnDamage=0;ShockStacks=0;
 SetActorTickInterval(0);Body->SetForcedLodModel(0);AnimationClock=0;PosePhase=FMath::FRand()*2*PI;HitReaction=0;StrikeVisual=0;DeathRemaining=0;bPursuing=false;AnimationPose=EEnemyPose::Idle;SetActorTickEnabled(true);Body->SetRelativeScale3D(FVector::OneVector);
 Health->Maximum=Health->Current=Spec.Health*(Elite?12:1);
 SetActorLocation(Location);SetActorHiddenInGame(false);
 const float VisualScale=Spec.Scale*(Elite?2.2f:1);SetActorScale3D(FVector(VisualScale));
 if(BleepMesh){
  Body->SetStaticMesh(BleepMesh);
  if(auto* GRun=Run(this);GRun&&GRun->RunDirector)for(int Slot=0;Slot<Body->GetNumMaterials();Slot++){const FString Path=FString::Printf(TEXT("/Game/Release/Environment/M_Enemy_LOD%d.M_Enemy_LOD%d"),Slot,Slot);if(auto* Base=LoadObject<UMaterialInterface>(nullptr,*Path)){auto* Dynamic=Cast<UMaterialInstanceDynamic>(Body->GetMaterial(Slot));if(!Dynamic||Dynamic->Parent!=Base){Dynamic=UMaterialInstanceDynamic::Create(Base,this);Body->SetMaterial(Slot,Dynamic);}Dynamic->SetVectorParameterValue(TEXT("RoleColor"),Spec.Color);}}
  // Keep the gameplay anchor unchanged while grounding the bottom-origin static visual.
  Body->SetRelativeLocation(FVector(0,0,-55.f/FMath::Max(VisualScale,.01f)));
  Body->SetRelativeRotation(FRotator(0,-90,0));
 }else{
  Body->SetRelativeTransform(FTransform::Identity);
  Body->SetStaticMesh(Shape(Spec.Name==TEXT("Angry Sushi")?TEXT("Cube"):Spec.Name==TEXT("Barrel Crab")?TEXT("Cylinder"):TEXT("Sphere")));
  Tint(Body,Elite?FLinearColor(.45,.08,.8):Spec.Color);
 }
}
void ASurvivalEnemy::Simulate(float Delta,ACatmuraiCharacter* P){
 auto* G=Run(this);if(!G||G->State!=ERunState::Playing||!Active||!P||Delta<=0)return;bPursuing=false;
 const bool Frozen=FrostRemaining>(G->Combat&&G->Combat->FrostRank>=3?3.4f:2.65f);
 const float Slow=FrostRemaining>0?(Elite?.7f:.5f):1.f;
 // Frost slows attacks as well as locomotion; elite freeze is a brief stagger only.
 if(Frozen&&!Elite)return;
 AttackTimer=FMath::Max(0.f,AttackTimer-Delta*Slow);
 FVector Location=GetActorLocation()+Push*Delta;
 if(G->RunDirector&&Windup<=0){FVector Separation=FVector::ZeroVector;int Neighbors=0;for(auto Other:G->EnemyPool){if(!Other||Other==this||!Other->Active)continue;FVector Away=Location-Other->GetActorLocation();Away.Z=0;const float DistanceApart=Away.Size();const float Spacing=FMath::Clamp(40.f*(Spec.Scale+Other->Spec.Scale),50.f,160.f);if(DistanceApart<Spacing){if(DistanceApart<.1f)Away=FVector::ForwardVector.RotateAngleAxis(float(GetUniqueID()%360),FVector::UpVector);Separation+=Away.GetSafeNormal()*(1-DistanceApart/Spacing);if(++Neighbors>=8)break;}}Location+=Separation.GetClampedToMaxSize(1.5f)*120*Delta;}
 Location.X=FMath::Clamp(Location.X,-2250.f,2250.f);Location.Y=FMath::Clamp(Location.Y,-2250.f,2250.f);
 SetActorLocation(Location);Push*=FMath::Exp(-Delta*8);
 const FVector Offset=P->GetActorLocation()-Location;const float Distance=Offset.Size2D();
 const bool Ranged=Spec.Archetype==EEnemyArchetype::Shaman;
 const float Reach=125*Spec.Scale*(Elite?1.25f:1.f);
 if(Windup>0){
  Windup=FMath::Max(0.f,Windup-Delta*Slow);
  if(Windup<=0){StrikeVisual=.18f;
   if(!Ranged&&Distance<Reach&&FMath::Abs(Offset.Z)<160)P->ApplyIncomingHit(Spec.Damage*(Elite?2:1));
   AttackTimer=Elite?1.4f:Ranged?2.6f:Spec.Archetype==EEnemyArchetype::Runner?.9f:1.1f;
  }return;
 }
 // Bolts lock their aim when the warning starts, allowing a deliberate dodge.
 if(Ranged&&Distance<1050&&AttackTimer<=0){Windup=Elite?.95f:.85f;SetActorRotation(Offset.Rotation());if(G->Combat)G->Combat->FireEnemyBolt(this,Offset,Windup);return;}
 if(!Ranged&&Distance<Reach&&AttackTimer<=0){Windup=Elite?.85f:Spec.Archetype==EEnemyArchetype::Brute?.75f:Spec.Scale>1.2f?.65f:Spec.Archetype==EEnemyArchetype::Runner?.35f:.45f;
  // Boss fan marks the attack direction while its slower melee strike winds up.
  if(Elite&&G->Combat)G->Combat->FireEnemyBolt(this,Offset,Windup);return;
 }
 const float Desired=Ranged?620.f:85*Spec.Scale;
 if(Push.Size2D()<60&&((Ranged&&Distance<380)||Distance>Desired)){bPursuing=true;
  FVector Direction=Offset.GetSafeNormal2D();if(Ranged&&Distance<380)Direction*=-1;
  const float BehaviorSpeed=Spec.Archetype==EEnemyArchetype::Runner?1.15f:Spec.Archetype==EEnemyArchetype::Brute?.85f:1.f;
  const float Step=FMath::Min(Spec.Speed*BehaviorSpeed*Slow*Delta,Ranged&&Distance<380?380-Distance:FMath::Max(0.f,Distance-Desired));
  Location+=Direction*Step;Location.X=FMath::Clamp(Location.X,-2250.f,2250.f);Location.Y=FMath::Clamp(Location.Y,-2250.f,2250.f);
  SetActorLocation(Location);SetActorRotation(Offset.Rotation());
 }
}
void ASurvivalEnemy::Hit(float Damage,FVector Direction,float Force,bool Critical){if(!Active||!FMath::IsFinite(Damage)||Damage<=0)return;auto* G=Run(this);if(!G||G->State!=ERunState::Playing)return;HitReaction=.18f;StrikeVisual=0;Push=Direction.GetSafeNormal2D()*Force;if(Force>0){Windup=0;AttackTimer=FMath::Max(AttackTimer,.4f);}FCombatNumber Number;Number.Position=GetActorLocation()+FVector(0,0,70);Number.Value=Damage;Number.Critical=Critical;if(G->Numbers.Num()<100)G->Numbers.Add(Number);if(auto* P=G->Player())P->CombatCue(Critical?TEXT("Critical"):TEXT("Impact"),GetActorLocation(),Critical);if(Health->Hurt(Damage)){Active=false;DeathRemaining=.32f;AnimationPose=EEnemyPose::Death;bPursuing=false;G->EnemyDefeated(this);}}
AXPOrb::AXPOrb(){PrimaryActorTick.bCanEverTick=false;auto* Mesh=CreateDefaultSubobject<UStaticMeshComponent>(TEXT("XP"));SetRootComponent(Mesh);Mesh->SetStaticMesh(Shape(TEXT("Sphere")));Mesh->SetWorldScale3D(FVector(.18f));Mesh->SetCollisionEnabled(ECollisionEnabled::NoCollision);}
void AXPOrb::Activate(FVector Location,int32 InValue){Value=InValue;Active=true;Location.Z=35;SetActorLocation(Location);SetActorHiddenInGame(false);Tint(Cast<UStaticMeshComponent>(RootComponent),FLinearColor(.025,1,.4));}
void AXPOrb::Simulate(float Delta,ACatmuraiCharacter* P){if(!Active||!P)return;const float D=FVector::Dist2D(P->GetActorLocation(),GetActorLocation());if(D<260){FVector Goal=P->GetActorLocation();Goal.Z=35;SetActorLocation(FMath::VInterpTo(GetActorLocation(),Goal,Delta,8));}if(D<70){Active=false;SetActorHiddenInGame(true);P->CombatCue(TEXT("Pickup"),P->GetActorLocation(),false);if(auto* G=Run(this))G->AddXP(Value);}}
ACombatProjectile::ACombatProjectile(){PrimaryActorTick.bCanEverTick=true;auto* Root=CreateDefaultSubobject<USceneComponent>(TEXT("Root"));SetRootComponent(Root);}
static void InitializeProjectileVisual(ACombatProjectile* Projectile);
void ACombatProjectile::Tick(float Delta){Super::Tick(Delta);auto* G=Run(this);if(!G||G->State!=ERunState::Playing)return;if(!VisualAttempted){VisualAttempted=true;InitializeProjectileVisual(this);}const FVector Old=GetActorLocation();const FVector Next=Old+Direction*1000*Delta;SetActorLocation(Next);for(auto E:G->EnemyPool)if(E->Active&&!Struck.Contains(E)&&FMath::PointDistToSegment(E->GetActorLocation(),Old,Next)<85*E->Spec.Scale){Struck.Add(E);const float Before=E->Health->Current;E->Hit(Damage,Direction,180,false);if(G->Combat&&E->Health->Current<Before)G->Combat->ApplyInfusion(E,Damage);}Life-=Delta;if(Life<=0)Destroy();}





void ACombatProjectile::BeginPlay(){
 Super::BeginPlay();
}

static void InitializeProjectileVisual(ACombatProjectile* Projectile){
 auto& Visual=Projectile->Visual;
 Visual=ASpellEffect::Spawn(Projectile,TEXT("spell_shadow_claw_crescent"),Projectile->GetActorLocation()+FVector(0,0,-25),160,Projectile->Life,Projectile->Direction.Rotation().Yaw);
 if(Visual)Visual->AttachToActor(Projectile,FAttachmentTransformRules::KeepWorldTransform);
}
void ACombatProjectile::EndPlay(const EEndPlayReason::Type Reason){if(IsValid(Visual))Visual->Destroy();Super::EndPlay(Reason);}
