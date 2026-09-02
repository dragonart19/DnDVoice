using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DndProximityVoice.Session;
using UnityEngine;

namespace DndProximityVoice.Map
{
    public static class MapSaveSerializer
    {
        private const int CurrentVersion = 1;
        private const int MaximumMapNameLength = 32;

        [Serializable]
        private sealed class SavedMapFile
        {
            public int version;
            public string name;
            public float width;
            public float height;
            public List<SavedWall> walls = new List<SavedWall>();
        }

        [Serializable]
        private sealed class SavedWall
        {
            public int id;
            public float startX;
            public float startY;
            public float endX;
            public float endY;
            public float thickness;
            public byte kind;
            public byte state;
        }

        public static bool TryNormalizeName(string value, out string normalized, out string error)
        {
            normalized = (value ?? string.Empty).Trim();
            error = string.Empty;
            if (normalized.Length == 0)
            {
                error = "Scrivi un nome per la mappa.";
                return false;
            }

            if (normalized.Length > MaximumMapNameLength)
            {
                error = $"Il nome può contenere al massimo {MaximumMapNameLength} caratteri.";
                return false;
            }

            foreach (var character in normalized)
            {
                if (!char.IsLetterOrDigit(character) && character != ' ' &&
                    character != '-' && character != '_' && character != '\'')
                {
                    error = "Usa soltanto lettere, numeri, spazi, trattini o apostrofi.";
                    return false;
                }
            }

            return true;
        }

        public static string Serialize(
            string mapName,
            Vector2 mapSize,
            IReadOnlyList<WallData> walls)
        {
            if (!TryNormalizeName(mapName, out var normalizedName, out var error))
            {
                throw new ArgumentException(error, nameof(mapName));
            }

            var data = new SavedMapFile
            {
                version = CurrentVersion,
                name = normalizedName,
                width = mapSize.x,
                height = mapSize.y
            };
            if (walls != null)
            {
                foreach (var wall in walls)
                {
                    if (wall == null)
                    {
                        continue;
                    }

                    data.walls.Add(new SavedWall
                    {
                        id = wall.Id,
                        startX = wall.Start.x,
                        startY = wall.Start.y,
                        endX = wall.End.x,
                        endY = wall.End.y,
                        thickness = wall.ThicknessMeters,
                        kind = (byte)wall.Kind,
                        state = (byte)wall.State
                    });
                }
            }

            return JsonUtility.ToJson(data, true);
        }

        public static bool TryDeserialize(
            string json,
            out string mapName,
            out Vector2 mapSize,
            out List<WallNetworkSnapshot> walls,
            out string error)
        {
            mapName = string.Empty;
            mapSize = Vector2.zero;
            walls = new List<WallNetworkSnapshot>();
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Il file della mappa è vuoto.";
                return false;
            }

            SavedMapFile data;
            try
            {
                data = JsonUtility.FromJson<SavedMapFile>(json);
            }
            catch (Exception)
            {
                error = "Il file della mappa non è un JSON valido.";
                return false;
            }

            if (data == null || data.version != CurrentVersion ||
                !TryNormalizeName(data.name, out mapName, out _))
            {
                error = "Questa mappa non è valida o usa una versione non supportata.";
                return false;
            }

            if (!IsFinite(data.width) || !IsFinite(data.height) ||
                data.width < TacticalMapManager.MinimumMapSizeMeters ||
                data.width > TacticalMapManager.MaximumMapSizeMeters ||
                data.height < TacticalMapManager.MinimumMapSizeMeters ||
                data.height > TacticalMapManager.MaximumMapSizeMeters)
            {
                error = "Le dimensioni salvate della mappa non sono valide.";
                return false;
            }

            if (data.walls == null || data.walls.Count > TacticalMapManager.MaximumWalls)
            {
                error = "Il numero di muri salvati non è valido.";
                return false;
            }

