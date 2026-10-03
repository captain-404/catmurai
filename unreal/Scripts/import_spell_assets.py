import unreal
from pathlib import Path
import struct

root = Path(unreal.Paths.project_dir())
source = root / 'SourceAssets/Spells'
tools = unreal.AssetToolsHelpers.get_asset_tools()
lib = unreal.EditorAssetLibrary
textures = {}
keys = {}

for png in sorted(source.glob('*.png')):
    task = unreal.AssetImportTask()
    task.set_editor_property('filename', str(png))
    task.set_editor_property('destination_path', '/Game/Spells/Textures')
    task.set_editor_property('destination_name', png.stem)
    task.set_editor_property('automated', True)
    task.set_editor_property('replace_existing', True)
    task.set_editor_property('save', True)
    tools.import_asset_tasks([task])
    texture = lib.load_asset('/Game/Spells/Textures/' + png.stem)
    assert texture, png.name
    texture.set_editor_property('max_texture_size', 512)
    texture.set_editor_property('power_of_two_mode', unreal.TexturePowerOfTwoSetting.RESIZE_TO_SPECIFIC_RESOLUTION)
    texture.set_editor_property('resize_during_build_x', 512)
    texture.set_editor_property('resize_during_build_y', 512)
    texture.set_editor_property('lod_group', unreal.TextureGroup.TEXTUREGROUP_UI)
    texture.set_editor_property('compression_settings', unreal.TextureCompressionSettings.TC_DEFAULT)
    texture.set_editor_property('srgb', True)
    lib.save_loaded_asset(texture)
    textures[png.stem] = texture
    # PNG IHDR color type 2 is RGB, 6 is RGBA. Keep source art unchanged.
    keys[png.stem] = 1.0 if png.read_bytes()[25] == 2 else 0.0

assert len(textures) == 15

def make_material(name, ui=False):
    path = '/Game/Spells/' + name
    material = lib.load_asset(path) if lib.does_asset_exist(path) else None
    if material:
        unreal.MaterialEditingLibrary.delete_all_material_expressions(material)
    else:
        material = tools.create_asset(name, '/Game/Spells', unreal.Material, unreal.MaterialFactoryNew())
    material.set_editor_property('blend_mode', unreal.BlendMode.BLEND_TRANSLUCENT)
    material.set_editor_property('two_sided', True)
    if ui:
        material.set_editor_property('material_domain', unreal.MaterialDomain.MD_UI)
    else:
        material.set_editor_property('shading_model', unreal.MaterialShadingModel.MSM_UNLIT)
    editing = unreal.MaterialEditingLibrary
    def node(cls, x, y):
        return editing.create_material_expression(material, cls, x, y)
    def param(name, default, x, y):
        p = node(unreal.MaterialExpressionScalarParameter, x, y)
        p.set_editor_property('parameter_name', name)
        p.set_editor_property('default_value', default)
        return p
    texture = node(unreal.MaterialExpressionTextureSampleParameter2D, -700, 0)
    texture.set_editor_property('parameter_name', 'SpellTexture')
    texture.set_editor_property('texture', textures['spell_shadow_claw_crescent'])
    texture.set_editor_property('sampler_type', unreal.MaterialSamplerType.SAMPLERTYPE_COLOR)
    black = param('BlackKey', 0.0, -700, 380)
    fade = param('Fade', 1.0, -700, 520)
    # Soft black key: preserve bright glow and suppress RGB background squares.
    desat = node(unreal.MaterialExpressionDesaturation, -460, 180)
    assert editing.connect_material_expressions(texture, 'RGB', desat, '')
    gain = node(unreal.MaterialExpressionMultiply, -260, 180)
    gain.set_editor_property('const_b', 8.0)
    assert editing.connect_material_expressions(desat, '', gain, 'A')
    saturate = node(unreal.MaterialExpressionSaturate, -80, 180)
    assert editing.connect_material_expressions(gain, '', saturate, '')
    blend = node(unreal.MaterialExpressionLinearInterpolate, 100, 240)
    blend.set_editor_property('const_a', 1.0)
    assert editing.connect_material_expressions(saturate, '', blend, 'B')
    assert editing.connect_material_expressions(black, '', blend, 'Alpha')
    alpha = node(unreal.MaterialExpressionMultiply, 300, 180)
    assert editing.connect_material_expressions(texture, 'A', alpha, 'A')
    assert editing.connect_material_expressions(blend, '', alpha, 'B')
    opacity = node(unreal.MaterialExpressionMultiply, 500, 180)
    assert editing.connect_material_expressions(alpha, '', opacity, 'A')
    assert editing.connect_material_expressions(fade, '', opacity, 'B')
    assert editing.connect_material_property(texture, 'RGB', unreal.MaterialProperty.MP_EMISSIVE_COLOR)
    assert editing.connect_material_property(opacity, '', unreal.MaterialProperty.MP_OPACITY)
    editing.recompile_material(material)
    lib.save_loaded_asset(material)
    return material

effect = make_material('M_SpellEffect')
icon = make_material('M_SpellIcon', True)
library = lib.load_asset('/Game/Spells/DA_SpellPresentation') if lib.does_asset_exist('/Game/Spells/DA_SpellPresentation') else None
if not library:
    factory = unreal.DataAssetFactory()
    factory.set_editor_property('data_asset_class', unreal.SpellPresentation)
    library = tools.create_asset('DA_SpellPresentation', '/Game/Spells', unreal.SpellPresentation, factory)
library.set_editor_property('textures', textures)
library.set_editor_property('black_keys', keys)
library.set_editor_property('effect_material', effect)
library.set_editor_property('icon_material', icon)
lib.save_loaded_asset(library)
unreal.log('SPELL_ASSETS_PASS: 15 textures, RGB black key, alpha transparency, effect/UI materials, presentation data asset')
