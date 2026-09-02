using System;
using System.Collections.Generic;
using UnityEngine;

namespace DndProximityVoice.Map
{
    public enum AcousticObstacleKind : byte
    {
        Wall = 0,
        Door = 1
    }

    public enum DoorState : byte
    {
        Open = 0,
        Closed = 1,
        Locked = 2
    }

    public sealed class WallData
    {
        public WallData(
            int id,
            Vector2 start,
            Vector2 end,
            float thicknessMeters,
            AcousticObstacleKind kind = AcousticObstacleKind.Wall,
            DoorState doorState = DoorState.Closed)
        {
            Id = id;
            Start = start;
            End = end;
            ThicknessMeters = thicknessMeters;
            Kind = kind;
            State = doorState;
        }

        public int Id { get; }

        public string Name => IsDoor ? $"PORTA {Id:00}" : $"MURO {Id:00}";

        public bool IsDoor => Kind == AcousticObstacleKind.Door;

        public Vector2 Start { get; internal set; }

        public Vector2 End { get; internal set; }

        public float ThicknessMeters { get; internal set; }

        public AcousticObstacleKind Kind { get; }

        public DoorState State { get; internal set; }

        public float LengthMeters => Vector2.Distance(Start, End);
    }

    public sealed class RoomData
    {
        private readonly List<Vector2> boundary;

        internal RoomData(int id, IReadOnlyList<Vector2> points)
        {
            Id = id;
            boundary = new List<Vector2>(points);
            SignedArea = CalculateSignedArea(boundary);
            Center = CalculateCentroid(boundary, SignedArea);
        }

        public int Id { get; }

        public string Name => $"STANZA {Id:00}";

        public IReadOnlyList<Vector2> Boundary => boundary;

        public Vector2 Center { get; }

        public float AreaSquareMeters => Mathf.Abs(SignedArea);

        private float SignedArea { get; }

        public bool Contains(Vector2 point)
        {
            var inside = false;
            for (var current = 0; current < boundary.Count; current++)
            {
                var previous = current == 0 ? boundary.Count - 1 : current - 1;
                var first = boundary[current];
                var second = boundary[previous];
                if (DistancePointToSegment(point, first, second) <= RoomDetector.GeometryEpsilon)
                {
                    return true;
                }

                var crosses = (first.y > point.y) != (second.y > point.y) &&
                              point.x < (second.x - first.x) * (point.y - first.y) /
                              (second.y - first.y) + first.x;
                if (crosses)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static float CalculateSignedArea(IReadOnlyList<Vector2> points)
        {
            var doubleArea = 0f;
            for (var index = 0; index < points.Count; index++)
            {
                var next = (index + 1) % points.Count;
                doubleArea += points[index].x * points[next].y - points[next].x * points[index].y;
            }

            return doubleArea * 0.5f;
        }

        private static Vector2 CalculateCentroid(IReadOnlyList<Vector2> points, float signedArea)
        {
            if (Mathf.Abs(signedArea) <= RoomDetector.GeometryEpsilon)
            {
                var average = Vector2.zero;
                foreach (var point in points)
                {
                    average += point;
                }

                return points.Count == 0 ? Vector2.zero : average / points.Count;
            }

            var centroid = Vector2.zero;
            var factorSum = 0f;
            for (var index = 0; index < points.Count; index++)
            {
                var next = (index + 1) % points.Count;
                var factor = points[index].x * points[next].y - points[next].x * points[index].y;
                centroid += (points[index] + points[next]) * factor;
                factorSum += factor;
            }

            return centroid / (3f * factorSum);
        }

        private static float DistancePointToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            var segment = end - start;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= RoomDetector.GeometryEpsilon * RoomDetector.GeometryEpsilon)
            {
                return Vector2.Distance(point, start);
            }

            var amount = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            return Vector2.Distance(point, start + segment * amount);
        }
    }

    internal static class RoomDetector
    {
        internal const float GeometryEpsilon = 0.02f;
        private const float MinimumRoomArea = 1f;

        private sealed class SourceSegment
        {
            internal SourceSegment(Vector2 start, Vector2 end)
            {
                Start = start;
                End = end;
                Cuts.Add(0f);
                Cuts.Add(1f);
            }

            internal Vector2 Start { get; }
            internal Vector2 End { get; }
            internal List<float> Cuts { get; } = new List<float>();
        }

        private sealed class GraphVertex
        {
            internal GraphVertex(Vector2 position)
            {
                Position = position;
            }

