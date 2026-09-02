using System.Collections.Generic;
using DndProximityVoice.Discord;
using DndProximityVoice.Players;
using DndProximityVoice.Realtime;
using DndProximityVoice.Session;
using DndProximityVoice.UI;
using DndProximityVoice.Voice;
using UnityEngine;

namespace DndProximityVoice.Map
{
    [DisallowMultipleComponent]
    public sealed class ProximityMapOverlay : MonoBehaviour
    {
        private const float ReferenceWidth = 1180f;
        private const float ReferenceHeight = 720f;
        private const float OuterMargin = 16f;
        private const float HeaderHeight = 72f;
        private const float FooterHeight = 30f;
        private const float RightPanelWidth = 300f;
        private const float PanelGap = 12f;
        private const float TokenSize = 56f;
        private const float DefaultMapPixelsPerMeter = 28f;
        private const float MinimumMapPixelsPerMeter = 12f;
        private const float MaximumMapPixelsPerMeter = 84f;
        private const float MapZoomSensitivity = 0.12f;
        private const float MapToolbarHeight = 56f;
        private const float MapScrollbarSize = 18f;

        private DiscordAuthManager authManager;
        private DiscordSessionManager sessionManager;
        private DiscordVoiceManager voiceManager;
        private PlayerManager playerManager;
        private PositionSyncManager positionSyncManager;
        private TacticalMapManager tacticalMapManager;

        private ulong selectedPlayerId;
        private ulong draggingPlayerId;
        private bool burgerMenuOpen;
        private bool playersDrawerOpen;
        private bool savedMapsDrawerOpen;
        private bool wallBuildMode;
        private bool doorPlacementMode;
        private bool wallEraseMode;
        private bool wallDragActive;
        private Vector2 wallDragStart;
        private Vector2 wallDragCurrent;
        private bool wallChainActive;
        private Vector2 wallChainStart;
        private Vector2 wallChainEnd;
        private int wallChainSegmentCount;
        private int selectedWallId;
        private float wallThicknessMeters = 0.6f;
        private Vector2 playersScroll;
        private Vector2 savedMapsScroll;
        private Vector2 mapScroll;
        private Vector2 previousMapSize;
        private bool mapScrollInitialized;
        private float mapPixelsPerMeter = DefaultMapPixelsPerMeter;
        private readonly List<string> savedMapNames = new List<string>();
        private string savedMapNameInput = "Nuova mappa";
        private string savedMapMessage = string.Empty;
        private bool savedMapMessageIsError;
        private string pendingOverwriteMapName = string.Empty;
        private string pendingLoadMapName = string.Empty;
        private string pendingDeleteMapName = string.Empty;

        private Texture2D mapTexture;
        private Texture2D mapGridTexture;
        private Texture2D circleTexture;
        private Texture2D rangeTexture;
        private Texture2D selectionTexture;
        private Texture2D wallTexture;

        private void Awake()
        {
            authManager = GetComponent<DiscordAuthManager>();
            sessionManager = GetComponent<DiscordSessionManager>();
            voiceManager = GetComponent<DiscordVoiceManager>();
            playerManager = GetComponent<PlayerManager>();
            positionSyncManager = GetComponent<PositionSyncManager>();
            tacticalMapManager = GetComponent<TacticalMapManager>();
        }

        private void OnGUI()
        {
            if (sessionManager?.State != DiscordSessionState.Joined || playerManager == null)
            {
                return;
            }

            EnsureTextures();
            EnsureSelection();
            HandleVoiceModeShortcuts();

            var previousMatrix = GUI.matrix;
            AppUiTheme.BeginResponsive(ReferenceWidth, ReferenceHeight, out var viewport);
            AppUiTheme.DrawBackdrop(viewport);

            var root = new Rect(
                OuterMargin,
                OuterMargin,
                viewport.width - OuterMargin * 2f,
                viewport.height - OuterMargin * 2f);
            var headerRect = new Rect(root.x, root.y, root.width, HeaderHeight);
            var footerRect = new Rect(root.x, root.yMax - FooterHeight, root.width, FooterHeight);
            var bodyY = headerRect.yMax + PanelGap;
            var bodyHeight = footerRect.y - bodyY - PanelGap;
            var rightRect = new Rect(root.xMax - RightPanelWidth, bodyY, RightPanelWidth, bodyHeight);
            var mapRect = new Rect(
                root.x,
                bodyY,
                rightRect.x - root.x - PanelGap,
                bodyHeight);

            DrawMap(mapRect);
            DrawVoicePanel(rightRect);
            DrawHeader(headerRect);
            DrawFooter(footerRect);
            DrawBurgerMenu(root);

            GUI.matrix = previousMatrix;
        }

        private void DrawHeader(Rect rect)
        {
            AppUiTheme.DrawCard(rect, true, false);

            GUI.Label(
                new Rect(rect.x + 78f, rect.y + 12f, 320f, 26f),
                "D&D PROXIMITY VOICE",
                AppUiTheme.TitleCompact);
            GUI.Label(
                new Rect(rect.x + 78f, rect.y + 38f, 320f, 19f),
                playerManager.CanMovePlayers ? "TAVOLO DEL DUNGEON MASTER" : "TAVOLO DEL PARTY",
                AppUiTheme.Eyebrow);

            var codeRect = new Rect(rect.center.x - 112f, rect.y + 15f, 224f, 42f);
            GUI.Box(codeRect, GUIContent.none, AppUiTheme.CardSoft);
            GUI.Label(
                new Rect(codeRect.x + 12f, codeRect.y + 4f, 70f, 14f),
                "SESSIONE",
                AppUiTheme.EyebrowSmall);
            GUI.Label(
                new Rect(codeRect.x + 12f, codeRect.y + 14f, codeRect.width - 24f, 24f),
                FormatCode(sessionManager.CurrentSessionCode),
                AppUiTheme.CodeRightCompact);

            var userRect = new Rect(rect.xMax - 274f, rect.y + 15f, 250f, 42f);
            GUI.Box(userRect, GUIContent.none, AppUiTheme.CardSoft);
            AppUiTheme.DrawDot(new Vector2(userRect.x + 22f, userRect.center.y), 13f, AppUiTheme.Success);
            AppUiTheme.DrawLabel(
                new Rect(userRect.x + 40f, userRect.y + 4f, userRect.width - 50f, 15f),
                "DISCORD CONNESSO",
                AppUiTheme.EyebrowSmall,
                AppUiTheme.Success);
            GUI.Label(
                new Rect(userRect.x + 40f, userRect.y + 19f, userRect.width - 50f, 18f),
                authManager?.CurrentUser?.DisplayName ?? "Utente Discord",
                AppUiTheme.Caption);
        }

        private void DrawFooter(Rect rect)
        {
            GUI.Label(
                new Rect(rect.x + 8f, rect.y + 3f, 620f, 22f),
                playerManager.CanMovePlayers
                    ? "Trascina una pedina per spostarla  ·  selezionala per vedere il suo raggio vocale"
                    : "Il Dungeon Master controlla il movimento  ·  seleziona una pedina per ispezionarla",
                AppUiTheme.Caption);

            var voiceConnected = voiceManager?.State == DiscordVoiceState.Connected;
            AppUiTheme.DrawLabel(
                new Rect(rect.xMax - 500f, rect.y + 3f, 232f, 22f),
                voiceConnected ? "●  Voce Discord" : "○  Voce non connessa",
                AppUiTheme.CaptionRight,
                voiceConnected ? AppUiTheme.Success : AppUiTheme.Muted);

            var mapConnected = positionSyncManager?.State == PositionSyncState.Connected;
            var mapConnecting = positionSyncManager?.State == PositionSyncState.Connecting ||
                                positionSyncManager?.State == PositionSyncState.StartingHost;
            var connectedFriends = positionSyncManager?.ConnectedFriendCount ?? 0;
            var mapStatus = mapConnected
                ? positionSyncManager.IsHost && connectedFriends == 0
                    ? "●  Relay pronto · in attesa"
                    : $"●  Mappa sincronizzata · {connectedFriends}"
                : mapConnecting
                    ? "◌  Mappa in connessione…"
                    : "⚠  Mappa solo su questo PC";
            AppUiTheme.DrawLabel(
                new Rect(rect.xMax - 258f, rect.y + 3f, 250f, 22f),
                mapStatus,
                AppUiTheme.CaptionRight,
                mapConnected ? AppUiTheme.Success : mapConnecting ? AppUiTheme.Warning : AppUiTheme.Danger);
        }

        private void DrawPlayersDrawer(Rect menuRect)
        {
            if (!playersDrawerOpen)
            {
                return;
            }

            var rect = new Rect(menuRect.xMax + 10f, menuRect.y, 320f, menuRect.height);
            AppUiTheme.DrawCard(rect, false, false);
            GUI.Label(
                new Rect(rect.x + 20f, rect.y + 18f, rect.width - 40f, 24f),
                $"Giocatori connessi  ·  {playerManager.Players.Count}",
                AppUiTheme.Heading);
            GUI.Label(
                new Rect(rect.x + 20f, rect.y + 42f, rect.width - 40f, 34f),
                "Seleziona una pedina per mostrarla sulla mappa.",
                AppUiTheme.Caption);
            AppUiTheme.DrawDivider(new Rect(rect.x + 20f, rect.y + 82f, rect.width - 40f, 1f));

            const float bottomHeight = 18f;
            var scrollRect = new Rect(rect.x + 12f, rect.y + 94f, rect.width - 20f, rect.height - 94f - bottomHeight);
            var contentHeight = Mathf.Max(scrollRect.height, playerManager.Players.Count * 72f + 4f);
            playersScroll = GUI.BeginScrollView(
                scrollRect,
                playersScroll,
                new Rect(0f, 0f, scrollRect.width - 16f, contentHeight));

            var y = 2f;
            foreach (var player in playerManager.Players)
            {
                DrawPlayerListItem(player, new Rect(0f, y, scrollRect.width - 18f, 64f));
                y += 72f;
            }

            GUI.EndScrollView();

        }

