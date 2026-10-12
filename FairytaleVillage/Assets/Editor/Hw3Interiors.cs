using UnityEditor;
using UnityEngine;

// HW3 interior design pass (part of Hw3Builder):
//   DecorateRoom      - House #1's room (HouseInterior scene): textured wood /
//                       plaster / stone, wainscoting, patterned rugs, a dining
//                       table with food, a made bed, an armchair by the fire,
//                       a kitchen shelf, windows with curtains, paintings,
//                       a tapestry, plants and firewood.
//   DecorateTreasury  - House #2 (the locked coin house): plank floor, panelled
//                       walls, gold hoards, an open treasure chest, torches,
//                       shields and gilded shelves.
public static partial class Hw3Builder
{
    const string ITEX = "Assets/Textures/Interior/";

    static Material TexMatI(string file, Color tint, float smooth = 0.1f)
    {
        var m = new Material(Lit());
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(ITEX + file);
        if (t != null) { m.SetTexture("_BaseMap", t); m.mainTexture = t; }
        m.color = tint; m.SetColor("_BaseColor", tint);
        m.SetFloat("_Smoothness", smooth);
        return m;
    }

    // Retexture an existing box, tiling the texture by its two largest sides
    // (same idea as PubBuilder.BoxT) so planks/stones keep a sensible size.
    static void Retex(Transform t, Material baseMat, float tpm)
    {
        if (t == null) return;
        var r = t.GetComponent<Renderer>();
        if (r == null) return;
        Vector3 s = t.lossyScale;
        float ax = Mathf.Abs(s.x), ay = Mathf.Abs(s.y), az = Mathf.Abs(s.z), a, b;
        if (ay <= ax && ay <= az) { a = ax; b = az; }
        else if (ax <= ay && ax <= az) { a = az; b = ay; }
        else { a = ax; b = ay; }
        var m = new Material(baseMat);
        m.mainTextureScale = new Vector2(Mathf.Max(1f, a * tpm), Mathf.Max(1f, b * tpm));
        r.sharedMaterial = m;
    }

    static GameObject BoxTex(string n, Transform p, Vector3 pos, Vector3 size, Material baseMat, float tpm, float yaw = 0f)
    {
        var g = BoxM(n, p, pos, size, baseMat);
        g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Retex(g.transform, baseMat, tpm);
        return g;
    }

    static GameObject Rug(string n, Transform p, Vector3 pos, Vector3 size, string tex, float yaw = 0f)
    {
        var g = BoxM(n, p, pos, size, TexMatI(tex, Color.white, 0.05f));
        g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    static void Candle(Transform p, Vector3 pos, bool withLight)
    {
        var c = new GameObject("Candle").transform; c.SetParent(p, false); c.localPosition = pos;
        CylM("Dish", c, new Vector3(0f, 0.01f, 0f), new Vector3(0.18f, 0.015f, 0.18f), BrassM);
        CylM("Wax", c, new Vector3(0f, 0.12f, 0f), new Vector3(0.08f, 0.11f, 0.08f), Wax);
        CylM("Flame", c, new Vector3(0f, 0.27f, 0f), new Vector3(0.05f, 0.07f, 0.05f), FlameM);
        if (withLight)
        {
            var l = AddLight("CandleLight", c, new Vector3(0f, 0.35f, 0f), new Color(1f, 0.72f, 0.4f), 1.2f, 3.5f);
            l.shadows = LightShadows.None;
            l.gameObject.AddComponent<FireFlicker>();
        }
        StripCols(c.gameObject);
    }

    static void PottedPlant(Transform p, Vector3 pos, float scale = 1f)
    {
        var pot = new GameObject("PottedPlant").transform; pot.SetParent(p, false); pot.localPosition = pos;
        pot.localScale = Vector3.one * scale;
        var terracotta = MatPBR(new Color(0.62f, 0.32f, 0.2f), 0f, 0.2f);
        CylM("Pot", pot, new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.25f, 0.5f), terracotta);
        CylM("Rim", pot, new Vector3(0f, 0.5f, 0f), new Vector3(0.58f, 0.04f, 0.58f), terracotta);
        CylM("Soil", pot, new Vector3(0f, 0.51f, 0f), new Vector3(0.48f, 0.02f, 0.48f), MatPBR(new Color(0.2f, 0.13f, 0.08f), 0f, 0.1f));
        Spawn("Nature/Vegetation/Bushes/rpgpp_lt_bush_01.prefab", pot, new Vector3(0f, 0.45f, 0f), 30f, 0.42f);
        Spawn("Nature/Vegetation/Flowers/rpgpp_lt_flower_02.prefab", pot, new Vector3(0.08f, 0.5f, 0.05f), 0f, 0.8f);
        StripCols(pot.gameObject);
        CylM("Hit", pot, new Vector3(0f, 0.4f, 0f), new Vector3(0.55f, 0.4f, 0.55f), terracotta).GetComponent<Renderer>().enabled = false;
    }

