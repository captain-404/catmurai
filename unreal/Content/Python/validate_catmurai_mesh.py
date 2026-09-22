import unreal

PATH = "/Game/Characters/Catmurai/Catmurai.Catmurai"
mesh = unreal.load_asset(PATH)
if not isinstance(mesh, unreal.StaticMesh):
    raise RuntimeError("Expected StaticMesh at " + PATH + ", got " + str(type(mesh)))

materials = mesh.get_editor_property("static_materials")
unreal.log("CATMURAI_MODEL_VALIDATED class=StaticMesh materials=" + str(len(materials)))
