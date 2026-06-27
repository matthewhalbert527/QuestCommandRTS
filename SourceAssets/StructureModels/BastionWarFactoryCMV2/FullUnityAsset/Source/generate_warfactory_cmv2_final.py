from __future__ import annotations
import importlib.util, sys, math, json, shutil, zipfile, textwrap, os
from pathlib import Path
from dataclasses import dataclass
from typing import Iterable, Sequence, Dict, List, Tuple
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import trimesh
from trimesh.transformations import rotation_matrix

# Load the approved blockout generator as the base construction grammar.
spec = importlib.util.spec_from_file_location('cmv2_blockout', '/mnt/data/create_warfactory_cmv2_blockout.py')
c = importlib.util.module_from_spec(spec)
sys.modules['cmv2_blockout'] = c
spec.loader.exec_module(c)

OUT = Path('/mnt/data/Bastion_WarFactoryCMV2_Unity_Package')
ASSET_ROOT = OUT / 'Assets' / 'BastionWarFactoryCMV2'
MESH_DIR = ASSET_ROOT / 'Meshes'
TEXTURE_DIR = ASSET_ROOT / 'Textures'
MATERIAL_DIR = ASSET_ROOT / 'Materials'
SCRIPT_DIR = ASSET_ROOT / 'Scripts'
EDITOR_DIR = ASSET_ROOT / 'Editor'
PREFAB_DIR = ASSET_ROOT / 'Prefabs'
PREVIEW_DIR = OUT / 'Preview'
SPEC_DIR = OUT / 'Spec'
SOURCE_DIR = OUT / 'Source'
QUICK_ROOT = Path('/mnt/data/Bastion_WarFactoryCMV2_QuickImport')
QUICK_MESH_DIR = QUICK_ROOT / 'Meshes'
QUICK_TEXTURE_DIR = QUICK_ROOT / 'Textures'

ASSET_SLUG = 'Bastion_WarFactoryCMV2'
ASSET_NAME = 'Bastion War Factory Concept-Match V2'
MTL_NAME = f'{ASSET_SLUG}.mtl'

# Rendering / atlas palette. This is intentionally close to the concept: olive, black metal,
# white team panels, cyan optics, orange industrial glow.
PALETTE: Dict[str, Tuple[int,int,int,int]] = {
    'Armor': (86, 96, 70, 255),
    'ArmorLight': (126, 133, 92, 255),
    'DarkArmor': (38, 43, 40, 255),
    'Deck': (83, 84, 73, 255),
    'Metal': (49, 53, 51, 255),
    'Black': (15, 18, 19, 255),
    'TeamColor': (246, 246, 236, 255),
    'TeamColorShadow': (208, 210, 200, 255),
    'OrangeEmit': (255, 111, 24, 255),
    'CyanEmit': (34, 206, 232, 255),
    'Glass': (31, 93, 108, 255),
    'Hazard': (238, 170, 38, 255),
    'Smoke': (148, 152, 148, 180),
}
PALETTE_ORDER = list(PALETTE.keys())
SWATCH = 64
TEXTURE_H = 64

# Rebind blockout palette for rendering/export convenience.
c.PALETTE.update(PALETTE)

@dataclass
class Shape:
    mesh: trimesh.Trimesh
    material: str
    name: str = ''

# Alias helpers from blockout module to keep the construction style consistent.
shape = c.shape
box = c.box
cylinder = c.cylinder
cyl_between = c.cyl_between
ramp = c.ramp
gable_roof = c.gable_roof
moved = c.moved
armor_panel = c.armor_panel
fan_unit = c.fan_unit
robot_arm = c.robot_arm
antenna = c.antenna
louver = c.louver
clean = c.clean


def reset_dirs() -> None:
    for p in [OUT, QUICK_ROOT]:
        if p.exists(): shutil.rmtree(p)
    for p in [MESH_DIR, TEXTURE_DIR, MATERIAL_DIR, SCRIPT_DIR, EDITOR_DIR, PREFAB_DIR, PREVIEW_DIR, SPEC_DIR, SOURCE_DIR, QUICK_MESH_DIR, QUICK_TEXTURE_DIR]:
        p.mkdir(parents=True, exist_ok=True)


def add_refined_team_panels(groups: Dict[str,List[c.Shape]]) -> Dict[str,List[c.Shape]]:
    T = groups['TeamColorPanels']; B = groups['BaseArmor']; E = groups['Emissive']
    def front(name,x,y,z,w,h):
        T.append(shape(box([w,h,0.07], (x,y,z)), 'TeamColor', name))
        B.append(shape(box([w+0.10,h+0.10,0.045], (x,y,z+0.035)), 'DarkArmor', name+'_backing'))
    front('v2_front_left_white_shoulder', -3.18,3.53,-4.97,1.55,1.35)
    front('v2_front_right_white_shoulder', 3.18,3.53,-4.97,1.55,1.35)
    front('v2_front_center_emblem_plate', 0.0,3.80,-4.99,2.0,0.72)
    front('v2_front_left_lower_white_panel', -5.58,1.95,-4.98,0.58,1.55)
    front('v2_front_right_lower_white_panel', 5.58,1.95,-4.98,0.58,1.55)
    front('v2_front_left_annex_white_band', -6.35,1.92,-6.02,1.35,0.55)
    front('v2_front_right_annex_white_band', 6.25,1.94,-6.03,1.35,0.55)
    def side(name,x,y,z,h,d):
        T.append(shape(box([0.07,h,d], (x,y,z)), 'TeamColor', name))
        B.append(shape(box([0.045,h+0.10,d+0.10], (x-np.sign(x)*0.035,y,z)), 'DarkArmor', name+'_backing'))
    side('v2_right_side_long_mid_plate', 7.88,2.05,0.20,0.85,2.95)
    side('v2_right_side_rear_angle_plate', 7.88,1.65,3.10,0.70,1.55)
    side('v2_left_side_long_mid_plate', -7.88,2.05,0.15,0.85,2.95)
    side('v2_left_side_rear_angle_plate', -7.88,1.65,3.10,0.70,1.55)
    for x in [-4.15,4.15]:
        T.append(shape(box([0.62,0.06,5.75], (x,5.44,0.05)), 'TeamColor','v2_roof_long_white_spine'))
        B.append(shape(box([0.72,0.04,5.9], (x,5.40,0.05)), 'DarkArmor','v2_roof_team_plate_backing'))
    for x,z,w,d in [(-2.8,2.25,1.15,0.65),(2.8,2.25,1.15,0.65),(-2.9,-2.70,1.05,0.55),(2.9,-2.70,1.05,0.55),(0,-3.35,1.2,0.38)]:
        T.append(shape(box([w,0.055,d], (x,5.56,z)), 'TeamColor','v2_roof_small_white_panel'))
    for x,z in [(6.95,4.98),(5.65,5.35),(7.85,2.85)]:
        T.append(shape(box([0.72,0.72,0.06], (x,3.35,z-0.50)), 'TeamColor','v2_stack_visible_team_plaque'))
    for x in [-2.4,2.4,0.0]:
        T.append(shape(box([0.38,0.05,1.15], (x,0.91,-5.58)), 'TeamColor','v2_ramp_lane_white'))
    for x in [-3.0,0,3.0]:
        E.append(shape(box([0.46,0.08,0.10], (x,3.35,-4.99)), 'CyanEmit','v2_bay_extra_cyan_spot'))
    for x,z in [(-6.9,0.6),(6.9,0.6),(-6.9,2.4),(6.9,2.4),(6.9,4.4)]:
        E.append(shape(box([0.08,1.0,0.08], (x,1.6,z)), 'OrangeEmit','v2_side_vertical_orange_strip'))
    return groups


