using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Components.ProceduralGeneration.SimpleRoomPlacement
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/Noise Generator")]
    public class NoiseGenerator : ProceduralGenerationMethod
    {
        [Header("Map Settings")]
        [SerializeField, Range(16, 1024)] private int _width = 128;
        [SerializeField, Range(16, 1024)] private int _height = 128;
        [SerializeField, Range(4, 128)] private int _chunkSize = 32;

        [Header("Noise Settings")]
        [SerializeField] private FastNoiseLite.NoiseType _noiseType = FastNoiseLite.NoiseType.OpenSimplex2;
        [SerializeField] private FastNoiseLite.FractalType _fractalType = FastNoiseLite.FractalType.FBm;
        [SerializeField] private FastNoiseLite.RotationType3D _rotationType3D = FastNoiseLite.RotationType3D.None;
        [SerializeField] private FastNoiseLite.DomainWarpType _domainWarpType = FastNoiseLite.DomainWarpType.None;
        [SerializeField] private FastNoiseLite.CellularDistanceFunction _cellularDistanceFunction = FastNoiseLite.CellularDistanceFunction.Euclidean;
        [SerializeField] private FastNoiseLite.CellularReturnType _cellularReturnType = FastNoiseLite.CellularReturnType.Distance;

        [Header("Noise Numeric Parameters")]
        [SerializeField] private int _seed = 1337;
        [SerializeField, Range(0.001f, 0.2f)] private float _frequency = 0.05f;
        [SerializeField, Range(1, 8)] private int _octaves = 4;
        [SerializeField, Range(0.1f, 4f)] private float _lacunarity = 2f;
        [SerializeField, Range(0.0f, 1.0f)] private float _gain = 0.5f;
        [SerializeField, Range(0.0f, 1.0f)] private float _weightedStrength = 0.0f;
        [SerializeField, Range(0.5f, 4.0f)] private float _pingPongStrength = 2f;
        [SerializeField, Range(0.0f, 3.0f)] private float _domainWarpAmp = 1f;
        [SerializeField, Range(0.0f, 2.0f)] private float _cellularJitter = 1f;

        [Header("Biome Thresholds")]
        [SerializeField, Range(0f, 1f)] private float _waterLevel = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _sandLevel = 0.4f;
        [SerializeField, Range(0f, 1f)] private float _grassLevel = 0.7f;
        [SerializeField, Range(0f, 1f)] private float _rockLevel = 0.85f;

        [Header("Height Map Coloring")]
        [SerializeField] private Gradient _heightGradient;
        [SerializeField] private float _heightMultiplier = 5f; // ajustable
        [SerializeField] private AnimationCurve _heightCurve = AnimationCurve.Linear(0, 0, 1, 1);


        private FastNoiseLite _fnl;
        private CancellationTokenSource _cts;
        private bool _isUpdating;

        // --- Génération principale (appelée par ApplyGeneration) ---
        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            _fnl = new FastNoiseLite();
            InitFNL();

            int chunksX = Mathf.CeilToInt((float)_width / _chunkSize);
            int chunksY = Mathf.CeilToInt((float)_height / _chunkSize);

            var tasks = new UniTask<float[,]>[chunksX * chunksY];
            int t = 0;

            for (int cx = 0; cx < chunksX; cx++)
            {
                for (int cy = 0; cy < chunksY; cy++)
                {
                    int startX = cx * _chunkSize;
                    int startY = cy * _chunkSize;
                    int sizeX = Mathf.Min(_chunkSize, _width - startX);
                    int sizeY = Mathf.Min(_chunkSize, _height - startY);

                    tasks[t++] = GenerateChunkAsync(startX, startY, sizeX, sizeY, cancellationToken);
                }
            }

            var results = await UniTask.WhenAll(tasks);

            float[,] fullNoise = new float[_width, _height];
            int index = 0;
            for (int cx = 0; cx < chunksX; cx++)
            {
                for (int cy = 0; cy < chunksY; cy++)
                {
                    float[,] chunk = results[index++];
                    int startX = cx * _chunkSize;
                    int startY = cy * _chunkSize;

                    for (int x = 0; x < chunk.GetLength(0); x++)
                        for (int y = 0; y < chunk.GetLength(1); y++)
                            fullNoise[startX + x, startY + y] = chunk[x, y];
                }
            }

            ApplyTilesFromNoise(fullNoise);
            //CreateHeightMapMesh(fullNoise);
        }

        private async UniTask<float[,]> GenerateChunkAsync(int startX, int startY, int sizeX, int sizeY, CancellationToken token)
        {
            return await UniTask.RunOnThreadPool(() =>
            {
                float[,] chunkNoise = new float[sizeX, sizeY];
                for (int x = 0; x < sizeX; x++)
                {
                    for (int y = 0; y < sizeY; y++)
                    {
                        float nx = (float)(startX + x) / _width;
                        float ny = (float)(startY + y) / _height;
                        chunkNoise[x, y] = _fnl.GetNoise(nx * _width, ny * _height);
                    }
                }
                return chunkNoise;
            }, cancellationToken: token);
        }

        private void ApplyTilesFromNoise(float[,] noiseMap)
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                        continue;

                    string tileName = GetTileNameFromNoise(noiseMap[x, y]);
                    AddTileToCell(cell, tileName, true);
                }
            }
        }
        private void CreateHeightMapMesh(float[,] fullNoise)
        {
            Color[] colors;
            Mesh mesh = HeightMapMeshGenerator.GenerateMesh(
                fullNoise,
                Grid.CellSize,
                _heightMultiplier,
                _heightCurve,
                out colors,
                _heightGradient
            );

            GameObject terrainGO = new GameObject("ProceduralTerrain");
            MeshFilter mf = terrainGO.AddComponent<MeshFilter>();
            MeshRenderer mr = terrainGO.AddComponent<MeshRenderer>();
            mf.mesh = mesh;

            // Shader supportant vertex color
            mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        }


        private void InitFNL()
        {
            _fnl.SetSeed(_seed);
            _fnl.SetFrequency(_frequency);
            _fnl.SetNoiseType(_noiseType);
            _fnl.SetRotationType3D(_rotationType3D);
            _fnl.SetFractalType(_fractalType);
            _fnl.SetFractalOctaves(_octaves);
            _fnl.SetFractalLacunarity(_lacunarity);
            _fnl.SetFractalGain(_gain);
            _fnl.SetFractalWeightedStrength(_weightedStrength);
            _fnl.SetFractalPingPongStrength(_pingPongStrength);
            _fnl.SetCellularDistanceFunction(_cellularDistanceFunction);
            _fnl.SetCellularReturnType(_cellularReturnType);
            _fnl.SetCellularJitter(_cellularJitter);
            _fnl.SetDomainWarpType(_domainWarpType);
            _fnl.SetDomainWarpAmp(_domainWarpAmp);
        }

        private string GetTileNameFromNoise(float noise)
        {
            float height = (noise + 1f) * 0.5f;

            if (height < _waterLevel)
                return WATER_TILE_NAME;
            else if (height < _sandLevel)
                return SAND_TILE_NAME;
            else if (height < _grassLevel)
                return GRASS_TILE_NAME;
            else if (height < _rockLevel)
                return ROCK_TILE_NAME;
            else
                return ROOM_TILE_NAME;
        }

#if UNITY_EDITOR
        // --- Auto Update en direct ---
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                if (_isUpdating)
                    return;

                _isUpdating = true;
                _cts?.Cancel();
                _cts = new CancellationTokenSource();

                // On redémarre une génération asynchrone
                UniTask.Void(async () =>
                {
                    await UniTask.DelayFrame(1);
                    await ApplyGeneration(_cts.Token);
                    _isUpdating = false;
                });
            }
        }
#endif
    }
}
