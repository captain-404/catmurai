#include "Survival.h"
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
 GetCharacterMovement()->MaxWalkSpeed=580;GetCharacterMovement()->bOrientRotationToMovement=true;GetCharacterMovement()->RotationRate=FRotator(0,900,0);
 Health=CreateDefaultSubobject<UHealthComponent>(TEXT("Health"));Abilities=CreateDefaultSubobject<UAbilityComponent>(TEXT("Abilities"));
 Presentation=CreateDefaultSubobject<USceneComponent>(TEXT("ReplaceablePresentation"));Presentation->SetupAttachment(RootComponent);
 // The handoff's drawn variant includes the katana weighted to weapon_r.
 GetMesh()->SetupAttachment(Presentation);
 GetMesh()->SetSkeletalMesh(LoadObject<USkeletalMesh>(nullptr,TEXT("/Game/Characters/CatmuraiV2/SK_Catmurai_Drawn.SK_Catmurai_Drawn")));
 GetMesh()->SetRelativeLocation(FVector(0,0,-75));
 GetMesh()->SetRelativeRotation(FRotator(0,90,0));
 GetMesh()->SetCollisionEnabled(ECollisionEnabled::NoCollision);
 IdleAnimation=LoadObject<UAnimSequence>(nullptr,TEXT("/Game/Characters/CatmuraiV2/A_Catmurai_Idle.A_Catmurai_Idle"));
 RunAnimation=LoadObject<UAnimSequence>(nullptr,TEXT("/Game/Characters/CatmuraiV2/A_Catmurai_Run.A_Catmurai_Run"));
 SlashAnimation=LoadObject<UAnimSequence>(nullptr,TEXT("/Game/Characters/CatmuraiV2/A_Catmurai_Slash.A_Catmurai_Slash"));
 SwordPivot=CreateDefaultSubobject<USceneComponent>(TEXT("SwordPivot"));SwordPivot->SetupAttachment(GetMesh(),TEXT("weapon_r"));
 CameraBoom=CreateDefaultSubobject<USpringArmComponent>(TEXT("CameraBoom"));CameraBoom->SetupAttachment(RootComponent);CameraBoom->TargetArmLength=1250;CameraBoom->SetRelativeRotation(FRotator(-57,-45,0));CameraBoom->bInheritPitch=false;CameraBoom->bInheritYaw=false;CameraBoom->bInheritRoll=false;CameraBoom->bDoCollisionTest=false;CameraBoom->bEnableCameraLag=true;CameraBoom->CameraLagSpeed=12;
 Camera=CreateDefaultSubobject<UCameraComponent>(TEXT("Camera"));Camera->SetupAttachment(CameraBoom);Camera->FieldOfView=65;
}
void ACatmuraiCharacter::BeginPlay(){Super::BeginPlay();if(IdleAnimation){GetMesh()->PlayAnimation(IdleAnimation,true);CurrentAnimation=IdleAnimation;}UE_LOG(LogTemp,Display,TEXT("CATMURAI_V2: mesh=%s idle=%s run=%s slash=%s weaponBone=%d"),*GetNameSafe(GetMesh()->GetSkeletalMeshAsset()),*GetNameSafe(IdleAnimation),*GetNameSafe(RunAnimation),*GetNameSafe(SlashAnimation),GetMesh()->GetBoneIndex(TEXT("weapon_r")));}
void ACatmuraiCharacter::PlaySlashAnimation(){if(!SlashAnimation)return;SlashVisualTime=FMath::Min(.65f,FMath::Max(.12f,Abilities->SlashRemaining));GetMesh()->PlayAnimation(SlashAnimation,false);GetMesh()->SetPlayRate(SlashAnimation->GetPlayLength()/SlashVisualTime);CurrentAnimation=SlashAnimation;}
void ACatmuraiCharacter::SetupPlayerInputComponent(UInputComponent* Input){
 Super::SetupPlayerInputComponent(Input);Input->BindAxis(TEXT("MoveForward"),this,&ACatmuraiCharacter::Forward);Input->BindAxis(TEXT("MoveRight"),this,&ACatmuraiCharacter::Right);
 Input->BindKey(EKeys::LeftMouseButton,IE_Pressed,this,&ACatmuraiCharacter::Attack);Input->BindKey(EKeys::Q,IE_Pressed,this,&ACatmuraiCharacter::Special);Input->BindKey(EKeys::SpaceBar,IE_Pressed,this,&ACatmuraiCharacter::Dash);
 Input->BindKey(EKeys::One,IE_Pressed,this,&ACatmuraiCharacter::Choice1).bExecuteWhenPaused=true;Input->BindKey(EKeys::Two,IE_Pressed,this,&ACatmuraiCharacter::Choice2).bExecuteWhenPaused=true;Input->BindKey(EKeys::Three,IE_Pressed,this,&ACatmuraiCharacter::Choice3).bExecuteWhenPaused=true;Input->BindKey(EKeys::Escape,IE_Pressed,this,&ACatmuraiCharacter::PauseRun).bExecuteWhenPaused=true;Input->BindKey(EKeys::R,IE_Pressed,this,&ACatmuraiCharacter::Retry).bExecuteWhenPaused=true;
}
void ACatmuraiCharacter::Forward(float Value){if(auto* G=Run(this);G&&G->State==ERunState::Playing)AddMovementInput(FRotator(0,-45,0).Vector(),Value);}
void ACatmuraiCharacter::Right(float Value){if(auto* G=Run(this);G&&G->State==ERunState::Playing)AddMovementInput(FRotator(0,45,0).Vector(),Value);}
void ACatmuraiCharacter::Attack(){Abilities->Katana();}void ACatmuraiCharacter::Special(){Abilities->FlyingSlash();}
void ACatmuraiCharacter::Choice1(){if(auto* G=Run(this))G->Choose(0);}void ACatmuraiCharacter::Choice2(){if(auto* G=Run(this))G->Choose(1);}void ACatmuraiCharacter::Choice3(){if(auto* G=Run(this))G->Choose(2);}
void ACatmuraiCharacter::PauseRun(){if(auto* G=Run(this))G->TogglePause();}
void ACatmuraiCharacter::Retry(){if(auto* G=Run(this);G&&(G->State==ERunState::Defeat||G->State==ERunState::Victory)){UGameplayStatics::SetGamePaused(this,false);UGameplayStatics::OpenLevel(this,FName(*GetWorld()->GetMapName()),false,TEXT("game=/Script/CatmuraiSurvival.SurvivalGameMode"));}}
void ACatmuraiCharacter::Dash(){auto* G=Run(this);if(!G||G->State!=ERunState::Playing||DashCooldown>0)return;DashTime=.18f;DashCooldown=2.4f;DashDirection=GetLastMovementInputVector().GetSafeNormal2D();if(DashDirection.IsNearlyZero())DashDirection=GetActorForwardVector();CombatCue(TEXT("Dash"),GetActorLocation(),false);}
void ACatmuraiCharacter::ApplyIncomingHit(float Amount){if(DashTime>0||ImpactTime>0)return;ImpactTime=.3f;CombatCue(TEXT("Hurt"),GetActorLocation(),false);if(Health->Hurt(Amount))if(auto* G=Run(this))G->SetRunState(ERunState::Defeat);}
void ACatmuraiCharacter::Tick(float Delta){Super::Tick(Delta);DashCooldown=FMath::Max(0.f,DashCooldown-Delta);ImpactTime=FMath::Max(0.f,ImpactTime-Delta);if(DashTime>0){DashTime-=Delta;AddActorWorldOffset(DashDirection*1900*Delta,true);DrawDebugLine(GetWorld(),GetActorLocation(),GetActorLocation()-DashDirection*90,FColor::Cyan,false,.12,0,5);}
 FVector L=GetActorLocation();L.X=FMath::Clamp(L.X,-2250.f,2250.f);L.Y=FMath::Clamp(L.Y,-2250.f,2250.f);if(L.Z<-100)L.Z=100;SetActorLocation(L);
 SlashTime=FMath::Max(0.f,SlashTime-Delta);SlashVisualTime=FMath::Max(0.f,SlashVisualTime-Delta);
 UAnimSequence* Desired=SlashVisualTime>0?SlashAnimation.Get():(GetVelocity().Size2D()>10||DashTime>0?RunAnimation.Get():IdleAnimation.Get());
 if(Desired&&Desired!=CurrentAnimation){GetMesh()->PlayAnimation(Desired,true);GetMesh()->SetPlayRate(1.f);CurrentAnimation=Desired;}
}

