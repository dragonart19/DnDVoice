using System.Collections.Generic;
using DndProximityVoice.Map;
using DndProximityVoice.Players;
using DndProximityVoice.Voice;
using NUnit.Framework;
using UnityEngine;

namespace DndProximityVoice.Tests.EditMode
{
    public sealed class WallAcousticsTests
    {
        [Test]
        public void PrivateGroupsOnlyHearMatchingMembersWhenIsolationIsEnabled()
        {
            Assert.That(
                PrivateVoiceGroupRules.CanHear(
                    true,
                    PrivateVoiceGroup.A,
                    PrivateVoiceGroup.A),
                Is.True);
            Assert.That(
                PrivateVoiceGroupRules.CanHear(
                    true,
                    PrivateVoiceGroup.A,
                    PrivateVoiceGroup.B),
                Is.False);
            Assert.That(
                PrivateVoiceGroupRules.CanHear(
                    true,
                    PrivateVoiceGroup.None,
                    PrivateVoiceGroup.None),
                Is.False);
        }

        [Test]
        public void DisabledPrivateGroupIsolationKeepsEveryoneAudible()
        {
            Assert.That(
                PrivateVoiceGroupRules.CanHear(
                    false,
                    PrivateVoiceGroup.A,
                    PrivateVoiceGroup.C),
                Is.True);
        }

        [Test]
        public void SavedMapRoundTripPreservesDimensionsWallsAndDoorState()
        {
            var sourceWalls = new List<WallData>
            {
                new WallData(
                    1,
                    new Vector2(-4f, -3f),
                    new Vector2(4f, -3f),
                    0.6f),
                new WallData(
                    2,
                    new Vector2(4f, -3f),
                    new Vector2(4f, 3f),
                    1.2f,
                    AcousticObstacleKind.Door,
                    DoorState.Locked)
            };

            var json = MapSaveSerializer.Serialize(
                "Castello di Strahd",
                new Vector2(64f, 80f),
                sourceWalls);

            Assert.That(
                MapSaveSerializer.TryDeserialize(
                    json,
                    out var mapName,
                    out var mapSize,
                    out var restoredWalls,
                    out var error),
                Is.True,
                error);
            Assert.That(mapName, Is.EqualTo("Castello di Strahd"));
            Assert.That(mapSize, Is.EqualTo(new Vector2(64f, 80f)));
            Assert.That(restoredWalls.Count, Is.EqualTo(2));
            Assert.That(restoredWalls[0].Start, Is.EqualTo(new Vector2(-4f, -3f)));
            Assert.That(restoredWalls[0].ThicknessMeters, Is.EqualTo(0.6f));
            Assert.That(restoredWalls[1].Kind, Is.EqualTo(AcousticObstacleKind.Door));
            Assert.That(restoredWalls[1].DoorState, Is.EqualTo(DoorState.Locked));
        }

