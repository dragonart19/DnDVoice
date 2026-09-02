using UnityEngine;

namespace DndProximityVoice.UI
{
    /// <summary>
    /// Shared runtime IMGUI theme. Keeping the palette and controls here makes every
    /// screen feel like the same application while the prototype still uses IMGUI.
    /// </summary>
    public static class AppUiTheme
    {
        private const float MaximumUiScale = 1.16f;
        public static readonly Color Background = new Color32(8, 9, 9, 255);
        public static readonly Color Surface = new Color32(20, 19, 16, 250);
        public static readonly Color SurfaceRaised = new Color32(31, 28, 22, 252);
        public static readonly Color SurfaceSoft = new Color32(38, 34, 27, 245);
        public static readonly Color Stroke = new Color32(132, 100, 51, 175);
        public static readonly Color Text = new Color32(244, 234, 211, 255);
        public static readonly Color Muted = new Color32(181, 164, 132, 255);
        public static readonly Color Faint = new Color32(120, 105, 78, 255);
        public static readonly Color Accent = new Color32(166, 116, 47, 255);
        public static readonly Color AccentBright = new Color32(225, 181, 91, 255);
        public static readonly Color Success = new Color32(78, 184, 128, 255);
        public static readonly Color Warning = new Color32(226, 157, 58, 255);
        public static readonly Color Danger = new Color32(198, 78, 70, 255);

        private static bool initialized;
        private static Texture2D backdropTexture;
        private static Texture2D backdropGridTexture;
        private static Texture2D glowTexture;
        private static Texture2D vignetteTexture;
        private static Texture2D cardTexture;
        private static Texture2D raisedTexture;
        private static Texture2D softTexture;
        private static Texture2D inputTexture;
        private static Texture2D accentTexture;
        private static Texture2D accentHoverTexture;
        private static Texture2D accentPressedTexture;
        private static Texture2D secondaryTexture;
        private static Texture2D secondaryHoverTexture;
        private static Texture2D dangerTexture;
        private static Texture2D chipTexture;
        private static Texture2D controlTrackTexture;
        private static Texture2D controlThumbTexture;
        private static Texture2D whiteTexture;
        private static GUIStyle pillBoxStyle;

        public static GUIStyle Card { get; private set; }
        public static GUIStyle CardRaised { get; private set; }
        public static GUIStyle CardSoft { get; private set; }
        public static GUIStyle Title { get; private set; }
        public static GUIStyle TitleCentered { get; private set; }
        public static GUIStyle TitleCompact { get; private set; }
        public static GUIStyle Display { get; private set; }
        public static GUIStyle DisplayLeft { get; private set; }
        public static GUIStyle Heading { get; private set; }
        public static GUIStyle HeadingCentered { get; private set; }
        public static GUIStyle Body { get; private set; }
        public static GUIStyle BodyCentered { get; private set; }
        public static GUIStyle BodyBold { get; private set; }
        public static GUIStyle BodyBoldClip { get; private set; }
        public static GUIStyle Caption { get; private set; }
        public static GUIStyle CaptionCentered { get; private set; }
        public static GUIStyle CaptionRight { get; private set; }
        public static GUIStyle CaptionSmall { get; private set; }
        public static GUIStyle CaptionSmallCentered { get; private set; }
        public static GUIStyle Eyebrow { get; private set; }
        public static GUIStyle EyebrowCentered { get; private set; }
        public static GUIStyle EyebrowSmall { get; private set; }
        public static GUIStyle EyebrowSmallCentered { get; private set; }
        public static GUIStyle PillLabel { get; private set; }
        public static GUIStyle Code { get; private set; }
        public static GUIStyle CodeRightCompact { get; private set; }
        public static GUIStyle Input { get; private set; }
        public static GUIStyle PrimaryButton { get; private set; }
        public static GUIStyle SecondaryButton { get; private set; }
        public static GUIStyle DangerButton { get; private set; }
        public static GUIStyle IconButton { get; private set; }
        public static GUIStyle TokenLabel { get; private set; }
        public static GUIStyle TokenName { get; private set; }

