import unreal
from pathlib import Path
p=Path(r'D:\YoutubeChannel\catmurai-unreal');src=p/'SourceAssets/Release';tools=unreal.AssetToolsHelpers.get_asset_tools();lib=unreal.EditorAssetLibrary;edit=unreal.MaterialEditingLibrary
def imp(file,dest,name,options=None):
 task=unreal.AssetImportTask();task.filename=str(file);task.destination_path=dest;task.destination_name=name;task.automated=True;task.replace_existing=True;task.save=True
 if options:task.options=options
 tools.import_asset_tasks([task]);asset=lib.load_asset(dest+'/'+name);assert asset,(file,name)
 return asset
for file,name,dest in [(src/'title.png','T_Title','/Game/Release/UI'),(src/'paving.png','T_Paving','/Game/Release/Environment')]:
 tex=imp(file,dest,name);tex.set_editor_property('max_texture_size',2048 if name=='T_Title' else 1024);tex.set_editor_property('lod_group',unreal.TextureGroup.TEXTUREGROUP_UI if name=='T_Title' else unreal.TextureGroup.TEXTUREGROUP_WORLD);lib.save_loaded_asset(tex)
for f in src.glob('*.wav'):
 wave=imp(f,'/Game/Release/Audio',f.stem);wave.set_editor_property('looping',f.stem=='Music_Loop');lib.save_loaded_asset(wave)
unreal.SystemLibrary.execute_console_command(None,'Interchange.FeatureFlags.Import.FBX 0')
for f in src.glob('SM_*.fbx'):
 options=unreal.FbxImportUI();options.import_mesh=True;options.import_as_skeletal=False;options.import_materials=False;options.import_textures=False;options.import_animations=False;options.static_mesh_import_data.set_editor_property('auto_generate_collision',False)
 mesh=imp(f,'/Game/Release/Environment',f.stem,options);box=mesh.get_bounds();assert 30<box.box_extent.x<100,(f,box.box_extent);unreal.log('RELEASE_IMPORTED_MESH '+f.stem+' bounds='+str(box.box_extent));lib.save_loaded_asset(mesh)
# World-space painted paving retains a simple lighting model and instanced usage.
path='/Game/Moonfall/Materials/M_ShrineStone';mat=lib.load_asset(path);assert mat;edit.delete_all_material_expressions(mat);mat.set_editor_property('used_with_instanced_static_meshes',True)
world=edit.create_material_expression(mat,unreal.MaterialExpressionWorldPosition,-800,0);uv=edit.create_material_expression(mat,unreal.MaterialExpressionCustom,-600,0);uv.set_editor_property('code','return P.xy/1800.0;');uv.set_editor_property('output_type',unreal.CustomMaterialOutputType.CMOT_FLOAT2);inp=unreal.CustomInput();inp.set_editor_property('input_name','P');uv.set_editor_property('inputs',[inp]);assert edit.connect_material_expressions(world,'',uv,'P')
sample=edit.create_material_expression(mat,unreal.MaterialExpressionTextureSample,-350,0);sample.set_editor_property('texture',lib.load_asset('/Game/Release/Environment/T_Paving'));assert edit.connect_material_expressions(uv,'',sample,'UVs');assert edit.connect_material_property(sample,'RGB',unreal.MaterialProperty.MP_BASE_COLOR)
rough=edit.create_material_expression(mat,unreal.MaterialExpressionConstant,-200,250);rough.set_editor_property('r',.95);assert edit.connect_material_property(rough,'',unreal.MaterialProperty.MP_ROUGHNESS)
edit.recompile_material(mat);lib.save_loaded_asset(mat)
unreal.log('RELEASE_ASSET_IMPORT_PASS: title, painted paving, original audio, 4 authored environment meshes')
cloud=imp(src/'clouds.png','/Game/Release/Environment','T_CloudSea');cloud.set_editor_property('max_texture_size',1024);lib.save_loaded_asset(cloud)
cloudmat=lib.load_asset('/Game/Release/Environment/M_CloudSea') if lib.does_asset_exist('/Game/Release/Environment/M_CloudSea') else tools.create_asset('M_CloudSea','/Game/Release/Environment',unreal.Material,unreal.MaterialFactoryNew());edit.delete_all_material_expressions(cloudmat);cloudmat.set_editor_property('shading_model',unreal.MaterialShadingModel.MSM_UNLIT)
cloudtex=edit.create_material_expression(cloudmat,unreal.MaterialExpressionTextureSample,-350,0);cloudtex.set_editor_property('texture',cloud)
cloudgain=edit.create_material_expression(cloudmat,unreal.MaterialExpressionMultiply,-100,0);cloudgain.set_editor_property('const_b',.45);assert edit.connect_material_expressions(cloudtex,'RGB',cloudgain,'A');assert edit.connect_material_property(cloudgain,'',unreal.MaterialProperty.MP_EMISSIVE_COLOR);edit.recompile_material(cloudmat);lib.save_loaded_asset(cloudmat)
unreal.log('RELEASE_CLOUD_IMPORT_PASS')
# Derived enemy materials preserve the supplied mesh, textures and normal maps.
for i in range(3):
 original=f'/Game/Characters/BleepFinal/Materials/M_Bleep_LOD{i}'
 target=f'/Game/Release/Environment/M_Enemy_LOD{i}'
 if lib.does_asset_exist(target):assert lib.delete_asset(target)
 assert lib.duplicate_asset(original,target)
 enemy=lib.load_asset(target)
 old=edit.get_material_property_input_node(enemy,unreal.MaterialProperty.MP_BASE_COLOR)
 outname=edit.get_material_property_input_node_output_name(enemy,unreal.MaterialProperty.MP_BASE_COLOR)
 tint=edit.create_material_expression(enemy,unreal.MaterialExpressionVectorParameter,-250,600);tint.set_editor_property('parameter_name','RoleColor');tint.set_editor_property('default_value',unreal.LinearColor(.1,.22,1,1))
 remap=edit.create_material_expression(enemy,unreal.MaterialExpressionCustom,0,600);remap.set_editor_property('code','float m=saturate((C.b-max(C.r,C.g))*3);float brightness=max(C.r,max(C.g,C.b));return lerp(C,Tint*brightness,m*.9);');remap.set_editor_property('output_type',unreal.CustomMaterialOutputType.CMOT_FLOAT3)
 inputs=[]
 for name in ['C','Tint']:
  inp=unreal.CustomInput();inp.set_editor_property('input_name',name);inputs.append(inp)
 remap.set_editor_property('inputs',inputs);assert edit.connect_material_expressions(old,outname,remap,'C');assert edit.connect_material_expressions(tint,'',remap,'Tint');assert edit.connect_material_property(remap,'',unreal.MaterialProperty.MP_BASE_COLOR);edit.recompile_material(enemy);lib.save_loaded_asset(enemy)
unreal.log('RELEASE_ENEMY_PALETTE_IMPORT_PASS')
