using UnityEngine;
using System.Collections.Generic;

public class CaveGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    public int width = 100;
    public int height = 100;
    public float fillPercent = 0.51f;
    public int smoothIterations = 6;

    [Header("Wall Settings")]
    public int minWallHeight = 3;
    public int maxWallHeight = 7;

    [Header("Tile Prefabs")]
    public Mesh groundMesh;
    public Mesh wallMeshHighDetail;
    public Mesh wallMeshLowDetail;
    public Material groundMaterial;
    public Material wallMaterial;

    private int[,] map;
    private int[,] wallHeights;

    void Start()
    {
        GenerateMap();
        SmoothMap();
        GenerateWallHeights();
        SmoothWallHeights();
        RenderMapWithLOD();
    }

    void GenerateMap()
    {
        map = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                {
                    map[x, y] = 1;
                }
                else
                {
                    map[x, y] = Random.value < fillPercent ? 1 : 0;
                }
            }
        }
    }

    void SmoothMap()
    {
        for (int i = 0; i < smoothIterations; i++)
        {
            int[,] newMap = (int[,])map.Clone();

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    int wallCount = GetSurroundingWallCount(x, y);

                    if (wallCount > 4)
                        newMap[x, y] = 1;
                    else if (wallCount < 4)
                        newMap[x, y] = 0;
                }
            }

            map = newMap;
        }
    }

    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;

        for (int neighborX = gridX - 1; neighborX <= gridX + 1; neighborX++)
        {
            for (int neighborY = gridY - 1; neighborY <= gridY + 1; neighborY++)
            {
                if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
                {
                    if (neighborX != gridX || neighborY != gridY)
                    {
                        wallCount += map[neighborX, neighborY];
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }

        return wallCount;
    }

    void GenerateWallHeights()
    {
        wallHeights = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (map[x, y] == 1)
                {
                    wallHeights[x, y] = Random.Range(minWallHeight, maxWallHeight + 1);
                }
                else
                {
                    wallHeights[x, y] = 0;
                }
            }
        }
    }

    void SmoothWallHeights()
    {
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (map[x, y] == 1)
                {
                    int totalHeight = 0;
                    int wallCount = 0;

                    for (int neighborX = x - 1; neighborX <= x + 1; neighborX++)
                    {
                        for (int neighborY = y - 1; neighborY <= y + 1; neighborY++)
                        {
                            if (map[neighborX, neighborY] == 1)
                            {
                                totalHeight += wallHeights[neighborX, neighborY];
                                wallCount++;
                            }
                        }
                    }

                    if (wallCount > 0)
                    {
                        wallHeights[x, y] = Mathf.RoundToInt((float)totalHeight / wallCount);
                    }
                }
            }
        }
    }

    void RenderMapWithLOD()
    {
        List<CombineInstance> groundCombines = new List<CombineInstance>();
        List<CombineInstance> wallCombinesHighDetail = new List<CombineInstance>();
        List<CombineInstance> wallCombinesLowDetail = new List<CombineInstance>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 basePosition = new Vector3(x, 0, y);

                if (map[x, y] == 0) // Пол
                {
                    CombineInstance groundInstance = new CombineInstance();
                    groundInstance.mesh = groundMesh;
                    groundInstance.transform = Matrix4x4.TRS(basePosition, Quaternion.identity, Vector3.one);
                    groundCombines.Add(groundInstance);
                }
                else if (map[x, y] == 1) // Стена
                {
                    int wallHeight = wallHeights[x, y];

                    for (int z = 0; z < wallHeight; z++)
                    {
                        Vector3 wallPosition = basePosition + new Vector3(0, z, 0);

                        CombineInstance wallInstanceHigh = new CombineInstance();
                        wallInstanceHigh.mesh = wallMeshHighDetail;
                        wallInstanceHigh.transform = Matrix4x4.TRS(wallPosition, Quaternion.identity, Vector3.one);
                        wallCombinesHighDetail.Add(wallInstanceHigh);

                        CombineInstance wallInstanceLow = new CombineInstance();
                        wallInstanceLow.mesh = wallMeshLowDetail;
                        wallInstanceLow.transform = Matrix4x4.TRS(wallPosition, Quaternion.identity, Vector3.one);
                        wallCombinesLowDetail.Add(wallInstanceLow);
                    }
                }
            }
        }

        // Создание и настройка мешей
        CreateMeshWithUInt32("Ground Mesh", groundCombines, groundMaterial);
        CreateLODMeshes(wallCombinesHighDetail, wallCombinesLowDetail);
    }

    void CreateMeshWithUInt32(string meshName, List<CombineInstance> combines, Material material)
    {
        GameObject obj = new GameObject(meshName);
        obj.transform.position = Vector3.zero;
        MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
        MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
        renderer.material = material;

        Mesh combinedMesh = new Mesh();
        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        combinedMesh.CombineMeshes(combines.ToArray(), true, true);
        meshFilter.mesh = combinedMesh;
    }

    void CreateLODMeshes(List<CombineInstance> highDetailCombines, List<CombineInstance> lowDetailCombines)
    {
        GameObject wallsLOD = new GameObject("Walls LOD");
        wallsLOD.transform.position = Vector3.zero;
        LODGroup lodGroup = wallsLOD.AddComponent<LODGroup>();

        GameObject highDetailWalls = new GameObject("High Detail Walls");
        highDetailWalls.transform.parent = wallsLOD.transform;
        MeshFilter highDetailFilter = highDetailWalls.AddComponent<MeshFilter>();
        MeshRenderer highDetailRenderer = highDetailWalls.AddComponent<MeshRenderer>();
        highDetailRenderer.material = wallMaterial;
        highDetailFilter.mesh = new Mesh();
        highDetailFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        highDetailFilter.mesh.CombineMeshes(highDetailCombines.ToArray(), true, true);

        GameObject lowDetailWalls = new GameObject("Low Detail Walls");
        lowDetailWalls.transform.parent = wallsLOD.transform;
        MeshFilter lowDetailFilter = lowDetailWalls.AddComponent<MeshFilter>();
        MeshRenderer lowDetailRenderer = lowDetailWalls.AddComponent<MeshRenderer>();
        lowDetailRenderer.material = wallMaterial;
        lowDetailFilter.mesh = new Mesh();
        lowDetailFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        lowDetailFilter.mesh.CombineMeshes(lowDetailCombines.ToArray(), true, true);

        LOD[] lods = new LOD[2];
        lods[0] = new LOD(0.6f, new Renderer[] { highDetailRenderer });
        lods[1] = new LOD(0.3f, new Renderer[] { lowDetailRenderer });
        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();
    }
}
