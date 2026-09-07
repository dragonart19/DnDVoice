using UnityEngine;

namespace DndProximityVoice.UI
{
    [DisallowMultipleComponent]
    public sealed class AppUiPointer : MonoBehaviour
    {
        private static Texture2D hand;
        private static bool active;
        private static int lastFrame;

        public static void SetPointer(bool requested)
        {
            lastFrame = Time.frameCount;
            requested &= Application.isFocused;
            if (active == requested) return;
            if (requested && hand == null) hand = CreateHand();
            Cursor.SetCursor(requested ? hand : null, requested ? new Vector2(10f, 2f) : Vector2.zero, CursorMode.Auto);
            active = requested;
        }

        private void Update() { if (Time.frameCount - lastFrame > 1) SetPointer(false); }
        private void OnApplicationFocus(bool focused) { if (!focused) SetPointer(false); }
        private void OnDisable() { SetPointer(false); }
        private void OnDestroy()
        {
            SetPointer(false);
            if (hand != null) Destroy(hand);
            hand = null;
        }

        private static Texture2D CreateHand()
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = "DndVoice Hand", hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point };
            var pixels = new Color32[32 * 32];
            bool Inside(int x, int y) => (x >= 8 && x <= 12 && y >= 1 && y <= 23) ||
                (x >= 13 && x <= 17 && y >= 10 && y <= 26) || (x >= 18 && x <= 22 && y >= 12 && y <= 26) ||
                (x >= 23 && x <= 26 && y >= 15 && y <= 24) || (x >= 9 && x <= 23 && y >= 20 && y <= 29) ||
                (x >= 3 && x <= 9 && y >= 17 && y <= 21) || (x >= 5 && x <= 11 && y >= 21 && y <= 25);
            for (var y = 0; y < 32; y++) for (var x = 0; x < 32; x++)
                if (Inside(x, y)) pixels[(31-y)*32+x] = Inside(x-1,y) && Inside(x+1,y) && Inside(x,y-1) && Inside(x,y+1) ? new Color32(250,244,225,255) : new Color32(32,27,20,255);
            texture.SetPixels32(pixels);
            texture.Apply(false, false); // Hardware cursors require a readable RGBA32 texture.
            return texture;
        }
    }
}
