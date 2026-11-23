using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using TempleOfBoredTitans.Environment;

/// <summary>
/// One-time scene builder for the expanded Temple of Bored Titans blockout.
/// It keeps the previous floor/wall/ceiling groups as an inactive scene backup.
/// </summary>
public static class TempleLevelRedesignBuilder
{
    private const int ChunkSize = 8;
    private const float WallTierHeight = 1.1f;
    private const string MeshFolder = "Assets/Environment/Meshes";
    private const string MaterialFolder = "Assets/Environment/Materials";

    public static string BuildExpandedTemple()
    {
        if (EditorApplication.isPlaying)
            return "Stop Play Mode before rebuilding the temple.";

        if (GameObject.Find("Expanded Temple Floors") != null)
            return "Expanded temple geometry is already in the scene; refusing to create duplicates.";

        GameObject oldFloors = GameObject.Find("Temple Floors");
        GameObject oldWalls = GameObject.Find("Temple Walls");
        GameObject oldCeilings = GameObject.Find("Temple Ceilings");
        GameObject props = GameObject.Find("Temple Props");
        if (oldFloors == null || oldWalls == null || oldCeilings == null || props == null)
            return "The expected Temple Floors, Temple Walls, Temple Ceilings, or Temple Props object is missing.";

        MeshFilter floorFilter = oldFloors.GetComponentInChildren<MeshFilter>(true);
        MeshRenderer floorRenderer = oldFloors.GetComponentInChildren<MeshRenderer>(true);
        MeshFilter wallFilter = oldWalls.GetComponentInChildren<MeshFilter>(true);
        MeshRenderer wallRenderer = oldWalls.GetComponentInChildren<MeshRenderer>(true);
        GameObject wallTemplate = oldWalls.transform.GetChild(0).gameObject;
        if (floorFilter == null || floorRenderer == null || wallFilter == null || wallRenderer == null)
            return "Could not find the imported floor and wall meshes/materials.";

        Material grassMaterial = null;
        Transform floorTransform = oldFloors.transform;
        for (int i = 0; i < floorTransform.childCount; i++)
        {
            MeshRenderer renderer = floorTransform.GetChild(i).GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.name == "CourtyardGround")
            {
                grassMaterial = renderer.sharedMaterial;
                break;
            }
        }
        if (grassMaterial == null)
            return "The imported CourtyardGround material is missing.";

        EnsureFolder("Assets/Environment");
        EnsureFolder(MeshFolder);
        EnsureFolder(MaterialFolder);

        GameObject legacy = new GameObject("Legacy Temple Blockout (inactive backup)");
        oldFloors.transform.SetParent(legacy.transform, true);
        oldWalls.transform.SetParent(legacy.transform, true);
        oldCeilings.transform.SetParent(legacy.transform, true);
        oldFloors.name = "Legacy Temple Floors";
        oldWalls.name = "Legacy Temple Walls";
        oldCeilings.name = "Legacy Temple Ceilings";
        legacy.SetActive(false);

        HashSet<Vector2Int> cells = BuildFloorCells();
        Dictionary<bool, Dictionary<Vector2Int, List<Vector2Int>>> floorChunks =
            new Dictionary<bool, Dictionary<Vector2Int, List<Vector2Int>>>();
        floorChunks[false] = new Dictionary<Vector2Int, List<Vector2Int>>();
        floorChunks[true] = new Dictionary<Vector2Int, List<Vector2Int>>();
        Dictionary<Vector2Int, List<Vector2Int>> ceilingChunks = new Dictionary<Vector2Int, List<Vector2Int>>();

        foreach (Vector2Int cell in cells)
        {
            bool courtyard = IsCourtyard(cell);
            Vector2Int chunk = ChunkOf(cell);
            AddToChunk(floorChunks[courtyard], chunk, cell);
            if (!courtyard) AddToChunk(ceilingChunks, chunk, cell);
        }