        private void DrawPlayerListItem(PlayerData player, Rect rect)
        {
            GUI.Box(rect, GUIContent.none, AppUiTheme.CardSoft);
            if (selectedPlayerId == player.DiscordUserId)
            {
                AppUiTheme.DrawAccentBar(new Rect(rect.x, rect.y + 10f, 3f, rect.height - 20f));
            }

            DrawColoredCircle(new Rect(rect.x + 12f, rect.y + 13f, 38f, 38f), player.Color, GetInitials(player));
            GUI.Label(
                new Rect(rect.x + 60f, rect.y + 10f, rect.width - 132f, 23f),
                player.DisplayName,
                AppUiTheme.BodyBoldClip);
            AppUiTheme.DrawLabel(
                new Rect(rect.x + 60f, rect.y + 33f, rect.width - 132f, 18f),
                player.IsLocal ? "Questo sei tu" : player.IsConnected ? "Online" : "Non connesso",
                AppUiTheme.Caption,
                player.IsConnected ? AppUiTheme.Success : AppUiTheme.Muted);

            var role = player.IsDM ? "DM" : "GIOCATORE";
            if (player.PrivateGroup != PrivateVoiceGroup.None)
            {
                role += $" · G.{PrivateVoiceGroupRules.GetDisplayName(player.PrivateGroup)}";
            }
            var roleColor = player.IsDM ? AppUiTheme.Warning : AppUiTheme.Success;
            var roleWidth = player.IsDM ? 54f : 84f;
            if (player.PrivateGroup != PrivateVoiceGroup.None)
            {
                roleWidth += 42f;
            }
            AppUiTheme.DrawPill(
                new Rect(rect.xMax - 12f - roleWidth, rect.y + 20f, roleWidth, 24f),
                role,
                roleColor,
                AppUiTheme.EyebrowSmallCentered);

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                selectedPlayerId = player.DiscordUserId;
                playersDrawerOpen = false;
                savedMapsDrawerOpen = false;
                burgerMenuOpen = false;
            }
        }

        private void DrawSavedMapsDrawer(Rect menuRect)
        {
            if (!savedMapsDrawerOpen)
            {
                return;
            }

            var rect = new Rect(menuRect.xMax + 10f, menuRect.y, 360f, menuRect.height);
            AppUiTheme.DrawCard(rect, false, false);
            GUI.Label(
                new Rect(rect.x + 20f, rect.y + 18f, rect.width - 40f, 24f),
                "Mappe salvate",
                AppUiTheme.Heading);
            GUI.Label(
                new Rect(rect.x + 20f, rect.y + 42f, rect.width - 40f, 34f),
                "Conserva dimensioni, muri e porte sul tuo PC.",
                AppUiTheme.Caption);

            GUI.Label(
                new Rect(rect.x + 20f, rect.y + 78f, rect.width - 40f, 18f),
                "NOME MAPPA",
                AppUiTheme.EyebrowSmall);
            var nextName = GUI.TextField(
                new Rect(rect.x + 20f, rect.y + 100f, rect.width - 40f, 40f),
                savedMapNameInput,
                32,
                AppUiTheme.Input);
            if (nextName != savedMapNameInput)
            {
                savedMapNameInput = nextName;
                pendingOverwriteMapName = string.Empty;
            }

            var validName = MapSaveSerializer.TryNormalizeName(
                savedMapNameInput,
                out var normalizedInput,
                out _);
            var mapAlreadyExists = validName && SavedMapNameIsListed(normalizedInput);
            var overwriteConfirmed = mapAlreadyExists &&
                                     pendingOverwriteMapName == normalizedInput;
            var saveLabel = overwriteConfirmed
                ? "CONFERMA SOVRASCRITTURA"
                : mapAlreadyExists
                    ? "SOVRASCRIVI MAPPA"
                    : "SALVA MAPPA";
            if (GUI.Button(
                    new Rect(rect.x + 20f, rect.y + 150f, rect.width - 40f, 40f),
                    saveLabel,
                    overwriteConfirmed ? AppUiTheme.DangerButton : AppUiTheme.PrimaryButton))
            {
                if (!validName)
                {
                    MapSaveSerializer.TryNormalizeName(
                        savedMapNameInput,
                        out _,
                        out var validationError);
                    SetSavedMapMessage(validationError, true);
                }
                else if (mapAlreadyExists && !overwriteConfirmed)
                {
                    pendingOverwriteMapName = normalizedInput;
                    SetSavedMapMessage(
                        "Premi di nuovo per sostituire la mappa salvata.",
                        true);
                }
                else if (tacticalMapManager.TrySaveCurrentMap(
                             normalizedInput,
                             out var savedName))
                {
                    savedMapNameInput = savedName;
                    pendingOverwriteMapName = string.Empty;
                    RefreshSavedMaps();
                    SetSavedMapMessage($"«{savedName}» salvata correttamente.", false);
                }
                else
                {
                    SetSavedMapMessage(tacticalMapManager.LastError, true);
                }
            }

            if (!string.IsNullOrEmpty(savedMapMessage))
            {
                AppUiTheme.DrawLabel(
                    new Rect(rect.x + 20f, rect.y + 196f, rect.width - 40f, 32f),
                    savedMapMessage,
                    AppUiTheme.CaptionCentered,
                    savedMapMessageIsError ? AppUiTheme.Warning : AppUiTheme.Success);
            }

            AppUiTheme.DrawDivider(new Rect(rect.x + 20f, rect.y + 232f, rect.width - 40f, 1f));
            GUI.Label(
                new Rect(rect.x + 20f, rect.y + 242f, rect.width - 40f, 20f),
                $"ARCHIVIO  ·  {savedMapNames.Count}",
                AppUiTheme.Eyebrow);

            var scrollRect = new Rect(
                rect.x + 12f,
                rect.y + 270f,
                rect.width - 20f,
                rect.height - 286f);
            var contentHeight = Mathf.Max(scrollRect.height, savedMapNames.Count * 70f + 4f);
            savedMapsScroll = GUI.BeginScrollView(
                scrollRect,
                savedMapsScroll,
                new Rect(0f, 0f, scrollRect.width - 16f, contentHeight));

            if (savedMapNames.Count == 0)
            {
                GUI.Label(
                    new Rect(12f, 20f, scrollRect.width - 40f, 54f),
                    "Non hai ancora salvato nessuna mappa.",
                    AppUiTheme.CaptionCentered);
            }
            else
            {
                var y = 2f;
                foreach (var mapName in savedMapNames)
                {
                    DrawSavedMapListItem(
                        mapName,
                        new Rect(0f, y, scrollRect.width - 18f, 62f));
                    y += 70f;
                }
            }

            GUI.EndScrollView();
        }

        private void DrawSavedMapListItem(string mapName, Rect rect)
        {
            GUI.Box(rect, GUIContent.none, AppUiTheme.CardSoft);
            GUI.Label(
                new Rect(rect.x + 12f, rect.y + 8f, rect.width - 180f, rect.height - 16f),
                mapName,
                AppUiTheme.BodyBoldClip);

            var loadPending = pendingLoadMapName == mapName;
            if (GUI.Button(
                    new Rect(rect.xMax - 166f, rect.y + 12f, 88f, 38f),
                    loadPending ? "CONFERMA" : "CARICA",
                    loadPending ? AppUiTheme.PrimaryButton : AppUiTheme.SecondaryButton))
            {
                pendingDeleteMapName = string.Empty;
                if (!loadPending)
                {
                    pendingLoadMapName = mapName;
                    SetSavedMapMessage(
                        "Conferma: la mappa attuale verrà sostituita.",
                        true);
                }
                else if (tacticalMapManager.TryLoadSavedMap(
                             mapName,
                             out var loadedName))
                {
                    savedMapNameInput = loadedName;
                    pendingLoadMapName = string.Empty;
                    pendingOverwriteMapName = string.Empty;
                    mapScrollInitialized = false;
                    SetSavedMapMessage($"«{loadedName}» caricata e sincronizzata.", false);
                }
                else
                {
                    SetSavedMapMessage(tacticalMapManager.LastError, true);
                }
            }

            var deletePending = pendingDeleteMapName == mapName;
            if (GUI.Button(
                    new Rect(rect.xMax - 70f, rect.y + 12f, 58f, 38f),
                    deletePending ? "OK?" : "X",
                    AppUiTheme.DangerButton))
            {
                pendingLoadMapName = string.Empty;
                if (!deletePending)
                {
                    pendingDeleteMapName = mapName;
                    SetSavedMapMessage(
                        $"Premi di nuovo X per eliminare «{mapName}».",
                        true);
                }
                else if (tacticalMapManager.TryDeleteSavedMap(mapName))
                {
                    pendingDeleteMapName = string.Empty;
                    RefreshSavedMaps();
                    SetSavedMapMessage($"«{mapName}» eliminata.", false);
                }
                else
                {
                    SetSavedMapMessage(tacticalMapManager.LastError, true);
                }
            }
        }

