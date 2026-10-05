#include "MoonfallArena.h"
#include "Components/HierarchicalInstancedStaticMeshComponent.h"
#include "Components/BoxComponent.h"
#include "Components/PointLightComponent.h"
#include "Materials/MaterialInstanceDynamic.h"
#include "Engine/StaticMesh.h"
#include "Engine/World.h"

AMoonfallArena::AMoonfallArena(){PrimaryActorTick.bCanEverTick=false;RootComponent=CreateDefaultSubobject<USceneComponent>(TEXT("ShrineRoot"));RootComponent->SetMobility(EComponentMobility::Static);}
UHierarchicalInstancedStaticMeshComponent* AMoonfallArena::Batch(const TCHAR* Name,const TCHAR* Mesh,const TCHAR* Material,FLinearColor Fallback,bool Shadows){
 auto* C=NewObject<UHierarchicalInstancedStaticMeshComponent>(this,FName(Name));C->SetupAttachment(RootComponent);C->SetMobility(EComponentMobility::Static);C->SetStaticMesh(LoadObject<UStaticMesh>(nullptr,Mesh));C->SetCollisionEnabled(ECollisionEnabled::NoCollision);C->SetCastShadow(Shadows);C->bAutoRebuildTreeOnInstanceChanges=false;
 FString Path=FString::Printf(TEXT("/Game/Moonfall/Materials/%s.%s"),Material,Material);auto* M=LoadObject<UMaterialInterface>(nullptr,*Path);
 if(!M){auto* Base=LoadObject<UMaterialInterface>(nullptr,TEXT("/Game/Materials/M_Prototype.M_Prototype"));if(Base){auto* D=UMaterialInstanceDynamic::Create(Base,this);D->SetVectorParameterValue(TEXT("Color"),Fallback);M=D;}}
 if(M)C->SetMaterial(0,M);C->InstanceStartCullDistance=0;C->InstanceEndCullDistance=12000;C->RegisterComponent();Batches.Add(C);++InstanceComponentCount;return C;
}
void AMoonfallArena::Put(UHierarchicalInstancedStaticMeshComponent* C,FVector P,FVector S,FRotator R){if(C){C->AddInstance(FTransform(R,P,S));++DecorativeInstanceCount;}}
void AMoonfallArena::BuildShrine(){if(bBuilt)return;bBuilt=true;
 const TCHAR* Cube=TEXT("/Engine/BasicShapes/Cube.Cube");const TCHAR* Cone=TEXT("/Engine/BasicShapes/Cone.Cone");const TCHAR* Sphere=TEXT("/Engine/BasicShapes/Sphere.Sphere");const TCHAR* Cylinder=TEXT("/Engine/BasicShapes/Cylinder.Cylinder");
 auto* Floor=Batch(TEXT("Paving"),Cube,TEXT("M_ShrineStone"),FLinearColor(.22,.17,.3));
 const TCHAR* Shard=TEXT("/Game/Release/Environment/SM_BasaltShard.SM_BasaltShard");const TCHAR* BasaltBlock=TEXT("/Game/Release/Environment/SM_BasaltBlock.SM_BasaltBlock");const TCHAR* CurvedRoof=TEXT("/Game/Release/Environment/SM_CurvedRoof.SM_CurvedRoof");const TCHAR* CherryCluster=TEXT("/Game/Release/Environment/SM_CherryCluster.SM_CherryCluster");
 auto* Rock=Batch(TEXT("Basalt"),Shard,TEXT("M_Basalt"),FLinearColor(.085,.065,.14));
 auto* Blocks=Batch(TEXT("Masonry"),BasaltBlock,TEXT("M_Basalt"),FLinearColor(.085,.065,.14));
 auto* Wood=Batch(TEXT("Vermilion"),Cylinder,TEXT("M_Vermilion"),FLinearColor(.36,.025,.05));
 auto* Beams=Batch(TEXT("RailBeams"),Cube,TEXT("M_Vermilion"),FLinearColor(.36,.025,.05));
 auto* Roof=Batch(TEXT("RoofTiles"),CurvedRoof,TEXT("M_Roof"),FLinearColor(.045,.055,.095));
 auto* Trunk=Batch(TEXT("CherryBranches"),Cylinder,TEXT("M_Bark"),FLinearColor(.1,.045,.075));
 auto* Leaves=Batch(TEXT("CherryCanopies"),CherryCluster,TEXT("M_Cherry"),FLinearColor(.48,.1,.45),false);
 auto* Glow=Batch(TEXT("MoonRunes"),Cube,TEXT("M_MoonGlow"),FLinearColor(.48,.12,1),false);
 auto* LanternGlow=Batch(TEXT("LanternGlow"),Sphere,TEXT("M_LanternGlow"),FLinearColor(1,.06,.4),false);
 auto* Water=Batch(TEXT("SpiritFalls"),Cylinder,TEXT("M_SpiritWater"),FLinearColor(.2,.14,.65),false);
 auto* Halo=Batch(TEXT("MistIslands"),Sphere,TEXT("M_Mist"),FLinearColor(.13,.07,.25),false);
 // A continuous collider has exactly Z=0 as its top; paving/runes never affect movement.
 auto Collider=[this](const TCHAR* Name,FVector P,FVector Extent){auto* C=NewObject<UBoxComponent>(this,FName(Name));C->SetupAttachment(RootComponent);C->SetMobility(EComponentMobility::Static);C->SetBoxExtent(Extent);C->SetRelativeLocation(P);C->SetCollisionProfileName(TEXT("BlockAll"));C->SetHiddenInGame(true);C->RegisterComponent();};
 Collider(TEXT("WalkableFloor"),FVector(0,0,-30),FVector(2400,2400,30));
 Collider(TEXT("SafetyEast"),FVector(2380,0,90),FVector(20,2400,130));Collider(TEXT("SafetyWest"),FVector(-2380,0,90),FVector(20,2400,130));
 Collider(TEXT("SafetyNorth"),FVector(0,2380,90),FVector(2400,20,130));Collider(TEXT("SafetySouth"),FVector(0,-2380,90),FVector(2400,20,130));
 Put(Floor,FVector(0,0,-35),FVector(48,48,.7));
 FRandomStream Random(4042026);
 // Cliff columns point downward around the floating island. Upper broken stones stay outside combat.
 for(int Side=0;Side<4;++Side)for(int i=0;i<18;++i){float T=-2320+i*273.f;FVector P=Side<2?FVector(T,Side?2440:-2440,-500):FVector(Side==2?2440:-2440,T,-500);
  // Narrow staggered basalt prisms make a fractured cliff silhouette instead of visible circular cone caps.
  float Depth=Random.FRandRange(900,1650);
  FVector CliffCenter=P+FVector(Random.FRandRange(-70,70),Random.FRandRange(-70,70),500-Depth*.5f-85);
  Put(Blocks,CliffCenter,FVector(Random.FRandRange(2.3,4.1),Random.FRandRange(2.3,4.1),Depth/100),FRotator(Random.FRandRange(-5,5),Random.FRandRange(0,360),Random.FRandRange(-5,5)));
  Put(Blocks,CliffCenter+FVector(Random.FRandRange(-110,110),Random.FRandRange(-110,110),-Depth*.33f),FVector(1.4,1.6,Random.FRandRange(4,8)),FRotator(Random.FRandRange(-12,12),Random.FRandRange(0,360),Random.FRandRange(-12,12)));
  Put(Blocks,P+FVector(0,0,420),FVector(2.7,2.5,1.8),FRotator(0,Random.FRandRange(-15,15),0));
 }
 // Circular sanctuary is a flush inlaid mosaic, with a crescent and constellations.
 for(int Ring=0;Ring<3;++Ring){float Radius=Ring==0?395:Ring==1?530:620;int Segments=Ring==0?64:48;for(int i=0;i<Segments;++i){float A=2*PI*i/Segments;Put(Ring==2?Floor:Glow,FVector(FMath::Cos(A)*Radius,FMath::Sin(A)*Radius,2),FVector(Ring==2?.95:.13,2*PI*Radius/Segments/100.f*.87,.025),FRotator(0,FMath::RadiansToDegrees(A),0));}}
 for(int i=0;i<39;++i){float A=FMath::Lerp(.4f,5.85f,i/38.f);Put(Glow,FVector(-45+FMath::Cos(A)*218,FMath::Sin(A)*218,3),FVector(.09,.33,.025),FRotator(0,FMath::RadiansToDegrees(A),0));}
 for(int i=0;i<18;++i){float A=2*PI*i/18;Put(Glow,FVector(FMath::Cos(A)*460,FMath::Sin(A)*460,3),FVector(.2,.45,.025),FRotator(0,FMath::RadiansToDegrees(A)+35,0));}
 auto Beam=[&](auto* B,FVector A,FVector Z,float Width){FVector D=Z-A;Put(B,(A+Z)*.5f,FVector(D.Size()/100,Width,Width),D.Rotation());};
 auto Branch=[&](FVector A,FVector B,float Width){FVector D=B-A;Put(Trunk,(A+B)*.5f,FVector(Width,Width,D.Size()/100),FRotationMatrix::MakeFromZ(D).Rotator());};
 auto Gate=[&](FVector P,float Yaw){auto Local=[&](FVector V){return P+FRotator(0,Yaw,0).RotateVector(V);};
  for(int Sign:{-1,1}){Put(Blocks,Local(FVector(Sign*280,0,40)),FVector(1.1,1.1,.8));Put(Wood,Local(FVector(Sign*280,0,270)),FVector(.65,.65,5.2));Put(Roof,Local(FVector(Sign*280,0,150)),FVector(.9,.9,.24),FRotator(0,Yaw,0));Put(LanternGlow,Local(FVector(Sign*220,0,385)),FVector(.45,.45,.6));}
  Beam(Beams,Local(FVector(-390,0,430)),Local(FVector(390,0,430)),.38);
  // Curved roof silhouette assembled from short sloped segments, each with an overlapping tile cap.
  for(int i=-5;i<=5;++i){float X=i*79.f;float Height=510+FMath::Square(float(i)/5)*58;Put(Roof,Local(FVector(X,0,Height)),FVector(.86,1.05,.22),FRotator(-i*3.8f,Yaw,0));Put(Beams,Local(FVector(X,0,Height-24)),FVector(.86,.64,.2),FRotator(-i*3.8f,Yaw,0));}
 };
 // Four satellite shrine foundations attach each torii to real stonework.
 for(int Shrine=0;Shrine<4;++Shrine){float Yaw=Shrine*90.f;FVector Out=FRotator(0,Yaw,0).RotateVector(FVector(0,1,0));FVector P=Out*2840;
  Put(Blocks,P-FVector(0,0,220),FVector(10.4,9.4,6.4),FRotator(0,Yaw,0));
  Put(Floor,P+FVector(0,0,80),FVector(10.8,9.8,.6),FRotator(0,Yaw,0));
  Put(Blocks,P-FVector(0,0,760),FVector(6.7,6.4,9),FRotator(3,Yaw+12,-4));
  Put(Blocks,P-FVector(0,0,1290),FVector(4.2,4.6,5.5),FRotator(-8,Yaw-17,7));
  // Decorative treads rise from the outer arena edge; collision boundaries remain untouched.
  for(int Step=0;Step<7;++Step){float Height=(Step+1)*15.f;Put(Floor,Out*(2405+Step*31)+FVector(0,0,Height-12),FVector(5.9,.46,.24),FRotator(0,Yaw,0));}
  for(int Segment=0;Segment<32;++Segment){float A=2*PI*Segment/32;Put(Glow,P+FVector(FMath::Cos(A)*290,FMath::Sin(A)*290,111),FVector(.08,.5,.025),FRotator(0,FMath::RadiansToDegrees(A),0));}
  Gate(Out*2740+FVector(0,0,110),Yaw);
 }
 // Rails and lit stone lanterns give the edges a readable safety boundary.
 int LightIndex=0;
 for(int Side=0;Side<4;++Side)for(int i=0;i<11;++i){float T=-2220+i*444.f;if(FMath::Abs(T)<450)continue;FVector P=Side<2?FVector(T,Side?2470:-2470,0):FVector(Side==2?2470:-2470,T,0);float Yaw=Side<2?0:90;
  Put(Blocks,P+FVector(0,0,75),FVector(.58,.58,1.5));Put(Beams,P+FVector(0,0,92),FVector(4.5,.16,.16),FRotator(0,Yaw,0));Put(Beams,P+FVector(0,0,150),FVector(4.5,.18,.18),FRotator(0,Yaw,0));
  if(i%3==0){Put(Blocks,P+FVector(0,0,200),FVector(.75,.75,.28));Put(LanternGlow,P+FVector(0,0,245),FVector(.46,.46,.65));Put(Roof,P+FVector(0,0,290),FVector(1,1,.16),FRotator(0,Yaw+45,0));
   if(LightIndex<8){auto* L=NewObject<UPointLightComponent>(this);L->SetupAttachment(RootComponent);L->SetMobility(EComponentMobility::Movable);L->SetRelativeLocation(P+FVector(0,0,275));L->SetIntensity(650);L->SetAttenuationRadius(560);L->SetLightColor(FLinearColor(1,.12,.48));L->SetCastShadows(false);L->RegisterComponent();++LightIndex;}
  }
 }
 SmallLightCount=LightIndex;
 // Segmented trees are deliberately beyond the playable rectangle, so canopies do not hide combat.
 for(int Tree=0;Tree<12;++Tree){float A=2*PI*Tree/12+.17f;float TreeRadius=3000.f/FMath::Max(FMath::Abs(FMath::Cos(A)),FMath::Abs(FMath::Sin(A)));FVector P(FMath::Cos(A)*TreeRadius,FMath::Sin(A)*TreeRadius,0);Put(Blocks,P-FVector(0,0,140),FVector(4,4,2.8),FRotator(0,Tree*47,0));
  FVector Crown=P+FVector(60,-20,440);Branch(P,Crown,.45);for(int Limb=0;Limb<5;++Limb){float B=Limb*2*PI/5+Tree;FVector Tip=Crown+FVector(FMath::Cos(B)*220,FMath::Sin(B)*220,Random.FRandRange(25,170));Branch(Crown,Tip,.2);Branch(Tip,Tip+FVector(FMath::Cos(B)*90,FMath::Sin(B)*90,100),.1);
   for(int Cluster=0;Cluster<4;++Cluster){FVector Q=Tip+FVector(Random.FRandRange(-85,85),Random.FRandRange(-85,85),Random.FRandRange(-15,100));Put(Leaves,Q,FVector(Random.FRandRange(1.3,2.1),Random.FRandRange(1.2,1.9),.65),FRotator(0,Random.FRandRange(0,360),0));}
  }
  for(int i=0;i<7;++i)Put(Rock,P+FVector(Random.FRandRange(-240,240),Random.FRandRange(-240,240),Random.FRandRange(40,190)),FVector(1,1,Random.FRandRange(1.5,3.7)),FRotator(0,Random.FRandRange(0,360),0));
 }
 // Long spirit falls and low mist banks establish depth without any translucent overdraw.
 for(int i=0;i<8;++i){float A=(i+.5f)*PI/4;FVector P(FMath::Cos(A)*2760,FMath::Sin(A)*2760,-900);Put(Water,P,FVector(.5,.65,17),FRotator(0,i*45,0));Put(Halo,P-FVector(0,0,320),FVector(8,8,2));}
 for(int i=0;i<22;++i){float A=Random.FRandRange(0,2*PI);float R=Random.FRandRange(3450,4300);Put(Rock,FVector(FMath::Cos(A)*R,FMath::Sin(A)*R,Random.FRandRange(-850,-200)),FVector(1.5,1.4,Random.FRandRange(3,6)),FRotator(180,i*37,0));}
 // Low ruins and petal beds mark courtyard quarters without hiding enemies or changing the combat floor.
 const FVector Gardens[]={FVector(-1600,900,0),FVector(1500,1000,0),FVector(400,-1700,0),FVector(-1550,-1450,0)};
 for(auto Site:Gardens){for(int i=0;i<9;++i){const float A=i*2*PI/9;Put(Blocks,Site+FVector(cosf(A)*140,sinf(A)*140,12),FVector(.6,.7,.22),FRotator(0,i*41,0));Put(Leaves,Site+FVector(cosf(A)*85,sinf(A)*85,18),FVector(.35,.3,.09),FRotator(0,i*61,0));}Put(Rock,Site+FVector(0,0,22),FVector(.5,.5,.44),FRotator(0,44,0));}
 for(auto C:Batches)C->BuildTreeIfOutdated(false,true);
 UE_LOG(LogTemp,Display,TEXT("MOONFALL_ARENA_READY: batches=%d instances=%d lights=%d walkable=+/-%.0f floor=0"),InstanceComponentCount,DecorativeInstanceCount,SmallLightCount,WalkableHalfExtent);
}
