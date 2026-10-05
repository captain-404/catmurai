@echo off
set "CATMURAI_BUILD=%~dp0Builds\SinglePlayerPreview\Windows\CatmuraiUnreal.exe"
if not exist "%CATMURAI_BUILD%" set "CATMURAI_BUILD=%~dp0Builds\MoonfallLightingFix\Windows\CatmuraiUnreal.exe"
if not exist "%CATMURAI_BUILD%" set "CATMURAI_BUILD=%~dp0Builds\Moonfall\Windows\CatmuraiUnreal.exe"
if not exist "%CATMURAI_BUILD%" set "CATMURAI_BUILD=%~dp0Builds\SpellArt\Windows\CatmuraiUnreal.exe"
if exist "%CATMURAI_BUILD%" (
 start "Catmurai Survival" "%CATMURAI_BUILD%" -windowed -ResX=1280 -ResY=720
 exit /b 0
)
set "UE_EDITOR=D:\UE_5.8\Engine\Binaries\Win64\UnrealEditor.exe"
if not exist "%UE_EDITOR%" (
 echo Unreal Engine 5.8 was not found at D:\UE_5.8, and no standalone game build is available.
 pause
 exit /b 1
)
start "Catmurai Survival" "%UE_EDITOR%" "%~dp0CatmuraiUnreal.uproject" /Game/Maps/SurvivalArena?game=/Script/CatmuraiSurvival.SurvivalGameMode -game -windowed -ResX=1280 -ResY=720 -NoSplash
