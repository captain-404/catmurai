import unreal
root='/Game/Characters/CatmuraiV10/'
mesh=unreal.load_asset(root+'SK_Catmurai_v10')
materials={'Catmurai_Game':'M_Body','Material_1':'M_Cape','Material_2':'M_Tail','Catmurai_Katana':'M_Katana'}
slots=mesh.get_editor_property('materials')
for i,slot in enumerate(slots):
    name=str(slot.get_editor_property('imported_material_slot_name'))
    key=next((key for key in materials if name==key or name.startswith(key+'_')),None)
    assert key, name
    slot.set_editor_property('material_interface',unreal.load_asset(root+'Materials/'+materials[key]))
    slots[i]=slot
mesh.set_editor_property('materials',slots)
unreal.EditorAssetLibrary.save_loaded_asset(mesh,False)
unreal.log('V10_MATERIAL_REMAP_PASS')
