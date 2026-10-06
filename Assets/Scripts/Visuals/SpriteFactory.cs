using System.Collections.Generic;
using UnityEngine;

namespace BoxBlast
{
    /// <summary>
    /// Generates modern 3D glossy spheres/balls and circular socket sprites at runtime.
    /// Features dynamic lighting, specular glass highlights, and rim glows.
    /// </summary>
    public static class SpriteFactory
    {
        private static readonly Dictionary<string, Sprite> s_SpriteCache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_SpriteCache.Clear();
        }

        public static void ClearSpriteCache()
        {
            s_SpriteCache.Clear();
        }

        public static readonly Color[] BlockColors = new Color[]
        {
            new Color(0.95f, 0.22f, 0.22f), // Ruby Red
            new Color(0.98f, 0.44f, 0.10f), // Sunset Orange
            new Color(0.98f, 0.75f, 0.12f), // Golden Yellow
            new Color(0.25f, 0.78f, 0.15f), // Emerald Green
            new Color(0.12f, 0.74f, 0.94f), // Crystal Cyan
            new Color(0.18f, 0.50f, 0.95f), // Sapphire Blue
            new Color(0.62f, 0.30f, 0.88f), // Amethyst Purple
            new Color(0.95f, 0.28f, 0.65f)  // Neon Pink
        };

                        /// <summary>
        /// Generates a realistic 3D glossy sphere with dynamic specular glass highlight,
        /// ambient rim glow, and procedural skin pattern overlays (Star Core, Diamond Crystal, Neon Pulse, etc.).
        /// Automatically adopts the equipped skin from SkinManager unless an explicit skin is requested.
        /// </summary>
        public static Sprite GetBlockSprite(Color baseColor, BallSkinType? skinType = null)
        {
            BallSkinType effectiveSkin = skinType.HasValue ? skinType.Value : SkinManager.CurrentSkin;
            string key = $"Sphere_{effectiveSkin}_{ColorUtility.ToHtmlStringRGBA(baseColor)}";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            int size = 256;
            float radius = 118f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            Texture2D tex = CreateCrispTexture(size, size);
            Color[] pixels = new Color[size * size];
            Vector3 lightDir = new Vector3(-0.45f, 0.55f, 0.70f).normalized;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center.x) / radius;
                    float dy = (y - center.y) / radius;
                    float distSq = dx * dx + dy * dy;

                    if (distSq > 1.04f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    // Anti-aliased outer edge
                    float edgeAlpha = Mathf.Clamp01((1.02f - Mathf.Sqrt(distSq)) * 36f);

                    // Spherical surface normal
                    float z = Mathf.Sqrt(Mathf.Max(0f, 1f - Mathf.Clamp01(distSq)));
                    Vector3 normal = new Vector3(dx, dy, z).normalized;

                    // 1. Lambertian Diffuse with warm ambient base
                    float diffuse = Mathf.Max(0.22f, Vector3.Dot(normal, lightDir));

                    // 2. Specular Glass Highlight (sharp reflection spot on outer glass surface)
                    Vector3 reflectDir = Vector3.Reflect(-lightDir, normal);
                    float specular = Mathf.Pow(Mathf.Max(0f, Vector3.Dot(reflectDir, new Vector3(0, 0, 1))), 32f) * 0.90f;

                    // 3. Secondary Glass Glint (Soft oval glint on glass surface)
                    float glintDist = Mathf.Sqrt(Mathf.Pow(dx + 0.32f, 2f) + Mathf.Pow(dy - 0.32f, 2f));
                    float glint = Mathf.Pow(Mathf.Clamp01(1f - glintDist / 0.38f), 2.2f) * 0.45f;

                    // 4. Fresnel Rim Light (Luminous glow around sphere perimeter)
                    float fresnel = Mathf.Pow(1.0f - z, 3.0f) * 0.40f;

                    // 5. Interior Sphere Base Body Color
                    Color bodyColor = baseColor * (diffuse * 0.85f + 0.15f);

                    // 6. Apply Unique Ball Skin Overlay Pattern to interior
                    if (distSq <= 1.0f && effectiveSkin != BallSkinType.Classic)
                    {
                        bodyColor = ApplySkinPattern(bodyColor, effectiveSkin, dx, dy, z, baseColor, lightDir);
                    }

                    // 7. Composite Outer Glass Layer (Rim Fresnel + Specular Glints ON TOP)
                    Color finalColor = bodyColor + (baseColor * fresnel) + (Color.white * (specular + glint));

                    finalColor.a = baseColor.a * edgeAlpha;
                    pixels[y * size + x] = finalColor;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            s_SpriteCache[key] = sprite;
            return sprite;
        }

