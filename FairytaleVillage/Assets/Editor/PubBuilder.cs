using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// HW2 - builds the interior of the pub: an open-front ("doll-house") two-storey
// tavern with a bar, tables and chairs, a fireplace, a mezzanine second floor,
// a staircase connecting the floors, and warm lighting.
//   Menu:  Tools > Pub Interior > Build   (or Clear)
// Characters, animation and audio are added by the other HW2 scripts.
public static class PubBuilder
{
    const float W = 15f, D = 12f, T = 0.3f;   // footprint + wall thickness
    const float H1 = 4.2f, H2 = 3.6f;         // ground / second floor clear heights
    const float TOP = H1 + H2;
    const float halfW = W / 2f, backZ = D / 2f, frontZ = -D / 2f;

    static readonly Color Stone = new Color(0.55f, 0.53f, 0.49f);
    static readonly Color Wood = new Color(0.42f, 0.28f, 0.16f);
    static readonly Color WoodL = new Color(0.52f, 0.37f, 0.22f);
    static readonly Color Plaster = new Color(0.87f, 0.76f, 0.55f); // warmer tavern cream (was flat grey-beige)
    static readonly Color Timber = new Color(0.26f, 0.17f, 0.10f);
    static readonly Color Rug = new Color(0.55f, 0.14f, 0.12f);

    static Transform s_root;

    [MenuItem("Tools/Pub Interior/Build")]
    public static void Build()
    {
        foreach (var n in new[] { "Village", "Pub" })
        {
            var g = GameObject.Find(n);
            if (g != null) Object.DestroyImmediate(g);
        }
        s_root = new GameObject("Pub").transform;

        BuildShell();
        BuildMezzanineAndStairs();
        BuildFireplace();
        BuildBar();
        BuildFurniture();
        BuildLightsAndDeco();
        BuildTavernFlair();
        PlaceCamera();

        Selection.activeGameObject = s_root.gameObject;
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Pub interior built. Children: " + s_root.childCount);
    }

    [MenuItem("Tools/Pub Interior/Clear")]
    public static void Clear()
    {
        var g = GameObject.Find("Pub");
        if (g != null) Object.DestroyImmediate(g);
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
    }

    // ---- helpers ----------------------------------------------------------

