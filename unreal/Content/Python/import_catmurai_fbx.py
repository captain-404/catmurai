import unreal

SOURCE = r"D:\\AI-Lab\\Apps\\api\\comfy.git\\app\\output\\3d\\catmurai_game_50k_ue.fbx"
DESTINATION = "/Game/Characters/Catmurai"

task = unreal.AssetImportTask()
task.filename = SOURCE
task.destination_path = DESTINATION
task.destination_name = "Catmurai"
task.automated = True
task.replace_existing = True
task.save = True

# Let the FBX importer preserve the export's real mesh type. The report below
# is then used to bind the correct Unreal component in the gameplay class.
options = unreal.FbxImportUI()
options.import_mesh = True
options.import_materials = True
options.import_textures = True
task.options = options

unreal.AssetToolsHelpers.get_asset_tools().import_asset_tasks([task])
paths = list(task.get_editor_property("imported_object_paths"))
unreal.log("CATMURAI_FBX_IMPORTED_PATHS=" + ";".join(paths))

registry = unreal.AssetRegistryHelpers.get_asset_registry()
for path in paths:
    asset = registry.get_asset_by_object_path(path)
    unreal.log("CATMURAI_FBX_ASSET=" + path + " CLASS=" + str(asset.asset_class_path.asset_name))

if not paths:
    unreal.log_error("CATMURAI_FBX_IMPORT_FAILED: importer returned no assets")
    raise RuntimeError("Catmurai FBX import produced no assets")
