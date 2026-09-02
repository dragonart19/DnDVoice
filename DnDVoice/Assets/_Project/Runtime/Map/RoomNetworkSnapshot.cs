using UnityEngine;

namespace DndProximityVoice.Map
{
    public readonly struct WallNetworkSnapshot
    {
        public WallNetworkSnapshot(int id, Vector2 start, Vector2 end, float thicknessMeters)
            : this(
                id,
                start,
                end,
                thicknessMeters,
                AcousticObstacleKind.Wall,
                DoorState.Closed)
        {
        }

        public WallNetworkSnapshot(
            int id,
            Vector2 start,
            Vector2 end,
            float thicknessMeters,
            AcousticObstacleKind kind,
            DoorState doorState)
        {
            Id = id;
            Start = start;
            End = end;
            ThicknessMeters = thicknessMeters;
            Kind = kind;
            DoorState = doorState;
        }

        public int Id { get; }

        public Vector2 Start { get; }

        public Vector2 End { get; }

        public float ThicknessMeters { get; }

        public AcousticObstacleKind Kind { get; }

        public DoorState DoorState { get; }
    }
}
