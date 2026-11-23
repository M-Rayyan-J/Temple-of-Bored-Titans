using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using TempleOfBoredTitans.Story;
using TempleOfBoredTitans.Titans;
using TempleOfBoredTitans.Player;
using TempleOfBoredTitans.Core;

/// <summary>Rebuilds the active blockout around the user's storyboard beats.</summary>
public static class TempleStoryboardBuilder
{
    private const int LayoutVersion = 22;
    private static Transform layout;
    private static Material stone;
    private static Material grass;
    private static Material roof;

    // If Unity reloads this editor script while the legacy prototype is open,
    // rebuild it once after Play Mode stops. This prevents the old starter room
    // from remaining in the saved scene just because the menu item was missed.
    static TempleStoryboardBuilder()
    {
        EditorApplication.delayCall += QueueLegacySceneRebuild;
    }

    private static void QueueLegacySceneRebuild()
    {
        if (EditorApplication.isCompiling) { EditorApplication.delayCall += QueueLegacySceneRebuild; return; }
        if (EditorSceneManager.GetActiveScene().path != "Assets/Scenes/TemplePrototype.unity") return;
        if (GameObject.Find("Storyboard Layout Version " + LayoutVersion) != null)
        {
            RefreshAncientTorchLights();
            return;
        }
        EditorApplication.update -= RebuildLegacySceneWhenReady;
        EditorApplication.update += RebuildLegacySceneWhenReady;
    }

