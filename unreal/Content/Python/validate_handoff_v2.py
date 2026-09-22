import unreal
root='/Game/Characters/CatmuraiV2/'
mesh=unreal.load_asset(root+'SK_Catmurai_Drawn')
assert isinstance(mesh,unreal.SkeletalMesh)
skeleton=mesh.get_editor_property('skeleton')
assert isinstance(skeleton,unreal.Skeleton)
slots=mesh.get_editor_property('materials')
assert len(slots)==4
for slot in slots:
    assert slot.material_interface is not None
    assert '/CatmuraiV2/Materials/' in slot.material_interface.get_path_name()
    assert slot.material_interface.get_editor_property('used_with_skeletal_mesh')
    unreal.log('V2_MATERIAL '+str(slot.material_slot_name)+' '+slot.material_interface.get_path_name())
for clip in ['Idle','Run','Slash']:
    asset=unreal.load_asset(root+'A_Catmurai_'+clip)
    assert isinstance(asset,unreal.AnimSequence)
    assert asset.get_editor_property('skeleton')==skeleton
    assert asset.get_play_length()>0
    unreal.log('V2_ANIMATION '+clip+' duration='+str(asset.get_play_length()))
bounds=mesh.get_bounds()
assert 145 < bounds.box_extent.z * 2 < 155, 'Unexpected character height'
unreal.log('V2_BOUNDS '+str(bounds))
cape=unreal.load_asset(root+'Materials/M_Cape')
assert cape.get_editor_property('blend_mode')==unreal.BlendMode.BLEND_MASKED
assert cape.get_editor_property('two_sided')
orm=unreal.load_asset(root+'Textures/T_Katana_ORM')
assert not orm.get_editor_property('srgb')
unreal.log('CATMURAI_V2_ASSET_PASS: skeletal mesh, shared animations, four materials, masked cape, linear ORM')
