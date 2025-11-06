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
    [CreateAssetMenu(menuName = "Procedural Generation Method/Cellular Automata")]
    public class CellularAutomata : ProceduralGenerationMethod
    {
        [Header("Automata Settings")]
        [SerializeField, Range(0f, 1f)] private float _noiseDensity = 0.5f;
        [SerializeField, Range(1, 10)] private int _maxSteps = 5;
        [SerializeField] private int _width = 64;
        [SerializeField] private int _height = 64;

        private Dictionary<Vector2Int, string> _gridState;

        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            // 🔧 Initialisation
            _gridState = new Dictionary<Vector2Int, string>();

            CreateNoiseGrid();

            for (int i = 0; i < _maxSteps; i++)
            {
                ApplyAutomataStep();
            }

            ReplaceTiles();
        }

        // -------------------------------------- INIT NOISE ---------------------------------------------
        private void CreateNoiseGrid()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    bool isLand = RandomService.Chance(_noiseDensity);
                    _gridState[new Vector2Int(x, y)] = isLand ? GRASS_TILE_NAME : WATER_TILE_NAME;
                }
            }
        }

        // -------------------------------------- STEP ---------------------------------------------
        private void ApplyAutomataStep()
        {
            var nextGrid = new Dictionary<Vector2Int, string>();

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    int grassNeighbors = CountNeighbors(x, y, GRASS_TILE_NAME);

                    string current = _gridState[new Vector2Int(x, y)];

                    // Règle type "Cave generation"
                    if (grassNeighbors > 4)
                        nextGrid[new Vector2Int(x, y)] = GRASS_TILE_NAME;
                    else if (grassNeighbors < 4)
                        nextGrid[new Vector2Int(x, y)] = WATER_TILE_NAME;
                    else
                        nextGrid[new Vector2Int(x, y)] = current;
                }
            }

            _gridState = nextGrid;
        }

        // -------------------------------------- COUNT NEIGHBORS ---------------------------------------------
        private int CountNeighbors(int cx, int cy, string type)
        {
            int count = 0;

            for (int nx = -1; nx <= 1; nx++)
            {
                for (int ny = -1; ny <= 1; ny++)
                {
                    if (nx == 0 && ny == 0) continue;

                    int x = cx + nx;
                    int y = cy + ny;

                    // Si hors limite → on compte comme "eau"
                    if (x < 0 || y < 0 || x >= _width || y >= _height)
                        continue;

                    if (_gridState.TryGetValue(new Vector2Int(x, y), out var tileType))
                    {
                        if (tileType == type)
                            count++;
                    }
                }
            }

            return count;
        }

        // -------------------------------------- REPLACE ---------------------------------------------
        private void ReplaceTiles()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                        continue;

                    string tile = _gridState[new Vector2Int(x, y)];
                    AddTileToCell(cell, tile, true);
                }
            }
        }
    }
}
