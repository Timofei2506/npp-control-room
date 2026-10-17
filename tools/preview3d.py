#!/usr/bin/env python3
"""Грубый софт-рендер по координатам из NPPComplexBuilder — проверка расстановки.
Не замена Unity: ловит «висит в воздухе», «стоит поперёк прохода», «закрывает проход».
Правильное отсечение по ближней плоскости и сортировка по глубине."""
import math, sys
from PIL import Image, ImageDraw

W, H = 1280, 720
FOV = 70.0
NEAR = 0.15

def render(cam, yaw, pitch, objects, out, fov=FOV):
    cx, cy, cz = cam
    f = (W / 2) / math.tan(math.radians(fov / 2))
    cy_, sy_ = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
    cp, sp = math.cos(math.radians(pitch)), math.sin(math.radians(pitch))

    def to_cam(p):
        dx, dy, dz = p[0] - cx, p[1] - cy, p[2] - cz
        # поворот вокруг Y
        x = dx * cy_ - dz * sy_
        z = dx * sy_ + dz * cy_
        # наклон
        y = dy * cp - z * sp
        z2 = dy * sp + z * cp
        return (x, y, z2)

    def project(pc):
        return (W / 2 + f * pc[0] / pc[2], H / 2 - f * pc[1] / pc[2])

    def clip_near(poly):
        out = []
        n = len(poly)
        for i in range(n):
            a, b = poly[i], poly[(i + 1) % n]
            ain, bin_ = a[2] >= NEAR, b[2] >= NEAR
            if ain: out.append(a)
            if ain != bin_:
                t = (NEAR - a[2]) / (b[2] - a[2])
                out.append((a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, NEAR))
        return out

    img = Image.new("RGB", (W, H), (18, 20, 24))
    d = ImageDraw.Draw(img)
    faces = []

    def add(poly3, col):
        cp3 = [to_cam(p) for p in poly3]
        cl = clip_near(cp3)
        if len(cl) < 3: return
        scr = [project(p) for p in cl]
        depth = sum(p[2] for p in cl) / len(cl)
        faces.append((depth, scr, col))

    for o in objects:
        kind, col, tone = o[0], o[1], o[-1]
        c = tuple(max(0, min(255, int(v * tone))) for v in col)
        if kind == "box":
            x, y, z, sx, sy, sz = o[2:8]
            v = []
            for iy in (-1, 1):
                for iz in (-1, 1):
                    for ix in (-1, 1):
                        v.append((x + ix*sx/2, y + iy*sy/2, z + iz*sz/2))
            # индексы: 0(-x,-y,-z) 1(+x,-y,-z) 2(-x,-y,+z) 3(+x,-y,+z) 4(-x,+y,-z) 5(+x,+y,-z) 6(-x,+y,+z) 7(+x,+y,+z)
            quads = [(4,5,7,6),(0,2,3,1),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)]
            for q in quads:
                add([v[k] for k in q], c)
        elif kind == "cyl":
            x, y, z, r, h = o[2:7]
            n = 16
            ring = [(x + r*math.cos(2*math.pi*i/n), z + r*math.sin(2*math.pi*i/n)) for i in range(n)]
            for i in range(n):
                j = (i + 1) % n
                add([(ring[i][0], y-h/2, ring[i][1]), (ring[j][0], y-h/2, ring[j][1]),
                     (ring[j][0], y+h/2, ring[j][1]), (ring[i][0], y+h/2, ring[i][1])], c)
            add([(x+r*math.cos(2*math.pi*i/n), y+h/2, z+r*math.sin(2*math.pi*i/n)) for i in range(n)], c)

    faces.sort(key=lambda t: -t[0])
    for _, poly, col in faces:
        d.polygon(poly, fill=col)
    d.text((14, 12), out, fill=(230, 230, 230))
    img.save(out + ".png")

WALL = (168, 166, 158); FLOOR = (104, 101, 96); STEEL = (104, 108, 112)
CONC = (112, 110, 103); YEL = (196, 162, 42); RAIL = (222, 184, 60)
obj = []
def box(x, y, z, sx, sy, sz, col, tone=1.0): obj.append(("box", col, x, y, z, sx, sy, sz, tone))
def cyl(x, y, z, r, h, col, tone=1.0): obj.append(("cyl", col, x, y, z, r, h, tone))

