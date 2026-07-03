using System.Collections.Generic;
using UnityEngine;

namespace Mistforge
{
    /// Runtime-generated placeholder sprites (bordered color blocks) standing in
    /// for the 16-bit sheets specified in §3. All sprites use PPU 16 so pixel
    /// dimensions here match the sprite-spec tables 1:1 — swapping in real art
    /// is a drop-in replacement.
    public static class PlaceholderArt
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public const float PPU = 16f;

        public static Sprite Solid(int w, int h, Color color, bool border = true, bool speckle = false)
        {
            string key = w + "x" + h + color + border + speckle;
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var borderColor = color * 0.6f;
            borderColor.a = color.a;
            var speckleColor = color * 0.82f;
            speckleColor.a = color.a;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool edge = border && (x == 0 || y == 0 || x == w - 1 || y == h - 1);
                    // Deterministic speckle pattern so tiles look textured but stable.
                    bool dot = speckle && ((x * 7 + y * 13) % 11 == 0);
                    tex.SetPixel(x, y, edge ? borderColor : dot ? speckleColor : color);
                }
            }
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h),
                new Vector2(0.5f, 0f), PPU); // bottom-center pivot for characters
            cache[key] = sprite;
            return sprite;
        }

        /// Center-pivot variant, used for tiles, bars and backdrops.
        public static Sprite Centered(int w, int h, Color color, bool border = true, bool speckle = false)
        {
            string key = "c" + w + "x" + h + color + border + speckle;
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var source = Solid(w, h, color, border, speckle);
            var sprite = Sprite.Create(source.texture, new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f), PPU);
            cache[key] = sprite;
            return sprite;
        }

        /// Left-edge pivot, used for HP/AP bar fills that scale from the left.
        public static Sprite LeftPivot(int w, int h, Color color)
        {
            string key = "l" + w + "x" + h + color;
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var source = Solid(w, h, color, false);
            var sprite = Sprite.Create(source.texture, new Rect(0, 0, w, h),
                new Vector2(0f, 0.5f), PPU);
            cache[key] = sprite;
            return sprite;
        }

        public static Sprite Pixel(Color color)
        {
            return Centered(4, 4, color, false);
        }
    }
}
