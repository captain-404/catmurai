#pragma once
#include "CoreMinimal.h"
struct CATMURAISURVIVAL_API FReleaseAudio {
 static void StartMusic(UObject* Context);
 static void PlayCue(UObject* Context,FName Name);
 static void SetMix(float Music,float SFX);
};