    // A wall window seen from inside: timber frame, glowing daylight panes with a
    // mullion cross, a sill with a flower box and two velvet curtains. `inward`
    // is the room-facing direction (unit X or Z).
    static void InteriorWindow(Transform p, Vector3 pos, Vector3 inward, float width = 1.4f, float height = 1.5f)
    {
        var w = new GameObject("Window").transform; w.SetParent(p, false); w.localPosition = pos;
        w.localRotation = Quaternion.LookRotation(inward, Vector3.up);   // local +Z = into the room
        var sky = new Color(0.72f, 0.86f, 1f);
        Glow("Daylight", w, new Vector3(0f, 0f, 0.02f), new Vector3(width, height, 0.04f), sky, sky * 1.6f);
        BoxM("FrameT", w, new Vector3(0f, height * 0.5f + 0.06f, 0.06f), new Vector3(width + 0.24f, 0.14f, 0.14f), Ebony);
        BoxM("FrameB", w, new Vector3(0f, -height * 0.5f - 0.06f, 0.06f), new Vector3(width + 0.24f, 0.14f, 0.14f), Ebony);
        BoxM("FrameL", w, new Vector3(-width * 0.5f - 0.06f, 0f, 0.06f), new Vector3(0.14f, height + 0.24f, 0.14f), Ebony);
        BoxM("FrameR", w, new Vector3(width * 0.5f + 0.06f, 0f, 0.06f), new Vector3(0.14f, height + 0.24f, 0.14f), Ebony);
        BoxM("MullionV", w, new Vector3(0f, 0f, 0.06f), new Vector3(0.07f, height, 0.07f), Ebony);
        BoxM("MullionH", w, new Vector3(0f, 0f, 0.06f), new Vector3(width, 0.07f, 0.07f), Ebony);
        BoxM("Sill", w, new Vector3(0f, -height * 0.5f - 0.16f, 0.16f), new Vector3(width + 0.4f, 0.08f, 0.34f), Oak);
        BoxM("FlowerBox", w, new Vector3(0f, -height * 0.5f - 0.04f, 0.2f), new Vector3(width * 0.7f, 0.16f, 0.18f), Oak);
        for (int i = 0; i < 5; i++)
        {
            var col = i % 2 == 0 ? new Color(0.85f, 0.22f, 0.3f) : new Color(0.95f, 0.8f, 0.25f);
            Ball("Bloom", w, new Vector3(-width * 0.3f + i * width * 0.15f, -height * 0.5f + 0.1f, 0.2f), 0.13f, MatPBR(col, 0f, 0.2f));
        }
        foreach (float side in new[] { -1f, 1f })
        {
            BoxM("Curtain", w, new Vector3(side * (width * 0.5f + 0.18f), -0.05f, 0.2f), new Vector3(0.36f, height + 0.5f, 0.06f), Velvet);
            BoxM("Tieback", w, new Vector3(side * (width * 0.5f + 0.18f), -0.2f, 0.24f), new Vector3(0.4f, 0.06f, 0.08f), BrassM);
        }
        CylM("Rod", w, new Vector3(0f, height * 0.5f + 0.3f, 0.2f), new Vector3(0.05f, width * 0.5f + 0.5f, 0.05f), BrassM, new Vector3(0f, 0f, 90f));
        var l = AddLight("WindowLight", w, new Vector3(0f, 0f, 0.9f), new Color(0.8f, 0.88f, 1f), 1.3f, 5f);
        l.shadows = LightShadows.None;
        StripCols(w.gameObject);
    }

    // ------------------------------------------------------------ House #1 --