        private void RefreshSavedMaps()
        {
            savedMapNames.Clear();
            var names = tacticalMapManager.GetSavedMapNames();
            foreach (var mapName in names)
            {
                savedMapNames.Add(mapName);
            }

            if (!string.IsNullOrEmpty(tacticalMapManager.LastError))
            {
                SetSavedMapMessage(tacticalMapManager.LastError, true);
            }
        }

        private bool SavedMapNameIsListed(string mapName)
        {
            foreach (var savedName in savedMapNames)
            {
                if (string.Equals(
                        savedName,
                        mapName,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void SetSavedMapMessage(string message, bool isError)
        {
            savedMapMessage = message ?? string.Empty;
            savedMapMessageIsError = isError;
        }

        private void DrawVoicePanel(Rect rect)
        {
            AppUiTheme.DrawCard(rect, false, false);
            GUI.Label(
                new Rect(rect.x + 20f, rect.y + 18f, rect.width - 40f, 24f),
                "Voce di prossimità",
                AppUiTheme.Heading);
            GUI.Label(
                new Rect(rect.x + 20f, rect.y + 42f, rect.width - 40f, 30f),
                "Audio separato e posizionale per ogni giocatore.",
                AppUiTheme.Caption);

            DrawVoiceStatus(new Rect(rect.x + 18f, rect.y + 76f, rect.width - 36f, 58f));
            var selected = playerManager.GetPlayer(selectedPlayerId);
            DrawVoiceModeSelector(new Rect(rect.x + 18f, rect.y + 146f, rect.width - 36f, 84f));
            DrawSelectedPlayerCard(new Rect(rect.x + 18f, rect.y + 240f, rect.width - 36f, 98f), selected);

            var y = rect.y + 350f;
            if (voiceManager.State == DiscordVoiceState.Connected)
            {
                if (GUI.Button(
                        new Rect(rect.x + 18f, y, rect.width - 36f, 44f),
                        voiceManager.IsSelfMuted ? "RIATTIVA MICROFONO" : "DISATTIVA MICROFONO",
                        voiceManager.IsSelfMuted ? AppUiTheme.PrimaryButton : AppUiTheme.SecondaryButton))
                {
                    voiceManager.ToggleSelfMute();
                }

                if (GUI.Button(
                        new Rect(rect.x + 18f, rect.yMax - 112f, rect.width - 36f, 40f),
                        "DISCONNETTI VOCE",
                        AppUiTheme.SecondaryButton))
                {
                    voiceManager.StopVoice();
                }
            }
            else if (voiceManager.State == DiscordVoiceState.Ready || voiceManager.State == DiscordVoiceState.Failed)
            {
                if (voiceManager.State == DiscordVoiceState.Failed)
                {
                    AppUiTheme.DrawLabel(
                        new Rect(rect.x + 22f, y, rect.width - 44f, 58f),
                        voiceManager.ErrorMessage,
                        AppUiTheme.CaptionCentered,
                        AppUiTheme.Danger);
                    y += 68f;
                }

                if (GUI.Button(
                        new Rect(rect.x + 18f, y, rect.width - 36f, 46f),
                        voiceManager.State == DiscordVoiceState.Failed ? "RIPROVA VOCE" : "ATTIVA VOCE",
                        AppUiTheme.PrimaryButton))
                {
                    voiceManager.StartVoice();
                }
            }
            else
            {
                GUI.Label(
                    new Rect(rect.x + 20f, y, rect.width - 40f, 42f),
                    "Connessione della voce in corso…",
                    AppUiTheme.CaptionCentered);
            }

            if (GUI.Button(
                    new Rect(rect.x + 18f, rect.yMax - 60f, rect.width - 36f, 42f),
                    "ESCI DALLA SESSIONE",
                    AppUiTheme.DangerButton))
            {
                voiceManager.StopVoice();
                sessionManager.LeaveSession();
            }
        }

        private void DrawVoiceModeSelector(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, AppUiTheme.CardSoft);
            var localPlayer = GetLocalPlayer();
            var currentMode = localPlayer?.VoiceMode ?? VoiceMode.Normal;
            GUI.Label(
                new Rect(rect.x + 12f, rect.y + 6f, rect.width - 24f, 17f),
                "COME PARLI  ·  TASTI 1 / 2 / 3",
                AppUiTheme.EyebrowSmall);

            var buttonY = rect.y + 25f;
            var availableWidth = rect.width - 24f;
            var gap = 6f;
            var buttonWidth = (availableWidth - gap * 2f) / 3f;
            DrawVoiceModeButton(
                new Rect(rect.x + 12f, buttonY, buttonWidth, 34f),
                "1  SUSS.",
                VoiceMode.Whisper,
                currentMode);
            DrawVoiceModeButton(
                new Rect(rect.x + 12f + buttonWidth + gap, buttonY, buttonWidth, 34f),
                "2  NORMALE",
                VoiceMode.Normal,
                currentMode);
            DrawVoiceModeButton(
                new Rect(rect.x + 12f + (buttonWidth + gap) * 2f, buttonY, buttonWidth, 34f),
                "3  URLO",
                VoiceMode.Shout,
                currentMode);

            var maximumDistance = VoiceModeProfile.GetMaximumDistance(currentMode);
            AppUiTheme.DrawLabel(
                new Rect(rect.x + 12f, rect.y + 61f, rect.width - 24f, 18f),
                $"{VoiceModeProfile.GetDisplayName(currentMode)}  ·  portata {maximumDistance:0} m",
                AppUiTheme.CaptionCentered,
                GetVoiceModeColor(currentMode));
        }

        private void DrawVoiceModeButton(
            Rect rect,
            string label,
            VoiceMode mode,
            VoiceMode currentMode)
        {
            if (GUI.Button(
                    rect,
                    label,
                    mode == currentMode ? AppUiTheme.PrimaryButton : AppUiTheme.SecondaryButton))
            {
                playerManager.TrySetLocalVoiceMode(mode);
            }
        }

        private void DrawVoiceStatus(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, AppUiTheme.CardSoft);
            var connected = voiceManager.State == DiscordVoiceState.Connected;
            var color = connected
                ? AppUiTheme.Success
                : voiceManager.State == DiscordVoiceState.Failed
                    ? AppUiTheme.Danger
                    : AppUiTheme.Warning;
            AppUiTheme.DrawDot(new Vector2(rect.x + 27f, rect.center.y), 15f, color);
            AppUiTheme.DrawLabel(
                new Rect(rect.x + 50f, rect.y + 9f, rect.width - 62f, 20f),
                connected ? "Voce connessa" : voiceManager.State == DiscordVoiceState.Failed ? "Voce non disponibile" : "Voce disconnessa",
                AppUiTheme.Heading,
                color);
            GUI.Label(
                new Rect(rect.x + 50f, rect.y + 31f, rect.width - 62f, 20f),
                connected
                    ? $"{voiceManager.ParticipantCount} partecipanti  ·  " +
                      "audio Discord diretto  ·  nessuna coda Unity"
                    : "Attivala per parlare con il party.",
                AppUiTheme.Caption);
        }

        private void DrawSelectedPlayerCard(Rect rect, PlayerData selected)
        {
            GUI.Box(rect, GUIContent.none, AppUiTheme.CardSoft);
            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 8f, rect.width - 28f, 18f),
                "PEDINA SELEZIONATA",
                AppUiTheme.EyebrowSmall);
            if (selected == null)
            {
                GUI.Label(
                    new Rect(rect.x + 14f, rect.y + 34f, rect.width - 28f, 48f),
                    "Seleziona una pedina sulla mappa.",
                    AppUiTheme.CaptionCentered);
                return;
            }

            DrawColoredCircle(new Rect(rect.x + 14f, rect.y + 36f, 46f, 46f), selected.Color, GetInitials(selected));
            GUI.Label(
                new Rect(rect.x + 72f, rect.y + 32f, rect.width - 88f, 24f),
                selected.DisplayName,
                AppUiTheme.BodyBoldClip);

            var localPlayer = GetLocalPlayer();
            var distance = localPlayer == null ? 0f : Vector2.Distance(localPlayer.Position, selected.Position);
            var wallOcclusion = localPlayer == null || selected.IsLocal
                ? 0f
                : tacticalMapManager?.CalculateOcclusion(localPlayer.Position, selected.Position) ?? 0f;
            var acousticDetail = wallOcclusion > 0.01f
                ? $"muri {wallOcclusion * 100f:0}%"
                : "percorso libero";
            var detail = selected.IsLocal
                ? "Punto d'ascolto  ·  area libera"
                : $"{distance:0.0} m da te  ·  {acousticDetail}";
            GUI.Label(
                new Rect(rect.x + 72f, rect.y + 57f, rect.width - 88f, 20f),
                detail,
                AppUiTheme.Caption);
            AppUiTheme.DrawLabel(
                new Rect(rect.x + 72f, rect.y + 78f, rect.width - 88f, 18f),
                $"{VoiceModeProfile.GetDisplayName(selected.VoiceMode)}  ·  " +
                $"raggio {VoiceModeProfile.GetMaximumDistance(selected.VoiceMode):0} m",
                AppUiTheme.Caption,
                GetVoiceModeColor(selected.VoiceMode));
        }