            internal Vector2 Position { get; }
            internal List<int> Neighbours { get; } = new List<int>();
        }

        private sealed class DetectedPolygon
        {
            internal DetectedPolygon(List<Vector2> points)
            {
                Points = points;
                Area = SignedArea(points);
                Center = Average(points);
            }

            internal List<Vector2> Points { get; }
            internal float Area { get; }
            internal Vector2 Center { get; }
        }

        internal static void Detect(IReadOnlyList<WallData> obstacles, List<RoomData> destination)
        {
            destination.Clear();
            if (obstacles == null || obstacles.Count < 3)
            {
                return;
            }

            var segments = BuildSourceSegments(obstacles);
            AddJunctionCuts(segments);
            var vertices = BuildGraph(segments);
            if (vertices.Count < 3)
            {
                return;
            }

            SortNeighbours(vertices);
            var polygons = TraceBoundedFaces(vertices);
            polygons.Sort((first, second) =>
            {
                var vertical = -first.Center.y.CompareTo(second.Center.y);
                return vertical != 0 ? vertical : first.Center.x.CompareTo(second.Center.x);
            });

            for (var index = 0; index < polygons.Count; index++)
            {
                destination.Add(new RoomData(index + 1, polygons[index].Points));
            }
        }

        private static List<SourceSegment> BuildSourceSegments(IReadOnlyList<WallData> obstacles)
        {
            var segments = new List<SourceSegment>(obstacles.Count);
            foreach (var obstacle in obstacles)
            {
                if (obstacle.LengthMeters > GeometryEpsilon)
                {
                    segments.Add(new SourceSegment(obstacle.Start, obstacle.End));
                }
            }

            return segments;
        }

        private static void AddJunctionCuts(IReadOnlyList<SourceSegment> segments)
        {
            for (var firstIndex = 0; firstIndex < segments.Count; firstIndex++)
            {
                for (var secondIndex = firstIndex + 1; secondIndex < segments.Count; secondIndex++)
                {
                    var first = segments[firstIndex];
                    var second = segments[secondIndex];
                    AddPointCut(first, second.Start);
                    AddPointCut(first, second.End);
                    AddPointCut(second, first.Start);
                    AddPointCut(second, first.End);

                    if (TryIntersect(first.Start, first.End, second.Start, second.End, out var firstAmount, out var secondAmount))
                    {
                        AddCut(first.Cuts, firstAmount);
                        AddCut(second.Cuts, secondAmount);
                    }
                }
            }
        }

        private static void AddPointCut(SourceSegment segment, Vector2 point)
        {
            var direction = segment.End - segment.Start;
            var lengthSquared = direction.sqrMagnitude;
            if (lengthSquared <= GeometryEpsilon * GeometryEpsilon)
            {
                return;
            }

            var amount = Mathf.Clamp01(Vector2.Dot(point - segment.Start, direction) / lengthSquared);
            var projection = segment.Start + direction * amount;
            if (Vector2.Distance(point, projection) <= GeometryEpsilon)
            {
                AddCut(segment.Cuts, amount);
            }
        }

        private static void AddCut(List<float> cuts, float value)
        {
            value = Mathf.Clamp01(value);
            foreach (var existing in cuts)
            {
                if (Mathf.Abs(existing - value) <= GeometryEpsilon * 0.1f)
                {
                    return;
                }
            }

            cuts.Add(value);
        }

        private static List<GraphVertex> BuildGraph(IReadOnlyList<SourceSegment> segments)
        {
            var vertices = new List<GraphVertex>();
            foreach (var segment in segments)
            {
                segment.Cuts.Sort();
                for (var index = 0; index < segment.Cuts.Count - 1; index++)
                {
                    var start = Vector2.Lerp(segment.Start, segment.End, segment.Cuts[index]);
                    var end = Vector2.Lerp(segment.Start, segment.End, segment.Cuts[index + 1]);
                    if (Vector2.Distance(start, end) <= GeometryEpsilon)
                    {
                        continue;
                    }

                    var startIndex = FindOrAddVertex(vertices, start);
                    var endIndex = FindOrAddVertex(vertices, end);
                    AddNeighbour(vertices[startIndex].Neighbours, endIndex);
                    AddNeighbour(vertices[endIndex].Neighbours, startIndex);
                }
            }

            return vertices;
        }

        private static int FindOrAddVertex(List<GraphVertex> vertices, Vector2 position)
        {
            for (var index = 0; index < vertices.Count; index++)
            {
                if (Vector2.Distance(vertices[index].Position, position) <= GeometryEpsilon)
                {
                    return index;
                }
            }

            vertices.Add(new GraphVertex(position));
            return vertices.Count - 1;
        }