    static void DecorateRoom(Transform root, float W, float D, float H, float hw, float hd)
    {
        // ---- surfaces: real textures instead of flat colours ----
        Retex(root.Find("Floor"), PubBuilder.MWoodFloor, 0.45f);
        Retex(root.Find("Ceiling"), PubBuilder.MWoodDark, 0.5f);
        foreach (var n in new[] { "Wall+X", "Wall-X", "Wall+Z", "Wall-Za", "Wall-Zb", "Lintel" })
            Retex(root.Find(n), PubBuilder.MPlaster, 0.35f);
        foreach (Transform c in root)
            if (c.name == "Post" || c.name == "Beam") Retex(c, PubBuilder.MWoodDark, 0.6f);

        // wood-panel wainscoting (replaces the plain dark base band) + a cap rail
        var ws = new GameObject("Wainscot").transform; ws.SetParent(root, false);
        foreach (var n in new[] { "Base+X", "Base-X", "Base+Z" })
        {
            var b = root.Find(n);
            if (b == null) continue;
            Retex(b, PubBuilder.MWoodWall, 0.9f);
            var cap = BoxM("CapRail", ws, b.localPosition + new Vector3(0f, 0.63f, 0f),
                           new Vector3(Mathf.Max(b.localScale.x, 0.5f), 0.07f, Mathf.Max(b.localScale.z, 0.5f)), Ebony);
            if (n == "Base+Z") cap.transform.localScale = new Vector3(W, 0.07f, 0.5f);
        }
        foreach (float side in new[] { -1f, 1f })
        {
            float len = hw - 1.5f, cx = side * (1.5f + len * 0.5f);
            BoxTex("Base-Z", ws, new Vector3(cx, 0.6f, -hd + 0.05f), new Vector3(len, 1.2f, 0.42f), PubBuilder.MWoodWall, 0.9f);
            BoxM("CapRail", ws, new Vector3(cx, 1.23f, -hd + 0.05f), new Vector3(len, 0.07f, 0.5f), Ebony);
        }

        // stone fireplace + dark mantel
        var fp = root.Find("Fireplace");
        if (fp != null)
        {
            Retex(fp.Find("Surround"), PubBuilder.MStone, 0.7f);
            Retex(fp.Find("Breast"), PubBuilder.MStone, 0.7f);
            Retex(fp.Find("Mantel"), PubBuilder.MWoodDark, 0.8f);
        }
        var logs = new GameObject("Firewood").transform; logs.SetParent(root, false);
        logs.localPosition = new Vector3(-2.35f, 0f, hd - 0.75f);
        var bark = MatPBR(new Color(0.33f, 0.22f, 0.13f), 0f, 0.15f);
        for (int row = 0, k = 0; row < 3; row++)
            for (int i = 0; i < 3 - row; i++, k++)
                CylM("Log", logs, new Vector3(-0.26f * (2 - row) * 0.5f + i * 0.26f, 0.13f + row * 0.22f, 0f),
                     new Vector3(0.24f, 0.45f, 0.24f), bark, new Vector3(90f, 0f, 0f));

        // ---- rugs: patterned, and no longer one giant velvet slab ----
        foreach (var n in new[] { "HallRug", "HallRugTrim", "Rug", "RugTrim" })
        {
            var r = root.Find(n);
            if (r != null) Object.DestroyImmediate(r.gameObject);
        }
        Rug("FireRug", root, new Vector3(0f, 0.02f, hd - 2.7f), new Vector3(3.2f, 0.03f, 2.6f), "rug_gold.png");
        Rug("Runner", root, new Vector3(0f, 0.02f, -hd + 3.2f), new Vector3(1.3f, 0.03f, 5.2f), "rug_runner.png");
        Rug("DiningRug", root, new Vector3(3.2f, 0.02f, -1.0f), new Vector3(2.8f, 0.03f, 3.8f), "rug_red.png", 90f);

        // ---- replace the undersized RPGPP table/chairs + back-corner clutter ----
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var c = root.GetChild(i);
            if (c.name.StartsWith("rpgpp_lt_table") || c.name.StartsWith("rpgpp_lt_chair") ||
                c.name.StartsWith("rpgpp_lt_crate") || c.name.StartsWith("rpgpp_lt_sack"))
                Object.DestroyImmediate(c.gameObject);
        }
        var furn = new GameObject("Furniture").transform; furn.SetParent(root, false);

        // dining table with benches, laid for a meal
        var table = new Vector3(3.2f, 0f, -1.0f);
        PubBuilder.TableWithBenches(table, furn);
        float ty = PubBuilder.TableTopY + 0.07f;
        PubBuilder.FoodPlate(table + new Vector3(-0.55f, ty, 0.05f), furn, 0);
        PubBuilder.FoodPlate(table + new Vector3(0.6f, ty, -0.1f), furn, 1);
        PubBuilder.Tankard(table + new Vector3(0.05f, ty - 0.01f, 0.3f), furn);
        PubBuilder.Tankard(table + new Vector3(-0.1f, ty - 0.01f, -0.32f), furn);
        Candle(furn, table + new Vector3(0.05f, ty - 0.02f, -0.02f), true);
        Spawn("Props/Containers/rpgpp_lt_vase_01.prefab", furn, table + new Vector3(0.95f, ty - 0.04f, 0.28f), 0f, 0.55f);
        Spawn("Nature/Vegetation/Flowers/rpgpp_lt_flower_01.prefab", furn, table + new Vector3(0.95f, ty + 0.28f, 0.28f), 0f, 0.55f);

