using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// HW1 upgrade - rebuilds the village using real textured assets from the
// "RPG Poly Pack - Lite" (Assets/RPGPP_LT). The river/bridge stay simple
// primitives (no such asset in this pack); everything else - houses, fences,
// paths, market props, trees/bushes/rocks - are real prefabs.
// NOTE: the real building prefabs are large (roughly 8-12 units per side),
// much bigger than the old primitive houses, so this layout uses generous
// spacing to avoid overlap.
//   Menu:  Tools > Fairytale Village (Real Assets) > Build
public static class RealVillageBuilder
{
    const string P = "Assets/RPGPP_LT/Prefabs/";
    const float RiverLen = 100f, RiverHalf = 3.2f;
    const float HouseOffset = 22f;   // distance from river centerline to a house row (pushed back to de-clutter)
    const float PathOffset = 10f;    // distance from river centerline to the footpath

    // House z-slots per bank, staggered across the river and generously spaced so
    // the village reads as open, not crowded. Shared by BuildHouses + BeautifyVillage.
    static readonly float[] LeftZ = { -44f, -22f, 0f, 22f, 44f };
    static readonly float[] RightZ = { -33f, -11f, 11f, 33f };
    static float RiverX(float z) => 3.0f * Mathf.Sin(z * 0.05f) + 0.8f * Mathf.Sin(z * 0.11f);

    static Transform s_root;
    static readonly Dictionary<string, GameObject> s_cache = new Dictionary<string, GameObject>();

    static GameObject Prefab(string relPath)
    {
        if (s_cache.TryGetValue(relPath, out var g)) return g;
        g = AssetDatabase.LoadAssetAtPath<GameObject>(P + relPath);
        if (g == null) Debug.LogError("Missing prefab: " + P + relPath);
        s_cache[relPath] = g;
        return g;
    }

    static GameObject Spawn(string relPath, Vector3 pos, float yaw, Transform parent, float scale = 1f)
    {
        var prefab = Prefab(relPath);
        if (prefab == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        if (scale != 1f) go.transform.localScale = Vector3.one * scale;
        return go;
    }

    static float J(float r) => Random.Range(-r, r);

    [MenuItem("Tools/Fairytale Village (Real Assets)/Build")]
    public static void Build()
    {
        Random.InitState(11);
        s_cache.Clear();

        var existing = GameObject.Find("Village");
        if (existing != null) Object.DestroyImmediate(existing);
        s_root = new GameObject("Village").transform;

        BuildGround();
        BuildRiver();
        BuildPaths();
        BuildBridge();
        BuildHouses();
        BuildFencesAndProps();
        BuildMarket();
        BuildNature();
        BeautifyVillage();
        PlaceCamera();

        Selection.activeGameObject = s_root.gameObject;
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Real-asset village built. Groups: " + s_root.childCount);
    }

    [MenuItem("Tools/Fairytale Village (Real Assets)/Debug Log House Bounds")]
    public static void DebugBounds()
    {
        s_cache.Clear();
        var tmp = new GameObject("__BoundsProbe").transform;
        foreach (var v in new[] { "01", "02", "03", "04", "05" })
        {
            var go = Spawn("Buildings/Bld_closed/rpgpp_lt_building_" + v + ".prefab", Vector3.zero, 0, tmp);
            if (go == null) continue;
            var rends = go.GetComponentsInChildren<Renderer>();
            Bounds b = rends.Length > 0 ? rends[0].bounds : new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in rends) b.Encapsulate(r.bounds);
            Debug.Log("building_" + v + " size = " + b.size);
        }
        Object.DestroyImmediate(tmp.gameObject);
    }

    [MenuItem("Tools/Fairytale Village (Real Assets)/Debug Scan Near Pub")]
    public static void DebugScanNearPub()
    {
        var all = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        int count = 0;
        foreach (var r in all)
        {
            Vector3 c = r.bounds.center;
            float d = Vector2.Distance(new Vector2(c.x, c.z), PubCenter);
            if (d < 30f)
            {
                Debug.Log(r.gameObject.name + " | world pos=" + r.transform.position + " | bounds.center=" + c + " size=" + r.bounds.size + " | dist=" + d, r.gameObject);
                count++;
            }
        }
        Debug.Log("Scan complete: " + count + " renderers within 30 units of pub center.");
    }

