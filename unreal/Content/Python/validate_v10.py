import unreal
root='/Game/Characters/CatmuraiV10/'
mesh=unreal.load_asset(root+'SK_Catmurai_v10')
clips=[]
for name,frames in [('Idle',73),('Run',25),('Slash',33),('JumpStart',9),('JumpRise',13),('JumpFall',17),('JumpLand',17),('JumpFlip',19),('Dash',15)]:
    clip=unreal.load_asset(root+'A_Catmurai_'+name)
    assert clip and clip.get_editor_property('skeleton')==mesh.get_editor_property('skeleton')
    assert abs(clip.get_play_length()-(frames-1)/30)<0.04, (name,clip.get_play_length())
    assert not clip.get_editor_property('enable_root_motion')
    unreal.log('V10_CLIP '+name+' duration='+str(clip.get_play_length()))
    clips.append(clip)
assert unreal.V10AssetTools.validate_assets(mesh,clips)
normal=unreal.load_asset(root+'Textures/T_Body_Normal')
assert normal.get_editor_property('flip_green_channel')
assert not normal.get_editor_property('srgb')
assert normal.get_editor_property('compression_settings')==unreal.TextureCompressionSettings.TC_NORMALMAP
cape=unreal.load_asset(root+'Materials/M_Cape')
assert cape.get_editor_property('two_sided')
assert cape.get_editor_property('blend_mode')==unreal.BlendMode.BLEND_MASKED
assert abs(cape.get_editor_property('opacity_mask_clip_value')-.5)<.001
for slot in mesh.get_editor_property('materials'):
    mat=slot.material_interface
    assert mat and mat.get_editor_property('used_with_skeletal_mesh') and mat.get_editor_property('used_with_morph_targets')
unreal.log('CATMURAI_V10_REOPEN_PASS: nine clips, four LODs, morphs, bind grip, body scale, normal map and materials')
