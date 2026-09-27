import unreal
from pathlib import Path
ROOT=Path(unreal.Paths.project_dir())/'SourceAssets/BleepFinal'
DEST='/Game/Characters/BleepFinal'
tools=unreal.AssetToolsHelpers.get_asset_tools()
sub=unreal.get_editor_subsystem(unreal.StaticMeshEditorSubsystem)
unreal.SystemLibrary.execute_console_command(None,'Interchange.FeatureFlags.Import.FBX 0')
def texture(name,normal=False,linear=False):
    path=DEST+'/Textures/'+name
    if unreal.EditorAssetLibrary.does_asset_exist(path):return unreal.load_asset(path)
    task=unreal.AssetImportTask();task.filename=str(ROOT/'textures'/(name+'.png'));task.destination_path=DEST+'/Textures';task.destination_name=name;task.automated=True;task.save=True
    tools.import_asset_tasks([task]);tex=unreal.load_asset(task.imported_object_paths[0])
    if normal:
        tex.set_editor_property('compression_settings',unreal.TextureCompressionSettings.TC_NORMALMAP)
        tex.set_editor_property('srgb',False);tex.set_editor_property('flip_green_channel',True)
    elif linear:
        tex.set_editor_property('srgb',False);tex.set_editor_property('compression_settings',unreal.TextureCompressionSettings.TC_MASKS)
    unreal.EditorAssetLibrary.save_loaded_asset(tex,False);return tex
base=texture('Bleep_BaseColor');emission=texture('Bleep_Emission');roughness=texture('Bleep_Roughness',linear=True)
normals=[texture(f'Bleep_Normal_LOD{i}_OpenGL',normal=True) for i in range(3)]
def material(i):
    name=f'M_Bleep_LOD{i}';path=DEST+'/Materials/'+name
    if unreal.EditorAssetLibrary.does_asset_exist(path):return unreal.load_asset(path)
    m=tools.create_asset(name,DEST+'/Materials',unreal.Material,unreal.MaterialFactoryNew());ed=unreal.MaterialEditingLibrary
    def node(kind):return ed.create_material_expression(m,kind)
    def sample(tex,kind=unreal.MaterialSamplerType.SAMPLERTYPE_COLOR):
        n=node(unreal.MaterialExpressionTextureSample);n.texture=tex;n.sampler_type=kind;return n
    def scalar(v):
        n=node(unreal.MaterialExpressionConstant);n.set_editor_property('r',v);return n
    ed.connect_material_property(sample(base),'RGB',unreal.MaterialProperty.MP_BASE_COLOR)
    ed.connect_material_property(sample(roughness,unreal.MaterialSamplerType.SAMPLERTYPE_MASKS),'R',unreal.MaterialProperty.MP_ROUGHNESS)
    ed.connect_material_property(scalar(.04),'',unreal.MaterialProperty.MP_METALLIC)
    glow=node(unreal.MaterialExpressionMultiply);ed.connect_material_expressions(sample(emission),'RGB',glow,'A');ed.connect_material_expressions(scalar(1.4),'',glow,'B');ed.connect_material_property(glow,'',unreal.MaterialProperty.MP_EMISSIVE_COLOR)
    flat=node(unreal.MaterialExpressionConstant3Vector);flat.set_editor_property('constant',unreal.LinearColor(0,0,1,1))
    lerp=node(unreal.MaterialExpressionLinearInterpolate);ed.connect_material_expressions(flat,'',lerp,'A');ed.connect_material_expressions(sample(normals[i],unreal.MaterialSamplerType.SAMPLERTYPE_NORMAL),'RGB',lerp,'B');ed.connect_material_expressions(scalar(.3),'',lerp,'Alpha')
    normal=node(unreal.MaterialExpressionNormalize);ed.connect_material_expressions(lerp,'',normal,'VectorInput');ed.connect_material_property(normal,'',unreal.MaterialProperty.MP_NORMAL)
    ed.recompile_material(m);unreal.EditorAssetLibrary.save_loaded_asset(m,False);return m
materials=[material(i) for i in range(3)]
path=DEST+'/SM_Bleep_Final'
if unreal.EditorAssetLibrary.does_asset_exist(path):mesh=unreal.load_asset(path)
else:
    options=unreal.FbxImportUI();options.automated_import_should_detect_type=False;options.import_as_skeletal=False;options.import_mesh=True;options.import_animations=False;options.import_materials=False;options.import_textures=False;options.mesh_type_to_import=unreal.FBXImportType.FBXIT_STATIC_MESH
    data=options.static_mesh_import_data;data.set_editor_property('combine_meshes',True);data.set_editor_property('auto_generate_collision',False);data.set_editor_property('normal_import_method',unreal.FBXNormalImportMethod.FBXNIM_IMPORT_NORMALS_AND_TANGENTS)
    task=unreal.AssetImportTask();task.filename=str(ROOT/'Bleep_Final_LOD0.fbx');task.destination_path=DEST;task.destination_name='SM_Bleep_Final';task.automated=True;task.save=True;task.options=options;task.factory=unreal.FbxFactory();tools.import_asset_tasks([task]);mesh=unreal.load_asset(path)
assert isinstance(mesh,unreal.StaticMesh)
for i in range(1,3):
    if sub.get_lod_count(mesh)<=i:assert sub.import_lod(mesh,i,str(ROOT/f'Bleep_Final_LOD{i}.fbx'))==i
slots=mesh.get_editor_property('static_materials')
for index,slot in enumerate(slots):
    name=str(slot.get_editor_property('imported_material_slot_name'))
    lod=next((i for i in range(3) if f'LOD{i}' in name),None)
    assert lod is not None,name
    slot.set_editor_property('material_interface',materials[lod]);slots[index]=slot
mesh.set_editor_property('static_materials',slots)
assert sub.set_lod_screen_sizes(mesh,[1.0,.35,.12])
if mesh.get_bounds().box_extent.z < 2:
    for i in range(3):
        settings=sub.get_lod_build_settings(mesh,i)
        settings.set_editor_property('build_scale3d',unreal.Vector(100,100,100))
        settings.set_editor_property('recompute_tangents',True)
        sub.set_lod_build_settings(mesh,i,settings)
unreal.EditorAssetLibrary.save_directory(DEST,False,True)
assert sub.get_lod_count(mesh)==3
bounds=mesh.get_bounds();assert 95<bounds.box_extent.z*2<105,str(bounds)
for i in range(3):unreal.log('BLEEP_LOD '+str(i)+' vertices='+str(sub.get_number_verts(mesh,i)))
unreal.log('BLEEP_IMPORT_PASS height_cm='+str(bounds.box_extent.z*2)+' slots='+str(len(slots)))
