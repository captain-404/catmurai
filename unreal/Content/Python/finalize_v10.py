import unreal
from pathlib import Path
root=Path(unreal.Paths.project_dir())/'SourceAssets/CatmuraiV10'
dest='/Game/Characters/CatmuraiV10'
mesh=unreal.load_asset(dest+'/SK_Catmurai_v10')
sub=unreal.get_editor_subsystem(unreal.SkeletalMeshEditorSubsystem)
for i in range(1,4):
    assert sub.import_lod(mesh,i,str(root/'lods'/('Catmurai_LOD%d.fbx'%i)))==i
import runpy
runpy.run_path(str(Path(unreal.Paths.project_dir())/'Content/Python/repair_v10_materials.py'))
assert unreal.V10AssetTools.configure_mesh(mesh)
clips=[unreal.load_asset(dest+'/A_Catmurai_'+c) for c in ['Idle','Run','Slash','JumpStart','JumpRise','JumpFall','JumpLand','JumpFlip','Dash']]
for clip in clips:
    assert unreal.V10AssetTools.lock_grip(clip,mesh)
unreal.EditorAssetLibrary.save_directory(dest,False,True)
assert unreal.V10AssetTools.validate_assets(mesh,clips)
unreal.log('CATMURAI_V10_IMPORT_PASS')