    [MenuItem("Tools/Fairytale Village (Real Assets)/Debug Find Huge Objects")]
    public static void DebugFindHuge()
    {
        var all = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        var list = new List<Renderer>(all);
        list.Sort((a, b) => b.bounds.size.magnitude.CompareTo(a.bounds.size.magnitude));
        int shown = 0;
        foreach (var r in list)
        {
            if (r.bounds.size.magnitude < 10f) continue;
            Debug.Log("HUGE: " + r.gameObject.name + " | transform.pos=" + r.transform.position +
                " | transform.lossyScale=" + r.transform.lossyScale + " | bounds.center=" + r.bounds.center +
                " size=" + r.bounds.size, r.gameObject);
            shown++;
            if (shown > 25) break;
        }
        Debug.Log("Huge-object scan complete. Shown: " + shown);
    }

    [MenuItem("Tools/Fairytale Village (Real Assets)/Clear")]
    public static void Clear()
    {
        var e = GameObject.Find("Village");
        if (e != null) Object.DestroyImmediate(e);
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
    }

    // ---- primitive helpers (only for ground/river/bridge - no such assets in the pack) ----

    static Shader Lit() { var s = Shader.Find("Universal Render Pipeline/Lit"); return s != null ? s : Shader.Find("Standard"); }
    static Material Mat(Color c) { var m = new Material(Lit()); m.color = c; m.SetColor("_BaseColor", c); return m; }
    static GameObject Box(string n, Vector3 p, Vector3 s, Color c, Transform par, Vector3 e = default)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = n; g.transform.SetParent(par, false);
        g.transform.localScale = s; g.transform.localPosition = p; g.transform.localRotation = Quaternion.Euler(e);
        g.GetComponent<Renderer>().sharedMaterial = Mat(c); return g;
    }

    static void BuildGround()
    {
        var pl = GameObject.CreatePrimitive(PrimitiveType.Plane);
        pl.name = "Ground"; pl.transform.SetParent(s_root, false);
        pl.transform.localScale = new Vector3(13, 1, 13); // ~130 x 130
        pl.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.42f, 0.53f, 0.30f));
    }

    static void BuildRiver()
    {
        var r = new GameObject("River").transform; r.SetParent(s_root, false);
        Color water = new Color(0.22f, 0.46f, 0.62f), mud = new Color(0.44f, 0.36f, 0.25f);
        float step = 3f;
        for (float z = -RiverLen / 2f; z < RiverLen / 2f; z += step)
        {
            float x0 = RiverX(z), x1 = RiverX(z + step);
            Vector3 c = new Vector3((x0 + x1) / 2f, 0.02f, z + step / 2f);
            float yaw = Mathf.Atan2(x1 - x0, step) * Mathf.Rad2Deg;
            float len = Mathf.Sqrt(step * step + (x1 - x0) * (x1 - x0)) + 0.6f;
            Box("Bank", new Vector3(c.x, 0.015f, c.z), new Vector3(RiverHalf * 2f + 1.4f, 0.03f, len), mud, r, new Vector3(0, yaw, 0));
            Box("Water", c, new Vector3(RiverHalf * 2f, 0.05f, len), water, r, new Vector3(0, yaw, 0));
        }
    }

    static void BuildPaths()
    {
        var pth = new GameObject("Paths").transform; pth.SetParent(s_root, false);
        float step = 4.2f;
        for (float z = -RiverLen / 2f + 3; z < RiverLen / 2f - 3; z += step)
            foreach (float side in new float[] { -1f, 1f })
            {
                float x = RiverX(z) + side * (PathOffset + J(0.6f));
                Spawn("Nature/Terrain/rpgpp_lt_terrain_path_01a.prefab", new Vector3(x, 0.02f, z), J(12f) + (side > 0 ? 0 : 180), pth);
            }
    }

    static void BuildBridge()
    {
        var b = new GameObject("Bridge").transform; b.SetParent(s_root, false);
        Color wood = new Color(0.45f, 0.30f, 0.18f);
        Box("Deck", new Vector3(0, 0.5f, 0), new Vector3(RiverHalf * 2f + 3.5f, 0.22f, 2.8f), wood, b);
        Box("RailA", new Vector3(0, 0.9f, 1.3f), new Vector3(RiverHalf * 2f + 3.5f, 0.5f, 0.18f), wood, b);
        Box("RailB", new Vector3(0, 0.9f, -1.3f), new Vector3(RiverHalf * 2f + 3.5f, 0.5f, 0.18f), wood, b);
    }

    // ---- houses (real building prefabs, generously spaced) ----------------

    static void BuildHouses()
    {
        var houses = new GameObject("Houses").transform; houses.SetParent(s_root, false);
        string[] variants = { "01", "02", "03", "04", "05" };

        // left bank
        foreach (float z in LeftZ)
        {
            string v = variants[Random.Range(0, variants.Length)];
            float x = RiverX(z) - HouseOffset + J(1.5f);
            Spawn("Buildings/Bld_closed/rpgpp_lt_building_" + v + ".prefab", new Vector3(x, 0, z + J(1.5f)), 90 + J(8f), houses);
        }
        // right bank (staggered z relative to the left bank)
        foreach (float z in RightZ)
        {
            string v = variants[Random.Range(0, variants.Length)];
            float x = RiverX(z) + HouseOffset + J(1.5f);
            Spawn("Buildings/Bld_closed/rpgpp_lt_building_" + v + ".prefab", new Vector3(x, 0, z + J(1.5f)), -90 + J(8f), houses);
        }
    }

    static void BuildFencesAndProps()
    {
        var f = new GameObject("Fences").transform; f.SetParent(s_root, false);
        // a small fenced garden behind a house on each bank
        Vector3[] gardenOrigins = { new Vector3(RiverX(-22f) - HouseOffset - 11f, 0, -22f), new Vector3(RiverX(22f) + HouseOffset + 11f, 0, 22f) };
        foreach (var o in gardenOrigins)
        {
            for (int i = 0; i < 3; i++) Spawn("Exterior/Wood_path/rpgpp_lt_fence_wood_01a.prefab", o + new Vector3(i * 1.6f - 1.6f, 0, 2f), 0, f);
            for (int i = 0; i < 3; i++) Spawn("Exterior/Wood_path/rpgpp_lt_fence_wood_01a.prefab", o + new Vector3(i * 1.6f - 1.6f, 0, -2f), 180, f);
            Spawn("Exterior/Wood_path/rpgpp_lt_fence_wood_01_corner_a.prefab", o + new Vector3(-2.2f, 0, 2.2f), 0, f);
            Spawn("Exterior/Wood_path/rpgpp_lt_fence_wood_01_corner_a.prefab", o + new Vector3(2.2f, 0, 2.2f), -90, f);
        }
        // sheds beside a couple of houses
        Spawn("Exterior/Wood_path/rpgpp_lt_shed_wood_01.prefab", new Vector3(RiverX(-42f) - HouseOffset - 10f, 0, -42f), -60, f);
        Spawn("Exterior/Wood_path/rpgpp_lt_shed_wood_02.prefab", new Vector3(RiverX(42f) + HouseOffset + 10f, 0, 42f), 70, f);
        // wells
        Spawn("Props/Misc/rpgpp_lt_well_01.prefab", new Vector3(RiverX(-8f) - PathOffset - 2f, 0, -8f), 20, f);
        // a wagon near the pub
        Spawn("Props/Misc/rpgpp_lt_wagon_01.prefab", new Vector3(RiverX(0f) + 6f, 0, -3f), 25, f);
    }

    static void BuildMarket()
    {
        var m = new GameObject("Market").transform; m.SetParent(s_root, false);
        // stall-like clusters near the bridge, between the path and the river
        Vector3[] spots = { new Vector3(RiverX(9f) + 5.5f, 0, 9f), new Vector3(RiverX(-6f) + 5.5f, 0, -6f), new Vector3(RiverX(-10f) - 5.5f, 0, -10f), new Vector3(RiverX(10f) - 5.5f, 0, 10f) };
        string[] goods = {
            "Props/Containers/rpgpp_lt_crate_01.prefab", "Props/Containers/rpgpp_lt_crate_02.prefab",
            "Props/Containers/rpgpp_lt_sack_01.prefab", "Props/Containers/rpgpp_lt_basket_01.prefab"
        };
        foreach (var spot in spots)
        {
            Spawn("Props/Tables/rpgpp_lt_table_01.prefab", spot, J(30f), m);
            Spawn(goods[Random.Range(0, goods.Length)], spot + new Vector3(0.9f, 0, 0.4f), J(45f), m);
            Spawn("Props/Containers/rpgpp_lt_barrel_0" + Random.Range(1, 3) + ".prefab", spot + new Vector3(-0.8f, 0, -0.5f), J(45f), m);
            if (Random.value < 0.6f) Spawn("Props/Chairs/rpgpp_lt_chair_01a.prefab", spot + new Vector3(0, 0, -1f), J(30f), m);
        }
        // benches by the bridge
        Spawn("Props/Benches/rpgpp_lt_bench_wood_01.prefab", new Vector3(RiverX(4f) - 4f, 0, 4f), 90, m);
        Spawn("Props/Benches/rpgpp_lt_bench_wood_02.prefab", new Vector3(RiverX(-4f) + 4f, 0, -4f), -90, m);
    }

    // World position (and clearance radius) of the separate Pub interior dollhouse
    // that CombinedBuilder places beside the village - nature must never scatter
    // into it (the real building/rock prefabs are large enough to swallow it).
    static readonly Vector2 PubCenter = new Vector2(40f, 0f);
    const float PubClearance = 24f;
    static bool NearPub(float x, float z) => Vector2.Distance(new Vector2(x, z), PubCenter) < PubClearance;

    static void BuildNature()
    {
        var n = new GameObject("Nature").transform; n.SetParent(s_root, false);
        string[] trees = { "Nature/Vegetation/Trees/rpgpp_lt_tree_01.prefab", "Nature/Vegetation/Trees/rpgpp_lt_tree_02.prefab", "Nature/Vegetation/Trees/rpgpp_lt_tree_pine_01.prefab" };
        string[] bushes = { "Nature/Vegetation/Bushes/rpgpp_lt_bush_01.prefab", "Nature/Vegetation/Bushes/rpgpp_lt_bush_02.prefab" };
        // small rocks only - "rock_01"/"rock_02" are boulder/hill-sized in this pack
        string[] rocks = { "Nature/Rocks/rpgpp_lt_rock_small_01.prefab", "Nature/Rocks/rpgpp_lt_rock_small_02.prefab", "Nature/Rocks/rpgpp_lt_rocks_tiny_01.prefab" };
        string[] flowers = { "Nature/Vegetation/Flowers/rpgpp_lt_flower_01.prefab", "Nature/Vegetation/Flowers/rpgpp_lt_flower_02.prefab", "Nature/Vegetation/Flowers/rpgpp_lt_flower_03.prefab" };

        int made = 0, guard = 0;
        while (made < 26 && guard++ < 500)
        {
            float x = J(58f), z = J(58f);
            if (Mathf.Abs(x - RiverX(z)) < HouseOffset + 6f) continue; // keep off river, path & house rows
            if (NearPub(x, z)) continue;
            Spawn(trees[Random.Range(0, trees.Length)], new Vector3(x, 0, z), J(180f), n, Random.Range(0.85f, 1.3f));
            made++;
        }
        for (int i = 0; i < 10; i++)
        {
            float x, z; int g = 0;
            do { x = J(60f); z = J(60f); g++; } while (NearPub(x, z) && g < 20);
            Spawn(bushes[Random.Range(0, bushes.Length)], new Vector3(x, 0, z), J(180f), n);
        }
        for (int i = 0; i < 8; i++)
        {
            float x, z; int g = 0;
            do { x = J(60f); z = J(60f); g++; } while (NearPub(x, z) && g < 20);
            Spawn(rocks[Random.Range(0, rocks.Length)], new Vector3(x, 0, z), J(180f), n);
        }
        for (int i = 0; i < 12; i++)
        {
            float z = J(50f);
            float x = RiverX(z) + (Random.value < 0.5f ? -1 : 1) * Random.Range(11f, 15f);
            if (NearPub(x, z)) continue;
            Spawn(flowers[Random.Range(0, flowers.Length)], new Vector3(x, 0, z), J(180f), n);
        }
    }

    // A generous colour pass over the whole village: continuous flower borders
    // lining both riverbank footpaths, blooming clusters + bushes hugging every
    // house, colourful market awnings, and lily-pad style flower dabs along the
    // water's edge. Kept off the paths/river/pub so nothing blocks the walk or
    // the fly-through.
    static void BeautifyVillage()
    {
        var b = new GameObject("Beautify").transform; b.SetParent(s_root, false);
        string[] flowers = {
            "Nature/Vegetation/Flowers/rpgpp_lt_flower_01.prefab",
            "Nature/Vegetation/Flowers/rpgpp_lt_flower_02.prefab",
            "Nature/Vegetation/Flowers/rpgpp_lt_flower_03.prefab",
        };
        string[] plants = {
            "Nature/Vegetation/Plants/rpgpp_lt_plant_01.prefab",
            "Nature/Vegetation/Plants/rpgpp_lt_plant_02.prefab",
        };
        string[] bushes = {
            "Nature/Vegetation/Bushes/rpgpp_lt_bush_01.prefab",
            "Nature/Vegetation/Bushes/rpgpp_lt_bush_02.prefab",
        };
        GameObject Flower(Vector3 p) => Spawn(flowers[Random.Range(0, flowers.Length)], p, J(180f), b, Random.Range(0.9f, 1.4f));

        // 1) flower ribbons running alongside BOTH footpaths, just inside the path
        //    (paths sit at RiverX(z) ± PathOffset). Two staggered rows per side.
        for (float z = -RiverLen / 2f + 4; z < RiverLen / 2f - 4; z += 2.6f)
        {
            foreach (float side in new float[] { -1f, 1f })
            {
                float baseX = RiverX(z) + side * PathOffset;
                // a row just inside the path (toward the houses) and one just outside
                float xi = baseX + side * (2.2f + J(0.5f));
                float xo = baseX - side * (1.6f + J(0.4f));
                if (!NearPub(xi, z)) Flower(new Vector3(xi, 0, z + J(0.8f)));
                if (Random.value < 0.7f && !NearPub(xo, z)) Flower(new Vector3(xo, 0, z + J(0.8f)));
            }
        }

        // 2) a blooming garden hugging every house (both banks)
        void HouseGarden(float x, float z)
        {
            for (int i = 0; i < 7; i++)
            {
                float fx = x + J(3.2f), fz = z + J(3.2f);
                if (NearPub(fx, fz)) continue;
                Flower(new Vector3(fx, 0, fz));
            }
            Spawn(bushes[Random.Range(0, bushes.Length)], new Vector3(x + J(2.5f), 0, z + J(2.5f)), J(180f), b);
            if (Random.value < 0.7f)
                Spawn(plants[Random.Range(0, plants.Length)], new Vector3(x + J(2.5f), 0, z + J(2.5f)), J(180f), b);
        }
        foreach (float z in LeftZ) HouseGarden(RiverX(z) - HouseOffset + 5f, z);   // garden on the village-facing side
        foreach (float z in RightZ) HouseGarden(RiverX(z) + HouseOffset - 5f, z);

        // 3) colourful awnings over the market stalls for a splash of fabric colour
        Vector3[] stalls = { new Vector3(RiverX(9f) + 5.5f, 0, 9f), new Vector3(RiverX(-6f) + 5.5f, 0, -6f),
                             new Vector3(RiverX(-10f) - 5.5f, 0, -10f), new Vector3(RiverX(10f) - 5.5f, 0, 10f) };
        string[] awnings = { "Exterior/Wood_path/rpgpp_lt_awning_standing_01a.prefab",
                             "Exterior/Wood_path/rpgpp_lt_awning_standing_01b.prefab" };
        for (int i = 0; i < stalls.Length; i++)
            Spawn(awnings[i % awnings.Length], stalls[i] + new Vector3(0, 0, -0.2f), (i % 2 == 0 ? 0 : 180) + J(8f), b);

        // 4) flower dabs + reeds along the water's edge (just outside the banks)
        for (float z = -RiverLen / 2f + 3; z < RiverLen / 2f - 3; z += 3.4f)
        {
            foreach (float side in new float[] { -1f, 1f })
            {
                float x = RiverX(z) + side * (RiverHalf + 1.2f + J(0.3f));
                if (NearPub(x, z)) continue;
                if (Random.value < 0.6f) Flower(new Vector3(x, 0, z + J(1f)));
                else Spawn(plants[Random.Range(0, plants.Length)], new Vector3(x, 0, z + J(1f)), J(180f), b);
            }
        }
    }

    static void PlaceCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = new Vector3(-42f, 26f, -50f);
        cam.transform.rotation = Quaternion.Euler(20f, 46f, 0);
    }
}