        GameObject floorRoot = new GameObject("Expanded Temple Floors");
        GameObject wallRoot = new GameObject("Expanded Temple Walls");
        GameObject ceilingRoot = new GameObject("Expanded High Ceilings");
        GameObject areasRoot = new GameObject("Temple Areas and Landmarks");
        BuildFloorChunks(floorRoot.transform, floorChunks[false], floorFilter.sharedMesh, floorRenderer.sharedMaterial, "Indoor");
        BuildFloorChunks(floorRoot.transform, floorChunks[true], floorFilter.sharedMesh, grassMaterial, "Courtyard");
        BuildWallChunks(wallRoot.transform, cells, wallFilter.sharedMesh, wallRenderer.sharedMaterial, wallTemplate.transform.localScale);
        BuildCeilingChunks(ceilingRoot.transform, ceilingChunks, GetCeilingMaterial(), CreateCubeMesh());
        BuildCentralPlatform(areasRoot.transform, floorFilter.sharedMesh, floorRenderer.sharedMaterial);

        CreateAreaMarker(areasRoot.transform, "Grand Entrance", new Vector3(0f, 0f, -38f));
        CreateAreaMarker(areasRoot.transform, "Central Temple Hall", new Vector3(0f, 0f, 10f));
        CreateAreaMarker(areasRoot.transform, "Western Angry Titan Wing", new Vector3(-34f, 0f, 10f));
        CreateAreaMarker(areasRoot.transform, "Eastern Monk Wing", new Vector3(34f, 0f, 10f));
        CreateAreaMarker(areasRoot.transform, "Northern Guardian Wing", new Vector3(0f, 0f, 43f));
        CreateAreaMarker(areasRoot.transform, "Southern Crypt", new Vector3(0f, 0f, -61f));
        CreateAreaMarker(areasRoot.transform, "Forgotten Courtyard", new Vector3(-72f, 0f, 2f));
        CreateAreaMarker(areasRoot.transform, "Fifth Titan Court", new Vector3(68f, 0f, -49f));
        CreateAreaMarker(areasRoot.transform, "Final Chamber", new Vector3(0f, 0f, 82f));

