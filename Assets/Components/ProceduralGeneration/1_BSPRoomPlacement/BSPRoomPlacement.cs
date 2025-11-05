using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VTools.Grid;
using VTools.ScriptableObjectDatabase;
using VTools.Utility;

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
            RoomNode roomNode = new RoomNode(RandomService);

            CreatePartition(roomNode, 4);

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

        /// Creates a horizontal corridor from x1 to x2 at the given y coordinate
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

        /// Creates a vertical corridor from y1 to y2 at the given x coordinate
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

        private void CreatePartition(RoomNode node, int splitNumber)
        {
            if (splitNumber == 0)
            {
                RectInt nodeRect = node.size;
                int marginX = RandomService.Range(2, Mathf.Max(2, nodeRect.width / 4));
                int marginY = RandomService.Range(2, Mathf.Max(2, nodeRect.height / 4));
                int newWidth = Mathf.Max(4, nodeRect.width - marginX);
                int newHeight = Mathf.Max(4, nodeRect.height - marginY);
                int newX = nodeRect.x + RandomService.Range(0, nodeRect.width - newWidth);
                int newY = nodeRect.y + RandomService.Range(0, nodeRect.height - newHeight);

                RectInt room = new RectInt(newX, newY, newWidth, newHeight);
                PlaceRoom(room, ROOM_TILE_NAME);
                return;
            }

            node.Split();

            CreatePartition(node.FirstChild, splitNumber - 1);
            CreatePartition(node.SecondChild, splitNumber - 1);

            Vector2 centerA = node.FirstChild.size.center;
            Vector2 centerB = node.SecondChild.size.center;

            Vector2Int intA = new Vector2Int(Mathf.RoundToInt(centerA.x), Mathf.RoundToInt(centerA.y));
            Vector2Int intB = new Vector2Int(Mathf.RoundToInt(centerB.x), Mathf.RoundToInt(centerB.y));

            CreateDogLegCorridor(intA, intB);

        }


    }
}