        [Test]
        public void SavedMapRejectsUnsafeFileNameCharacters()
        {
            Assert.That(
                MapSaveSerializer.TryNormalizeName(
                    "../mappa",
                    out _,
                    out var error),
                Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void WallDataPreservesFreeformSegment()
        {
            var wall = new WallData(3, new Vector2(-2f, 1f), new Vector2(4f, 5f), 0.8f);

            Assert.That(wall.Name, Is.EqualTo("MURO 03"));
            Assert.That(wall.Start, Is.EqualTo(new Vector2(-2f, 1f)));
            Assert.That(wall.End, Is.EqualTo(new Vector2(4f, 5f)));
            Assert.That(wall.ThicknessMeters, Is.EqualTo(0.8f));
        }

        [Test]
        public void NetworkSnapshotPreservesWallGeometryAndThickness()
        {
            var snapshot = new WallNetworkSnapshot(
                4,
                new Vector2(-6f, 2f),
                new Vector2(8f, 7f),
                1.25f);

            Assert.That(snapshot.Id, Is.EqualTo(4));
            Assert.That(snapshot.Start, Is.EqualTo(new Vector2(-6f, 2f)));
            Assert.That(snapshot.End, Is.EqualTo(new Vector2(8f, 7f)));
            Assert.That(snapshot.ThicknessMeters, Is.EqualTo(1.25f));
        }

        [Test]
        public void CrossingThickWallAttenuatesMoreThanThinWall()
        {
            var listener = new Vector2(-3f, 0f);
            var speaker = new Vector2(3f, 0f);
            var thin = new List<WallData>
            {
                new WallData(1, new Vector2(0f, -4f), new Vector2(0f, 4f), 0.2f)
            };
            var thick = new List<WallData>
            {
                new WallData(1, new Vector2(0f, -4f), new Vector2(0f, 4f), 2f)
            };

            var thinOcclusion = TacticalMapManager.CalculateOcclusion(thin, listener, speaker);
            var thickOcclusion = TacticalMapManager.CalculateOcclusion(thick, listener, speaker);

            Assert.That(thinOcclusion, Is.GreaterThan(0f));
            Assert.That(thickOcclusion, Is.GreaterThan(thinOcclusion));
            Assert.That(
                VoiceAudioSource.CalculateWallGain(thickOcclusion),
                Is.LessThan(VoiceAudioSource.CalculateWallGain(thinOcclusion)));
        }

        [Test]
        public void WallAwayFromVoicePathDoesNotAttenuate()
        {
            var walls = new List<WallData>
            {
                new WallData(1, new Vector2(0f, 5f), new Vector2(4f, 5f), 2f)
            };

            var occlusion = TacticalMapManager.CalculateOcclusion(
                walls,
                new Vector2(-3f, 0f),
                new Vector2(3f, 0f));

            Assert.That(occlusion, Is.EqualTo(0f));
        }

        [Test]
        public void SnapUsesOneMeterGridAndExistingEndpoints()
        {
            var root = new GameObject("Tactical map snap test");
            try
            {
                var map = root.AddComponent<TacticalMapManager>();
                map.ApplyAuthoritativeMap(
                    new Vector2(48f, 48f),
                    new[]
                    {
                        new WallNetworkSnapshot(
                            1,
                            new Vector2(-2f, -2f),
                            new Vector2(2f, -2f),
                            0.5f)
                    });

                Assert.That(map.SnapPosition(new Vector2(5.35f, 4.6f)), Is.EqualTo(new Vector2(5f, 5f)));
                Assert.That(map.SnapPosition(new Vector2(2.45f, -1.7f)), Is.EqualTo(new Vector2(2f, -2f)));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void OpenDoorDoesNotBlockVoiceWhileClosedDoorDoes()
        {
            var openDoor = new WallData(
                8,
                new Vector2(0f, -1f),
                new Vector2(0f, 1f),
                0.5f,
                AcousticObstacleKind.Door,
                DoorState.Open);
            var closedDoor = new WallData(
                9,
                new Vector2(0f, -1f),
                new Vector2(0f, 1f),
                0.5f,
                AcousticObstacleKind.Door,
                DoorState.Closed);

            var openOcclusion = TacticalMapManager.CalculateOcclusion(
                new List<WallData> { openDoor },
                new Vector2(-3f, 0f),
                new Vector2(3f, 0f));
            var closedOcclusion = TacticalMapManager.CalculateOcclusion(
                new List<WallData> { closedDoor },
                new Vector2(-3f, 0f),
                new Vector2(3f, 0f));

            Assert.That(openOcclusion, Is.EqualTo(0f));
            Assert.That(closedOcclusion, Is.GreaterThan(0.57f));
        }

        [Test]
        public void ClosedWallLoopCreatesARecognizedRoom()
        {
            var root = new GameObject("Closed room detection test");
            try
            {
                var map = root.AddComponent<TacticalMapManager>();
                map.ApplyAuthoritativeMap(
                    new Vector2(48f, 48f),
                    new[]
                    {
                        new WallNetworkSnapshot(1, new Vector2(-4f, -3f), new Vector2(4f, -3f), 0.5f),
                        new WallNetworkSnapshot(2, new Vector2(4f, -3f), new Vector2(4f, 3f), 0.5f),
                        new WallNetworkSnapshot(3, new Vector2(4f, 3f), new Vector2(-4f, 3f), 0.5f),
                        new WallNetworkSnapshot(4, new Vector2(-4f, 3f), new Vector2(-4f, -3f), 0.5f)
                    });

                Assert.That(map.Rooms.Count, Is.EqualTo(1));
                Assert.That(map.Rooms[0].AreaSquareMeters, Is.EqualTo(48f).Within(0.01f));
                Assert.That(map.GetRoomAt(Vector2.zero), Is.SameAs(map.Rooms[0]));
                Assert.That(map.GetRoomAt(new Vector2(8f, 8f)), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void OpenWallChainDoesNotCreateARoom()
        {
            var root = new GameObject("Open room detection test");
            try
            {
                var map = root.AddComponent<TacticalMapManager>();
                map.ApplyAuthoritativeMap(
                    new Vector2(48f, 48f),
                    new[]
                    {
                        new WallNetworkSnapshot(1, new Vector2(-4f, -3f), new Vector2(4f, -3f), 0.5f),
                        new WallNetworkSnapshot(2, new Vector2(4f, -3f), new Vector2(4f, 3f), 0.5f),
                        new WallNetworkSnapshot(3, new Vector2(4f, 3f), new Vector2(-4f, 3f), 0.5f)
                    });

                Assert.That(map.Rooms, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DoorSegmentsStillCompleteTheRoomBoundary()
        {
            var root = new GameObject("Room with door detection test");
            try
            {
                var map = root.AddComponent<TacticalMapManager>();
                map.ApplyAuthoritativeMap(
                    new Vector2(48f, 48f),
                    new[]
                    {
                        new WallNetworkSnapshot(1, new Vector2(-4f, -3f), new Vector2(-1f, -3f), 0.5f),
                        new WallNetworkSnapshot(
                            2,
                            new Vector2(-1f, -3f),
                            new Vector2(1f, -3f),
                            0.5f,
                            AcousticObstacleKind.Door,
                            DoorState.Open),
                        new WallNetworkSnapshot(3, new Vector2(1f, -3f), new Vector2(4f, -3f), 0.5f),
                        new WallNetworkSnapshot(4, new Vector2(4f, -3f), new Vector2(4f, 3f), 0.5f),
                        new WallNetworkSnapshot(5, new Vector2(4f, 3f), new Vector2(-4f, 3f), 0.5f),
                        new WallNetworkSnapshot(6, new Vector2(-4f, 3f), new Vector2(-4f, -3f), 0.5f)
                    });

                Assert.That(map.Rooms.Count, Is.EqualTo(1));
                Assert.That(map.GetRoomAt(Vector2.zero), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
