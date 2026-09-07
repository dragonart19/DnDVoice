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

        // Vector strokes avoid platform-dependent emoji/font glyphs and external assets.
        public static void DrawIcon(Rect rect, UiIcon icon, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            void Line(float x, float y, float ex, float ey)
            {
                var start = rect.position + new Vector2(x, y) * (rect.width / 24f);
                var end = rect.position + new Vector2(ex, ey) * (rect.width / 24f);
                var matrix = GUI.matrix;
                GUIUtility.RotateAroundPivot(Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg, start);
                AppUiTheme.DrawRect(new Rect(start.x, start.y - 1f, Vector2.Distance(start, end), 2f), color);
                GUI.matrix = matrix;
            }
            void Box(float x, float y, float w, float h) { Line(x,y,x+w,y); Line(x+w,y,x+w,y+h); Line(x+w,y+h,x,y+h); Line(x,y+h,x,y); }
            switch (icon)
            {
                case UiIcon.Menu: Line(3,5,21,5); Line(3,12,21,12); Line(3,19,21,19); break;
                case UiIcon.Close: Line(5,5,19,19); Line(19,5,5,19); break;
                case UiIcon.Select: Line(5,3,5,20); Line(5,3,19,14); Line(19,14,12,14); Line(12,14,5,20); break;
                case UiIcon.Wall: Box(2,5,20,15); Line(2,12,22,12); Line(9,5,9,12); Line(15,12,15,20); break;
                case UiIcon.Door: Box(5,3,14,19); Line(5,3,14,6); Line(14,6,14,22); Line(11,13,12,13); break;
                case UiIcon.Delete: Line(3,6,21,6); Box(6,6,12,15); Line(9,3,15,3); Line(10,10,10,17); Line(14,10,14,17); break;
                case UiIcon.ZoomIn: case UiIcon.ZoomOut:
                    Box(3,3,12,12); Line(15,15,22,22); Line(6,9,12,9); if (icon == UiIcon.ZoomIn) Line(9,6,9,12); break;
                case UiIcon.Reset: Box(3,3,18,18); Line(8,12,16,12); Line(12,8,12,16); break;
                case UiIcon.Move:
                    Line(2,12,22,12); Line(12,2,12,22); Line(2,12,6,8); Line(2,12,6,16); Line(22,12,18,8); Line(22,12,18,16);
                    Line(12,2,8,6); Line(12,2,16,6); Line(12,22,8,18); Line(12,22,16,18); break;
                case UiIcon.Rotate: Line(5,15,5,6); Line(5,6,18,6); Line(18,6,18,17); Line(18,17,9,17); Line(9,17,13,13); Line(9,17,13,21); break;
                case UiIcon.CloseRoom: Box(3,3,18,18); Line(8,12,11,15); Line(11,15,17,8); break;
                case UiIcon.Microphone: case UiIcon.Muted:
                    Box(9,2,6,12); Line(5,10,5,16); Line(5,16,19,16); Line(19,16,19,10); Line(12,16,12,22); Line(8,22,16,22);
                    if (icon == UiIcon.Muted) Line(2,2,22,22); break;
                case UiIcon.Kick: Box(3,3,9,18); Line(9,12,23,12); Line(23,12,18,7); Line(23,12,18,17); break;
            }
        }
    }
}
