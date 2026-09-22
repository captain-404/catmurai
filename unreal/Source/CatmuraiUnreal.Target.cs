using UnrealBuildTool;
public class CatmuraiUnrealTarget : TargetRules {
 public CatmuraiUnrealTarget(TargetInfo Target) : base(Target) { Type=TargetType.Game; DefaultBuildSettings=BuildSettingsVersion.V7; IncludeOrderVersion=EngineIncludeOrderVersion.Unreal5_8; ExtraModuleNames.Add("CatmuraiSurvival"); }
}

