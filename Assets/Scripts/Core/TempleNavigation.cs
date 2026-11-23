using System.Collections.Generic;
using UnityEngine;

namespace TempleOfBoredTitans.Core
{
    /// <summary>Builds a small walkability grid and paths Titans through the temple maze.</summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class TempleNavigation : MonoBehaviour
    {
        private const float MinCoordinate = -27f;
        private const float CellSize = 0.5f;
        private const int Side = 125;
        private static readonly Collider[] OverlapBuffer = new Collider[32];
        private static bool[,] walkable;
        private static Vector3[,] points;
        private static GameObject[] obstacleGroups;

        public static bool IsReady { get; private set; }

        private void Awake()
        {
            IsReady = false;
            GameObject storyLayout = GameObject.Find("Storyboard Layout");
            GameObject floorGroup = storyLayout != null ? storyLayout : GameObject.Find("Expanded Temple Floors");
            GameObject wallGroup = storyLayout != null ? storyLayout : GameObject.Find("Expanded Temple Walls");
            GameObject props = GameObject.Find("Temple Props");
            obstacleGroups = storyLayout != null
                ? new[] { storyLayout, GameObject.Find("Final Chamber Gate"), GameObject.Find("Temple Lute Interaction") }
                : new[] { wallGroup, props };
            if (floorGroup == null || wallGroup == null)
            {
                Debug.LogWarning("Titan navigation could not find the temple floor and wall groups.", this);
                return;
            }

            Collider[] allColliders = floorGroup.GetComponentsInChildren<Collider>(false);
            List<Collider> floorList = new List<Collider>();
            for (int i = 0; i < allColliders.Length; i++)
            {
                if (storyLayout == null || IsStoryboardFloor(allColliders[i].gameObject.name)) floorList.Add(allColliders[i]);
            }
            Collider[] floors = floorList.ToArray();
            if (floors.Length == 0)
            {
                Debug.LogWarning("Titan navigation found no floor colliders.", this);
                return;
            }

            walkable = new bool[Side, Side];
            points = new Vector3[Side, Side];
            for (int x = 0; x < Side; x++)
            for (int z = 0; z < Side; z++)
            {
                float worldX = MinCoordinate + x * CellSize;
                float worldZ = MinCoordinate + z * CellSize;
                bool hasFloor = false;
                float floorY = 0f;
                Vector3 sample = new Vector3(worldX, -0.01f, worldZ);
                for (int i = 0; i < floors.Length; i++)
                {
                    if (!floors[i].enabled || !floors[i].bounds.Contains(sample)) continue;
                    hasFloor = true;
                    floorY = floors[i].bounds.max.y;
                    break;
                }
                if (!hasFloor) continue;

                Vector3 bottom = new Vector3(worldX, floorY + 0.22f, worldZ);
                Vector3 top = new Vector3(worldX, floorY + 1.45f, worldZ);
                int hitCount = Physics.OverlapCapsuleNonAlloc(bottom, top, 0.2f, OverlapBuffer,
                    ~0, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                for (int i = 0; i < hitCount; i++)
                {
                    Transform hit = OverlapBuffer[i].transform;
                    for (int group = 0; group < obstacleGroups.Length; group++)
                    {
                        if (obstacleGroups[group] != null && hit.IsChildOf(obstacleGroups[group].transform))
                        {
                            blocked = true;
                            break;
                        }
                    }
                    if (blocked) break;
                }
                if (!blocked)
                {
                    walkable[x, z] = true;
                    points[x, z] = new Vector3(worldX, floorY, worldZ);
                }
            }

            IsReady = true;
            Debug.Log("Titan path grid built for the branching burial temple.", this);
        }

        private static bool IsStoryboardFloor(string objectName)
        {
            return objectName == "Entry Vestibule Floor" || objectName == "Burial Nave Floor" ||
                   objectName == "West Chapel Floor" || objectName == "East Chapel Floor" ||
                   objectName == "Ring Crypt Floor" || objectName == "West Reliquary Floor" ||
                   objectName == "East Reliquary Floor" || objectName == "Processional Turn Floor" ||
                   objectName == "Burial Sanctum Floor" || objectName == "Escape Landing Floor";
        }

        private void OnDestroy()
        {
            IsReady = false;
            walkable = null;
            points = null;
        }

        public static bool FindPath(Vector3 start, Vector3 destination, List<Vector3> result)
        {
            if (!IsReady || walkable == null || result == null) return false;
            result.Clear();
            if (!TryFindWalkableCell(start, out int startIndex) || !TryFindWalkableCell(destination, out int goalIndex)) return false;
            if (startIndex == goalIndex)
            {
                result.Add(points[goalIndex / Side, goalIndex % Side]);
                return true;
            }

            int total = Side * Side;
            int[] parent = new int[total];
            int[] cost = new int[total];
            bool[] closed = new bool[total];
            for (int i = 0; i < total; i++) { parent[i] = -1; cost[i] = int.MaxValue; }
            List<OpenNode> heap = new List<OpenNode>(256);
            cost[startIndex] = 0;
            Push(heap, new OpenNode(startIndex, Heuristic(startIndex, goalIndex), 0));

            int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
            int[] dz = { 0, 0, 1, -1, 1, -1, 1, -1 };
            bool found = false;
            while (heap.Count > 0)
            {
                OpenNode current = Pop(heap);
                int currentCost = cost[current.index];
                if (closed[current.index] || current.g != currentCost) continue;
                if (current.index == goalIndex) { found = true; break; }
                closed[current.index] = true;
                int cx = current.index / Side;
                int cz = current.index % Side;
                for (int d = 0; d < 8; d++)
                {
                    int nx = cx + dx[d];
                    int nz = cz + dz[d];
                    if (nx < 0 || nx >= Side || nz < 0 || nz >= Side || !walkable[nx, nz]) continue;
                    if (d >= 4 && (!walkable[cx + dx[d], cz] || !walkable[cx, cz + dz[d]])) continue;
                    int next = nx * Side + nz;
                    int nextCost = currentCost + (d < 4 ? 10 : 14);
                    if (nextCost >= cost[next]) continue;
                    parent[next] = current.index;
                    cost[next] = nextCost;
                    Push(heap, new OpenNode(next, nextCost + Heuristic(next, goalIndex), nextCost));
                }
            }
            if (!found) return false;

            List<int> reverse = new List<int>();
            int step = goalIndex;
            while (step != startIndex && step >= 0)
            {
                reverse.Add(step);
                step = parent[step];
            }
            for (int i = reverse.Count - 1; i >= 0; i--)
                result.Add(points[reverse[i] / Side, reverse[i] % Side]);
            return result.Count > 0;
        }

        private static bool TryFindWalkableCell(Vector3 position, out int index)
        {
            int sx = Mathf.Clamp(Mathf.RoundToInt((position.x - MinCoordinate) / CellSize), 0, Side - 1);
            int sz = Mathf.Clamp(Mathf.RoundToInt((position.z - MinCoordinate) / CellSize), 0, Side - 1);
            for (int radius = 0; radius < 12; radius++)
            {
                float best = float.MaxValue;
                int bestIndex = -1;
                int minX = Mathf.Max(0, sx - radius), maxX = Mathf.Min(Side - 1, sx + radius);
                int minZ = Mathf.Max(0, sz - radius), maxZ = Mathf.Min(Side - 1, sz + radius);
                for (int x = minX; x <= maxX; x++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (!walkable[x, z]) continue;
                    float dx = x - sx, dz = z - sz;
                    float d = dx * dx + dz * dz;
                    if (d < best) { best = d; bestIndex = x * Side + z; }
                }
                if (bestIndex >= 0) { index = bestIndex; return true; }
            }
            index = -1;
            return false;
        }

        private static int Heuristic(int a, int b)
        {
            int dx = Mathf.Abs(a / Side - b / Side);
            int dz = Mathf.Abs(a % Side - b % Side);
            int diagonal = Mathf.Min(dx, dz);
            return diagonal * 14 + (Mathf.Max(dx, dz) - diagonal) * 10;
        }

        private struct OpenNode
        {
            public int index;
            public int f;
            public int g;
            public OpenNode(int index, int f, int g) { this.index = index; this.f = f; this.g = g; }
        }

        private static void Push(List<OpenNode> heap, OpenNode node)
        {
            heap.Add(node);
            int i = heap.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (heap[parent].f <= node.f) break;
                heap[i] = heap[parent];
                i = parent;
            }
            heap[i] = node;
        }

        private static OpenNode Pop(List<OpenNode> heap)
        {
            OpenNode first = heap[0];
            OpenNode last = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            if (heap.Count == 0) return first;
            int i = 0;
            while (true)
            {
                int left = i * 2 + 1;
                if (left >= heap.Count) break;
                int right = left + 1;
                int child = right < heap.Count && heap[right].f < heap[left].f ? right : left;
                if (heap[child].f >= last.f) break;
                heap[i] = heap[child];
                i = child;
            }
            heap[i] = last;
            return first;
        }
    }
}

