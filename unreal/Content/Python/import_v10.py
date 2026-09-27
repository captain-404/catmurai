import unreal
from pathlib import Path
unreal.SystemLibrary.execute_console_command(None, 'Interchange.FeatureFlags.Import.FBX 0')

ROOT = Path(unreal.Paths.project_dir()) / 'SourceAssets/CatmuraiV10'
DEST = '/Game/Characters/CatmuraiV10'
tools = unreal.AssetToolsHelpers.get_asset_tools()

def import_fbx(relative, name, skeleton=None, animation=False):
    # Rerunning the setup preserves verified assets; explicit reimports need fresh settings.
    existing = unreal.load_asset(DEST + '/' + name) if unreal.EditorAssetLibrary.does_asset_exist(DEST + '/' + name) else None
    if existing:
        return [existing]
    options = unreal.FbxImportUI()
    options.automated_import_should_detect_type = False
    options.import_as_skeletal = True
    options.import_mesh = not animation
    options.import_animations = animation
    options.import_materials = False
    options.import_textures = False
    options.mesh_type_to_import = unreal.FBXImportType.FBXIT_ANIMATION if animation else unreal.FBXImportType.FBXIT_SKELETAL_MESH
    options.skeletal_mesh_import_data.set_editor_property('import_morph_targets', True)
    options.skeletal_mesh_import_data.normal_import_method = unreal.FBXNormalImportMethod.FBXNIM_IMPORT_NORMALS
    options.anim_sequence_import_data.set_editor_property('use_default_sample_rate', True)
    if skeleton:
        options.skeleton = skeleton
    data = options.anim_sequence_import_data if animation else options.skeletal_mesh_import_data
    # These FBX files contain meter-sized coordinates tagged as centimeters.
    # Verified raw height is 0.977 Unreal units; 153 gives a 149.5 cm character.
    data.import_uniform_scale = 153.0
    task = unreal.AssetImportTask()
    task.filename = str(ROOT / relative)
    task.destination_path = DEST
    task.destination_name = name
    task.automated = True
    task.replace_existing = True
    task.save = True
    task.options = options
    task.factory = unreal.FbxFactory()
    tools.import_asset_tasks([task])
    assets = [unreal.load_asset(p) for p in task.imported_object_paths]
    unreal.log('HANDOFF_IMPORTED ' + str([(a.get_path_name(), a.get_class().get_name()) for a in assets]))
    assert assets, 'No assets imported for ' + relative
    return assets

drawn = next(a for a in import_fbx('model/Catmurai_v10_Drawn_UE.fbx', 'SK_Catmurai_v10') if isinstance(a, unreal.SkeletalMesh))
skeleton = drawn.get_editor_property('skeleton')
assert skeleton
for clip in ['Idle', 'Run', 'Slash', 'JumpStart', 'JumpRise', 'JumpFall', 'JumpLand', 'JumpFlip', 'Dash']:
    assets = import_fbx('animations/Anim_' + clip + '_ue.fbx', 'A_Catmurai_' + clip, skeleton, True)
    for asset in assets:
        if isinstance(asset, unreal.AnimSequence):
            asset.set_editor_property('enable_root_motion', False)
            unreal.EditorAssetLibrary.save_loaded_asset(asset, False)

def texture(relative, name, linear=False):
    task = unreal.AssetImportTask()
    task.filename = str(ROOT / relative)
    task.destination_path = DEST + '/Textures'
    task.destination_name = name
    task.automated = True
    task.replace_existing = True
    task.save = True
    tools.import_asset_tasks([task])
    tex = unreal.load_asset(task.imported_object_paths[0])
    if linear:
        tex.set_editor_property('srgb', False)
        tex.set_editor_property('compression_settings', unreal.TextureCompressionSettings.TC_MASKS)
    unreal.EditorAssetLibrary.save_loaded_asset(tex, False)
    return tex

body = texture('textures/T_Catmurai_Body_BaseColor_4096_v10.jpg', 'T_Body')
cape = texture('textures/T_Catmurai_Cape_BaseColor_RGBA.png', 'T_Cape')
tail = texture('textures/T_Catmurai_Tail_BaseColor.png', 'T_Tail')
blade = texture('textures/T_Katana_BaseColor.png', 'T_Katana')
orm = unreal.load_asset('/Game/Characters/CatmuraiV2/Textures/T_Katana_ORM')
assert orm
normal = texture('textures/T_Catmurai_Body_Normal_4096_v10_5.png', 'T_Body_Normal')
normal.set_editor_property('compression_settings', unreal.TextureCompressionSettings.TC_NORMALMAP)
normal.set_editor_property('srgb', False)
normal.set_editor_property('flip_green_channel', True)
unreal.EditorAssetLibrary.save_loaded_asset(normal, False)
emissive = texture('textures/T_Katana_Emissive.png', 'T_Katana_Emissive')