            var wallIds = new HashSet<int>();
            foreach (var wall in data.walls)
            {
                if (wall == null || wall.id <= 0 || !wallIds.Add(wall.id) ||
                    !IsFinite(wall.startX) || !IsFinite(wall.startY) ||
                    !IsFinite(wall.endX) || !IsFinite(wall.endY) ||
                    !IsFinite(wall.thickness) ||
                    wall.thickness < TacticalMapManager.MinimumWallThicknessMeters ||
                    wall.thickness > TacticalMapManager.MaximumWallThicknessMeters ||
                    wall.kind > (byte)AcousticObstacleKind.Door ||
                    wall.state > (byte)DoorState.Locked)
                {
                    error = "Uno dei muri salvati contiene dati non validi.";
                    walls.Clear();
                    return false;
                }

                var start = new Vector2(wall.startX, wall.startY);
                var end = new Vector2(wall.endX, wall.endY);
                if (Vector2.Distance(start, end) < 0.5f)
                {
                    error = "Uno dei muri salvati è troppo corto.";
                    walls.Clear();
                    return false;
                }

                walls.Add(new WallNetworkSnapshot(
                    wall.id,
                    start,
                    end,
                    wall.thickness,
                    wall.kind == (byte)AcousticObstacleKind.Door
                        ? AcousticObstacleKind.Door
                        : AcousticObstacleKind.Wall,
                    wall.state == (byte)DoorState.Open
                        ? DoorState.Open
                        : wall.state == (byte)DoorState.Locked
                            ? DoorState.Locked
                            : DoorState.Closed));
            }

            mapSize = new Vector2(data.width, data.height);
            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    internal static class MapSaveStorage
    {
        private const string FolderName = "SavedMaps";
        private const string FilePrefix = "map_";
        private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);

        internal static bool Exists(string mapName)
        {
            return TryGetPath(mapName, out var path, out _) && File.Exists(path);
        }

        internal static bool TrySave(
            string mapName,
            Vector2 mapSize,
            IReadOnlyList<WallData> walls,
            out string normalizedName,
            out string error)
        {
            normalizedName = string.Empty;
            if (!TryGetPath(mapName, out var path, out error) ||
                !MapSaveSerializer.TryNormalizeName(mapName, out normalizedName, out error))
            {
                return false;
            }

            var temporaryPath = path + ".tmp";
            try
            {
                Directory.CreateDirectory(GetDirectoryPath());
                File.WriteAllText(
                    temporaryPath,
                    MapSaveSerializer.Serialize(normalizedName, mapSize, walls),
                    Utf8WithoutBom);
                if (File.Exists(path))
                {
                    File.Replace(temporaryPath, path, null);
                }
                else
                {
                    File.Move(temporaryPath, path);
                }

                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                TryDeleteTemporaryFile(temporaryPath);
                error = $"Impossibile salvare la mappa: {exception.Message}";
                return false;
            }
        }

        internal static bool TryLoad(
            string mapName,
            out string normalizedName,
            out Vector2 mapSize,
            out List<WallNetworkSnapshot> walls,
            out string error)
        {
            normalizedName = string.Empty;
            mapSize = Vector2.zero;
            walls = new List<WallNetworkSnapshot>();
            if (!TryGetPath(mapName, out var path, out error))
            {
                return false;
            }

            try
            {
                if (!File.Exists(path))
                {
                    error = "La mappa salvata non esiste più.";
                    return false;
                }

                return MapSaveSerializer.TryDeserialize(
                    File.ReadAllText(path, Utf8WithoutBom),
                    out normalizedName,
                    out mapSize,
                    out walls,
                    out error);
            }
            catch (Exception exception)
            {
                error = $"Impossibile caricare la mappa: {exception.Message}";
                return false;
            }
        }

        internal static bool TryDelete(string mapName, out string error)
        {
            if (!TryGetPath(mapName, out var path, out error))
            {
                return false;
            }

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                error = string.Empty;
                return true;
            }
            catch (Exception exception)
            {
                error = $"Impossibile eliminare la mappa: {exception.Message}";
                return false;
            }
        }

        internal static List<string> GetSavedMapNames(out string error)
        {
            var names = new List<string>();
            error = string.Empty;
            try
            {
                var directory = GetDirectoryPath();
                if (!Directory.Exists(directory))
                {
                    return names;
                }

                foreach (var path in Directory.GetFiles(directory, FilePrefix + "*.json"))
                {
                    if (MapSaveSerializer.TryDeserialize(
                            File.ReadAllText(path, Utf8WithoutBom),
                            out var mapName,
                            out _,
                            out _,
                            out _))
                    {
                        names.Add(mapName);
                    }
                }

                names.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception exception)
            {
                error = $"Impossibile leggere le mappe salvate: {exception.Message}";
            }

            return names;
        }