    private static void RebuildLegacySceneWhenReady()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        EditorApplication.update -= RebuildLegacySceneWhenReady;
        if (EditorSceneManager.GetActiveScene().path == "Assets/Scenes/TemplePrototype.unity" && GameObject.Find("Storyboard Layout Version " + LayoutVersion) == null)
            Rebuild();
    }

    private static void RefreshAncientTorchLights()
    {
        bool changed = false;
        foreach (var light in Resources.FindObjectsOfTypeAll<Light>())
        {
            if (!light.gameObject.scene.IsValid() || light.gameObject.name != "Torch Glow") continue;
            light.shadows = LightShadows.None;
            light.color = new Color(1f, .72f, .28f);
            light.intensity = 3.7f;
            light.range = 13f;
            changed = true;
        }
        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }
    

    [MenuItem("Tools/Temple of Bored Titans/Rebuild Storyboard Layout")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Stop Play Mode before rebuilding the story layout."); return; }
        var renderers = Object.FindObjectsByType<MeshRenderer>();
        Material stoneSource = AssetDatabase.LoadAssetAtPath<Material>("Assets/Environment/Materials/ExpandedTempleCeiling.mat");
        Material grassSource = AssetDatabase.LoadAssetAtPath<Material>("Assets/Environment/Materials/CourtyardGround.mat");
        if (stoneSource == null)
        {
            foreach (var r in renderers)
                if (r.sharedMaterial != null && r.sharedMaterial.name == "colormap") { stoneSource = r.sharedMaterial; break; }
        }
        if (stoneSource == null && renderers.Length > 0) stoneSource = renderers[0].sharedMaterial;
        if (stoneSource == null) { Debug.LogError("Could not find a temple material to build the new layout."); return; }

        // Warm weathered sandstone replaces the clean blue-gray blockout.
        stone = MakeMaterial("Assets/Environment/Materials/StoryboardStone.mat", stoneSource, new Color(.26f, .20f, .13f));
        var savedGrass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Environment/Materials/StoryboardJungleGround.mat");
        grass = MakeMaterial("Assets/Environment/Materials/StoryboardJungleGround.mat", grassSource ?? stoneSource, new Color(.15f, .28f, .065f), false);
        roof = MakeMaterial("Assets/Environment/Materials/StoryboardRoof.mat", stoneSource, new Color(.075f, .055f, .035f));

        foreach (string old in new[] { "Expanded Temple Floors", "Expanded Temple Walls", "Expanded High Ceilings", "Expanded Temple Roof", "Visible Temple Roof Beams", "Continuous Temple Roof", "Temple Areas and Landmarks", "Temple Props", "Temple Floors", "Temple Walls", "Temple Ceilings", "Legacy Temple Floors", "Legacy Temple Walls", "Legacy Temple Ceilings", "Legacy Temple Props" })
        {
            var g = GameObject.Find(old);
            if (g != null) g.SetActive(false);
        }

        var previous = GameObject.Find("Storyboard Layout");
        if (previous != null) Object.DestroyImmediate(previous);
        layout = new GameObject("Storyboard Layout").transform;

        BuildFloors();
        BuildWallsAndRoof();
        BuildTemplePillars();
        BuildJungleProps();
        BuildCryptDecor();
        BuildFiveStatues();
        BuildTreasureDoorway();
        PlaceGameplay();
        ApplyHorrorAtmosphere();
        BuildEntrySequence();
        var versionMarker = new GameObject("Storyboard Layout Version " + LayoutVersion).transform;
        versionMarker.SetParent(layout, false);

        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.scene.IsValid()) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Storyboard layout rebuilt: long explorable forest, torch-lit branching crypt, five Titan chambers, two exterior temple openings, and an escape cinematic.");
    }

    public static void RebuildTemplePrototypeBatch()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/TemplePrototype.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
        if (!scene.IsValid()) { Debug.LogError("Could not open TemplePrototype."); return; }
        Rebuild();
    }
    private static Material MakeMaterial(string path, Material source, Color tint, bool retainTexture = true)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null) { Debug.LogError("Could not find a supported Lit shader."); return source; }
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }
        Texture texture = retainTexture && source != null ? source.mainTexture : null;
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        if (source != null)
        {
            if (material.HasProperty("_BaseMap")) { material.SetTextureScale("_BaseMap", source.mainTextureScale); material.SetTextureOffset("_BaseMap", source.mainTextureOffset); }
            if (material.HasProperty("_MainTex")) { material.SetTextureScale("_MainTex", source.mainTextureScale); material.SetTextureOffset("_MainTex", source.mainTextureOffset); }
        }
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .16f);
        material.color = tint;
        if (material.HasProperty("_EmissionColor"))
        {
            Color glow = path.Contains("StoryboardJungleGround") ? new Color(.035f,.085f,.012f) :
                path.Contains("StoryboardStone") ? new Color(.025f,.014f,.006f) : Color.black;
            if (glow.maxColorComponent > 0f) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", glow); }
            else { material.DisableKeyword("_EMISSION"); material.SetColor("_EmissionColor", Color.black); }
        }
        return material;
    }

    private static void ConvertAvatarMaterialsToUrp(GameObject avatar)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (avatar == null || shader == null) return;
        foreach (var renderer in avatar.GetComponentsInChildren<Renderer>(true))
        {
            var originals = renderer.sharedMaterials;
            var converted = new Material[originals.Length];
            for (int i = 0; i < originals.Length; i++)
            {
                Material original = originals[i];
                if (original == null) { converted[i] = null; continue; }
                string safeName = System.Text.RegularExpressions.Regex.Replace(original.name, "[^A-Za-z0-9_-]", "_");
                string path = "Assets/Environment/Materials/Avatar_" + safeName + ".mat";
                Material urp = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (urp == null)
                {
                    urp = new Material(shader) { name = "Avatar " + original.name };
                    AssetDatabase.CreateAsset(urp, path);
                }
                else urp.shader = shader;
                Texture texture = original.mainTexture;
                if (urp.HasProperty("_BaseMap")) urp.SetTexture("_BaseMap", texture);
                if (urp.HasProperty("_MainTex")) urp.SetTexture("_MainTex", texture);
                if (urp.HasProperty("_Metallic")) urp.SetFloat("_Metallic", 0f);
                if (urp.HasProperty("_Smoothness")) urp.SetFloat("_Smoothness", .24f);
                urp.color = original.color;
                converted[i] = urp;
            }
            renderer.sharedMaterials = converted;
        }
    }

    private static void BuildFloors()
    {
        // Four adjoining forest fields plus the temple slab cover the entire
        // playable rectangle. The old 72-unit forest width left bare blue strips
        // beside the temple, where players could walk off the ground.
        Box("Continuous Forest Ground", new Vector3(0, -.18f, -44f), new Vector3(120, .36f, 64), grass, true);
        Box("Escape Forest Ground", new Vector3(0, -.18f, 57f), new Vector3(120, .36f, 42), grass, true);
        Box("West Forest Ground beside Temple", new Vector3(-39.5f, -.18f, 12f), new Vector3(41f, .36f, 48f), grass, true);
        Box("East Forest Ground beside Temple", new Vector3(39.5f, -.18f, 12f), new Vector3(41f, .36f, 48f), grass, true);
        Box("Continuous Ancient Temple Flagstone Floor", new Vector3(0, -.15f, 12f), new Vector3(38f, .3f, 48f), stone, true);
        for (int i = 0; i < 3; i++) Box("Crypt Rune Marker " + (i + 1), new Vector3((i - 1) * 4.8f, .03f, 18f), new Vector3(1.4f, .12f, .8f), roof, false);
    }

    private static void BuildWallsAndRoof()
    {
        // Rebuild the architecture as weathered, jointed stone instead of smooth modern slabs.
        BuildAncientMasonry();
        BuildBrokenRoof();
        BuildSafetyBoundaries();
    }

    private static void BuildAncientMasonry()
    {
        // Tall, enclosed burial architecture with a single route through connected chambers.
        AncientWall("Temple Exterior West Wall", -18.8f, 10.6f, 45.7f, true, -1);
        AncientWall("Temple Exterior East Wall", 18.8f, 10.6f, 45.7f, true, 1);
        AncientWall("Temple Entrance Outer Wing West", -11.4f, -12.25f, 14.8f, false, -1);
        AncientWall("Temple Entrance Outer Wing East", 11.4f, -12.25f, 14.8f, false, 1);
        AncientWall("Temple Escape Outer Wall West", -11.4f, 33.5f, 14.8f, false, -1);
        AncientWall("Temple Escape Outer Wall East", 11.4f, 33.5f, 14.8f, false, 1);
        AncientPier("Entry Jamb West", -3.2f, -12.25f, true);
        AncientPier("Entry Jamb East", 3.2f, -12.25f, true);
        AncientArch("Entry", 0f, -12.25f);
        AncientPier("Escape Door Jamb West", -3.2f, 33.5f, true);
        AncientPier("Escape Door Jamb East", 3.2f, 33.5f, true);
        AncientArch("Escape", 0f, 33.5f);

        // Vestibule partitions feed the nave through a tomb arch.
        AncientWall("Vestibule West Wall", -7.2f, -6.5f, 11f, true, -1);
        AncientWall("Vestibule East Wall", 7.2f, -6.5f, 11f, true, 1);
        AncientWall("Vestibule North West Pier", -4.5f, -1.5f, 5.4f, false, -1);
        AncientWall("Vestibule North East Pier", 4.5f, -1.5f, 5.4f, false, 1);

        // Nave side walls have broad portals to chapels, narrowing the central sightlines.
        AncientWall("Nave West Front Wall", -10.1f, 1.2f, 5.5f, true, -1);
        AncientWall("Nave West Rear Wall", -10.1f, 8.4f, 5.3f, true, 1);
        AncientWall("Nave East Front Wall", 10.1f, 1.2f, 5.5f, true, 1);
        AncientWall("Nave East Rear Wall", 10.1f, 8.4f, 5.3f, true, -1);
        foreach (float x in new[] {-13.5f,13.5f})
        {
            AncientWall("Chapel South Wing "+x, x, -4.25f, 9f, false, x<0?1:-1);
            AncientWall("Reliquary Rear "+x, x, 18.2f, 9f, false, x<0?1:-1);
        }
        // The ring crypt wraps around low tombs; its north opening leads to the maze.
        AncientWall("Ring Crypt South Divider West", -6.5f, 11.2f, 7f, false, -1);
        AncientWall("Ring Crypt South Divider East", 6.5f, 11.2f, 7f, false, 1);
        // Keep the approach to the three vault doors visually open. The old
        // full-width S-turn partitions hid the side doors from the player.
        // Final sanctum and escape vestibule.
        AncientWall("Sanctum West Wall", -10.1f, 29f, 8f, true, -1);
        AncientWall("Sanctum East Wall", 10.1f, 29f, 8f, true, 1);
        // The three vault openings are built as a separate cross-wall below.

    }

    private static void AncientWall(string name, float cross, float along, float length, bool longitudinal, int lean)
    {
        // Large, rough-cut ashlar: fewer substantial stones keep the route legible
        // and avoid the dense stack of tiny blocks from the previous pass.
        int count = Mathf.CeilToInt(length / 3.5f);
        float segmentLength = length / count;
        for (int course = 0; course < 2; course++)
        {
            float y = course == 0 ? 1.35f : 4.0f;
            for (int i = 0; i < count; i++)
            {
                float offset = course == 0 ? 0f : segmentLength * .34f;
                float u = -length * .5f + segmentLength * .5f + i * segmentLength + offset;
                u = Mathf.Clamp(u, -length * .5f + segmentLength * .5f, length * .5f - segmentLength * .5f);
                float irregular = Mathf.Sin((i + 2f) * 2.13f + course * 4.8f) * .07f;
                float depth = .86f + ((i + course) % 4 == 0 ? .2f : 0f);
                Vector3 p = longitudinal ? new Vector3(cross + irregular * lean, y, along + u) : new Vector3(cross + u, y, along + irregular * lean);
                Vector3 size = longitudinal ? new Vector3(depth, 2.62f, segmentLength + .08f) : new Vector3(segmentLength + .08f, 2.62f, depth);
                var block = Box(name + " Ashlar " + course + " " + i, p, size, stone, true);
                if ((i + course) % 5 == 0)
                {
                    block.transform.rotation = Quaternion.Euler(0f, longitudinal ? (i % 2 == 0 ? .8f : -.8f) : 0f, longitudinal ? 0f : (i % 2 == 0 ? .8f : -.8f));
                    block.transform.position += Vector3.down * .08f;
                }
            }
        }
        for (float u = -length * .5f + 1.5f; u <= length * .5f - 1f; u += 12f)
        {
            Vector3 p = longitudinal ? new Vector3(cross + lean * .5f, 2.65f, along + u) : new Vector3(cross + u, 2.65f, along + lean * .5f);
            var buttress = Box(name + " Battered Buttress", p, longitudinal ? new Vector3(1.0f,5.2f,1.15f) : new Vector3(1.15f,5.2f,1.0f), roof, true);
            buttress.transform.rotation = Quaternion.Euler(0f, longitudinal ? 0f : lean * 2.5f, longitudinal ? -lean * 2.5f : 0f);
            Box(name + " Buttress Foot", p + Vector3.down * 2.05f, new Vector3(1.35f,.55f,1.35f), stone, true);
        }
    }

    private static void AncientPier(string name, float x, float z, bool collision)
    {
        Box(name + " Lower Plinth", new Vector3(x,.45f,z), new Vector3(1.45f,.9f,1.05f), roof, collision);
        Box(name + " Carved Base", new Vector3(x,1.05f,z), new Vector3(1.18f,.32f,.9f), stone, collision);
        for (int c = 0; c < 3; c++)
            Box(name + " Cut Stone " + c, new Vector3(x + ((c%2)*.06f),1.6f+c*1.2f,z), new Vector3(.96f,1.18f,.86f), c%2==0?stone:roof, collision);
        Box(name + " Carved Capital", new Vector3(x,5.38f,z), new Vector3(1.5f,.45f,1.2f), stone, collision);
    }

    private static void AncientArch(string name, float x, float z)
    {
        for (int side = -1; side <= 1; side += 2)
            for (int step = 0; step < 3; step++)
                Box(name + " Arch Corbel " + side + " " + step, new Vector3(side * (2.5f - step*.42f), 4.35f + step*.36f, z), new Vector3(1.05f,.55f,1.0f), step == 1 ? roof : stone, false);
        Box(name + " Ancient Lintel", new Vector3(0,5.52f,z), new Vector3(6.8f,.72f,1.15f), roof, false);
        Box(name + " Lintel Crown", new Vector3(0,5.98f,z), new Vector3(5.2f,.26f,1.35f), stone, false);
    }

    private static void BuildBrokenRoof()
    {
        // A broad pitched stone vault makes the main passage feel like an old
        // processional temple, while using a small number of large roof courses.
        for (float z = -9.5f; z <= 29f; z += 5.5f)
        {
            var westSlope = Box("West Vault Stone Course", new Vector3(-8.9f,6.42f,z), new Vector3(18.1f,.68f,5.45f), roof, false);
            westSlope.transform.rotation = Quaternion.Euler(0f,0f,5f);
            var eastSlope = Box("East Vault Stone Course", new Vector3(8.9f,6.42f,z), new Vector3(18.1f,.68f,5.45f), roof, false);
            eastSlope.transform.rotation = Quaternion.Euler(0f,0f,-5f);
        }
        Box("Ancient Vault Ridge Cap", new Vector3(0,7.02f,10.6f), new Vector3(.9f,.42f,45.4f), stone, false);
    }

    private static void BuildSafetyBoundaries()
    {
        // Invisible perimeter colliders keep the player and Titans within the garden and temple grounds.
        foreach (var boundary in new[]
        {
            new object[] { "West Safety Boundary", new Vector3(-60.8f, 4f, 1f), new Vector3(.7f, 10f, 154f) },
            new object[] { "East Safety Boundary", new Vector3(60.8f, 4f, 1f), new Vector3(.7f, 10f, 154f) },
            new object[] { "South Safety Boundary", new Vector3(0f, 4f, -76.8f), new Vector3(122f, 10f, .7f) },
            new object[] { "North Safety Boundary", new Vector3(0f, 4f, 78.8f), new Vector3(122f, 10f, .7f) }
        })
        {
            var barrier = Box((string)boundary[0], (Vector3)boundary[1], (Vector3)boundary[2], stone, true);
            barrier.GetComponent<Renderer>().enabled = false;
        }
    }

    private static void BuildJungleProps()
    {
        Transform props = FindAnyTransform("Temple Props");
        Transform pineTemplate = props != null ? FindChild(props, "Courtyard Pine 1") : null;
        Transform rockTemplate = props != null ? FindChild(props, "Courtyard Rocks 1") : null;
        int treeIndex = 0;
        System.Random random = new System.Random(471);
        for (float z = -75f; z <= -17f; z += 9f)
        {
            float trailCenter = 3.5f * Mathf.Sin((z + 67f) * .075f);
            for (float x = -25f; x <= 25f; x += 8f)
            {
                if (Mathf.Abs(x - trailCenter) < 5.4f) continue;
                AddForestTree(pineTemplate, new Vector3(x + (float)(random.NextDouble() - .5) * 2f, 0f, z + (float)(random.NextDouble() - .5) * 3f), treeIndex++, random);
            }
        }
        for (float z = 38f; z <= 62f; z += 8f)
        {
            foreach (float x in new[] { -24f, -16f, -9f, 9f, 16f, 24f })
                AddForestTree(pineTemplate, new Vector3(x + (float)(random.NextDouble() - .5) * 2f, 0f, z + (float)(random.NextDouble() - .5) * 2f), treeIndex++, random);
        }
        if (rockTemplate != null)
            foreach (var p in new[] { new Vector3(-21, 0, -60), new Vector3(20, 0, -50), new Vector3(-19, 0, -37), new Vector3(22, 0, -26), new Vector3(-20, 0, 45), new Vector3(20, 0, 56) })
            {
                var clone = Object.Instantiate(rockTemplate.gameObject, layout);
                clone.name = "Forest Moss Rock";
                clone.transform.position = p;
                clone.transform.localScale = Vector3.one * .9f;
                foreach (var col in clone.GetComponentsInChildren<Collider>(true)) col.enabled = false;
            }

        // A winding stone trail runs through a broad, walkable forest instead of a straight corridor.
        for (float z = -76f; z <= -14f; z += 2.8f)
        {
            float x = 3.5f * Mathf.Sin((z + 67f) * .075f);
            Box("Forest Path Slab", new Vector3(x, .025f, z), new Vector3(2.2f, .08f, 1.45f), roof, false);
        }
        for (float z = 36f; z <= 61f; z += 3.5f)
            Box("Escape Forest Path Slab", new Vector3(2f * Mathf.Sin(z * .11f), .025f, z), new Vector3(2.4f, .08f, 1.55f), roof, false);

        // Low red-orange flames guide the player through the woods.
        foreach (var p in new[]
        {
            new Vector3(-7f, 2.3f, -60f), new Vector3(8f, 2.3f, -48f),
            new Vector3(-8f, 2.3f, -35f), new Vector3(7f, 2.3f, -21f)
        })
        {
            Box("Torch Post", p, new Vector3(.22f, 2.8f, .22f), roof, false);
            var flame = new GameObject("Torch Glow"); flame.transform.SetParent(layout, false); flame.transform.position = p + Vector3.up * .9f;
            var light = flame.AddComponent<Light>(); light.type = LightType.Point; light.color = new Color(1f, .42f, .12f); light.intensity = 2.6f; light.range = 10f; light.shadows = LightShadows.None;
            flame.AddComponent<TempleOfBoredTitans.Core.TempleTorchFlicker>();
        }
        // Visible wall sconces run down both nave walls and light the vault face.
        foreach (float z in new[] { -8f, -2f, 4f, 10f, 16f, 22f, 29f })
        {
            AddTorchSconce(new Vector3(-17.45f, 3.15f, z), 1f);
            AddTorchSconce(new Vector3(17.45f, 3.15f, z), -1f);
        }
        // Extra interior sconces light both sides of the route and the final
        // treasure room, where the existing torches left the floor too dim.
        foreach (float z in new[] { -4f, 5f, 14f, 22f })
        {
            AddTorchSconce(new Vector3(-9.15f, 3.35f, z), 1f);
            AddTorchSconce(new Vector3(9.15f, 3.35f, z), -1f);
        }
        AddTorchSconce(new Vector3(-6.8f, 3.4f, 29f), 1f);
        AddTorchSconce(new Vector3(6.8f, 3.4f, 29f), -1f);
        AddTorchSconce(new Vector3(-11f, 4.55f, 24.25f), 1f);
        AddTorchSconce(new Vector3(0f, 4.55f, 24.25f), 1f);
        AddTorchSconce(new Vector3(11f, 4.55f, 24.25f), 1f);
        if (props != null) props.gameObject.SetActive(false);
    }

    private static void AddTorchSconce(Vector3 position, float inward)
    {
        Box("Ancient Torch Sconce Bracket", position + Vector3.right * inward * .34f,
            new Vector3(.7f, .16f, .2f), roof, false);
        var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flame.name = "Visible Torch Flame";
        flame.transform.SetParent(layout, false);
        flame.transform.position = position + Vector3.up * .34f;
        flame.transform.localScale = new Vector3(.22f, .42f, .22f);
        var flameMaterial = MakeMaterial("Assets/Environment/Materials/VisibleTorchFlame.mat", null, new Color(1f, .23f, .025f), false);
        if (flameMaterial != null && flameMaterial.HasProperty("_EmissionColor"))
        {
            flameMaterial.EnableKeyword("_EMISSION");
            flameMaterial.SetColor("_EmissionColor", new Color(1f, .14f, .015f) * 3.2f);
        }
        flame.GetComponent<Renderer>().sharedMaterial = flameMaterial;
        Object.DestroyImmediate(flame.GetComponent<Collider>());
        var lightObject = new GameObject("Torch Glow");
        lightObject.transform.SetParent(layout, false);
        lightObject.transform.position = position + Vector3.up * .4f;
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, .72f, .28f);
        light.intensity = 3.7f;
        light.range = 13f;
        light.shadows = LightShadows.None;
        lightObject.AddComponent<TempleOfBoredTitans.Core.TempleTorchFlicker>();
    }

    private static void AddForestTree(Transform template, Vector3 position, int index, System.Random random)
    {
        if (template != null)
        {
            GameObject tree = Object.Instantiate(template.gameObject, layout);
            tree.name = "Forest Pine " + index.ToString("00");
            tree.transform.position = position;
            tree.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
            tree.transform.localScale = new Vector3(1.2f, 2.6f, 1.2f);
            foreach (var collider in tree.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var renderer in tree.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = true;
            }
            tree.isStatic = true;
            return;
        }

        PrimitiveUnder(layout, "Forest Trunk " + index, PrimitiveType.Cylinder, position + Vector3.up * 2f, new Vector3(.36f, 2f, .36f), roof);
        PrimitiveUnder(layout, "Forest Canopy " + index, PrimitiveType.Sphere, position + Vector3.up * 4.5f, new Vector3(2.4f, 4f, 2.4f), grass);
    }

    private static void BuildTemplePillars()
    {
        for (int z = 0; z <= 8; z += 4)
        {
            foreach (int side in new[] { -1, 1 })
            {
                var column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                column.name = "Crypt Nave Ribbed Pillar"; column.transform.SetParent(layout, false);
                column.transform.position = new Vector3(side * 8.8f, 2.65f, z);
                column.transform.localScale = new Vector3(.72f, 2.65f, .72f);
                column.GetComponent<Renderer>().sharedMaterial = roof;
                var collider = column.GetComponent<Collider>(); if (collider != null) Object.DestroyImmediate(collider);
                Box("Ribbed Pillar Capital", new Vector3(side * 8.8f, 5.05f, z), new Vector3(1.25f, .38f, 1.25f), stone, false);
                Box("Ribbed Pillar Tomb Base", new Vector3(side * 8.8f, .3f, z), new Vector3(1.4f, .48f, 1.4f), stone, false);
            }
        }
    }

    private static void BuildCryptDecor()
    {
        string modelRoot = "Assets/ImportedAssets/kenney_graveyard-kit_5.0/Models/FBX format/";
        AddCryptProp(modelRoot + "crypt-small.fbx", "West Chapel Tomb Shrine", new Vector3(-15.8f, 0f, 6f), Vector3.one * .72f, 90f);
        AddCryptProp(modelRoot + "crypt-large.fbx", "East Chapel Tomb Shrine", new Vector3(15.8f, 0f, 6f), Vector3.one * .64f, -90f);
        AddCryptProp(modelRoot + "coffin-old.fbx", "Broken Coffin West Reliquary", new Vector3(-15.4f, .02f, 13.4f), Vector3.one * .72f, 90f);
        AddCryptProp(modelRoot + "coffin-old.fbx", "Broken Coffin East Reliquary", new Vector3(15.4f, .02f, 13.4f), Vector3.one * .72f, -90f);
        AddCryptProp(modelRoot + "altar-stone.fbx", "Nave Sepulchral Altar", new Vector3(0f, .02f, 5.5f), Vector3.one * .85f, 0f);
        AddCryptProp(modelRoot + "pillar-obelisk.fbx", "Ring Crypt Obelisk West", new Vector3(-6.6f, 0f, 13.2f), Vector3.one * .75f, 0f);
        AddCryptProp(modelRoot + "pillar-obelisk.fbx", "Ring Crypt Obelisk East", new Vector3(6.6f, 0f, 13.2f), Vector3.one * .75f, 0f);
        AddCryptProp(modelRoot + "gravestone-broken.fbx", "Vestibule Fallen Grave Marker", new Vector3(-5f, 0f, -8f), Vector3.one * .8f, 17f);
        AddCryptProp(modelRoot + "gravestone-cross-large.fbx", "Sanctum Crooked Marker", new Vector3(7f, 0f, 30f), Vector3.one * .72f, -13f);
        AddCryptProp(modelRoot + "fire-basket.fbx", "Sanctum Funeral Brazier", new Vector3(0f, 0f, 30f), Vector3.one * .75f, 0f);
    }

    private static void AddCryptProp(string path, string objectName, Vector3 position, Vector3 scale, float yaw)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (model == null) return;
        GameObject instance = Object.Instantiate(model, layout);
        instance.name = objectName;
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        instance.transform.localScale = scale;
    }

    private static void BuildFiveStatues()
    {
        var root = new GameObject("Five Forgotten Titan Statues").transform; root.SetParent(layout, false);
        string[] sources = { "Dragon Visual", "Skeleton Visual", "Slime Visual", "Bat Visual", "Orc Visual" };
        string[] labels = { "Ashen Dragon", "Wailing Skeleton", "Plague Brute", "Winged Reaper", "Stone Colossus" };
        var eyes = new List<Light>();
        for (int i = 0; i < sources.Length; i++)
        {
            var source = FindAnyTransform(sources[i]);
            float x = (i - 2) * 3.5f;
            BoxUnder(root, labels[i] + " Statue Plinth", new Vector3(x, .28f, 5.6f), new Vector3(2.5f, .56f, 2.1f), stone, true);
            if (source != null)
            {
                var copy = Object.Instantiate(source.gameObject, root);
                copy.name = labels[i] + " Statue Model";
                copy.transform.position = new Vector3(x, .56f, 5.6f);
                // The sanctuary entrance is on the negative Z side.
                copy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                foreach (var animator in copy.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                foreach (var col in copy.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
                foreach (var renderer in copy.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    var statueMaterials = renderer.sharedMaterials;
                    for (int materialIndex = 0; materialIndex < statueMaterials.Length; materialIndex++) statueMaterials[materialIndex] = stone;
                    renderer.sharedMaterials = statueMaterials;
                }
                Bounds bounds = new Bounds(copy.transform.position, new Vector3(1.4f, 2.8f, 1.4f));
                foreach (var renderer in copy.GetComponentsInChildren<Renderer>(true)) bounds.Encapsulate(renderer.bounds);
                var solid = copy.AddComponent<BoxCollider>();
                solid.center = copy.transform.InverseTransformPoint(bounds.center);
                solid.size = new Vector3(Mathf.Max(1.2f, bounds.size.x), Mathf.Max(2.1f, bounds.size.y), Mathf.Max(1.2f, bounds.size.z));
            }
            var eye = new GameObject(labels[i] + " Awakening Glow"); eye.transform.SetParent(root, false); eye.transform.position = new Vector3(x, 2f, 4.9f);
            var light = eye.AddComponent<Light>(); light.type = LightType.Point; light.color = new Color(.55f, .72f, 1f); light.intensity = 0f; light.range = 4.5f; light.shadows = LightShadows.None; light.enabled = false; eyes.Add(light);
        }
        var trigger = new GameObject("Temple Entrance Story Trigger"); trigger.transform.SetParent(layout, false); trigger.transform.position = new Vector3(0, 1.2f, -11f);
        var collider = trigger.AddComponent<BoxCollider>(); collider.isTrigger = true; collider.size = new Vector3(7f, 2.6f, 1.3f);
        var intro = trigger.AddComponent<TempleStoryboardIntro>();
        var so = new SerializedObject(intro);
        so.FindProperty("statueEyes").arraySize = eyes.Count;
        for (int i = 0; i < eyes.Count; i++) so.FindProperty("statueEyes").GetArrayElementAtIndex(i).objectReferenceValue = eyes[i];
        so.FindProperty("entranceDoor").objectReferenceValue = BuildEntranceDoor();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BuildTreasureDoorway()
    {
        // Three visually equal tomb doors share one wall. The player discovers
        // which one the key opens by testing each door.
        foreach (var wall in new[] { new Vector3(-14.6f, 2.55f, 25.1f), new Vector3(-4f, 2.55f, 25.1f), new Vector3(4f, 2.55f, 25.1f), new Vector3(14.6f, 2.55f, 25.1f) })
            Box("Vault Door Wall Masonry", wall, new Vector3(Mathf.Abs(wall.x) > 10f ? 8.35f : 3f, 5.1f, 1.05f), stone, true);

        float[] centers = { -8f, 0f, 8f };
        for (int i = 0; i < centers.Length; i++)
        {
            float x = centers[i];
            AncientPier("Vault Door " + (i + 1) + " West Jamb", x - 2.5f, 25.1f, true);
            AncientPier("Vault Door " + (i + 1) + " East Jamb", x + 2.5f, 25.1f, true);
            AncientArch("Vault Door " + (i + 1), x, 25.1f);
            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Vault Door " + (i + 1) + (i == 1 ? " Central Key Door" : " Sealed Side Door");
            panel.transform.SetParent(layout, false);
            panel.transform.position = new Vector3(x, 2.25f, 25.1f);
            panel.transform.localScale = new Vector3(4.75f, 4.35f, .58f);
            panel.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Assets/Environment/Materials/VaultDoorFace" + i + ".mat", null,
                new Color(.28f, .21f, .13f), false);
            // Raised bronze-colored borders make all three separate door leaves
            // easy to distinguish from the surrounding masonry at a distance.
            var trimMaterial = MakeMaterial("Assets/Environment/Materials/VaultDoorTrim.mat", null, new Color(.48f, .27f, .075f), false);
            BoxUnder(panel.transform, "Raised Vault Door Left Border", new Vector3(-.72f, 0f, -.31f), new Vector3(.12f, 3.7f, .08f), trimMaterial, false);
            BoxUnder(panel.transform, "Raised Vault Door Right Border", new Vector3(.72f, 0f, -.31f), new Vector3(.12f, 3.7f, .08f), trimMaterial, false);
            BoxUnder(panel.transform, "Raised Vault Door Top Border", new Vector3(0f, 1.8f, -.31f), new Vector3(1.55f, .12f, .08f), trimMaterial, false);
            AddDoorLabel(panel.transform, "VAULT " + (i + 1), false);
            var door = panel.AddComponent<TempleOfBoredTitans.Environment.TempleVaultDoor>();
            var doorData = new SerializedObject(door);
            doorData.FindProperty("doorIndex").intValue = i;
            doorData.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void AddDoorLabel(Transform door, string label, bool treasureDoor)
    {
        var sign = new GameObject("Vault Door Carved Name");
        sign.transform.SetParent(door, false);
        sign.transform.localPosition = new Vector3(0f, 1.28f, -.38f);
        sign.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        // Compensate for the door leaf's large transform scale so the labels
        // stay neatly inside each panel rather than stretching across the wall.
        sign.transform.localScale = new Vector3(-.05f, .055f, .12f);
        var text = sign.AddComponent<TextMesh>();
        text.text = label;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 48;
        text.characterSize = 1f;
        text.color = treasureDoor ? new Color(1f, .75f, .25f) : new Color(.78f, .68f, .5f);
        text.fontStyle = FontStyle.Bold;
    }

    private static Transform BuildEntranceDoor()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = "Sealing Entrance Door"; go.transform.SetParent(layout, false);
        go.transform.position = new Vector3(0, 5.3f, -12.25f); go.transform.localScale = new Vector3(5.8f, 4.1f, .55f);
        go.GetComponent<Renderer>().sharedMaterial = stone; go.isStatic = false; return go.transform;
    }

    private static void PlaceGameplay()
    {
        // Begin outside in the forest. No cabin, starter room, or enclosing walls.
        var checkpoint = new GameObject("Initial Forest Checkpoint").transform;
        checkpoint.SetParent(layout, false); checkpoint.position = new Vector3(0f, 0f, -16.5f);
        var player = GameObject.Find("Treasure Hunter Player");
        if (player != null)
        {
            player.transform.position = new Vector3(0f, 0f, -16.5f); player.transform.rotation = Quaternion.identity;
            var health = player.GetComponent<PlayerHealth>();
            if (health != null) { var so = new SerializedObject(health); so.FindProperty("maximumHealth").intValue = 110; so.FindProperty("startingLives").intValue = 1; so.FindProperty("checkpointTransform").objectReferenceValue = checkpoint; so.ApplyModifiedPropertiesWithoutUndo(); }
            var weapon = player.GetComponent<PlayerWeapon>() ?? player.AddComponent<PlayerWeapon>();
            // Use the imported Lumberjack character as the visible third-person hunter.
            // The pack's WeaponSlot is authored on the character's right hand.
            var avatarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FreeCharacter/Prefabs/Lumberjack1.prefab");
            if (avatarPrefab != null)
            {
                Transform oldAvatar = player.transform.Find("Treasure Hunter Avatar");
                if (oldAvatar != null) Object.DestroyImmediate(oldAvatar.gameObject);
                var avatarObject = (GameObject)PrefabUtility.InstantiatePrefab(avatarPrefab, player.scene);
                avatarObject.name = "Treasure Hunter Avatar";
                avatarObject.transform.SetParent(player.transform, false);
                avatarObject.transform.localPosition = Vector3.zero;
                avatarObject.transform.localRotation = Quaternion.identity;
                avatarObject.transform.localScale = Vector3.one;
                foreach (var animator in avatarObject.GetComponentsInChildren<Animator>(true)) animator.applyRootMotion = false;
                var legacyAnimation = avatarObject.GetComponentInChildren<Animation>(true);
                AnimationClip idleClip = null, walkClip = null;
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath("Assets/FreeCharacter/FreeCharacter_Animations.FBX"))
                {
                    var candidate = clip as AnimationClip;
                    if (candidate == null) continue;
                    if (candidate.name.Equals("Idle", System.StringComparison.OrdinalIgnoreCase)) idleClip = candidate;
                    else if (candidate.name.Equals("Walk", System.StringComparison.OrdinalIgnoreCase)) walkClip = candidate;
                }
                AnimationClip runClip = walkClip;
                if (legacyAnimation != null)
                {
                    legacyAnimation.playAutomatically = false;
                    foreach (var clip in new[] { idleClip, walkClip, runClip })
                        if (clip != null && legacyAnimation.GetClip(clip.name) == null) legacyAnimation.AddClip(clip, clip.name);
                }
                var avatarMotion = avatarObject.GetComponent<PlayerAvatarLocomotion>() ?? avatarObject.AddComponent<PlayerAvatarLocomotion>();
                avatarMotion.Configure(legacyAnimation, idleClip != null ? idleClip.name : "Idle", walkClip != null ? walkClip.name : "Walk", runClip != null ? runClip.name : "Walk");
                ConvertAvatarMaterialsToUrp(avatarObject);
                foreach (var child in avatarObject.GetComponentsInChildren<Transform>(true))
                    if (child.name.Contains("NPC_Tools_Axe_004")) child.gameObject.SetActive(false);
                Transform weaponSlot = FindChild(avatarObject.transform, "WeaponSlot") ?? FindChild(avatarObject.transform, "CATRHand");
                var weaponComponentData = new SerializedObject(weapon);
                weaponComponentData.FindProperty("weaponMount").objectReferenceValue = weaponSlot;
                weaponComponentData.ApplyModifiedPropertiesWithoutUndo();

                // Show the character and its hand-held weapon from a follow camera.
                Transform cameraPivot = FindChild(player.transform, "Camera Pivot");
                if (cameraPivot != null) cameraPivot.localPosition = new Vector3(0f, 1.65f, -3.6f);
            }
            var weaponData = new SerializedObject(weapon);
            weaponData.FindProperty("weaponTier").intValue = 0;
            Camera playerCamera = player.GetComponentInChildren<Camera>(true);
            if (playerCamera != null) weaponData.FindProperty("viewCamera").objectReferenceValue = playerCamera.transform;
            weaponData.FindProperty("weaponModelRotationOffset").vector3Value = new Vector3(0f, -90f, 0f);
            string[] weaponPaths =
            {
                "Assets/PolyOne/Free Gun/Prefabs/FN_Five_Seven.prefab",
                "Assets/PolyOne/Free Gun/Prefabs/SM_HK_MP5.prefab",
                "Assets/PolyOne/Free Gun/Prefabs/SM_Ak47.prefab",
                "Assets/PolyOne/Free Gun/Prefabs/SM_M16A1.prefab",
                "Assets/PolyOne/Free Gun/Prefabs/SM_Barrett_M82A1.prefab"
            };
            var weaponModels = weaponData.FindProperty("weaponModelPrefabs");
            weaponModels.arraySize = 1;
            weaponModels.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(weaponPaths[0]);
            weaponData.ApplyModifiedPropertiesWithoutUndo();
        }

        foreach (string symbol in new[] { "Titan Symbol 1 - Entrance", "Titan Symbol 2 - Courtyard", "Titan Symbol 3 - Hall", "Titan Symbol 4 - Guardian", "Titan Symbol 5 - Puzzle" })
        { var oldSymbol = FindAnyTransform(symbol); if (oldSymbol != null) Object.DestroyImmediate(oldSymbol.gameObject); }
        foreach (var collectible in Object.FindObjectsByType<TempleOfBoredTitans.Collectibles.CollectibleSymbol>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (collectible != null && collectible.gameObject.scene.IsValid()) Object.DestroyImmediate(collectible.gameObject);
        string[] names = { "Angry Titan - Dragon", "Crying Monk", "Lazy Guardian", "Dancing Titan", "Echo Archivist" };
        string[] display = { "Ashen Dragon", "Wailing Skeleton", "Plague Brute", "Winged Reaper", "Stone Colossus" };
        Vector3[] positions = { new Vector3(0,0,-7f), new Vector3(-13.5f,0,2.5f), new Vector3(13.5f,0,2.5f), new Vector3(-13.5f,0,13f), new Vector3(0,0,29.15f) };
        float[] healthValues = { 220, 280, 350, 430, 520 };
        TitanBossHealth[] bosses = new TitanBossHealth[5];
        for (int i = 0; i < names.Length; i++)
        {
            GameObject enemy = FindGameplayObject(names[i]);
            if (enemy == null) enemy = FindGameplayObject(display[i]); // Supports retry after an interrupted rebuild renamed it.
            if (enemy == null) continue;
            enemy.name = display[i]; enemy.transform.position = positions[i];
            foreach (var old in enemy.GetComponents<MonoBehaviour>())
            {
                if (old == null) continue; // Imported scene objects can contain missing-script slots.
                string typeName = old.GetType().Name;
                if (typeName.Contains("CryingMonk") || typeName.Contains("LazyGuardian") || typeName.Contains("DancingTitan") || typeName.Contains("NoiseMaker") || typeName.Contains("Banter")) Object.DestroyImmediate(old);
            }
            var ai = enemy.GetComponent<TitanAI>() ?? enemy.AddComponent<TitanAI>();
            var aiData = new SerializedObject(ai);
            aiData.FindProperty("detectionDistance").floatValue = 34f;
            aiData.FindProperty("fieldOfView").floatValue = 180f;
            aiData.FindProperty("patrolSpeed").floatValue = 1.5f + i * .08f;
            aiData.FindProperty("chaseSpeed").floatValue = 2.7f + i * .12f;
            aiData.FindProperty("attackCooldown").floatValue = 3.5f;
            aiData.ApplyModifiedPropertiesWithoutUndo();
            var boss = enemy.GetComponent<TitanBossHealth>() ?? enemy.AddComponent<TitanBossHealth>();
            var bossData = new SerializedObject(boss);
            bossData.FindProperty("stageIndex").intValue = i;
            bossData.FindProperty("displayName").stringValue = display[i];
            bossData.FindProperty("maxHealth").intValue = (int)healthValues[i];
            bossData.ApplyModifiedPropertiesWithoutUndo();
            var special = enemy.GetComponent<TitanSpecialAbility>() ?? enemy.AddComponent<TitanSpecialAbility>();
            var abilityData = new SerializedObject(special);
            abilityData.FindProperty("ability").enumValueIndex = i;
            abilityData.FindProperty("cooldown").floatValue = i == 1 ? 5f : 3.8f;
            abilityData.FindProperty("damage").intValue = 8 + i;
            abilityData.ApplyModifiedPropertiesWithoutUndo();
            foreach (var col in enemy.GetComponentsInChildren<Collider>(true)) col.enabled = true;
            bosses[i] = boss;
            enemy.SetActive(false);
        }
        var manager = GameObject.Find("Game Manager");
        if (manager != null)
        {
            var director = manager.GetComponent<TitanEncounterDirector>() ?? manager.AddComponent<TitanEncounterDirector>();
            var data = new SerializedObject(director); var arr = data.FindProperty("bosses"); arr.arraySize = bosses.Length;
            for (int i = 0; i < bosses.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = bosses[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        var noise = GameObject.Find("Guardian Noise Maker"); if (noise != null) noise.SetActive(false);
        var exit = GameObject.Find("Temple Escape Trigger");
        if (exit == null) exit = new GameObject("Temple Escape Trigger");
        exit.transform.SetParent(layout, false); exit.transform.position = new Vector3(0,1f,38.5f);
        var trigger = exit.GetComponent<BoxCollider>();
        if (trigger == null) trigger = exit.AddComponent<BoxCollider>();
        trigger.isTrigger = true; trigger.size = new Vector3(12f,3f,2f);
        if (exit.GetComponent<TempleEscapeExit>() == null) exit.AddComponent<TempleEscapeExit>();
        var gate = GameObject.Find("Final Chamber Gate");
        if (gate != null)
        {
            // Place the escape gate in the last treasure room, beside the chest.
            // This prefab's pivot and box collider begin at floor level. Keeping
            // its root at y=1.9 left a walk-under gap beneath the closed door.
            gate.transform.position = new Vector3(0,0f,32.25f);
            gate.transform.localScale = new Vector3(6.5f,3.8f,.8f);
            gate.isStatic = false;
            // Replace the imported barred gate mesh with the same kind of solid
            // stone leaf used by the entrance door. Its floor-level pivot and
            // full-width collider make it visibly closed and impossible to walk under.
            foreach (var renderer in gate.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            var oldLeaf = gate.transform.Find("Closed Escape Stone Panel");
            if (oldLeaf != null) Object.DestroyImmediate(oldLeaf.gameObject);
            var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = "Closed Escape Stone Panel";
            leaf.transform.SetParent(gate.transform, false);
            leaf.transform.localPosition = new Vector3(0f, .5f, 0f);
            leaf.transform.localRotation = Quaternion.identity;
            leaf.transform.localScale = Vector3.one;
            leaf.GetComponent<Renderer>().sharedMaterial = stone;
            Object.DestroyImmediate(leaf.GetComponent<Collider>());
            var exitDoor = gate.GetComponent<TempleOfBoredTitans.Environment.DoorController>();
            if (exitDoor == null) exitDoor = gate.AddComponent<TempleOfBoredTitans.Environment.DoorController>();
            var doorData = new SerializedObject(exitDoor);
            doorData.FindProperty("closedPosition").vector3Value = gate.transform.position;
            doorData.FindProperty("openHeight").floatValue = 4.6f;
            doorData.ApplyModifiedPropertiesWithoutUndo();
        }
        BuildTreasureChest();
        Set("Courtyard Moon Glow", new Vector3(0,18,-43), Quaternion.identity);
        Set("Final Chamber Rune Light", new Vector3(0,4.2f,29), Quaternion.identity);
    }

    private static void BuildTreasureChest()
    {
        var oldChest = FindAnyTransform("Sealed Treasure Chest") ?? FindAnyTransform("Treasure Vault Chest");
        if (oldChest != null) Object.DestroyImmediate(oldChest.gameObject);
        var root = new GameObject("Sealed Treasure Chest");
        root.transform.SetParent(layout, false);
        // Keep the hoard inside the final sanctum's open center, easy to spot
        // from the doorway and reachable without squeezing around the wall.
        root.transform.position = new Vector3(0f, .08f, 29.5f);
        root.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
        var trigger = root.AddComponent<BoxCollider>();
        trigger.size = new Vector3(2.8f, 2f, 2.5f);
        var interactable = root.AddComponent<TempleOfBoredTitans.Interaction.TreasureChestInteractable>();
        // The chest pack uses legacy shaders that appear magenta in URP. Replace
        // them with project URP/Lit materials and keep the metal parts gold.
        var wood = MakeMaterial("Assets/Environment/Materials/VaultChestWood.mat", null, new Color(.24f, .09f, .028f), false);
        var metal = MakeMaterial("Assets/Environment/Materials/VaultChestGold.mat", null, new Color(.78f, .43f, .075f), false);
        if (metal != null)
        {
            if (metal.HasProperty("_Metallic")) metal.SetFloat("_Metallic", .58f);
            if (metal.HasProperty("_Smoothness")) metal.SetFloat("_Smoothness", .38f);
            if (metal.HasProperty("_EmissionColor"))
            {
                metal.EnableKeyword("_EMISSION");
                metal.SetColor("_EmissionColor", new Color(.26f, .12f, .018f));
            }
        }
        var chestModel = AddChestModel(root.transform, "Assets/Sat Productions/B-01/Free Treasure Chest/Prefabs/Gold Chest.prefab", "Old Gold Treasure Chest", Vector3.zero, Vector3.one * 1.1f);
        ApplyChestPalette(chestModel, wood, metal);
        var key = AddChestModel(root.transform, "Assets/ImportedAssets/fantasy_props_megakitstandard/Exports/FBX/Key_Gold.fbx", "Treasure Key Reward", new Vector3(0f, 1.75f, -.75f), Vector3.one * .8f);
        var gold = AddChestModel(root.transform, "Assets/ImportedAssets/fantasy_props_megakitstandard/Exports/FBX/Coin_Pile_2.fbx", "Full Gold Treasure", new Vector3(0f, .9f, -.45f), Vector3.one * .9f);
        ApplyChestPalette(key, metal, metal);
        ApplyChestPalette(gold, metal, metal);
        if (key != null) key.SetActive(false);
        if (gold != null) gold.SetActive(false);
        var markerMaterial = MakeMaterial("Assets/Environment/Materials/VaultMarkerGold.mat", null, new Color(1f, .48f, .035f), false);
        if (markerMaterial != null && markerMaterial.HasProperty("_EmissionColor"))
        {
            markerMaterial.EnableKeyword("_EMISSION");
            markerMaterial.SetColor("_EmissionColor", new Color(1f, .32f, .015f) * 3f);
        }
        var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beam.name = "Visible Treasure Beacon Beam";
        beam.transform.SetParent(root.transform, false);
        beam.transform.localPosition = new Vector3(0f, 3.55f, 0f);
        beam.transform.localScale = new Vector3(.12f, 1.65f, .12f);
        beam.GetComponent<Renderer>().sharedMaterial = markerMaterial;
        Object.DestroyImmediate(beam.GetComponent<Collider>());
        var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.name = "Treasure Beacon Crown";
        crown.transform.SetParent(root.transform, false);
        crown.transform.localPosition = new Vector3(0f, 5.35f, 0f);
        crown.transform.localScale = Vector3.one * .62f;
        crown.GetComponent<Renderer>().sharedMaterial = markerMaterial;
        Object.DestroyImmediate(crown.GetComponent<Collider>());
        var beacon = new GameObject("Treasure Vault Gold Beacon");
        beacon.transform.SetParent(root.transform, false);
        beacon.transform.localPosition = new Vector3(0f, 2.8f, 0f);
        var beaconLight = beacon.AddComponent<Light>();
        beaconLight.type = LightType.Point;
        beaconLight.color = new Color(1f, .62f, .16f);
        beaconLight.intensity = 2.2f;
        beaconLight.range = 11f;
        beaconLight.shadows = LightShadows.None;
        var so = new SerializedObject(interactable);
        so.FindProperty("keyVisual").objectReferenceValue = key;
        so.FindProperty("goldVisual").objectReferenceValue = gold;
        so.FindProperty("goldBeacon").objectReferenceValue = beacon;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ApplyChestPalette(GameObject model, Material wood, Material metal)
    {
        if (model == null) return;
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            string part = renderer.gameObject.name;
            bool isMetal = part.IndexOf("Lock", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                part.IndexOf("Tongue", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                part.IndexOf("Treasure", System.StringComparison.OrdinalIgnoreCase) >= 0;
            Material replacement = isMetal ? metal : wood;
            if (replacement == null) continue;
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = replacement;
            renderer.sharedMaterials = materials;
        }
    }

    private static GameObject AddChestModel(Transform parent, string assetPath, string name, Vector3 localPosition, Vector3 scale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null) return null;
        GameObject instance = Object.Instantiate(prefab, parent);
        instance.name = name;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = scale;
        foreach (var collider in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
        return instance;
    }

    private static GameObject CreateSigil(int index, string room, string titan, Vector3 position)
    {
        string path = "Assets/Environment/Materials/TitanSigil_" + (index + 1) + ".mat";
        Material sigilMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (sigilMaterial == null)
        {
            sigilMaterial = new Material(stone) { name = "Titan Sigil " + (index + 1) + " Glow" };
            AssetDatabase.CreateAsset(sigilMaterial, path);
        }
        Color[] colors = { new Color(.9f,.22f,.07f), new Color(.08f,.58f,.95f), new Color(.25f,.9f,.38f), new Color(.7f,.12f,.95f), new Color(1f,.62f,.08f) };
        sigilMaterial.color = colors[index];
        if (sigilMaterial.HasProperty("_EmissionColor"))
        {
            sigilMaterial.EnableKeyword("_EMISSION");
            sigilMaterial.SetColor("_EmissionColor", colors[index] * 1.6f);
        }
        GameObject sigil = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sigil.name = "Titan Sigil " + (index + 1) + " - " + room;
        sigil.transform.SetParent(layout, false);
        sigil.transform.position = position;
        sigil.transform.localScale = Vector3.one * .72f;
        sigil.transform.rotation = Quaternion.Euler(45f, 45f, 0f);
        sigil.GetComponent<Renderer>().sharedMaterial = sigilMaterial;
        var collectible = sigil.AddComponent<TempleOfBoredTitans.Collectibles.CollectibleSymbol>();
        collectible.Configure(index, titan);
        var glowObject = new GameObject("Sigil Glow"); glowObject.transform.SetParent(sigil.transform, false);
        var glow = glowObject.AddComponent<Light>(); glow.type = LightType.Point; glow.color = colors[index]; glow.intensity = .55f; glow.range = 3f; glow.shadows = LightShadows.None;
        sigil.SetActive(false);
        return sigil;
    }

    private static void ApplyHorrorAtmosphere()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.12f,.095f,.066f);
        RenderSettings.fog = true; RenderSettings.fogColor = new Color(.025f,.021f,.014f); RenderSettings.fogDensity = .0038f;
        foreach (var light in Object.FindObjectsByType<Light>())
        {
            if (light.type == LightType.Directional) { light.intensity = .38f; light.color = new Color(.58f,.50f,.37f); light.shadows = LightShadows.Soft; }
            else if (light.name.Contains("Moon Glow")) { light.color = new Color(.12f,.18f,.42f); light.intensity = .6f; }
        }
    }

    private static void BuildEntrySequence()
    {
        // Raise the original entrance wall position so the first walk into the temple is visibly open.
        var door = GameObject.Find("Sealing Entrance Door"); if (door != null) { door.transform.position = new Vector3(0, 5.3f, -12.25f); door.isStatic = false; }
    }

    private static void PrimitiveUnder(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localScale = size;
        var renderer = go.GetComponent<Renderer>(); if (renderer != null) renderer.sharedMaterial = material;
        var collider = go.GetComponent<Collider>(); if (collider != null) Object.DestroyImmediate(collider);
    }

    private static GameObject Box(string name, Vector3 pos, Vector3 size, Material mat, bool collision)
    {
        return BoxUnder(layout, name, pos, size, mat, collision);
    }

    private static GameObject BoxUnder(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool collision)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false); go.transform.position = pos; go.transform.localScale = size;
        var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = mat; renderer.shadowCastingMode = name.Contains("Roof") ? ShadowCastingMode.Off : ShadowCastingMode.On;
        if (!collision) { var collider = go.GetComponent<Collider>(); if (collider != null) Object.DestroyImmediate(collider); }
        go.isStatic = true;
        return go;
    }

    private static Transform FindChild(Transform parent, string name)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name) return child;
            Transform nested = FindChild(child, name);
            if (nested != null) return nested;
        }
        return null;
    }

    private static Transform FindAnyTransform(string name)
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>()) if (t.gameObject.scene.IsValid() && t.name == name) return t;
        return null;
    }

    private static GameObject FindGameplayObject(string name)
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t == null || !t.gameObject.scene.IsValid() || t.name != name) continue;
            bool hiddenByParent = false;
            for (Transform parent = t.parent; parent != null; parent = parent.parent)
            {
                if (!parent.gameObject.activeInHierarchy) { hiddenByParent = true; break; }
            }
            if (!hiddenByParent) return t.gameObject;
        }
        return null;
    }

    private static void Set(string name, Vector3 position, Quaternion rotation)
    {
        var g = GameObject.Find(name); if (g == null) return; g.transform.position = position; g.transform.rotation = rotation;
    }
}










