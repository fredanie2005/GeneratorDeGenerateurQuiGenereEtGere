using UnityEngine;

public static class HeightMapMeshGenerator
{
    public static Mesh GenerateMesh(float[,] heightMap, float cellSize, float heightMultiplier, AnimationCurve heightCurve, out Color[] colors, Gradient gradient)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Vector3[] vertices = new Vector3[width * height];
        Vector2[] uvs = new Vector2[width * height];
        int[] triangles = new int[(width - 1) * (height - 1) * 6];
        colors = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = x + y * width;
                float noiseValue = (heightMap[x, y] + 1f) * 0.5f; // normalize -1..1 -> 0..1
                float h = heightCurve.Evaluate(noiseValue) * heightMultiplier;
                vertices[i] = new Vector3(x * cellSize, h, y * cellSize);
                uvs[i] = new Vector2((float)x / (width - 1), (float)y / (height - 1));
                colors[i] = gradient.Evaluate(noiseValue);
            }
        }

        int t = 0;
        for (int y = 0; y < height - 1; y++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                int i = x + y * width;

                // Triangle 1
                triangles[t++] = i;
                triangles[t++] = i + width;
                triangles[t++] = i + width + 1;

                // Triangle 2
                triangles[t++] = i;
                triangles[t++] = i + width + 1;
                triangles[t++] = i + 1;
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.RecalculateNormals();

        return mesh;
    }
}