# ---- реакторный зал
box(0, -0.1, 21, 18, 0.2, 18, FLOOR, 0.85)
box(0, 6.5, 30.15, 18, 13, 0.3, WALL)
box(0, 13.1, 21, 18, 0.3, 18, WALL, 0.7)
box(-9.15, 6.5, 21, 0.3, 13, 18, WALL)
box(9.15, 6.5, 16, 0.3, 13, 8, WALL)
box(9.15, 6.5, 26.3, 0.3, 13, 7.4, WALL)
cyl(0, 0.15, 21, 3.0, 0.3, CONC)
cyl(0, 3.4, 21, 2.05, 6.6, (132, 122, 108))
cyl(0, 6.85, 21, 1.75, 0.5, (120, 116, 108))
for i in range(12):
    a = 2*math.pi*i/12
    box(2.75*math.cos(a), 0.55, 21 + 2.75*math.sin(a), 0.55, 1.1, 0.55, CONC)
for dx, dz, sx, sz in ((0, 3.6, 7.2, 0.06), (0, -3.6, 7.2, 0.06), (3.6, 0, 0.06, 7.2), (-3.6, 0, 0.06, 7.2)):
    box(dx, 1.02, 21 + dz, sx, 0.07, sz, RAIL)
for sx, sz in ((-7.2, 16.4), (-7.2, 25.6), (-4.6, 28.3), (0.4, 28.3)):
    cyl(sx, 0.15, sz, 1.35, 0.3, CONC)
    cyl(sx, 3.0, sz, 1.15, 6.0, (150, 154, 148))
    cyl(sx, 6.1, sz, 1.0, 0.35, STEEL)
    cyl(sx + 0.95, 7.0, sz, 0.22, 1.9, (172, 164, 152))
for sx, sz in ((-7.7, 28.6), (3.7, 28.6)):
    box(sx, 0.15, sz, 1.6, 0.3, 1.6, CONC)
    cyl(sx, 1.2, sz, 0.55, 1.8, (76, 100, 136))
for i in range(3):
    box(-8.0 + i*1.15, 0.95, 12.55, 1.05, 1.9, 0.7, (98, 108, 100))
box(2.4, 1.15, 12.35, 2.4, 2.3, 0.2, (88, 92, 102))
box(0, 11.4, 21, 17.4, 0.45, 0.9, YEL)
for i in range(6):
    box(0, 11.95, 13.9 + i*3.1, 17.2, 0.16, 0.16, STEEL)

# ---- машинный зал
box(18, -0.1, 21, 18, 0.2, 18, FLOOR, 0.85)
box(18, 13.1, 21, 18, 0.3, 18, WALL, 0.7)
box(27.15, 6.5, 21, 0.3, 13, 18, WALL)
box(18, 6.5, 12.0, 18, 13, 0.3, WALL)
box(18, 6.5, 30.15, 18, 13, 0.3, WALL)
box(16.55, 0.16, 21.5, 3.9, 0.32, 15.2, CONC)
box(18.5, 0.51, 21.5, 3.4, 0.7, 14.6, (124, 120, 112))
cyl(18.5, 1.91, 18.0, 1.05, 3.2, (150, 152, 154))
cyl(18.5, 2.31, 21.8, 1.75, 4.4, (140, 142, 144))
cyl(18.5, 2.16, 25.4, 1.55, 3.6, (92, 120, 144))
cyl(18.5, 2.16, 28.15, 0.75, 0.9, STEEL)
box(23.6, 1.2, 18.6, 3.2, 2.4, 4.2, (104, 112, 114))
box(15.0, 0.42, 13.35, 2.2, 0.84, 0.75, (84, 88, 96))
box(15.0, 1.45, 13.01, 2.2, 0.75, 0.08, (68, 72, 80))
for i in range(2):
    cyl(17.6 + i*1.8, 5.7, 18.9, 0.3, 5.6, (172, 164, 152))
for i in range(6):
    box(18, 11.95, 13.9 + i*3.1, 17.2, 0.16, 0.16, STEEL)

render((-2.6, 1.68, 12.6), 0, -4, obj, "view_from_corridor")
render((-1.2, 1.68, 14.6), 28, -2, obj, "view_reactor")
render((10.2, 1.68, 21.3), 80, 0, obj, "view_turbine")
render((13.0, 1.68, 27.0), 190, -3, obj, "view_back")
print("rendered")
