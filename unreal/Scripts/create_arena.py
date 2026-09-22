import unreal
world = unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
world.new_level('/Game/Maps/SurvivalArena')
world.save_current_level()
unreal.log('CATMURAI_ARENA_SAVED')
