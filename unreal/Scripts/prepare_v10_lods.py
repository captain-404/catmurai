import bpy, json
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'SourceAssets/CatmuraiV10'
out=root/'lods'
out.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'model/Catmurai_v10_Drawn_UE.fbx'))
sword=next(o for o in bpy.data.objects if o.type=='MESH' and 'Sword' in o.name)
sword.parent=None
for m in list(sword.modifiers): sword.modifiers.remove(m)
for o in list(bpy.data.objects):
    if o!=sword: bpy.data.objects.remove(o,do_unlink=True)
bpy.ops.import_scene.fbx(filepath=str(root/'model/Catmurai_v10_LODs_UE.fbx'))
rig=next(o for o in bpy.data.objects if o.type=='ARMATURE')
sword.parent=rig
sword.modifiers.new('Armature','ARMATURE').object=rig
for i in range(1,4):
    for o in bpy.data.objects: o.select_set(False)
    mesh=bpy.data.objects['Catmurai_LOD'+str(i)]
    for o in (rig,mesh,sword): o.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=str(out/f'Catmurai_LOD{i}.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,global_scale=0.01,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Y',axis_up='Z')
print('V10_LOD_EXPORT_PASS')
