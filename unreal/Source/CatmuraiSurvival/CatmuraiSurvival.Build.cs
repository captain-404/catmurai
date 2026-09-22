using UnrealBuildTool;
public class CatmuraiSurvival : ModuleRules {
 public CatmuraiSurvival(ReadOnlyTargetRules Target) : base(Target) {
  PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
  PublicDependencyModuleNames.AddRange(new string[]{"Core","CoreUObject","Engine","InputCore","SlateCore"});
 }
}
