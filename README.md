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
Simple room placement génère des salles de taille au hazard, et essaie de trouver une place libre pour poser la salle parmis une boucle de recherche de position au hazard
## BSPRoomPlacement
Binary Space Partitioning, sépare la map en 
## CellularAutomata
## NoiseGeneration

