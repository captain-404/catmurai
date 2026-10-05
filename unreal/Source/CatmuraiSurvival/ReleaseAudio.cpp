#include "ReleaseAudio.h"
#include "Kismet/GameplayStatics.h"
#include "Components/AudioComponent.h"
#include "Sound/SoundWave.h"
#include "HAL/PlatformTime.h"
namespace {TWeakObjectPtr<UAudioComponent> MusicPlayer;float MusicVolume=.65f,SFXVolume=.8f;TMap<FName,TWeakObjectPtr<USoundWave>> Cues;}
void FReleaseAudio::StartMusic(UObject* Context){
 if(MusicPlayer.IsValid()){MusicPlayer->Stop();MusicPlayer->DestroyComponent();}
 auto* Wave=LoadObject<USoundWave>(nullptr,TEXT("/Game/Release/Audio/Music_Loop.Music_Loop"));
 if(Wave)MusicPlayer=UGameplayStatics::SpawnSound2D(Context,Wave,MusicVolume,1,0,nullptr,true,false);
 if(MusicPlayer.IsValid())MusicPlayer->bIsUISound=true;
}
void FReleaseAudio::PlayCue(UObject* Context,FName Name){
 static TMap<FName,double> LastPlayed;const double Now=FPlatformTime::Seconds();double* Last=LastPlayed.Find(Name);if(Last&&Now-*Last<.06)return;LastPlayed.Add(Name,Now);auto& Ref=Cues.FindOrAdd(Name);if(!Ref.IsValid()){const FString Path=FString::Printf(TEXT("/Game/Release/Audio/S_%s.S_%s"),*Name.ToString(),*Name.ToString());Ref=LoadObject<USoundWave>(nullptr,*Path);}
 if(Ref.IsValid()&&SFXVolume>0)UGameplayStatics::PlaySound2D(Context,Ref.Get(),SFXVolume);
}
void FReleaseAudio::SetMix(float Music,float SFX){MusicVolume=FMath::Clamp(Music,0.f,1.f);SFXVolume=FMath::Clamp(SFX,0.f,1.f);if(MusicPlayer.IsValid())MusicPlayer->SetVolumeMultiplier(MusicVolume);}
