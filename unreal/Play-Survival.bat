@echo off
set "UE_EDITOR=D:\UE_5.8\Engine\Binaries\Win64\UnrealEditor.exe"
if not exist "%UE_EDITOR%" (
 echo Unreal Engine 5.8 was not found at D:\UE_5.8.
 pause
 exit /b 1
)
start "Catmurai Survival" "%UE_EDITOR%" "%~dp0CatmuraiUnreal.uproject" /Game/Maps/SurvivalArena?game=/Script/CatmuraiSurvival.SurvivalGameMode -game -windowed -ResX=1280 -ResY=720 -NoSplash