        // a made bed against the +X wall, headboard at the wall
        var bed = new GameObject("Bed").transform; bed.SetParent(furn, false);
        bed.localPosition = new Vector3(hw - 1.35f, 0f, hd - 2.4f);
        BoxTex("Frame", bed, new Vector3(0f, 0.3f, 0f), new Vector3(2.2f, 0.3f, 1.6f), PubBuilder.MWoodDark, 1.2f);
        foreach (var lp in new[] { new Vector2(-1.02f, -0.72f), new Vector2(1.02f, -0.72f), new Vector2(-1.02f, 0.72f), new Vector2(1.02f, 0.72f) })
            BoxM("Leg", bed, new Vector3(lp.x, 0.1f, lp.y), new Vector3(0.14f, 0.2f, 0.14f), Ebony);
        BoxM("Mattress", bed, new Vector3(0f, 0.55f, 0f), new Vector3(2.05f, 0.22f, 1.45f), MatPBR(new Color(0.93f, 0.9f, 0.82f), 0f, 0.1f));
        BoxM("Quilt", bed, new Vector3(-0.28f, 0.68f, 0f), new Vector3(1.55f, 0.08f, 1.55f), TexMatI("quilt.png", Color.white, 0.05f));
        BoxM("QuiltFold", bed, new Vector3(0.45f, 0.7f, 0f), new Vector3(0.18f, 0.1f, 1.55f), TexMatI("quilt.png", new Color(0.9f, 0.9f, 0.9f), 0.05f));
        foreach (float pz in new[] { -0.36f, 0.36f })
            BoxM("Pillow", bed, new Vector3(0.78f, 0.74f, pz), new Vector3(0.42f, 0.16f, 0.6f), MatPBR(new Color(0.97f, 0.95f, 0.9f), 0f, 0.1f));
        BoxTex("Headboard", bed, new Vector3(1.1f, 0.85f, 0f), new Vector3(0.12f, 1.3f, 1.7f), PubBuilder.MWoodDark, 1.2f);
        BoxM("HeadboardCap", bed, new Vector3(1.1f, 1.53f, 0f), new Vector3(0.18f, 0.08f, 1.8f), BrassM);
        BoxTex("Footboard", bed, new Vector3(-1.1f, 0.5f, 0f), new Vector3(0.1f, 0.75f, 1.7f), PubBuilder.MWoodDark, 1.2f);
        // night stand with a candle + a book
        var ns = new GameObject("NightStand").transform; ns.SetParent(furn, false);
        ns.localPosition = new Vector3(hw - 0.6f, 0f, hd - 4.0f);
        BoxTex("Body", ns, new Vector3(0f, 0.32f, 0f), new Vector3(0.55f, 0.64f, 0.55f), PubBuilder.MWoodLite, 1.5f);
        BoxM("Drawer", ns, new Vector3(-0.28f, 0.42f, 0f), new Vector3(0.02f, 0.18f, 0.4f), Ebony);
        BoxM("Knob", ns, new Vector3(-0.3f, 0.42f, 0f), new Vector3(0.04f, 0.05f, 0.05f), BrassM);
        Candle(ns, new Vector3(0.05f, 0.64f, 0.1f), false);
        BoxM("Book", ns, new Vector3(0f, 0.67f, -0.13f), new Vector3(0.3f, 0.06f, 0.22f), MatPBR(new Color(0.18f, 0.25f, 0.45f), 0f, 0.2f));

        // an armchair angled at the fire, with a side table
        var ac = new GameObject("Armchair").transform; ac.SetParent(furn, false);
        ac.localPosition = new Vector3(2.35f, 0f, hd - 3.1f);
        ac.localRotation = Quaternion.Euler(0f, -55f, 0f);
        BoxTex("Base", ac, new Vector3(0f, 0.22f, 0f), new Vector3(0.95f, 0.44f, 0.9f), PubBuilder.MWoodDark, 1.5f);
        BoxM("Cushion", ac, new Vector3(0f, 0.5f, 0.02f), new Vector3(0.78f, 0.14f, 0.78f), Velvet);
        BoxM("Back", ac, new Vector3(0f, 0.95f, -0.38f), new Vector3(0.95f, 1.0f, 0.16f), Velvet);
        BoxM("BackFrame", ac, new Vector3(0f, 1.47f, -0.38f), new Vector3(1.0f, 0.08f, 0.2f), Ebony);
        foreach (float sx in new[] { -0.47f, 0.47f })
            BoxM("Arm", ac, new Vector3(sx, 0.66f, 0f), new Vector3(0.16f, 0.36f, 0.88f), Velvet);
        var st = new GameObject("SideTable").transform; st.SetParent(furn, false);
        st.localPosition = new Vector3(3.45f, 0f, hd - 2.3f);
        CylM("Top", st, new Vector3(0f, 0.7f, 0f), new Vector3(0.6f, 0.03f, 0.6f), Oak);
        CylM("Stem", st, new Vector3(0f, 0.35f, 0f), new Vector3(0.08f, 0.35f, 0.08f), Ebony);
        CylM("Foot", st, new Vector3(0f, 0.02f, 0f), new Vector3(0.4f, 0.02f, 0.4f), Ebony);
        PubBuilder.Tankard(new Vector3(0.1f, 0.72f, 0.05f), st);

