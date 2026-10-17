using System;
using System.Collections.Generic;
using UnityEngine;

namespace NPP.Room
{
    /// <summary>
    /// Пиксельный «холст» для процедурной графики приборов и экранов.
    /// Рисуем в Color32[] и одним Apply заливаем в Texture2D.
    /// </summary>
    public class PixelCanvas
    {
        public readonly int W, H;
        public readonly Color32[] Px;
        readonly Texture2D _tex;

        public PixelCanvas(int w, int h, Color32 fill)
        {
            W = w; H = h;
            Px = new Color32[w * h];
            _tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            _tex.filterMode = FilterMode.Bilinear;
            _tex.wrapMode = TextureWrapMode.Clamp;
            Clear(fill);
        }

        public Texture2D Texture { get { return _tex; } }

        public void Clear(Color32 c)
        {
            for (int i = 0; i < Px.Length; i++) Px[i] = c;
        }

        public void Apply()
        {
            _tex.SetPixels32(Px);
            _tex.Apply(false, false);
            _tex.filterMode = FilterMode.Bilinear;
            _tex.wrapMode = TextureWrapMode.Clamp;
        }

        public void P(int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            Px[y * W + x] = c;
        }

        public void FillRect(int x, int y, int w, int h, Color32 c)
        {
            int x0 = Mathf.Max(0, x), y0 = Mathf.Max(0, y);
            int x1 = Mathf.Min(W, x + w), y1 = Mathf.Min(H, y + h);
            for (int yy = y0; yy < y1; yy++)
                for (int xx = x0; xx < x1; xx++)
                    Px[yy * W + xx] = c;
        }

        public void FrameRect(int x, int y, int w, int h, int t, Color32 c)
        {
            FillRect(x, y, w, t, c);
            FillRect(x, y + h - t, w, t, c);
            FillRect(x, y, t, h, c);
            FillRect(x + w - t, y, t, h, c);
        }

