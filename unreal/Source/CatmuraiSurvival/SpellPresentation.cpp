#include "SpellPresentation.h"
#include "Survival.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/Texture2D.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "Kismet/GameplayStatics.h"

UTexture2D* USpellPresentation::Texture(FName Name) const {
 const auto* Found=Textures.Find(Name);return Found?Found->Get():nullptr;
}
FName USpellPresentation::UpgradeIcon(uint8 Kind){
 static const FName Names[]={TEXT("spell_infernal_moon"),TEXT("spell_shadow_trio_dash"),TEXT("spell_phantom_claw_rend"),TEXT("spell_sakura_bloom"),TEXT("spell_spirit_bell"),TEXT("spell_ember_potion"),TEXT("spell_shadow_shuriken_trio"),TEXT("spell_glacial_paw")};
 return Kind<UE_ARRAY_COUNT(Names)?Names[Kind]:NAME_None;
}
ASpellEffect::ASpellEffect(){
 PrimaryActorTick.bCanEverTick=true;
 Sprite=CreateDefaultSubobject<UStaticMeshComponent>(TEXT("SpellSprite"));SetRootComponent(Sprite);
 Sprite->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,TEXT("/Engine/BasicShapes/Plane.Plane")));
 Sprite->SetCollisionEnabled(ECollisionEnabled::NoCollision);Sprite->SetCastShadow(false);
 Sprite->bReceivesDecals=false;Sprite->SetTranslucentSortPriority(1);
}
ASpellEffect* ASpellEffect::Spawn(UObject* Context,FName Art,FVector Location,float Size,float Life,float Yaw,float RotationSpeed,float Alpha){
 auto* G=Cast<ASurvivalGameMode>(UGameplayStatics::GetGameMode(Context));
 if(!G||!G->SpellArt||!G->SpellArt->EffectMaterial)return nullptr;
 auto* Texture=G->SpellArt->Texture(Art);if(!Texture)return nullptr;
 G->SpellEffects.RemoveAll([](const TWeakObjectPtr<ASpellEffect>& E){return !E.IsValid();});
 // Bound translucent overdraw and actor allocation in dense waves.
 if(G->SpellEffects.Num()>=64)return nullptr;
 auto* E=G->GetWorld()->SpawnActor<ASpellEffect>(Location,FRotator(0,Yaw,0));if(!E)return nullptr;
 E->Diameter=Size;E->Duration=FMath::Max(.05f,Life);E->Spin=RotationSpeed;E->Opacity=Alpha;
 E->Dynamic=UMaterialInstanceDynamic::Create(G->SpellArt->EffectMaterial,E);
 E->Dynamic->SetTextureParameterValue(TEXT("SpellTexture"),Texture);
 E->Dynamic->SetScalarParameterValue(TEXT("BlackKey"),G->SpellArt->BlackKeys.FindRef(Art));
 E->Dynamic->SetScalarParameterValue(TEXT("Fade"),0);
 E->Sprite->SetMaterial(0,E->Dynamic);E->SetActorScale3D(FVector(Size/100,Size/100,1));
 G->SpellEffects.Add(E);return E;
}
void ASpellEffect::Tick(float Delta){
 Super::Tick(Delta);
 auto* G=Cast<ASurvivalGameMode>(UGameplayStatics::GetGameMode(this));if(!G||G->State!=ERunState::Playing)return;
 Age+=Delta;const float T=FMath::Clamp(Age/Duration,0.f,1.f);
 CurrentFade=Opacity*FMath::Min(T/.08f,1.f)*FMath::Clamp((1-T)/.6f,0.f,1.f);
 if(Dynamic)Dynamic->SetScalarParameterValue(TEXT("Fade"),CurrentFade);
 const float Growth=FMath::Lerp(.75f,1.15f,T);SetActorScale3D(FVector(Diameter*Growth/100,Diameter*Growth/100,1));
 AddActorLocalRotation(FRotator(0,Spin*Delta,0));if(Age>=Duration)Destroy();
}

void ACatmuraiCharacter::CombatCue_Implementation(FName Cue,FVector Location,bool Critical){
 const float Yaw=GetActorRotation().Yaw;
 if(Cue==TEXT("Katana"))ASpellEffect::Spawn(this,TEXT("spell_phantom_claw_rend"),Location+GetActorForwardVector()*120+FVector(0,0,-25),300*AreaMultiplier,.24f,Yaw,240);
 else if(Cue==TEXT("Dash"))ASpellEffect::Spawn(this,TEXT("spell_frost_dash"),Location-FVector(0,0,35),200,.28f,Yaw,0,.65f);
 else if(Cue==TEXT("DashTrail"))ASpellEffect::Spawn(this,TEXT("spell_shadow_trio_dash"),Location-FVector(0,0,38),150,.22f,Yaw,0,.42f);
 else if(Cue==TEXT("Impact")||Cue==TEXT("Critical"))ASpellEffect::Spawn(this,Critical?TEXT("spell_solar_pounce"):TEXT("spell_glacial_paw"),Location+FVector(0,0,18),Critical?140:95,.18f,Yaw,150,.7f);
 else if(Cue==TEXT("EnemyDeath"))ASpellEffect::Spawn(this,TEXT("spell_shadow_bomb"),Location+FVector(0,0,-15),130,.28f,Yaw,90,.55f);
 else if(Cue==TEXT("Hurt"))ASpellEffect::Spawn(this,TEXT("spell_guardian_eye_ward"),Location+FVector(0,0,-28),180,.25f,Yaw,0,.7f);
 else if(Cue==TEXT("Jump"))ASpellEffect::Spawn(this,TEXT("spell_moonlit_pounce"),Location+FVector(0,0,-50),145,.28f,Yaw,90,.55f);
 else if(Cue==TEXT("DoubleJump"))ASpellEffect::Spawn(this,TEXT("spell_solar_pounce"),Location+FVector(0,0,-40),170,.3f,Yaw,90,.6f);
 else if(Cue==TEXT("Upgrade"))ASpellEffect::Spawn(this,TEXT("spell_sakura_bloom"),Location+FVector(0,0,-42),260,.6f,Yaw,160,.7f);
 else if(Cue==TEXT("Heal"))ASpellEffect::Spawn(this,TEXT("spell_verdant_lotus_paw"),Location+FVector(0,0,-42),240,.65f,Yaw,80,.7f);
 else if(Cue==TEXT("EliteIntro"))ASpellEffect::Spawn(this,TEXT("spell_infernal_moon"),Location+FVector(0,0,-40),280,.65f,Yaw,80,.7f);
}
