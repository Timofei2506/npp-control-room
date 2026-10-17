#!/usr/bin/env python3
"""План здания по координатам из NPPComplexBuilder. Рисует PNG для проверки планировки."""
from PIL import Image, ImageDraw
import sys

S = 26
X0, X1 = -10, 28
Z0, Z1 = 4, 31
W, H = int((X1 - X0) * S), int((Z1 - Z0) * S)
img = Image.new("RGB", (W, H), (225, 226, 223))
d = ImageDraw.Draw(img)

def px(x, z): return ((x - X0) * S, (z - Z0) * S)
def rect(x0, x1, z0, z1, fill, outline=(70, 70, 70), width=1):
    d.rectangle([px(x0, z0), px(x1, z1)], fill=fill, outline=outline, width=width)
def circle(cx, cz, r, fill, outline=(70, 70, 70)):
    d.ellipse([px(cx - r, cz - r), px(cx + r, cz + r)], fill=fill, outline=outline)
def text(x, z, t, col=(20, 20, 20)):
    d.text(px(x, z), t, fill=col)

WALL = (150, 150, 150)
# стены
rect(-9.15, -8.85, 12, 30, WALL)
rect(-8.85, -4.6, 11.85, 12.15, WALL)
rect(-1.4, 8.85, 11.85, 12.15, WALL)
rect(-9.15, 9.15, 29.85, 30.15, WALL)
rect(8.85, 9.15, 12, 20, WALL)
rect(8.85, 9.15, 22.6, 30, WALL)
rect(9, 26.85, 11.85, 12.15, WALL)
rect(9, 26.85, 29.85, 30.15, WALL)
rect(26.85, 27.15, 12, 30, WALL)
rect(-4.7, -1.3, 5.5, 12, (206, 206, 200))

# реакторный зал
circle(0, 21, 3.95, (247, 238, 186))
circle(0, 21, 3.6, (238, 228, 176))
circle(0, 21, 3.0, (203, 193, 190))
circle(0, 21, 2.05, (150, 130, 120))
for sx, sz in [(-7.2, 16.4), (-7.2, 25.6), (-4.6, 28.3), (0.4, 28.3)]:
    circle(sx, sz, 1.35, (168, 188, 198))
for sx, sz in [(-7.7, 28.6), (3.7, 28.6)]:
    rect(sx - .8, sx + .8, sz - .8, sz + .8, (120, 150, 190))
for i in range(3):
    x = -8.0 + i * 1.15
    rect(x - .525, x + .525, 12.2, 12.9, (108, 120, 110))
rect(1.2, 3.6, 12.24, 12.44, (86, 86, 108))          # HallInfo

# машинный зал
rect(16.55, 20.45, 13.9, 29.1, (208, 208, 204))      # ступень фундамента
rect(16.3, 20.7, 14.2, 28.8, (196, 196, 192))        # фундамент
circle(18.5, 18.0, 1.05, (182, 182, 182))
circle(18.5, 21.8, 1.75, (170, 170, 170))
circle(18.5, 25.4, 1.55, (150, 172, 192))
circle(18.5, 28.15, .75, (140, 150, 160))
rect(22.0, 25.2, 16.5, 20.7, (160, 160, 150))        # конденсатор
rect(13.9, 16.1, 12.93, 13.78, (88, 88, 110))        # пульт

# маршрут: коридор → зал → дверь → машинный зал → вдоль турбины
route = [(-3.0, 11.5), (-3.0, 13.2), (-0.6, 15.2), (2.4, 17.6), (5.4, 19.6), (7.4, 21.3),
         (9.0, 21.3), (12.0, 21.3), (14.4, 21.3), (14.4, 26.0)]
pts = [px(x, z) for x, z in route]
d.line(pts, fill=(200, 30, 30), width=3)
for x, z in route:
    d.ellipse([px(x, z)[0]-3, px(x, z)[1]-3, px(x, z)[0]+3, px(x, z)[1]+3], fill=(200, 30, 30))

text(-8.6, 13.2, "РЕАКТОРНЫЙ ЗАЛ")
text(9.4, 13.2, "МАШИННЫЙ ЗАЛ")
text(-0.6, 20.4, "реактор")
text(17.3, 17.0, "турбина")
img.save(sys.argv[1] if len(sys.argv) > 1 else "/home/user/npp-sim/tools/layout.png")
print("ok")