        private static Color ApplySkinPattern(Color sphereColor, BallSkinType skin, float dx, float dy, float z, Color baseColor, Vector3 lightDir)
        {
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(dy, dx);

            switch (skin)
            {
                case BallSkinType.Classic:
                    // Pure pristine 3D glossy marble
                    break;

                case BallSkinType.StarCore:
                {
                    // Radiant 8-Point Golden Celestial Star
                    float ax = Mathf.Abs(dx); float ay = Mathf.Abs(dy);
                    float beamV = Mathf.Exp(-ax * 28f) * Mathf.Max(0f, 0.60f - ay);
                    float beamH = Mathf.Exp(-ay * 28f) * Mathf.Max(0f, 0.60f - ax);
                    float diag1 = Mathf.Exp(-Mathf.Abs(ax - ay) * 24f) * Mathf.Max(0f, 0.45f - r) * 0.75f;
                    float diag2 = Mathf.Exp(-Mathf.Abs(ax + ay) * 24f) * Mathf.Max(0f, 0.45f - r) * 0.75f;
                    float starRays = Mathf.Clamp01(beamV + beamH + diag1 + diag2);
                    float nucleus = Mathf.Pow(Mathf.Clamp01(1f - r / 0.24f), 2.2f);

                    Color goldStar = Color.Lerp(new Color(1.0f, 0.85f, 0.25f), Color.white, nucleus);
                    sphereColor = Color.Lerp(sphereColor, goldStar, Mathf.Clamp01(starRays * 1.15f + nucleus * 0.95f));
                    break;
                }

                case BallSkinType.DiamondCrystal:
                {
                    // Geometric Faceted Prismatic Diamond
                    float octDist = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy), (Mathf.Abs(dx) + Mathf.Abs(dy)) * 0.707f);
                    float facetRing = Mathf.Abs(octDist - 0.46f);
                    if (facetRing < 0.038f)
                    {
                        float alpha = Mathf.Clamp01(1f - facetRing / 0.038f);
                        sphereColor += Color.white * (alpha * 0.75f);
                    }
                    float modAngle = Mathf.Abs((angle * Mathf.Rad2Deg + 360f) % 45f - 22.5f);
                    if (modAngle < 2.2f && r > 0.12f && r < 0.82f)
                    {
                        sphereColor += Color.white * 0.55f;
                    }
                    if (r < 0.16f)
                    {
                        sphereColor = Color.Lerp(sphereColor, Color.white, 0.70f);
                    }
                    break;
                }

                case BallSkinType.NeonPulse:
                {
                    // Tilted High-Tech Cyber Neon Orbital Ring
                    float tiltAngle = -28f * Mathf.Deg2Rad;
                    float rx = dx * Mathf.Cos(tiltAngle) - dy * Mathf.Sin(tiltAngle);
                    float ry = dx * Mathf.Sin(tiltAngle) + dy * Mathf.Cos(tiltAngle);
                    float ringDist = Mathf.Sqrt(Mathf.Pow(rx / 0.78f, 2f) + Mathf.Pow(ry / 0.26f, 2f));
                    float ringDelta = Mathf.Abs(ringDist - 1.0f);
                    if (ringDelta < 0.14f)
                    {
                        float glow = Mathf.Pow(Mathf.Clamp01(1f - ringDelta / 0.14f), 2f);
                        Color neonCyan = new Color(0.12f, 0.96f, 1.0f);
                        sphereColor = Color.Lerp(sphereColor, neonCyan, glow * 0.88f);
                        if (ringDelta < 0.04f) sphereColor += Color.white * 0.85f;
                    }
                    if (r < 0.18f)
                    {
                        float cGlow = Mathf.Pow(1f - r / 0.18f, 2f);
                        sphereColor = Color.Lerp(sphereColor, new Color(0.12f, 0.96f, 1.0f), cGlow * 0.9f);
                    }
                    break;
                }