    static Shader Lit() { var s = Shader.Find("Universal Render Pipeline/Lit"); return s != null ? s : Shader.Find("Standard"); }
    static Material Mat(Color c) { var m = new Material(Lit()); m.color = c; m.SetColor("_BaseColor", c); return m; }
    static Material MatE(Color baseC, Color emis)
    {
        var m = new Material(Lit()); m.color = baseC; m.SetColor("_BaseColor", baseC);
        m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emis);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; return m;
    }

    // ---- textured materials (wood planks / stone / plaster) so surfaces read
    //      like a real timber tavern instead of flat-coloured blocks ----------
    static Texture2D Tex(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Pub/" + n + ".png");
    internal static Material MakeTexMat(string tex, Color tint, float smooth = 0.08f)
    {
        var m = new Material(Lit());
        var t = Tex(tex);
        if (t != null) { m.SetTexture("_BaseMap", t); m.mainTexture = t; }
        m.color = tint; m.SetColor("_BaseColor", tint);
        m.SetFloat("_Smoothness", smooth);
        return m;
    }
    static Material s_mWoodWall, s_mWoodFloor, s_mWoodDark, s_mStone, s_mPlaster, s_mWoodLite;
    // NOTE: use Unity's == null (not ??=) so a cached material that Unity has
    // destroyed between builds ("fake null") is recreated instead of reused.
    static Material Cached(ref Material slot, string tex, Color tint)
    {
        if (slot == null) slot = MakeTexMat(tex, tint);
        return slot;
    }
    internal static Material MWoodWall => Cached(ref s_mWoodWall, "wood_planks", new Color(0.95f, 0.9f, 0.85f));
    internal static Material MWoodFloor => Cached(ref s_mWoodFloor, "wood_floor", new Color(0.92f, 0.88f, 0.83f));
    internal static Material MWoodDark => Cached(ref s_mWoodDark, "wood_dark", new Color(0.9f, 0.86f, 0.8f));
    internal static Material MWoodLite => Cached(ref s_mWoodLite, "wood_planks", new Color(1.05f, 1.0f, 0.92f));
    internal static Material MStone => Cached(ref s_mStone, "stone_wall", new Color(0.95f, 0.93f, 0.9f));
    internal static Material MPlaster => Cached(ref s_mPlaster, "plaster", new Color(1f, 0.97f, 0.92f));

    // Textured box: clones the base material and tiles it by the box's two
    // largest dimensions so texel density stays roughly constant.
    internal static GameObject BoxT(string n, Vector3 p, Vector3 s, Material baseMat, Transform par, float tpm = 0.45f, Vector3 e = default)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = n; g.transform.SetParent(par, false);
        g.transform.localScale = s; g.transform.localPosition = p; g.transform.localRotation = Quaternion.Euler(e);
        float ax = Mathf.Abs(s.x), ay = Mathf.Abs(s.y), az = Mathf.Abs(s.z);
        float a, b;
        if (ay <= ax && ay <= az) { a = ax; b = az; }          // flat (floor/ceiling)
        else if (ax <= ay && ax <= az) { a = az; b = ay; }     // thin in X (side wall)
        else { a = ax; b = ay; }                                // thin in Z (front/back wall)
        var m = new Material(baseMat);
        m.mainTextureScale = new Vector2(Mathf.Max(1f, a * tpm), Mathf.Max(1f, b * tpm));
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

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
    static GameObject Glow(string n, Vector3 p, Vector3 s, Color baseC, Color emis, Transform par, PrimitiveType t = PrimitiveType.Cube)
    {
        var g = GameObject.CreatePrimitive(t);
        g.name = n; g.transform.SetParent(par, false);
        g.transform.localScale = s; g.transform.localPosition = p;
        g.GetComponent<Renderer>().sharedMaterial = MatE(baseC, emis); return g;
    }
    static Light AddLight(string n, Vector3 p, Color c, float intensity, float range, Transform par)
    {
        var go = new GameObject(n); go.transform.SetParent(par, false); go.transform.localPosition = p;
        var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = c;
        l.intensity = intensity; l.range = range; l.shadows = LightShadows.None; return l;
    }

    // ---- structure --------------------------------------------------------

    static void BuildShell()
    {
        var shell = new GameObject("Shell").transform; shell.SetParent(s_root, false);

        // grass ground for context
        var grass = GameObject.CreatePrimitive(PrimitiveType.Plane);
        grass.name = "Ground"; grass.transform.SetParent(shell, false);
        grass.transform.localScale = new Vector3(6, 1, 6);
        grass.transform.localPosition = new Vector3(0, -0.2f, 0);
        grass.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.40f, 0.50f, 0.28f));

        // textured wood-plank floor
        BoxT("Floor", new Vector3(0, -0.1f, 0), new Vector3(W, 0.2f, D), MWoodFloor, shell);

        // three textured wood-plank walls (front gets a framed wall + door below)
        BoxT("WallBack", new Vector3(0, TOP / 2f, backZ), new Vector3(W, TOP, T), MWoodWall, shell);
        BoxT("WallLeft", new Vector3(-halfW, TOP / 2f, 0), new Vector3(T, TOP, D), MWoodWall, shell);
        BoxT("WallRight", new Vector3(halfW, TOP / 2f, 0), new Vector3(T, TOP, D), MWoodWall, shell);
        // stone base band
        BoxT("BaseBack", new Vector3(0, 0.6f, backZ + 0.02f), new Vector3(W, 1.2f, T + 0.1f), MStone, shell);
        BoxT("BaseLeft", new Vector3(-halfW - 0.02f, 0.6f, 0), new Vector3(T + 0.1f, 1.2f, D), MStone, shell);
        BoxT("BaseRight", new Vector3(halfW + 0.02f, 0.6f, 0), new Vector3(T + 0.1f, 1.2f, D), MStone, shell);

        // timber corner posts + a floor-band between storeys (dark wood)
        foreach (var xz in new[] { new Vector2(-halfW, backZ), new Vector2(halfW, backZ), new Vector2(-halfW, frontZ), new Vector2(halfW, frontZ) })
            BoxT("Post", new Vector3(xz.x, TOP / 2f, xz.y), new Vector3(0.35f, TOP, 0.35f), MWoodDark, shell);
        BoxT("MidBandBack", new Vector3(0, H1, backZ), new Vector3(W, 0.3f, T + 0.15f), MWoodDark, shell);
        BoxT("MidBandLeft", new Vector3(-halfW, H1, 0), new Vector3(T + 0.15f, 0.3f, D), MWoodDark, shell);
        BoxT("MidBandRight", new Vector3(halfW, H1, 0), new Vector3(T + 0.15f, 0.3f, D), MWoodDark, shell);

        // ceiling beams across the width
        for (int i = 0; i < 5; i++)
            BoxT("Beam", new Vector3(0, TOP - 0.25f, -5f + i * 2.4f), new Vector3(W, 0.35f, 0.35f), MWoodDark, shell);

        BuildFrontWall(shell);
        BuildWindows(shell);
        BuildRoof(shell);
    }

    // A proper timber front wall with a wide central doorway (so the camera can
    // still fly in and the interior is filmable), an OPEN plank door, a lamp, and
    // a stone step - matching the reference tavern's entrance.
    static void BuildFrontWall(Transform shell)
    {
        var fw = new GameObject("FrontWall").transform; fw.SetParent(shell, false);
        float doorHalf = 3.0f;                  // half-width of the (wide) entrance opening
        float wingW = halfW - doorHalf;         // width of each side wing
        float wingCx = doorHalf + wingW / 2f;   // centre x of a wing

        foreach (float sgn in new[] { -1f, 1f })
        {
            // full-height wing wall on each side of the doorway
            BoxT("FrontWing", new Vector3(sgn * wingCx, TOP / 2f, frontZ), new Vector3(wingW, TOP, T), MWoodWall, fw);
            BoxT("FrontBase", new Vector3(sgn * wingCx, 0.6f, frontZ - 0.02f), new Vector3(wingW, 1.2f, T + 0.1f), MStone, fw);
        }
        // lintel above the doorway + door frame posts
        BoxT("Lintel", new Vector3(0, H1 - 0.6f, frontZ), new Vector3(doorHalf * 2f + 0.6f, 0.6f, T + 0.05f), MWoodDark, fw);
        foreach (float sgn in new[] { -1f, 1f })
            BoxT("DoorPost", new Vector3(sgn * doorHalf, (H1 - 0.9f) / 2f, frontZ), new Vector3(0.35f, H1 - 0.9f, T + 0.1f), MWoodDark, fw);

        // an OPEN plank door, swung flat against the inner face of the left wing
        // (so the wide entrance stays clear for the fly-through camera)
        float doorW = 1.7f, doorH = H1 - 1.0f;
        float dcx = -doorHalf - doorW / 2f;   // centre x, tucked against the left wing
        float dz = frontZ + 0.22f;            // just inside the wall
        var door = new GameObject("Door").transform; door.SetParent(fw, false);
        BoxT("Leaf", new Vector3(dcx, doorH / 2f + 0.05f, dz), new Vector3(doorW, doorH, 0.12f), MWoodDark, door, 0.7f);
        BoxT("BandT", new Vector3(dcx, doorH - 0.3f, dz + 0.07f), new Vector3(doorW - 0.1f, 0.14f, 0.05f), MStone, door);
        BoxT("BandB", new Vector3(dcx, 0.55f, dz + 0.07f), new Vector3(doorW - 0.1f, 0.14f, 0.05f), MStone, door);
        Cyl("Handle", new Vector3(dcx + doorW / 2f - 0.2f, doorH / 2f, dz + 0.1f), new Vector3(0.1f, 0.1f, 0.1f), new Color(0.1f, 0.1f, 0.1f), door);

        // entrance lamp above the door
        Glow("EntryLamp", new Vector3(0, H1 - 0.2f, frontZ - 0.35f), new Vector3(0.4f, 0.4f, 0.4f), new Color(1f, 0.85f, 0.5f), new Color(1f, 0.8f, 0.45f) * 2.5f, fw);
        AddLight("EntryLight", new Vector3(0, H1 - 0.5f, frontZ - 1.0f), new Color(1f, 0.82f, 0.5f), 2.2f, 7f, fw);
        // stone step outside the door
        BoxT("Step", new Vector3(0, 0.06f, frontZ - 1.0f), new Vector3(doorHalf * 2f + 0.6f, 0.12f, 1.6f), MStone, fw, 0.6f);
    }

    // Framed windows with a warm glowing pane on the side and back walls.
    static void BuildWindows(Transform shell)
    {
        var wz = new GameObject("Windows").transform; wz.SetParent(shell, false);
        void Win(Vector3 p, bool onX)
        {
            var frame = onX ? new Vector3(0.2f, 1.5f, 1.7f) : new Vector3(1.7f, 1.5f, 0.2f);
            var pane = onX ? new Vector3(0.12f, 1.15f, 1.35f) : new Vector3(1.35f, 1.15f, 0.12f);
            BoxT("WinFrame", p, frame, MWoodDark, wz, 0.8f);
            Glow("WinPane", p, pane, new Color(1f, 0.86f, 0.55f), new Color(1f, 0.8f, 0.45f) * 1.4f, wz);
        }
        Win(new Vector3(-halfW + 0.02f, 2.3f, -2.5f), true);   // left wall
        Win(new Vector3(-halfW + 0.02f, 2.3f, 2.5f), true);
        Win(new Vector3(halfW - 0.02f, 2.3f, -2.5f), true);    // right wall
        Win(new Vector3(-3.5f, 2.3f, backZ - 0.02f), false);   // back wall (one side of fireplace)
        Win(new Vector3(3.5f, 2.3f, backZ - 0.02f), false);
    }

    // A proper gable roof: two flat panels meeting at a ridge, sitting entirely
    // ABOVE the wall top (TOP) so nothing dips down into the open front - unlike
    // a single cube-rotated-45 "diamond" roof, which would hang a low point right
    // through the doorway. The back gets a simple rectangular gable fill so the
    // building reads as closed from outside; the front stays open for filming.
    static readonly Color RoofColor = new Color(0.42f, 0.16f, 0.12f);
    static void BuildRoof(Transform shell)
    {
        var roof = new GameObject("Roof").transform; roof.SetParent(shell, false);
        float rise = 2.6f;
        float angle = Mathf.Atan2(rise, halfW) * Mathf.Rad2Deg;
        float length = Mathf.Sqrt(halfW * halfW + rise * rise);
        float thick = 0.3f;
        float depth = D + 1.4f; // small eave overhang front/back

        Box("RoofR", new Vector3(halfW / 2f, TOP + rise / 2f, 0), new Vector3(length, thick, depth), RoofColor, roof, new Vector3(0, 0, -angle));
        Box("RoofL", new Vector3(-halfW / 2f, TOP + rise / 2f, 0), new Vector3(length, thick, depth), RoofColor, roof, new Vector3(0, 0, angle));
        // ridge cap
        Box("Ridge", new Vector3(0, TOP + rise, 0), new Vector3(0.5f, 0.3f, depth), RoofColor, roof);
        // back gable fill (closes the building from outside; front is left open)
        BoxT("BackGable", new Vector3(0, TOP + rise / 2f, backZ), new Vector3(W, rise, 0.2f), MWoodWall, roof, 0.5f);
    }

    static void BuildMezzanineAndStairs()
    {
        var mz = new GameObject("Mezzanine").transform; mz.SetParent(s_root, false);

        // second-floor slab over the back half (z from 0 to backZ)
        float slabDepth = backZ; // 6
        BoxT("Slab", new Vector3(0, H1, backZ / 2f), new Vector3(W, 0.3f, slabDepth), MWoodFloor, mz);

        // railing along the open front edge of the mezzanine (z = 0), gap for stairs on the right
        for (float x = -halfW + 0.6f; x <= halfW - 0.6f; x += 1.1f)
        {
            if (x > 3.2f) continue; // leave opening where the stairs arrive
            Box("RailPost", new Vector3(x, H1 + 0.55f, 0.1f), new Vector3(0.12f, 1.1f, 0.12f), Wood, mz);
        }
        Box("RailTop", new Vector3(-1.9f, H1 + 1.05f, 0.1f), new Vector3(7.5f, 0.12f, 0.12f), Wood, mz);

        // staircase against the right wall: front(bottom) -> back(top landing at mezz)
        int steps = 12;
        float run = 4.6f / steps, rise = H1 / steps;
        var stairs = new GameObject("Stairs").transform; stairs.SetParent(mz, false);
        for (int i = 0; i < steps; i++)
        {
            float y = (i + 1) * rise;
            float z = -4.2f + i * run;
            BoxT("Step", new Vector3(halfW - 1.7f, y - rise / 2f, z), new Vector3(3.0f, rise, run + 0.05f), MWoodLite, stairs, 0.7f);
        }
        // diagonal stringer + hand rail on the open (left) side of the stairs
        float len = Mathf.Sqrt(4.6f * 4.6f + H1 * H1);
        float ang = -Mathf.Atan2(H1, 4.6f) * Mathf.Rad2Deg;
        Box("Stringer", new Vector3(halfW - 3.1f, H1 / 2f, -1.9f), new Vector3(0.2f, 0.5f, len), Wood, stairs, new Vector3(ang, 0, 0));
        Box("HandRail", new Vector3(halfW - 3.1f, H1 / 2f + 1.0f, -1.9f), new Vector3(0.1f, 0.1f, len), Wood, stairs, new Vector3(ang, 0, 0));
    }

    static void BuildFireplace()
    {
        var fp = new GameObject("Fireplace").transform; fp.SetParent(s_root, false);
        float z = backZ - 0.55f;
        BoxT("Surround", new Vector3(0, 1.7f, z), new Vector3(4f, 3.4f, 1.1f), MStone, fp, 0.6f);
        BoxT("Breast", new Vector3(0, TOP - 1.2f, backZ - 0.4f), new Vector3(2.4f, 3.4f, 0.8f), MStone, fp, 0.6f);   // chimney breast up to ceiling
        BoxT("Mantel", new Vector3(0, 2.5f, z - 0.2f), new Vector3(4.4f, 0.25f, 1.3f), MWoodDark, fp, 0.7f);
        Box("Opening", new Vector3(0, 0.95f, z - 0.35f), new Vector3(2.4f, 1.7f, 0.5f), new Color(0.06f, 0.05f, 0.05f), fp);
        // logs + fire (emissive) + firelight
        Cyl("Log", new Vector3(-0.4f, 0.4f, z - 0.35f), new Vector3(0.25f, 0.7f, 0.25f), new Color(0.3f, 0.2f, 0.12f), fp, new Vector3(0, 20, 90));
        Cyl("Log", new Vector3(0.4f, 0.4f, z - 0.35f), new Vector3(0.25f, 0.7f, 0.25f), new Color(0.28f, 0.18f, 0.1f), fp, new Vector3(0, -20, 90));
        Glow("Fire", new Vector3(0, 0.6f, z - 0.35f), new Vector3(1.4f, 1.0f, 0.6f), new Color(1f, 0.5f, 0.15f), new Color(1f, 0.45f, 0.12f) * 4f, fp);
        var l = AddLight("FireLight", new Vector3(0, 1.1f, z - 1f), new Color(1f, 0.62f, 0.30f), 6f, 14f, fp);
        l.gameObject.AddComponent<FireFlicker>();

        // ---- decorative crossed axes + round shield on the breast (like the ref) ----
        var iron = new Color(0.16f, 0.16f, 0.18f); var steel = new Color(0.55f, 0.57f, 0.6f);
        float by = TOP - 1.2f;   // breast centre height
        Cyl("Shield", new Vector3(0, by, backZ - 0.02f), new Vector3(1.1f, 0.08f, 1.1f), new Color(0.34f, 0.22f, 0.12f), fp, new Vector3(90, 0, 0));
        Cyl("ShieldBoss", new Vector3(0, by, backZ - 0.06f), new Vector3(0.28f, 0.06f, 0.28f), steel, fp, new Vector3(90, 0, 0));
        foreach (float s in new[] { -1f, 1f })
        {
            var axe = new GameObject("Axe").transform; axe.SetParent(fp, false);
            axe.localPosition = new Vector3(0, by, backZ - 0.12f);
            axe.localRotation = Quaternion.Euler(0, 0, s * 32f);
            Box("Haft", new Vector3(0, 0, 0), new Vector3(0.09f, 2.2f, 0.09f), Wood, axe);
            Box("Head", new Vector3(0, 0.95f, 0), new Vector3(0.5f, 0.4f, 0.12f), steel, axe);
        }
    }

    static readonly Color Brass = new Color(0.80f, 0.60f, 0.24f);
    static readonly Color Ale = new Color(0.72f, 0.45f, 0.12f);
    static readonly Color Foam = new Color(0.96f, 0.92f, 0.82f);

    static void BuildBar()
    {
        var bar = new GameObject("Bar").transform; bar.SetParent(s_root, false);
        float x = -halfW + 0.9f;
        float topY = 1.15f, frontX = x + 0.45f;
        BoxT("Front", new Vector3(frontX, 0.55f, 0), new Vector3(1.0f, 1.1f, 6f), MWoodDark, bar, 0.6f);
        Box("FrontPanelA", new Vector3(frontX + 0.5f, 0.55f, -1.5f), new Vector3(0.08f, 0.9f, 2.6f), WoodL, bar);
        Box("FrontPanelB", new Vector3(frontX + 0.5f, 0.55f, 1.5f), new Vector3(0.08f, 0.9f, 2.6f), WoodL, bar);
        BoxT("Top", new Vector3(frontX, topY, 0), new Vector3(1.3f, 0.15f, 6.2f), MWoodLite, bar, 0.6f);
        Box("Rail", new Vector3(frontX + 0.62f, topY + 0.04f, 0), new Vector3(0.08f, 0.12f, 6.2f), Brass, bar);

        // back-bar shelving with lots of bottles
        Box("Shelf", new Vector3(x - 0.1f, 2.6f, 0), new Vector3(0.4f, 0.12f, 5f), Wood, bar);
        Box("Shelf2", new Vector3(x - 0.1f, 3.3f, 0), new Vector3(0.4f, 0.12f, 5f), Wood, bar);
        Color[] bottle = { new Color(0.2f, 0.4f, 0.2f), new Color(0.5f, 0.3f, 0.1f), new Color(0.3f, 0.3f, 0.5f), new Color(0.45f, 0.15f, 0.15f), new Color(0.15f,0.3f,0.35f) };
        for (int i = 0; i < 11; i++)
        {
            float bz = -2.4f + i * 0.48f;
            Cyl("Bottle", new Vector3(x - 0.1f, 2.85f, bz), new Vector3(0.15f, 0.22f, 0.15f), bottle[i % bottle.Length], bar);
            Cyl("Bottle", new Vector3(x - 0.1f, 3.55f, bz), new Vector3(0.15f, 0.22f, 0.15f), bottle[(i + 1) % bottle.Length], bar);
        }

        // ---- beer engine: a row of brass taps along the counter ----
        for (int i = 0; i < 4; i++)
        {
            float tz = -2.1f + i * 1.4f;
            var tap = new GameObject("BeerTap").transform; tap.SetParent(bar, false);
            tap.localPosition = new Vector3(frontX - 0.25f, topY + 0.08f, tz);
            Cyl("Body", new Vector3(0, 0.22f, 0), new Vector3(0.1f, 0.22f, 0.1f), Brass, tap);
            Box("Spout", new Vector3(0.12f, 0.34f, 0), new Vector3(0.24f, 0.06f, 0.06f), Brass, tap);
            Box("Handle", new Vector3(0, 0.5f, 0), new Vector3(0.05f, 0.2f, 0.05f), Wood, tap);
        }

        // filled tankards of ale (mug + amber body + foam head) waiting on the bar
        for (int i = 0; i < 5; i++)
            Tankard(new Vector3(frontX + 0.15f, topY + 0.08f, -2.2f + i * 1.1f), bar);

        // kegs stacked behind the bar
        SpawnReal("Containers/rpgpp_lt_barrel_01.prefab", new Vector3(x - 0.2f, 0, 2.2f), 0, bar);
        SpawnReal("Containers/rpgpp_lt_barrel_02.prefab", new Vector3(x - 0.2f, 0, -2.2f), 20, bar);
        // a tapped keg on its side at the end of the counter
        var keg = new GameObject("Keg").transform; keg.SetParent(bar, false); keg.localPosition = new Vector3(frontX + 0.9f, 0.5f, -3.6f);
        Cyl("Body", Vector3.zero, new Vector3(0.7f, 0.5f, 0.7f), new Color(0.35f, 0.22f, 0.12f), keg, new Vector3(90, 0, 0));
        foreach (float rz in new[] { -0.3f, 0.3f }) Cyl("Hoop", new Vector3(0, 0, rz), new Vector3(0.74f, 0.05f, 0.74f), Brass, keg, new Vector3(90, 0, 0));
        Box("Tap", new Vector3(0.36f, -0.1f, 0), new Vector3(0.16f, 0.1f, 0.1f), Brass, keg);

        // stools along the counter
        for (int i = 0; i < 3; i++) Stool(new Vector3(frontX + 1.5f, 0, -2f + i * 2f), bar);
    }

    // A served meal on a plate: a pewter platter with either a roast + drumsticks
    // or a stack of bread loaves, for that "real pub food" look.
    internal static void FoodPlate(Vector3 p, Transform par, int variant)
    {
        var f = new GameObject("Food").transform; f.SetParent(par, false); f.localPosition = p;
        Cyl("Plate", new Vector3(0, 0, 0), new Vector3(0.5f, 0.03f, 0.5f), new Color(0.6f, 0.6f, 0.62f), f);
        if (variant % 2 == 0)
        {
            // roast + two drumsticks
            var roast = new Color(0.5f, 0.28f, 0.14f);
            Cyl("Roast", new Vector3(0, 0.12f, 0), new Vector3(0.34f, 0.14f, 0.26f), roast, f, new Vector3(90, 0, 0));
            Cyl("Leg", new Vector3(-0.18f, 0.1f, 0.16f), new Vector3(0.1f, 0.16f, 0.1f), new Color(0.55f, 0.32f, 0.16f), f, new Vector3(20, 0, 40));
            Cyl("Leg", new Vector3(0.18f, 0.1f, -0.14f), new Vector3(0.1f, 0.16f, 0.1f), new Color(0.55f, 0.32f, 0.16f), f, new Vector3(-20, 0, -40));
        }
        else
        {
            // a little stack of golden bread loaves
            var bread = new Color(0.72f, 0.52f, 0.26f);
            Cyl("Loaf", new Vector3(-0.1f, 0.1f, 0.05f), new Vector3(0.16f, 0.2f, 0.28f), bread, f, new Vector3(90, 20, 0));
            Cyl("Loaf", new Vector3(0.12f, 0.1f, -0.06f), new Vector3(0.16f, 0.2f, 0.28f), bread, f, new Vector3(90, -30, 0));
            Cyl("Loaf", new Vector3(0.02f, 0.24f, 0f), new Vector3(0.15f, 0.18f, 0.26f), new Color(0.66f, 0.46f, 0.22f), f, new Vector3(90, 60, 0));
        }
    }

    // A filled tankard of ale: wooden mug, amber ale, a foam head and a handle.
    internal static void Tankard(Vector3 p, Transform par)
    {
        var m = new GameObject("Tankard").transform; m.SetParent(par, false); m.localPosition = p;
        Cyl("Mug", new Vector3(0, 0.13f, 0), new Vector3(0.2f, 0.14f, 0.2f), new Color(0.45f, 0.3f, 0.18f), m);
        Cyl("Ale", new Vector3(0, 0.2f, 0), new Vector3(0.16f, 0.05f, 0.16f), Ale, m);
        Cyl("Foam", new Vector3(0, 0.27f, 0), new Vector3(0.17f, 0.03f, 0.17f), Foam, m);
        Box("Handle", new Vector3(0.13f, 0.13f, 0), new Vector3(0.06f, 0.14f, 0.06f), new Color(0.4f, 0.26f, 0.15f), m);
    }

    // Real furniture prefabs from the RPG Poly Pack (Assets/RPGPP_LT), with a
    // primitive fallback so the pub still builds if the pack is missing.
    const string PropsPath = "Assets/RPGPP_LT/Prefabs/Props/";
    static GameObject RealPrefab(string relPath) => AssetDatabase.LoadAssetAtPath<GameObject>(PropsPath + relPath);
    static GameObject SpawnReal(string relPath, Vector3 localPos, float yaw, Transform parent)
    {
        var prefab = RealPrefab(relPath);
        if (prefab == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        return go;
    }

    // Loads from the RPGPP Prefabs ROOT (for Nature/Vegetation, which lives beside
    // Props). AssetDatabase paths don't resolve "..", so use an explicit base.
    const string PrefabsRoot = "Assets/RPGPP_LT/Prefabs/";
    internal static GameObject SpawnVeg(string relToRoot, Vector3 localPos, float yaw, Transform parent, float scale = 1f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsRoot + relToRoot);
        if (prefab == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        if (scale != 1f) go.transform.localScale = Vector3.one * scale;
        return go;
    }

    internal static void Stool(Vector3 p, Transform par)
    {
        var s = new GameObject("Stool").transform; s.SetParent(par, false); s.localPosition = p;
        Cyl("Seat", new Vector3(0, 0.95f, 0), new Vector3(0.55f, 0.1f, 0.55f), WoodL, s);
        Cyl("Leg", new Vector3(0, 0.47f, 0), new Vector3(0.16f, 0.47f, 0.16f), Wood, s);
        Cyl("Ring", new Vector3(0, 0.25f, 0), new Vector3(0.5f, 0.04f, 0.5f), Wood, s);
    }

    // ---- custom tavern furniture, sized for the full-height Mixamo people ----
    // (The RPGPP table/chair prefabs are built for the pack's own short characters
    // and looked tiny/wrong under the real adults, and their chairs' off-centre
    // pivots slid seated characters off the seat. These sturdy hand-built pieces
    // are proportioned for adults and use benches, so seating is clean.)
    public const float TableTopY = 1.05f;   // table surface height
    public const float BenchTopY = 0.52f;   // bench seat height (used by MixamoSetup)
    const float TableHalfX = 1.25f, TableHalfZ = 0.55f, BenchZ = 0.95f;

    static Color Darker(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f);

    internal static void TavernTable(Vector3 p, Transform par)
    {
        var t = new GameObject("Table").transform; t.SetParent(par, false); t.localPosition = p;
        // thick plank top with a rim (textured wood)
        BoxT("Top", new Vector3(0, TableTopY, 0), new Vector3(TableHalfX * 2f, 0.12f, TableHalfZ * 2f), MWoodLite, t, 0.7f);
        BoxT("Rim", new Vector3(0, TableTopY - 0.09f, 0), new Vector3(TableHalfX * 2f + 0.12f, 0.06f, TableHalfZ * 2f + 0.12f), MWoodDark, t, 0.7f);
        // four sturdy legs + a long stretcher
        foreach (var s in new[] { new Vector2(-TableHalfX + 0.18f, -TableHalfZ + 0.16f), new Vector2(TableHalfX - 0.18f, -TableHalfZ + 0.16f),
                                  new Vector2(-TableHalfX + 0.18f, TableHalfZ - 0.16f), new Vector2(TableHalfX - 0.18f, TableHalfZ - 0.16f) })
            Box("Leg", new Vector3(s.x, (TableTopY - 0.12f) / 2f, s.y), new Vector3(0.17f, TableTopY - 0.12f, 0.17f), Wood, t);
        Box("Stretcher", new Vector3(0, 0.3f, 0), new Vector3(TableHalfX * 2f - 0.4f, 0.1f, 0.12f), Wood, t);
        // a couple of mugs + a candle for life
        Cyl("Mug", new Vector3(0.5f, TableTopY + 0.13f, 0.18f), new Vector3(0.2f, 0.16f, 0.2f), new Color(0.5f, 0.35f, 0.2f), t);
        Cyl("Mug", new Vector3(-0.45f, TableTopY + 0.13f, -0.12f), new Vector3(0.2f, 0.16f, 0.2f), new Color(0.55f, 0.4f, 0.25f), t);
    }

    internal static void TavernBench(Vector3 p, Transform par)
    {
        var b = new GameObject("Bench").transform; b.SetParent(par, false); b.localPosition = p;
        BoxT("Seat", new Vector3(0, BenchTopY, 0), new Vector3(TableHalfX * 2f, 0.11f, 0.42f), MWoodLite, b, 0.7f);
        foreach (float lx in new[] { -TableHalfX + 0.25f, TableHalfX - 0.25f })
        {
            Box("Leg", new Vector3(lx, BenchTopY / 2f, 0), new Vector3(0.14f, BenchTopY - 0.05f, 0.36f), Wood, b);
        }
        Box("Rail", new Vector3(0, 0.22f, 0), new Vector3(TableHalfX * 2f - 0.5f, 0.08f, 0.1f), Wood, b);
    }

    // A table flanked by a bench on each long (Z) side.
    internal static void TableWithBenches(Vector3 p, Transform par)
    {
        TavernTable(p, par);
        TavernBench(p + new Vector3(0, 0, -BenchZ), par);
        TavernBench(p + new Vector3(0, 0, BenchZ), par);
    }

    // Ground-floor table centres (also read by MixamoSetup to seat patrons on the
    // front/-Z bench of the first two, facing the table). Kept off the central
    // x=0 barmaid lane and clear of the bar (x<-5.5) and stairs (x>4.5).
    public static readonly Vector3[] GroundTables = {
        new Vector3(-3.2f, 0, -3.2f),
        new Vector3(2.6f, 0, -3.2f),
        new Vector3(-3.4f, 0, 0.6f),
    };

    static void BuildFurniture()
    {
        var f = new GameObject("Furniture").transform; f.SetParent(s_root, false);
        foreach (var p in GroundTables) TableWithBenches(p, f);
        // upstairs on the mezzanine (z > 0, y = H1)
        var up = new GameObject("Upstairs").transform; up.SetParent(f, false); up.localPosition = new Vector3(0, H1 + 0.15f, 0);
        TableWithBenches(new Vector3(-2.5f, 0, 3.2f), up);
        TableWithBenches(new Vector3(2.5f, 0, 4.2f), up);
    }

    static void BuildLightsAndDeco()
    {
        var d = new GameObject("Lighting").transform; d.SetParent(s_root, false);

        // chandelier over the double-height space
        var ch = new GameObject("Chandelier").transform; ch.SetParent(d, false); ch.localPosition = new Vector3(0, TOP - 1.3f, -2.5f);
        Cyl("Chain", new Vector3(0, 0.9f, 0), new Vector3(0.06f, 0.9f, 0.06f), Timber, ch);
        Cyl("Ring", new Vector3(0, 0, 0), new Vector3(1.6f, 0.08f, 1.6f), Timber, ch);
        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.PI * 2f;
            var cp = new Vector3(Mathf.Cos(a) * 0.7f, 0.2f, Mathf.Sin(a) * 0.7f);
            Cyl("Candle", cp, new Vector3(0.12f, 0.2f, 0.12f), new Color(0.9f, 0.85f, 0.7f), ch);
            Glow("Flame", cp + new Vector3(0, 0.3f, 0), new Vector3(0.12f, 0.22f, 0.12f), new Color(1f, 0.8f, 0.4f), new Color(1f, 0.7f, 0.3f) * 3f, ch, PrimitiveType.Sphere);
        }
        AddLight("ChandelierLight", new Vector3(0, 0.2f, 0), new Color(1f, 0.80f, 0.5f), 5f, 16f, ch);

        // wall sconces (candle + small light)
        Vector3[] sconce = {
            new Vector3(-halfW + 0.4f, 2.6f, -3f), new Vector3(-halfW + 0.4f, 2.6f, 3f),
            new Vector3(halfW - 0.4f, 2.6f, 3.5f), new Vector3(0, 2.6f, backZ - 0.3f)
        };
        foreach (var p in sconce)
        {
            Glow("SconceFlame", p + new Vector3(0, 0.3f, 0), Vector3.one * 0.18f, new Color(1f, 0.8f, 0.4f), new Color(1f, 0.7f, 0.3f) * 3f, d, PrimitiveType.Sphere);
            Cyl("SconceCandle", p, new Vector3(0.12f, 0.2f, 0.12f), new Color(0.9f, 0.85f, 0.7f), d);
            AddLight("SconceLight", p + new Vector3(0, 0.2f, 0), new Color(1f, 0.78f, 0.45f), 2.2f, 7f, d);
        }
    }

    // Warms the place up so it reads as a lived-in tavern rather than an empty
    // hall: real barrels/banners/crates, a rug by the fire, bottles on tables.
    static void BuildTavernFlair()
    {
        var d = new GameObject("TavernFlair").transform; d.SetParent(s_root, false);

        // a rug in front of the fireplace
        Box("Rug", new Vector3(0, 0.03f, backZ - 3.5f), new Vector3(3.2f, 0.05f, 2.2f), Rug, d);
        Box("RugTrim", new Vector3(0, 0.035f, backZ - 3.5f), new Vector3(3.5f, 0.02f, 2.5f), new Color(0.30f, 0.08f, 0.07f), d);

        // real barrels clustered by the bar and by the entrance
        SpawnReal("Containers/rpgpp_lt_barrel_01.prefab", new Vector3(-halfW + 2.6f, 0, 3.6f), 0, d);
        SpawnReal("Containers/rpgpp_lt_barrel_02.prefab", new Vector3(-halfW + 2.6f, 0, 4.5f), 15, d);
        SpawnReal("Containers/rpgpp_lt_barrel_01.prefab", new Vector3(halfW - 1.2f, 0, frontZ + 1.2f), 20, d);
        SpawnReal("Containers/rpgpp_lt_crate_01.prefab", new Vector3(halfW - 1.2f, 0, frontZ + 2.4f), -10, d);
        SpawnReal("Containers/rpgpp_lt_sack_01.prefab", new Vector3(halfW - 2.2f, 0, frontZ + 1.6f), 30, d);

        // hanging tavern banners flanking the open entrance
        SpawnReal("Banners/rpgpp_lt_banner_01a.prefab", new Vector3(-halfW + 0.6f, H1 - 0.2f, frontZ + 0.3f), 0, d);
        SpawnReal("Banners/rpgpp_lt_banner_01b.prefab", new Vector3(halfW - 0.6f, H1 - 0.2f, frontZ + 0.3f), 0, d);

        // a jug and vase on the bar/mantel for lived-in detail
        SpawnReal("Containers/rpgpp_lt_jug_01.prefab", new Vector3(-halfW + 1.7f, 1.15f, -2.0f), 0, d);
        SpawnReal("Containers/rpgpp_lt_vase_01.prefab", new Vector3(0, 2.5f, backZ - 0.75f), 0, d);

        // a little vase of flowers as a centrepiece on each table (colour + life)
        string[] fl = {
            "Nature/Vegetation/Flowers/rpgpp_lt_flower_01.prefab",
            "Nature/Vegetation/Flowers/rpgpp_lt_flower_02.prefab",
            "Nature/Vegetation/Flowers/rpgpp_lt_flower_03.prefab",
        };
        string[] vases = { "Containers/rpgpp_lt_vase_02.prefab", "Containers/rpgpp_lt_vase_03.prefab" };
        (Vector3 pos, float y)[] tables = {
            (GroundTables[0], 0f), (GroundTables[1], 0f), (GroundTables[2], 0f),
            (new Vector3(-2.5f, 0, 3.2f), H1 + 0.15f), (new Vector3(2.5f, 0, 4.2f), H1 + 0.15f),
        };
        for (int i = 0; i < tables.Length; i++)
        {
            var t = tables[i];
            float surf = t.y + TableTopY;
            SpawnReal(vases[i % vases.Length], new Vector3(t.pos.x - 0.4f, surf + 0.06f, t.pos.z), 0, d);
            SpawnVeg(fl[i % fl.Length], new Vector3(t.pos.x - 0.4f, surf + 0.22f, t.pos.z), Random.Range(0f, 360f), d, 0.6f);
            // a served meal: a platter with a roast / loaves, plus a couple of ale mugs
            FoodPlate(new Vector3(t.pos.x + 0.35f, surf + 0.07f, t.pos.z + 0.15f), d, i);
            Tankard(new Vector3(t.pos.x + 0.2f, surf + 0.07f, t.pos.z - 0.28f), d);
            if (i % 2 == 0) Tankard(new Vector3(t.pos.x + 0.55f, surf + 0.07f, t.pos.z - 0.15f), d);
        }
        // a big roast platter + bread on the bar counter
        FoodPlate(new Vector3(-halfW + 1.7f, 1.15f + 0.07f, 1.6f), d, 3);

        // potted plants brightening the back corners
        SpawnVeg("Nature/Vegetation/Plants/rpgpp_lt_plant_01.prefab", new Vector3(-halfW + 1.0f, 0, backZ + 1.0f), 0, d);
        SpawnVeg("Nature/Vegetation/Plants/rpgpp_lt_plant_02.prefab", new Vector3(halfW - 1.0f, 0, backZ + 1.0f), 0, d);

        // extra banners along the side walls for warmth/colour
        SpawnReal("Banners/rpgpp_lt_banner_01a.prefab", new Vector3(-halfW + 0.35f, H1 - 0.4f, -2.5f), 90, d);
        SpawnReal("Banners/rpgpp_lt_banner_01b.prefab", new Vector3(halfW - 0.35f, H1 - 0.4f, 2.5f), -90, d);

        BuildPubSign(d);
    }

    // A distinctive hanging tavern sign over the entrance - an iron bracket, a
    // swinging carved board, and a glowing golden ale-mug emblem, so the pub
    // reads as a special landmark while staying in the village's wood/iron palette.
    static void BuildPubSign(Transform d)
    {
        var sign = new GameObject("PubSign").transform; sign.SetParent(d, false);
        sign.localPosition = new Vector3(halfW - 0.2f, H1 - 0.6f, frontZ - 0.6f);
        var iron = new Color(0.10f, 0.10f, 0.11f);
        var gold = new Color(1f, 0.82f, 0.30f);
        // wall bracket reaching out over the doorway
        Box("Bracket", new Vector3(-0.9f, 0.55f, 0), new Vector3(1.9f, 0.1f, 0.1f), iron, sign);
        Box("Brace", new Vector3(-0.2f, 0.2f, 0), new Vector3(0.1f, 0.8f, 0.1f), iron, sign, new Vector3(0, 0, 40));
        Box("Chain1", new Vector3(-1.75f, 0.35f, 0), new Vector3(0.05f, 0.45f, 0.05f), iron, sign);
        Box("Chain2", new Vector3(-0.15f, 0.35f, 0), new Vector3(0.05f, 0.45f, 0.05f), iron, sign);
        // the swinging board
        Box("Board", new Vector3(-0.95f, -0.05f, 0), new Vector3(1.8f, 1.0f, 0.1f), WoodL, sign);
        Box("BoardFrame", new Vector3(-0.95f, -0.05f, -0.03f), new Vector3(1.95f, 1.15f, 0.06f), iron, sign);
        // glowing ale-mug emblem (mug body + handle + foam)
        Glow("MugBody", new Vector3(-0.95f, -0.1f, -0.09f), new Vector3(0.6f, 0.6f, 0.05f), gold, gold * 1.6f, sign);
        Glow("MugHandle", new Vector3(-0.55f, -0.1f, -0.09f), new Vector3(0.22f, 0.4f, 0.05f), gold, gold * 1.6f, sign);
        Glow("MugFoam", new Vector3(-0.95f, 0.28f, -0.09f), new Vector3(0.66f, 0.18f, 0.05f), new Color(1f, 0.97f, 0.9f), new Color(1f, 0.95f, 0.85f) * 1.3f, sign);
        var l = AddLight("SignLight", new Vector3(-0.95f, 0.1f, -1.0f), new Color(1f, 0.8f, 0.45f), 2.0f, 5f, sign);
    }

    static void PlaceCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = new Vector3(0, 5.2f, -13.5f);
        cam.transform.rotation = Quaternion.Euler(15f, 0, 0);
    }
}
