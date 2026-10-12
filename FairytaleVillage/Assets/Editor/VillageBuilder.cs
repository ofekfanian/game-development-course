using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Builds a simple fairy-tale village that matches the assignment: houses on two
// banks of a river, paths, a pub, a bridge and a few stalls, plus some trees.
// Kept deliberately modest and slightly irregular so it looks hand-placed.
//   Menu:  Tools > Fairytale Village > Build   (or Clear)
public static class VillageBuilder
{
    static readonly Color Timber = new Color(0.28f, 0.19f, 0.12f);
    static Transform s_root;

    // Gentle river meander
    static float RiverX(float z) => 2.0f * Mathf.Sin(z * 0.08f) + 0.6f * Mathf.Sin(z * 0.17f);
    const float RiverLen = 56f, RiverHalf = 3.2f;

    [MenuItem("Tools/Fairytale Village/Build")]
    public static void Build()
    {
        Random.InitState(11); // fixed seed: reproducible layout
        var existing = GameObject.Find("Village");
        if (existing != null) Object.DestroyImmediate(existing);
        s_root = new GameObject("Village").transform;

        BuildGround();
        BuildRiver();
        BuildPaths();
        BuildBridge();
        BuildHouses();
        BuildPub(new Vector3(9.5f, 0, 1.5f));
        BuildStalls();
        BuildTrees();
        PlaceCamera();

        Selection.activeGameObject = s_root.gameObject;
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Village built. Groups: " + s_root.childCount);
    }

    [MenuItem("Tools/Fairytale Village/Clear")]
    public static void Clear()
    {
        var e = GameObject.Find("Village");
        if (e != null) Object.DestroyImmediate(e);
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
    }

    // Attaches the fly-through component to the Main Camera for recording.
    [MenuItem("Tools/Fairytale Village/Setup Recording Camera")]
    public static void SetupRecordingCamera()
    {
        var cam = Camera.main;
        if (cam == null) { Debug.LogError("No Main Camera found."); return; }
        if (cam.GetComponent<CameraFlythrough>() == null) cam.gameObject.AddComponent<CameraFlythrough>();
        Selection.activeGameObject = cam.gameObject;
        Debug.Log("Camera fly-through attached. Enter Play mode (or start the Recorder) to run it.");
    }

    // ---- helpers ----------------------------------------------------------

    static Shader Lit() { var s = Shader.Find("Universal Render Pipeline/Lit"); return s != null ? s : Shader.Find("Standard"); }
    static Material Mat(Color c) { var m = new Material(Lit()); m.color = c; m.SetColor("_BaseColor", c); return m; }
    static float J(float r) => Random.Range(-r, r);

