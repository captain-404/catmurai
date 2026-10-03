"""Create only Moonfall-owned reusable opaque materials. Run in Unreal Python.
No source texture is modified; shader patterns are world-space and instance-safe.
"""
import unreal
TOOLS = unreal.AssetToolsHelpers.get_asset_tools()
LIB = unreal.EditorAssetLibrary
EDIT = unreal.MaterialEditingLibrary
DEST = '/Game/Moonfall/Materials'

STONE = r'''
float2 p=P.xy/145.0;
float2 cell=floor(p); float2 f=frac(p);
float nearest=10.0, second=10.0; float hue=0;
[unroll] for(int y=-1;y<=1;y++) [unroll] for(int x=-1;x<=1;x++) {
 float2 g=float2(x,y); float2 k=cell+g;
 float2 rnd=frac(sin(float2(dot(k,float2(127.1,311.7)),dot(k,float2(269.5,183.3))))*43758.5453);
 float2 offset=g+0.18+0.64*rnd-f;float d=dot(offset,offset);
 if(d<nearest){second=nearest;nearest=d;hue=rnd.x;}else second=min(second,d);
}
float seam=smoothstep(0.018,0.065,second-nearest);
float grit=frac(sin(dot(floor(P.xy*0.22),float2(12.9898,78.233)))*43758.5453);
float veins=0.96+0.04*sin(P.x*.06+sin(P.y*.035)*2);
float3 slab=lerp(float3(.115,.092,.155),float3(.235,.19,.29),hue)*(0.9+grit*.16)*veins;
return lerp(float3(.032,.024,.047),slab,seam);
'''
BASALT = r'''
float grain=sin(P.x*.035+P.z*.021)*sin(P.y*.028-P.z*.017)*.5+.5;
float strata=smoothstep(.30,.43,abs(sin(P.z*.023+sin(P.x*.008))));
return lerp(float3(.045,.039,.076),float3(.115,.095,.17),grain)*lerp(.6,1.0,strata);
'''
CHERRY = r'''
float3 q=P*.06;
float v=frac(sin(dot(floor(q),float3(12.9898,78.233,34.52)))*43758.5453);
float petal=pow(saturate(sin(q.x)*sin(q.y)*sin(q.z)),3);
return lerp(float3(.22,.028,.22),float3(.7,.19,.56),v)*(.85+petal*.35);
'''
ROOF = r'''
float tile=smoothstep(.08,.15,abs(sin(P.x*.065))*abs(sin(P.y*.065)));
return lerp(float3(.016,.023,.043),float3(.055,.067,.12),tile);
'''
WATER = r'''
float v=sin(P.z*.015+T*2.1+sin(P.x*.04)*3)*.5+.5;
float streak=pow(saturate(sin(P.x*.05+P.y*.041)),6);
return lerp(float3(.09,.03,.35),float3(.34,.16,.88),v)*(.6+streak*.7);
'''
MIST = r'''
float n=sin(P.x*.004+T*.15)*sin(P.y*.005-T*.1)*.5+.5;
return lerp(float3(.035,.018,.075),float3(.11,.045,.2),n);
'''