def material(name, tex, masked=False, metal=False):
    mat = unreal.load_asset(DEST + '/Materials/' + name) if unreal.EditorAssetLibrary.does_asset_exist(DEST + '/Materials/' + name) else tools.create_asset(name, DEST + '/Materials', unreal.Material, unreal.MaterialFactoryNew())
    if mat and mat.get_editor_property('used_with_morph_targets'):
        return mat
    assert mat
    mat.set_editor_property('used_with_skeletal_mesh', True)
    mat.set_editor_property('used_with_morph_targets', True)
    ed = unreal.MaterialEditingLibrary
    ed.delete_all_material_expressions(mat)
    def sample(t, x=0, y=0):
        node = ed.create_material_expression(mat, unreal.MaterialExpressionTextureSample, x, y)
        node.texture = t
        if t == orm:
            node.sampler_type = unreal.MaterialSamplerType.SAMPLERTYPE_MASKS
        return node
    color = sample(tex)
    ed.connect_material_property(color, 'RGB', unreal.MaterialProperty.MP_BASE_COLOR)
    if name == 'M_Body':
        n = sample(normal, 0, 750)
        n.sampler_type = unreal.MaterialSamplerType.SAMPLERTYPE_NORMAL
        ed.connect_material_property(n, 'RGB', unreal.MaterialProperty.MP_NORMAL)
    if masked:
        mat.set_editor_property('blend_mode', unreal.BlendMode.BLEND_MASKED)
        mat.set_editor_property('two_sided', True)
        mat.set_editor_property('opacity_mask_clip_value', 0.5)
        ed.connect_material_property(color, 'A', unreal.MaterialProperty.MP_OPACITY_MASK)
    if metal:
        packed = sample(orm, 0, 250)
        for channel, prop in [('R', unreal.MaterialProperty.MP_AMBIENT_OCCLUSION), ('G', unreal.MaterialProperty.MP_ROUGHNESS), ('B', unreal.MaterialProperty.MP_METALLIC)]:
            ed.connect_material_property(packed, channel, prop)
        glow = sample(emissive, 0, 500)
        ed.connect_material_property(glow, 'RGB', unreal.MaterialProperty.MP_EMISSIVE_COLOR)
    else:
        rough = ed.create_material_expression(mat, unreal.MaterialExpressionConstant, 0, 250)
        rough.set_editor_property('r', 0.75)
        ed.connect_material_property(rough, '', unreal.MaterialProperty.MP_ROUGHNESS)
    ed.recompile_material(mat)
    unreal.EditorAssetLibrary.save_loaded_asset(mat, False)
    return mat

materials = {'Catmurai_Game': material('M_Body', body), 'Material_1': material('M_Cape', cape, True), 'Material_2': material('M_Tail', tail)}
katana = material('M_Katana', blade, metal=True)
for mesh in [drawn]:
    slots = mesh.get_editor_property('materials')
    for i in range(len(slots)):
        slot = slots[i]
        name = str(slot.get_editor_property('imported_material_slot_name'))
        unreal.log('HANDOFF_MATERIAL_SLOT ' + mesh.get_name() + ' ' + name)
        key = next((key for key in materials if name == key or name.startswith(key + '_')), None)
        assert key or 'katana' in name.lower(), 'Unrecognized material slot: ' + name
        slot.set_editor_property('material_interface', materials[key] if key else katana)
        slots[i] = slot
    mesh.set_editor_property('materials', slots)
    unreal.EditorAssetLibrary.save_loaded_asset(mesh, False)
unreal.EditorAssetLibrary.save_directory(DEST, False, True)
# LOD FBXs contain the supplied lower-detail body and the original skinned sword.
sub = unreal.get_editor_subsystem(unreal.SkeletalMeshEditorSubsystem)
for i in range(1, 4):
    result = sub.import_lod(drawn, i, str(ROOT / 'lods' / ('Catmurai_LOD%d.fbx' % i)))
    assert result == i, 'LOD import failed: ' + str(i)
import runpy
runpy.run_path(str(Path(unreal.Paths.project_dir()) / 'Content/Python/repair_v10_materials.py'))
assert unreal.V10AssetTools.configure_mesh(drawn)
clips = [unreal.load_asset(DEST + '/A_Catmurai_' + c) for c in ['Idle','Run','Slash','JumpStart','JumpRise','JumpFall','JumpLand','JumpFlip','Dash']]
for clip in clips:
    assert unreal.V10AssetTools.lock_grip(clip, drawn)
assert unreal.V10AssetTools.validate_assets(drawn, clips)
unreal.EditorAssetLibrary.save_directory(DEST, False, True)
unreal.log('CATMURAI_V10_IMPORT_PASS')
