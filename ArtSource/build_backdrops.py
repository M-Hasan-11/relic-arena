import bpy,math,random,os
random.seed(73)
root=os.path.dirname(os.path.abspath(__file__))
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
mat=bpy.data.materials.new('BackdropStone');mat.diffuse_color=(.12,.23,.32,1)
def cone(name,loc,r1,r2,h,verts=7):
 bpy.ops.mesh.primitive_cone_add(vertices=verts,radius1=r1,radius2=r2,depth=h,location=loc)
 o=bpy.context.object;o.name=name;o.data.materials.append(mat);return o
for i in range(24):
 a=i*math.tau/24;d=random.uniform(65,100);x,y=math.cos(a)*d,math.sin(a)*d
 size=random.uniform(7,15);z=random.uniform(-9,4)
 cone('Floating crag',(x,y,z-9),0,size,25)
 cone('Mountain summit',(x,y,z+6),size*.95,0,12+random.random()*16)
 if i%3==0:
  for j in [-1,1]:cone('Distant citadel spire',(x+j*2.4,y,z+14),1.1,.7,18,8)
  bpy.ops.mesh.primitive_cube_add(size=1,location=(x,y,z+20));o=bpy.context.object;o.name='Sky bridge';o.scale=(6,1.4,1.1);o.data.materials.append(mat)
# An ancient ring above the distant skyline.
bpy.ops.mesh.primitive_torus_add(major_segments=64,minor_segments=6,location=(0,92,38),rotation=(math.pi/2,0,0),major_radius=21,minor_radius=.8)
bpy.context.object.name='Ancient celestial ring';bpy.context.object.data.materials.append(mat)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(root,'RealmBackdrops.blend'))
bpy.ops.export_scene.fbx(filepath=os.path.join(root,'..','game','Assets','Art','RealmBackdrops.fbx'),object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)