def add_final_detail_pass(groups: Dict[str,List[c.Shape]]) -> Dict[str,List[c.Shape]]:
    """Add modeled detail to move the asset from blockout to concept-match game geometry."""
    B = groups['BaseArmor']; T = groups['TeamColorPanels']; E = groups['Emissive']; G = groups['GlassLights']
    # Main front/fascia panel plates and bolt rows.
    for z in [-4.99]:
        for x in np.linspace(-4.7, 4.7, 12):
            for y in [0.95, 1.55, 2.25, 2.95, 3.70]:
                B.append(shape(box([0.055,0.055,0.045], (x,y,z-0.06)), 'Metal', 'final_front_bolt'))
        for x in [-4.86,-4.25,4.25,4.86]:
            B.append(shape(box([0.08,3.45,0.055], (x,2.55,z-0.05)), 'DarkArmor','final_front_vertical_panel_seam'))
        for y in [1.1,2.05,3.0,3.95]:
            B.append(shape(box([8.75,0.055,0.055], (0,y,z-0.055)), 'DarkArmor','final_front_horizontal_panel_seam'))

    # Ramp: double track strips, hatch panels, hazard bands.
    for x in [-3.25, 3.25]:
        B.append(shape(box([0.18,0.12,3.35], (x,0.83,-6.15)), 'Metal', 'final_ramp_outer_rail'))
        B.append(shape(box([0.10,0.08,3.05], (x*0.82,0.90,-6.05)), 'Black', 'final_ramp_inner_track_slot'))
    for z in np.linspace(-7.35,-4.75,9):
        B.append(shape(box([6.9,0.035,0.045], (0,0.95,z)), 'DarkArmor', 'final_ramp_panel_line'))
        for x in [-3.7,3.7]:
            E.append(shape(box([0.28,0.045,0.16], (x,0.99,z)), 'OrangeEmit', 'final_ramp_orange_marker'))
    for x,z in [(-1.75,-6.95),(-0.58,-6.35),(0.58,-5.75),(1.75,-5.15)]:
        B.append(shape(box([0.45,0.04,0.24], (x,1.0,z), (0,30,0)), 'Hazard', 'final_hazard_tick'))

    # Side annex plate seams, windows, vents, extra white team bands.
    for side_x in [-7.88,7.88]:
        # vertical side face details; x-oriented thin plates.
        for z in np.linspace(-2.85, 4.75, 8):
            B.append(shape(box([0.05,0.05,0.74], (side_x,2.55,z)), 'DarkArmor', 'final_side_upper_seam'))
        for y,z in [(1.08,-3.3),(1.45,-1.4),(1.45,0.8),(1.22,2.75),(2.35,3.95)]:
            G.append(shape(box([0.06,0.28,0.58], (side_x+np.sign(side_x)*0.05,y,z)), 'Glass','final_side_blue_window'))
            E.append(shape(box([0.04,0.10,0.42], (side_x+np.sign(side_x)*0.08,y,z)), 'CyanEmit','final_side_window_core'))
        for z in [-2.15,0.25,2.45,4.45]:
            B.append(shape(box([0.06,0.45,0.9], (side_x,1.9,z)), 'Black','final_side_vent_frame'))
            for yy in [1.76,1.90,2.04]:
                B.append(shape(box([0.07,0.045,0.76], (side_x+np.sign(side_x)*0.02,yy,z)), 'Metal','final_side_vent_blade'))
        # additional team color stripes and plates visible from side.
        T.append(shape(box([0.07,0.34,1.7], (side_x,2.85,-1.3)), 'TeamColor','final_side_mid_white_strip'))
        T.append(shape(box([0.07,0.32,1.35], (side_x,1.15,-4.6)), 'TeamColor','final_side_front_service_white'))

    # Roof machinery: more vents, slats, access panels, conduit bundles, white team strips.
    for z in np.linspace(-3.7,3.7,9):
        B.append(shape(box([9.4,0.055,0.035], (0,5.33,z)), 'DarkArmor','final_roof_cross_panel_line'))
    for x in np.linspace(-4.25,4.25,9):
        B.append(shape(box([0.035,0.055,8.1], (x,5.34,-0.05)), 'DarkArmor','final_roof_long_panel_line'))
    for x,z in [(-3.55,3.28),(-2.75,3.28),(2.75,3.28),(3.55,3.28),(-3.4,-2.75),(3.4,-2.75)]:
        for item in louver(0.7,0.48,0):
            B += moved([item], (x,5.55,z), (0,0,0))
    for x in [-3.95,3.95]:
        for z in np.linspace(-2.15,2.15,5):
            T.append(shape(box([0.62,0.052,0.34], (x,5.62,z)), 'TeamColor','final_roof_segmented_team_plate'))
    # rooftop conduit bundles.
    for x in [-2.25,2.25]:
        for dz in [-0.10,0.05,0.20]:
            B.append(shape(cyl_between((x,5.55,-3.35+dz),(x,5.55,2.4+dz),0.035,8),'Metal','final_roof_conduit'))
        for z in [-2.2,-0.6,1.0,2.2]:
            E.append(shape(box([0.28,0.045,0.08], (x,5.63,z)), 'OrangeEmit','final_roof_conduit_orange_clip'))

    # Front bay interior: panelized floor, lights, tool cabinets, extra machinery.
    for x in np.linspace(-2.7,2.7,7):
        B.append(shape(box([0.035,0.035,5.35], (x,0.94,-0.60)), 'Black','final_bay_floor_long_seam'))
    for z in np.linspace(-3.0,2.0,8):
        B.append(shape(box([5.85,0.035,0.035], (0,0.95,z)), 'Black','final_bay_floor_cross_seam'))
    for x,z in [(-2.85,-2.6),(2.85,-2.6),(-2.85,0.2),(2.85,0.2),(-2.35,1.75),(2.35,1.75)]:
        E.append(shape(box([0.46,0.035,0.26], (x,1.02,z)), 'CyanEmit','final_bay_floor_cyan_light'))
    for x,z in [(-3.25,-0.9),(3.25,-0.9),(-3.25,1.25),(3.25,1.25)]:
        B.append(shape(box([0.45,0.85,0.38], (x,1.25,z)), 'DarkArmor','final_interior_machine_box'))
        E.append(shape(box([0.26,0.08,0.10], (x,1.52,z-0.22)), 'CyanEmit','final_interior_machine_screen'))
    # Interior overhead service lights.
    for x in [-2.1,0,2.1]:
        E.append(shape(box([0.52,0.08,0.12], (x,3.42,-1.7)), 'CyanEmit','final_interior_overhead_cyan'))
        E.append(shape(box([0.52,0.08,0.12], (x,3.42,0.9)), 'CyanEmit','final_interior_overhead_cyan'))

    # Stacks: additional catwalk levels, white bands, pipes, exhaust caps.
    stack_positions = [(6.95,4.98,0.62,3.9), (5.65,5.35,0.38,2.8), (7.85,2.85,0.38,2.5)]
    for x,z,r,h in stack_positions:
        for y in [1.25, 2.25, 3.25]:
            B.append(shape(cylinder(r*1.11,0.06,'y',(x,y,z),24),'Metal','final_stack_thin_ring'))
            if y > 1.5:
                T.append(shape(cylinder(r*1.04,0.10,'y',(x,y+0.12,z),24),'TeamColor','final_stack_extra_team_band'))
        # ladder rails and rungs on visible side.
        side_x = x - r*0.85
        B.append(shape(cyl_between((side_x,0.95,z-r*0.25),(side_x,h+0.45,z-r*0.25),0.025,6),'Metal','final_stack_ladder_rail'))
        B.append(shape(cyl_between((side_x,0.95,z+r*0.03),(side_x,h+0.45,z+r*0.03),0.025,6),'Metal','final_stack_ladder_rail'))
        for yy in np.linspace(1.2,h+0.2,7):
            B.append(shape(cyl_between((side_x,yy,z-r*0.25),(side_x,yy,z+r*0.03),0.02,6),'Metal','final_stack_ladder_rung'))
    # Extra pipe maze around stacks and annexes.
    pipe_paths = [
        ((4.7,2.65,3.55),(6.25,2.65,4.90)), ((4.7,2.35,2.2),(7.75,2.35,2.85)),
        ((6.65,0.95,-2.5),(7.85,0.95,2.85)), ((-6.9,0.95,-2.85),(-7.5,0.95,3.8)),
        ((-5.25,2.15,3.0),(-7.3,2.15,4.35)), ((5.25,2.15,3.0),(7.4,2.15,4.25)),
    ]
    for a,b in pipe_paths:
        B.append(shape(cyl_between(a,b,0.075,10),'Metal','final_extra_pipe'))
        E.append(shape(cyl_between(np.asarray(a)+[0,0.11,0], np.asarray(b)+[0,0.11,0],0.027,8),'OrangeEmit','final_extra_pipe_glow_line'))

    # Small antennas, cameras, warning beacons and light bars.
    for x,z in [(-7.4,-5.5),(-4.6,-4.6),(4.6,-4.6),(7.4,-4.6),(-2.7,3.1),(2.7,3.1),(5.5,4.2)]:
        groups['Beacon'] += moved(antenna(0.9, True), (x,3.0 if abs(x)<5 else 1.95,z))
    for x,z in [(-5.35,-4.9),(5.35,-4.9),(-1.8,-4.9),(1.8,-4.9),(-7.8,-3.6),(7.8,-3.4)]:
        E.append(shape(box([0.32,0.12,0.08], (x,1.08,z)), 'CyanEmit','final_front_cyan_warning_light'))

    # Team-color emblem approximation on facade: wings + center diamond, modeled as separate white plates.
    T.append(shape(box([0.22,0.05,0.58], (0,4.08,-5.05), (0,0,0)), 'TeamColor','final_emblem_center'))
    for sx in [-1,1]:
        T.append(shape(box([0.82,0.05,0.22], (sx*0.55,4.10,-5.05), (0,0,sx*18)), 'TeamColor','final_emblem_wing'))
        T.append(shape(box([0.55,0.05,0.18], (sx*0.86,3.90,-5.05), (0,0,sx*-22)), 'TeamColor','final_emblem_lower_wing'))

    # Small exterior armor plates / crates with team color. More white regions for future faction tinting.
    for x,z in [(-6.85,-5.7),(6.85,-5.7),(-7.2,5.25),(7.2,5.35),(-5.0,-6.15),(5.0,-6.15)]:
        B.append(shape(box([0.75,0.42,0.52], (x,0.74,z)), 'DarkArmor','final_service_equipment_box'))
        T.append(shape(box([0.48,0.045,0.32], (x,0.98,z)), 'TeamColor','final_equipment_team_plate'))
        E.append(shape(box([0.32,0.05,0.07], (x,1.06,z-0.28)), 'OrangeEmit','final_equipment_orange_marker'))

    return groups


def build_groups() -> Dict[str, List[c.Shape]]:
    groups = c.build_blockout()
    add_refined_team_panels(groups)
    add_final_detail_pass(groups)
    return groups


def uv_for_material(mat: str) -> Tuple[float,float]:
    idx = PALETTE_ORDER.index(mat if mat in PALETTE_ORDER else 'Armor')
    return ((idx + 0.5) / len(PALETTE_ORDER), 0.5)


