using DndProximityVoice.UI;
using UnityEngine;

namespace DndProximityVoice.Discord
{
    [DisallowMultipleComponent]
    public sealed class DiscordLoginOverlay : MonoBehaviour
    {
        private const float ReferenceWidth = 1280f;
        private const float ReferenceHeight = 760f;
        private const float PanelWidth = 640f;
        private const float PanelHeight = 460f;

        private DiscordAuthManager authManager;

        private void Awake()
        {
            authManager = GetComponent<DiscordAuthManager>();
        }

        private void OnGUI()
        {
            if (authManager == null || authManager.State == DiscordAuthState.Connected)
            {
                return;
            }

            var previousMatrix = GUI.matrix;
            AppUiTheme.BeginResponsive(ReferenceWidth, ReferenceHeight, out var viewport);
            AppUiTheme.DrawBackdrop(viewport);
            DrawBrand(viewport);

            var panel = new Rect(
                viewport.center.x - PanelWidth * 0.5f,
                viewport.center.y - PanelHeight * 0.5f + 24f,
                PanelWidth,
                PanelHeight);
            AppUiTheme.DrawCard(panel, true);
            AppUiTheme.DrawAccentBar(new Rect(panel.x + 34f, panel.y, panel.width - 68f, 3f));

            GUI.Label(
                new Rect(panel.x + 56f, panel.y + 48f, panel.width - 112f, 72f),
                "La voce del party,\nnel posto giusto.",
                AppUiTheme.Display);
            GUI.Label(
                new Rect(panel.x + 84f, panel.y + 130f, panel.width - 168f, 48f),
                "Accedi con Discord per creare il tavolo virtuale e ascoltare ogni giocatore in base alla sua posizione.",
                AppUiTheme.BodyCentered);

            DrawStatus(new Rect(panel.x + 56f, panel.y + 202f, panel.width - 112f, 64f));

            GUI.enabled = authManager.CanBeginLogin;
            var buttonText = authManager.State == DiscordAuthState.Failed
                ? "RIPROVA CON DISCORD   →"
                : "CONTINUA CON DISCORD   →";
            if (GUI.Button(
                    new Rect(panel.x + 56f, panel.y + 300f, panel.width - 112f, 56f),
                    buttonText,
                    AppUiTheme.PrimaryButton))
            {
                authManager.BeginLogin();
            }

            GUI.enabled = true;
            GUI.Label(
                new Rect(panel.x + 56f, panel.y + 374f, panel.width - 112f, 38f),
                "Si aprirà Discord per una conferma sicura. La password non viene condivisa con l’app.",
                AppUiTheme.CaptionCentered);

            GUI.Label(
                new Rect(viewport.x + 28f, viewport.yMax - 36f, 360f, 22f),
                "D&D PROXIMITY VOICE  ·  BUILD 1.0  ·  DISCORD DIRECT",
                AppUiTheme.Caption);
            GUI.matrix = previousMatrix;
        }

        private static void DrawBrand(Rect viewport)
        {
            var brandRect = new Rect(viewport.center.x - 210f, 42f, 420f, 42f);
            AppUiTheme.DrawPill(
                new Rect(brandRect.x, brandRect.y + 6f, 30f, 30f),
                "D20",
                AppUiTheme.AccentBright,
                AppUiTheme.EyebrowSmallCentered);
            GUI.Label(
                new Rect(brandRect.x + 42f, brandRect.y, brandRect.width - 42f, brandRect.height),
                "D&D PROXIMITY VOICE",
                AppUiTheme.Title);
        }

        private void DrawStatus(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, AppUiTheme.CardSoft);
            var color = GetStatusColor();
            AppUiTheme.DrawDot(new Vector2(rect.x + 31f, rect.center.y), 15f, color);

            AppUiTheme.DrawLabel(
                new Rect(rect.x + 54f, rect.y + 8f, rect.width - 70f, 20f),
                GetStatusHeading(),
                AppUiTheme.Heading,
                color);
            GUI.Label(
                new Rect(rect.x + 54f, rect.y + 29f, rect.width - 70f, 27f),
                GetStatusText(),
                AppUiTheme.Caption);
        }

        private Color GetStatusColor()
        {
            switch (authManager.State)
            {
                case DiscordAuthState.ReadyToLogin:
                case DiscordAuthState.Connected:
                    return AppUiTheme.Success;
                case DiscordAuthState.Failed:
                    return AppUiTheme.Danger;
                default:
                    return AppUiTheme.Warning;
            }
        }

        private string GetStatusHeading()
        {
            switch (authManager.State)
            {
                case DiscordAuthState.ReadyToLogin:
                    return "Discord pronto";
                case DiscordAuthState.Authorizing:
                    return "In attesa di Discord";
                case DiscordAuthState.ExchangingToken:
                case DiscordAuthState.Connecting:
                    return "Connessione in corso";
                case DiscordAuthState.Failed:
                    return "Connessione non riuscita";
                default:
                    return "Preparazione dell’app";
            }
        }

        private string GetStatusText()
        {
            switch (authManager.State)
            {
                case DiscordAuthState.Initializing:
                    return "Caricamento dei servizi Discord…";
                case DiscordAuthState.ReadyToLogin:
                    return "Puoi collegare il tuo account e continuare.";
                case DiscordAuthState.Authorizing:
                    return "Completa l’autorizzazione nella finestra Discord.";
                case DiscordAuthState.ExchangingToken:
                    return "Autorizzazione ricevuta, quasi fatto…";
                case DiscordAuthState.Connecting:
                    return "Stiamo recuperando il tuo profilo Discord…";
                case DiscordAuthState.Connected:
                    return $"Connesso come {authManager.CurrentUser?.DisplayName ?? "utente Discord"}";
                case DiscordAuthState.Failed:
                    return string.IsNullOrWhiteSpace(authManager.ErrorMessage)
                        ? "Controlla che Discord sia aperto e riprova."
                        : authManager.ErrorMessage;
                default:
                    return string.Empty;
            }
        }
    }
}