        private void DrawMap(Rect rect)
        {
            AppUiTheme.DrawCard(rect, false, false);
            var innerRect = new Rect(rect.x + 7f, rect.y + 7f, rect.width - 14f, rect.height - 14f);
            GUI.BeginGroup(innerRect);
            var selectedPlayer = playerManager.GetPlayer(selectedPlayerId);
            DrawMapToolbar(new Rect(0f, 0f, innerRect.width, MapToolbarHeight), selectedPlayer);

            var viewportRect = new Rect(
                0f,
                MapToolbarHeight + 6f,
                innerRect.width - MapScrollbarSize - 4f,
                innerRect.height - MapToolbarHeight - MapScrollbarSize - 10f);
            var mapSizeMeters = tacticalMapManager?.MapSizeMeters ?? new Vector2(48f, 48f);
            var initialCanvasSize = new Vector2(
                mapSizeMeters.x * mapPixelsPerMeter,
                mapSizeMeters.y * mapPixelsPerMeter);
            EnsureMapScroll(initialCanvasSize, viewportRect.size, mapSizeMeters);
            HandleMapWheel(viewportRect, mapSizeMeters);
            var canvasRect = new Rect(
                0f,
                0f,
                mapSizeMeters.x * mapPixelsPerMeter,
                mapSizeMeters.y * mapPixelsPerMeter);
            var pointerInsideViewport = viewportRect.Contains(Event.current.mousePosition);

            GUI.Box(viewportRect, GUIContent.none, AppUiTheme.CardSoft);
            GUI.BeginGroup(viewportRect);
            GUI.BeginGroup(new Rect(-mapScroll.x, -mapScroll.y, canvasRect.width, canvasRect.height));
            GUI.DrawTextureWithTexCoords(
                canvasRect,
                mapTexture,
                new Rect(0f, 0f, canvasRect.width / 256f, canvasRect.height / 256f),
                true);
            DrawGrid(canvasRect, mapPixelsPerMeter);
            DrawRooms(canvasRect, mapPixelsPerMeter, selectedPlayer);

            if (selectedPlayer != null)
            {
                var center = MapToLocal(selectedPlayer.Position, canvasRect, mapPixelsPerMeter);
                var radius = VoiceModeProfile.GetMaximumDistance(selectedPlayer.VoiceMode) * mapPixelsPerMeter;
                var ringRect = new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f);
                var oldColor = GUI.color;
                GUI.color = selectedPlayer.Color;
                GUI.DrawTexture(ringRect, rangeTexture, ScaleMode.StretchToFill, true);
                GUI.color = oldColor;
            }

            DrawWalls(canvasRect, mapPixelsPerMeter);
            DrawWallPreview(canvasRect, mapPixelsPerMeter);

            foreach (var player in playerManager.Players)
            {
                DrawPlayerToken(player, canvasRect, mapPixelsPerMeter);
            }

            HandleMapInput(canvasRect, mapPixelsPerMeter, pointerInsideViewport);
            GUI.EndGroup();
            GUI.EndGroup();