        public void Line(int x0, int y0, int x1, int y1, Color32 c, int thickness = 1)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy, e2;
            while (true)
            {
                ThickDot(x0, y0, c, thickness);
                if (x0 == x1 && y0 == y1) break;
                e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        void ThickDot(int x, int y, Color32 c, int t)
        {
            if (t <= 1) { P(x, y, c); return; }
            FillRect(x - t / 2, y - t / 2, t, t, c);
        }

        public void Circle(float cx, float cy, float r, Color32 c, int thickness = 1)
        {
            int steps = Mathf.Max(24, (int)(r * 6));
            for (int i = 0; i < steps; i++)
            {
                float a0 = Mathf.PI * 2f * i / steps;
                int x = Mathf.RoundToInt(cx + Mathf.Cos(a0) * r);
                int y = Mathf.RoundToInt(cy + Mathf.Sin(a0) * r);
                ThickDot(x, y, c, thickness);
            }
        }

        public void Arc(float cx, float cy, float r, float aStart, float aEnd, Color32 c, int thickness = 1)
        {
            int steps = Mathf.Max(8, (int)(r * Mathf.Abs(aEnd - aStart) * 1.2f));
            for (int i = 0; i <= steps; i++)
            {
                float a = Mathf.Lerp(aStart, aEnd, i / (float)steps);
                int x = Mathf.RoundToInt(cx + Mathf.Cos(a) * r);
                int y = Mathf.RoundToInt(cy + Mathf.Sin(a) * r);
                ThickDot(x, y, c, thickness);
            }
        }

        public void FillCircle(float cx, float cy, float r, Color32 c)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy <= r * r) P(x, y, c);
                }
        }

        // ---------- 5x7 bitmap font ----------
        static readonly Dictionary<char, string[]> Font5x7 = new Dictionary<char, string[]>
        {
            {'A', new[]{"01110","10001","10001","11111","10001","10001","10001"}},
            {'B', new[]{"11110","10001","10001","11110","10001","10001","11110"}},
            {'C', new[]{"01110","10001","10000","10000","10000","10001","01110"}},
            {'D', new[]{"11110","10001","10001","10001","10001","10001","11110"}},
            {'E', new[]{"11111","10000","10000","11110","10000","10000","11111"}},
            {'F', new[]{"11111","10000","10000","11110","10000","10000","10000"}},
            {'G', new[]{"01110","10001","10000","10111","10001","10001","01111"}},
            {'H', new[]{"10001","10001","10001","11111","10001","10001","10001"}},
            {'I', new[]{"11111","00100","00100","00100","00100","00100","11111"}},
            {'J', new[]{"00111","00010","00010","00010","00010","10010","01100"}},
            {'K', new[]{"10001","10010","10100","11000","10100","10010","10001"}},
            {'L', new[]{"10000","10000","10000","10000","10000","10000","11111"}},
            {'M', new[]{"10001","11011","10101","10101","10001","10001","10001"}},
            {'N', new[]{"10001","11001","10101","10011","10001","10001","10001"}},
            {'O', new[]{"01110","10001","10001","10001","10001","10001","01110"}},
            {'P', new[]{"11110","10001","10001","11110","10000","10000","10000"}},
            {'Q', new[]{"01110","10001","10001","10001","10101","10010","01101"}},
            {'R', new[]{"11110","10001","10001","11110","10100","10010","10001"}},
            {'S', new[]{"01111","10000","10000","01110","00001","00001","11110"}},
            {'T', new[]{"11111","00100","00100","00100","00100","00100","00100"}},
            {'U', new[]{"10001","10001","10001","10001","10001","10001","01110"}},
            {'V', new[]{"10001","10001","10001","10001","10001","01010","00100"}},
            {'W', new[]{"10001","10001","10001","10101","10101","10101","01010"}},
            {'X', new[]{"10001","10001","01010","00100","01010","10001","10001"}},
            {'Y', new[]{"10001","10001","01010","00100","00100","00100","00100"}},
            {'Z', new[]{"11111","00001","00010","00100","01000","10000","11111"}},
            {'0', new[]{"01110","10001","10011","10101","11001","10001","01110"}},
            {'1', new[]{"00100","01100","00100","00100","00100","00100","01110"}},
            {'2', new[]{"01110","10001","00001","00010","00100","01000","11111"}},
            {'3', new[]{"11110","00001","00001","01110","00001","00001","11110"}},
            {'4', new[]{"00010","00110","01010","10010","11111","00010","00010"}},
            {'5', new[]{"11111","10000","11110","00001","00001","10001","01110"}},
            {'6', new[]{"00110","01000","10000","11110","10001","10001","01110"}},
            {'7', new[]{"11111","00001","00010","00100","01000","01000","01000"}},
            {'8', new[]{"01110","10001","10001","01110","10001","10001","01110"}},
            {'9', new[]{"01110","10001","10001","01111","00001","00010","01100"}},
            {' ', new[]{"00000","00000","00000","00000","00000","00000","00000"}},
            {'.', new[]{"00000","00000","00000","00000","00000","01100","01100"}},
            {',', new[]{"00000","00000","00000","00000","01100","01100","00100"}},
            {'-', new[]{"00000","00000","00000","11111","00000","00000","00000"}},
            {'+', new[]{"00000","00100","00100","11111","00100","00100","00000"}},
            {'/', new[]{"00001","00010","00010","00100","01000","01000","10000"}},
            {'%', new[]{"10001","00010","00100","00100","01000","10001","00000"}},
            {':', new[]{"00000","01100","01100","00000","01100","01100","00000"}},
            {'>', new[]{"01000","00100","00010","00001","00010","00100","01000"}},
            {'<', new[]{"00010","00100","01000","10000","01000","00100","00010"}},
            {'=', new[]{"00000","00000","11111","00000","11111","00000","00000"}},
            {'!', new[]{"00100","00100","00100","00100","00100","00000","00100"}},
            {'?', new[]{"01110","10001","00001","00110","00100","00000","00100"}},
            {'(', new[]{"00010","00100","01000","01000","01000","00100","00010"}},
            {')', new[]{"01000","00100","00010","00010","00010","00100","01000"}},
            {'°', new[]{"01100","10010","10010","01100","00000","00000","00000"}},
            {'#', new[]{"01010","01010","11111","01010","11111","01010","01010"}},
            {'*', new[]{"00000","10101","01110","11111","01110","10101","00000"}},
        };

        public int TextWidth(string s, int scale) => s.Length * (5 * scale + scale);

        public void Text5x7(string text, int x, int y, int scale, Color32 c)
        {
            int cx = x;
            foreach (char raw in text)
            {
                char ch = char.ToUpperInvariant(raw);
                string[] glyph;
                if (!Font5x7.TryGetValue(ch, out glyph)) glyph = Font5x7['?'];
                for (int row = 0; row < 7; row++)
                    for (int col = 0; col < 5; col++)
                        if (glyph[row][col] == '1')
                            FillRect(cx + col * scale, y + (6 - row) * scale, scale, scale, c);
                cx += 6 * scale;
            }
        }

        // ---------- семисегментные цифры ----------
        // биты: a=1 b=2 c=4 d=8 e=16 f=32 g=64
        static readonly Dictionary<char, int> Seg7 = new Dictionary<char, int>
        {
            {'0', 0b0111111}, {'1', 0b0000110}, {'2', 0b1011011}, {'3', 0b1001111},
            {'4', 0b1100110}, {'5', 0b1101101}, {'6', 0b1111101}, {'7', 0b0000111},
            {'8', 0b1111111}, {'9', 0b1101111}, {'-', 0b1000000}, {' ', 0},
            {'E', 0b1111001}, {'r', 0b1010000}, {'A', 0b1110111}, {'o', 0b1011100},
            {'C', 0b0111001}, {'P', 0b1110011}, {'d', 0b1011110}, {'H', 0b1110110},
        };

        public void Digit7(char ch, int x, int y, int w, int h, int t, Color32 c, Color32 dim)
        {
            int mask;
            if (!Seg7.TryGetValue(char.ToUpperInvariant(ch), out mask)) mask = 0;
            // a top, b tr, c br, d bottom, e bl, f tl, g mid
            DrawSeg((mask & 1) != 0, x + t, y + h - t, x + w - t, y + h - t, t, c, dim);
            DrawSeg((mask & 2) != 0, x + w - t, y + h / 2 + 1, x + w - t, y + h - t, t, c, dim);
            DrawSeg((mask & 4) != 0, x + w - t, y + t, x + w - t, y + h / 2 - 1, t, c, dim);
            DrawSeg((mask & 8) != 0, x + t, y + t, x + w - t, y + t, t, c, dim);
            DrawSeg((mask & 16) != 0, x + t, y + t, x + t, y + h / 2 - 1, t, c, dim);
            DrawSeg((mask & 32) != 0, x + t, y + h / 2 + 1, x + t, y + h - t, t, c, dim);
            DrawSeg((mask & 64) != 0, x + t, y + h / 2, x + w - t, y + h / 2, t, c, dim);
        }

        void DrawSeg(bool on, int x0, int y0, int x1, int y1, int t, Color32 c, Color32 dim)
        {
            Line(x0, y0, x1, y1, on ? c : dim, t);
        }

        public void Dot(int x, int y, int r, Color32 c)
        {
            FillCircle(x, y, r, c);
        }
    }
}
