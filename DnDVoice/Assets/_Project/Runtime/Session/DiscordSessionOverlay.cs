using DndProximityVoice.Core;
using DndProximityVoice.Discord;
using DndProximityVoice.UI;
using UnityEngine;

namespace DndProximityVoice.Session
{
    [DisallowMultipleComponent]
    public sealed class DiscordSessionOverlay : MonoBehaviour
    {
        private const float ReferenceWidth = 1280f;
        private const float ReferenceHeight = 800f;
        private const float PanelWidth = 920f;
        private const float PanelHeight = 590f;

        private DiscordAuthManager authManager;
        private ProductModeManager productModeManager;
        private DiscordSessionManager sessionManager;
        private string joinCode = string.Empty;

        private void Awake()
        {
            authManager = GetComponent<DiscordAuthManager>();
            productModeManager = GetComponent<ProductModeManager>();
            sessionManager = GetComponent<DiscordSessionManager>();
        }

        private void OnGUI()
        {
            if (authManager?.State != DiscordAuthState.Connected ||
                productModeManager?.CurrentMode != ProductMode.Tabletop2D ||
                sessionManager == null ||
                sessionManager.State == DiscordSessionState.Joined)
            {
                return;
            }

            var previousMatrix = GUI.matrix;
            AppUiTheme.BeginResponsive(ReferenceWidth, ReferenceHeight, out var viewport);
            AppUiTheme.DrawBackdrop(viewport);
            DrawTopBrand(viewport);

            var panel = new Rect(
                viewport.center.x - PanelWidth * 0.5f,
                viewport.center.y - PanelHeight * 0.5f + 28f,
                PanelWidth,
                PanelHeight);
            AppUiTheme.DrawCard(panel, true);
            AppUiTheme.DrawAccentBar(new Rect(panel.x + 36f, panel.y, panel.width - 72f, 3f));

            switch (sessionManager.State)
            {
                case DiscordSessionState.Ready:
                    DrawReadyState(panel);
                    break;
                case DiscordSessionState.Joining:
                    DrawProgressState(panel, "Connessione al tavolo", "Stiamo verificando il codice e caricando il party…");
                    break;
                case DiscordSessionState.Leaving:
                    DrawProgressState(panel, "Chiusura della sessione", "Un momento, stiamo lasciando il tavolo in modo sicuro…");
                    break;
                case DiscordSessionState.Failed:
                    DrawErrorState(panel);
                    break;
                default:
                    DrawProgressState(panel, "Preparazione del tavolo", "Discord sta preparando la sessione…");
                    break;
            }

            GUI.matrix = previousMatrix;
        }

        private void DrawReadyState(Rect panel)
        {
            GUI.Label(
                new Rect(panel.x + 48f, panel.y + 38f, 500f, 34f),
                "Prepara il tavolo",
                AppUiTheme.DisplayLeft);
            GUI.Label(
                new Rect(panel.x + 48f, panel.y + 76f, 580f, 28f),
                "Crea una nuova avventura oppure raggiungi il party con il suo codice.",
                AppUiTheme.Body);

            DrawDiscordBadge(new Rect(panel.xMax - 286f, panel.y + 44f, 238f, 40f));
            if (GUI.Button(
                    new Rect(panel.xMax - 286f, panel.y + 90f, 238f, 28f),
                    "←  CAMBIA MODALITÀ",
                    AppUiTheme.SecondaryButton))
            {
                joinCode = string.Empty;
                productModeManager.ClearSelection();
            }

            AppUiTheme.DrawDivider(new Rect(panel.x + 48f, panel.y + 122f, panel.width - 96f, 1f));

            var optionY = panel.y + 152f;
            var optionWidth = 384f;
            var optionHeight = 360f;
            var createRect = new Rect(panel.x + 48f, optionY, optionWidth, optionHeight);
            var joinRect = new Rect(panel.xMax - 48f - optionWidth, optionY, optionWidth, optionHeight);
            DrawCreateCard(createRect);
            DrawJoinCard(joinRect);
        }

        private void DrawCreateCard(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, AppUiTheme.CardSoft);
            AppUiTheme.DrawPill(
                new Rect(rect.x + 28f, rect.y + 26f, 96f, 28f),
                "DUNGEON MASTER",
                AppUiTheme.Warning,
                AppUiTheme.EyebrowSmallCentered);
            GUI.Label(
                new Rect(rect.x + 28f, rect.y + 76f, rect.width - 56f, 32f),
                "Crea una sessione",
                AppUiTheme.Title);
            GUI.Label(
                new Rect(rect.x + 28f, rect.y + 116f, rect.width - 56f, 74f),
                "Riceverai un codice da condividere. Come DM potrai spostare le pedine e gestire il tavolo.",
                AppUiTheme.Body);

            GUI.Label(
                new Rect(rect.x + 28f, rect.y + 210f, rect.width - 56f, 46f),
                "✓  Codice privato di 6 caratteri\n✓  Controllo completo della mappa",
                AppUiTheme.Caption);

            if (GUI.Button(
                    new Rect(rect.x + 28f, rect.yMax - 76f, rect.width - 56f, 50f),
                    "CREA SESSIONE   →",
                    AppUiTheme.PrimaryButton))
            {
                sessionManager.CreateSession();
            }
        }

