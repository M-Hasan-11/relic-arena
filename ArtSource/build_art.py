"""Relic Arena original art. Run with Blender 5.1 --background --python build_art.py.
Exports FBX meshes into the adjacent Unity project; keeps editable collections in .blend.
"""
import bpy, math, random, os
from mathutils import Vector
random.seed(27)
ROOT = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(ROOT, '..', 'game', 'Assets', 'Art')
os.makedirs(OUT, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

palette = {
 'Basalt': (.065,.10,.14,1), 'Slate':(.16,.23,.28,1),
 'Stone':(.30,.38,.40,1), 'Sandstone':(.57,.52,.40,1),
 'Gold':(.75,.46,.16,1), 'DarkGold':(.31,.21,.10,1),
 'Teal':(.025,.40,.43,1), 'Cloth':(.035,.16,.20,1),
 'Glow':(.15,.95,.83,1), 'Red':(.70,.12,.16,1),
 'RedGlow':(1,.16,.09,1), 'Obsidian':(.10,.045,.075,1),
 'Steel':(.53,.70,.74,1)
}
mats={}
for name,color in palette.items():
 m=bpy.data.materials.new(name); m.diffuse_color=color; m.use_nodes=True
 bs=m.node_tree.nodes.get('Principled BSDF'); bs.inputs['Base Color'].default_value=color
 bs.inputs['Metallic'].default_value=.65 if name in ['Gold','DarkGold','Steel'] else .1
 bs.inputs['Roughness'].default_value=.35 if name in ['Gold','Steel'] else .8
 if 'Glow' in name:
  bs.inputs['Emission Color'].default_value=color; bs.inputs['Emission Strength'].default_value=3
 mats[name]=m

def empty(name,loc=(0,0,0),parent=None):
 o=bpy.data.objects.new(name,None); bpy.context.collection.objects.link(o); o.location=loc; o.parent=parent; return o
def finish(o,name,mat,parent):
 o.name=name; o.data.materials.append(mats[mat]); o.parent=parent; return o
def box(name,loc,scale,mat,parent=None,bevel=.04):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc); o=bpy.context.object
 o.scale=scale; bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('Cut stone edges','BEVEL'); mod.width=bevel; mod.segments=1
  bpy.ops.object.modifier_apply(modifier=mod.name)
 return finish(o,name,mat,parent)
def cyl(name,loc,r,depth,mat,parent=None,vertices=12,r2=None):
 bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r,radius2=r if r2 is None else r2,depth=depth,location=loc)
 return finish(bpy.context.object,name,mat,parent)
def ico(name,loc,scale,mat,parent=None):
 bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=1,location=loc); o=bpy.context.object; o.scale=scale
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True); return finish(o,name,mat,parent)
def ring(name,r,z,width,mat,parent,n=96):
 for i in range(n):
  a=2*math.pi*i/n
  o=box(name,(math.cos(a)*r,math.sin(a)*r,z),(width,2*math.pi*r/n*.92,.035),mat,parent,.008); o.rotation_euler.z=a