        PlaceGameObjects(props);
        PositionGameplayObjects();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return "Expanded temple built: " + cells.Count + " floor cells, " + floorRoot.transform.childCount +
            " combined floor chunks, " + wallRoot.transform.childCount + " wall chunks, and " + ceilingRoot.transform.childCount +
            " high-ceiling chunks. The old blockout remains as an inactive backup.";
    }

    /// <summary>Rebuilds the expanded wall chunks with continuous indoor tiers.</summary>
    public static string RebuildExpandedWalls()
    {
        if (EditorApplication.isPlaying)
            return "Stop Play Mode before rebuilding the temple walls.";

        GameObject currentWalls = GameObject.Find("Expanded Temple Walls");
        if (currentWalls == null)
            return "Expanded Temple Walls was not found.";

        GameObject legacyWalls = null;
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            if (allObjects[i].name == "Legacy Temple Walls" && allObjects[i].scene.IsValid())
            {
                legacyWalls = allObjects[i];
                break;
            }
        }
        if (legacyWalls == null || legacyWalls.transform.childCount == 0)
            return "Could not find the original wall mesh to rebuild from.";

        MeshFilter sourceFilter = legacyWalls.GetComponentInChildren<MeshFilter>(true);
        MeshRenderer sourceRenderer = legacyWalls.GetComponentInChildren<MeshRenderer>(true);
        if (sourceFilter == null || sourceRenderer == null)
            return "Could not find the original wall mesh or material.";

        Undo.RecordObject(currentWalls, "Rebuild continuous temple walls");
        currentWalls.name = "Expanded Temple Walls (old spacing backup)";
        currentWalls.SetActive(false);

        GameObject rebuiltWalls = new GameObject("Expanded Temple Walls");
        Undo.RegisterCreatedObjectUndo(rebuiltWalls, "Rebuild continuous temple walls");
        GameObject wallTemplate = legacyWalls.transform.GetChild(0).gameObject;
        BuildWallChunks(rebuiltWalls.transform, BuildFloorCells(), sourceFilter.sharedMesh,
            sourceRenderer.sharedMaterial, wallTemplate.transform.localScale);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        return "Rebuilt " + rebuiltWalls.transform.childCount +
            " wall chunks with four continuous indoor tiers; courtyard ruins remain low.";
    }

    private static HashSet<Vector2Int> BuildFloorCells()
    {
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        AddRect(cells, -14, 14, -45, -32);       // Grand entrance court
        AddRect(cells, -11, 11, -31, -10);       // Entrance hall
        AddRect(cells, -3, 3, -10, -7);          // Entrance to central hall
        AddRect(cells, -18, 18, -8, 27);         // 37 x 36 central hall
        AddRect(cells, -48, -20, -6, 26);        // Western wing
        AddRect(cells, -22, -16, 8, 14);         // West hall connector
        AddRect(cells, 20, 48, -6, 26);          // Eastern wing
        AddRect(cells, 16, 22, 8, 14);           // East hall connector
        AddRect(cells, -90, -54, -21, 25);        // 37 x 47 foggy courtyard
        AddRect(cells, -55, -47, -5, 8);          // Courtyard connector
        AddRect(cells, -16, 16, 28, 55);          // Northern guardian wing
        AddRect(cells, -3, 3, 25, 30);            // North connector
        AddRect(cells, -3, 3, 54, 67);            // Locked-gate approach
        AddRect(cells, -20, 20, 66, 99);          // 41 x 34 final chamber
        AddRect(cells, -17, 17, -76, -48);        // Southern crypt
        AddRect(cells, -3, 3, -51, -43);          // Crypt/entrance connector
        AddRect(cells, 54, 82, -68, -30);         // Fifth Titan court
        AddRect(cells, 47, 55, -30, 0);           // Eastern exploration connector
        return cells;
    }

    private static void AddRect(HashSet<Vector2Int> cells, int minX, int maxX, int minZ, int maxZ)
    {
        for (int x = minX; x <= maxX; x++)
            for (int z = minZ; z <= maxZ; z++)
                cells.Add(new Vector2Int(x, z));
    }

    private static bool IsCourtyard(Vector2Int cell)
    {
        return cell.x >= -90 && cell.x <= -54 && cell.y >= -21 && cell.y <= 25;
    }

    private static Vector2Int ChunkOf(Vector2Int cell)
    {
        return new Vector2Int(Mathf.FloorToInt(cell.x / (float)ChunkSize) * ChunkSize,
            Mathf.FloorToInt(cell.y / (float)ChunkSize) * ChunkSize);
    }

    private static void AddToChunk(Dictionary<Vector2Int, List<Vector2Int>> chunks, Vector2Int key, Vector2Int cell)
    {
        List<Vector2Int> list;
        if (!chunks.TryGetValue(key, out list))
        {
            list = new List<Vector2Int>();
            chunks.Add(key, list);
        }
        list.Add(cell);
    }

    private static void BuildFloorChunks(Transform parent, Dictionary<Vector2Int, List<Vector2Int>> chunks,
        Mesh tileMesh, Material material, string label)
    {
        foreach (KeyValuePair<Vector2Int, List<Vector2Int>> entry in chunks)
        {
            Vector2Int origin = entry.Key;
            List<CombineInstance> combines = new List<CombineInstance>();
            foreach (Vector2Int cell in entry.Value)
            {
                CombineInstance combine = new CombineInstance();
                combine.mesh = tileMesh;
                combine.transform = Matrix4x4.Translate(new Vector3(cell.x - origin.x, 0f, cell.y - origin.y));
                combines.Add(combine);
            }
            Mesh mesh = Combine(combines, label + "Floor_" + origin.x + "_" + origin.y);
            SaveMesh(mesh, "ExpandedTemple_" + label + "Floor_" + origin.x + "_" + origin.y + ".asset");
            GameObject chunk = new GameObject(label + " Floor Chunk " + origin.x + " " + origin.y);
            chunk.transform.SetParent(parent, false);
            chunk.transform.position = new Vector3(origin.x, 0f, origin.y);
            chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = chunk.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            chunk.AddComponent<MeshCollider>().sharedMesh = mesh;
            chunk.isStatic = true;
        }
    }

    private static void BuildWallChunks(Transform parent, HashSet<Vector2Int> cells, Mesh wallMesh,
        Material material, Vector3 wallScale)
    {
        Dictionary<Vector2Int, List<CombineInstance>> chunks = new Dictionary<Vector2Int, List<CombineInstance>>();
        Vector2Int[] directions = new Vector2Int[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        foreach (Vector2Int cell in cells)
        {
            for (int d = 0; d < directions.Length; d++)
            {
                if (cells.Contains(cell + directions[d])) continue;
                if (cell.x >= -4 && cell.x <= 4 && cell.y == -45 && d == 3) continue; // open monumental entrance
                Vector3 position;
                if (d == 0) position = new Vector3(cell.x + 0.5f, 0f, cell.y);
                else if (d == 1) position = new Vector3(cell.x - 0.5f, 0f, cell.y);
                else if (d == 2) position = new Vector3(cell.x, 0f, cell.y + 0.5f);
                else position = new Vector3(cell.x, 0f, cell.y - 0.5f);

                Vector2Int chunkOrigin = ChunkOf(new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)));
                List<CombineInstance> list;
                if (!chunks.TryGetValue(chunkOrigin, out list))
                {
                    list = new List<CombineInstance>();
                    chunks.Add(chunkOrigin, list);
                }
                int tiers = IsCourtyard(cell) ? 1 : 4;
                for (int tier = 0; tier < tiers; tier++)
                {
                    CombineInstance combine = new CombineInstance();
                    combine.mesh = wallMesh;
                    combine.transform = Matrix4x4.TRS(
                        new Vector3(position.x - chunkOrigin.x, tier * WallTierHeight, position.z - chunkOrigin.y),
                        Quaternion.identity, wallScale);
                    list.Add(combine);
                }
            }
        }

        foreach (KeyValuePair<Vector2Int, List<CombineInstance>> entry in chunks)
        {
            Mesh mesh = Combine(entry.Value, "Wall_" + entry.Key.x + "_" + entry.Key.y);
            SaveMesh(mesh, "ContinuousTemple_Wall_" + entry.Key.x + "_" + entry.Key.y + ".asset");
            GameObject chunk = new GameObject("Wall Chunk " + entry.Key.x + " " + entry.Key.y);
            chunk.transform.SetParent(parent, false);
            chunk.transform.position = new Vector3(entry.Key.x, 0f, entry.Key.y);
            chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = chunk.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            chunk.AddComponent<MeshCollider>().sharedMesh = mesh;
            chunk.isStatic = true;
        }
    }

    private static void BuildCeilingChunks(Transform parent, Dictionary<Vector2Int, List<Vector2Int>> chunks,
        Material material, Mesh cubeMesh)
    {
        foreach (KeyValuePair<Vector2Int, List<Vector2Int>> entry in chunks)
        {
            Vector2Int origin = entry.Key;
            List<CombineInstance> combines = new List<CombineInstance>();
            foreach (Vector2Int cell in entry.Value)
            {
                CombineInstance combine = new CombineInstance();
                combine.mesh = cubeMesh;
                combine.transform = Matrix4x4.TRS(new Vector3(cell.x - origin.x + 0.5f, 7.1f, cell.y - origin.y + 0.5f),
                    Quaternion.identity, new Vector3(1f, 0.12f, 1f));
                combines.Add(combine);
            }
            Mesh mesh = Combine(combines, "HighCeiling_" + origin.x + "_" + origin.y);
            SaveMesh(mesh, "ExpandedTemple_Ceiling_" + origin.x + "_" + origin.y + ".asset");
            GameObject chunk = new GameObject("High Ceiling Chunk " + origin.x + " " + origin.y);
            chunk.transform.SetParent(parent, false);
            chunk.transform.position = new Vector3(origin.x, 0f, origin.y);
            chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = chunk.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            chunk.isStatic = true;
        }
    }

    private static void BuildCentralPlatform(Transform parent, Mesh tileMesh, Material material)
    {
        List<CombineInstance> combines = new List<CombineInstance>();
        for (int x = -3; x <= 3; x++)
            for (int z = -3; z <= 3; z++)
            {
                CombineInstance combine = new CombineInstance();
                combine.mesh = tileMesh;
                combine.transform = Matrix4x4.Translate(new Vector3(x, 0f, z));
                combines.Add(combine);
            }
        Mesh mesh = Combine(combines, "CentralHallLandmark");
        SaveMesh(mesh, "ExpandedTemple_CentralPlatform.asset");
        GameObject platform = new GameObject("Central Hall Landmark Platform");
        platform.transform.SetParent(parent, false);
        platform.transform.position = new Vector3(0f, 0.22f, 10f);
        platform.AddComponent<MeshFilter>().sharedMesh = mesh;
        platform.AddComponent<MeshRenderer>().sharedMaterial = material;
        platform.AddComponent<MeshCollider>().sharedMesh = mesh;
        platform.isStatic = true;
    }

    private static Mesh Combine(List<CombineInstance> combines, string name)
    {
        Mesh mesh = new Mesh();
        mesh.name = name;
        mesh.indexFormat = combines.Count * 36 > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.CombineMeshes(combines.ToArray(), true, true, false);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateCubeMesh()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh mesh = cube.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(cube);
        return mesh;
    }

    private static Material GetCeilingMaterial()
    {
        string path = MaterialFolder + "/ExpandedTempleCeiling.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            material = new Material(shader);
            material.name = "ExpandedTempleCeiling";
            material.color = new Color(0.055f, 0.065f, 0.11f, 1f);
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }

    private static void SaveMesh(Mesh mesh, string fileName)
    {
        AssetDatabase.CreateAsset(mesh, MeshFolder + "/" + fileName);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string name = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static void CreateAreaMarker(Transform parent, string label, Vector3 position)
    {
        GameObject marker = new GameObject("Area - " + label);
        marker.transform.SetParent(parent, false);
        marker.transform.position = position;
    }

    private static void PlaceGameObjects(GameObject props)
    {
        Transform root = props.transform;
        List<Transform> pillars = FindChildren(root, "Stone Pillar");
        if (pillars.Count > 0)
        {
            Transform pillarTemplate = pillars[0];
            Vector3[] pillarPositions = new Vector3[]
            {
                new Vector3(-15f,0f,-4f), new Vector3(-15f,0f,4f), new Vector3(-15f,0f,16f), new Vector3(-15f,0f,24f),
                new Vector3(15f,0f,-4f), new Vector3(15f,0f,4f), new Vector3(15f,0f,16f), new Vector3(15f,0f,24f),
                new Vector3(-9f,0f,26f), new Vector3(9f,0f,26f), new Vector3(-9f,0f,-7f), new Vector3(9f,0f,-7f),
                new Vector3(-45f,0f,-2f), new Vector3(-24f,0f,22f), new Vector3(24f,0f,-2f), new Vector3(45f,0f,22f),
                new Vector3(-12f,0f,70f), new Vector3(12f,0f,70f), new Vector3(-12f,0f,94f), new Vector3(12f,0f,94f)
            };
            for (int i = 0; i < pillarPositions.Length; i++)
            {
                Transform target;
                if (i < pillars.Count) target = pillars[i];
                else target = Object.Instantiate(pillarTemplate, root);
                target.name = "Temple Pillar " + (i + 1);
                target.position = pillarPositions[i];
                target.localScale = new Vector3(1f, 1.8f, 1f);
            }
        }

        List<Transform> pines = FindChildren(root, "Courtyard Pine");
        if (pines.Count > 0)
        {
            Transform template = pines[0];
            Vector3[] positions = new Vector3[]
            {
                new Vector3(-88f,0f,-17f), new Vector3(-57f,0f,-17f), new Vector3(-88f,0f,20f), new Vector3(-57f,0f,20f),
                new Vector3(-82f,0f,-20f), new Vector3(-64f,0f,23f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                Transform target = i < pines.Count ? pines[i] : Object.Instantiate(template, root);
                target.name = "Courtyard Pine " + (i + 1);
                target.position = positions[i];
            }
        }

        List<Transform> rocks = FindChildren(root, "Courtyard Rocks");
        if (rocks.Count > 0)
        {
            Transform template = rocks[0];
            Vector3[] positions = new Vector3[]
            {
                new Vector3(-88f,0f,-7f), new Vector3(-58f,0f,0f), new Vector3(-86f,0f,12f),
                new Vector3(-64f,0f,-19f), new Vector3(-78f,0f,22f), new Vector3(-55f,0f,18f)
            };
            for (int i = 0; i < positions.Length; i++)
            {
                Transform target = i < rocks.Count ? rocks[i] : Object.Instantiate(template, root);
                target.name = "Courtyard Rocks " + (i + 1);
                target.position = positions[i];
            }
        }

        Transform chest = FindChild(root, "Sealed Treasure Chest");
        if (chest != null) chest.position = new Vector3(7f, 0f, -39f);

        Vector2[] torchPositions = new Vector2[]
        {
            new Vector2(-10f,-40f),new Vector2(10f,-40f),new Vector2(-10f,-33f),new Vector2(10f,-33f),
            new Vector2(-9f,-23f),new Vector2(9f,-23f),new Vector2(-9f,-15f),new Vector2(9f,-15f),
            new Vector2(-17f,0f),new Vector2(17f,0f),new Vector2(-17f,20f),new Vector2(17f,20f),new Vector2(0f,25f)
        };
        Transform propsRoot = root;
        Transform torchTemplate = FindChild(propsRoot, "Wall Torch 0");
        Transform glowTemplate = FindChild(propsRoot, "Torch Glow 0");
        if (torchTemplate != null && glowTemplate != null)
        {
            for (int i = 0; i < torchPositions.Length; i++)
            {
                Transform torch = i < 13 ? FindChild(propsRoot, "Wall Torch " + i) : null;
                Transform glow = i < 13 ? FindChild(propsRoot, "Torch Glow " + i) : null;
                if (torch == null) torch = Object.Instantiate(torchTemplate, propsRoot);
                if (glow == null) glow = Object.Instantiate(glowTemplate, propsRoot);
                torch.name = "Expanded Wall Torch " + i;
                glow.name = "Expanded Torch Glow " + i;
                torch.position = new Vector3(torchPositions[i].x, 2.5f, torchPositions[i].y);
                glow.position = new Vector3(torchPositions[i].x, 2.9f, torchPositions[i].y);
                Light light = glow.GetComponent<Light>();
                if (light != null) light.range = 8f;
            }

            Vector2[] extraTorches = new Vector2[]
            {
                new Vector2(-43f,2f),new Vector2(-25f,22f),new Vector2(25f,2f),new Vector2(43f,22f),
                new Vector2(-4f,44f),new Vector2(4f,52f),new Vector2(-12f,75f),new Vector2(12f,90f),
                new Vector2(-68f,4f),new Vector2(-78f,-10f),new Vector2(-4f,-60f),new Vector2(4f,-51f),
                new Vector2(62f,-37f),new Vector2(76f,-60f)
            };
            for (int i = 0; i < extraTorches.Length; i++)
            {
                Transform torch = Object.Instantiate(torchTemplate, propsRoot);
                Transform glow = Object.Instantiate(glowTemplate, propsRoot);
                torch.name = "Expanded Wing Torch " + i;
                glow.name = "Expanded Wing Torch Glow " + i;
                torch.position = new Vector3(extraTorches[i].x, 2.5f, extraTorches[i].y);
                glow.position = new Vector3(extraTorches[i].x, 2.9f, extraTorches[i].y);
                Light light = glow.GetComponent<Light>();
                if (light != null) light.range = 8f;
            }
        }
    }

    private static List<Transform> FindChildren(Transform parent, string exactName)
    {
        List<Transform> matches = new List<Transform>();
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == exactName) matches.Add(parent.GetChild(i));
        return matches;
    }

    private static Transform FindChild(Transform parent, string exactName)
    {
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == exactName) return parent.GetChild(i);
        return null;
    }

    private static void PositionGameplayObjects()
    {
        SetPosition("Treasure Hunter Player", new Vector3(0f, 0f, -39f), Quaternion.identity);
        SetPosition("Titan Symbol 1 - Entrance", new Vector3(9f, 0f, -37f), Quaternion.identity);
        SetPosition("Titan Symbol 2 - Courtyard", new Vector3(-82f, 0f, 15f), Quaternion.identity);
        SetPosition("Titan Symbol 3 - Hall", new Vector3(0f, 0.25f, 10f), Quaternion.identity);
        SetPosition("Titan Symbol 4 - Guardian", new Vector3(12f, 0f, 48f), Quaternion.identity);
        SetPosition("Titan Symbol 5 - Puzzle", new Vector3(0f, 0f, -61f), Quaternion.identity);
        SetPosition("Angry Titan - Dragon", new Vector3(-71f, 0f, 1f), Quaternion.Euler(0f, 90f, 0f));
        SetPosition("Wailing Skeleton", new Vector3(34f, 0f, 12f), Quaternion.identity);
        SetPosition("Plague Brute", new Vector3(0f, 0f, 40f), Quaternion.identity);
        SetPosition("Winged Reaper", new Vector3(0f, 0f, -61f), Quaternion.identity);
        SetPosition("Echo Archivist", new Vector3(68f, 0f, -49f), Quaternion.Euler(0f, 180f, 0f));
        SetPosition("Guardian Noise Maker", new Vector3(8f, 0.35f, 39f), Quaternion.identity);
        SetPosition("Courtyard Moon Glow", new Vector3(-72f, 8f, 2f), Quaternion.identity);
        SetPosition("Final Chamber Rune Light", new Vector3(0f, 5f, 82f), Quaternion.identity);

        GameObject gate = GameObject.Find("Final Chamber Gate");
        if (gate != null)
        {
            gate.transform.position = new Vector3(0f, 0f, 61f);
            gate.transform.localScale = new Vector3(7f, 2.6f, 1f);
            DoorController door = gate.GetComponent<DoorController>();
            if (door != null)
            {
                SerializedObject so = new SerializedObject(door);
                so.FindProperty("openHeight").floatValue = 5f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        GameObject moon = GameObject.Find("Courtyard Moon Glow");
        if (moon != null)
        {
            Light light = moon.GetComponent<Light>();
            if (light != null) light.range = 55f;
        }
        GameObject rune = GameObject.Find("Final Chamber Rune Light");
        if (rune != null)
        {
            Light light = rune.GetComponent<Light>();
            if (light != null) light.range = 28f;
        }
    }

    private static void SetPosition(string objectName, Vector3 position, Quaternion rotation)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj == null) return;
        obj.transform.position = position;
        obj.transform.rotation = rotation;
    }
}