    static GameObject Box(string n, Vector3 p, Vector3 s, Color c, Transform par, Vector3 e = default)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = n; g.transform.SetParent(par, false);
        g.transform.localScale = s; g.transform.localPosition = p; g.transform.localRotation = Quaternion.Euler(e);
        g.GetComponent<Renderer>().sharedMaterial = Mat(c); return g;
    }
    static GameObject Cyl(string n, Vector3 p, Vector3 s, Color c, Transform par, Vector3 e = default)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = n; g.transform.SetParent(par, false);
        g.transform.localScale = s; g.transform.localPosition = p; g.transform.localRotation = Quaternion.Euler(e);
        g.GetComponent<Renderer>().sharedMaterial = Mat(c); return g;
    }
    static GameObject Sph(string n, Vector3 p, float d, Color c, Transform par)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = n; g.transform.SetParent(par, false);
        g.transform.localScale = Vector3.one * d; g.transform.localPosition = p;
        g.GetComponent<Renderer>().sharedMaterial = Mat(c); return g;
    }

    // ---- terrain ----------------------------------------------------------

    static void BuildGround()
    {
        var pl = GameObject.CreatePrimitive(PrimitiveType.Plane);
        pl.name = "Ground"; pl.transform.SetParent(s_root, false);
        pl.transform.localScale = new Vector3(7, 1, 7);
        pl.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.42f, 0.53f, 0.30f));
    }

    static void BuildRiver()
    {
        var r = new GameObject("River").transform; r.SetParent(s_root, false);
        Color water = new Color(0.22f, 0.46f, 0.62f), mud = new Color(0.44f, 0.36f, 0.25f);
        float step = 2.5f;
        for (float z = -RiverLen / 2f; z < RiverLen / 2f; z += step)
        {
            float x0 = RiverX(z), x1 = RiverX(z + step);
            Vector3 c = new Vector3((x0 + x1) / 2f, 0.02f, z + step / 2f);
            float yaw = Mathf.Atan2(x1 - x0, step) * Mathf.Rad2Deg;
            float len = Mathf.Sqrt(step * step + (x1 - x0) * (x1 - x0)) + 0.6f;
            Box("Bank", new Vector3(c.x, 0.015f, c.z), new Vector3(RiverHalf * 2f + 1.2f, 0.03f, len), mud, r, new Vector3(0, yaw, 0));
            Box("Water", c, new Vector3(RiverHalf * 2f, 0.05f, len), water, r, new Vector3(0, yaw, 0));
        }
    }

    static void BuildPaths()
    {
        var pth = new GameObject("Paths").transform; pth.SetParent(s_root, false);
        Color path = new Color(0.60f, 0.51f, 0.36f);
        float step = 3f;
        for (float z = -RiverLen / 2f + 2; z < RiverLen / 2f - 2; z += step)
            foreach (float side in new float[] { -1f, 1f })
            {
                float off = 5f + J(0.5f);
                float x0 = RiverX(z) + side * off, x1 = RiverX(z + step) + side * off;
                Vector3 c = new Vector3((x0 + x1) / 2f, 0.03f, z + step / 2f);
                float yaw = Mathf.Atan2(x1 - x0, step) * Mathf.Rad2Deg;
                Box("Path", c, new Vector3(Random.Range(1.7f, 2.3f), 0.05f, step + 0.4f), path, pth, new Vector3(0, yaw, 0));
            }
        Box("PathToBridge", new Vector3(0, 0.03f, 0), new Vector3(16f, 0.05f, 2.6f), path, pth);
    }

    static void BuildBridge()
    {
        var b = new GameObject("Bridge").transform; b.SetParent(s_root, false);
        Color wood = new Color(0.45f, 0.30f, 0.18f);
        Box("Deck", new Vector3(0, 0.5f, 0), new Vector3(RiverHalf * 2f + 3.5f, 0.22f, 2.8f), wood, b);
        Box("RailA", new Vector3(0, 0.9f, 1.3f), new Vector3(RiverHalf * 2f + 3.5f, 0.5f, 0.18f), wood, b);
        Box("RailB", new Vector3(0, 0.9f, -1.3f), new Vector3(RiverHalf * 2f + 3.5f, 0.5f, 0.18f), wood, b);
    }

    // ---- houses (simple, irregular) --------------------------------------

    static void BuildHouses()
    {
        // Rows of { x, z, yaw }. +x is the right bank. Spacing/angles are uneven on
        // purpose (some houses sit in a 2nd row) so the banks are not straight lines.
        float[,] spec = {
            { 8f, -4f, -62f },     // angled down the street
            { 12.5f, -9f, -60f },  // pulled back to a 2nd row
            { 7.5f, -18f, -100f },
            { 10f, -22f, -80f },
            { 12f, 7f, -116f },    // pulled back to a 2nd row
            { 8f, 17f, -66f },
            { -7.5f, -6f, 90f },
            { -9.5f, -13f, 120f }, // re-angled
            { -11.5f, -18f, 66f }, // pulled back to a 2nd row
            { -8.5f, 6f, 84f },
            { -12f, 14f, 100f },   // pulled back to a 2nd row
            { -7.5f, 19f, 88f },
        };
        for (int i = 0; i < spec.GetLength(0); i++)
            BuildHouse(new Vector3(spec[i, 0] + J(0.7f), 0, spec[i, 1] + J(1f)), spec[i, 2] + J(8f));
    }

    static void BuildHouse(Vector3 pos, float yaw, float wOverride = -1f, Color? roofOverride = null)
    {
        var h = new GameObject("House").transform;
        h.SetParent(s_root, false); h.localPosition = pos; h.localRotation = Quaternion.Euler(0, yaw, 0);

        float w = wOverride > 0 ? wOverride : Random.Range(2.6f, 3.8f);
        float d = Random.Range(3f, 4.2f);
        float ht = Random.Range(2.6f, 3.4f);

        Color[] walls = { new Color(0.92f, 0.89f, 0.79f), new Color(0.87f, 0.82f, 0.70f), new Color(0.90f, 0.86f, 0.76f), new Color(0.82f, 0.76f, 0.64f) };
        Color[] roofs = { new Color(0.55f, 0.27f, 0.18f), new Color(0.47f, 0.22f, 0.16f), new Color(0.50f, 0.35f, 0.22f), new Color(0.42f, 0.20f, 0.15f) };
        Color wall = walls[Random.Range(0, walls.Length)];
        Color roof = roofOverride ?? roofs[Random.Range(0, roofs.Length)];

        Box("Walls", new Vector3(0, ht / 2f, 0), new Vector3(w, ht, d), wall, h);
        // subtle timber corner posts (a light medieval hint, nothing more)
        foreach (int sx in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
                Box("Post", new Vector3(sx * w / 2f, ht / 2f, sz * d / 2f), new Vector3(0.14f, ht, 0.14f), Timber, h);

        // Peaked roof: a cube rotated 45 deg reads as a gable.
        float s = w / 1.414f;
        Box("Roof", new Vector3(0, ht, 0), new Vector3(s, s, d * 1.1f), roof, h, new Vector3(0, 0, 45));

        if (Random.value < 0.55f)
            Box("Chimney", new Vector3(w * 0.2f, ht + 0.6f, d * 0.15f), new Vector3(0.35f, 1.1f, 0.35f), new Color(0.42f, 0.28f, 0.22f), h);

        Box("Door", new Vector3(w / 2f + 0.02f, 0.85f, 0), new Vector3(0.1f, 1.5f, 0.8f), new Color(0.33f, 0.20f, 0.11f), h);
        foreach (int sz in new[] { -1, 1 })
            Box("Win", new Vector3(w / 2f + 0.03f, 1.85f, sz * d * 0.28f), new Vector3(0.1f, 0.6f, 0.6f), new Color(0.55f, 0.70f, 0.80f), h);
    }

    // The pub: the biggest building (two storeys) with a hanging beer-mug sign,
    // so it stands out from the ordinary houses.
    static void BuildPub(Vector3 pos)
    {
        var pub = new GameObject("Pub (Tavern)").transform;
        pub.SetParent(s_root, false); pub.localPosition = pos;
        pub.localRotation = Quaternion.Euler(0, -90, 0); // door/front faces the river (-x world)

        Color wall1 = new Color(0.70f, 0.53f, 0.32f);  // warm ochre ground floor
        Color wall2 = new Color(0.90f, 0.86f, 0.76f);  // cream upper floor
        Color roof = new Color(0.45f, 0.16f, 0.12f);
        float w = 6f, d = 5f, h1 = 3f, h2 = 2.4f;

        Box("GroundFloor", new Vector3(0, h1 / 2f, 0), new Vector3(w, h1, d), wall1, pub);
        Box("UpperFloor", new Vector3(0, h1 + h2 / 2f, 0), new Vector3(w + 0.4f, h2, d + 0.4f), wall2, pub);
        // timber on the upper floor
        foreach (int sx in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
                Box("Post", new Vector3(sx * (w + 0.4f) / 2f, h1 + h2 / 2f, sz * (d + 0.4f) / 2f), new Vector3(0.16f, h2, 0.16f), Timber, pub);
        Box("Band", new Vector3(0, h1, 0), new Vector3(w + 0.5f, 0.22f, d + 0.5f), Timber, pub);

        float s = (w + 0.4f) / 1.414f;
        Box("Roof", new Vector3(0, h1 + h2, 0), new Vector3(s, s, (d + 0.4f) * 1.12f), roof, pub, new Vector3(0, 0, 45));
        Box("Chimney", new Vector3(w * 0.28f, h1 + h2 + 0.7f, d * 0.2f), new Vector3(0.45f, 1.3f, 0.45f), new Color(0.42f, 0.28f, 0.22f), pub);

        // double door
        Box("Door", new Vector3(w / 2f + 0.03f, 1.0f, 0), new Vector3(0.12f, 2.0f, 1.3f), new Color(0.33f, 0.20f, 0.11f), pub);
        // ordinary windows on the front, both floors
        foreach (int sz in new[] { -1, 1 })
        {
            Box("Win1", new Vector3(w / 2f + 0.04f, 1.5f, sz * 1.6f), new Vector3(0.1f, 0.9f, 0.8f), new Color(0.55f, 0.70f, 0.80f), pub);
            Box("Win2", new Vector3((w + 0.4f) / 2f + 0.04f, h1 + 1.2f, sz * 1.4f), new Vector3(0.1f, 0.8f, 0.7f), new Color(0.55f, 0.70f, 0.80f), pub);
        }

        // hanging mug sign on a bracket out front
        var sign = new GameObject("PubSign").transform; sign.SetParent(pub, false);
        sign.localPosition = new Vector3(w / 2f + 1.2f, 0, 2.2f);
        Box("Post", new Vector3(0, 1.7f, 0), new Vector3(0.2f, 3.4f, 0.2f), new Color(0.30f, 0.20f, 0.10f), sign);
        Box("Arm", new Vector3(0.5f, 3.2f, 0), new Vector3(1.1f, 0.12f, 0.12f), new Color(0.30f, 0.20f, 0.10f), sign);
        Box("BoardFrame", new Vector3(1.0f, 2.4f, 0), new Vector3(1.5f, 1.2f, 0.1f), new Color(0.30f, 0.20f, 0.10f), sign);
        Box("Board", new Vector3(1.0f, 2.4f, 0.02f), new Vector3(1.3f, 1.0f, 0.12f), new Color(0.82f, 0.66f, 0.34f), sign);
        Box("Mug", new Vector3(1.0f, 2.4f, 0.1f), new Vector3(0.42f, 0.55f, 0.08f), new Color(0.40f, 0.26f, 0.14f), sign);
        Box("MugHandle", new Vector3(1.35f, 2.4f, 0.1f), new Vector3(0.14f, 0.32f, 0.08f), new Color(0.40f, 0.26f, 0.14f), sign);
        Box("MugFoam", new Vector3(1.0f, 2.72f, 0.1f), new Vector3(0.42f, 0.14f, 0.09f), new Color(0.92f, 0.90f, 0.82f), sign);

        // barrels + benches outside (a little terrace)
        Vector3 fr = new Vector3(w / 2f + 0.6f, 0, -1.6f);
        Cyl("Barrel", fr + new Vector3(0, 0.45f, 0), new Vector3(0.55f, 0.45f, 0.55f), new Color(0.45f, 0.30f, 0.17f), pub);
        Cyl("Barrel", fr + new Vector3(0.3f, 0.45f, -0.6f), new Vector3(0.55f, 0.45f, 0.55f), new Color(0.42f, 0.28f, 0.16f), pub);
        Cyl("Barrel", fr + new Vector3(0.15f, 0.95f, -0.3f), new Vector3(0.5f, 0.4f, 0.5f), new Color(0.44f, 0.29f, 0.17f), pub, new Vector3(90, 0, 0));
        Box("Bench", new Vector3(w / 2f + 1.1f, 0.35f, 2.2f), new Vector3(1.8f, 0.15f, 0.5f), new Color(0.45f, 0.32f, 0.2f), pub);
    }

    static void BuildStalls()
    {
        var stalls = new GameObject("Stalls").transform; stalls.SetParent(s_root, false);
        Color[] aw = { new Color(0.75f, 0.20f, 0.20f), new Color(0.20f, 0.45f, 0.55f), new Color(0.85f, 0.60f, 0.20f), new Color(0.30f, 0.55f, 0.35f) };
        Vector3[] spots = { new Vector3(5.5f, 0, 5f), new Vector3(6.5f, 0, -4f), new Vector3(-5.5f, 0, 3.5f), new Vector3(-6f, 0, -6f) };
        for (int i = 0; i < spots.Length; i++)
        {
            var st = new GameObject("Stall").transform; st.SetParent(stalls, false);
            st.localPosition = spots[i]; st.localRotation = Quaternion.Euler(0, J(30f), 0);
            Box("Counter", new Vector3(0, 0.5f, 0), new Vector3(1.7f, 1f, 1.1f), new Color(0.5f, 0.38f, 0.25f), st);
            Box("PostL", new Vector3(-0.75f, 1.1f, 0.45f), new Vector3(0.1f, 2.1f, 0.1f), new Color(0.35f, 0.25f, 0.15f), st);
            Box("PostR", new Vector3(0.75f, 1.1f, 0.45f), new Vector3(0.1f, 2.1f, 0.1f), new Color(0.35f, 0.25f, 0.15f), st);
            Box("Awning", new Vector3(0, 2.1f, 0.15f), new Vector3(2.1f, 0.1f, 1.5f), aw[i % aw.Length], st, new Vector3(-18f, 0, 0));
        }
    }

    static void BuildTrees()
    {
        var trees = new GameObject("Trees").transform; trees.SetParent(s_root, false);
        Color trunk = new Color(0.36f, 0.25f, 0.15f);
        Color[] leaves = { new Color(0.25f, 0.45f, 0.22f), new Color(0.30f, 0.50f, 0.25f), new Color(0.22f, 0.40f, 0.20f) };
        int made = 0, guard = 0;
        while (made < 14 && guard++ < 300)
        {
            float x = J(30f), z = J(30f);
            if (Mathf.Abs(x - RiverX(z)) < 6f && Mathf.Abs(z) < 26f) continue; // keep off river & house rows
            var t = new GameObject("Tree").transform; t.SetParent(trees, false);
            t.localPosition = new Vector3(x, 0, z);
            float sc = Random.Range(0.9f, 1.5f);
            Cyl("Trunk", new Vector3(0, 1f * sc, 0), new Vector3(0.3f, 1f * sc, 0.3f), trunk, t);
            Sph("Foliage", new Vector3(J(0.2f), 2.6f * sc, J(0.2f)), 2.2f * sc, leaves[Random.Range(0, leaves.Length)], t);
            made++;
        }
    }

    static void PlaceCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = new Vector3(-22f, 15f, -28f);
        cam.transform.rotation = Quaternion.Euler(20f, 46f, 0);
    }
}
