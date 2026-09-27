import bpy,sys,json,hashlib
from pathlib import Path
source=Path(sys.argv[sys.argv.index('--')+1])
dest=Path(sys.argv[sys.argv.index('--')+2]);dest.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source))
report={'source':str(source),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),'lods':[]}
for i in range(3):
    bpy.ops.object.select_all(action='DESELECT')
    o=bpy.data.objects['Bleep_Final_LOD'+str(i)]
    o.hide_set(False);o.hide_viewport=False;o.hide_render=False;o.select_set(True);bpy.context.view_layer.objects.active=o
    o.data.calc_loop_triangles()
    report['lods'].append({'lod':i,'triangles':len(o.data.loop_triangles),'dimensions_m':list(o.dimensions),'materials':[m.name for m in o.data.materials]})
    bpy.ops.export_scene.fbx(filepath=str(dest/f'Bleep_Final_LOD{i}.fbx'),use_selection=True,object_types={'MESH'},bake_anim=False,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Y',axis_up='Z',mesh_smooth_type='FACE',path_mode='STRIP')
textures=dest/'textures';textures.mkdir(exist_ok=True)
for name,file in [('Bleep_BaseColor_FinalDetails','Bleep_BaseColor.png'),('Bleep_Emission_FinalDetails','Bleep_Emission.png'),('Bleep_Roughness_FinalDetails','Bleep_Roughness.png')]+[(f'Bleep_Normal_LOD{i}_OpenGL.png',f'Bleep_Normal_LOD{i}_OpenGL.png') for i in range(3)]:
    im=bpy.data.images[name];assert im.packed_file,name
    (textures/file).write_bytes(im.packed_file.data)
(dest/'source-manifest.json').write_text(json.dumps(report,indent=2))
print('BLEEP_EXPORT_PASS',json.dumps(report))