        private static bool TryGetPath(string mapName, out string path, out string error)
        {
            path = string.Empty;
            if (!MapSaveSerializer.TryNormalizeName(mapName, out var normalizedName, out error))
            {
                return false;
            }

            var safeFileName = normalizedName.Replace(' ', '_');
            path = Path.Combine(GetDirectoryPath(), FilePrefix + safeFileName + ".json");
            return true;
        }

        private static string GetDirectoryPath()
        {
            return Path.Combine(Application.persistentDataPath, FolderName);
        }

        private static void TryDeleteTemporaryFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Il file temporaneo non contiene dati dell'utente necessari.
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class TacticalMapManager : MonoBehaviour
    {
        public const int MaximumWalls = 44;
        public const float MinimumMapSizeMeters = 32f;
        public const float MaximumMapSizeMeters = 96f;
        public const float MapResizeStepMeters = 8f;
        public const float GridSizeMeters = 1f;
        public const float MinimumWallThicknessMeters = 0.2f;
        public const float MaximumWallThicknessMeters = 2f;
        private const float DefaultMapSizeMeters = 48f;
        private const float MinimumWallLengthMeters = 0.5f;
        private const float DoorLengthMeters = 2f;
        private const float MinimumDoorSideMeters = 0.5f;
        private const float ClosedDoorOcclusion = 0.58f;
        private const float ComparisonEpsilon = 0.001f;

        private readonly List<WallData> walls = new List<WallData>();
        private readonly List<RoomData> rooms = new List<RoomData>();
        private DiscordSessionManager sessionManager;
        private int nextWallId = 1;
        private Vector2 mapSizeMeters = new Vector2(DefaultMapSizeMeters, DefaultMapSizeMeters);

        public event Action MapChanged;

        public IReadOnlyList<WallData> Walls => walls;

        public IReadOnlyList<RoomData> Rooms => rooms;

        public Vector2 MapSizeMeters => mapSizeMeters;

        public Rect MapBounds => new Rect(
            mapSizeMeters.x * -0.5f,
            mapSizeMeters.y * -0.5f,
            mapSizeMeters.x,
            mapSizeMeters.y);

        public bool CanEdit => sessionManager?.State == DiscordSessionState.Joined && sessionManager.IsHost;

        public string LastError { get; private set; } = string.Empty;

        public void Initialize(DiscordSessionManager discordSessionManager)
        {
            if (sessionManager != null)
            {
                sessionManager.StateChanged -= OnSessionStateChanged;
            }

            sessionManager = discordSessionManager;
            if (sessionManager != null)
            {
                sessionManager.StateChanged += OnSessionStateChanged;
            }
        }

        public bool TryResizeMap(Vector2 requestedSizeMeters)
        {
            if (!CanEdit)
            {
                LastError = "Solo il Dungeon Master può ridimensionare la mappa.";
                return false;
            }

            var nextSize = ClampMapSize(requestedSizeMeters);
            if (Approximately(nextSize, mapSizeMeters))
            {
                return true;
            }

            mapSizeMeters = nextSize;
            ClampWallsToMap();
            LastError = string.Empty;
            NotifyMapChanged();
            return true;
        }

        public IReadOnlyList<string> GetSavedMapNames()
        {
            var names = MapSaveStorage.GetSavedMapNames(out var error);
            LastError = error;
            return names;
        }

        public bool SavedMapExists(string mapName)
        {
            return MapSaveStorage.Exists(mapName);
        }

        public bool TrySaveCurrentMap(string mapName, out string normalizedName)
        {
            normalizedName = string.Empty;
            if (!CanEdit)
            {
                LastError = "Solo il Dungeon Master può salvare la mappa.";
                return false;
            }

            if (!MapSaveStorage.TrySave(
                    mapName,
                    mapSizeMeters,
                    walls,
                    out normalizedName,
                    out var error))
            {
                LastError = error;
                return false;
            }

            LastError = string.Empty;
            return true;
        }

        public bool TryLoadSavedMap(string mapName, out string normalizedName)
        {
            normalizedName = string.Empty;
            if (!CanEdit)
            {
                LastError = "Solo il Dungeon Master può caricare una mappa.";
                return false;
            }

            if (!MapSaveStorage.TryLoad(
                    mapName,
                    out normalizedName,
                    out var loadedSize,
                    out var loadedWalls,
                    out var error))
            {
                LastError = error;
                return false;
            }

            ReplaceMap(loadedSize, loadedWalls);
            return true;
        }

        public bool TryDeleteSavedMap(string mapName)
        {
            if (!CanEdit)
            {
                LastError = "Solo il Dungeon Master può eliminare una mappa salvata.";
                return false;
            }

            if (!MapSaveStorage.TryDelete(mapName, out var error))
            {
                LastError = error;
                return false;
            }

            LastError = string.Empty;
            return true;
        }

        public bool TryCreateWall(
            Vector2 start,
            Vector2 end,
            float thicknessMeters,
            out WallData wall)
        {
            wall = null;
            LastError = string.Empty;
            if (!CanEdit)
            {
                LastError = "Solo il Dungeon Master può disegnare muri.";
                return false;
            }

            if (walls.Count >= MaximumWalls)
            {
                LastError = $"Puoi creare al massimo {MaximumWalls} segmenti di muro.";
                return false;
            }

            start = SnapPosition(start);
            end = SnapPosition(end);
            if (Vector2.Distance(start, end) < MinimumWallLengthMeters)
            {
                LastError = "Il muro è troppo corto: disegna almeno mezzo metro.";
                return false;
            }

            foreach (var existing in walls)
            {
                var sameDirection = Approximately(existing.Start, start) && Approximately(existing.End, end);
                var oppositeDirection = Approximately(existing.Start, end) && Approximately(existing.End, start);
                if (!sameDirection && !oppositeDirection)
                {
                    continue;
                }

                LastError = "Questo muro esiste già.";
                return false;
            }

            wall = new WallData(
                nextWallId++,
                start,
                end,
                Mathf.Clamp(
                    thicknessMeters,
                    MinimumWallThicknessMeters,
                    MaximumWallThicknessMeters));
            walls.Add(wall);
            NotifyMapChanged();
            return true;
        }

        public bool TryRemoveWall(int wallId)
        {
            if (!CanEdit)
            {
                return false;
            }

            for (var index = 0; index < walls.Count; index++)
            {
                if (walls[index].Id != wallId)
                {
                    continue;
                }

                walls.RemoveAt(index);
                LastError = string.Empty;
                NotifyMapChanged();
                return true;
            }

            return false;
        }

        public bool TryInsertDoor(Vector2 position, out WallData door)
        {
            door = null;
            LastError = string.Empty;
            if (!CanEdit)
            {
                LastError = "Solo il Dungeon Master può aggiungere porte.";
                return false;
            }

            if (walls.Count > MaximumWalls - 2)
            {
                LastError = "Rimuovi qualche muro prima di aggiungere una porta.";
                return false;
            }

            var wallIndex = -1;
            var nearestDistance = float.MaxValue;
            for (var index = 0; index < walls.Count; index++)
            {
                var candidate = walls[index];
                if (candidate.IsDoor || candidate.LengthMeters < DoorLengthMeters + MinimumDoorSideMeters * 2f)
                {
                    continue;
                }

                var distance = DistancePointToSegment(position, candidate.Start, candidate.End);
                var hitRadius = Mathf.Max(0.6f, candidate.ThicknessMeters * 0.5f + 0.2f);
                if (distance > hitRadius || distance >= nearestDistance)
                {
                    continue;
                }

                wallIndex = index;
                nearestDistance = distance;
            }

            if (wallIndex < 0)
            {
                LastError = "Clicca sopra un muro lungo almeno 3 metri.";
                return false;
            }

            var source = walls[wallIndex];
            var direction = (source.End - source.Start).normalized;
            var distanceAlongWall = Vector2.Dot(position - source.Start, direction);
            distanceAlongWall = Mathf.Round(distanceAlongWall / GridSizeMeters) * GridSizeMeters;
            var halfDoor = DoorLengthMeters * 0.5f;
            distanceAlongWall = Mathf.Clamp(
                distanceAlongWall,
                halfDoor + MinimumDoorSideMeters,
                source.LengthMeters - halfDoor - MinimumDoorSideMeters);
            var doorStart = source.Start + direction * (distanceAlongWall - halfDoor);
            var doorEnd = source.Start + direction * (distanceAlongWall + halfDoor);

            var replacements = new List<WallData>(3);
            if (Vector2.Distance(source.Start, doorStart) >= MinimumWallLengthMeters)
            {
                replacements.Add(new WallData(
                    nextWallId++,
                    source.Start,
                    doorStart,
                    source.ThicknessMeters));
            }

            door = new WallData(
                nextWallId++,
                doorStart,
                doorEnd,
                source.ThicknessMeters,
                AcousticObstacleKind.Door,
                DoorState.Closed);
            replacements.Add(door);

            if (Vector2.Distance(doorEnd, source.End) >= MinimumWallLengthMeters)
            {
                replacements.Add(new WallData(
                    nextWallId++,
                    doorEnd,
                    source.End,
                    source.ThicknessMeters));
            }

            walls.RemoveAt(wallIndex);
            walls.InsertRange(wallIndex, replacements);
            NotifyMapChanged();
            return true;
        }

        public bool TryCycleDoorState(int doorId)
        {
            if (!CanEdit)
            {
                return false;
            }

            foreach (var obstacle in walls)
            {
                if (obstacle.Id != doorId || !obstacle.IsDoor)
                {
                    continue;
                }

                obstacle.State = obstacle.State == DoorState.Open
                    ? DoorState.Closed
                    : obstacle.State == DoorState.Closed
                        ? DoorState.Locked
                        : DoorState.Open;
                LastError = string.Empty;
                NotifyMapChanged();
                return true;
            }

            return false;
        }

        public void ClearWalls()
        {
            if (!CanEdit || walls.Count == 0)
            {
                return;
            }

            walls.Clear();
            LastError = string.Empty;
            NotifyMapChanged();
        }

        public WallData GetWallAt(Vector2 position)
        {
            WallData nearest = null;
            var nearestDistance = float.MaxValue;
            foreach (var wall in walls)
            {
                var distance = DistancePointToSegment(position, wall.Start, wall.End);
                var selectableRadius = Mathf.Max(0.35f, wall.ThicknessMeters * 0.5f);
                if (distance > selectableRadius || distance >= nearestDistance)
                {
                    continue;
                }

                nearest = wall;
                nearestDistance = distance;
            }

            return nearest;
        }

        public RoomData GetRoomAt(Vector2 position)
        {
            RoomData smallestRoom = null;
            foreach (var room in rooms)
            {
                if (!room.Contains(position) ||
                    smallestRoom != null && smallestRoom.AreaSquareMeters <= room.AreaSquareMeters)
                {
                    continue;
                }

                smallestRoom = room;
            }

            return smallestRoom;
        }

        public float CalculateOcclusion(Vector2 listenerPosition, Vector2 speakerPosition)
        {
            return CalculateOcclusion(walls, listenerPosition, speakerPosition);
        }

        public static float CalculateOcclusion(
            IReadOnlyList<WallData> wallSegments,
            Vector2 listenerPosition,
            Vector2 speakerPosition)
        {
            if (wallSegments == null || wallSegments.Count == 0 ||
                Vector2.Distance(listenerPosition, speakerPosition) <= ComparisonEpsilon)
            {
                return 0f;
            }

            var strength = 0f;
            foreach (var wall in wallSegments)
            {
                if (wall.IsDoor && wall.State == DoorState.Open)
                {
                    continue;
                }

                var crossingDistance = DistanceBetweenSegments(
                    listenerPosition,
                    speakerPosition,
                    wall.Start,
                    wall.End);
                if (crossingDistance > wall.ThicknessMeters * 0.5f + 0.03f)
                {
                    continue;
                }

                var normalizedThickness = Mathf.InverseLerp(
                    MinimumWallThicknessMeters,
                    MaximumWallThicknessMeters,
                    wall.ThicknessMeters);
                var obstacleStrength = Mathf.Lerp(0.2f, 0.86f, normalizedThickness);
                if (wall.IsDoor)
                {
                    obstacleStrength = Mathf.Max(obstacleStrength, ClosedDoorOcclusion);
                }

                strength += obstacleStrength;
                if (strength >= 1f)
                {
                    return 1f;
                }
            }

            return Mathf.Clamp01(strength);
        }

        public Vector2 ClampPosition(Vector2 position)
        {
            var bounds = MapBounds;
            return new Vector2(
                Mathf.Clamp(position.x, bounds.xMin, bounds.xMax),
                Mathf.Clamp(position.y, bounds.yMin, bounds.yMax));
        }

        public Vector2 SnapPosition(Vector2 position)
        {
            var clamped = ClampPosition(position);
            var endpointSnapDistance = GridSizeMeters * 0.8f;
            var segmentSnapDistance = GridSizeMeters * 0.32f;
            var nearestEndpoint = clamped;
            var nearestEndpointDistance = float.MaxValue;
            foreach (var wall in walls)
            {
                var startDistance = Vector2.Distance(clamped, wall.Start);
                if (startDistance < nearestEndpointDistance)
                {
                    nearestEndpoint = wall.Start;
                    nearestEndpointDistance = startDistance;
                }

                var endDistance = Vector2.Distance(clamped, wall.End);
                if (endDistance < nearestEndpointDistance)
                {
                    nearestEndpoint = wall.End;
                    nearestEndpointDistance = endDistance;
                }
            }

            if (nearestEndpointDistance <= endpointSnapDistance)
            {
                return nearestEndpoint;
            }

            var nearestPointOnWall = clamped;
            var nearestWallDistance = float.MaxValue;
            foreach (var wall in walls)
            {
                var pointOnWall = ClosestPointOnSegment(clamped, wall.Start, wall.End);
                var distance = Vector2.Distance(clamped, pointOnWall);
                if (distance >= nearestWallDistance)
                {
                    continue;
                }

                nearestPointOnWall = pointOnWall;
                nearestWallDistance = distance;
            }

            if (nearestWallDistance <= segmentSnapDistance)
            {
                return ClampPosition(nearestPointOnWall);
            }

            return ClampPosition(new Vector2(
                Mathf.Round(clamped.x / GridSizeMeters) * GridSizeMeters,
                Mathf.Round(clamped.y / GridSizeMeters) * GridSizeMeters));
        }

        public void ApplyAuthoritativeMap(
            Vector2 authoritativeMapSize,
            IReadOnlyList<WallNetworkSnapshot> snapshots)
        {
            if (CanEdit || snapshots == null)
            {
                return;
            }

            authoritativeMapSize = ClampMapSize(authoritativeMapSize);
            if (MapMatches(authoritativeMapSize, snapshots))
            {
                return;
            }

            ReplaceMap(authoritativeMapSize, snapshots);
        }

        private void ReplaceMap(
            Vector2 requestedMapSize,
            IReadOnlyList<WallNetworkSnapshot> snapshots)
        {
            mapSizeMeters = ClampMapSize(requestedMapSize);
            walls.Clear();
            var maximumId = 0;
            foreach (var snapshot in snapshots)
            {
                if (snapshot.Id <= 0 || walls.Count >= MaximumWalls)
                {
                    continue;
                }

                var start = ClampPosition(snapshot.Start);
                var end = ClampPosition(snapshot.End);
                if (Vector2.Distance(start, end) < MinimumWallLengthMeters)
                {
                    continue;
                }

                walls.Add(new WallData(
                    snapshot.Id,
                    start,
                    end,
                    Mathf.Clamp(
                        snapshot.ThicknessMeters,
                        MinimumWallThicknessMeters,
                        MaximumWallThicknessMeters),
                    snapshot.Kind == AcousticObstacleKind.Door
                        ? AcousticObstacleKind.Door
                        : AcousticObstacleKind.Wall,
                    snapshot.DoorState == DoorState.Open ||
                    snapshot.DoorState == DoorState.Locked
                        ? snapshot.DoorState
                        : DoorState.Closed));
                maximumId = Mathf.Max(maximumId, snapshot.Id);
            }

            nextWallId = maximumId + 1;
            LastError = string.Empty;
            NotifyMapChanged();
        }

        private bool MapMatches(Vector2 size, IReadOnlyList<WallNetworkSnapshot> snapshots)
        {
            if (!Approximately(mapSizeMeters, size) || walls.Count != snapshots.Count)
            {
                return false;
            }

            for (var index = 0; index < walls.Count; index++)
            {
                var wall = walls[index];
                var snapshot = snapshots[index];
                if (wall.Id != snapshot.Id ||
                    !Approximately(wall.Start, snapshot.Start) ||
                    !Approximately(wall.End, snapshot.End) ||
                    Mathf.Abs(wall.ThicknessMeters - snapshot.ThicknessMeters) > ComparisonEpsilon ||
                    wall.Kind != snapshot.Kind ||
                    wall.State != snapshot.DoorState)
                {
                    return false;
                }
            }

            return true;
        }

        private void NotifyMapChanged()
        {
            RoomDetector.Detect(walls, rooms);
            MapChanged?.Invoke();
        }

        private void ClampWallsToMap()
        {
            for (var index = walls.Count - 1; index >= 0; index--)
            {
                var wall = walls[index];
                wall.Start = ClampPosition(wall.Start);
                wall.End = ClampPosition(wall.End);
                if (wall.LengthMeters < MinimumWallLengthMeters)
                {
                    walls.RemoveAt(index);
                }
            }
        }

        private static Vector2 ClampMapSize(Vector2 size)
        {
            return new Vector2(SnapMapDimension(size.x), SnapMapDimension(size.y));
        }

        private static float SnapMapDimension(float value)
        {
            var clamped = Mathf.Clamp(value, MinimumMapSizeMeters, MaximumMapSizeMeters);
            return Mathf.Round(clamped / MapResizeStepMeters) * MapResizeStepMeters;
        }

        private static float DistanceBetweenSegments(
            Vector2 firstStart,
            Vector2 firstEnd,
            Vector2 secondStart,
            Vector2 secondEnd)
        {
            if (SegmentsIntersect(firstStart, firstEnd, secondStart, secondEnd))
            {
                return 0f;
            }

            return Mathf.Min(
                Mathf.Min(
                    DistancePointToSegment(firstStart, secondStart, secondEnd),
                    DistancePointToSegment(firstEnd, secondStart, secondEnd)),
                Mathf.Min(
                    DistancePointToSegment(secondStart, firstStart, firstEnd),
                    DistancePointToSegment(secondEnd, firstStart, firstEnd)));
        }

        private static bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            var first = Cross(b - a, c - a);
            var second = Cross(b - a, d - a);
            var third = Cross(d - c, a - c);
            var fourth = Cross(d - c, b - c);
            if (Mathf.Abs(first) <= ComparisonEpsilon && PointInsideSegmentBounds(c, a, b) ||
                Mathf.Abs(second) <= ComparisonEpsilon && PointInsideSegmentBounds(d, a, b) ||
                Mathf.Abs(third) <= ComparisonEpsilon && PointInsideSegmentBounds(a, c, d) ||
                Mathf.Abs(fourth) <= ComparisonEpsilon && PointInsideSegmentBounds(b, c, d))
            {
                return true;
            }

            return ((first > ComparisonEpsilon && second < -ComparisonEpsilon) ||
                    (first < -ComparisonEpsilon && second > ComparisonEpsilon)) &&
                   ((third > ComparisonEpsilon && fourth < -ComparisonEpsilon) ||
                    (third < -ComparisonEpsilon && fourth > ComparisonEpsilon));
        }