def write_mtl(path: Path, texture_name: str | None = None) -> None:
    with path.open('w') as f:
        for name, rgba in PALETTE.items():
            r,g,b,a = [v/255.0 for v in rgba]
            f.write(f'newmtl {name}\n')
            f.write(f'Kd {r:.4f} {g:.4f} {b:.4f}\nKa {r*0.25:.4f} {g*0.25:.4f} {b*0.25:.4f}\nKs 0.1000 0.1000 0.1000\nNs 30\nd {a:.4f}\n')
            if texture_name:
                f.write(f'map_Kd {texture_name}\n')
            f.write('\n')


def write_obj(path: Path, shapes: Iterable[c.Shape], atlas_rel: str = '../Textures/Bastion_WarFactoryCMV2_Atlas.png') -> int:
    path.parent.mkdir(parents=True, exist_ok=True)
    mtl_path = path.parent / MTL_NAME
    if not mtl_path.exists():
        write_mtl(mtl_path, atlas_rel)
    tris = 0
    with path.open('w') as f:
        f.write(f'# {ASSET_NAME}\nmtllib {MTL_NAME}\n')
        v_offset = 1
        vt_offset = 1
        for idx, s in enumerate(shapes):
            m = clean(s.mesh)
            if len(m.faces) == 0 or len(m.vertices) == 0:
                continue
            tris += len(m.faces)
            uv = uv_for_material(s.material)
            f.write(f'o {s.name or s.material}_{idx}\nusemtl {s.material}\n')
            for v in m.vertices:
                f.write(f'v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}\n')
            for _ in m.vertices:
                f.write(f'vt {uv[0]:.6f} {uv[1]:.6f}\n')
            for face in m.faces:
                a,b,d = face + v_offset
                ta,tb,td = face + vt_offset
                f.write(f'f {a}/{ta} {b}/{tb} {d}/{td}\n')
            v_offset += len(m.vertices)
            vt_offset += len(m.vertices)
    return tris


def shapes_local(shapes: Iterable[c.Shape], pivot=(0,0,0), rot_y_deg=0.0) -> List[c.Shape]:
    # Convert world/local authored shapes into an object-local model around a pivot.
    out=[]
    p=np.asarray(pivot, dtype=float)
    if abs(rot_y_deg) > 1e-6:
        # subtract pivot, then inverse rotation around Y so Unity can re-apply the object rotation.
        angle=math.radians(-rot_y_deg)
        R=np.array([[math.cos(angle),0,math.sin(angle)],[0,1,0],[-math.sin(angle),0,math.cos(angle)]], dtype=float)
    else:
        R=None
    for s in shapes:
        m=clean(s.mesh.copy())
        V=m.vertices - p
        if R is not None:
            V = V @ R.T
        m.vertices = V
        out.append(c.Shape(m, s.material, s.name))
    return out


def local_door() -> List[c.Shape]:
    parts=[]
    # Closed door bottom at local origin; placed at (0,1.02,-4.86) in Unity.
    for i in range(6):
        y=0.16+i*0.34
        parts.append(shape(box([6.05,0.26,0.15], (0,y,0)), 'DarkArmor','door_segment'))
        parts.append(shape(box([5.70,0.04,0.17], (0,y+0.10,-0.02)), 'Metal','door_segment_rib'))
    parts.append(shape(box([6.35,0.10,0.18], (0,2.20,0)), 'OrangeEmit','door_orange_lip'))
    parts.append(shape(box([5.85,0.06,0.18], (0,2.32,-0.02)), 'CyanEmit','door_cyan_lip'))
    return parts


def local_conveyor() -> List[c.Shape]:
    parts=[]
    parts.append(shape(box([2.55,0.13,5.65], (0,0,0)), 'Black','conveyor_belt_black'))
    parts.append(shape(box([2.25,0.08,5.30], (0,0.08,0)), 'Deck','conveyor_belt_deck'))
    for z in np.linspace(-2.45,2.45,10):
        parts.append(shape(cylinder(0.09,2.5,'x',(0,0.20,z),12),'Metal','conveyor_roller'))
    for x in [-1.38,1.38]:
        parts.append(shape(box([0.15,0.12,5.75], (x,0.24,0)), 'OrangeEmit','conveyor_orange_side_strip'))
    for z in [-1.8,-0.5,0.8]:
        parts.append(shape(box([0.70,0.05,0.34], (-0.55,0.31,z)), 'CyanEmit','conveyor_cyan_panel'))
        parts.append(shape(box([0.70,0.05,0.34], (0.55,0.31,z)), 'CyanEmit','conveyor_cyan_panel'))
    # partial vehicle chassis on the belt, matching concept interior.
    parts.append(shape(box([1.38,0.30,2.65], (0,0.48,-0.05)), 'DarkArmor','vehicle_chassis'))
    parts.append(shape(box([1.70,0.22,0.72], (0,0.66,-1.00)), 'ArmorLight','vehicle_cab'))
    parts.append(shape(box([1.15,0.12,0.42], (0,0.81,-1.36)), 'Glass','vehicle_window'))
    for x in [-0.92,0.92]:
        for z in [-1.3,-0.15,1.0]:
            parts.append(shape(cylinder(0.24,0.18,'x',(x,0.38,z),16),'Metal','chassis_wheel'))
    return parts


def local_gantry_trolley() -> List[c.Shape]:
    return [
        shape(box([0.92,0.38,0.74], (0,0,0)), 'OrangeEmit','gantry_trolley_body'),
        shape(box([6.7,0.16,0.16], (0,0.04,0)), 'Metal','gantry_crossbeam'),
        shape(cyl_between((0,-0.20,0),(0,-1.35,0),0.045,8),'Metal','gantry_hoist_cable'),
        shape(box([0.36,0.25,0.36], (0,-1.47,0)), 'DarkArmor','gantry_hook_block'),
        shape(cyl_between((-0.2,-1.55,0),(-0.52,-1.78,0.12),0.025,6),'Metal','gantry_claw_l'),
        shape(cyl_between((0.2,-1.55,0),(0.52,-1.78,0.12),0.025,6),'Metal','gantry_claw_r'),
    ]


def local_beacon_lights() -> List[c.Shape]:
    parts=[]
    for off in [(-0.18,0,0),(0.18,0,0)]:
        parts.append(shape(cylinder(0.055,0.10,'y',(off[0],0.05,off[2]),8),'OrangeEmit','beacon_bulb'))
    parts.append(shape(cylinder(0.15,0.08,'y',(0,0,0),12),'DarkArmor','beacon_base'))
    return parts


def build_lod1_shapes() -> Tuple[List[c.Shape], List[c.Shape], List[c.Shape], List[c.Shape]]:
    base=[]; team=[]; emiss=[]; glass=[]
    base += [shape(box([17.0,0.25,14.2], (0,0.12,0)), 'Black','lod1_foundation'), shape(box([16.2,0.18,13.35], (0,0.32,0)), 'Deck','lod1_pad')]
    base += [shape(ramp(7.6,-7.85,-4.2,0.08,0.62,0.12), 'Deck','lod1_ramp')]
    base += [shape(box([10.3,4.4,8.8], (0,2.62,-0.1)), 'Armor','lod1_main_block'), shape(gable_roof(10.6,8.95,1.0,0.28,(0,4.92,-0.05)), 'ArmorLight','lod1_roof')]
    # Open-ish door dark front to hint bay
    base += [shape(box([6.3,2.2,0.12], (0,1.8,-4.9)), 'DarkArmor','lod1_bay_dark')]
    for x in [-6.45,6.55]:
        base.append(shape(box([2.6,2.4,5.8], (x,1.48,0.0)), 'Armor','lod1_side_annex'))
        base.append(shape(gable_roof(2.9,5.9,0.45,0.17,(x,2.86,0.0)), 'ArmorLight','lod1_annex_roof'))
    for x,z,r,h in [(6.95,4.98,0.62,3.9),(5.65,5.35,0.38,2.8),(7.85,2.85,0.38,2.5)]:
        base.append(shape(cylinder(r,h,'y',(x,0.60+h/2,z),18),'Armor','lod1_stack'))
        base.append(shape(cylinder(r*1.1,0.15,'y',(x,0.60+h,z),18),'DarkArmor','lod1_stack_top'))
        emiss.append(shape(cylinder(r*1.05,0.07,'y',(x,1.55,z),18),'OrangeEmit','lod1_stack_orange'))
        team.append(shape(cylinder(r*1.03,0.12,'y',(x,0.60+h*0.72,z),18),'TeamColor','lod1_stack_team_band'))
    base += [shape(box([6.8,0.28,1.35],(0,5.35,1.05)),'DarkArmor','lod1_louver_bank')]
    for pos in [(-1.95,5.88,3.05),(1.95,5.88,3.05),(0.0,5.88,-2.35)]:
        base.append(shape(cylinder(0.62,0.16,'y',pos,18),'DarkArmor','lod1_fan'))
    team += [shape(box([1.55,1.35,0.07],(-3.18,3.53,-4.97)),'TeamColor','lod1_front_l'), shape(box([1.55,1.35,0.07],(3.18,3.53,-4.97)),'TeamColor','lod1_front_r'), shape(box([0.62,0.06,5.75],(-4.15,5.44,0.05)),'TeamColor','lod1_roof_l'), shape(box([0.62,0.06,5.75],(4.15,5.44,0.05)),'TeamColor','lod1_roof_r')]
    emiss += [shape(box([0.14,3.0,0.10],(-5.55,2.55,-4.90)),'OrangeEmit','lod1_orange_column_l'), shape(box([0.14,3.0,0.10],(5.55,2.55,-4.90)),'OrangeEmit','lod1_orange_column_r'), shape(box([6.2,0.11,0.11],(0,3.27,-4.72)),'CyanEmit','lod1_cyan_header')]
    glass += [shape(box([0.45,0.24,0.12],(-4.2,1.18,-5.08)),'Glass','lod1_glass_l'), shape(box([0.45,0.24,0.12],(4.2,1.18,-5.08)),'Glass','lod1_glass_r')]
    return base, team, emiss, glass


