using UnityEngine;

public class ProceduralCaveGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    public int width = 100;
    public int height = 50;
    public int depth = 100;
    public float noiseScale = 0.1f; // Масштаб шума
    public float surfaceLevel = 0.5f; // Уровень для Marching Cubes
    public Vector3 offset;

    [Header("Low Poly Floor")]
    public float floorNoiseStrength = 10f; // Высота неровностей пола
    public Material floorMaterial;

    [Header("Wall and Ceiling Settings")]
    public Material wallMaterial;
    public float ceilingHeightOffset = 30f; // Смещение высоты потолка

    private float[,,] noiseMap;

    void Start()
    {
        GenerateNoiseMap();
        GenerateLowPolyFloor();
        GenerateCaveMesh();
    }

    void GenerateNoiseMap()
    {
        noiseMap = new float[width, height, depth];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    float xCoord = (x + offset.x) * noiseScale;
                    float yCoord = (y + offset.y) * noiseScale;
                    float zCoord = (z + offset.z) * noiseScale;

                    noiseMap[x, y, z] = Mathf.PerlinNoise(xCoord, zCoord) * Mathf.PerlinNoise(yCoord, xCoord);
                }
            }
        }
    }

    void GenerateLowPolyFloor()
    {
        // Создаем объект пола
        GameObject floorObject = new GameObject("LowPolyFloor");
        MeshFilter floorFilter = floorObject.AddComponent<MeshFilter>();
        MeshRenderer floorRenderer = floorObject.AddComponent<MeshRenderer>();
        floorRenderer.material = floorMaterial;

        // Генерация сетки пола
        Mesh floorMesh = GenerateLowPolyMesh(floorNoiseStrength);
        floorFilter.mesh = floorMesh;
    }

    void GenerateCaveMesh()
    {
        // Стены и потолок
        GameObject caveObject = new GameObject("Cave");
        MeshFilter caveFilter = caveObject.AddComponent<MeshFilter>();
        MeshRenderer caveRenderer = caveObject.AddComponent<MeshRenderer>();
        caveRenderer.material = wallMaterial;

        // Генерация меша через Marching Cubes
        MarchingCubesGenerator marchingCubes = new MarchingCubesGenerator();
        Mesh caveMesh = marchingCubes.GenerateMesh(noiseMap, surfaceLevel);

        caveFilter.mesh = caveMesh;
    }

    Mesh GenerateLowPolyMesh(float noiseStrength)
    {
        Mesh mesh = new Mesh();

        // Генерация вершин
        Vector3[] vertices = new Vector3[width * depth];
        int[] triangles = new int[(width - 1) * (depth - 1) * 6];

        int vertIndex = 0;
        int triIndex = 0;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                float y = Mathf.PerlinNoise(x * 0.1f, z * 0.1f) * noiseStrength;
                vertices[vertIndex] = new Vector3(x, y, z);

                if (x < width - 1 && z < depth - 1)
                {
                    // Треугольники для сетки
                    triangles[triIndex] = vertIndex;
                    triangles[triIndex + 1] = vertIndex + width + 1;
                    triangles[triIndex + 2] = vertIndex + width;

                    triangles[triIndex + 3] = vertIndex;
                    triangles[triIndex + 4] = vertIndex + 1;
                    triangles[triIndex + 5] = vertIndex + width + 1;

                    triIndex += 6;
                }

                vertIndex++;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }
}