        private static bool PointInsideSegmentBounds(Vector2 point, Vector2 start, Vector2 end)
        {
            return point.x >= Mathf.Min(start.x, end.x) - ComparisonEpsilon &&
                   point.x <= Mathf.Max(start.x, end.x) + ComparisonEpsilon &&
                   point.y >= Mathf.Min(start.y, end.y) - ComparisonEpsilon &&
                   point.y <= Mathf.Max(start.y, end.y) + ComparisonEpsilon;
        }

        private static float DistancePointToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            return Vector2.Distance(point, ClosestPointOnSegment(point, start, end));
        }

        private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            var segment = end - start;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= ComparisonEpsilon)
            {
                return start;
            }

            var amount = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            return start + segment * amount;
        }

        private static float Cross(Vector2 first, Vector2 second)
        {
            return first.x * second.y - first.y * second.x;
        }

        private static bool Approximately(Vector2 first, Vector2 second)
        {
            return Vector2.SqrMagnitude(first - second) <= ComparisonEpsilon * ComparisonEpsilon;
        }

        private void OnSessionStateChanged(DiscordSessionState state)
        {
            if (state != DiscordSessionState.Ready && state != DiscordSessionState.WaitingForDiscord)
            {
                return;
            }

            walls.Clear();
            mapSizeMeters = new Vector2(DefaultMapSizeMeters, DefaultMapSizeMeters);
            nextWallId = 1;
            LastError = string.Empty;
            NotifyMapChanged();
        }

        private void OnDestroy()
        {
            if (sessionManager != null)
            {
                sessionManager.StateChanged -= OnSessionStateChanged;
            }

            sessionManager = null;
        }
    }
}