def export(root,filename):
 bpy.ops.object.select_all(action='DESELECT'); root.select_set(True)
 for o in root.children_recursive:o.select_set(True)
 bpy.context.view_layer.objects.active=root
 bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,filename+'.fbx'),use_selection=True,object_types={'EMPTY','MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False)

arena=empty('TempleArena')
cyl('Foundation',(0,0,-1.45),19,2.4,'Basalt',arena,64,r2=18.2)
cyl('GoldLip',(0,0,-.34),18.35,.18,'DarkGold',arena,64)
cyl('ArenaFloor',(0,0,-.17),18,.34,'Slate',arena,64)
cyl('CentralMedallion',(0,0,.018),3.6,.04,'Basalt',arena,12)
ring('InnerInlay',3.5,.05,.07,'Gold',arena,64)
ring('ArenaRunes',16.9,.025,.12,'Glow',arena,96)
ring('OuterInlay',17.5,.025,.10,'Gold',arena,96)
ring('MiddleInlay',9.5,.025,.035,'Stone',arena,96)
for i in range(12):
 a=i*math.tau/12
 o=box('RadialSeam',(math.cos(a)*10.4,math.sin(a)*10.4,.013),(12,.025,.018),'Basalt',arena,0);o.rotation_euler.z=a
 for r in [4.3,4.6,4.9]:
  o=box('Sigil',(math.cos(a)*r,math.sin(a)*r,.043),(.12,.38,.04),'Gold',arena,.01);o.rotation_euler.z=a
for i in range(12):
 a=i*math.tau/12; x,y=19.0*math.cos(a),19.0*math.sin(a)
 p=empty('Pillar_%02d'%i,(x,y,0),arena)
 box('Plinth',(0,0,.18),(2.2,2.2,.36),'Sandstone',p)
 box('Foot',(0,0,.6),(1.5,1.5,.5),'Basalt',p)
 height=4.5 if i%3 else 6.8
 cyl('FlutedPillar',(0,0,height/2+.8),.66,height,'Stone',p,6,r2=.45)
 cyl('PillarBand',(0,0,height*.72),.73,.20,'Gold',p,6)
 cyl('Capital',(0,0,height+.9),.95,.35,'Sandstone',p,6)
 ico('Beacon',(0,0,height+1.65),(.4,.4,.8),'Glow',p)
 box('LightSlit',(.0,-.59,2.1),(.13,.08,1.8),'Glow',p)
 p.rotation_euler.z=a+math.pi/2
 # Broken parapet blocks outside the combat circle.
 for j in range(4):
  aa=a+(j+1)*math.tau/60
  o=box('Battlement',(19*math.cos(aa),19*math.sin(aa),.42),(1.65,.9,.85),'Basalt',arena);o.rotation_euler.z=aa+math.pi/2
 # Floating rocks below the island.
 ico('Understone',(x*.75,y*.75,-2.7-random.random()*2),(2.5,2.7,3.2),'Basalt',arena)
for i in range(4):
 a=math.pi/4+i*math.pi/2
 p=empty('DistantArch', (25*math.cos(a),25*math.sin(a),-1),arena);p.rotation_euler.z=a+math.pi/2
 for x in [-2.8,2.8]:
  box('ArchLeg',(x,0,4),(1.2,1.6,8),'Basalt',p)
  box('ArchTrim',(x,-.85,5),(.18,.08,5),'Gold',p)
 box('Lintel',(0,0,8),(7,1.9,1.2),'Stone',p)
 ico('ArchKeystone',(0,-1.0,8),(.6,.35,.8),'Glow',p)
export(arena,'TempleArena')

def guardian(name,enemy=False):
 root=empty(name)
 bodymat='Obsidian' if enemy else 'Teal'; accent='RedGlow' if enemy else 'Glow'; trim='Red' if enemy else 'Gold'
 body=empty('Torso',(0,0,1.08),root)
 box('Waist',(0,0,.08),(.46,.32,.24),'Basalt',body)
 ico('Breastplate',(0,0,.43),(.45,.29,.48),bodymat,body)
 box('ChestBand',(0,-.265,.43),(.58,.055,.12),trim,body)
 ico('Heart',(0,-.31,.47),(.11,.07,.16),accent,body)
 for x in [-.22,.22]:
  o=box('Tasset',(x,0,-.12),(.22,.35,.38),bodymat,body);o.rotation_euler.y=-x
 head=empty('Head',(0,0,1.93),root)
 ico('Helm',(0,0,0),(.27,.24,.30),'Basalt' if enemy else 'Steel',head)
 box('Visor',(0,-.221,.01),(.35,.055,.07),accent,head)
 box('Crest',(0,.02,.27),(.095,.33,.24),trim,head)
 if enemy:
  for x in [-.22,.22]:
   o=cyl('Horn',(x,0,.23),.1,.48,trim,head,4,r2=0);o.rotation_euler.y=x*2
 for side,x in [('L',-.48),('R',.48)]:
  arm=empty('Arm'+side,(x,0,1.62),root)
  ico('Pauldron',(0,0,0),(.29,.30,.27),trim,arm)
  box('UpperArm',(0,0,-.25),(.20,.24,.38),bodymat,arm)
  box('Gauntlet',(0,-.045,-.53),(.24,.28,.26),'Basalt',arm)
  box('Fist',(0,-.05,-.7),(.18,.20,.15),'Steel',arm)
  leg=empty('Leg'+side,(x*.43,0,1.03),root)
  box('Thigh',(0,0,-.22),(.23,.27,.40),bodymat,leg)
  ico('Knee',(0,-.13,-.46),(.16,.12,.15),trim,leg)
  box('Shin',(0,0,-.65),(.24,.26,.37),'Basalt',leg)
  box('Boot',(0,-.10,-.91),(.28,.46,.18),'Basalt',leg)
  if side=='R':
   sword=empty('Weapon',(0,-.1,-.68),arm)
   box('Handle',(0,-.14,0),(.075,.34,.075),'DarkGold',sword)
   box('Crossguard',(0,-.34,0),(.48,.08,.10),trim,sword)
   blade=box('Blade',(0,-.88,0),(.19,1.0,.075),'Steel',sword)
   box('BladeRune',(0,-.88,.046),(.065,.91,.022),accent,sword,.005)
   ico('BladeTip',(0,-1.44,0),(.12,.22,.04),'Steel',sword)
 if not enemy:
  # Angular half-cape, facing backward (+Y).
  verts=[(-.28,.22,1.67),(.28,.22,1.67),(.40,.43,.65),(-.40,.43,.65)]
  mesh=bpy.data.meshes.new('CapeMesh');mesh.from_pydata(verts,[],[(0,1,2,3),(3,2,1,0)]);mesh.update()
  ob=bpy.data.objects.new('Cape',mesh);bpy.context.collection.objects.link(ob);ob.parent=root;ob.data.materials.append(mats['Cloth'])
 return root
hero=guardian('Guardian');export(hero,'Guardian')
enemy=guardian('Sentinel',True);export(enemy,'Sentinel')
# Keep asset groups apart in the editable source scene after export.
hero.location=(23,0,1);enemy.location=(26,0,1)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT,'RelicArena.blend'))
print('RELIC_ART_COMPLETE')