        // kitchen dresser against the -X wall, near the door
        var dr = new GameObject("Dresser").transform; dr.SetParent(furn, false);
        dr.localPosition = new Vector3(-hw + 0.55f, 0f, -hd + 2.3f);
        BoxTex("Back", dr, new Vector3(-0.22f, 1.3f, 0f), new Vector3(0.06f, 2.6f, 2.2f), PubBuilder.MWoodWall, 1.2f);
        BoxTex("Cupboard", dr, new Vector3(0f, 0.45f, 0f), new Vector3(0.5f, 0.9f, 2.2f), PubBuilder.MWoodLite, 1.2f);
        foreach (float dz in new[] { -0.55f, 0.55f })
        {
            BoxM("DoorPanel", dr, new Vector3(0.26f, 0.45f, dz), new Vector3(0.02f, 0.7f, 0.95f), Oak);
            BoxM("Knob", dr, new Vector3(0.28f, 0.5f, dz * 0.2f), new Vector3(0.04f, 0.06f, 0.06f), BrassM);
        }
        foreach (float sy in new[] { 1.5f, 2.1f, 2.6f })
            BoxM("Shelf", dr, new Vector3(-0.05f, sy, 0f), new Vector3(0.36f, 0.05f, 2.2f), Oak);
        foreach (float sz in new[] { -1.08f, 1.08f })
            BoxM("Side", dr, new Vector3(-0.05f, 1.75f, sz), new Vector3(0.36f, 1.7f, 0.05f), Oak);
        Spawn("Props/Containers/rpgpp_lt_jug_01.prefab", dr, new Vector3(0f, 0.9f, -0.7f), 30f);
        Spawn("Props/Containers/rpgpp_lt_bowl_metal_01.prefab", dr, new Vector3(0f, 0.9f, 0.35f), 0f);
        PubBuilder.FoodPlate(new Vector3(0f, 0.92f, -0.1f), dr, 1);
        Spawn("Props/Containers/rpgpp_lt_vase_02.prefab", dr, new Vector3(-0.05f, 1.53f, -0.6f), 0f, 0.7f);
        Spawn("Props/Containers/rpgpp_lt_vase_03.prefab", dr, new Vector3(-0.05f, 1.53f, 0.55f), 0f, 0.7f);
        var bottleCols = new[] { new Color(0.18f, 0.36f, 0.2f), new Color(0.45f, 0.26f, 0.12f), new Color(0.25f, 0.2f, 0.4f) };
        for (int i = 0; i < 5; i++)
        {
            var bottle = MatPBR(bottleCols[i % 3], 0f, 0.85f);
            CylM("Bottle", dr, new Vector3(-0.05f, 2.28f, -0.8f + i * 0.4f), new Vector3(0.13f, 0.16f, 0.13f), bottle);
            CylM("Neck", dr, new Vector3(-0.05f, 2.49f, -0.8f + i * 0.4f), new Vector3(0.05f, 0.06f, 0.05f), bottle);
        }
        for (int i = 0; i < 4; i++)
            CylM("PlateOnEdge", dr, new Vector3(-0.12f, 2.82f, -0.75f + i * 0.5f), new Vector3(0.4f, 0.02f, 0.4f),
                 MatPBR(new Color(0.85f, 0.83f, 0.78f), 0f, 0.6f), new Vector3(0f, 0f, 80f));
        CylM("Cheese", dr, new Vector3(0f, 1.62f, 0f), new Vector3(0.36f, 0.07f, 0.36f), MatPBR(new Color(0.95f, 0.78f, 0.3f), 0f, 0.3f));
        Spawn("Props/Items/rpgpp_lt_broom_01.prefab", furn, new Vector3(-hw + 0.5f, 0f, -hd + 0.75f), 0f);
        StripCols(dr.gameObject);
        BoxM("DresserHit", dr, new Vector3(0f, 1.3f, 0f), new Vector3(0.55f, 2.6f, 2.2f), Oak).GetComponent<Renderer>().enabled = false;

        // coat rack by the door
        Spawn("Props/Hangers/rpgpp_lt_hanger_clothes_01.prefab", furn, new Vector3(2.6f, 0f, -hd + 0.6f), 0f);

        // plants either side of the doorway
        PottedPlant(furn, new Vector3(-1.95f, 0f, -hd + 0.55f));
        PottedPlant(furn, new Vector3(1.95f, 0f, -hd + 0.55f));
        PottedPlant(furn, new Vector3(hw - 0.6f, 0f, -0.1f), 0.9f);

        // ---- walls: windows, paintings, a tapestry ----
        InteriorWindow(root, new Vector3(hw - 0.17f, 2.35f, 1.1f), Vector3.left);
        InteriorWindow(root, new Vector3(4.2f, 2.35f, hd - 0.17f), Vector3.back);
        InteriorWindow(root, new Vector3(-hw + 0.17f, 2.35f, -0.9f), Vector3.right);

        int k2 = 0;
        foreach (Transform c in root)
            if (c.name == "Canvas")
            {
                c.GetComponent<Renderer>().sharedMaterial =
                    TexMatI(k2++ == 0 ? "painting_day.png" : "painting_dusk.png", Color.white, 0.25f);
                c.localRotation = Quaternion.Euler(0f, 180f, 0f);   // face the room, not the wall
            }