        private void DrawJoinCard(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, AppUiTheme.CardSoft);
            AppUiTheme.DrawPill(
                new Rect(rect.x + 28f, rect.y + 26f, 80f, 28f),
                "GIOCATORE",
                AppUiTheme.Success,
                AppUiTheme.EyebrowSmallCentered);
            GUI.Label(
                new Rect(rect.x + 28f, rect.y + 76f, rect.width - 56f, 32f),
                "Entra nel party",
                AppUiTheme.Title);
            GUI.Label(
                new Rect(rect.x + 28f, rect.y + 116f, rect.width - 56f, 46f),
                "Inserisci il codice ricevuto dal Dungeon Master.",
                AppUiTheme.Body);

            GUI.SetNextControlName("SessionCode");
            joinCode = SessionCode.Normalize(GUI.TextField(
                new Rect(rect.x + 28f, rect.y + 184f, rect.width - 56f, 54f),
                joinCode,
                SessionCode.Length,
                AppUiTheme.Input));
            AppUiTheme.DrawLabel(
                new Rect(rect.x + 28f, rect.y + 244f, rect.width - 56f, 22f),
                SessionCode.IsValid(joinCode)
                    ? "Codice pronto"
                    : $"{joinCode.Length}/{SessionCode.Length} caratteri",
                AppUiTheme.CaptionRight,
                SessionCode.IsValid(joinCode) ? AppUiTheme.Success : AppUiTheme.Muted);

            GUI.enabled = SessionCode.IsValid(joinCode);
            if (GUI.Button(
                    new Rect(rect.x + 28f, rect.yMax - 76f, rect.width - 56f, 50f),
                    "ENTRA NELLA SESSIONE   →",
                    AppUiTheme.PrimaryButton))
            {
                sessionManager.JoinSession(joinCode);
            }

            GUI.enabled = true;
        }

        private void DrawDiscordBadge(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, AppUiTheme.CardSoft);
            AppUiTheme.DrawDot(new Vector2(rect.x + 22f, rect.center.y), 13f, AppUiTheme.Success);
            AppUiTheme.DrawLabel(
                new Rect(rect.x + 40f, rect.y + 4f, rect.width - 50f, 16f),
                "DISCORD CONNESSO",
                AppUiTheme.EyebrowSmall,
                AppUiTheme.Success);
            GUI.Label(
                new Rect(rect.x + 40f, rect.y + 18f, rect.width - 50f, 18f),
                authManager.CurrentUser?.DisplayName ?? "Utente Discord",
                AppUiTheme.Caption);
        }

        private void DrawProgressState(Rect panel, string heading, string description)
        {
            var center = panel.center;
            AppUiTheme.DrawPill(
                new Rect(center.x - 48f, center.y - 102f, 96f, 32f),
                "IN CORSO",
                AppUiTheme.Warning,
                AppUiTheme.EyebrowCentered);
            GUI.Label(
                new Rect(panel.x + 80f, center.y - 50f, panel.width - 160f, 42f),
                heading,
                AppUiTheme.Display);
            GUI.Label(
                new Rect(panel.x + 150f, center.y + 4f, panel.width - 300f, 48f),
                description,
                AppUiTheme.BodyCentered);
            GUI.Label(
                new Rect(panel.x + 150f, center.y + 72f, panel.width - 300f, 24f),
                "Non chiudere l’applicazione.",
                AppUiTheme.CaptionCentered);
        }

        private void DrawErrorState(Rect panel)
        {
            var center = panel.center;
            AppUiTheme.DrawPill(
                new Rect(center.x - 46f, center.y - 150f, 92f, 32f),
                "ERRORE",
                AppUiTheme.Danger,
                AppUiTheme.EyebrowCentered);
            GUI.Label(
                new Rect(panel.x + 80f, center.y - 98f, panel.width - 160f, 42f),
                "Non è stato possibile continuare",
                AppUiTheme.Display);
            GUI.Box(
                new Rect(panel.x + 150f, center.y - 34f, panel.width - 300f, 88f),
                GUIContent.none,
                AppUiTheme.CardSoft);
            AppUiTheme.DrawLabel(
                new Rect(panel.x + 174f, center.y - 22f, panel.width - 348f, 64f),
                sessionManager.ErrorMessage,
                AppUiTheme.BodyCentered,
                AppUiTheme.Danger);
            if (GUI.Button(
                    new Rect(center.x - 150f, center.y + 90f, 300f, 50f),
                    "TORNA INDIETRO",
                    AppUiTheme.PrimaryButton))
            {
                sessionManager.DismissError();
            }
        }

        private static void DrawTopBrand(Rect viewport)
        {
            GUI.Label(
                new Rect(viewport.center.x - 240f, 34f, 480f, 34f),
                "D&D PROXIMITY VOICE",
                AppUiTheme.TitleCentered);
            GUI.Label(
                new Rect(viewport.center.x - 240f, 66f, 480f, 20f),
                "IL TUO TAVOLO VOCALE",
                AppUiTheme.EyebrowCentered);
        }
    }
}
