using System.Collections.Generic;
using UnityEngine;

namespace DndProximityVoice.UI
{
    public enum UiIcon { Select, Wall, Door, Delete, ZoomIn, ZoomOut, Reset, Move, Rotate, CloseRoom, Microphone, Muted, Kick, Menu, Close }

    /// <summary>IMGUI controls shared by login, session, map and drawers.</summary>
    public static class AppUiControls
    {
        private static readonly Stack<Rect> clips = new Stack<Rect>();
        private static bool pointer;
        private static string tooltip;
        private static Vector2 tooltipAnchor;
        private static string previousTooltip;
        private static float tooltipSince;
        private static readonly Dictionary<UiIcon, Texture2D> iconTextures = new Dictionary<UiIcon, Texture2D>();
        private static readonly HashSet<UiIcon> missingIconWarnings = new HashSet<UiIcon>();
        public static bool PointerEnabled { get; set; } = true;

        public static void BeginSurface()
        {
            pointer = false;
            tooltip = null;
            clips.Clear();
            PointerEnabled = true;
        }

        public static void EndSurface(Rect viewport)
        {
            if (Event.current.type != EventType.Repaint) return;
            AppUiPointer.SetPointer(pointer);
            if (previousTooltip != tooltip)
            {
                previousTooltip = tooltip;
                tooltipSince = Time.realtimeSinceStartup;
            }
            if (string.IsNullOrEmpty(tooltip) || Time.realtimeSinceStartup - tooltipSince < 0.45f) return;
            var width = Mathf.Min(300f, AppUiTheme.Caption.CalcSize(new GUIContent(tooltip)).x + 24f);
            var height = AppUiTheme.Caption.CalcHeight(new GUIContent(tooltip), width - 20f) + 16f;
            var anchor = GUIUtility.ScreenToGUIPoint(tooltipAnchor);
            var rect = PlacePopup(anchor, new Vector2(width, height), viewport, 18f);
            AppUiTheme.DrawCard(rect, true, false);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f), tooltip, AppUiTheme.Caption);
        }

        public static bool Button(Rect rect, string text, GUIStyle style) => Button(rect, new GUIContent(text), style);

        public static bool Button(Rect rect, GUIContent content, GUIStyle style)
        {
            Hover(rect, content.tooltip);
            return GUI.Button(rect, content, style);
        }

        public static bool IconButton(Rect rect, UiIcon icon, string hint, bool selected = false, bool danger = false)
        {
            var clicked = Button(rect, new GUIContent(string.Empty, hint), selected ? AppUiTheme.PrimaryButton : danger ? AppUiTheme.DangerButton : AppUiTheme.SecondaryButton);
            DrawIcon(new Rect(rect.center.x - 12f, rect.center.y - 12f, 24f, 24f), icon, GUI.enabled ? AppUiTheme.Text : AppUiTheme.Muted);
            return clicked;
        }

        public static void Hover(Rect rect, string hint = null)
        {
            if (!GUI.enabled || !PointerEnabled || !rect.Contains(Event.current.mousePosition)) return;
            var point = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            foreach (var clip in clips) if (!clip.Contains(point)) return;
            pointer = true;
            if (!string.IsNullOrEmpty(hint)) { tooltip = hint; tooltipAnchor = point; }
        }

        public static void ClearHover() { pointer = false; tooltip = null; }

        public static Vector2 BeginScrollView(Rect rect, Vector2 scroll, Rect content)
        {
            clips.Push(new Rect(GUIUtility.GUIToScreenPoint(rect.position), GUIUtility.GUIToScreenPoint(rect.max) - GUIUtility.GUIToScreenPoint(rect.position)));
            return GUI.BeginScrollView(rect, scroll, content);
        }

        public static void EndScrollView() { GUI.EndScrollView(); clips.Pop(); }

        public static Rect PlacePopup(Vector2 anchor, Vector2 size, Rect viewport, float gap = 12f)
        {
            size = Vector2.Min(size, new Vector2(Mathf.Max(0f, viewport.width - 12f), Mathf.Max(0f, viewport.height - 12f)));
            var y = anchor.y - size.y - gap;
            if (y < viewport.yMin + 6f) y = anchor.y + gap;
            return new Rect(Mathf.Clamp(anchor.x - size.x * 0.5f, viewport.xMin + 6f, viewport.xMax - size.x - 6f),
                Mathf.Clamp(y, viewport.yMin + 6f, viewport.yMax - size.y - 6f), size.x, size.y);
        }

        public static void DrawIcon(Rect rect, UiIcon icon, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;

            var texture = GetIconTexture(icon);
            if (texture == null)
            {
                if (missingIconWarnings.Add(icon)) Debug.LogWarning($"Icona UI non trovata: {icon}");
                return;
            }

            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
            GUI.color = previousColor;
        }

        private static Texture2D GetIconTexture(UiIcon icon)
        {
            if (iconTextures.TryGetValue(icon, out var texture)) return texture;

            string assetName;
            switch (icon)
            {
                case UiIcon.Select: assetName = "select"; break;
                case UiIcon.Wall: assetName = "wall"; break;
                case UiIcon.Door: assetName = "door"; break;
                case UiIcon.Delete: assetName = "delete"; break;
                case UiIcon.ZoomIn: assetName = "zoom-in"; break;
                case UiIcon.ZoomOut: assetName = "zoom-out"; break;
                case UiIcon.Reset: assetName = "reset"; break;
                case UiIcon.Move: assetName = "move"; break;
                case UiIcon.Rotate: assetName = "rotate"; break;
                case UiIcon.CloseRoom: assetName = "close-room"; break;
                case UiIcon.Microphone: assetName = "microphone"; break;
                case UiIcon.Muted: assetName = "muted"; break;
                case UiIcon.Kick: assetName = "kick"; break;
                case UiIcon.Menu: assetName = "menu"; break;
                case UiIcon.Close: assetName = "close"; break;
                default: assetName = icon.ToString().ToLowerInvariant(); break;
            }

            texture = Resources.Load<Texture2D>($"DndVoiceIcons/{assetName}");
            iconTextures[icon] = texture;
            return texture;
        }
    }
}