        var tp = new GameObject("Tapestry").transform; tp.SetParent(root, false);
        // on the +Z wall above the chest
        tp.localPosition = new Vector3(-4.3f, 2.5f, hd - 0.2f);
        BoxM("Cloth", tp, Vector3.zero, new Vector3(1.4f, 2.0f, 0.04f), TexMatI("tapestry.png", Color.white, 0.05f));
        CylM("Rod", tp, new Vector3(0f, 1.08f, -0.03f), new Vector3(0.05f, 0.85f, 0.05f), BrassM, new Vector3(0f, 0f, 90f));
        foreach (float ex in new[] { -0.85f, 0.85f })
            Ball("RodEnd", tp, new Vector3(ex, 1.08f, -0.03f), 0.1f, BrassM);
        StripCols(tp.gameObject);
        foreach (Transform c in root)
            if (c.name == "Window" || c.name == "Tapestry") StripCols(c.gameObject);
    }

    // Garden planting placed around the cottages (yards, the strip between
    // them) can land inside a house footprint - a tree growing through the
    // treasury. Remove any tree/bush/flower whose base is inside either house
    // (trees get a wider margin so their canopy doesn't poke through walls).
    static void ClearPlantsInsideHouses()
    {
        var houses = new[] { ("House_1_Enterable", H1W, H1D), ("House_2_Locked", 8f, 8f) };
        var kill = new System.Collections.Generic.List<GameObject>();
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                bool tree = n.StartsWith("rpgpp_lt_tree");
                if (!tree && !n.StartsWith("rpgpp_lt_bush") && !n.StartsWith("rpgpp_lt_flower")) continue;
                if (t.parent != null && t.parent.name.StartsWith("rpgpp_lt_")) continue;   // part of another prefab
                // trees/bushes just outside still poke their canopy through the walls
                float margin = tree ? 2.8f : n.StartsWith("rpgpp_lt_bush") ? 1.4f : 0.3f;
                foreach (var (name, w, d) in houses)
                {
                    var h = GameObject.Find(name);
                    if (h == null || t.IsChildOf(h.transform)) continue;
                    Vector3 lp = h.transform.InverseTransformPoint(t.position);
                    if (Mathf.Abs(lp.x) < w * 0.5f + margin && Mathf.Abs(lp.z) < d * 0.5f + margin)
                    { kill.Add(t.gameObject); break; }
                }
            }
        foreach (var g in kill) if (g != null) Object.DestroyImmediate(g);
        Debug.Log($"ClearPlantsInsideHouses removed {kill.Count} plant(s) from inside the cottages.");
    }

    // ------------------------------------------------------------ House #2 --

    static void DecorateTreasury(Transform house)
    {
        float hw = CW * 0.5f, hd = CD * 0.5f, inner = hw - CT, fy = 0.12f;
        var tr = new GameObject("Treasury").transform; tr.SetParent(house, false);

        // plank floor + panelled walls (wainscot) + cap rail
        var fl = BoxTex("PlankFloor", tr, new Vector3(0f, fy + 0.01f, 0f), new Vector3(inner * 2f, 0.02f, inner * 2f), PubBuilder.MWoodFloor, 0.45f);
        Object.DestroyImmediate(fl.GetComponent<Collider>());
        BoxTex("Panel+X", tr, new Vector3(inner - 0.03f, fy + 0.6f, 0f), new Vector3(0.06f, 1.2f, inner * 2f), PubBuilder.MWoodWall, 0.9f);
        BoxTex("Panel+Z", tr, new Vector3(0f, fy + 0.6f, inner - 0.03f), new Vector3(inner * 2f, 1.2f, 0.06f), PubBuilder.MWoodWall, 0.9f);
        BoxTex("Panel-Z", tr, new Vector3(0f, fy + 0.6f, -inner + 0.03f), new Vector3(inner * 2f, 1.2f, 0.06f), PubBuilder.MWoodWall, 0.9f);
        BoxM("Cap+X", tr, new Vector3(inner - 0.05f, fy + 1.23f, 0f), new Vector3(0.1f, 0.06f, inner * 2f), Ebony);
        BoxM("Cap+Z", tr, new Vector3(0f, fy + 1.23f, inner - 0.05f), new Vector3(inner * 2f, 0.06f, 0.1f), Ebony);
        BoxM("Cap-Z", tr, new Vector3(0f, fy + 1.23f, -inner + 0.05f), new Vector3(inner * 2f, 0.06f, 0.1f), Ebony);

        // a plank ceiling with beams (hides the roof underside + chimney base)
        BoxTex("Ceiling", tr, new Vector3(0f, CH - 0.06f, 0f), new Vector3(inner * 2f, 0.1f, inner * 2f), PubBuilder.MWoodDark, 0.5f);
        foreach (float bx in new[] { -2.2f, 0f, 2.2f })
            BoxTex("Beam", tr, new Vector3(bx, CH - 0.25f, 0f), new Vector3(0.28f, 0.28f, inner * 2f), PubBuilder.MWoodDark, 0.6f);

        Rug("TreasureRug", tr, new Vector3(0.4f, fy + 0.03f, 0f), new Vector3(3.4f, 0.02f, 3.4f), "rug_gold.png");

        var gold = MatPBRE(new Color(1f, 0.78f, 0.25f), 1f, 0.85f, new Color(0.5f, 0.33f, 0.05f));
        var gems = new[] { new Color(0.9f, 0.1f, 0.15f), new Color(0.1f, 0.75f, 0.3f), new Color(0.2f, 0.4f, 1f), new Color(0.7f, 0.2f, 0.9f) };
        var rng = new System.Random(21);

        // gold hoards heaped in the two back corners, with loose coins and gems
        foreach (float sz in new[] { -1f, 1f })
        {
            var h = new GameObject("GoldHoard").transform; h.SetParent(tr, false);
            h.localPosition = new Vector3(inner - 0.85f, fy, sz * (inner - 0.85f));
            var mound = Ball("Mound", h, Vector3.zero, 1f, gold);
            mound.transform.localScale = new Vector3(1.6f, 1.3f, 1.6f);
            Ball("Mound2", h, new Vector3(-0.35f, 0f, -sz * 0.4f), 1f, gold).transform.localScale = new Vector3(1.1f, 0.85f, 1.1f);
            for (int i = 0; i < 14; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = 0.5f + (float)rng.NextDouble() * 0.6f;
                CylM("LooseCoin", h, new Vector3(Mathf.Cos(a) * r, 0.03f + (float)rng.NextDouble() * 0.2f, Mathf.Sin(a) * r),
                     new Vector3(0.22f, 0.02f, 0.22f), gold,
                     new Vector3((float)rng.NextDouble() * 40f - 20f, 0f, (float)rng.NextDouble() * 40f - 20f));
            }
            for (int i = 0; i < 4; i++)
            {
                var gc = gems[(i + (sz > 0 ? 2 : 0)) % gems.Length];
                Ball("Gem", h, new Vector3(-0.3f + i * 0.2f, 0.6f + (i % 2) * 0.06f, 0.1f * (i - 1.5f)), 0.13f,
                     MatPBRE(gc, 0f, 0.95f, gc * 0.8f));
            }
            CylM("Goblet", h, new Vector3(0.1f, 0.8f, 0f), new Vector3(0.14f, 0.1f, 0.14f), gold);
            CylM("GobletStem", h, new Vector3(0.1f, 0.66f, 0f), new Vector3(0.04f, 0.06f, 0.04f), gold);
            StripCols(h.gameObject);
            var hit = new GameObject("HoardHit"); hit.transform.SetParent(h, false);
            var sc = hit.AddComponent<SphereCollider>(); sc.radius = 0.75f;
        }

        // an open treasure chest against the back wall, lid thrown back, gold inside
        var ch = new GameObject("TreasureChest").transform; ch.SetParent(tr, false);
        ch.localPosition = new Vector3(inner - 0.5f, fy, 0f);
        ch.localRotation = Quaternion.Euler(0f, -90f, 0f);   // faces the door (-X)
        BoxTex("Body", ch, new Vector3(0f, 0.32f, 0f), new Vector3(1.2f, 0.64f, 0.75f), PubBuilder.MWoodDark, 1.5f);
        foreach (float bx in new[] { -0.45f, 0f, 0.45f })
            BoxM("Band", ch, new Vector3(bx, 0.32f, 0f), new Vector3(0.07f, 0.66f, 0.77f), IronM);
        BoxM("GoldFill", ch, new Vector3(0f, 0.6f, 0f), new Vector3(1.1f, 0.12f, 0.66f), gold);
        for (int i = 0; i < 7; i++)
            CylM("TopCoin", ch, new Vector3(-0.45f + i * 0.15f, 0.68f, (float)rng.NextDouble() * 0.4f - 0.2f),
                 new Vector3(0.2f, 0.02f, 0.2f), gold, new Vector3(20f, 0f, (i % 2 == 0 ? 15f : -15f)));
        var lid = new GameObject("Lid").transform; lid.SetParent(ch, false);
        lid.localPosition = new Vector3(0f, 0.64f, 0.37f);
        lid.localRotation = Quaternion.Euler(-105f, 0f, 0f);
        BoxTex("LidPanel", lid, new Vector3(0f, 0f, -0.37f), new Vector3(1.2f, 0.12f, 0.75f), PubBuilder.MWoodDark, 1.5f);
        Glow("TreasureGlow", ch, new Vector3(0f, 0.67f, 0f), new Vector3(1.0f, 0.02f, 0.6f), new Color(1f, 0.85f, 0.4f), new Color(1f, 0.7f, 0.2f) * 1.5f);
        StripCols(ch.gameObject);
        BoxM("ChestHit", ch, new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.7f, 0.75f), Oak).GetComponent<Renderer>().enabled = false;

        // wall torches either side of the chest
        foreach (float sz in new[] { -1.6f, 1.6f })
        {
            var t = new GameObject("WallTorch").transform; t.SetParent(tr, false);
            t.localPosition = new Vector3(inner - 0.12f, fy + 2.1f, sz);
            BoxM("Bracket", t, new Vector3(0.04f, 0f, 0f), new Vector3(0.1f, 0.3f, 0.1f), IronM);
            CylM("Stick", t, new Vector3(-0.12f, 0.12f, 0f), new Vector3(0.08f, 0.25f, 0.08f), Oak, new Vector3(0f, 0f, 20f));
            Glow("Flame", t, new Vector3(-0.2f, 0.42f, 0f), new Vector3(0.16f, 0.26f, 0.16f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.5f, 0.15f) * 4f);
            var l = AddLight("TorchLight", t, new Vector3(-0.4f, 0.4f, 0f), new Color(1f, 0.66f, 0.32f), 2.4f, 6f);
            l.shadows = LightShadows.None;
            l.gameObject.AddComponent<FireFlicker>();
            StripCols(t.gameObject);
        }
        var goldLight = AddLight("GoldBounce", tr, new Vector3(0.5f, 2.6f, 0f), new Color(1f, 0.82f, 0.45f), 1.6f, 7f);
        goldLight.shadows = LightShadows.None;

        // heraldic shields with crossed swords on the side walls
        foreach (float sz in new[] { -1f, 1f })
        {
            var s = new GameObject("Shield").transform; s.SetParent(tr, false);
            s.localPosition = new Vector3(0.9f, fy + 2.2f, sz * (inner - 0.08f));
            s.localRotation = Quaternion.LookRotation(new Vector3(0f, 0f, -sz), Vector3.up);
            foreach (float r in new[] { 40f, -40f })
            {
                var blade = BoxM("Sword", s, new Vector3(0f, 0f, -0.02f), new Vector3(0.07f, 1.5f, 0.02f), MatPBR(new Color(0.75f, 0.77f, 0.8f), 1f, 0.8f));
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, r);
                var hilt = BoxM("Hilt", s, Quaternion.Euler(0f, 0f, r) * new Vector3(0f, -0.62f, -0.03f), new Vector3(0.3f, 0.06f, 0.04f), BrassM);
                hilt.transform.localRotation = Quaternion.Euler(0f, 0f, r);
            }
            var face = CylM("Face", s, new Vector3(0f, 0f, 0.06f), new Vector3(0.75f, 0.03f, 0.75f),
                            MatPBR(sz > 0 ? new Color(0.55f, 0.1f, 0.12f) : new Color(0.12f, 0.2f, 0.45f), 0f, 0.35f), new Vector3(90f, 0f, 0f));
            CylM("Rim", s, new Vector3(0f, 0f, 0.05f), new Vector3(0.83f, 0.025f, 0.83f), BrassM, new Vector3(90f, 0f, 0f));
            Ball("Boss", s, new Vector3(0f, 0f, 0.1f), 0.18f, gold);
            BoxM("Stripe", s, new Vector3(0f, 0f, 0.085f), new Vector3(0.12f, 0.7f, 0.02f), gold);
            StripCols(s.gameObject);
        }

        // gilded shelves with goblets + gems near the front
        foreach (float sz in new[] { -1f, 1f })
        {
            var sh = new GameObject("GoldShelf").transform; sh.SetParent(tr, false);
            sh.localPosition = new Vector3(-1.4f, fy + 1.75f, sz * (inner - 0.2f));
            BoxM("Board", sh, Vector3.zero, new Vector3(1.8f, 0.06f, 0.36f), Oak);
            BoxM("Trim", sh, new Vector3(0f, -0.05f, -sz * 0.17f), new Vector3(1.8f, 0.04f, 0.03f), BrassM);
            for (int i = 0; i < 3; i++)
            {
                CylM("Goblet", sh, new Vector3(-0.6f + i * 0.6f, 0.2f, 0f), new Vector3(0.16f, 0.1f, 0.16f), gold);
                CylM("Stem", sh, new Vector3(-0.6f + i * 0.6f, 0.07f, 0f), new Vector3(0.05f, 0.05f, 0.05f), gold);
                var gc = gems[(i + (sz > 0 ? 1 : 3)) % gems.Length];
                Ball("Gem", sh, new Vector3(-0.3f + i * 0.6f, 0.1f, 0.02f), 0.12f, MatPBRE(gc, 0f, 0.95f, gc * 0.8f));
            }
            StripCols(sh.gameObject);
        }

        // one barrel tucked into a front corner (kept sparse so the room stays open)
        Spawn("Props/Containers/rpgpp_lt_barrel_01.prefab", tr, new Vector3(-inner + 0.6f, fy, inner - 0.6f), 0f, 1.25f);
    }
}