        public static void Ensure()
        {
            if (initialized)
            {
                return;
            }

            backdropTexture = MakeVerticalGradient(Background, new Color32(24, 21, 16, 255));
            backdropGridTexture = MakeOrnamentTile(96);
            glowTexture = MakeRadialTexture(96, new Color32(255, 255, 255, 170));
            vignetteTexture = MakeVignetteTexture(128);
            cardTexture = MakeRoundedTexture(48, 9, Surface, Stroke, 1.3f);
            raisedTexture = MakeRoundedTexture(48, 9, SurfaceRaised, new Color32(176, 132, 62, 205), 1.5f);
            softTexture = MakeRoundedTexture(48, 8, SurfaceSoft, new Color32(122, 94, 53, 125), 1f);
            inputTexture = MakeRoundedTexture(48, 8, new Color32(12, 12, 10, 255), new Color32(151, 113, 56, 220), 1.5f);
            accentTexture = MakeRoundedTexture(48, 8, new Color32(124, 82, 30, 255), AccentBright, 1.4f);
            accentHoverTexture = MakeRoundedTexture(48, 8, new Color32(166, 116, 47, 255), new Color32(245, 209, 126, 240), 1.5f);
            accentPressedTexture = MakeRoundedTexture(48, 8, new Color32(93, 58, 21, 255), new Color32(210, 158, 73, 235), 1.4f);
            secondaryTexture = MakeRoundedTexture(48, 8, new Color32(39, 35, 28, 255), new Color32(120, 91, 50, 190), 1f);
            secondaryHoverTexture = MakeRoundedTexture(48, 8, new Color32(57, 49, 36, 255), new Color32(188, 143, 67, 220), 1.2f);
            dangerTexture = MakeRoundedTexture(48, 8, new Color32(79, 30, 27, 255), new Color32(206, 91, 76, 210), 1.2f);
            chipTexture = MakeRoundedTexture(32, 12, new Color32(46, 40, 30, 238), new Color32(144, 108, 55, 150), 1f);
            controlTrackTexture = MakeRoundedTexture(32, 7, new Color32(15, 14, 12, 255), new Color32(112, 83, 43, 190), 1f);
            controlThumbTexture = MakeRoundedTexture(32, 9, new Color32(154, 105, 39, 255), new Color32(231, 188, 98, 235), 1.2f);
            whiteTexture = MakeSolid(Color.white);

            Card = CreatePanelStyle(cardTexture, 12);
            CardRaised = CreatePanelStyle(raisedTexture, 12);
            CardSoft = CreatePanelStyle(softTexture, 11);
            pillBoxStyle = CreatePanelStyle(chipTexture, 14);

            Display = CreateLabel(30, FontStyle.Bold, Text, TextAnchor.MiddleCenter);
            Display.wordWrap = true;
            DisplayLeft = new GUIStyle(Display) { alignment = TextAnchor.MiddleLeft };
            Title = CreateLabel(22, FontStyle.Bold, Text, TextAnchor.MiddleLeft);
            TitleCentered = new GUIStyle(Title) { alignment = TextAnchor.MiddleCenter };
            TitleCompact = new GUIStyle(Title) { fontSize = 18 };
            Heading = CreateLabel(15, FontStyle.Bold, Text, TextAnchor.MiddleLeft);
            HeadingCentered = new GUIStyle(Heading) { alignment = TextAnchor.MiddleCenter };
            Body = CreateLabel(14, FontStyle.Normal, new Color32(221, 209, 184, 255), TextAnchor.MiddleLeft);
            Body.wordWrap = true;
            BodyCentered = new GUIStyle(Body) { alignment = TextAnchor.MiddleCenter };
            BodyBold = new GUIStyle(Body) { fontStyle = FontStyle.Bold };
            BodyBoldClip = new GUIStyle(BodyBold) { clipping = TextClipping.Clip, wordWrap = false };
            Caption = CreateLabel(12, FontStyle.Normal, Muted, TextAnchor.MiddleLeft);
            Caption.wordWrap = true;
            CaptionCentered = new GUIStyle(Caption) { alignment = TextAnchor.MiddleCenter };
            CaptionRight = new GUIStyle(Caption) { alignment = TextAnchor.MiddleRight };
            CaptionSmall = new GUIStyle(Caption) { fontSize = 11 };
            CaptionSmallCentered = new GUIStyle(CaptionSmall) { alignment = TextAnchor.MiddleCenter };
            Eyebrow = CreateLabel(11, FontStyle.Bold, AccentBright, TextAnchor.MiddleLeft);
            EyebrowCentered = new GUIStyle(Eyebrow) { alignment = TextAnchor.MiddleCenter };
            EyebrowSmall = new GUIStyle(Eyebrow) { fontSize = 9 };
            EyebrowSmallCentered = new GUIStyle(EyebrowSmall) { alignment = TextAnchor.MiddleCenter };
            PillLabel = new GUIStyle(EyebrowCentered);
            Code = CreateLabel(25, FontStyle.Bold, Text, TextAnchor.MiddleCenter);
            CodeRightCompact = new GUIStyle(Code) { alignment = TextAnchor.MiddleRight, fontSize = 17 };
            TokenLabel = CreateLabel(12, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            TokenName = CreateLabel(12, FontStyle.Bold, Text, TextAnchor.UpperCenter);
            TokenName.clipping = TextClipping.Overflow;

            Input = new GUIStyle(GUI.skin.textField)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                border = new RectOffset(14, 14, 14, 14),
                padding = new RectOffset(16, 16, 8, 8),
                normal = { background = inputTexture, textColor = Text },
                hover = { background = inputTexture, textColor = Text },
                focused = { background = inputTexture, textColor = Color.white }
            };

            PrimaryButton = CreateButtonStyle(accentTexture, accentHoverTexture, accentPressedTexture);
            SecondaryButton = CreateButtonStyle(secondaryTexture, secondaryHoverTexture, secondaryTexture);
            DangerButton = CreateButtonStyle(dangerTexture, secondaryHoverTexture, dangerTexture);
            IconButton = new GUIStyle(SecondaryButton) { fontSize = 22, padding = new RectOffset(0, 0, 0, 2) };
            ApplyControlSkin();

            // This guard is essential: OnGUI can run several times per rendered frame.
            // Without it every control recreated all native textures and styles on every event.
            initialized = true;
        }