            var horizontalRect = new Rect(
                viewportRect.x,
                viewportRect.yMax + 3f,
                viewportRect.width,
                MapScrollbarSize);
            var verticalRect = new Rect(
                viewportRect.xMax + 3f,
                viewportRect.y,
                MapScrollbarSize,
                viewportRect.height);
            mapScroll.x = GUI.HorizontalScrollbar(
                horizontalRect,
                mapScroll.x,
                viewportRect.width,
                0f,
                canvasRect.width);
            mapScroll.y = GUI.VerticalScrollbar(
                verticalRect,
                mapScroll.y,
                viewportRect.height,
                0f,
                canvasRect.height);
            ClampMapScroll(canvasRect.size, viewportRect.size);
            GUI.EndGroup();
        }

        private void DrawMapToolbar(Rect rect, PlayerData selectedPlayer)
        {
            GUI.Box(rect, GUIContent.none, AppUiTheme.CardSoft);
            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 8f, 150f, 18f),
                "MAPPA TATTICA",
                AppUiTheme.EyebrowSmall);
            var mapSize = tacticalMapManager?.MapSizeMeters ?? new Vector2(48f, 48f);
            GUI.Label(
                new Rect(rect.x + 14f, rect.y + 27f, 320f, 22f),
                $"{mapSize.x:0} × {mapSize.y:0} m  ·  " +
                $"{CountWalls(false)} muri  ·  {CountWalls(true)} porte  ·  " +
                FormatRoomCount(tacticalMapManager?.Rooms.Count ?? 0),
                AppUiTheme.Caption);
            GUI.Label(
                new Rect(rect.center.x - 118f, rect.y + 27f, 236f, 22f),
                $"ZOOM {Mathf.RoundToInt(mapPixelsPerMeter / DefaultMapPixelsPerMeter * 100f)}%  ·  CTRL + ROTELLA",
                AppUiTheme.CaptionCentered);

            if (tacticalMapManager?.CanEdit != true)
            {
                var voiceMode = selectedPlayer?.VoiceMode ?? VoiceMode.Normal;
                AppUiTheme.DrawLabel(
                    new Rect(rect.xMax - 220f, rect.y + 19f, 204f, 24f),
                    $"{VoiceModeProfile.GetDisplayName(voiceMode)}  ·  " +
                    $"{VoiceModeProfile.GetMaximumDistance(voiceMode):0} m",
                    AppUiTheme.CaptionRight,
                    GetVoiceModeColor(voiceMode));
                return;
            }

            var activeTool = wallBuildMode
                ? "DISEGNO MURI"
                : doorPlacementMode
                    ? "INSERIMENTO PORTA"
                    : wallEraseMode
                        ? "GOMMA ATTIVA"
                        : string.Empty;
            if (!string.IsNullOrEmpty(activeTool))
            {
                AppUiTheme.DrawPill(
                    new Rect(rect.xMax - 184f, rect.y + 13f, 170f, 30f),
                    activeTool,
                    wallEraseMode ? AppUiTheme.Danger : AppUiTheme.AccentBright);
            }
            else
            {
                GUI.Label(
                    new Rect(rect.xMax - 230f, rect.y + 17f, 216f, 22f),
                    "Strumenti di costruzione nel menu  ☰",
                    AppUiTheme.CaptionRight);
            }
        }

        private void DrawGrid(Rect rect, float pixelsPerMeter)
        {
            var tileSpan = Mathf.Max(1f, pixelsPerMeter * 5f);
            GUI.DrawTextureWithTexCoords(
                rect,
                mapGridTexture,
                new Rect(
                    -rect.center.x / tileSpan,
                    -rect.center.y / tileSpan,
                    rect.width / tileSpan,
                    rect.height / tileSpan),
                true);
        }

        private void DrawRooms(Rect mapRect, float pixelsPerMeter, PlayerData selectedPlayer)
        {
            if (tacticalMapManager == null || tacticalMapManager.Rooms.Count == 0)
            {
                return;
            }

            foreach (var room in tacticalMapManager.Rooms)
            {
                var selected = selectedPlayer != null && room.Contains(selectedPlayer.Position);
                DrawRoomFill(
                    room,
                    mapRect,
                    pixelsPerMeter,
                    selected
                        ? new Color(0.72f, 0.48f, 0.16f, 0.18f)
                        : new Color(0.32f, 0.29f, 0.20f, 0.14f));
            }

            foreach (var room in tacticalMapManager.Rooms)
            {
                var center = MapToLocal(room.Center, mapRect, pixelsPerMeter);
                var playerCount = CountPlayersInRoom(room);
                var label = playerCount == 1
                    ? $"{room.Name}  ·  1 giocatore"
                    : $"{room.Name}  ·  {playerCount} giocatori";
                AppUiTheme.DrawPill(
                    new Rect(center.x - 74f, center.y - 13f, 148f, 26f),
                    label,
                    AppUiTheme.AccentBright,
                    AppUiTheme.EyebrowSmallCentered);
            }
        }

        private void DrawRoomFill(
            RoomData room,
            Rect mapRect,
            float pixelsPerMeter,
            Color color)
        {
            if (room.Boundary.Count < 3)
            {
                return;
            }

            var minimumY = room.Boundary[0].y;
            var maximumY = minimumY;
            foreach (var point in room.Boundary)
            {
                minimumY = Mathf.Min(minimumY, point.y);
                maximumY = Mathf.Max(maximumY, point.y);
            }

            const float stripHeightMeters = 0.5f;
            var intersections = new List<float>(room.Boundary.Count);
            var previousColor = GUI.color;
            GUI.color = color;
            for (var bottom = minimumY; bottom < maximumY; bottom += stripHeightMeters)
            {
                var top = Mathf.Min(bottom + stripHeightMeters, maximumY);
                var scanY = (bottom + top) * 0.5f;
                intersections.Clear();
                for (var index = 0; index < room.Boundary.Count; index++)
                {
                    var next = (index + 1) % room.Boundary.Count;
                    var first = room.Boundary[index];
                    var second = room.Boundary[next];
                    if ((first.y > scanY) == (second.y > scanY))
                    {
                        continue;
                    }

                    intersections.Add(
                        first.x + (scanY - first.y) * (second.x - first.x) / (second.y - first.y));
                }

                intersections.Sort();
                for (var index = 0; index + 1 < intersections.Count; index += 2)
                {
                    var topLeft = MapToLocal(
                        new Vector2(intersections[index], top),
                        mapRect,
                        pixelsPerMeter);
                    var bottomRight = MapToLocal(
                        new Vector2(intersections[index + 1], bottom),
                        mapRect,
                        pixelsPerMeter);
                    GUI.DrawTexture(
                        new Rect(
                            topLeft.x,
                            topLeft.y,
                            Mathf.Max(0f, bottomRight.x - topLeft.x),
                            Mathf.Max(0f, bottomRight.y - topLeft.y)),
                        wallTexture,
                        ScaleMode.StretchToFill);
                }
            }

            GUI.color = previousColor;
        }

        private int CountPlayersInRoom(RoomData room)
        {
            var count = 0;
            foreach (var player in playerManager.Players)
            {
                if (room.Contains(player.Position))
                {
                    count++;
                }
            }

            return count;
        }

        private static string FormatRoomCount(int count)
        {
            return count == 1 ? "1 stanza" : $"{count} stanze";
        }

        private void DrawWalls(Rect mapRect, float pixelsPerMeter)
        {
            if (tacticalMapManager == null)
            {
                return;
            }

            foreach (var wall in tacticalMapManager.Walls)
            {
                var start = MapToLocal(wall.Start, mapRect, pixelsPerMeter);
                var end = MapToLocal(wall.End, mapRect, pixelsPerMeter);
                var thicknessPixels = Mathf.Max(4f, wall.ThicknessMeters * pixelsPerMeter);
                if (wall.IsDoor)
                {
                    DrawDoorSegment(wall, start, end, thicknessPixels);
                    continue;
                }

                DrawWallSegment(
                    start,
                    end,
                    thicknessPixels,
                    wall.Id == selectedWallId ? AppUiTheme.AccentBright : new Color32(137, 127, 105, 255),
                    wall.Id == selectedWallId);
            }
        }

        private void DrawDoorSegment(WallData door, Vector2 start, Vector2 end, float thicknessPixels)
        {
            var isSelected = door.Id == selectedWallId;
            var color = door.State == DoorState.Open
                ? AppUiTheme.Success
                : door.State == DoorState.Locked
                    ? AppUiTheme.Danger
                    : AppUiTheme.Warning;
            if (door.State == DoorState.Open)
            {
                DrawWallSegment(
                    start,
                    Vector2.Lerp(start, end, 0.2f),
                    thicknessPixels,
                    color,
                    isSelected);
                DrawWallSegment(
                    Vector2.Lerp(start, end, 0.8f),
                    end,
                    thicknessPixels,
                    color,
                    isSelected);
            }
            else
            {
                DrawWallSegment(start, end, thicknessPixels, color, isSelected);
            }

            var center = Vector2.Lerp(start, end, 0.5f);
            var label = door.State == DoorState.Open
                ? "APERTA"
                : door.State == DoorState.Locked
                    ? "BLOCCATA"
                    : "CHIUSA";
            AppUiTheme.DrawPill(
                new Rect(center.x - 37f, center.y - 12f, 74f, 24f),
                label,
                color,
                AppUiTheme.EyebrowSmallCentered);
        }

        private int CountWalls(bool doors)
        {
            if (tacticalMapManager == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var obstacle in tacticalMapManager.Walls)
            {
                if (obstacle.IsDoor == doors)
                {
                    count++;
                }
            }

            return count;
        }

        private void DrawWallPreview(Rect mapRect, float pixelsPerMeter)
        {
            if (!wallBuildMode || !wallDragActive)
            {
                return;
            }

            DrawWallSegment(
                MapToLocal(wallDragStart, mapRect, pixelsPerMeter),
                MapToLocal(wallDragCurrent, mapRect, pixelsPerMeter),
                Mathf.Max(4f, wallThicknessMeters * pixelsPerMeter),
                AppUiTheme.AccentBright,
                true);
        }

        private void EnsureMapScroll(Vector2 canvasSize, Vector2 viewportSize, Vector2 mapSizeMeters)
        {
            if (mapScrollInitialized && previousMapSize == mapSizeMeters)
            {
                return;
            }

            mapScroll = new Vector2(
                Mathf.Max(0f, (canvasSize.x - viewportSize.x) * 0.5f),
                Mathf.Max(0f, (canvasSize.y - viewportSize.y) * 0.5f));
            previousMapSize = mapSizeMeters;
            mapScrollInitialized = true;
        }

        private void HandleMapWheel(Rect viewportRect, Vector2 mapSizeMeters)
        {
            var currentEvent = Event.current;
            if (currentEvent.type != EventType.ScrollWheel ||
                !viewportRect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            if (currentEvent.control || currentEvent.command)
            {
                var oldPixelsPerMeter = mapPixelsPerMeter;
                var zoomFactor = Mathf.Exp(-currentEvent.delta.y * MapZoomSensitivity);
                var newPixelsPerMeter = Mathf.Clamp(
                    oldPixelsPerMeter * zoomFactor,
                    MinimumMapPixelsPerMeter,
                    MaximumMapPixelsPerMeter);
                if (!Mathf.Approximately(newPixelsPerMeter, oldPixelsPerMeter))
                {
                    var pointerInViewport = currentEvent.mousePosition - viewportRect.position;
                    var oldCanvasRect = new Rect(
                        0f,
                        0f,
                        mapSizeMeters.x * oldPixelsPerMeter,
                        mapSizeMeters.y * oldPixelsPerMeter);
                    var mapPositionUnderPointer = LocalToMap(
                        mapScroll + pointerInViewport,
                        oldCanvasRect,
                        oldPixelsPerMeter);

                    mapPixelsPerMeter = newPixelsPerMeter;
                    var newCanvasSize = new Vector2(
                        mapSizeMeters.x * mapPixelsPerMeter,
                        mapSizeMeters.y * mapPixelsPerMeter);
                    var newPointerPosition = MapToLocal(
                        mapPositionUnderPointer,
                        new Rect(0f, 0f, newCanvasSize.x, newCanvasSize.y),
                        mapPixelsPerMeter);
                    mapScroll = newPointerPosition - pointerInViewport;
                    ClampMapScroll(newCanvasSize, viewportRect.size);
                }

                currentEvent.Use();
                return;
            }

            if (currentEvent.shift)
            {
                mapScroll.x += currentEvent.delta.y * 36f;
            }
            else
            {
                mapScroll.y += currentEvent.delta.y * 36f;
            }

            var canvasSize = new Vector2(
                mapSizeMeters.x * mapPixelsPerMeter,
                mapSizeMeters.y * mapPixelsPerMeter);
            ClampMapScroll(canvasSize, viewportRect.size);
            currentEvent.Use();
        }

        private void ClampMapScroll(Vector2 canvasSize, Vector2 viewportSize)
        {
            mapScroll = new Vector2(
                Mathf.Clamp(mapScroll.x, 0f, Mathf.Max(0f, canvasSize.x - viewportSize.x)),
                Mathf.Clamp(mapScroll.y, 0f, Mathf.Max(0f, canvasSize.y - viewportSize.y)));
        }

        private void DrawWallSegment(
            Vector2 start,
            Vector2 end,
            float thickness,
            Color color,
            bool highlighted)
        {
            var direction = end - start;
            var length = direction.magnitude;
            if (length < 0.5f)
            {
                return;
            }

            var shadowThickness = thickness + (highlighted ? 8f : 5f);
            if (Mathf.Abs(direction.x) <= 0.01f)
            {
                DrawVerticalBar(start, end, shadowThickness, new Color(0f, 0f, 0f, 0.48f));
                DrawVerticalBar(start, end, thickness, color);
            }
            else if (Mathf.Abs(direction.y) <= 0.01f)
            {
                DrawHorizontalBar(start, end, shadowThickness, new Color(0f, 0f, 0f, 0.48f));
                DrawHorizontalBar(start, end, thickness, color);
            }
            else
            {
                DrawDiagonalBar(start, end, shadowThickness, new Color(0f, 0f, 0f, 0.48f));
                DrawDiagonalBar(start, end, thickness, color);
            }

            DrawWallCap(start, shadowThickness, new Color(0f, 0f, 0f, 0.48f));
            DrawWallCap(end, shadowThickness, new Color(0f, 0f, 0f, 0.48f));
            DrawWallCap(start, thickness, color);
            DrawWallCap(end, thickness, color);
        }

        private void DrawVerticalBar(Vector2 start, Vector2 end, float thickness, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(
                new Rect(
                    start.x - thickness * 0.5f,
                    Mathf.Min(start.y, end.y),
                    thickness,
                    Mathf.Abs(end.y - start.y)),
                wallTexture,
                ScaleMode.StretchToFill);
            GUI.color = previousColor;
        }

        private void DrawHorizontalBar(Vector2 start, Vector2 end, float thickness, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(
                new Rect(
                    Mathf.Min(start.x, end.x),
                    start.y - thickness * 0.5f,
                    Mathf.Abs(end.x - start.x),
                    thickness),
                wallTexture,
                ScaleMode.StretchToFill);
            GUI.color = previousColor;
        }

        private void DrawDiagonalBar(Vector2 start, Vector2 end, float thickness, Color color)
        {
            var length = Vector2.Distance(start, end);
            var step = Mathf.Max(4f, thickness * 0.65f);
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(length / step));
            var previousColor = GUI.color;
            GUI.color = color;

            for (var index = 0; index <= sampleCount; index++)
            {
                var center = Vector2.Lerp(start, end, index / (float)sampleCount);
                GUI.DrawTexture(
                    new Rect(
                        center.x - thickness * 0.5f,
                        center.y - thickness * 0.5f,
                        thickness,
                        thickness),
                    circleTexture,
                    ScaleMode.StretchToFill,
                    true);
            }

            GUI.color = previousColor;
        }

        private void DrawWallCap(Vector2 center, float diameter, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(
                new Rect(
                    center.x - diameter * 0.5f,
                    center.y - diameter * 0.5f,
                    diameter,
                    diameter),
                circleTexture,
                ScaleMode.StretchToFill,
                true);
            GUI.color = previousColor;
        }

        private void DrawPlayerToken(PlayerData player, Rect mapRect, float pixelsPerMeter)
        {
            var center = MapToLocal(player.Position, mapRect, pixelsPerMeter);
            var tokenRect = new Rect(
                center.x - TokenSize * 0.5f,
                center.y - TokenSize * 0.5f,
                TokenSize,
                TokenSize);
            var oldColor = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, 0.52f);
            GUI.DrawTexture(
                new Rect(tokenRect.x + 2f, tokenRect.y + 5f, tokenRect.width, tokenRect.height),
                circleTexture,
                ScaleMode.StretchToFill,
                true);
            GUI.color = new Color32(121, 86, 39, 255);
            GUI.DrawTexture(
                new Rect(tokenRect.x - 4f, tokenRect.y - 4f, tokenRect.width + 8f, tokenRect.height + 8f),
                circleTexture,
                ScaleMode.StretchToFill,
                true);
            GUI.color = Color.Lerp(player.Color, new Color32(40, 29, 18, 255), 0.18f);
            GUI.DrawTexture(tokenRect, circleTexture, ScaleMode.StretchToFill, true);
            GUI.color = new Color(0.96f, 0.78f, 0.40f, 0.78f);
            GUI.DrawTexture(tokenRect, selectionTexture, ScaleMode.StretchToFill, true);
            GUI.color = AppUiTheme.Text;
            GUI.Label(tokenRect, GetInitials(player), AppUiTheme.TokenLabel);

            if (player.PrivateGroup != PrivateVoiceGroup.None)
            {
                AppUiTheme.DrawPill(
                    new Rect(tokenRect.xMax - 5f, tokenRect.y - 7f, 34f, 20f),
                    $"G.{PrivateVoiceGroupRules.GetDisplayName(player.PrivateGroup)}",
                    GetPrivateGroupColor(player.PrivateGroup),
                    AppUiTheme.EyebrowSmallCentered);
            }

            if (selectedPlayerId == player.DiscordUserId)
            {
                GUI.color = AppUiTheme.AccentBright;
                GUI.DrawTexture(
                    new Rect(tokenRect.x - 9f, tokenRect.y - 9f, tokenRect.width + 18f, tokenRect.height + 18f),
                    selectionTexture,
                    ScaleMode.StretchToFill,
                    true);
            }

            GUI.color = oldColor;
            GUI.Label(
                new Rect(tokenRect.x - 62f, tokenRect.yMax + 5f, tokenRect.width + 124f, 22f),
                player.DisplayName,
                AppUiTheme.TokenName);
        }

        private void DrawBurgerMenu(Rect root)
        {
            var burgerRect = new Rect(root.x + 14f, root.y + 13f, 48f, 46f);
            if (GUI.Button(burgerRect, burgerMenuOpen ? "×" : "☰", AppUiTheme.IconButton))
            {
                burgerMenuOpen = !burgerMenuOpen;
                if (!burgerMenuOpen)
                {
                    playersDrawerOpen = false;
                    savedMapsDrawerOpen = false;
                }
            }

            if (!burgerMenuOpen)
            {
                return;
            }

            var menuRect = new Rect(root.x + 14f, burgerRect.yMax + 10f, 340f, root.height - 88f);
            AppUiTheme.DrawCard(menuRect, true);
            GUI.Label(
                new Rect(menuRect.x + 22f, menuRect.y + 20f, menuRect.width - 44f, 28f),
                "Menu del tavolo",
                AppUiTheme.Title);
            GUI.Label(
                new Rect(menuRect.x + 22f, menuRect.y + 50f, menuRect.width - 44f, 22f),
                $"Sessione  ·  {FormatCode(sessionManager.CurrentSessionCode)}",
                AppUiTheme.Caption);
            AppUiTheme.DrawDivider(new Rect(menuRect.x + 22f, menuRect.y + 82f, menuRect.width - 44f, 1f));

            var y = menuRect.y + 100f;
            GUI.Label(
                new Rect(menuRect.x + 22f, y, menuRect.width - 44f, 20f),
                "AZIONI RAPIDE",
                AppUiTheme.Eyebrow);
            y += 30f;

            var canManageSavedMaps = playerManager.CanMovePlayers;
            var quickButtonWidth = canManageSavedMaps
                ? (menuRect.width - 50f) * 0.5f
                : menuRect.width - 44f;
            if (GUI.Button(
                    new Rect(menuRect.x + 22f, y, quickButtonWidth, 42f),
                    $"GIOCATORI · {playerManager.Players.Count} " +
                    (playersDrawerOpen ? "◂" : "▸"),
                    playersDrawerOpen ? AppUiTheme.PrimaryButton : AppUiTheme.SecondaryButton))
            {
                playersDrawerOpen = !playersDrawerOpen;
                savedMapsDrawerOpen = false;
            }

            if (canManageSavedMaps && GUI.Button(
                    new Rect(menuRect.x + 28f + quickButtonWidth, y, quickButtonWidth, 42f),
                    "MAPPE " + (savedMapsDrawerOpen ? "◂" : "▸"),
                    savedMapsDrawerOpen ? AppUiTheme.PrimaryButton : AppUiTheme.SecondaryButton))
            {
                savedMapsDrawerOpen = !savedMapsDrawerOpen;
                playersDrawerOpen = false;
                if (savedMapsDrawerOpen)
                {
                    pendingLoadMapName = string.Empty;
                    pendingDeleteMapName = string.Empty;
                    RefreshSavedMaps();
                }
            }

            y += 52f;

            if (playerManager.CanMovePlayers)
            {
                DrawPrivateGroupControls(menuRect, ref y);

                var mapSize = tacticalMapManager?.MapSizeMeters ?? new Vector2(48f, 48f);
                GUI.Label(
                    new Rect(menuRect.x + 22f, y, menuRect.width - 44f, 18f),
                    $"DIMENSIONI MAPPA  ·  {mapSize.x:0} × {mapSize.y:0} m",
                    AppUiTheme.EyebrowSmall);
                y += 24f;
                var compactWidth = (menuRect.width - 50f) * 0.25f;
                if (GUI.Button(
                        new Rect(menuRect.x + 22f, y, compactWidth, 38f),
                        "L −",
                        AppUiTheme.SecondaryButton))
                {
                    tacticalMapManager?.TryResizeMap(
                        mapSize + new Vector2(-TacticalMapManager.MapResizeStepMeters, 0f));
                }

                if (GUI.Button(
                        new Rect(menuRect.x + 24f + compactWidth, y, compactWidth, 38f),
                        "L +",
                        AppUiTheme.SecondaryButton))
                {
                    tacticalMapManager?.TryResizeMap(
                        mapSize + new Vector2(TacticalMapManager.MapResizeStepMeters, 0f));
                }

                if (GUI.Button(
                        new Rect(menuRect.x + 26f + compactWidth * 2f, y, compactWidth, 38f),
                        "A −",
                        AppUiTheme.SecondaryButton))
                {
                    tacticalMapManager?.TryResizeMap(
                        mapSize + new Vector2(0f, -TacticalMapManager.MapResizeStepMeters));
                }

                if (GUI.Button(
                        new Rect(menuRect.x + 28f + compactWidth * 3f, y, compactWidth, 38f),
                        "A +",
                        AppUiTheme.SecondaryButton))
                {
                    tacticalMapManager?.TryResizeMap(
                        mapSize + new Vector2(0f, TacticalMapManager.MapResizeStepMeters));
                }

                y += 50f;

                GUI.Label(
                    new Rect(menuRect.x + 22f, y, menuRect.width - 44f, 18f),
                    "COSTRUZIONE",
                    AppUiTheme.EyebrowSmall);
                y += 20f;
                var toolWidth = (menuRect.width - 52f) / 3f;
                if (GUI.Button(
                        new Rect(menuRect.x + 22f, y, toolWidth, 32f),
                        wallBuildMode ? "✓ MURI" : "MURI",
                        wallBuildMode ? AppUiTheme.PrimaryButton : AppUiTheme.SecondaryButton))
                {
                    wallBuildMode = !wallBuildMode;
                    doorPlacementMode = false;
                    wallEraseMode = false;
                    wallDragActive = false;
                    if (!wallBuildMode)
                    {
                        ResetWallChain();
                    }

                    burgerMenuOpen = false;
                    playersDrawerOpen = false;
                    savedMapsDrawerOpen = false;
                }

                if (GUI.Button(
                        new Rect(menuRect.x + 26f + toolWidth, y, toolWidth, 32f),
                        doorPlacementMode ? "✓ PORTA" : "PORTA",
                        doorPlacementMode ? AppUiTheme.PrimaryButton : AppUiTheme.SecondaryButton))
                {
                    doorPlacementMode = !doorPlacementMode;
                    wallBuildMode = false;
                    wallEraseMode = false;
                    wallDragActive = false;
                    ResetWallChain();
                    burgerMenuOpen = false;
                    playersDrawerOpen = false;
                    savedMapsDrawerOpen = false;
                }

                if (GUI.Button(
                        new Rect(menuRect.x + 30f + toolWidth * 2f, y, toolWidth, 32f),
                        wallEraseMode ? "✓ GOMMA" : "GOMMA",
                        wallEraseMode ? AppUiTheme.PrimaryButton : AppUiTheme.DangerButton))
                {
                    wallEraseMode = !wallEraseMode;
                    wallBuildMode = false;
                    doorPlacementMode = false;
                    wallDragActive = false;
                    ResetWallChain();
                    burgerMenuOpen = false;
                    playersDrawerOpen = false;
                    savedMapsDrawerOpen = false;
                }

                y += 40f;
                GUI.Label(
                    new Rect(menuRect.x + 22f, y, 142f, 18f),
                    $"SPESSORE  ·  {wallThicknessMeters:0.0} m",
                    AppUiTheme.EyebrowSmall);
                wallThicknessMeters = GUI.HorizontalSlider(
                    new Rect(menuRect.x + 166f, y + 2f, menuRect.width - 188f, 18f),
                    wallThicknessMeters,
                    TacticalMapManager.MinimumWallThicknessMeters,
                    TacticalMapManager.MaximumWallThicknessMeters);
                y += 24f;
                GUI.enabled = wallChainActive && wallChainSegmentCount >= 2;
                if (GUI.Button(
                        new Rect(menuRect.x + 22f, y, menuRect.width - 44f, 30f),
                        "CHIUDI STANZA",
                        AppUiTheme.SecondaryButton))
                {
                    CloseWallChain();
                    burgerMenuOpen = false;
                    playersDrawerOpen = false;
                    savedMapsDrawerOpen = false;
                }

                GUI.enabled = true;
                y += 38f;
            }

            if (voiceManager.State == DiscordVoiceState.Connected)
            {
                if (GUI.Button(
                        new Rect(menuRect.x + 22f, y, menuRect.width - 44f, 42f),
                        voiceManager.IsSelfMuted ? "RIATTIVA MICROFONO" : "DISATTIVA MICROFONO",
                        AppUiTheme.SecondaryButton))
                {
                    voiceManager.ToggleSelfMute();
                }

                y += 52f;
            }
            else if (voiceManager.State == DiscordVoiceState.Ready || voiceManager.State == DiscordVoiceState.Failed)
            {
                if (GUI.Button(
                        new Rect(menuRect.x + 22f, y, menuRect.width - 44f, 42f),
                        "ATTIVA VOCE",
                        AppUiTheme.PrimaryButton))
                {
                    voiceManager.StartVoice();
                }

                y += 52f;
            }

            if (GUI.Button(
                    new Rect(menuRect.x + 22f, menuRect.yMax - 62f, menuRect.width - 44f, 42f),
                    "ESCI DALLA SESSIONE",
                    AppUiTheme.DangerButton))
            {
                burgerMenuOpen = false;
                playersDrawerOpen = false;
                savedMapsDrawerOpen = false;
                wallBuildMode = false;
                doorPlacementMode = false;
                wallEraseMode = false;
                wallDragActive = false;
                ResetWallChain();
                voiceManager.StopVoice();
                sessionManager.LeaveSession();
            }

            DrawPlayersDrawer(menuRect);
            DrawSavedMapsDrawer(menuRect);
        }

        private void DrawPrivateGroupControls(Rect menuRect, ref float y)
        {
            var selected = playerManager.GetPlayer(selectedPlayerId);
            var selectedName = selected?.DisplayName ?? "nessuna pedina";
            GUI.Label(
                new Rect(menuRect.x + 22f, y, menuRect.width - 44f, 18f),
                $"GRUPPI PRIVATI  ·  {selectedName}",
                AppUiTheme.EyebrowSmall);
            y += 22f;

            var buttonWidth = (menuRect.width - 50f) * 0.25f;
            var previousEnabled = GUI.enabled;
            GUI.enabled = selected != null;
            DrawPrivateGroupButton(menuRect.x + 22f, y, buttonWidth, "NESSUNO", PrivateVoiceGroup.None, selected);
            DrawPrivateGroupButton(menuRect.x + 24f + buttonWidth, y, buttonWidth, "A", PrivateVoiceGroup.A, selected);
            DrawPrivateGroupButton(menuRect.x + 26f + buttonWidth * 2f, y, buttonWidth, "B", PrivateVoiceGroup.B, selected);
            DrawPrivateGroupButton(menuRect.x + 28f + buttonWidth * 3f, y, buttonWidth, "C", PrivateVoiceGroup.C, selected);
            GUI.enabled = previousEnabled;
            y += 38f;

            var isolationEnabled = playerManager.PrivateGroupsIsolated;
            if (GUI.Button(
                    new Rect(menuRect.x + 22f, y, menuRect.width - 44f, 32f),
                    isolationEnabled ? "✓ ISOLAMENTO GRUPPI ATTIVO" : "ISOLAMENTO GRUPPI DISATTIVO",
                    isolationEnabled ? AppUiTheme.PrimaryButton : AppUiTheme.SecondaryButton))
            {
                playerManager.TrySetPrivateGroupsIsolated(!isolationEnabled);
            }

            y += 40f;
        }

        private void DrawPrivateGroupButton(
            float x,
            float y,
            float width,
            string label,
            PrivateVoiceGroup group,
            PlayerData selected)
        {
            var isSelected = selected != null && selected.PrivateGroup == group;
            if (GUI.Button(
                    new Rect(x, y, width, 30f),
                    isSelected ? $"✓ {label}" : label,
                    isSelected ? AppUiTheme.PrimaryButton : AppUiTheme.SecondaryButton))
            {
                playerManager.TrySetPrivateGroup(selected.DiscordUserId, group);
            }
        }

        private void HandleMapInput(
            Rect mapRect,
            float pixelsPerMeter,
            bool pointerInsideViewport)
        {
            if (burgerMenuOpen)
            {
                draggingPlayerId = 0;
                wallDragActive = false;
                return;
            }

            var currentEvent = Event.current;
            var pointerInsideMap = pointerInsideViewport && mapRect.Contains(currentEvent.mousePosition);
            if ((wallBuildMode || doorPlacementMode || wallEraseMode) &&
                currentEvent.type == EventType.KeyDown &&
                currentEvent.keyCode == KeyCode.Escape)
            {
                wallBuildMode = false;
                doorPlacementMode = false;
                wallEraseMode = false;
                wallDragActive = false;
                ResetWallChain();
                currentEvent.Use();
                return;
            }

            if (selectedWallId != 0 &&
                tacticalMapManager?.CanEdit == true &&
                currentEvent.type == EventType.KeyDown &&
                (currentEvent.keyCode == KeyCode.Delete || currentEvent.keyCode == KeyCode.Backspace))
            {
                tacticalMapManager.TryRemoveWall(selectedWallId);
                selectedWallId = 0;
                ResetWallChain();
                currentEvent.Use();
                return;
            }

            if (wallEraseMode && tacticalMapManager?.CanEdit == true)
            {
                if (currentEvent.type == EventType.MouseDown &&
                    currentEvent.button == 0 &&
                    pointerInsideMap)
                {
                    var mapPosition = LocalToMap(currentEvent.mousePosition, mapRect, pixelsPerMeter);
                    var wall = tacticalMapManager.GetWallAt(mapPosition);
                    if (wall != null)
                    {
                        tacticalMapManager.TryRemoveWall(wall.Id);
                        selectedWallId = 0;
                    }

                    currentEvent.Use();
                }

                return;
            }

            if (doorPlacementMode && tacticalMapManager?.CanEdit == true)
            {
                if (currentEvent.type == EventType.MouseDown &&
                    currentEvent.button == 0 &&
                    pointerInsideMap)
                {
                    var mapPosition = LocalToMap(currentEvent.mousePosition, mapRect, pixelsPerMeter);
                    var existing = tacticalMapManager.GetWallAt(mapPosition);
                    if (existing != null && existing.IsDoor)
                    {
                        selectedWallId = existing.Id;
                        tacticalMapManager.TryCycleDoorState(existing.Id);
                    }
                    else if (tacticalMapManager.TryInsertDoor(mapPosition, out var createdDoor))
                    {
                        selectedWallId = createdDoor.Id;
                    }

                    currentEvent.Use();
                }

                return;
            }

            if (wallBuildMode && tacticalMapManager?.CanEdit == true)
            {
                if (currentEvent.type == EventType.MouseDown &&
                    currentEvent.button == 0 &&
                    pointerInsideMap)
                {
                    wallDragStart = SnapToWallGrid(LocalToMap(
                        currentEvent.mousePosition,
                        mapRect,
                        pixelsPerMeter));
                    wallDragCurrent = wallDragStart;
                    wallDragActive = true;
                    draggingPlayerId = 0;
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.MouseDrag && wallDragActive)
                {
                    wallDragCurrent = SnapToWallGrid(LocalToMap(
                        currentEvent.mousePosition,
                        mapRect,
                        pixelsPerMeter));
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0 && wallDragActive)
                {
                    wallDragCurrent = SnapToWallGrid(LocalToMap(
                        currentEvent.mousePosition,
                        mapRect,
                        pixelsPerMeter));
                    wallDragActive = false;
                    if (tacticalMapManager.TryCreateWall(
                            wallDragStart,
                            wallDragCurrent,
                            wallThicknessMeters,
                            out var createdWall))
                    {
                        selectedWallId = createdWall.Id;
                        TrackWallChain(createdWall);
                    }

                    currentEvent.Use();
                }

                return;
            }

            if (currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 0 &&
                pointerInsideMap)
            {
                var hitToken = false;
                for (var index = playerManager.Players.Count - 1; index >= 0; index--)
                {
                    var player = playerManager.Players[index];
                    var center = MapToLocal(player.Position, mapRect, pixelsPerMeter);
                    var hitRect = new Rect(
                        center.x - TokenSize * 0.5f,
                        center.y - TokenSize * 0.5f,
                        TokenSize,
                        TokenSize);
                    if (!hitRect.Contains(currentEvent.mousePosition))
                    {
                        continue;
                    }

                    selectedPlayerId = player.DiscordUserId;
                    if (playerManager.CanMovePlayers)
                    {
                        draggingPlayerId = player.DiscordUserId;
                    }

                    hitToken = true;
                    currentEvent.Use();
                    break;
                }

                if (!hitToken && playerManager.CanMovePlayers)
                {
                    var mapPosition = LocalToMap(currentEvent.mousePosition, mapRect, pixelsPerMeter);
                    var obstacle = tacticalMapManager?.GetWallAt(mapPosition);
                    selectedWallId = obstacle?.Id ?? 0;
                    if (obstacle?.IsDoor == true)
                    {
                        tacticalMapManager.TryCycleDoorState(obstacle.Id);
                    }

                    currentEvent.Use();
                }
            }
            else if (currentEvent.type == EventType.MouseDrag && draggingPlayerId != 0)
            {
                playerManager.TryMovePlayer(
                    draggingPlayerId,
                    LocalToMap(currentEvent.mousePosition, mapRect, pixelsPerMeter));
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
            {
                draggingPlayerId = 0;
            }
        }

        private void HandleVoiceModeShortcuts()
        {
            var currentEvent = Event.current;
            if (currentEvent.type != EventType.KeyDown)
            {
                return;
            }

            var handled = true;
            var voiceMode = VoiceMode.Normal;
            switch (currentEvent.keyCode)
            {
                case KeyCode.Alpha1:
                case KeyCode.Keypad1:
                    voiceMode = VoiceMode.Whisper;
                    break;
                case KeyCode.Alpha2:
                case KeyCode.Keypad2:
                    voiceMode = VoiceMode.Normal;
                    break;
                case KeyCode.Alpha3:
                case KeyCode.Keypad3:
                    voiceMode = VoiceMode.Shout;
                    break;
                default:
                    handled = false;
                    break;
            }

            if (handled)
            {
                playerManager.TrySetLocalVoiceMode(voiceMode);
                currentEvent.Use();
            }
        }

        private static Color GetVoiceModeColor(VoiceMode voiceMode)
        {
            switch (voiceMode)
            {
                case VoiceMode.Whisper:
                    return AppUiTheme.AccentBright;
                case VoiceMode.Shout:
                    return AppUiTheme.Warning;
                default:
                    return AppUiTheme.Success;
            }
        }

        private PlayerData GetLocalPlayer()
        {
            foreach (var player in playerManager.Players)
            {
                if (player.IsLocal)
                {
                    return player;
                }
            }

            return null;
        }

        private void EnsureSelection()
        {
            if (playerManager.GetPlayer(selectedPlayerId) != null)
            {
                return;
            }

            selectedPlayerId = 0;
            foreach (var player in playerManager.Players)
            {
                if (player.IsLocal)
                {
                    selectedPlayerId = player.DiscordUserId;
                    return;
                }
            }

            if (playerManager.Players.Count > 0)
            {
                selectedPlayerId = playerManager.Players[0].DiscordUserId;
            }
        }

        private static Color GetPrivateGroupColor(PrivateVoiceGroup group)
        {
            switch (group)
            {
                case PrivateVoiceGroup.A:
                    return new Color32(211, 158, 63, 255);
                case PrivateVoiceGroup.B:
                    return new Color32(69, 177, 127, 255);
                case PrivateVoiceGroup.C:
                    return new Color32(151, 104, 218, 255);
                default:
                    return AppUiTheme.Muted;
            }
        }

        private void EnsureTextures()
        {
            AppUiTheme.Ensure();
            if (mapTexture != null)
            {
                return;
            }

            mapTexture = MakeFantasyMapTexture(256);
            mapGridTexture = MakeMapGridTexture();
            circleTexture = MakeCircleTexture(128, 255, 255, 0f);
            rangeTexture = MakeCircleTexture(256, 13, 155, 3f);
            selectionTexture = MakeCircleTexture(128, 0, 255, 5f);
            wallTexture = MakeTexture(Color.white);
        }

        private void DrawColoredCircle(Rect rect, Color color, string label)
        {
            var oldColor = GUI.color;
            GUI.color = new Color32(117, 83, 38, 255);
            GUI.DrawTexture(
                new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f),
                circleTexture,
                ScaleMode.StretchToFill,
                true);
            GUI.color = Color.Lerp(color, new Color32(42, 32, 20, 255), 0.18f);
            GUI.DrawTexture(rect, circleTexture, ScaleMode.StretchToFill, true);
            GUI.color = AppUiTheme.Text;
            GUI.Label(rect, label, AppUiTheme.TokenLabel);
            GUI.color = oldColor;
        }

        private static Vector2 MapToLocal(Vector2 position, Rect mapRect, float pixelsPerMeter)
        {
            return new Vector2(
                mapRect.width * 0.5f + position.x * pixelsPerMeter,
                mapRect.height * 0.5f - position.y * pixelsPerMeter);
        }

        private static Vector2 LocalToMap(Vector2 position, Rect mapRect, float pixelsPerMeter)
        {
            return new Vector2(
                (position.x - mapRect.width * 0.5f) / pixelsPerMeter,
                (mapRect.height * 0.5f - position.y) / pixelsPerMeter);
        }

        private Vector2 SnapToWallGrid(Vector2 position)
        {
            return tacticalMapManager?.SnapPosition(position) ?? position;
        }

        private void TrackWallChain(WallData wall)
        {
            if (!wallChainActive || Vector2.Distance(wall.Start, wallChainEnd) > 0.01f)
            {
                wallChainStart = wall.Start;
                wallChainSegmentCount = 0;
            }

            wallChainActive = true;
            wallChainEnd = wall.End;
            wallChainSegmentCount++;
            if (wallChainSegmentCount >= 3 && Vector2.Distance(wallChainEnd, wallChainStart) <= 0.01f)
            {
                ResetWallChain();
            }
        }

        private void CloseWallChain()
        {
            if (!wallChainActive || wallChainSegmentCount < 2 || tacticalMapManager?.CanEdit != true)
            {
                return;
            }

            if (tacticalMapManager.TryCreateWall(
                    wallChainEnd,
                    wallChainStart,
                    wallThicknessMeters,
                    out var closingWall))
            {
                selectedWallId = closingWall.Id;
                ResetWallChain();
            }
        }

        private void ResetWallChain()
        {
            wallChainActive = false;
            wallChainStart = Vector2.zero;
            wallChainEnd = Vector2.zero;
            wallChainSegmentCount = 0;
        }

        private static string FormatCode(string code)
        {
            return string.IsNullOrEmpty(code) ? string.Empty : string.Join("  ", code.ToCharArray());
        }

        private static string GetInitials(PlayerData player)
        {
            if (player.IsDM)
            {
                return "DM";
            }

            if (string.IsNullOrWhiteSpace(player.DisplayName))
            {
                return "?";
            }

            var name = player.DisplayName.Trim();
            var separator = name.IndexOfAny(new[] { ' ', '_', '-' });
            if (separator > 0 && separator + 1 < name.Length)
            {
                return string.Concat(
                    char.ToUpperInvariant(name[0]),
                    char.ToUpperInvariant(name[separator + 1]));
            }

            return name.Substring(0, Mathf.Min(2, name.Length)).ToUpperInvariant();
        }

        private static Texture2D MakeTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D MakeFantasyMapTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            var fullTurn = Mathf.PI * 2f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var normalizedX = x / (float)size;
                    var normalizedY = y / (float)size;
                    var grain =
                        Mathf.Sin(normalizedX * fullTurn * 3f) * 0.45f +
                        Mathf.Cos(normalizedY * fullTurn * 4f) * 0.32f +
                        Mathf.Sin((normalizedX + normalizedY) * fullTurn * 5f) * 0.23f;
                    var fibers = Mathf.Abs(Mathf.Sin(
                        normalizedX * fullTurn * 13f +
                        Mathf.Sin(normalizedY * fullTurn * 2f) * 0.8f));
                    var shade = grain * 3.2f + fibers * 1.8f;
                    pixels[y * size + x] = new Color32(
                        (byte)Mathf.Clamp(20f + shade, 12f, 31f),
                        (byte)Mathf.Clamp(20f + shade * 0.86f, 12f, 29f),
                        (byte)Mathf.Clamp(16f + shade * 0.58f, 9f, 24f),
                        255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D MakeCircleTexture(int size, byte fillAlpha, byte edgeAlpha, float edgeWidth)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            var radius = size * 0.48f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    var alpha = (byte)0;
                    if (distance <= radius)
                    {
                        alpha = edgeWidth > 0f && distance >= radius - edgeWidth ? edgeAlpha : fillAlpha;
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D MakeMapGridTexture()
        {
            const int size = 100;
            const int cell = 20;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point
            };
            var pixels = new Color32[size * size];
            var clear = new Color32(0, 0, 0, 0);
            var minor = new Color32(171, 136, 75, 30);
            var major = new Color32(218, 172, 86, 78);
            var node = new Color32(235, 194, 112, 102);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var isNode = x % cell <= 1 && y % cell <= 1;
                    var isMajor = x <= 1 || y <= 1;
                    var isMinor = x % cell == 0 || y % cell == 0;
                    pixels[y * size + x] = isNode ? node : isMajor ? major : isMinor ? minor : clear;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void OnDestroy()
        {
            DestroyTexture(mapTexture);
            DestroyTexture(mapGridTexture);
            DestroyTexture(circleTexture);
            DestroyTexture(rangeTexture);
            DestroyTexture(selectionTexture);
            DestroyTexture(wallTexture);
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture != null)
            {
                Destroy(texture);
            }
        }
    }
}