                case BallSkinType.GalaxySwirl:
                {
                    // Radiant Celestial Galaxy:
                    // 1. Brilliant Crystalline Diamond Star at Core with 4 Lens-Flare Cross Rays
                    if (r < 0.42f)
                    {
                        float nucleus = Mathf.Pow(Mathf.Clamp01(1f - r / 0.20f), 2.5f);
                        float ax = Mathf.Abs(dx); float ay = Mathf.Abs(dy);
                        float beamV = Mathf.Exp(-ax * 36f) * Mathf.Max(0f, 0.40f - ay);
                        float beamH = Mathf.Exp(-ay * 36f) * Mathf.Max(0f, 0.40f - ax);
                        float diag = Mathf.Exp(-Mathf.Abs(ax - ay) * 28f) * Mathf.Max(0f, 0.26f - r) * 0.6f 
                                   + Mathf.Exp(-Mathf.Abs(ax + ay) * 28f) * Mathf.Max(0f, 0.26f - r) * 0.6f;
                        float starIntensity = Mathf.Clamp01(nucleus * 1.15f + (beamV + beamH) * 0.95f + diag);
                        sphereColor = Color.Lerp(sphereColor, Color.white, starIntensity * 0.95f);
                    }

                    // 2. Slender, Luminous Starlight Spiral Arms (Quadratic Curvature)
                    float spiral = angle - 2.2f * (r * r);
                    float arm = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(2f * spiral)), 8f);
                    if (arm > 0.05f && r > 0.14f && r < 0.82f)
                    {
                        float edgeFade = Mathf.Sin((r - 0.14f) / (0.82f - 0.14f) * Mathf.PI);
                        float starlight = arm * edgeFade;
                        Color starlightColor = Color.Lerp(new Color(1f, 0.92f, 0.65f), Color.white, arm);
                        sphereColor += starlightColor * (starlight * 0.80f);
                    }

                    // 3. Twinkling Diamond Stardust Crystals
                    float stardust = Mathf.Sin(dx * 32f) * Mathf.Cos(dy * 32f);
                    if (stardust > 0.86f && r > 0.16f && r < 0.78f)
                    {
                        sphereColor += Color.white * 0.60f;
                    }
                    break;
                }

                case BallSkinType.SunBurst:
                {
                    // Radiant Golden Solar Corona & Sunburst Flares
                    float ray = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(8f * angle)), 4f);
                    if (ray > 0.15f && r > 0.18f && r < 0.82f)
                    {
                        float flare = ray * (1f - Mathf.Abs(r - 0.50f) / 0.35f);
                        sphereColor = Color.Lerp(sphereColor, new Color(1.0f, 0.85f, 0.22f), flare * 0.85f);
                    }
                    if (r < 0.28f)
                    {
                        float sunCore = Mathf.Pow(1f - r / 0.28f, 1.8f);
                        sphereColor = Color.Lerp(sphereColor, new Color(1.0f, 0.98f, 0.80f), sunCore * 0.90f);
                    }
                    break;
                }

                case BallSkinType.HeartPearl:
                {
                    // Glowing Ruby Heart Emblem
                    float hy = dy - 0.06f;
                    float heartD = Mathf.Sqrt(dx * dx + hy * hy) + 0.32f * Mathf.Max(0f, -hy) * Mathf.Abs(dx);
                    if (heartD <= 0.40f)
                    {
                        float hEdge = Mathf.Clamp01((0.40f - heartD) * 18f);
                        float hGlint = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(Mathf.Pow(dx + 0.10f, 2f) + Mathf.Pow(hy - 0.10f, 2f)) / 0.16f), 2f);
                        Color heartCol = Color.Lerp(new Color(1.0f, 0.18f, 0.42f), new Color(1.0f, 0.88f, 0.94f), hGlint);
                        sphereColor = Color.Lerp(sphereColor, heartCol, hEdge * 0.92f);
                    }
                    break;
                }

                case BallSkinType.LightningVolt:
                {
                    // High-Voltage Electric Arc Discharge
                    float boltDist = DistanceToLightningBolt(dx, dy);
                    if (boltDist < 0.085f)
                    {
                        float intensity = Mathf.Clamp01(1f - boltDist / 0.085f);
                        Color boltColor = (boltDist < 0.030f) ? Color.white : new Color(0.25f, 0.90f, 1.0f);
                        sphereColor = Color.Lerp(sphereColor, boltColor, intensity * 0.95f);
                    }
                    break;
                }

                case BallSkinType.GoldenCrown:
                {
                    // Imperial Royal Golden Crown
                    if (IsInsideCrown(dx, dy, out float crownAlpha, out bool isJewel))
                    {
                        Color crownCol = isJewel ? Color.white : new Color(1.0f, 0.84f, 0.15f);
                        sphereColor = Color.Lerp(sphereColor, crownCol, crownAlpha * 0.95f);
                    }
                    break;
                }

                case BallSkinType.RainbowPrism:
                {
                    // Prismatic Holographic Sheen
                    float rainbowT = Mathf.Repeat(angle / (2f * Mathf.PI) + r * 0.5f, 1.0f);
                    Color rainbow = Color.HSVToRGB(rainbowT, 0.80f, 1.0f);
                    sphereColor = Color.Lerp(sphereColor, rainbow, 0.45f * z);
                    break;
                }
            }

            return sphereColor;
        }

        private static float DistanceToLightningBolt(float px, float py)
        {
            Vector2 p = new Vector2(px, py);
            Vector2 p1 = new Vector2(0.12f, 0.58f);
            Vector2 p2 = new Vector2(-0.06f, 0.10f);
            Vector2 p3 = new Vector2(0.10f, -0.06f);
            Vector2 p4 = new Vector2(-0.14f, -0.58f);

            float d1 = DistToSegment(p, p1, p2);
            float d2 = DistToSegment(p, p2, p3);
            float d3 = DistToSegment(p, p3, p4);

            return Mathf.Min(d1, Mathf.Min(d2, d3));
        }

        private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
            return Vector2.Distance(p, a + t * ab);
        }

        private static bool IsInsideCrown(float x, float y, out float alpha, out bool isJewel)
        {
            alpha = 0f;
            isJewel = false;

            Vector2[] jewels = new Vector2[] { new Vector2(-0.28f, 0.22f), new Vector2(0f, 0.32f), new Vector2(0.28f, 0.22f) };
            for (int i = 0; i < jewels.Length; i++)
            {
                if (Vector2.Distance(new Vector2(x, y), jewels[i]) <= 0.055f)
                {
                    alpha = 1f;
                    isJewel = true;
                    return true;
                }
            }

            if (x >= -0.32f && x <= 0.32f && y >= -0.22f && y <= -0.10f)
            {
                alpha = 1f;
                return true;
            }

            if (x >= -0.30f && x <= 0.30f && y >= -0.10f)
            {
                float peakProfile;
                float ax = Mathf.Abs(x);
                if (ax <= 0.14f)
                {
                    peakProfile = Mathf.Lerp(0.30f, 0.02f, ax / 0.14f);
                }
                else
                {
                    peakProfile = Mathf.Lerp(0.02f, 0.20f, (ax - 0.14f) / 0.14f);
                }

                if (y <= peakProfile)
                {
                    alpha = 1f;
                    return true;
                }
            }

            return false;
        }