        public static void DrawBackdrop(Rect viewport)
        {
            Ensure();
            GUI.DrawTexture(viewport, backdropTexture, ScaleMode.StretchToFill);

            var oldColor = GUI.color;
            GUI.color = new Color(0.88f, 0.59f, 0.22f, 0.18f);
            GUI.DrawTexture(
                new Rect(viewport.xMax - 520f, viewport.y - 250f, 720f, 720f),
                glowTexture,
                ScaleMode.StretchToFill,
                true);
            GUI.color = new Color(0.19f, 0.52f, 0.36f, 0.10f);
            GUI.DrawTexture(
                new Rect(viewport.x - 340f, viewport.yMax - 410f, 620f, 620f),
                glowTexture,
                ScaleMode.StretchToFill,
                true);
            GUI.color = oldColor;

            GUI.color = new Color(1f, 0.83f, 0.48f, 0.32f);
            GUI.DrawTextureWithTexCoords(
                viewport,
                backdropGridTexture,
                new Rect(0f, 0f, viewport.width / 96f, viewport.height / 96f),
                true);
            GUI.color = Color.white;
            GUI.DrawTexture(viewport, vignetteTexture, ScaleMode.StretchToFill, true);
            GUI.color = oldColor;
        }

        public static void DrawCard(Rect rect, bool raised = false, bool shadow = true)
        {
            Ensure();
            if (shadow)
            {
                var oldColor = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.34f);
                GUI.Box(new Rect(rect.x + 2f, rect.y + 8f, rect.width, rect.height), GUIContent.none, Card);
                GUI.color = oldColor;
            }

            GUI.Box(rect, GUIContent.none, raised ? CardRaised : Card);
            DrawFrameCorners(rect, raised ? 0.66f : 0.34f);
        }

        public static void DrawAccentBar(Rect rect)
        {
            DrawRect(rect, AccentBright);
        }