def build_lod2_shapes() -> Tuple[List[c.Shape], List[c.Shape]]:
    base=[]; team=[]
    base += [shape(box([17.0,0.25,14.2],(0,0.12,0)),'Black','lod2_base'), shape(box([10.5,4.2,8.8],(0,2.55,-0.1)),'Armor','lod2_main'), shape(gable_roof(10.8,9.0,0.85,0.25,(0,4.8,-0.05)),'ArmorLight','lod2_roof'), shape(ramp(7.4,-7.85,-4.2,0.08,0.62,0.12),'Deck','lod2_ramp')]
    base += [shape(box([2.7,2.3,5.8],(-6.5,1.45,0.0)),'Armor','lod2_left'), shape(box([2.8,2.4,6.0],(6.6,1.45,0.1)),'Armor','lod2_right')]
    for x,z,r,h in [(6.95,4.98,0.6,3.6),(5.65,5.35,0.34,2.5),(7.85,2.85,0.34,2.4)]:
        base.append(shape(cylinder(r,h,'y',(x,0.60+h/2,z),12),'Armor','lod2_stack'))
    team += [shape(box([1.6,1.2,0.07],(-3.2,3.45,-4.95)),'TeamColor','lod2_team_l'), shape(box([1.6,1.2,0.07],(3.2,3.45,-4.95)),'TeamColor','lod2_team_r'), shape(box([0.55,0.05,5.2],(-4.0,5.28,0.0)),'TeamColor','lod2_roof_team_l'), shape(box([0.55,0.05,5.2],(4.0,5.28,0.0)),'TeamColor','lod2_roof_team_r')]
    return base, team


def split_static_groups(groups: Dict[str,List[c.Shape]]) -> Dict[str,List[c.Shape]]:
    fx = [s for s in groups['SmokeStackFX'] if 'smoke_socket' in s.name]
    return {
        'BaseArmor': groups['BaseArmor'],
        'TeamColorPanels': groups['TeamColorPanels'],
        'Emissive': groups['Emissive'],
        'GlassLights': groups['GlassLights'],
        'SmokeStackFX': fx,
    }


def make_atlas(path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    img = Image.new('RGBA', (len(PALETTE_ORDER)*SWATCH, TEXTURE_H), (0,0,0,0))
    d = ImageDraw.Draw(img)
    for i,name in enumerate(PALETTE_ORDER):
        col = PALETTE[name]
        x0=i*SWATCH
        for y in range(TEXTURE_H):
            # tiny vertical gradient inside swatch
            f=1.0 + (0.10 if y < TEXTURE_H/2 else -0.08)
            rgb=tuple(max(0,min(255,int(v*f))) for v in col[:3])
            d.line([x0,y,x0+SWATCH-1,y], fill=rgb+(col[3],))
        d.rectangle([x0,0,x0+SWATCH-1,TEXTURE_H-1], outline=(0,0,0,80))
    img.save(path)
    # Emission map uses only orange and cyan swatches.
    em = Image.new('RGBA', img.size, (0,0,0,255))
    ed=ImageDraw.Draw(em)
    for name in ['OrangeEmit','CyanEmit']:
        i=PALETTE_ORDER.index(name)
        ed.rectangle([i*SWATCH,0,(i+1)*SWATCH-1,TEXTURE_H-1], fill=PALETTE[name])
    em.save(path.with_name('Bastion_WarFactoryCMV2_Emission.png'))


def export_meshes(groups: Dict[str,List[c.Shape]]) -> Dict[str,int]:
    tri_counts: Dict[str,int] = {}
    static = split_static_groups(groups)
    for name, shs in static.items():
        tri_counts[name] = write_obj(MESH_DIR / f'{ASSET_SLUG}_{name}.obj', shs)
    # Active / articulated local meshes.
    active_defs = {
        'AssemblyDoor': local_door(),
        'Conveyor': local_conveyor(),
        'GantryTrolley': local_gantry_trolley(),
        'RoofFan': fan_unit(0.55),
        'RobotArmA': robot_arm(side=1),
        'RobotArmB': robot_arm(side=-1),
        'BeaconSpin': local_beacon_lights(),
    }
    for name, shs in active_defs.items():
        tri_counts[name] = write_obj(MESH_DIR / f'{ASSET_SLUG}_{name}.obj', shs)
    # LODs.
    l1_base,l1_team,l1_emiss,l1_glass = build_lod1_shapes()
    l2_base,l2_team = build_lod2_shapes()
    tri_counts['LOD1_BaseArmor'] = write_obj(MESH_DIR / f'{ASSET_SLUG}_LOD1_BaseArmor.obj', l1_base)
    tri_counts['LOD1_TeamColorPanels'] = write_obj(MESH_DIR / f'{ASSET_SLUG}_LOD1_TeamColorPanels.obj', l1_team)
    tri_counts['LOD1_Emissive'] = write_obj(MESH_DIR / f'{ASSET_SLUG}_LOD1_Emissive.obj', l1_emiss)
    tri_counts['LOD1_GlassLights'] = write_obj(MESH_DIR / f'{ASSET_SLUG}_LOD1_GlassLights.obj', l1_glass)
    tri_counts['LOD2_BaseArmor'] = write_obj(MESH_DIR / f'{ASSET_SLUG}_LOD2_BaseArmor.obj', l2_base)
    tri_counts['LOD2_TeamColorPanels'] = write_obj(MESH_DIR / f'{ASSET_SLUG}_LOD2_TeamColorPanels.obj', l2_team)

    # Quick import static OBJ: door open / all pieces placed.
    world = world_shapes_for_preview(groups, door_open=True, include_smoke_fx=True)
    tri_counts['StaticQuickImport'] = write_obj(QUICK_MESH_DIR / f'{ASSET_SLUG}_Static.obj', world, atlas_rel='../Textures/Bastion_WarFactoryCMV2_Atlas.png')
    # Copy one MTL to quick mesh dir after writing static; it exists there.
    return tri_counts


def transform_shapes(shapes: Iterable[c.Shape], t=(0,0,0), euler=(0,0,0)) -> List[c.Shape]:
    return moved(list(shapes), t, euler)


def world_shapes_for_preview(groups: Dict[str,List[c.Shape]], door_open=True, include_smoke_fx=True) -> List[c.Shape]:
    shapes=[]
    static = split_static_groups(groups)
    for key in ['BaseArmor','TeamColorPanels','Emissive','GlassLights']:
        shapes += static[key]
    if include_smoke_fx:
        shapes += [s for s in groups['SmokeStackFX'] if 'smoke_socket' in s.name]
    door_pos = (0,1.02 + (1.95 if door_open else 0), -4.86)
    shapes += transform_shapes(local_door(), door_pos)
    shapes += transform_shapes(local_conveyor(), (0,0.78,-0.58))
    shapes += transform_shapes(local_gantry_trolley(), (0,3.92,0.80))
    for pos in [(-1.95,5.88,3.05),(1.95,5.88,3.05),(0.0,5.88,-2.35)]:
        shapes += transform_shapes(fan_unit(0.55), pos)
    robot_specs = [
        (robot_arm(side=1), (-2.25,0.83,-1.45), (0,25,0)),
        (robot_arm(side=-1), (2.25,0.83,-1.45), (0,-25,0)),
        (robot_arm(side=1), (-2.15,0.83,1.25), (0,5,0)),
        (robot_arm(side=-1), (2.15,0.83,1.25), (0,-5,0)),
    ]
    for shs,pos,rot in robot_specs:
        shapes += transform_shapes(shs, pos, rot)
    for pos in [(-7.25,3.92,-5.4),(-4.75,6.62,-3.25),(4.85,6.37,-3.00),(-2.6,6.70,3.1),(2.6,6.70,3.1),(5.5,7.05,4.0)]:
        shapes += transform_shapes(local_beacon_lights(), pos)
    return shapes


def render_actual_mesh(shapes: List[c.Shape], path: Path, camera='iso', W=1600, H=1000, title='WAR FACTORY CONCEPT-MATCH V2 — ACTUAL MESH') -> None:
    sf=2; w=W*sf; h=H*sf
    img=Image.new('RGBA',(w,h),(225,228,225,255))
    d=ImageDraw.Draw(img,'RGBA')
    d.rectangle([0,0,w,h], fill=(226,229,226,255))
    d.ellipse([int(w*0.12), int(h*0.73), int(w*0.94), int(h*0.96)], fill=(0,0,0,34))
    if camera=='iso':
        eye=np.array([10.5,7.7,-12.7],float); target=np.array([0,2.55,-0.55],float); area=(int(w*0.035), int(h*0.055), int(w*0.975), int(h*0.92))
    elif camera=='front':
        eye=np.array([0,5.3,-15.8],float); target=np.array([0,2.6,-1.6],float); area=(int(w*0.04), int(h*0.05), int(w*0.96), int(h*0.92))
    elif camera=='top':
        eye=np.array([0,23,0.01],float); target=np.array([0,0,0],float); area=(int(w*0.06), int(h*0.04), int(w*0.94), int(h*0.92))
    else:
        raise ValueError(camera)
    upw=np.array([0,1,0],float) if camera!='top' else np.array([0,0,1],float)
    fwd=target-eye; fwd=fwd/np.linalg.norm(fwd)
    right=np.cross(fwd,upw); right=right/np.linalg.norm(right)
    up=np.cross(right,fwd); up=up/np.linalg.norm(up)
    light=np.array([-0.46,0.78,-0.34],float); light=light/np.linalg.norm(light)
    polys=[]; all2=[]
    for s in shapes:
        m=clean(s.mesh); V=m.vertices
        if len(V)==0: continue
        coords=np.stack([V@right,V@up,V@fwd],axis=1)
        all2.append(coords[:,:2])
        col=np.array(PALETTE.get(s.material, PALETTE['Armor'])[:3],float)
        for face in m.faces:
            P=V[face]
            n=np.cross(P[1]-P[0],P[2]-P[0]); L=np.linalg.norm(n)
            if L<1e-9: continue
            n=n/L
            shade=0.48+0.50*max(0,float(np.dot(n,light)))+0.08*max(0,float(np.dot(n,[0,1,0])))
            if s.material in ['OrangeEmit','CyanEmit']: shade=1.28
            elif s.material in ['TeamColor','TeamColorShadow']: shade=max(0.76,min(1.18,shade))
            else: shade=max(0.36,min(1.12,shade))
            cc=tuple(np.clip(col*shade,0,255).astype(np.uint8).tolist())
            polys.append((float(np.mean(coords[face,2])), coords[face,:2], cc, PALETTE.get(s.material,PALETTE['Armor'])[3], s.material))
    if not all2: return
    all2=np.concatenate(all2,axis=0); mn=all2.min(0); mx=all2.max(0); cen=(mn+mx)/2
    rw0,rh0,rw1,rh1=area; sc=min((rw1-rw0)/(mx[0]-mn[0]), (rh1-rh0)/(mx[1]-mn[1]))*0.93
    off=np.array([(rw0+rw1)/2, (rh0+rh1)/2])
    for depth,pts,cc,a,mat in sorted(polys,key=lambda p:p[0],reverse=True):
        sp=(pts-cen)*sc; xy=np.stack([off[0]+sp[:,0],off[1]-sp[:,1]],axis=1)
        outline=(0,0,0,42) if mat!='Smoke' else (0,0,0,0)
        d.polygon([tuple(p) for p in xy], fill=cc+(a,), outline=outline)
    try:
        font_big=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', int(36*sf))
        font_small=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', int(22*sf))
    except Exception:
        font_big=font_small=None
    d.rectangle([0,0,w,int(60*sf)], fill=(0,0,0,72))
    d.text((30*sf,14*sf), title, fill=(246,246,236,255), font=font_big)
    d.text((32*sf,h-int(42*sf)), 'Rendered from generated mesh • white = team-color material • cyan/orange = emissive • no image-generation preview', fill=(55,60,58,255), font=font_small)
    img=img.convert('RGB').resize((W,H), Image.Resampling.LANCZOS)
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path, quality=95)


