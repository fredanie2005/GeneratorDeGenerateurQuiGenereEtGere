using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VTools.Grid;
using VTools.ScriptableObjectDatabase;
using VTools.Utility;
using VTools.RandomService;

namespace Components.ProceduralGeneration.SimpleRoomPlacement
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/BSP Room Placement")]
    public class BSPRoomPlacement : ProceduralGenerationMethod
    {
        [Header("Room Parameters")]
        [SerializeField] private int _maxRooms = 10;
        [SerializeField] private Vector2Int _roomMinSize = new(5, 5);
        [SerializeField] private Vector2Int _roomMaxSize = new(12, 8);

        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            // Root node covers the whole grid
            var rootRect = new RectInt(0, 0, Grid.Width, Grid.Lenght);
            RoomNode root = new RoomNode(RandomService, rootRect);

            // Calculate splits from desired max rooms (log2), at least 0
            int splits = 0;
            if (_maxRooms > 1)
                splits = Mathf.Max(0, Mathf.CeilToInt(Mathf.Log(_maxRooms, 2f)));

            CreatePartition(root, splits);

            BuildGround();
        }

        // -------------------------------------- ROOM ---------------------------------------------

        /// Marks the grid cells of the room as occupied
        private void PlaceRoom(RectInt room, string roomTexture)
        {
            for (int ix = room.xMin; ix < room.xMax; ix++)
            {
                for (int iy = room.yMin; iy < room.yMax; iy++)
                {
                    if (!Grid.TryGetCellByCoordinates(ix, iy, out var cell))
                        continue;

                    AddTileToCell(cell, roomTexture, true);
                }
            }
        }

        // -------------------------------------- CORRIDOR --------------------------------------------- 

        /// Creates an L-shaped corridor between two points, randomly choosing horizontal-first or vertical-first
        private void CreateDogLegCorridor(Vector2Int start, Vector2Int end)
        {
            bool horizontalFirst = RandomService.Chance(0.5f);

            if (horizontalFirst)
            {
                // Draw horizontal line first, then vertical
                CreateHorizontalCorridor(start.x, end.x, start.y);
                CreateVerticalCorridor(start.y, end.y, end.x);
            }
            else
            {
                // Draw vertical line first, then horizontal
                CreateVerticalCorridor(start.y, end.y, start.x);
                CreateHorizontalCorridor(start.x, end.x, end.y);
            }
        }

        private void CreateHorizontalCorridor(int x1, int x2, int y)
        {
            int xMin = Mathf.Min(x1, x2);
            int xMax = Mathf.Max(x1, x2);

            for (int x = xMin; x <= xMax; x++)
            {
                if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                    continue;

                AddTileToCell(cell, CORRIDOR_TILE_NAME, true);
            }
        }

        private void CreateVerticalCorridor(int y1, int y2, int x)
        {
            int yMin = Mathf.Min(y1, y2);
            int yMax = Mathf.Max(y1, y2);

            for (int y = yMin; y <= yMax; y++)
            {
                if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                    continue;

                AddTileToCell(cell, CORRIDOR_TILE_NAME, true);
            }
        }

        // -------------------------------------- GROUND --------------------------------------------- 

        private void BuildGround()
        {
            var groundTemplate = ScriptableObjectDatabase.GetScriptableObject<GridObjectTemplate>("Grass");

            // Instantiate ground blocks
            for (int x = 0; x < Grid.Width; x++)
            {
                for (int z = 0; z < Grid.Lenght; z++)
                {
                    if (!Grid.TryGetCellByCoordinates(x, z, out var chosenCell))
                    {
                        Debug.LogError($"Unable to get cell on coordinates : ({x}, {z})");
                        continue;
                    }

                    GridGenerator.AddGridObjectToCell(chosenCell, groundTemplate, false);
                }
            }
        }

        /// <summary>
        /// Partition the node recursively.
        /// Returns the center (int) of the room created inside this subtree so parent can connect corridors.
        /// </summary>
        private Vector2Int CreatePartition(RoomNode node, int remainingSplits)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            // If we reached split depth or node can't split, create a room inside this partition
            if (remainingSplits <= 0 || !node.CanSplit())
            {
                RectInt nodeRect = node.size;

                // compute workable room size clamped to node and min/max sizes
                int maxW = Mathf.Min(_roomMaxSize.x, nodeRect.width - 2);
                int maxH = Mathf.Min(_roomMaxSize.y, nodeRect.height - 2);
                int minW = Mathf.Min(_roomMinSize.x, Mathf.Max(1, nodeRect.width - 2));
                int minH = Mathf.Min(_roomMinSize.y, Mathf.Max(1, nodeRect.height - 2));

                // If node too small to respect min, fallback to fill
                if (maxW < 1) maxW = Mathf.Max(1, nodeRect.width - 2);
                if (maxH < 1) maxH = Mathf.Max(1, nodeRect.height - 2);

                int roomW = RandomService.Range(minW, Mathf.Max(minW, maxW + 1));
                int roomH = RandomService.Range(minH, Mathf.Max(minH, maxH + 1));

                int roomX = nodeRect.x + RandomService.Range(1, Mathf.Max(1, nodeRect.width - roomW));
                int roomY = nodeRect.y + RandomService.Range(1, Mathf.Max(1, nodeRect.height - roomH));

                RectInt room = new RectInt(roomX, roomY, roomW, roomH);
                PlaceRoom(room, ROOM_TILE_NAME);

                Vector2 center = room.center;
                return new Vector2Int(Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y));
            }

            // Try to split
            bool splitDidCreateChildren = node.Split();

            // if split failed, treat as leaf
            if (!splitDidCreateChildren || node.FirstChild == null || node.SecondChild == null)
            {
                RectInt fallback = node.size;
                RectInt room = fallback;
                PlaceRoom(room, ROOM_TILE_NAME);
                Vector2 center = room.center;
                return new Vector2Int(Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y));
            }

            // Recurse
            Vector2Int centerA = CreatePartition(node.FirstChild, remainingSplits - 1);
            Vector2Int centerB = CreatePartition(node.SecondChild, remainingSplits - 1);

            // Connect the two subtrees with a corridor
            CreateDogLegCorridor(centerA, centerB);

            // return midpoint as representative center
            return new Vector2Int(Mathf.RoundToInt((centerA.x + centerB.x) * 0.5f), Mathf.RoundToInt((centerA.y + centerB.y) * 0.5f));
        }
    }
}