private static Texture2D CreateCrispTexture(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, true)
            {
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        /// <summary>
        /// Generates a recessed circular socket/dish for empty board slots.
        /// </summary>
        public static Sprite GetEmptyCellSprite()
        {
            const string key = "CircularSocket";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            int size = 128;
            float radius = 56f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            Texture2D tex = CreateCrispTexture(size, size);

            Color innerSocket = new Color(0.08f, 0.11f, 0.19f, 1.0f); // Deep dark socket matching board
            Color rimHighlight = new Color(0.18f, 0.25f, 0.40f, 1.0f); // Beveled socket rim
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center.x) / radius;
                    float dy = (y - center.y) / radius;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > 1.04f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01((1.02f - dist) * 24f);

                    // Recessed pocket shading: top is in shadow, bottom has subtle rim
                    float cavityShade = Mathf.Lerp(0.75f, 1.15f, (dy + 1f) * 0.5f);
                    Color col = (dist > 0.82f) ? rimHighlight : innerSocket * cavityShade;
                    col.a = alpha;

                    pixels[y * size + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            s_SpriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite GetPreviewCellSprite(Color baseColor, BallSkinType? skinType = null)
        {
            BallSkinType effectiveSkin = skinType.HasValue ? skinType.Value : SkinManager.CurrentSkin;
            string key = $"SpherePreview_{effectiveSkin}_{ColorUtility.ToHtmlStringRGBA(baseColor)}";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            Color ghostColor = baseColor;
            ghostColor.a = 0.52f;
            Sprite ghost = GetBlockSprite(ghostColor, effectiveSkin);
            s_SpriteCache[key] = ghost;
            return ghost;
        }

        /// <summary>
        /// Generates a pulsating glowing ring highlight used for pre-blast line clear anticipation.
        /// </summary>
        public static Sprite GetLineHighlightSprite()
        {
            const string key = "CellLineHighlightGlowRing";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            int size = 128;
            float radius = 56f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            Texture2D tex = CreateCrispTexture(size, size);
            Color[] pixels = new Color[size * size];

            Color glowCore = new Color(1.0f, 0.92f, 0.35f, 0.95f); // Brilliant Gold
            Color glowInner = new Color(1.0f, 0.70f, 0.15f, 0.40f); // Warm Amber Fill

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float normalized = dist / radius;

                    if (normalized > 1.15f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    // Ring band between 0.82 and 1.02
                    float ringDist = Mathf.Abs(normalized - 0.92f);
                    float ringAlpha = Mathf.Clamp01(1f - ringDist / 0.14f);

                    // Soft luminous inner wash
                    float innerAlpha = Mathf.Clamp01(1f - normalized) * 0.35f;

                    Color col = Color.Lerp(glowInner, glowCore, ringAlpha);
                    col.a = Mathf.Max(ringAlpha * 0.95f, innerAlpha);

                    pixels[y * size + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            s_SpriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite GetBoardBackgroundSprite()
        {
            const string key = "BoardBackground8x8";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            int size = 256;
            int cornerRadius = 24;
            Texture2D tex = CreateCrispTexture(size, size);

            Color boardFill = new Color(0.05f, 0.07f, 0.13f, 1.0f);
            Color boardBorder = new Color(0.13f, 0.17f, 0.30f, 1.0f);

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = GetRoundedRectDistance(x, y, size, size, cornerRadius);
                    if (dist > 1.0f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01((1.0f - dist) * 16f);
                    Color col = (dist > 0.92f) ? boardBorder : boardFill;
                    col.a = alpha;
                    pixels[y * size + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            s_SpriteCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Generates a clean royal blue backdrop matching the Block Blast screenshot.
        /// </summary>
        public static Sprite GetCosmicStarryBackgroundSprite()
        {
            const string key = "RoyalBlueBackdrop";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            int size = 256;
            Texture2D tex = CreateCrispTexture(size, size);

            Color centerColor = new Color(0.24f, 0.32f, 0.68f, 1.0f); // Clean Royal Blue Center
            Color edgeColor = new Color(0.20f, 0.27f, 0.60f, 1.0f);   // Matching Blue Perimeter
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxR = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center.x) / maxR;
                    float dy = (y - center.y) / maxR;
                    float dist = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));

                    float t = dist * dist * (3f - 2f * dist);
                    pixels[y * size + x] = Color.Lerp(centerColor, edgeColor, t);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            s_SpriteCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Generates a sparkling 4-point diamond star shard for line-clear explosions.
        /// </summary>
        public static Sprite GetSparkleShardSprite()
        {
            const string key = "SparkleShard_HD";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            int size = 64;
            Texture2D tex = CreateCrispTexture(size, size);
            Color[] pixels = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float radius = 28f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = Mathf.Abs(x - c.x) / radius;
                    float ny = Mathf.Abs(y - c.y) / radius;

                    // 4-point star formula: sqrt(x) + sqrt(y) <= 1
                    float starVal = Mathf.Sqrt(nx) + Mathf.Sqrt(ny);
                    if (starVal > 1.05f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    float a = Mathf.Clamp01((1.05f - starVal) * 12f);
                    float distCenter = Mathf.Sqrt(nx * nx + ny * ny);
                    Color col = Color.Lerp(Color.white, new Color(1f, 1f, 1f, 0.7f), distCenter);
                    col.a *= a;
                    pixels[y * size + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            s_SpriteCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Generates an expanding shockwave ring for 3x3 sector clears and combo blasts.
        /// </summary>
        public static Sprite GetShockwaveRingSprite()
        {
            const string key = "ShockwaveRing_HD";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            int size = 128;
            Texture2D tex = CreateCrispTexture(size, size);
            Color[] pixels = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float maxR = 60f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - c.x) / maxR;
                    float dy = (y - c.y) / maxR;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    if (r < 0.65f || r > 1.0f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    float distFromPeak = Mathf.Abs(r - 0.85f) / 0.15f;
                    float alpha = Mathf.Clamp01(1.0f - distFromPeak);
                    alpha = alpha * alpha;

                    Color col = Color.white;
                    col.a = alpha;
                    pixels[y * size + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f);
            s_SpriteCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Generates a soft ambient floating stardust glow orb.
        /// </summary>
        public static Sprite GetSoftGlowOrbSprite()
        {
            const string key = "SoftGlowOrb_HD";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            int size = 64;
            Texture2D tex = CreateCrispTexture(size, size);
            Color[] pixels = new Color[size * size];
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float maxR = 28f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - c.x) / maxR;
                    float dy = (y - c.y) / maxR;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    if (r > 1.0f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    float a = Mathf.Pow(Mathf.Clamp01(1f - r), 1.8f);
                    Color col = Color.white;
                    col.a = a;
                    pixels[y * size + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            s_SpriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite GetRoundedButtonSprite(Color fillColor, Color borderColor, float borderRatio = 0.08f)
        {
            string key = $"Btn_{ColorUtility.ToHtmlStringRGBA(fillColor)}_{ColorUtility.ToHtmlStringRGBA(borderColor)}_{borderRatio:F2}";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            int size = 128;
            int radius = 28;
            Texture2D tex = CreateCrispTexture(size, size);

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = GetRoundedRectDistance(x, y, size, size, radius);
                    if (dist > 1.0f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01((1.0f - dist) * 20f);
                    float borderThreshold = 1.0f - borderRatio;
                    Color c = (dist >= borderThreshold) ? borderColor : fillColor;
                    c.a *= alpha;
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);

            Vector4 border = new Vector4(radius + 4, radius + 4, radius + 4, radius + 4);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            s_SpriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite Get3DPillButtonSprite(Color baseColor, Color shadowColor)
        {
            string key = $"Btn3D_{ColorUtility.ToHtmlStringRGBA(baseColor)}_{ColorUtility.ToHtmlStringRGBA(shadowColor)}";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            int size = 128;
            int radius = 32;
            Texture2D tex = CreateCrispTexture(size, size);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = GetRoundedRectDistance(x, y, size, size, radius);
                    if (dist > 1.0f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01((1.0f - dist) * 20f);
                    float normY = (float)y / size;

                    Color c;
                    // Bottom 18% is tactile 3D shadow bevel
                    if (normY < 0.16f)
                    {
                        c = shadowColor;
                    }
                    else
                    {
                        // Top face with subtle specular gradient
                        float topGrad = (normY - 0.16f) / 0.84f;
                        c = Color.Lerp(baseColor, baseColor + Color.white * 0.14f, topGrad);
                    }

                    c.a *= alpha;
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);

            Vector4 border = new Vector4(radius + 2, radius + 2, radius + 2, radius + 2);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            s_SpriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite GetChunkyBlockBlastButtonSprite(Color baseColor, Color shadowColor, Color rimHighlight)
        {
            string key = $"BtnChunkyBB_{ColorUtility.ToHtmlStringRGBA(baseColor)}_{ColorUtility.ToHtmlStringRGBA(shadowColor)}_{ColorUtility.ToHtmlStringRGBA(rimHighlight)}";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            int size = 128;
            int radius = 32;
            Texture2D tex = CreateCrispTexture(size, size);
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = GetRoundedRectDistance(x, y, size, size, radius);
                    if (dist > 1.0f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01((1.0f - dist) * 20f);
                    float normY = (float)y / size;

                    Color c;
                    // Bottom 18% is tactile 3D shadow bevel shelf
                    if (normY < 0.17f)
                    {
                        c = shadowColor;
                    }
                    else
                    {
                        // Main front face with rich vertical gradient
                        float faceT = (normY - 0.17f) / 0.83f;
                        c = Color.Lerp(baseColor, baseColor + Color.white * 0.14f, faceT);

                        // Top rim specular highlight (radiant candy shine along top curve)
                        if (faceT > 0.86f)
                        {
                            float rimT = (faceT - 0.86f) / 0.14f;
                            c = Color.Lerp(c, rimHighlight, rimT * 0.70f);
                        }
                    }

                    c.a *= alpha;
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);

            Vector4 border = new Vector4(radius + 2, radius + 2, radius + 2, radius + 2);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            s_SpriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite GetHomeBlueGradientSprite()
        {
            const string key = "HomeBlueGradient";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            int w = 64;
            int h = 256;
            Texture2D tex = CreateCrispTexture(w, h);
            Color[] pixels = new Color[w * h];

            Color topCol = new Color(0.11f, 0.42f, 0.88f);    // Vibrant Electric Blue
            Color midCol = new Color(0.10f, 0.33f, 0.76f);    // Deep Royal Blue
            Color botCol = new Color(0.08f, 0.24f, 0.60f);    // Bottom Navy Blue

            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                Color c = (t > 0.5f) ? Color.Lerp(midCol, topCol, (t - 0.5f) * 2f) : Color.Lerp(botCol, midCol, t * 2f);
                for (int x = 0; x < w; x++)
                {
                    pixels[y * w + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            s_SpriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite GetPanelSprite(Color fillColor, Color borderColor, float borderThickness = 2.5f, int cornerRadius = 24)
        {
            string key = $"Panel_{ColorUtility.ToHtmlStringRGBA(fillColor)}_{ColorUtility.ToHtmlStringRGBA(borderColor)}_{borderThickness:F1}_{cornerRadius}";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            int size = 128;
            int radius = Mathf.Clamp(cornerRadius, 12, 36);
            Texture2D tex = CreateCrispTexture(size, size);

            Color[] pixels = new Color[size * size];
            float borderRatio = borderThickness / radius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = GetRoundedRectDistance(x, y, size, size, radius);
                    if (dist > 1.0f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01((1.0f - dist) * 20f);
                    float borderThreshold = 1.0f - borderRatio;
                    Color c = (dist >= borderThreshold) ? borderColor : fillColor;
                    c.a *= alpha;
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);

            int sliceMargin = radius + 4;
            Vector4 border = new Vector4(sliceMargin, sliceMargin, sliceMargin, sliceMargin);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            s_SpriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite GetPillBadgeSprite(Color fillColor)
        {
            string key = $"PillBadge_{ColorUtility.ToHtmlStringRGBA(fillColor)}";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            int width = 128;
            int height = 56;
            int radius = 26;
            Texture2D tex = CreateCrispTexture(width, height);

            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = GetRoundedRectDistance(x, y, width, height, radius);
                    if (dist > 1.0f)
                    {
                        pixels[y * width + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01((1.0f - dist) * 20f);
                    Color c = fillColor;
                    c.a *= alpha;
                    pixels[y * width + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), height);
            s_SpriteCache[key] = sprite;
            return sprite;
        }

        public static Sprite GetCircleButtonSprite(Color fillColor, Color borderColor, float borderRatio = 0.08f)
        {
            string key = $"CircleBtn_{ColorUtility.ToHtmlStringRGBA(fillColor)}_{ColorUtility.ToHtmlStringRGBA(borderColor)}_{borderRatio:F2}";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            int size = 128;
            Texture2D tex = CreateCrispTexture(size, size);
            Color[] pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float radius = (size - 6) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    if (dist > 1.0f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    float alpha = Mathf.Clamp01((1.0f - dist) * 16f);
                    float borderThreshold = 1.0f - borderRatio;
                    Color c = (dist >= borderThreshold) ? borderColor : fillColor;
                    c.a *= alpha;
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            s_SpriteCache[key] = sprite;
            return sprite;
        }

        private static float GetRoundedRectDistance(int x, int y, int width, int height, int radius)
        {
            int cx = Mathf.Clamp(x, radius, width - 1 - radius);
            int cy = Mathf.Clamp(y, radius, height - 1 - radius);
            float dx = x - cx;
            float dy = y - cy;
            return Mathf.Sqrt(dx * dx + dy * dy) / radius;
        }

        /// <summary>
        /// Generates a blazing neon laser beam sprite with a bright white core and soft glowing aura.
        /// Stretches along cleared rows and columns for high-impact sweep blast animations.
        /// </summary>
        public static Sprite GetLaserBeamSprite()
        {
            const string key = "LaserBeam_HD";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            int width = 128;
            int height = 32;
            Texture2D tex = CreateCrispTexture(width, height);
            Color[] pixels = new Color[width * height];
            float halfH = (height - 1) * 0.5f;
            float halfW = (width - 1) * 0.5f;

            for (int y = 0; y < height; y++)
            {
                float ny = Mathf.Abs((y - halfH) / halfH);
                float core = Mathf.Pow(Mathf.Clamp01(1f - ny), 6f);
                float glow = Mathf.Pow(Mathf.Clamp01(1f - ny), 1.6f);

                for (int x = 0; x < width; x++)
                {
                    float nx = Mathf.Abs((x - halfW) / halfW);
                    float xFade = Mathf.Clamp01((1f - nx) * 5f);

                    float totalAlpha = (core * 0.7f + glow * 0.3f) * xFade;
                    Color col = Color.Lerp(Color.white, new Color(1f, 1f, 1f, 0.85f), glow);
                    col.a = Mathf.Clamp01(totalAlpha);
                    pixels[y * width + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f);
            s_SpriteCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Generates a dazzling 4-point lens flare star with glowing circular core.
        /// Used for line intersections and high-energy impact blasts.
        /// </summary>
        public static Sprite GetLensFlareStarSprite()
        {
            const string key = "LensFlareStar_HD";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            int size = 128;
            Texture2D tex = CreateCrispTexture(size, size);
            Color[] pixels = new Color[size * size];
            float c = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                float dy = (y - c) / c;
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - c) / c;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    // Central circular glow
                    float coreGlow = Mathf.Pow(Mathf.Clamp01(1f - r * 2.2f), 3f);

                    // Horizontal and vertical cross flares
                    float rayX = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(dy) * 9f), 2f) * Mathf.Clamp01(1f - Mathf.Abs(dx));
                    float rayY = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(dx) * 9f), 2f) * Mathf.Clamp01(1f - Mathf.Abs(dy));

                    // Diagonal softer spikes
                    float d1 = Mathf.Abs(dx - dy) * 0.7071f;
                    float d2 = Mathf.Abs(dx + dy) * 0.7071f;
                    float diag1 = Mathf.Pow(Mathf.Clamp01(1f - d1 * 10f), 2f) * Mathf.Clamp01(1f - r * 1.5f);
                    float diag2 = Mathf.Pow(Mathf.Clamp01(1f - d2 * 10f), 2f) * Mathf.Clamp01(1f - r * 1.5f);

                    float total = coreGlow + (rayX + rayY) * 0.9f + (diag1 + diag2) * 0.45f;
                    total = Mathf.Clamp01(total);

                    Color col = Color.white;
                    col.a = total;
                    pixels[y * size + x] = col;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
            s_SpriteCache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Ultra-crisp 4K 3D Chubby Cartoon Golden Crown matching Block Blast mobile game crown.
        /// </summary>
        public static Sprite GetChubbyCrownHDSprite()
        {
            const string key = "Icon_ChubbyGoldenCrown_4K_HD";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            Sprite loaded = Resources.Load<Sprite>("ChubbyGoldenCrownHD");
            if (loaded != null)
            {
                s_SpriteCache[key] = loaded;
                return loaded;
            }

            Texture2D tex = Resources.Load<Texture2D>("ChubbyGoldenCrownHD");
            if (tex == null)
            {
                string path = System.IO.Path.Combine(Application.dataPath, "Resources", "ChubbyGoldenCrownHD.png");
                if (System.IO.File.Exists(path))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.LoadImage(bytes);
                }
            }

            if (tex != null)
            {
                tex.filterMode = FilterMode.Bilinear;
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                s_SpriteCache[key] = sprite;
                return sprite;
            }

            return GemIconFactory.GetChubbyCrownSprite();
        }

        /// <summary>
        /// Ultra-crisp 4K 3D Chubby Candy Balloon Title Logo matching Block Blast mobile game aesthetic.
        /// </summary>
        public static Sprite GetSphereBlastTitleLogoSprite()
        {
            const string key = "Logo_SphereBlast_4K_HD";
            if (s_SpriteCache.TryGetValue(key, out Sprite cached) && cached != null && cached.texture != null) return cached;

            Sprite loaded = Resources.Load<Sprite>("SphereBlastLogoHD");
            if (loaded != null && loaded.texture != null)
            {
                // If the sprite was sliced into sub-sprites (e.g. cutting off BLAST), create a full-coverage sprite
                if (loaded.rect.height < loaded.texture.height * 0.95f || loaded.rect.width < loaded.texture.width * 0.95f)
                {
                    Sprite fullSprite = Sprite.Create(
                        loaded.texture,
                        new Rect(0, 0, loaded.texture.width, loaded.texture.height),
                        new Vector2(0.5f, 0.5f),
                        100f
                    );
                    s_SpriteCache[key] = fullSprite;
                    return fullSprite;
                }

                s_SpriteCache[key] = loaded;
                return loaded;
            }

            Texture2D tex = Resources.Load<Texture2D>("SphereBlastLogoHD");
            if (tex == null)
            {
                string path = System.IO.Path.Combine(Application.dataPath, "Resources", "SphereBlastLogoHD.png");
                if (System.IO.File.Exists(path))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    tex.LoadImage(bytes);
                }
            }

            if (tex != null)
            {
                tex.filterMode = FilterMode.Bilinear;
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                s_SpriteCache[key] = sprite;
                return sprite;
            }

            return null;
        }
    }
}
