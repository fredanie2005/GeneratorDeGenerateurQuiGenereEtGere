# 🌍 Generateur Procedural
## Sommaire

### Comment utiliser
- [Creation d'un Algorithme](#creation)
- [Utilisation du moteur](#utilisation)
- [Utilisation de l'aléatoire et des seeds](#aléatoire)
### Différent Algorithme
- [Simple Room Placement](#SimpleRoomPlacement)
- [BSP Room Placement](#BSPRoomPlacement)
- [Cellular Automata](#CellularAutomata)
- [NoiseGeneration](#NoiseGeneration)

## Creation

Crée une nouvelle classe enfant de **ProceduralGenerationMethod** 

Script de base pour l'algorithme:
```C#
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
    [CreateAssetMenu(menuName = "Procedural Generation Method/NewAlgorithm")] // A renommé par le nom de l'algorithme
    public class NewAlgorithm : ProceduralGenerationMethod // A renommé par le nom de l'algorithme
    {
        [Header("Algorithm Parameters")]
        // Champ ici

        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            // Valeur ici

            for (int i = 0; i < _maxSteps; i++)
            {
                // Algorithme ici
            }

        }
        // Autre fonction ici
    }
}
```

Crée le **Scriptable Object** de votre nouvelle algorithme
<img width="800" height="671" alt="Capture d&#39;écran 2025-11-14 162133" src="https://github.com/user-attachments/assets/aae710dc-a87e-48ff-847f-65353f82798e" />

## Utilisation
Insérer le **Scriptable Object** dans le Game Object **ProceduralGridGenerator**

<img width="449" height="465" alt="Capture d&#39;écran 2025-11-14 163024" src="https://github.com/user-attachments/assets/4bf3031a-7827-4de4-9129-0a39189402a3" />

Il est possible d'éditer les valeurs sérialisé dans le **ScriptableObject**

<img width="447" height="836" alt="Capture d&#39;écran 2025-11-14 163119" src="https://github.com/user-attachments/assets/27895122-5c61-47ec-bb05-c583833ca660" />

Pour lancer la génération, il suffis de lancer le jeu et d'appuyer sur le bouton [Generate Grid] du Game Object **ProceduralGridGenerator**

<img width="453" height="367" alt="Capture d&#39;écran 2025-11-14 164951" src="https://github.com/user-attachments/assets/55234cfc-1273-42ac-be34-59b5d24b551d" />

Une fois lancer l'algorithme va etre jouer X fois selon la valeur *maxSteps* assigner, *stepDelay* permet de rajouter du temps entre les cycles (utile pour voir l'evolution à chaque boucle)


## Aléatoire
**ProceduralGridGenerator** possède RandomService
```C#
[NonSerialized] protected RandomService RandomService;
```
RandomService permet de générer des nombres aléatoire via une seed donnée
```C#
/// Returns a random integer between min [inclusive] and max [exclusive]
public int Range(int minInclusive, int maxExclusive)
{
    if (maxExclusive <= minInclusive)
    {
        Debug.LogError("[RandomService] Range(maxExclusive <= minInclusive).");
        return minInclusive;
    }

    return Random.Next(minInclusive, maxExclusive);
}

/// Returns a random float between min [inclusive] and max [inclusive]
public float Range(float minInclusive, float maxInclusive)
{
    if (maxInclusive < minInclusive)
    {
        Debug.LogError("[RandomService] Range(maxInclusive < minInclusive).");
        return minInclusive;
    }

    double t = Random.NextDouble();  // returns [0.0, 1.0)
    return (float)(minInclusive + (t * (maxInclusive - minInclusive)));
}

/// Returns true with a given probability (0–1)
public bool Chance(float probability)
{
    if (probability <= 0f) return false;
    if (probability >= 1f) return true;

    return Random.NextDouble() < probability;
}

/// Returns a random element from a list or array (safe against empty collections)
public T Pick<T>(T[] array)
{
    if (array == null || array.Length == 0)
    {
        Debug.LogError("[RandomService] Pick called on empty array.");
        return default;
    }

    int index = Random.Next(0, array.Length);
    return array[index];
}
```

La seed est éditable via le Game Object **ProceduralGridGenerator**

<img width="449" height="462" alt="Capture d&#39;écran 2025-11-14 164912" src="https://github.com/user-attachments/assets/6691f031-52f2-4194-a3b8-ec7eff9c9568" />

# Différent Algorithme

## SimpleRoomPlacement
Génère des salles de tailles aléatoires et essaie de les placer à un emplacement libre. Chaque salle teste plusieurs positions au hasard jusqu’à en trouver une qui ne chevauche rien.

```C#
protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
{
    // ROOM CREATIONS
    List<RectInt> placedRooms = new();
    int roomsPlacedCount = 0;
    int attempts = 0;

    for (int i = 0; i < _maxSteps; i++)
    {
        // Check for cancellation
        cancellationToken.ThrowIfCancellationRequested();

        if (roomsPlacedCount >= _maxRooms)
        {
            break;
        }

        attempts++;

        // choose a random size
        int width = RandomService.Range(_roomMinSize.x, _roomMaxSize.x + 1);
        int lenght = RandomService.Range(_roomMinSize.y, _roomMaxSize.y + 1);

        // choose random position so entire room fits into grid
        int x = RandomService.Range(0, Grid.Width - width);
        int y = RandomService.Range(0, Grid.Lenght - lenght);

        RectInt newRoom = new RectInt(x, y, width, lenght);

        if (!CanPlaceRoom(newRoom, 1))
            continue;

        PlaceRoom(newRoom);
        placedRooms.Add(newRoom);

        roomsPlacedCount++;

        await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
    }

    if (roomsPlacedCount < _maxRooms)
    {
        Debug.LogWarning($"RoomPlacer Only placed {roomsPlacedCount}/{_maxRooms} rooms after {attempts} attempts.");
    }

    if (placedRooms.Count < 2)
    {
        Debug.Log("Not enough rooms to connect.");
        return;
    }

    // CORRIDOR CREATIONS
    for (int i = 0; i < placedRooms.Count - 1; i++)
    {
        // Check for cancellation
        cancellationToken.ThrowIfCancellationRequested();

        Vector2Int start = placedRooms[i].GetCenter();
        Vector2Int end = placedRooms[i + 1].GetCenter();

        CreateDogLegCorridor(start, end);

        await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
    }

    BuildGround();
}
```

<img width="488" height="486" alt="Capture d&#39;écran 2025-11-14 172248" src="https://github.com/user-attachments/assets/04cd316c-b386-49d2-b942-dfadfbc13b1d" />

## BSPRoomPlacement
Le Binary Space Partitioning coupe la map en 2, puis chaque partie est recoupée en 2, et ainsi de suite. Chaque zone obtenue sert ensuite à générer une salle, ce qui garantit qu’elles ne se superposent jamais, et permet de facilement trouver les rooms voisines.

```C#
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
```

<img width="485" height="487" alt="Capture d&#39;écran 2025-11-14 173204" src="https://github.com/user-attachments/assets/f3f40b41-c507-4ae0-94ba-e2c019cb4236" />

## CellularAutomata
Chaque case est d’abord définie comme terre ou eau avec 50% de chance.
Puis on applique plusieurs fois une règle, si une case a 4 voisins ou plus qui sont terre, elle devient terre, sinon elle devient eau.

```C#
protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
{
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
```

<img width="488" height="490" alt="Capture d&#39;écran 2025-11-14 172731" src="https://github.com/user-attachments/assets/8cdf4d81-7a20-4350-a085-c93d7b0fc202" />

## NoiseGeneration
Chaque case est générée à partir d’un bruit procédural (FastNoiseLite) https://github.com/shniqq/FastNoiseLite-Unity.

```C#
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
}
```
<img width="484" height="489" alt="Capture d&#39;écran 2025-11-14 172818" src="https://github.com/user-attachments/assets/baf518ed-42cc-44f4-aec4-b9e1c3cab9e5" />

