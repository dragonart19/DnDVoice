using DndProximityVoice.Core;
using DndProximityVoice.Discord;
using UnityEngine;

namespace DndProximityVoice.UI
{
    [DisallowMultipleComponent]
    public sealed class ProductModeOverlay : MonoBehaviour
    {
        private const float ReferenceWidth = 1280f;
        private const float ReferenceHeight = 800f;
        private const float PanelWidth = 920f;
        private const float PanelHeight = 560f;

        private DiscordAuthManager authManager;
        private ProductModeManager modeManager;

        private void Awake()
        {
            authManager = GetComponent<DiscordAuthManager>();
            modeManager = GetComponent<ProductModeManager>();
        }

        private void OnGUI()
        {
            if (authManager?.State != DiscordAuthState.Connected ||
                modeManager == null ||
                modeManager.HasSelection)
            {
                return;
            }

            var previousMatrix = GUI.matrix;
            AppUiTheme.BeginResponsive(ReferenceWidth, ReferenceHeight, out var viewport);
            AppUiTheme.DrawBackdrop(viewport);

            GUI.Label(
                new Rect(viewport.center.x - 260f, 34f, 520f, 34f),
                "D&D PROXIMITY VOICE",
                AppUiTheme.TitleCentered);
            GUI.Label(
                new Rect(viewport.center.x - 260f, 66f, 520f, 20f),
                "SCEGLI IL TUO SPAZIO DI GIOCO",
                AppUiTheme.EyebrowCentered);

            var panel = new Rect(
                viewport.center.x - PanelWidth * 0.5f,
                viewport.center.y - PanelHeight * 0.5f + 28f,
                PanelWidth,
                PanelHeight);
            AppUiTheme.DrawCard(panel, true);
            AppUiTheme.DrawAccentBar(new Rect(panel.x + 36f, panel.y, panel.width - 72f, 3f));

            GUI.Label(
                new Rect(panel.x + 48f, panel.y + 34f, panel.width - 96f, 38f),
                "Come vuoi preparare la sessione?",
                AppUiTheme.DisplayLeft);
            GUI.Label(
                new Rect(panel.x + 48f, panel.y + 76f, panel.width - 96f, 28f),
                "Le due modalità restano separate: puoi evolverle senza compromettere il tavolo stabile.",
                AppUiTheme.Body);
            AppUiTheme.DrawDivider(new Rect(panel.x + 48f, panel.y + 120f, panel.width - 96f, 1f));

            var optionY = panel.y + 146f;
            const float optionWidth = 384f;
            const float optionHeight = 330f;
            DrawTabletop2DCard(new Rect(panel.x + 48f, optionY, optionWidth, optionHeight));
            DrawWorldBuilder3DCard(new Rect(panel.xMax - 48f - optionWidth, optionY, optionWidth, optionHeight));

            GUI.Label(
                new Rect(panel.x + 48f, panel.yMax - 52f, panel.width - 96f, 24f),
                "La Build 1.0 resta protetta su main. Questo branch prepara l'architettura della V2.",
                AppUiTheme.CaptionCentered);
            GUI.matrix = previousMatrix;
        }

        private void DrawTabletop2DCard(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, AppUiTheme.CardSoft);
            AppUiTheme.DrawPill(
                new Rect(rect.x + 28f, rect.y + 24f, 92f, 28f),
                "DISPONIBILE",
                AppUiTheme.Success,
                AppUiTheme.EyebrowSmallCentered);
            GUI.Label(
                new Rect(rect.x + 28f, rect.y + 72f, rect.width - 56f, 34f),
                ProductModeCatalog.GetDisplayName(ProductMode.Tabletop2D),
                AppUiTheme.Title);
            GUI.Label(
                new Rect(rect.x + 28f, rect.y + 112f, rect.width - 56f, 72f),
                "La modalità attuale: mappa tattica, muri, porte, pedine e voce di prossimità.",
                AppUiTheme.Body);
            GUI.Label(
                new Rect(rect.x + 28f, rect.y + 198f, rect.width - 56f, 44f),
                "✓  Compatibile con le sessioni Build 1.0\n✓  Disponibile per DM e giocatori",
                AppUiTheme.Caption);

            if (GUI.Button(
                    new Rect(rect.x + 28f, rect.yMax - 72f, rect.width - 56f, 48f),
                    "CONTINUA IN 2D   →",
                    AppUiTheme.PrimaryButton))
            {
                modeManager.TrySelect(ProductMode.Tabletop2D);
            }
        }

        private static void DrawWorldBuilder3DCard(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, AppUiTheme.CardSoft);
            AppUiTheme.DrawPill(
                new Rect(rect.x + 28f, rect.y + 24f, 102f, 28f),
                "ROADMAP V2",
                AppUiTheme.Warning,
                AppUiTheme.EyebrowSmallCentered);
            GUI.Label(
                new Rect(rect.x + 28f, rect.y + 72f, rect.width - 56f, 34f),
                ProductModeCatalog.GetDisplayName(ProductMode.WorldBuilder3D),
                AppUiTheme.Title);
            GUI.Label(
                new Rect(rect.x + 28f, rect.y + 112f, rect.width - 56f, 72f),
                "Scene tridimensionali modulari con asset, NPC, luci e audio spaziale su più piani.",
                AppUiTheme.Body);
            GUI.Label(
                new Rect(rect.x + 28f, rect.y + 198f, rect.width - 56f, 44f),
                "•  Modulo separato dal tavolo 2D\n•  Non ancora disponibile in gioco",
                AppUiTheme.Caption);

            var previousEnabled = GUI.enabled;
            GUI.enabled = false;
            GUI.Button(
                new Rect(rect.x + 28f, rect.yMax - 72f, rect.width - 56f, 48f),
                "IN SVILUPPO",
                AppUiTheme.SecondaryButton);
            GUI.enabled = previousEnabled;
        }
    }
}
