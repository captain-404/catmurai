import bpy,math,random
from mathutils import Vector
from pathlib import Path
out=Path(r'D:\YoutubeChannel\catmurai-unreal\SourceAssets\Release');out.mkdir(exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system='METRIC';bpy.context.scene.unit_settings.scale_length=1
rng=random.Random(404)
mat=bpy.data.materials.new('Surface');mat.diffuse_color=(.2,.13,.26,1)
def export(o,name):
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;o.name=name
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 o.data.materials.clear();o.data.materials.append(mat)
 bpy.ops.object.modifier_add(type='TRIANGULATE');bpy.ops.object.modifier_apply(modifier=o.modifiers[-1].name)
 for p in o.data.polygons:p.use_smooth=False
 assert len(o.data.polygons)<4000,name
 bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,object_types={'MESH'},axis_forward='-Y',axis_up='Z',apply_unit_scale=True,bake_anim=False,add_leaf_bones=False)
 print('RELEASE_MESH',name,len(o.data.vertices),len(o.data.polygons),tuple(round(v,3) for v in o.dimensions))
 return o
# Irregular tapered basalt spire: varied cross-sections remove basic-cone silhouettes.
verts=[];faces=[];n=7
for k,(z,r) in enumerate([(-.5,.12),(-.34,.34),(-.12,.46),(.17,.39),(.48,.32)]):
 for i in range(n):
  a=i*2*math.pi/n+.07*k;rr=r*rng.uniform(.87,1.13);verts.append((math.cos(a)*rr,math.sin(a)*rr,z+rng.uniform(-.025,.025)))
for k in range(4):
 for i in range(n):a=k*n+i;b=k*n+(i+1)%n;c=(k+1)*n+(i+1)%n;d=(k+1)*n+i;faces.append((a,b,c,d))
faces.extend([tuple(range(n-1,-1,-1)),tuple(4*n+i for i in range(n))])
m=bpy.data.meshes.new('Basalt');m.from_pydata(verts,[],faces);m.update();o=bpy.data.objects.new('Shard',m);bpy.context.collection.objects.link(o);export(o,'SM_BasaltShard')
bpy.ops.mesh.primitive_cube_add(size=1);o=bpy.context.object;o.name='Block';bpy.ops.object.modifier_add(type='BEVEL');o.modifiers[-1].width=.065;o.modifiers[-1].segments=2;bpy.ops.object.modifier_apply(modifier=o.modifiers[-1].name)
for v in o.data.vertices:v.co.x+=.018*math.sin(v.co.z*18+v.co.y*9);v.co.y+=.014*math.sin(v.co.x*13-v.co.z*8)
export(o,'SM_BasaltBlock')
# Curved roof tile with folded eaves and shallow ridges, still a closed mesh.
bpy.ops.mesh.primitive_cube_add(size=1);o=bpy.context.object
bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.subdivide(number_cuts=8);bpy.ops.object.mode_set(mode='OBJECT')
for v in o.data.vertices:
 x,y,z=v.co;v.co.z=z*.5+.35*x*x+.055*math.cos(y*math.pi*12)*(z+.5)
export(o,'SM_CurvedRoof')
# Jagged blossom clusters: irregular facets and small protruding petals, no smooth blob.
parts=[]
for i in range(13):
 a=i*2.399;r=.26*math.sqrt(i/12);z=rng.uniform(-.09,.11)
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=rng.uniform(.105,.18),location=(math.cos(a)*r,math.sin(a)*r,z))
 o=bpy.context.object;o.scale=(1,1,.48);parts.append(o)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();o=bpy.context.object
bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY',center='BOUNDS');o.location=(0,0,0);o.dimensions=(1,1,.5);export(o,'SM_CherryCluster')
bpy.ops.wm.save_as_mainfile(filepath=str(out/'ReleaseEnvironment.blend'))
print('RELEASE_ENVIRONMENT_AUTHORING_PASS')
