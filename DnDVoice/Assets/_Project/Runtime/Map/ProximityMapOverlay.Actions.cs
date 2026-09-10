using DndProximityVoice.Players;
using DndProximityVoice.Session;
using DndProximityVoice.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DndProximityVoice.Map
{
    public sealed partial class ProximityMapOverlay
    {
        private enum Confirmation { None, DeleteWall, KickPlayer, Leave }
        private Confirmation confirmation;
        private WallData pendingWall;
        private ulong pendingPlayerId;
        private bool contextOpen;
        private bool quickMenuVisible;
        private bool pointerOverQuickMenu;
        private Rect quickMenuRect;
        private Vector2 mapViewportSize;
        private bool wallMoveMode;
        private Vector2 wallMovePreview;
        private float wallAnchorFraction = 0.5f;
        private string actionMessage = string.Empty;
        private bool ConfirmationOpen => confirmation != Confirmation.None;

        private void DrawMapToolbar(Rect rect, PlayerData selectedPlayer)
        {
            GUI.Box(rect, GUIContent.none, AppUiTheme.CardSoft);
            var x = rect.x + 8f;
            var y = rect.y + 5f;
            bool Tool(UiIcon icon, string hint, bool selected = false, bool danger = false)
            {
                var clicked = AppUiControls.IconButton(new Rect(x, y, 34f, 32f), icon, hint, selected, danger);
                x += 38f;
                return clicked;
            }
            if (tacticalMapManager?.CanEdit == true)
            {
                if (Tool(UiIcon.Select, "Seleziona elementi · Esc annulla", !wallBuildMode && !doorPlacementMode && !wallMoveMode)) SetTool(false, false);
                if (Tool(UiIcon.Wall, "Crea muro · trascina sulla griglia", wallBuildMode)) SetTool(!wallBuildMode, false);
                if (Tool(UiIcon.Door, "Inserisci porta in un muro", doorPlacementMode)) SetTool(false, !doorPlacementMode);
                var enabled = GUI.enabled;
                GUI.enabled = enabled && wallChainActive && wallChainSegmentCount >= 2;
                if (Tool(UiIcon.CloseRoom, "Completa il perimetro della stanza")) CloseWallChain();
                GUI.enabled = enabled && selectedWallId != 0;
                if (Tool(UiIcon.Delete, "Elimina elemento selezionato · Canc", false, true)) RequestDeleteWall();
                GUI.enabled = enabled;
                AppUiTheme.DrawDivider(new Rect(x + 2f, y + 3f, 1f, 26f));
                if (wallBuildMode)
                {
                    GUI.Label(new Rect(x + 12f, y - 1f, 152f, 16f), $"SPESSORE · {wallThicknessMeters:0.0} m", AppUiTheme.EyebrowSmall);
                    var slider = new Rect(x + 12f, y + 19f, Mathf.Min(160f, rect.width - x - 200f), 14f);
                    AppUiControls.Hover(slider);
                    wallThicknessMeters = GUI.HorizontalSlider(slider, wallThicknessMeters, TacticalMapManager.MinimumWallThicknessMeters, TacticalMapManager.MaximumWallThicknessMeters);
                }
            }
            else GUI.Label(new Rect(x, y + 5f, 200f, 22f), "MAPPA TATTICA", AppUiTheme.Eyebrow);

            x = rect.xMax - 164f;
            if (Tool(UiIcon.ZoomOut, "Riduci zoom")) SetMapZoom(mapPixelsPerMeter / 1.2f, mapViewportSize * 0.5f, mapViewportSize);
            if (Tool(UiIcon.ZoomIn, "Aumenta zoom")) SetMapZoom(mapPixelsPerMeter * 1.2f, mapViewportSize * 0.5f, mapViewportSize);
            if (Tool(UiIcon.Reset, "Zoom 100% e centra la mappa"))
            {
                SetMapZoom(DefaultMapPixelsPerMeter, mapViewportSize * 0.5f, mapViewportSize);
                mapScrollInitialized = false;
            }
            GUI.Label(new Rect(x, y + 7f, 48f, 18f), $"{mapPixelsPerMeter / DefaultMapPixelsPerMeter:P0}", AppUiTheme.CaptionSmallCentered);
            var hint = wallMoveMode ? "SPOSTA · clicca la nuova posizione · Esc annulla" : wallBuildMode ? "MURO · trascina per disegnare · Esc termina" : doorPlacementMode ? "PORTA · clicca un muro · Esc termina" : "Seleziona un elemento per le azioni · Ctrl + rotella per lo zoom";
            GUI.Label(new Rect(rect.x + 10f, rect.yMax - 17f, rect.width - 20f, 16f), string.IsNullOrEmpty(actionMessage) ? hint : actionMessage, AppUiTheme.CaptionSmall);
        }

        private void SetMapZoom(float requested, Vector2 anchorInViewport, Vector2 viewportSize)
        {
            var mapSize = tacticalMapManager?.MapSizeMeters ?? new Vector2(48f, 48f);
            var position = LocalToMap(mapScroll + anchorInViewport, new Rect(Vector2.zero, mapSize * mapPixelsPerMeter), mapPixelsPerMeter);
            mapPixelsPerMeter = Mathf.Clamp(requested, MinimumMapPixelsPerMeter, MaximumMapPixelsPerMeter);
            mapScroll = MapToLocal(position, new Rect(Vector2.zero, mapSize * mapPixelsPerMeter), mapPixelsPerMeter) - anchorInViewport;
            ClampMapScroll(mapSize * mapPixelsPerMeter, viewportSize);
        }

        private void SetTool(bool build, bool door)
        {
            ClearSelection();
            wallBuildMode = build;
            doorPlacementMode = door;
            wallDragActive = false;
            ResetWallChain();
            actionMessage = string.Empty;
            GUIUtility.keyboardControl = 0;
        }

        private void SelectPlayer(ulong userId)
        {
            ClearSelection();
            selectedPlayerId = userId;
            contextOpen = playerManager.CanMovePlayers;
            wallBuildMode = doorPlacementMode = false;
            ResetWallChain();
        }

        private void SelectWall(WallData wall, Vector2 mapPoint)
        {
            ClearSelection();
            if (wall == null) return;
            selectedWallId = wall.Id;
            var direction = wall.End - wall.Start;
            wallAnchorFraction = Mathf.Clamp01(Vector2.Dot(mapPoint - wall.Start, direction) / direction.sqrMagnitude);
            contextOpen = true;
        }

        private void ClearSelection()
        {
            selectedPlayerId = 0;
            selectedWallId = 0;
            CloseContext();
        }

        private void CloseContext()
        {
            contextOpen = quickMenuVisible = wallMoveMode = false;
            pointerOverQuickMenu = false;
            draggingPlayerId = 0;
            wallDragActive = false;
            confirmation = Confirmation.None;
            pendingWall = null;
            pendingPlayerId = 0;
        }

        private void EnsureSelection()
        {
            if (selectedPlayerId != 0 && playerManager.GetPlayer(selectedPlayerId) == null) ClearSelection();
            if (selectedWallId != 0 && tacticalMapManager?.GetWall(selectedWallId) == null) ClearSelection();
            if (!playerManager.CanMovePlayers)
            {
                CloseContext();
                wallBuildMode = doorPlacementMode = false;
            }
        }

        private void UpdateQuickMenuRect(Rect viewport, Vector2 canvasSize)
        {
            quickMenuVisible = false;
            if (!contextOpen || burgerMenuOpen || ConfirmationOpen || wallMoveMode || draggingPlayerId != 0 || tacticalMapManager?.CanEdit != true) return;
            var wall = tacticalMapManager.GetWall(selectedWallId);
            var player = playerManager.GetPlayer(selectedPlayerId);
            if (wall == null && (player == null || player.IsDM)) return;
            var anchor = wall != null ? Vector2.Lerp(wall.Start, wall.End, wallAnchorFraction) : player.Position;
            anchor = viewport.position + MapToLocal(anchor, new Rect(Vector2.zero, canvasSize), mapPixelsPerMeter) - mapScroll;
            if (!viewport.Contains(anchor)) return; // Keep the selection when it scrolls out of view.
            quickMenuRect = AppUiControls.PlacePopup(anchor, new Vector2(wall?.IsDoor == true ? 180f : 142f, 70f), viewport, wall != null ? 16f : TokenSize * 0.5f + 8f);
            quickMenuVisible = true;
        }

        private void DrawQuickMenu()
        {
            if (!quickMenuVisible) return;
            var wall = tacticalMapManager.GetWall(selectedWallId);
            var player = playerManager.GetPlayer(selectedPlayerId);
            AppUiTheme.DrawCard(quickMenuRect, true, false);
            GUI.Label(new Rect(quickMenuRect.x + 10f, quickMenuRect.y + 5f, quickMenuRect.width - 20f, 20f), wall != null ? wall.Name : player?.DisplayName, AppUiTheme.BodyBoldClip);
            var x = quickMenuRect.x + 10f;
            bool Action(UiIcon icon, string hint, bool selected = false, bool danger = false)
            {
                var clicked = AppUiControls.IconButton(new Rect(x, quickMenuRect.y + 29f, 34f, 32f), icon, hint, selected, danger);
                x += 40f;
                return clicked;
            }
            if (wall != null)
            {
                if (Action(UiIcon.Move, "Sposta · poi clicca sulla mappa")) { wallMoveMode = true; wallMovePreview = wall.Start; ResetWallChain(); }
                if (Action(UiIcon.Rotate, "Ruota di 90°"))
                {
                    if (!tacticalMapManager.TryRotateWall(wall.Id)) actionMessage = tacticalMapManager.LastError;
                    ResetWallChain();
                }
                if (Action(UiIcon.Delete, "Elimina elemento", false, true)) RequestDeleteWall();
                if (wall.IsDoor && Action(UiIcon.Door, $"Porta: {wall.State} · cambia stato")) tacticalMapManager.TryCycleDoorState(wall.Id);
            }
            else if (player != null && !player.IsDM)
            {
                if (Action(player.IsVoiceMutedByDm ? UiIcon.Muted : UiIcon.Microphone,
                    player.IsVoiceMutedByDm ? "Riattiva audio nella stanza" : "Disattiva audio per tutta la stanza", player.IsVoiceMutedByDm))
                    playerManager.TrySetVoiceMuted(player.DiscordUserId, !player.IsVoiceMutedByDm);
                if (Action(UiIcon.Kick, "Espelli dalla stanza", false, true))
                { pendingPlayerId = player.DiscordUserId; confirmation = Confirmation.KickPlayer; }
            }
            // Consume the padding too; no quick-menu event reaches the map below it.
            if (quickMenuRect.Contains(Event.current.mousePosition) && (Event.current.isMouse || Event.current.type == EventType.ScrollWheel)) Event.current.Use();
        }

        private void DrawMovePreview(Rect canvas, float scale, bool pointerInside)
        {
            if (!wallMoveMode || !pointerInside) return;
            var wall = tacticalMapManager.GetWall(selectedWallId);
            if (wall == null) return;
            var point = LocalToMap(Event.current.mousePosition, canvas, scale);
            wallMovePreview = tacticalMapManager.SnapPosition(point - (wall.End - wall.Start) * 0.5f, wall.Id);
            DrawWallSegment(MapToLocal(wallMovePreview, canvas, scale), MapToLocal(wallMovePreview + wall.End - wall.Start, canvas, scale), Mathf.Max(4f, wall.ThicknessMeters * scale), AppUiTheme.AccentBright, true);
        }

        private void RequestDeleteWall()
        {
            if (tacticalMapManager?.CanEdit != true) return;
            pendingWall = tacticalMapManager.GetWall(selectedWallId);
            if (pendingWall != null) confirmation = Confirmation.DeleteWall;
        }

        private void RequestLeave()
        {
            CloseContext();
            burgerMenuOpen = playersDrawerOpen = savedMapsDrawerOpen = utilitiesDrawerOpen = false;
            confirmation = Confirmation.Leave;
        }

        private void DrawConfirmation(Rect viewport)
        {
            if (!ConfirmationOpen) return;
            if ((confirmation == Confirmation.DeleteWall && tacticalMapManager.GetWall(pendingWall?.Id ?? 0) != pendingWall) ||
                (confirmation == Confirmation.KickPlayer && playerManager.GetPlayer(pendingPlayerId) == null))
            { confirmation = Confirmation.None; return; }
            AppUiControls.ClearHover();
            AppUiTheme.DrawRect(viewport, new Color(0f, 0f, 0f, 0.65f));
            var rect = new Rect(viewport.center - new Vector2(230f, 99f), new Vector2(460f, 198f));
            AppUiTheme.DrawCard(rect, true, false);
            var title = confirmation == Confirmation.DeleteWall ? "Eliminare questo elemento?" : confirmation == Confirmation.KickPlayer ? "Espellere il giocatore?" : sessionManager.IsHost ? "Chiudere la stanza?" : "Uscire dalla stanza?";
            var description = confirmation == Confirmation.DeleteWall ? $"{pendingWall.Name} sarà rimosso dalla mappa condivisa. Il perimetro e l'acustica verranno aggiornati." :
                confirmation == Confirmation.KickPlayer ? $"{playerManager.GetPlayer(pendingPlayerId).DisplayName} verrà disconnesso dalla stanza e dalla voce." :
                sessionManager.IsHost ? "La sessione e la voce termineranno per tutti. Salva la mappa prima di continuare." : "La connessione alla mappa e alla voce verrà chiusa.";
            GUI.Label(new Rect(rect.x + 22f, rect.y + 18f, rect.width - 44f, 30f), title, AppUiTheme.TitleCompact);
            GUI.Label(new Rect(rect.x + 22f, rect.y + 57f, rect.width - 44f, 60f), description, AppUiTheme.Body);
            var cancel = AppUiControls.Button(new Rect(rect.x + 22f, rect.yMax - 58f, 198f, 38f), "ANNULLA", AppUiTheme.SecondaryButton);
            if (AppUiControls.Button(new Rect(rect.x + 240f, rect.yMax - 58f, 198f, 38f), "CONFERMA", AppUiTheme.DangerButton))
            {
                var action = confirmation;
                confirmation = Confirmation.None;
                if (action == Confirmation.DeleteWall) { tacticalMapManager.TryRemoveWall(pendingWall.Id); ClearSelection(); ResetWallChain(); }
                if (action == Confirmation.KickPlayer) { sessionManager.TryKickPlayer(pendingPlayerId); ClearSelection(); }
                if (action == Confirmation.Leave) { ResetInteractions(); voiceManager.StopVoice(); sessionManager.LeaveSession(); }
            }
            if (cancel || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)) confirmation = Confirmation.None;
            if (Event.current.isMouse || Event.current.isKey || Event.current.type == EventType.ScrollWheel) Event.current.Use();
        }

        private void OnSessionChanged(DiscordSessionState state) { if (state != DiscordSessionState.Joined) ResetInteractions(); }
        private void OnActiveSceneChanged(Scene previous, Scene next) => ResetInteractions();
        private void OnDisable() => ResetInteractions();
        private void OnApplicationFocus(bool focused) { if (!focused) { CloseContext(); wallDragActive = false; } }
        private void ResetInteractions()
        {
            ClearSelection();
            burgerMenuOpen = playersDrawerOpen = savedMapsDrawerOpen = utilitiesDrawerOpen = false;
            wallBuildMode = doorPlacementMode = false;
            ResetWallChain();
            mapScrollInitialized = false;
            actionMessage = string.Empty;
        }
    }
}