UAbilityComponent::UAbilityComponent(){PrimaryComponentTick.bCanEverTick=true;FAbilitySpec Slash;Abilities.Add(Slash);FAbilitySpec Wave;Wave.Kind=EAbilityKind::FlyingSlash;Wave.Cooldown=3.5f;Wave.Damage=18;Wave.Range=1400;Abilities.Add(Wave);}
void UAbilityComponent::TickComponent(float Delta,ELevelTick TickType,FActorComponentTickFunction* Function){Super::TickComponent(Delta,TickType,Function);auto* G=Run(this);if(!G||G->State!=ERunState::Playing)return;SlashRemaining=FMath::Max(0.f,SlashRemaining-Delta);WaveRemaining=FMath::Max(0.f,WaveRemaining-Delta);if(AutoAttack){Katana();FlyingSlash();}}
void UAbilityComponent::Katana(){auto* P=Cast<ACatmuraiCharacter>(GetOwner());auto* G=Run(this);if(!P||!G||G->State!=ERunState::Playing||SlashRemaining>0)return;const FAbilitySpec* Spec=Abilities.FindByPredicate([](const FAbilitySpec& S){return S.Kind==EAbilityKind::Katana;});if(!Spec)return;
 ASurvivalEnemy* Nearest=nullptr;float Best=Spec->Range*P->AreaMultiplier;for(auto E:G->EnemyPool)if(E->Active){const float D=FVector::Dist2D(E->GetActorLocation(),P->GetActorLocation());if(D<Best){Best=D;Nearest=E;}}if(AutoAttack&&!Nearest)return;if(Nearest)P->SetActorRotation(FRotator(0,(Nearest->GetActorLocation()-P->GetActorLocation()).Rotation().Yaw,0));
 SlashRemaining=FMath::Max(.12f,Spec->Cooldown*P->CooldownMultiplier);P->SlashTime=.25f;P->PlaySlashAnimation();const FVector Origin=P->GetActorLocation();const FVector Forward=P->GetActorForwardVector();
 for(auto E:G->EnemyPool)if(E->Active){const FVector Offset=E->GetActorLocation()-Origin;if(Offset.Size2D()<=Spec->Range*P->AreaMultiplier&&FVector::DotProduct(Forward,Offset.GetSafeNormal2D())>.15f){const bool Critical=FMath::FRand()<P->CritChance;E->Hit(Spec->Damage*P->DamageMultiplier*(Critical?P->CritMultiplier:1),Offset.GetSafeNormal2D(),P->Knockback,Critical);}}
 FVector Previous;for(int i=0;i<=18;i++){const FVector Point=Origin+Forward.RotateAngleAxis(-80+i*160.f/18,FVector::UpVector)*Spec->Range*P->AreaMultiplier;if(i)DrawDebugLine(GetWorld(),Previous,Point,FColor(160,230,255),false,.18f,0,6);Previous=Point;}P->CombatCue(TEXT("Katana"),Origin,false);
}
void UAbilityComponent::FlyingSlash(){auto* P=Cast<ACatmuraiCharacter>(GetOwner());auto* G=Run(this);if(!P||!G||G->State!=ERunState::Playing||WaveRemaining>0)return;const FAbilitySpec* S=Abilities.FindByPredicate([](const FAbilitySpec& A){return A.Kind==EAbilityKind::FlyingSlash;});if(!S)return;
 ASurvivalEnemy* Nearest=nullptr;float Best=S->Range;for(auto E:G->EnemyPool)if(E->Active){float D=FVector::Dist2D(E->GetActorLocation(),P->GetActorLocation());if(D<Best){Best=D;Nearest=E;}}if(!Nearest)return;WaveRemaining=FMath::Max(.2f,S->Cooldown*P->CooldownMultiplier);const FVector Aim=(Nearest->GetActorLocation()-P->GetActorLocation()).GetSafeNormal2D();const int32 Count=FMath::Clamp(S->ProjectileCount+P->ExtraProjectiles,1,8);
 for(int32 i=0;i<Count;i++){auto* B=GetWorld()->SpawnActor<ACombatProjectile>(P->GetActorLocation(),FRotator::ZeroRotator);if(B){B->Direction=Aim.RotateAngleAxis((i-(Count-1)*.5f)*12,FVector::UpVector);B->Damage=S->Damage*P->DamageMultiplier;B->Life=S->Duration;}}P->CombatCue(TEXT("FlyingSlash"),P->GetActorLocation(),false);
}
ASurvivalEnemy::ASurvivalEnemy(){PrimaryActorTick.bCanEverTick=false;Body=CreateDefaultSubobject<UStaticMeshComponent>(TEXT("ReplaceableEnemy"));SetRootComponent(Body);Body->SetStaticMesh(Shape(TEXT("Sphere")));Body->SetCollisionEnabled(ECollisionEnabled::NoCollision);Health=CreateDefaultSubobject<UHealthComponent>(TEXT("Health"));}
void ASurvivalEnemy::Activate(const FEnemySpec& InSpec,FVector Location,bool IsElite){Spec=InSpec;Body->SetStaticMesh(Shape(Spec.Name==TEXT("Angry Sushi")?TEXT("Cube"):Spec.Name==TEXT("Barrel Crab")?TEXT("Cylinder"):TEXT("Sphere")));Elite=IsElite;Active=true;Push=FVector::ZeroVector;AttackTimer=1;Windup=0;Health->Maximum=Health->Current=Spec.Health*(Elite?12:1);SetActorLocation(Location);SetActorHiddenInGame(false);SetActorScale3D(FVector(Spec.Scale*(Elite?2.2f:1)));Tint(Body,Elite?FLinearColor(.45,.08,.8):Spec.Color);}
void ASurvivalEnemy::Simulate(float Delta,ACatmuraiCharacter* P){if(!Active||!P)return;AttackTimer-=Delta;FVector Offset=P->GetActorLocation()-GetActorLocation();Offset.Z=0;const float Distance=Offset.Size();const FVector Direction=Offset.GetSafeNormal();if(Distance>85*Spec.Scale){FVector Move=Direction*Spec.Speed*Delta+Push*Delta;SetActorLocation(GetActorLocation()+Move);SetActorRotation(Direction.Rotation());}Push*=FMath::Exp(-Delta*8);if(Distance<125*Spec.Scale&&AttackTimer<=0){if(Windup<=0)Windup=.35f;else{Windup-=Delta;DrawDebugCircle(GetWorld(),GetActorLocation(),125*Spec.Scale,24,FColor::Red,false,.06,0,3,FVector::ForwardVector,FVector::RightVector,false);if(Windup<=0){P->ApplyIncomingHit(Spec.Damage*(Elite?2:1));AttackTimer=1.1f;}}}else Windup=0;}
void ASurvivalEnemy::Hit(float Damage,FVector Direction,float Force,bool Critical){if(!Active)return;auto* G=Run(this);if(!G)return;Push=Direction*Force;FCombatNumber Number;Number.Position=GetActorLocation()+FVector(0,0,70);Number.Value=Damage;Number.Critical=Critical;if(G->Numbers.Num()<100)G->Numbers.Add(Number);DrawDebugSphere(GetWorld(),GetActorLocation(),Critical?55:30,8,Critical?FColor::Yellow:FColor::Cyan,false,.12,0,2);if(auto* P=G->Player())P->CombatCue(Critical?TEXT("Critical"):TEXT("Impact"),GetActorLocation(),Critical);if(Health->Hurt(Damage)){Active=false;SetActorHiddenInGame(true);G->EnemyDefeated(this);}}
AXPOrb::AXPOrb(){PrimaryActorTick.bCanEverTick=false;auto* Mesh=CreateDefaultSubobject<UStaticMeshComponent>(TEXT("XP"));SetRootComponent(Mesh);Mesh->SetStaticMesh(Shape(TEXT("Sphere")));Mesh->SetWorldScale3D(FVector(.18f));Mesh->SetCollisionEnabled(ECollisionEnabled::NoCollision);}
void AXPOrb::Activate(FVector Location,int32 InValue){Value=InValue;Active=true;Location.Z=35;SetActorLocation(Location);SetActorHiddenInGame(false);Tint(Cast<UStaticMeshComponent>(RootComponent),FLinearColor(.025,1,.4));}
void AXPOrb::Simulate(float Delta,ACatmuraiCharacter* P){if(!Active||!P)return;const float D=FVector::Dist2D(P->GetActorLocation(),GetActorLocation());if(D<260){FVector Goal=P->GetActorLocation();Goal.Z=35;SetActorLocation(FMath::VInterpTo(GetActorLocation(),Goal,Delta,8));}if(D<70){Active=false;SetActorHiddenInGame(true);if(auto* G=Run(this))G->AddXP(Value);}}
ACombatProjectile::ACombatProjectile(){PrimaryActorTick.bCanEverTick=true;auto* Root=CreateDefaultSubobject<USceneComponent>(TEXT("Root"));SetRootComponent(Root);}
void ACombatProjectile::Tick(float Delta){Super::Tick(Delta);auto* G=Run(this);if(!G||G->State!=ERunState::Playing)return;const FVector Old=GetActorLocation();const FVector Next=Old+Direction*1000*Delta;SetActorLocation(Next);const FVector Side=FVector::CrossProduct(Direction,FVector::UpVector)*70;DrawDebugLine(GetWorld(),Next-Side,Next+Direction*30,FColor::Cyan,false,.06,0,7);DrawDebugLine(GetWorld(),Next+Direction*30,Next+Side,FColor(170,80,255),false,.06,0,7);for(auto E:G->EnemyPool)if(E->Active&&!Struck.Contains(E)&&FMath::PointDistToSegment(E->GetActorLocation(),Old,Next)<85*E->Spec.Scale){Struck.Add(E);E->Hit(Damage,Direction,180,false);}Life-=Delta;if(Life<=0)Destroy();}