        private static void AddNeighbour(List<int> neighbours, int neighbour)
        {
            if (!neighbours.Contains(neighbour))
            {
                neighbours.Add(neighbour);
            }
        }

        private static void SortNeighbours(IReadOnlyList<GraphVertex> vertices)
        {
            foreach (var vertex in vertices)
            {
                vertex.Neighbours.Sort((first, second) =>
                {
                    var firstDirection = vertices[first].Position - vertex.Position;
                    var secondDirection = vertices[second].Position - vertex.Position;
                    return Mathf.Atan2(firstDirection.y, firstDirection.x)
                        .CompareTo(Mathf.Atan2(secondDirection.y, secondDirection.x));
                });
            }
        }

        private static List<DetectedPolygon> TraceBoundedFaces(IReadOnlyList<GraphVertex> vertices)
        {
            var visited = new HashSet<long>();
            var polygons = new List<DetectedPolygon>();
            for (var from = 0; from < vertices.Count; from++)
            {
                foreach (var to in vertices[from].Neighbours)
                {
                    if (visited.Contains(EdgeKey(from, to)))
                    {
                        continue;
                    }

                    var points = TraceFace(vertices, from, to, visited);
                    var area = SignedArea(points);
                    if (points.Count >= 3 && area >= MinimumRoomArea)
                    {
                        polygons.Add(new DetectedPolygon(points));
                    }
                }
            }

            return polygons;
        }

        private static List<Vector2> TraceFace(
            IReadOnlyList<GraphVertex> vertices,
            int startFrom,
            int startTo,
            HashSet<long> visited)
        {
            var points = new List<Vector2>();
            var previous = startFrom;
            var current = startTo;
            points.Add(vertices[previous].Position);
            var safetyLimit = Mathf.Max(8, vertices.Count * 4);
            for (var step = 0; step < safetyLimit; step++)
            {
                visited.Add(EdgeKey(previous, current));
                points.Add(vertices[current].Position);
                var neighbours = vertices[current].Neighbours;
                var reverseIndex = neighbours.IndexOf(previous);
                if (reverseIndex < 0 || neighbours.Count == 0)
                {
                    return new List<Vector2>();
                }

                var next = neighbours[(reverseIndex - 1 + neighbours.Count) % neighbours.Count];
                previous = current;
                current = next;
                if (previous == startFrom && current == startTo)
                {
                    points.RemoveAt(points.Count - 1);
                    return points;
                }
            }

            return new List<Vector2>();
        }

        private static bool TryIntersect(
            Vector2 firstStart,
            Vector2 firstEnd,
            Vector2 secondStart,
            Vector2 secondEnd,
            out float firstAmount,
            out float secondAmount)
        {
            var firstDirection = firstEnd - firstStart;
            var secondDirection = secondEnd - secondStart;
            var denominator = Cross(firstDirection, secondDirection);
            if (Mathf.Abs(denominator) <= GeometryEpsilon * GeometryEpsilon)
            {
                firstAmount = 0f;
                secondAmount = 0f;
                return false;
            }

            var difference = secondStart - firstStart;
            firstAmount = Cross(difference, secondDirection) / denominator;
            secondAmount = Cross(difference, firstDirection) / denominator;
            return firstAmount >= -GeometryEpsilon && firstAmount <= 1f + GeometryEpsilon &&
                   secondAmount >= -GeometryEpsilon && secondAmount <= 1f + GeometryEpsilon;
        }

        private static float SignedArea(IReadOnlyList<Vector2> points)
        {
            if (points == null || points.Count < 3)
            {
                return 0f;
            }

            var doubleArea = 0f;
            for (var index = 0; index < points.Count; index++)
            {
                var next = (index + 1) % points.Count;
                doubleArea += points[index].x * points[next].y - points[next].x * points[index].y;
            }

            return doubleArea * 0.5f;
        }

        private static Vector2 Average(IReadOnlyList<Vector2> points)
        {
            var result = Vector2.zero;
            foreach (var point in points)
            {
                result += point;
            }

            return points.Count == 0 ? result : result / points.Count;
        }

        private static long EdgeKey(int from, int to)
        {
            return ((long)from << 32) | (uint)to;
        }

        private static float Cross(Vector2 first, Vector2 second)
        {
            return first.x * second.y - first.y * second.x;
        }
    }
}