        public static void DrawDivider(Rect rect)
        {
            DrawRect(rect, new Color32(176, 132, 62, 105));
        }

        public static void DrawDot(Vector2 center, float diameter, Color color)
        {
            Ensure();
            var oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(
                new Rect(center.x - diameter * 0.5f, center.y - diameter * 0.5f, diameter, diameter),
                glowTexture,
                ScaleMode.StretchToFill,
                true);
            GUI.color = oldColor;
        }

        public static void DrawPill(Rect rect, string text, Color color, GUIStyle labelStyle = null)
        {
            Ensure();
            var oldColor = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, 0.62f);
            GUI.Box(rect, GUIContent.none, pillBoxStyle);
            GUI.color = oldColor;
            DrawLabel(rect, text, labelStyle ?? PillLabel, Color.Lerp(color, Color.white, 0.24f));
        }

        public static void DrawLabel(Rect rect, string text, GUIStyle style, Color color)
        {
            var previousContentColor = GUI.contentColor;
            GUI.contentColor = color;
            GUI.Label(rect, text, style);
            GUI.contentColor = previousContentColor;
        }

        public static void DrawRect(Rect rect, Color color)
        {
            Ensure();
            var oldColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whiteTexture);
            GUI.color = oldColor;
        }

        public static float BeginResponsive(float referenceWidth, float referenceHeight, out Rect viewport)
        {
            Ensure();
            var scale = Mathf.Min(Screen.width / referenceWidth, Screen.height / referenceHeight);
            scale = Mathf.Min(MaximumUiScale, Mathf.Max(0.55f, scale));
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            viewport = new Rect(0f, 0f, Screen.width / scale, Screen.height / scale);
            return scale;
        }

        private static GUIStyle CreatePanelStyle(Texture2D background, int border)
        {
            return new GUIStyle(GUI.skin.box)
            {
                border = new RectOffset(border, border, border, border),
                normal = { background = background }
            };
        }

        private static GUIStyle CreateLabel(int size, FontStyle style, Color color, TextAnchor alignment)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = style,
                alignment = alignment,
                normal = { textColor = color }
            };
        }

        private static GUIStyle CreateButtonStyle(Texture2D normal, Texture2D hover, Texture2D active)
        {
            return new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                border = new RectOffset(12, 12, 12, 12),
                padding = new RectOffset(16, 16, 8, 8),
                normal = { background = normal, textColor = Text },
                hover = { background = hover, textColor = Color.white },
                active = { background = active, textColor = Text },
                focused = { background = hover, textColor = Color.white }
            };
        }

        private static void DrawFrameCorners(Rect rect, float alpha)
        {
            var color = new Color(0.88f, 0.66f, 0.31f, alpha);
            const float length = 15f;
            const float inset = 7f;
            const float width = 1f;
            DrawRect(new Rect(rect.x + inset, rect.y + inset, length, width), color);
            DrawRect(new Rect(rect.x + inset, rect.y + inset, width, length), color);
            DrawRect(new Rect(rect.xMax - inset - length, rect.y + inset, length, width), color);
            DrawRect(new Rect(rect.xMax - inset - width, rect.y + inset, width, length), color);
            DrawRect(new Rect(rect.x + inset, rect.yMax - inset - width, length, width), color);
            DrawRect(new Rect(rect.x + inset, rect.yMax - inset - length, width, length), color);
            DrawRect(new Rect(rect.xMax - inset - length, rect.yMax - inset - width, length, width), color);
            DrawRect(new Rect(rect.xMax - inset - width, rect.yMax - inset - length, width, length), color);
        }

        private static void ApplyControlSkin()
        {
            ApplyBackground(GUI.skin.horizontalScrollbar, controlTrackTexture);
            ApplyBackground(GUI.skin.verticalScrollbar, controlTrackTexture);
            ApplyBackground(GUI.skin.horizontalScrollbarThumb, controlThumbTexture);
            ApplyBackground(GUI.skin.verticalScrollbarThumb, controlThumbTexture);
            ApplyBackground(GUI.skin.horizontalScrollbarLeftButton, secondaryTexture);
            ApplyBackground(GUI.skin.horizontalScrollbarRightButton, secondaryTexture);
            ApplyBackground(GUI.skin.verticalScrollbarUpButton, secondaryTexture);
            ApplyBackground(GUI.skin.verticalScrollbarDownButton, secondaryTexture);
            ApplyBackground(GUI.skin.horizontalSlider, controlTrackTexture);
            ApplyBackground(GUI.skin.horizontalSliderThumb, controlThumbTexture);
            GUI.skin.horizontalSliderThumb.fixedWidth = 18f;
            GUI.skin.horizontalSliderThumb.fixedHeight = 18f;
            GUI.skin.horizontalScrollbar.border = new RectOffset(8, 8, 8, 8);
            GUI.skin.verticalScrollbar.border = new RectOffset(8, 8, 8, 8);
            GUI.skin.horizontalScrollbarThumb.border = new RectOffset(8, 8, 8, 8);
            GUI.skin.verticalScrollbarThumb.border = new RectOffset(8, 8, 8, 8);
        }

        private static void ApplyBackground(GUIStyle style, Texture2D texture)
        {
            style.normal.background = texture;
            style.hover.background = texture;
            style.active.background = texture;
            style.focused.background = texture;
        }

        private static Texture2D MakeSolid(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D MakeVerticalGradient(Color top, Color bottom)
        {
            const int height = 128;
            var texture = new Texture2D(2, height, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            for (var y = 0; y < height; y++)
            {
                var color = Color.Lerp(bottom, top, y / (height - 1f));
                texture.SetPixel(0, y, color);
                texture.SetPixel(1, y, color);
            }

            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D MakeOrnamentTile(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            var clear = new Color32(0, 0, 0, 0);
            var faint = new Color32(255, 255, 255, 13);
            var rune = new Color32(255, 255, 255, 27);
            var center = size / 2;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var diagonal = Mathf.Abs(x - y) <= 0 || Mathf.Abs(x + y - (size - 1)) <= 0;
                    var diamond = Mathf.Abs(Mathf.Abs(x - center) + Mathf.Abs(y - center) - 8) <= 0;
                    pixels[y * size + x] = diamond ? rune : diagonal ? faint : clear;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D MakeVignetteTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var normalizedX = Mathf.Abs((x + 0.5f) / size * 2f - 1f);
                    var normalizedY = Mathf.Abs((y + 0.5f) / size * 2f - 1f);
                    var edge = Mathf.Clamp01((Mathf.Max(normalizedX, normalizedY) - 0.45f) / 0.55f);
                    edge = edge * edge * (3f - 2f * edge);
                    pixels[y * size + x] = new Color32(0, 0, 0, (byte)(edge * 188f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D MakeRadialTexture(int size, Color color)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            var radius = center;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    var fade = Mathf.Clamp01(1f - distance / radius);
                    fade = fade * fade * (3f - 2f * fade);
                    pixels[y * size + x] = new Color(
                        color.r,
                        color.g,
                        color.b,
                        color.a * fade);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D MakeRoundedTexture(
            int size,
            float radius,
            Color fill,
            Color stroke,
            float strokeWidth)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            var half = size * 0.5f;
            var innerRadius = Mathf.Max(0f, radius - strokeWidth);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var px = Mathf.Abs(x + 0.5f - half) - (half - radius);
                    var py = Mathf.Abs(y + 0.5f - half) - (half - radius);
                    var outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude;
                    var signedDistance = outside + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
                    var coverage = Mathf.Clamp01(0.8f - signedDistance);

                    var ipx = Mathf.Abs(x + 0.5f - half) - (half - innerRadius - strokeWidth);
                    var ipy = Mathf.Abs(y + 0.5f - half) - (half - innerRadius - strokeWidth);
                    var innerOutside = new Vector2(Mathf.Max(ipx, 0f), Mathf.Max(ipy, 0f)).magnitude;
                    var innerDistance = innerOutside + Mathf.Min(Mathf.Max(ipx, ipy), 0f) - innerRadius;
                    var innerCoverage = Mathf.Clamp01(0.8f - innerDistance);
                    var color = Color.Lerp(stroke, fill, innerCoverage);
                    color.a *= coverage;
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