def make_comparison(reference_path: Path, mesh_preview_path: Path, out_path: Path) -> None:
    if not reference_path.exists() or not mesh_preview_path.exists():
        return
    ref=Image.open(reference_path).convert('RGB')
    mesh=Image.open(mesh_preview_path).convert('RGB')
    W,H=1800,900
    bg=Image.new('RGB',(W,H),(38,40,40))
    def fit(im,w,h):
        im=im.copy()
        im.thumbnail((w,h), Image.Resampling.LANCZOS)
        canvas=Image.new('RGB',(w,h),(38,40,40))
        canvas.paste(im,((w-im.width)//2,(h-im.height)//2))
        return canvas
    left=fit(ref,880,810); right=fit(mesh,880,810)
    bg.paste(left,(20,70)); bg.paste(right,(900,70))
    d=ImageDraw.Draw(bg)
    try:
        f=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',32)
    except Exception: f=None
    d.text((30,20),'REFERENCE CONCEPT',fill=(245,245,235),font=f)
    d.text((910,20),'ACTUAL GENERATED UNITY MESH PREVIEW',fill=(245,245,235),font=f)
    out_path.parent.mkdir(parents=True,exist_ok=True)
    bg.save(out_path,quality=95)


def write_unity_scripts_and_builder(tri_counts: Dict[str,int]) -> None:
    SCRIPT_DIR.mkdir(parents=True, exist_ok=True)
    EDITOR_DIR.mkdir(parents=True, exist_ok=True)
    (SCRIPT_DIR/'BastionWarFactoryCMV2.cs').write_text(textwrap.dedent(r'''
    using UnityEngine;
    using UnityEngine.Events;

    namespace BastionWarFactoryCMV2
    {
        public class BastionWarFactoryCMV2 : MonoBehaviour
        {
            [Header("Gameplay sockets")]
            public Transform spawnPoint;
            public Transform rallyPoint;
            public Transform vehicleAssemblySocket;
            public Transform interiorPoint;
            public Transform doorSocket;
            public Transform conveyorSocket;
            public Transform[] smokeSockets;
            public Transform[] lightSockets;
            public Transform[] robotArmSockets;

            [Header("Production state")]
            public bool productionActive;
            [Range(0f, 1f)] public float productionProgress;
            public UnityEvent onProductionStarted;
            public UnityEvent onProductionCompleted;

            public void StartProduction()
            {
                productionActive = true;
                productionProgress = 0f;
                onProductionStarted?.Invoke();
            }

            public void SetProgress(float progress)
            {
                productionProgress = Mathf.Clamp01(progress);
                if (productionProgress >= 1f) CompleteProduction();
            }

            public void CompleteProduction()
            {
                productionProgress = 1f;
                productionActive = false;
                onProductionCompleted?.Invoke();
            }
        }
    }
    '''))
    (SCRIPT_DIR/'BastionTeamColor.cs').write_text(textwrap.dedent(r'''
    using UnityEngine;

    namespace BastionWarFactoryCMV2
    {
        public class BastionTeamColor : MonoBehaviour
        {
            public Renderer[] teamColorRenderers;
            public Color teamColor = Color.white;
            private MaterialPropertyBlock block;

            private void Awake()
            {
                ApplyTeamColor(teamColor);
            }

            public void ApplyTeamColor(Color color)
            {
                teamColor = color;
                if (block == null) block = new MaterialPropertyBlock();
                if (teamColorRenderers == null) return;
                foreach (Renderer r in teamColorRenderers)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(block);
                    block.SetColor("_BaseColor", color);
                    block.SetColor("_Color", color);
                    r.SetPropertyBlock(block);
                }
            }
        }
    }
    '''))
    (SCRIPT_DIR/'BastionWarFactoryActiveAnimator.cs').write_text(textwrap.dedent(r'''
    using UnityEngine;

    namespace BastionWarFactoryCMV2
    {
        public class BastionWarFactoryActiveAnimator : MonoBehaviour
        {
            [Header("Moving pieces")]
            public Transform door;
            public Vector3 doorOpenOffset = new Vector3(0f, 1.95f, 0f);
            public bool doorOpenOnStart = true;
            public float doorSpeed = 2.2f;
            public Transform conveyor;
            public float conveyorBobAmount = 0.04f;
            public float conveyorSpeed = 1.2f;
            public Transform gantryTrolley;
            public float gantryTravel = 2.3f;
            public float gantrySpeed = 0.35f;
            public Transform[] roofFans;
            public float fanDegreesPerSecond = 220f;
            public Transform[] robotArms;
            public float robotArmDegrees = 7.5f;
            public float robotArmSpeed = 0.7f;
            public Transform[] beacons;
            public float beaconDegreesPerSecond = 85f;

            [Header("Smoke")]
            public Transform[] smokeSockets;
            public bool createSmokeOnStart = true;
            public float smokeEmissionRate = 7f;
            public float smokeLifetime = 3.0f;
            public float smokeSpeed = 0.65f;

            private Vector3 doorClosed;
            private Vector3 conveyorHome;
            private Vector3 gantryHome;
            private Quaternion[] armHome;
            private bool doorIsOpen;

            private void Awake()
            {
                if (door != null) doorClosed = door.localPosition;
                if (conveyor != null) conveyorHome = conveyor.localPosition;
                if (gantryTrolley != null) gantryHome = gantryTrolley.localPosition;
                if (robotArms != null)
                {
                    armHome = new Quaternion[robotArms.Length];
                    for (int i = 0; i < robotArms.Length; i++)
                        armHome[i] = robotArms[i] != null ? robotArms[i].localRotation : Quaternion.identity;
                }
                doorIsOpen = doorOpenOnStart;
            }

            private void Start()
            {
                if (createSmokeOnStart) CreateSmokeEmitters();
            }

            private void Update()
            {
                float dt = Time.deltaTime;
                if (door != null)
                {
                    Vector3 target = doorClosed + (doorIsOpen ? doorOpenOffset : Vector3.zero);
                    door.localPosition = Vector3.MoveTowards(door.localPosition, target, doorSpeed * dt);
                }
                if (conveyor != null)
                {
                    Vector3 p = conveyorHome;
                    p.y += Mathf.Sin(Time.time * conveyorSpeed) * conveyorBobAmount;
                    conveyor.localPosition = p;
                }
                if (gantryTrolley != null)
                {
                    Vector3 p = gantryHome;
                    p.x += Mathf.Sin(Time.time * gantrySpeed) * gantryTravel;
                    gantryTrolley.localPosition = p;
                }
                if (roofFans != null)
                {
                    foreach (Transform fan in roofFans)
                        if (fan != null) fan.Rotate(Vector3.up, fanDegreesPerSecond * dt, Space.Self);
                }
                if (beacons != null)
                {
                    foreach (Transform b in beacons)
                        if (b != null) b.Rotate(Vector3.up, beaconDegreesPerSecond * dt, Space.Self);
                }
                if (robotArms != null && armHome != null)
                {
                    float wave = Mathf.Sin(Time.time * robotArmSpeed) * robotArmDegrees;
                    for (int i = 0; i < robotArms.Length; i++)
                    {
                        if (robotArms[i] == null) continue;
                        float sign = (i % 2 == 0) ? 1f : -1f;
                        robotArms[i].localRotation = armHome[i] * Quaternion.Euler(0f, sign * wave, sign * wave * 0.35f);
                    }
                }
            }

            public void SetDoorOpen(bool open) => doorIsOpen = open;
            public void OpenDoor() => SetDoorOpen(true);
            public void CloseDoor() => SetDoorOpen(false);

            private void CreateSmokeEmitters()
            {
                if (smokeSockets == null) return;
                foreach (Transform socket in smokeSockets)
                {
                    if (socket == null) continue;
                    GameObject go = new GameObject("ProceduralSmokeFX");
                    go.transform.SetParent(socket, false);
                    ParticleSystem ps = go.AddComponent<ParticleSystem>();
                    var main = ps.main;
                    main.loop = true;
                    main.startLifetime = smokeLifetime;
                    main.startSpeed = smokeSpeed;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.30f, 0.95f);
                    main.startColor = new Color(0.55f, 0.55f, 0.55f, 0.42f);
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    var emission = ps.emission;
                    emission.rateOverTime = smokeEmissionRate;
                    var shape = ps.shape;
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 10f;
                    shape.radius = 0.15f;
                    var velocity = ps.velocityOverLifetime;
                    velocity.enabled = true;
                    velocity.y = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
                    var color = ps.colorOverLifetime;
                    color.enabled = true;
                    Gradient g = new Gradient();
                    g.SetKeys(
                        new GradientColorKey[] { new GradientColorKey(Color.gray, 0f), new GradientColorKey(Color.gray, 1f) },
                        new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0f), new GradientAlphaKey(0.42f, 0.18f), new GradientAlphaKey(0.0f, 1f) }
                    );
                    color.color = g;
                    ps.Play();
                }
            }
        }
    }
    '''))
    # Import postprocessor: importer settings only; prefab builder handles hierarchy/materials.
    (EDITOR_DIR/'BastionWarFactoryCMV2ImportPostprocessor.cs').write_text(textwrap.dedent(r'''
    using UnityEditor;

    namespace BastionWarFactoryCMV2.Editor
    {
        public class BastionWarFactoryCMV2ImportPostprocessor : AssetPostprocessor
        {
            private void OnPreprocessModel()
            {
                if (!assetPath.Contains("BastionWarFactoryCMV2/Meshes/")) return;
                ModelImporter importer = (ModelImporter)assetImporter;
                importer.globalScale = 1.0f;
                importer.useFileScale = false;
                importer.isReadable = true;
                importer.importNormals = ModelImporterNormals.Calculate;
                importer.normalSmoothingAngle = 60f;
                importer.importCameras = false;
                importer.importLights = false;
                importer.animationType = ModelImporterAnimationType.None;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
            }
        }
    }
    '''))
    (EDITOR_DIR/'BastionWarFactoryCMV2PrefabBuilder.cs').write_text(textwrap.dedent(r'''
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEditor.Callbacks;
    using UnityEngine;
    using UnityEngine.Rendering;

    namespace BastionWarFactoryCMV2.Editor
    {
        public static class BastionWarFactoryCMV2PrefabBuilder
        {
            private const string Root = "Assets/BastionWarFactoryCMV2";
            private const string Meshes = Root + "/Meshes";
            private const string Prefabs = Root + "/Prefabs";
            private const string Materials = Root + "/Materials";
            private const string Textures = Root + "/Textures";
            private const string Slug = "Bastion_WarFactoryCMV2";

            [DidReloadScripts]
            private static void AutoBuildAfterCompile()
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/Bastion_WarFactoryCMV2.prefab") == null)
                    BuildPrefab(false);
            }

            [MenuItem("Tools/Bastion/Build War Factory CMV2 Prefab")]
            public static void BuildFromMenu() => BuildPrefab(true);

            public static void BuildPrefab(bool selectPrefab)
            {
                EnsureFolder(Prefabs);
                EnsureFolder(Materials);
                Material baseMat = CreateBaseMaterial();
                Material teamMat = CreateTeamMaterial();
                Material emissiveMat = CreateEmissiveMaterial();
                Material glassMat = CreateGlassMaterial();
                Material smokeMat = CreateSmokeMaterial();

                GameObject root = new GameObject("Bastion_WarFactoryCMV2");
                GameObject lod0 = NewChild("LOD0", root.transform, Vector3.zero);
                GameObject staticRoot = NewChild("StaticRenderers", lod0.transform, Vector3.zero);
                GameObject activeRoot = NewChild("ActivePieces", lod0.transform, Vector3.zero);

                GameObject baseArmor = InstantiateModel("BaseArmor", Meshes + "/" + Slug + "_BaseArmor.obj", staticRoot.transform, Vector3.zero, baseMat);
                GameObject team = InstantiateModel("TeamColorPanels", Meshes + "/" + Slug + "_TeamColorPanels.obj", staticRoot.transform, Vector3.zero, teamMat);
                GameObject emiss = InstantiateModel("EmissiveParts", Meshes + "/" + Slug + "_Emissive.obj", staticRoot.transform, Vector3.zero, emissiveMat);
                GameObject glass = InstantiateModel("GlassLights", Meshes + "/" + Slug + "_GlassLights.obj", staticRoot.transform, Vector3.zero, glassMat);
                GameObject smokeFx = InstantiateModel("SmokeStackFXMarkers", Meshes + "/" + Slug + "_SmokeStackFX.obj", staticRoot.transform, Vector3.zero, smokeMat);

                GameObject door = InstantiateModel("AssemblyDoor", Meshes + "/" + Slug + "_AssemblyDoor.obj", activeRoot.transform, new Vector3(0f, 1.02f, -4.86f), baseMat);
                GameObject conveyor = InstantiateModel("AssemblyConveyor", Meshes + "/" + Slug + "_Conveyor.obj", activeRoot.transform, new Vector3(0f, 0.78f, -0.58f), baseMat);
                GameObject trolley = InstantiateModel("GantryTrolley", Meshes + "/" + Slug + "_GantryTrolley.obj", activeRoot.transform, new Vector3(0f, 3.92f, 0.80f), baseMat);

                GameObject fanA = InstantiateModel("RoofFan_A", Meshes + "/" + Slug + "_RoofFan.obj", activeRoot.transform, new Vector3(-1.95f, 5.88f, 3.05f), baseMat);
                GameObject fanB = InstantiateModel("RoofFan_B", Meshes + "/" + Slug + "_RoofFan.obj", activeRoot.transform, new Vector3(1.95f, 5.88f, 3.05f), baseMat);
                GameObject fanC = InstantiateModel("RoofFan_C", Meshes + "/" + Slug + "_RoofFan.obj", activeRoot.transform, new Vector3(0.0f, 5.88f, -2.35f), baseMat);

                GameObject armA = InstantiateModel("RobotArm_FrontLeft", Meshes + "/" + Slug + "_RobotArmA.obj", activeRoot.transform, new Vector3(-2.25f, 0.83f, -1.45f), baseMat);
                armA.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
                GameObject armB = InstantiateModel("RobotArm_FrontRight", Meshes + "/" + Slug + "_RobotArmB.obj", activeRoot.transform, new Vector3(2.25f, 0.83f, -1.45f), baseMat);
                armB.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);
                GameObject armC = InstantiateModel("RobotArm_RearLeft", Meshes + "/" + Slug + "_RobotArmA.obj", activeRoot.transform, new Vector3(-2.15f, 0.83f, 1.25f), baseMat);
                armC.transform.localRotation = Quaternion.Euler(0f, 5f, 0f);
                GameObject armD = InstantiateModel("RobotArm_RearRight", Meshes + "/" + Slug + "_RobotArmB.obj", activeRoot.transform, new Vector3(2.15f, 0.83f, 1.25f), baseMat);
                armD.transform.localRotation = Quaternion.Euler(0f, -5f, 0f);

                GameObject beaconA = InstantiateModel("Beacon_FrontLeft", Meshes + "/" + Slug + "_BeaconSpin.obj", activeRoot.transform, new Vector3(-7.25f, 3.92f, -5.40f), emissiveMat);
                GameObject beaconB = InstantiateModel("Beacon_RoofLeft", Meshes + "/" + Slug + "_BeaconSpin.obj", activeRoot.transform, new Vector3(-4.75f, 6.62f, -3.25f), emissiveMat);
                GameObject beaconC = InstantiateModel("Beacon_RoofRight", Meshes + "/" + Slug + "_BeaconSpin.obj", activeRoot.transform, new Vector3(4.85f, 6.37f, -3.00f), emissiveMat);
                GameObject beaconD = InstantiateModel("Beacon_Stack", Meshes + "/" + Slug + "_BeaconSpin.obj", activeRoot.transform, new Vector3(5.50f, 7.05f, 4.00f), emissiveMat);

                GameObject lod1 = NewChild("LOD1", root.transform, Vector3.zero);
                InstantiateModel("LOD1_BaseArmor", Meshes + "/" + Slug + "_LOD1_BaseArmor.obj", lod1.transform, Vector3.zero, baseMat);
                GameObject lod1Team = InstantiateModel("LOD1_TeamColorPanels", Meshes + "/" + Slug + "_LOD1_TeamColorPanels.obj", lod1.transform, Vector3.zero, teamMat);
                InstantiateModel("LOD1_Emissive", Meshes + "/" + Slug + "_LOD1_Emissive.obj", lod1.transform, Vector3.zero, emissiveMat);
                InstantiateModel("LOD1_GlassLights", Meshes + "/" + Slug + "_LOD1_GlassLights.obj", lod1.transform, Vector3.zero, glassMat);

                GameObject lod2 = NewChild("LOD2", root.transform, Vector3.zero);
                InstantiateModel("LOD2_BaseArmor", Meshes + "/" + Slug + "_LOD2_BaseArmor.obj", lod2.transform, Vector3.zero, baseMat);
                GameObject lod2Team = InstantiateModel("LOD2_TeamColorPanels", Meshes + "/" + Slug + "_LOD2_TeamColorPanels.obj", lod2.transform, Vector3.zero, teamMat);

                Transform spawn = NewChild("SpawnPoint", root.transform, new Vector3(0f, 0.95f, -8.45f)).transform;
                Transform rally = NewChild("RallyPoint", root.transform, new Vector3(0f, 0.95f, -10.25f)).transform;
                Transform assembly = NewChild("VehicleAssemblySocket", root.transform, new Vector3(0f, 1.55f, -0.55f)).transform;
                Transform interior = NewChild("InteriorPoint", root.transform, new Vector3(0f, 1.80f, 0.35f)).transform;
                Transform doorSocket = NewChild("DoorSocket", root.transform, new Vector3(0f, 2.20f, -4.85f)).transform;
                Transform conveyorSocket = NewChild("ConveyorSocket", root.transform, new Vector3(0f, 1.05f, -0.55f)).transform;
                Transform smokeA = NewChild("SmokeSocket_MainStack", root.transform, new Vector3(6.95f, 5.40f, 4.98f)).transform;
                Transform smokeB = NewChild("SmokeSocket_MidStack", root.transform, new Vector3(5.65f, 4.40f, 5.35f)).transform;
                Transform smokeC = NewChild("SmokeSocket_OuterStack", root.transform, new Vector3(7.85f, 4.00f, 2.85f)).transform;
                Transform smokeD = NewChild("SmokeSocket_ServiceVent", root.transform, new Vector3(-7.20f, 3.30f, 4.25f)).transform;
                Transform lightA = NewChild("LightSocket_LeftBay", root.transform, new Vector3(-5.55f, 2.55f, -4.90f)).transform;
                Transform lightB = NewChild("LightSocket_RightBay", root.transform, new Vector3(5.55f, 2.55f, -4.90f)).transform;
                Transform lightC = NewChild("LightSocket_Interior", root.transform, new Vector3(0f, 3.30f, -1.00f)).transform;

                var metadata = root.AddComponent<BastionWarFactoryCMV2.BastionWarFactoryCMV2>();
                metadata.spawnPoint = spawn;
                metadata.rallyPoint = rally;
                metadata.vehicleAssemblySocket = assembly;
                metadata.interiorPoint = interior;
                metadata.doorSocket = doorSocket;
                metadata.conveyorSocket = conveyorSocket;
                metadata.smokeSockets = new Transform[] { smokeA, smokeB, smokeC, smokeD };
                metadata.lightSockets = new Transform[] { lightA, lightB, lightC };
                metadata.robotArmSockets = new Transform[] { armA.transform, armB.transform, armC.transform, armD.transform };

                var active = root.AddComponent<BastionWarFactoryCMV2.BastionWarFactoryActiveAnimator>();
                active.door = door.transform;
                active.conveyor = conveyor.transform;
                active.gantryTrolley = trolley.transform;
                active.roofFans = new Transform[] { fanA.transform, fanB.transform, fanC.transform };
                active.robotArms = new Transform[] { armA.transform, armB.transform, armC.transform, armD.transform };
                active.beacons = new Transform[] { beaconA.transform, beaconB.transform, beaconC.transform, beaconD.transform };
                active.smokeSockets = new Transform[] { smokeA, smokeB, smokeC, smokeD };

                var teamColor = root.AddComponent<BastionWarFactoryCMV2.BastionTeamColor>();
                List<Renderer> teamRenderers = new List<Renderer>();
                teamRenderers.AddRange(team.GetComponentsInChildren<Renderer>());
                teamRenderers.AddRange(lod1Team.GetComponentsInChildren<Renderer>());
                teamRenderers.AddRange(lod2Team.GetComponentsInChildren<Renderer>());
                teamColor.teamColorRenderers = teamRenderers.ToArray();
                teamColor.teamColor = Color.white;

                BoxCollider col = root.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 3.15f, -0.15f);
                col.size = new Vector3(17.4f, 6.6f, 14.8f);

                ConfigureLODGroup(root, lod0, lod1, lod2);

                string prefabPath = Prefabs + "/Bastion_WarFactoryCMV2.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                if (selectPrefab)
                {
                    Object prefab = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
                Debug.Log("Bastion War Factory CMV2 prefab created at " + prefabPath);
            }

            private static GameObject InstantiateModel(string name, string path, Transform parent, Vector3 localPosition, Material material)
            {
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null)
                {
                    Debug.LogError("Missing model asset: " + path);
                    GameObject missing = new GameObject(name + "_MISSING");
                    missing.transform.SetParent(parent, false);
                    missing.transform.localPosition = localPosition;
                    return missing;
                }
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                instance.name = name;
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = localPosition;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                foreach (Renderer r in instance.GetComponentsInChildren<Renderer>())
                {
                    r.sharedMaterial = material;
                    r.shadowCastingMode = ShadowCastingMode.On;
                    r.receiveShadows = true;
                }
                return instance;
            }

            private static GameObject NewChild(string name, Transform parent, Vector3 localPosition)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = localPosition;
                return go;
            }

            private static void ConfigureLODGroup(GameObject root, GameObject lod0, GameObject lod1, GameObject lod2)
            {
                LODGroup group = root.AddComponent<LODGroup>();
                LOD[] lods = new LOD[3];
                lods[0] = new LOD(0.60f, lod0.GetComponentsInChildren<Renderer>());
                lods[1] = new LOD(0.25f, lod1.GetComponentsInChildren<Renderer>());
                lods[2] = new LOD(0.08f, lod2.GetComponentsInChildren<Renderer>());
                group.SetLODs(lods);
                group.RecalculateBounds();
            }

            private static Material CreateBaseMaterial()
            {
                Material mat = LoadOrCreateMaterial("Bastion_WarFactoryCMV2_BaseAtlas");
                Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_WarFactoryCMV2_Atlas.png");
                mat.SetTexture("_BaseMap", atlas);
                mat.SetTexture("_MainTex", atlas);
                mat.SetFloat("_Smoothness", 0.24f);
                mat.SetFloat("_Metallic", 0.03f);
                EditorUtility.SetDirty(mat);
                return mat;
            }

            private static Material CreateEmissiveMaterial()
            {
                Material mat = LoadOrCreateMaterial("Bastion_WarFactoryCMV2_EmissiveAtlas");
                Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_WarFactoryCMV2_Atlas.png");
                Texture2D emission = AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + "/Bastion_WarFactoryCMV2_Emission.png");
                mat.SetTexture("_BaseMap", atlas);
                mat.SetTexture("_MainTex", atlas);
                mat.EnableKeyword("_EMISSION");
                mat.SetTexture("_EmissionMap", emission);
                mat.SetColor("_EmissionColor", Color.white * 1.2f);
                mat.SetFloat("_Smoothness", 0.36f);
                EditorUtility.SetDirty(mat);
                return mat;
            }

            private static Material CreateTeamMaterial()
            {
                Material mat = LoadOrCreateMaterial("Bastion_WarFactoryCMV2_TeamColor");
                mat.SetColor("_BaseColor", Color.white);
                mat.SetColor("_Color", Color.white);
                mat.SetFloat("_Smoothness", 0.34f);
                mat.SetFloat("_Metallic", 0.02f);
                EditorUtility.SetDirty(mat);
                return mat;
            }

            private static Material CreateGlassMaterial()
            {
                Material mat = LoadOrCreateMaterial("Bastion_WarFactoryCMV2_GlassLights");
                mat.SetColor("_BaseColor", new Color(0.12f, 0.55f, 0.64f, 0.86f));
                mat.SetColor("_Color", new Color(0.12f, 0.55f, 0.64f, 0.86f));
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.10f, 0.85f, 1.0f) * 0.7f);
                mat.SetFloat("_Smoothness", 0.55f);
                mat.SetFloat("_Metallic", 0.0f);
                EditorUtility.SetDirty(mat);
                return mat;
            }

            private static Material CreateSmokeMaterial()
            {
                Material mat = LoadOrCreateMaterial("Bastion_WarFactoryCMV2_SmokeFXMarkers");
                mat.SetColor("_BaseColor", new Color(0.55f, 0.55f, 0.55f, 0.35f));
                mat.SetColor("_Color", new Color(0.55f, 0.55f, 0.55f, 0.35f));
                EditorUtility.SetDirty(mat);
                return mat;
            }

            private static Material LoadOrCreateMaterial(string name)
            {
                EnsureFolder(Materials);
                string path = Materials + "/" + name + ".mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                {
                    mat = new Material(FindUsableShader());
                    mat.name = name;
                    AssetDatabase.CreateAsset(mat, path);
                }
                return mat;
            }

            private static Shader FindUsableShader()
            {
                Shader s = Shader.Find("Universal Render Pipeline/Lit");
                if (s == null) s = Shader.Find("Standard");
                return s;
            }

            private static void EnsureFolder(string path)
            {
                string[] parts = path.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }
    }
    '''))


def write_docs(tri_counts: Dict[str,int]) -> None:
    total_lod0 = sum(v for k,v in tri_counts.items() if k in ['BaseArmor','TeamColorPanels','Emissive','GlassLights','SmokeStackFX','AssemblyDoor','Conveyor','GantryTrolley','RoofFan','RobotArmA','RobotArmB','BeaconSpin']) + tri_counts.get('RoofFan',0)*2 + tri_counts.get('RobotArmA',0) + tri_counts.get('RobotArmB',0)  # approximate extra instancing
    lod1 = sum(v for k,v in tri_counts.items() if k.startswith('LOD1'))
    lod2 = sum(v for k,v in tri_counts.items() if k.startswith('LOD2'))
    (OUT/'README_FIRST.txt').write_text(textwrap.dedent(f'''
    Bastion War Factory Concept-Match V2
    ====================================

    Import instructions:
    1. Unzip this package into the root of your Unity project.
    2. Let Unity compile scripts and import the OBJ meshes.
    3. The editor script auto-creates:
       Assets/BastionWarFactoryCMV2/Prefabs/Bastion_WarFactoryCMV2.prefab
    4. If it does not auto-create, run:
       Tools > Bastion > Build War Factory CMV2 Prefab

    This package contains actual Unity-ready model files. Preview images were rendered from the generated mesh.
    The white armor panels are the team-color surfaces and can be recolored by BastionTeamColor.ApplyTeamColor(Color).
    '''))
    (ASSET_ROOT/'README.md').write_text(textwrap.dedent(f'''
    # Bastion War Factory Concept-Match V2

    High-detail RTS production structure based on the approved reference blockout.

    ## Included

    - LOD0 modular renderers: base armor, team-color panels, emissive parts, glass/lights, smoke-stack/fx markers, animated pieces.
    - LOD1 and LOD2 simplified silhouettes.
    - Auto prefab builder.
    - Team-color script: `BastionTeamColor.ApplyTeamColor(Color)`.
    - Active-building script with animated door, roof fans, conveyor, gantry trolley, robot arms, beacon lights, and smoke particle sockets.
    - Sockets for spawn, rally, vehicle assembly, interior, door, conveyor, lights, robot arms, and smoke.

    ## Triangle summary

    Approximate LOD0 mesh triangles, including instanced active pieces: {total_lod0:,}
    LOD1 mesh triangles: {lod1:,}
    LOD2 mesh triangles: {lod2:,}

    ## Coordinate conventions

    Unity units are meters. Y-up, Z-forward. The main vehicle bay faces negative Z.
    '''))
    (ASSET_ROOT/'ASSET_NOTICE.txt').write_text('Original procedural geometry for this project. No game trademarks, logos, or copyrighted source meshes are included.\n')
    spec = {
        'asset': ASSET_NAME,
        'version': 'Concept-Match V2 final build',
        'unity_path': 'Assets/BastionWarFactoryCMV2/Prefabs/Bastion_WarFactoryCMV2.prefab',
        'coordinate_system': 'Y-up, Z-forward, meters',
        'triangle_counts': tri_counts,
        'materials': ['BaseAtlas','TeamColor','EmissiveAtlas','GlassLights','SmokeFXMarkers'],
        'team_color_surfaces': 'Separate TeamColorPanels renderer plus LOD team panels; defaults to white.',
        'animated_pieces': ['AssemblyDoor','AssemblyConveyor','GantryTrolley','RoofFan_A/B/C','RobotArm_FrontLeft/FrontRight/RearLeft/RearRight','Beacons','ProceduralSmokeFX'],
        'sockets': ['SpawnPoint','RallyPoint','VehicleAssemblySocket','InteriorPoint','DoorSocket','ConveyorSocket','SmokeSocket_MainStack','SmokeSocket_MidStack','SmokeSocket_OuterStack','SmokeSocket_ServiceVent','LightSocket_LeftBay','LightSocket_RightBay','LightSocket_Interior'],
        'preview_generation': 'Software orthographic render from generated mesh; no image generation used for preview.'
    }
    (SPEC_DIR/'Bastion_WarFactoryCMV2_Spec.json').write_text(json.dumps(spec, indent=2))


def copy_textures_to_quick() -> None:
    for name in ['Bastion_WarFactoryCMV2_Atlas.png','Bastion_WarFactoryCMV2_Emission.png']:
        shutil.copy2(TEXTURE_DIR/name, QUICK_TEXTURE_DIR/name)
    (QUICK_ROOT/'README.txt').write_text(textwrap.dedent('''
    Bastion War Factory Concept-Match V2 quick import
    =================================================

    This folder contains a static OBJ and material/atlas textures for quick viewing or manual import.
    For the animated prefab, scripts, LODs, sockets, and team-color workflow, use the full Unity package ZIP.
    '''))


def zip_dir(src: Path, dest: Path) -> None:
    if dest.exists(): dest.unlink()
    with zipfile.ZipFile(dest, 'w', zipfile.ZIP_DEFLATED) as z:
        for p in src.rglob('*'):
            if p.is_file():
                z.write(p, p.relative_to(src))


def validate_outputs(tri_counts: Dict[str,int]) -> None:
    # Basic integrity and file existence checks.
    assert (MESH_DIR / f'{ASSET_SLUG}_BaseArmor.obj').exists()
    assert (MESH_DIR / f'{ASSET_SLUG}_TeamColorPanels.obj').exists()
    assert (PREVIEW_DIR / 'Bastion_WarFactoryCMV2_Preview_Iso.png').exists()
    assert (QUICK_MESH_DIR / f'{ASSET_SLUG}_Static.obj').exists()
    for zip_path in [Path('/mnt/data/Bastion_WarFactoryCMV2_Unity_Asset.zip'), Path('/mnt/data/Bastion_WarFactoryCMV2_QuickImport.zip')]:
        with zipfile.ZipFile(zip_path) as z:
            bad = z.testzip()
            assert bad is None, f'Bad ZIP entry: {bad}'
    # Write validation note.
    (SPEC_DIR/'Validation.txt').write_text('ZIP integrity checked. OBJ files and preview files were created. Unity Editor import not executed in this environment.\n')


def main() -> None:
    reset_dirs()
    groups = build_groups()
    # Textures first so MTL references are valid.
    make_atlas(TEXTURE_DIR/'Bastion_WarFactoryCMV2_Atlas.png')
    tri_counts = export_meshes(groups)
    # Copy materials/textures for quick import.
    copy_textures_to_quick()
    # Preview images from actual mesh.
    world = world_shapes_for_preview(groups, door_open=True, include_smoke_fx=True)
    render_actual_mesh(world, PREVIEW_DIR/'Bastion_WarFactoryCMV2_Preview_Iso.png', 'iso', title='WAR FACTORY CMV2 — ACTUAL GENERATED MESH')
    render_actual_mesh(world, PREVIEW_DIR/'Bastion_WarFactoryCMV2_Preview_Front.png', 'front', title='WAR FACTORY CMV2 — FRONT MESH PREVIEW')
    render_actual_mesh(world, PREVIEW_DIR/'Bastion_WarFactoryCMV2_Preview_Top.png', 'top', W=1400, H=1000, title='WAR FACTORY CMV2 — TOP MESH PREVIEW')
    ref = Path('/mnt/data/E1D9FE47-BBAB-42B6-9A37-AD078B894CB9.jpeg')
    if not ref.exists(): ref = Path('/mnt/data/1C152A02-E873-4AC2-8269-AC8DD400956A.jpeg')
    make_comparison(ref, PREVIEW_DIR/'Bastion_WarFactoryCMV2_Preview_Iso.png', PREVIEW_DIR/'Concept_vs_GeneratedMesh_Comparison.png')
    # Docs/scripts/editor.
    write_unity_scripts_and_builder(tri_counts)
    write_docs(tri_counts)
    # Source copy.
    shutil.copy2(Path(__file__), SOURCE_DIR/'generate_warfactory_cmv2_final.py')
    # Full package: zip contents starting at OUT root so Assets/ is at zip root.
    zip_dir(OUT, Path('/mnt/data/Bastion_WarFactoryCMV2_Unity_Asset.zip'))
    zip_dir(QUICK_ROOT, Path('/mnt/data/Bastion_WarFactoryCMV2_QuickImport.zip'))
    validate_outputs(tri_counts)
    print(json.dumps({'created': str(OUT), 'tri_counts': tri_counts}, indent=2))

if __name__ == '__main__':
    main()
