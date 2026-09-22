import unreal
asset = unreal.EditorAssetLibrary.load_asset('/Game/Materials/M_Prototype')
if not asset:
    asset = unreal.AssetToolsHelpers.get_asset_tools().create_asset('M_Prototype', '/Game/Materials', unreal.Material, unreal.MaterialFactoryNew())
    color = unreal.MaterialEditingLibrary.create_material_expression(asset, unreal.MaterialExpressionVectorParameter, -300, 0)
    color.set_editor_property('parameter_name', 'Color')
    color.set_editor_property('default_value', unreal.LinearColor(0.5, 0.3, 0.7, 1.0))
    unreal.MaterialEditingLibrary.connect_material_property(color, '', unreal.MaterialProperty.MP_BASE_COLOR)
    unreal.MaterialEditingLibrary.recompile_material(asset)
    unreal.EditorAssetLibrary.save_loaded_asset(asset)
unreal.log('CATMURAI_MATERIAL_READY')