def make(name, color, code=None, emissive=0.0, unlit=False, wind=False):
    path = DEST + '/' + name
    mat = LIB.load_asset(path) if LIB.does_asset_exist(path) else TOOLS.create_asset(name, DEST, unreal.Material, unreal.MaterialFactoryNew())
    EDIT.delete_all_material_expressions(mat)
    mat.set_editor_property('used_with_instanced_static_meshes',True)
    assert mat.get_editor_property('used_with_instanced_static_meshes')
    mat.set_editor_property('blend_mode', unreal.BlendMode.BLEND_OPAQUE)
    mat.set_editor_property('two_sided', False)
    mat.set_editor_property('shading_model', unreal.MaterialShadingModel.MSM_UNLIT if unlit else unreal.MaterialShadingModel.MSM_DEFAULT_LIT)
    def node(kind,x,y):
        return EDIT.create_material_expression(mat,kind,x,y)
    if code:
        world = node(unreal.MaterialExpressionWorldPosition,-700,0)
        clock = node(unreal.MaterialExpressionTime,-700,180)
        out = node(unreal.MaterialExpressionCustom,-400,0)
        out.set_editor_property('code',code)
        out.set_editor_property('output_type',unreal.CustomMaterialOutputType.CMOT_FLOAT3)
        inputs=[]
        for key in ['P','T']:
            inp=unreal.CustomInput();inp.set_editor_property('input_name',key);inputs.append(inp)
        out.set_editor_property('inputs',inputs)
        assert EDIT.connect_material_expressions(world,'',out,'P')
        assert EDIT.connect_material_expressions(clock,'',out,'T')
    else:
        out=node(unreal.MaterialExpressionVectorParameter,-400,0)
        out.set_editor_property('parameter_name','Color')
        out.set_editor_property('default_value',unreal.LinearColor(*color,1))
    if not unlit:
        assert EDIT.connect_material_property(out,'',unreal.MaterialProperty.MP_BASE_COLOR)
    if emissive or unlit:
        glow=node(unreal.MaterialExpressionMultiply,-100,130)
        glow.set_editor_property('const_b',emissive or 1.0)
        assert EDIT.connect_material_expressions(out,'',glow,'A')
        assert EDIT.connect_material_property(glow,'',unreal.MaterialProperty.MP_EMISSIVE_COLOR)
    rough=node(unreal.MaterialExpressionConstant,-200,360)
    rough.set_editor_property('r',.85)
    assert EDIT.connect_material_property(rough,'',unreal.MaterialProperty.MP_ROUGHNESS)
    if wind:
        world=node(unreal.MaterialExpressionWorldPosition,-700,480)
        clock=node(unreal.MaterialExpressionTime,-700,650)
        sway=node(unreal.MaterialExpressionCustom,-400,480)
        sway.set_editor_property('code','return float3(sin(T*1.1+P.x*.007+P.y*.003)*3.0,cos(T*.8+P.y*.006)*2.0,0);')
        sway.set_editor_property('output_type',unreal.CustomMaterialOutputType.CMOT_FLOAT3)
        inputs=[]
        for key in ['P','T']:
            inp=unreal.CustomInput();inp.set_editor_property('input_name',key);inputs.append(inp)
        sway.set_editor_property('inputs',inputs)
        assert EDIT.connect_material_expressions(world,'',sway,'P')
        assert EDIT.connect_material_expressions(clock,'',sway,'T')
        assert EDIT.connect_material_property(sway,'',unreal.MaterialProperty.MP_WORLD_POSITION_OFFSET)
    if name=='M_ShrineSky':
        mat.set_editor_property('two_sided',True)
        mat.set_editor_property('is_sky',True)
    EDIT.recompile_material(mat)
    LIB.save_loaded_asset(mat)
    return mat

make('M_ShrineStone',(.22,.17,.3),STONE)
make('M_Basalt',(.08,.06,.14),BASALT)
make('M_Vermilion',(.32,.018,.032))
make('M_Roof',(.035,.04,.08),ROOF)
make('M_Bark',(.095,.037,.065))
make('M_Cherry',(.48,.1,.45),CHERRY,emissive=.025,wind=True)
make('M_MoonGlow',(.3,.045,.9),emissive=2.5,unlit=True)
make('M_LanternGlow',(.9,.035,.19),emissive=3.0,unlit=True)
make('M_SpiritWater',(.2,.08,.7),WATER,emissive=1.3,unlit=True)
make('M_Mist',(.08,.04,.15),MIST,unlit=True)
make('M_ShrineSky',(.04,.018,.09),r'''
float3 n=normalize(P);
float glow=pow(saturate(1-abs(n.z)),4);
float clouds=sin(n.x*13+n.z*7)*sin(n.y*11-n.z*5)*.5+.5;
float3 sky=lerp(float3(.015,.007,.045),float3(.10,.022,.24),glow*.7+clouds*.3);
float moon=smoothstep(.9965,.9975,dot(n,normalize(float3(-.55,.55,.65))));
float halo=pow(saturate(dot(n,normalize(float3(-.55,.55,.65)))),80);
return sky+float3(.45,.23,.8)*halo*.15+float3(.55,.35,.85)*moon;
''',unlit=True)
unreal.log('MOONFALL_MATERIALS_PASS: 11 opaque materials, Voronoi cracked paving, basalt strata, petal grain, wind, flowing spirit falls')
