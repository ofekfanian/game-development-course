using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;

// ============================================================================
//  Homework 3 - builds the whole playable game from code.
//
//  Menu:  Tools > Homework 3 > Build Playable HW3
//
//  It produces two scenes and wires them into Build Settings:
//    Assets/Scenes/Village.unity        - the real-asset village + a walkable
//                                         third-person player, loose coins, and
//                                         the two special houses.
//    Assets/Scenes/HouseInterior.unity  - House #1 as its OWN scene: a room with
//                                         a chest that opens on SPACE and holds a
//                                         key the player can take.
//
//  Requirements covered:
//   - House #1 is a separate scene; coins carried in / collected inside persist
//     (GameState is a static class -> survives LoadScene).
//   - Coming back, the player is placed next to House #1 facing forward
//     (VillageSpawns).
//   - Chest opens with SPACE when close + looking (Chest + InteractUtil); key
//     inside is takeable.
//   - House #2's door only opens if the player holds a key, else shows
//     "You need a key to open this door" (LockedDoor).
//   - House #2 is full of gold coins and is NOT a separate scene.
//
//  Controls: WASD / arrows move, mouse looks, SPACE interacts, Esc frees cursor.
// ============================================================================
public static partial class Hw3Builder
{
    const string MIX = "Assets/Mixamo/";
    const string SAMPLE = "Assets/Scenes/SampleScene.unity";       // the HW2 world (village + pub)
    const string VILLAGE = "Assets/Scenes/Village.unity";
    const string INTERIOR = "Assets/Scenes/HouseInterior.unity";

    // The two special houses sit on the LEFT bank, just behind the house row,
    // near the bridge - well clear of the pub (which HW2 put at world x=40).
    const float HouseYaw = 180f;                                   // door faces +X, toward the village
    // separate-scene house: bigger than House2 (it wraps the interior room), so it
    // sits further back/aside - its door is still at world x = -28
    static readonly Vector3 House1 = new Vector3(-28f - H1W * 0.5f, 0f, 11f);
    static readonly Vector3 House2 = new Vector3(-32f, 0f, -7f);   // locked-door coin house
    // start in front of the cottages' yards, past the village house rows
    static readonly Vector3 PlayerStart = new Vector3(-20f, 0f, 0f);

    // ---- HW2 tavern palette (kept identical to PubBuilder so the new houses
    //      read as part of the same world, not a different art style) -----------
    static readonly Color Stone   = new Color(0.55f, 0.53f, 0.49f);
    static readonly Color Wood    = new Color(0.42f, 0.28f, 0.16f);
    static readonly Color WoodL   = new Color(0.52f, 0.37f, 0.22f);
    static readonly Color Plaster = new Color(0.87f, 0.76f, 0.55f);
    static readonly Color Timber  = new Color(0.26f, 0.17f, 0.10f);
    static readonly Color RoofCol = new Color(0.42f, 0.16f, 0.12f);
    static readonly Color RugCol  = new Color(0.55f, 0.14f, 0.12f);
    static readonly Color Pane    = new Color(0.10f, 0.12f, 0.14f);

    const string RPGPP = "Assets/RPGPP_LT/Prefabs/";

    static GameObject Spawn(string relPath, Transform parent, Vector3 localPos, float yaw, float scale = 1f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RPGPP + relPath);
        if (prefab == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        if (scale != 1f) go.transform.localScale = Vector3.one * scale;
        return go;
    }

    [MenuItem("Tools/Homework 3/Build Playable HW3")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var ctrl = EnsurePlayerController();

        BuildVillageScene(ctrl);
        BuildInteriorScene(ctrl);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(VILLAGE, true),
            new EditorBuildSettingsScene(INTERIOR, true),
        };

        EditorSceneManager.OpenScene(VILLAGE, OpenSceneMode.Single);
        Selection.activeObject = null;
        Debug.Log("HW3 built.  Open Village.unity and press Play.\n" +
                  "WASD/arrows move, mouse looks, SPACE interacts, Esc frees the cursor.");
    }

    // ---------------------------------------------------------------- Village --

    static void BuildVillageScene(RuntimeAnimatorController ctrl)
    {
        // Start from the HW2 world (village + pub + Mixamo NPCs + audio) and save
        // it out as Village.unity so HW1/HW2's SampleScene stays untouched.
        var scene = EditorSceneManager.OpenScene(SAMPLE, OpenSceneMode.Single);
        EditorSceneManager.SaveScene(scene, VILLAGE);

        // drop the HW2 fly-through camera - the player brings its own
        DestroyAllCameras();
        // remove old HW3 objects if this is a rebuild
        foreach (var n in new[] { "Player", "VillageSpawns", "House_1_Enterable", "House_2_Locked",
                                  "House_2_Locked_Coins", "VillageCoins" })
        {
            var g = GameObject.Find(n);
            if (g != null) Object.DestroyImmediate(g);
        }
        // clear a wide swath of trees around the two cottages + their yards so
        // nothing blocks the approach or peeks through the gap (radius kept < 15
        // so the left-bank village house row survives)
        // (also "Houses" + "Beautify": the re-spaced HW2 village puts a left-bank
        // house and its flower garden right at z=0 - on top of PlayerStart)
        ClearAround(new[] { "Nature", "Houses", "Beautify" },
                    new[] { House1, House2,
                            new Vector3(-32f, 0f, 0f),          // gap between the cottages
                            new Vector3(-22f, 0f, 0f) },        // approach lane
                    14f);
        // and a small tidy-up right where the player spawns
        ClearAround(new[] { "Nature", "Fences", "Market", "Beautify" }, new[] { PlayerStart }, 7f);

        BuildPlayer(ctrl, PlayerStart, 270f);            // face the cottages (toward -X)

        var spawns = new GameObject("VillageSpawns").AddComponent<VillageSpawns>();
        spawns.startPos = PlayerStart;
        spawns.startYaw = 270f;
        spawns.houseReturnPos = House1 + new Vector3(H1W * 0.5f + 1.2f, 0f, 0f); // on the doorstep
        spawns.houseReturnYaw = 90f;                     // face +X = away from the house, into the village

        // House #1 - a bigger cottage whose walls wrap the HouseInterior room.
        // HouseEntrance loads that scene additively INTO it; SPACE opens the door
        // and the player just walks in.
        var c1 = WithCottageSize(H1W, H1D, H1H, () => Cottage("House_1_Enterable", House1, HouseYaw, solidDoor: true));
        var entrance = new GameObject("Entrance");
        entrance.transform.SetParent(c1.transform, false);
        entrance.transform.localPosition = new Vector3(-H1W * 0.5f - 0.3f, 1.2f, 0f);   // just outside the doorway
        var he = entrance.AddComponent<HouseEntrance>();
        he.doorPivot = c1.transform.Find("DoorPivot");
        he.creak = DoorCreak;
        WithCottageSize(H1W, H1D, H1H, () => { DressCottage("House_1_Enterable", House1, HouseYaw); return 0; });

        // House #2 - locked door + a room packed with coins (same scene)
        BuildLockedCoinHouse("House_2_Locked", House2, HouseYaw);
        DressCottage("House_2_Locked", House2, HouseYaw);

        // a little garden filling the open ground between the two cottages, kept
        // off the z=0 walking lane (spawn -> gap -> either door)
        BeautifyBetweenCottages();

        // loose coins the player picks up around the start / between the cottages
        // (kept in the open central ground so collecting them doesn't force a
        // weave into a fence or wall)
        ScatterLine("VillageCoins", 12, new Vector3(-19f, 1f, 3.5f), new Vector3(-26f, 1f, -3.5f), 2.6f);

        // FINAL PASS: strip anything solid that ended up sitting on the lines the
        // player actually walks (spawn -> each door, and the return spot -> the
        // locked door). Whatever the dressing scatters, the routes stay walkable.
        ClearCorridors();
        ClearPlantsInsideHouses();
        RampUpBridge();

        // the user's real sounds: player footsteps + pub fire/crowd; no music
        Hw3Audio.AddVillageAudio();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, VILLAGE);
    }

    // -------------------------------------------------------------- Interior --

    static void BuildInteriorScene(RuntimeAnimatorController ctrl)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, INTERIOR);

        AddSun(0.5f);
        BuildRoom();

        // just inside the front door, facing into the room - as if the player
        // has just walked through it from the village
        BuildPlayer(ctrl, new Vector3(0f, 0f, -4.4f), 0f);
        BuildChest(new Vector3(-3.5f, 0f, 4f));

        // exit at the doorway: a plank door frame + a walk-through trigger volume
        Box("ExitFrameL", null, new Vector3(-1.35f, 1.3f, -5.85f), new Vector3(0.3f, 2.8f, 0.4f), Timber);
        Box("ExitFrameR", null, new Vector3(1.35f, 1.3f, -5.85f), new Vector3(0.3f, 2.8f, 0.4f), Timber);
        // the leaf hangs on a hinge at the left jamb; +Y swings it outward (-Z)
        var exitPivot = new GameObject("ExitDoorPivot").transform;
        exitPivot.position = new Vector3(-1.2f, 0f, -5.85f);
        Box("ExitLeaf", exitPivot, new Vector3(1.2f, 1.3f, 0f), new Vector3(2.4f, 2.6f, 0.14f), WoodL);
        Box("ExitHandle", exitPivot, new Vector3(2.05f, 1.15f, 0.12f), new Vector3(0.12f, 0.12f, 0.12f), Stone);
        var exit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        exit.name = "ExitDoorway";
        exit.transform.position = new Vector3(0f, 1.1f, -5.4f);
        exit.transform.localScale = new Vector3(2.3f, 2.2f, 0.5f);
        exit.GetComponent<MeshRenderer>().enabled = false;
        var exitCol = exit.GetComponent<BoxCollider>();
        exitCol.isTrigger = true;
        // a patch of garden outside, seen through the doorway once the door opens
        Box("OutsideGrass", null, new Vector3(0f, -0.05f, -13f), new Vector3(30f, 0.1f, 14f), new Color(0.42f, 0.53f, 0.30f));
        Box("OutsidePath", null, new Vector3(0f, -0.02f, -9f), new Vector3(2.2f, 0.06f, 6f), new Color(0.62f, 0.52f, 0.38f));
        var hx = exit.AddComponent<HouseExit>();   // (only used when this scene is played on its own)
        hx.doorPivot = exitPivot;
        hx.creak = DoorCreak;

        // a few coins to collect inside - proves they survive the trip home
        ScatterLine("InteriorCoins", 5, new Vector3(-4.5f, 1f, -1f), new Vector3(-2.5f, 1f, 2.5f), 1.2f);

        // footsteps + crackling fireplace, and the same post-processing look as
        // the village (tonemapping, bloom on the fire, colour grade, vignette)
        Hw3Audio.AddInteriorAudio();
        ScenePolish.ApplyPostFXOnly();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, INTERIOR);
    }

    // ---------------------------------------------------------------- Player --

    static GameObject BuildPlayer(RuntimeAnimatorController ctrl, Vector3 pos, float yaw)
    {
        // a real clothed + faced Mixamo character (same as the pub cast); the bare
        // "Breathing Idle" mannequin + code-built clothing is only the fallback
        var real = AssetDatabase.LoadAssetAtPath<GameObject>(MIX + "Characters/Char2.fbx");
        var model = real != null ? real : AssetDatabase.LoadAssetAtPath<GameObject>(MIX + "Breathing Idle.fbx");
        GameObject go = model != null
            ? (GameObject)Object.Instantiate(model)
            : GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Player";
        go.tag = "Player";
        go.transform.SetPositionAndRotation(pos + Vector3.up * 0.15f, Quaternion.Euler(0f, yaw, 0f));
        if (real == null) DressMixamoModel(go);

        var anim = go.GetComponent<Animator>();
        if (anim == null) anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;

        var cc = go.GetComponent<CharacterController>();
        if (cc == null) cc = go.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.stepOffset = 0.35f;
        cc.slopeLimit = 50f;

        var rig = new GameObject("CameraRig").transform;
        rig.SetParent(go.transform, false);
        rig.localPosition = new Vector3(0f, 1.6f, 0f);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(rig, false);
        camGo.transform.localPosition = new Vector3(0f, 0.35f, -3.4f);
        var cam = camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        // render the Global Volume post-processing (bloom, grading, vignette)
        var camData = cam.GetUniversalAdditionalCameraData();
        if (camData != null) camData.renderPostProcessing = true;

        var tpc = go.AddComponent<ThirdPersonController>();
        tpc.cameraRig = rig;
        tpc.cam = cam;
        tpc.camDistance = 3.4f;
        tpc.camHeight = 0.35f;
        tpc.moveSpeed = 3.2f;       // a brisker walk; the walk clip is sped up to match
        tpc.walkAnimSpeed = 1.45f;

        go.AddComponent<HudUI>();
        return go;
    }

    static RuntimeAnimatorController EnsurePlayerController()
    {
        // The HW2 Mixamo controller already has Idle/Walk driven by "Speed" - reuse it.
        var existing = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MIX + "PubAnimator.controller");
        if (existing != null) return existing;

        foreach (var n in new[] { "Breathing Idle", "Walking Animation" })
        {
            var imp = AssetImporter.GetAtPath(MIX + n + ".fbx") as ModelImporter;
            if (imp != null && imp.animationType != ModelImporterAnimationType.Human)
            {
                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.SaveAndReimport();
            }
        }

        const string cpath = "Assets/HW3/PlayerAnimator.controller";
        AssetDatabase.DeleteAsset(cpath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(cpath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        var sm = ctrl.layers[0].stateMachine;
        var idle = sm.AddState("Idle"); idle.motion = LoadClip("Breathing Idle");
        var walk = sm.AddState("Walk"); walk.motion = LoadClip("Walking Animation");
        sm.defaultState = idle;

        var toWalk = sm.AddAnyStateTransition(walk);
        toWalk.hasExitTime = false; toWalk.duration = 0.12f; toWalk.canTransitionToSelf = false;
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var toIdle = sm.AddAnyStateTransition(idle);
        toIdle.hasExitTime = false; toIdle.duration = 0.12f; toIdle.canTransitionToSelf = false;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        AssetDatabase.SaveAssets();
        return ctrl;
    }

    static AnimationClip LoadClip(string name)
    {
        string path = MIX + name + ".fbx";
        foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
        {
            var c = o as AnimationClip;
            if (c != null && !c.name.StartsWith("__preview")) return c;
        }
        return null;
    }

    // --------------------------------------------------------------- Chest ----

    static void BuildChest(Vector3 pos)
    {
        var root = new GameObject("Chest").transform;
        root.position = pos;
        // the player walks up from -Z (the room's door side): turn the chest so
        // its lock/front faces them and the hinged lid swings open AWAY from them
        root.rotation = Quaternion.Euler(0f, 180f, 0f);

        // --- raised display dais: velvet rug + brass-edged stone plinth ---------
        BoxM("DaisRug", root, new Vector3(0f, 0.02f, 0f), new Vector3(4.6f, 0.04f, 4.6f), Velvet);
        BoxM("DaisRugTrim", root, new Vector3(0f, 0.015f, 0f), new Vector3(5.0f, 0.03f, 5.0f), BrassM);
        CylM("Plinth", root, new Vector3(0f, 0.15f, 0f), new Vector3(2.7f, 0.15f, 2.7f), StoneP);
        CylM("PlinthCap", root, new Vector3(0f, 0.30f, 0f), new Vector3(2.45f, 0.04f, 2.45f), BrassM);
        CylM("Cloth", root, new Vector3(0f, 0.32f, 0f), new Vector3(2.2f, 0.06f, 2.2f), Velvet);

        var body = new GameObject("Coffer").transform;
        body.SetParent(root, false);
        body.localPosition = new Vector3(0f, 0.35f, 0f);

        // --- oak body with plank seams ---------------------------------------
        BoxM("Body", body, new Vector3(0f, 0.40f, 0f), new Vector3(1.7f, 0.80f, 1.12f), Oak);
        for (int i = -1; i <= 1; i++)
            BoxM("Seam", body, new Vector3(i * 0.5f, 0.40f, 0.575f), new Vector3(0.04f, 0.74f, 0.03f), Ebony);
        // iron corner brackets
        foreach (float sx in new[] { -0.83f, 0.83f })
            foreach (float sz in new[] { -0.54f, 0.54f })
                BoxM("Corner", body, new Vector3(sx, 0.40f, sz), new Vector3(0.13f, 0.86f, 0.13f), IronM);
        // vertical iron straps + brass rivets + a low strap
        foreach (float sx in new[] { -0.5f, 0.5f })
        {
            BoxM("Strap", body, new Vector3(sx, 0.40f, 0f), new Vector3(0.17f, 0.84f, 1.2f), IronM);
            foreach (float ry in new[] { 0.12f, 0.68f })
                Ball("Rivet", body, new Vector3(sx, ry, 0.6f), 0.09f, BrassM);
        }
        BoxM("StrapLow", body, new Vector3(0f, 0.06f, 0f), new Vector3(1.8f, 0.12f, 1.22f), IronM);
        // iron ball feet
        foreach (float sx in new[] { -0.72f, 0.72f })
            foreach (float sz in new[] { -0.44f, 0.44f })
                Ball("Foot", body, new Vector3(sx, 0.0f, sz), 0.17f, IronM);

        // --- ornate brass lock plate + hasp + keyhole ------------------------
        BoxM("LockPlate", body, new Vector3(0f, 0.5f, 0.60f), new Vector3(0.52f, 0.52f, 0.05f), BrassM);
        BoxM("LockInlay", body, new Vector3(0f, 0.5f, 0.63f), new Vector3(0.34f, 0.34f, 0.04f), GoldM);
        BoxM("Keyhole", body, new Vector3(0f, 0.47f, 0.66f), new Vector3(0.07f, 0.13f, 0.03f), MatPBR(Color.black, 0f, 0.1f));
        BoxM("Hasp", body, new Vector3(0f, 0.80f, 0.58f), new Vector3(0.17f, 0.30f, 0.06f), IronM);

        // --- arched lid on a hinge along the BACK edge ------------------------
        // Built from planks (flat base board + 3 planks forming a shallow arch)
        // instead of a cylinder, so there's no hidden half that pops into view
        // when it opens - you see the lid's flat underside, like a real chest.
        var lidPivot = new GameObject("LidPivot").transform;
        lidPivot.SetParent(body, false);
        lidPivot.localPosition = new Vector3(0f, 0.80f, -0.56f);
        const float LW = 1.72f;                                   // lid width (x)
        BoxM("LidBase", lidPivot, new Vector3(0f, 0.04f, 0.56f), new Vector3(LW, 0.08f, 1.14f), Oak);
        BoxM("LidBack", lidPivot, new Vector3(0f, 0.15f, 0.20f), new Vector3(LW, 0.08f, 0.44f), Oak).transform.localRotation = Quaternion.Euler(-24f, 0f, 0f);
        BoxM("LidTop", lidPivot, new Vector3(0f, 0.25f, 0.56f), new Vector3(LW, 0.08f, 0.42f), Oak);
        BoxM("LidFront", lidPivot, new Vector3(0f, 0.15f, 0.92f), new Vector3(LW, 0.08f, 0.44f), Oak).transform.localRotation = Quaternion.Euler(24f, 0f, 0f);
        foreach (float sx in new[] { -0.85f, 0.85f })          // end caps under the arch
            BoxM("LidEnd", lidPivot, new Vector3(sx, 0.12f, 0.56f), new Vector3(0.05f, 0.16f, 0.62f), Ebony);
        foreach (float sx in new[] { -0.5f, 0.5f })            // iron straps over the arch
        {
            BoxM("StrapBack", lidPivot, new Vector3(sx, 0.16f, 0.20f), new Vector3(0.17f, 0.10f, 0.46f), IronM).transform.localRotation = Quaternion.Euler(-24f, 0f, 0f);
            BoxM("StrapTop", lidPivot, new Vector3(sx, 0.26f, 0.56f), new Vector3(0.17f, 0.10f, 0.44f), IronM);
            BoxM("StrapFront", lidPivot, new Vector3(sx, 0.16f, 0.92f), new Vector3(0.17f, 0.10f, 0.46f), IronM).transform.localRotation = Quaternion.Euler(24f, 0f, 0f);
        }
        BoxM("LidRidge", lidPivot, new Vector3(0f, 0.31f, 0.56f), new Vector3(1.5f, 0.05f, 0.12f), BrassM);
        // The body box is solid, so the "contents" sit on its TOP face, hidden
        // under the closed lid and revealed when it opens: a warm golden glow,
        // a scatter of gold coins and a velvet cushion that the key rests on.
        Glow("InnerGlow", body, new Vector3(0f, 0.805f, 0f), new Vector3(1.55f, 0.02f, 1.0f),
             new Color(1f, 0.85f, 0.4f), new Color(1f, 0.8f, 0.3f) * 2.5f);
        var goldCoin = GoldM;
        var crng = new System.Random(21);
        for (int i = 0; i < 9; i++)
        {
            float cx = (float)(crng.NextDouble() * 1.3 - 0.65), cz = (float)(crng.NextDouble() * 0.8 - 0.4);
            if (Mathf.Abs(cx) < 0.3f && Mathf.Abs(cz) < 0.2f) cx += 0.45f;   // keep the cushion spot clear
            CylM("TreasureCoin", body, new Vector3(cx, 0.83f, cz), new Vector3(0.16f, 0.015f, 0.16f), goldCoin,
                 new Vector3((float)(crng.NextDouble() * 30), 0f, (float)(crng.NextDouble() * 30)));
        }

        // --- flanking candelabra for the "prized exhibit" feel --------------
        foreach (float sx in new[] { -1.95f, 1.95f })
            BuildCandelabra(root, new Vector3(sx, 0.34f, 0.2f));

        var key = BuildKey();
        key.transform.SetParent(root, false);
        // key floats just above the chest's top face (inside the closed lid),
        // spinning over its velvet cushion - visible as soon as the lid lifts
        key.transform.localPosition = new Vector3(0f, 0.35f + 0.8f + 0.12f, 0f);
        key.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        key.transform.localScale = Vector3.one * 0.75f;   // fits fully inside the closed lid
        BoxM("Cushion", body, new Vector3(0f, 0.84f, 0f), new Vector3(0.5f, 0.06f, 0.34f), Velvet);

        // one clean blocker collider; everything else is decoration
        StripCols(root.gameObject);
        var col = body.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.42f, 0f);
        col.size = new Vector3(1.9f, 0.95f, 1.3f);

        var chest = root.gameObject.AddComponent<Chest>();
        chest.lidPivot = lidPivot;
        chest.key = key;
    }

    static void BuildCandelabra(Transform parent, Vector3 pos)
    {
        var c = new GameObject("Candelabra").transform;
        c.SetParent(parent, false); c.localPosition = pos;
        CylM("Base", c, new Vector3(0f, 0.05f, 0f), new Vector3(0.5f, 0.1f, 0.5f), BrassM);
        CylM("Stem", c, new Vector3(0f, 0.75f, 0f), new Vector3(0.12f, 1.4f, 0.12f), BrassM);
        CylM("Cup", c, new Vector3(0f, 1.48f, 0f), new Vector3(0.34f, 0.06f, 0.34f), BrassM);
        CylM("Candle", c, new Vector3(0f, 1.66f, 0f), new Vector3(0.16f, 0.22f, 0.16f), Wax);
        CylM("Flame", c, new Vector3(0f, 1.92f, 0f), new Vector3(0.09f, 0.17f, 0.09f), FlameM);
        var l = AddLight("CandleLight", c, new Vector3(0f, 1.95f, 0f), new Color(1f, 0.72f, 0.36f), 2.4f, 6f);
        l.gameObject.AddComponent<FireFlicker>();
        StripCols(c.gameObject);
    }

    static GameObject BuildKey()
    {
        var k = new GameObject("Key");
        var t = k.transform;
        CylM("Shaft", t, new Vector3(0f, 0f, 0.05f), new Vector3(0.06f, 0.34f, 0.06f), GoldM, new Vector3(90f, 0f, 0f));
        foreach (float z in new[] { -0.20f, 0.28f })
            CylM("Collar", t, new Vector3(0f, 0f, z), new Vector3(0.12f, 0.03f, 0.12f), GoldM, new Vector3(90f, 0f, 0f));
        // clover bow: centre + four lobes, with a dark eye
        Ball("BowC", t, new Vector3(0f, 0f, -0.34f), 0.15f, GoldM);
        foreach (var o in new[] { new Vector3(0.13f, 0f, -0.34f), new Vector3(-0.13f, 0f, -0.34f),
                                  new Vector3(0f, 0.13f, -0.34f), new Vector3(0f, -0.13f, -0.34f) })
            Ball("BowLobe", t, o, 0.11f, GoldM);
        Ball("BowEye", t, new Vector3(0f, 0f, -0.34f), 0.07f, MatPBR(new Color(0.14f, 0.10f, 0.02f), 0f, 0.1f));
        // bit / teeth
        BoxM("BitStem", t, new Vector3(0.02f, -0.02f, 0.34f), new Vector3(0.05f, 0.05f, 0.16f), GoldM);
        BoxM("Tooth1", t, new Vector3(0.10f, -0.10f, 0.32f), new Vector3(0.16f, 0.12f, 0.05f), GoldM);
        BoxM("Tooth2", t, new Vector3(0.09f, -0.08f, 0.40f), new Vector3(0.13f, 0.16f, 0.05f), GoldM);
        StripCols(k);
        return k;
    }

    // ------------------------------------------------------- Houses / rooms ---

    // Cottage footprint (local): X = width (door on -X), Z = depth.
    // (CW/CD/CH are fields, not consts: House #1 is built bigger so the
    //  HouseInterior room fits exactly inside its walls - see H1W/H1D/H1H)
    static float CW = 8f, CD = 8f, CH = 3.6f;
    const float CT = 0.3f, CGap = 2.4f, DoorTop = 2.8f;

    // House #1 is sized to wrap the interior room (13 wide x 12 deep x 4.4 high,
    // walls 0.3): cottage X = room depth, cottage Z = room width.
    const float H1W = 13f, H1D = 14f, H1H = 4.8f;

    static T WithCottageSize<T>(float cw, float cd, float ch, System.Func<T> build)
    {
        float ow = CW, od = CD, oh = CH;
        CW = cw; CD = cd; CH = ch;
        try { return build(); }
        finally { CW = ow; CD = od; CH = oh; }
    }

    // roofs on big houses get a flatter pitch so the ridge doesn't tower
    static float RoofPitch(float hd) => Mathf.Min(36f, Mathf.Atan2(3.2f, hd) * Mathf.Rad2Deg);
    static float RoofRise(float hd) => hd * Mathf.Tan(RoofPitch(hd) * Mathf.Deg2Rad);

    // A half-timbered fairytale cottage in the HW2 tavern palette: plaster walls
    // on a stone base band, dark timber corner posts + beams + window frames, and
    // a proper dark-red gable roof with eaves. Closed; the doorway on -X gets a
    // solid plank door (House 1) or is left for a swinging leaf (House 2).
    static GameObject Cottage(string name, Vector3 pos, float yaw, bool solidDoor)
    {
        var root = new GameObject(name).transform;
        root.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));

        float hw = CW * 0.5f, hd = CD * 0.5f, seg = (CD - CGap) * 0.5f;

        Box("Floor", root, new Vector3(0f, 0.06f, 0f), new Vector3(CW, 0.12f, CD), Wood);

        // plaster walls
        Box("Wall+X", root, new Vector3(hw - CT * 0.5f, CH * 0.5f, 0f), new Vector3(CT, CH, CD), Plaster);
        Box("Wall+Z", root, new Vector3(0f, CH * 0.5f, hd - CT * 0.5f), new Vector3(CW, CH, CT), Plaster);
        Box("Wall-Z", root, new Vector3(0f, CH * 0.5f, -hd + CT * 0.5f), new Vector3(CW, CH, CT), Plaster);
        Box("Wall-Xa", root, new Vector3(-hw + CT * 0.5f, CH * 0.5f, CGap * 0.5f + seg * 0.5f), new Vector3(CT, CH, seg), Plaster);
        Box("Wall-Xb", root, new Vector3(-hw + CT * 0.5f, CH * 0.5f, -(CGap * 0.5f + seg * 0.5f)), new Vector3(CT, CH, seg), Plaster);
        Box("Lintel", root, new Vector3(-hw + CT * 0.5f, (CH + DoorTop) * 0.5f, 0f), new Vector3(CT, CH - DoorTop, CGap), Plaster);

        // stone base band around the foot of the walls
        Box("Base+X", root, new Vector3(hw - CT * 0.5f + 0.02f, 0.55f, 0f), new Vector3(CT + 0.1f, 1.1f, CD), Stone);
        Box("Base+Z", root, new Vector3(0f, 0.55f, hd - CT * 0.5f + 0.02f), new Vector3(CW, 1.1f, CT + 0.1f), Stone);
        Box("Base-Z", root, new Vector3(0f, 0.55f, -hd + CT * 0.5f - 0.02f), new Vector3(CW, 1.1f, CT + 0.1f), Stone);
        Box("Base-Xa", root, new Vector3(-hw + CT * 0.5f - 0.02f, 0.55f, CGap * 0.5f + seg * 0.5f), new Vector3(CT + 0.1f, 1.1f, seg), Stone);
        Box("Base-Xb", root, new Vector3(-hw + CT * 0.5f - 0.02f, 0.55f, -(CGap * 0.5f + seg * 0.5f)), new Vector3(CT + 0.1f, 1.1f, seg), Stone);

        // dark timber frame: corner posts, top plate, a mid rail + a few studs
        foreach (var xz in new[] { new Vector2(-hw, hd), new Vector2(hw, hd), new Vector2(-hw, -hd), new Vector2(hw, -hd) })
            Box("Post", root, new Vector3(xz.x, CH * 0.5f, xz.y), new Vector3(0.34f, CH, 0.34f), Timber);
        Box("Plate+Z", root, new Vector3(0f, CH - 0.18f, hd), new Vector3(CW + 0.3f, 0.28f, 0.34f), Timber);
        Box("Plate-Z", root, new Vector3(0f, CH - 0.18f, -hd), new Vector3(CW + 0.3f, 0.28f, 0.34f), Timber);
        Box("Plate+X", root, new Vector3(hw, CH - 0.18f, 0f), new Vector3(0.34f, 0.28f, CD + 0.3f), Timber);
        Box("Rail+Z", root, new Vector3(0f, 1.9f, hd + 0.01f), new Vector3(CW, 0.16f, 0.24f), Timber);
        Box("Rail-Z", root, new Vector3(0f, 1.9f, -hd - 0.01f), new Vector3(CW, 0.16f, 0.24f), Timber);
        for (int i = -1; i <= 1; i++)
        {
            Box("Stud+Z", root, new Vector3(i * 2.2f, CH * 0.5f, hd + 0.01f), new Vector3(0.22f, CH, 0.22f), Timber);
            Box("Stud-Z", root, new Vector3(i * 2.2f, CH * 0.5f, -hd - 0.01f), new Vector3(0.22f, CH, 0.22f), Timber);
        }

        // little framed windows on the side walls
        Window(root, new Vector3(0f, 2.0f, hd - 0.02f), false);
        Window(root, new Vector3(2.4f, 2.0f, -hd + 0.02f), false);
        Window(root, new Vector3(hw - 0.02f, 2.0f, 1.8f), true);

        BuildGableRoof(root, hw, hd);

        // chimney on the +X back corner
        Box("Chimney", root, new Vector3(hw - 0.9f, CH + 1.7f, -hd + 1.1f), new Vector3(0.9f, 3.4f, 0.9f), Stone);
        CylM("ChimneyPot", root, new Vector3(hw - 0.9f, CH + 3.5f, -hd + 1.1f), new Vector3(0.4f, 0.5f, 0.4f), Ebony);

        if (solidDoor) PlankDoor(root, new Vector3(-hw + CT * 0.5f, 0f, 0f), 0f);

        CottageLux(root, hw, hd);
        return root.gameObject;
    }

    // Exterior finishing that lifts the cottage from "grey box" to storybook:
    // a stone doorstep, an iron door-lantern that glows, planted window boxes,
    // roof finials and a hanging sign board. Decoration only (no colliders on
    // the door approach).
    static void CottageLux(Transform root, float hw, float hd)
    {
        var lux = new GameObject("Lux").transform;
        lux.SetParent(root, false);

        // stone doorstep (door is on local -X, doorway centred on z = 0)
        BoxM("Step1", lux, new Vector3(-hw - 0.35f, 0.09f, 0f), new Vector3(0.9f, 0.18f, CGap + 1.2f), StoneP);
        BoxM("Step2", lux, new Vector3(-hw - 0.95f, 0.045f, 0f), new Vector3(1.0f, 0.09f, CGap + 1.9f), MatPBR(new Color(0.46f, 0.44f, 0.40f), 0f, 0.3f));

        // iron wall lantern beside the door
        var ln = new GameObject("DoorLantern").transform; ln.SetParent(lux, false);
        ln.localPosition = new Vector3(-hw + 0.05f, 2.4f, -CGap * 0.5f - 0.5f);
        BoxM("Arm", ln, new Vector3(-0.22f, 0.1f, 0f), new Vector3(0.5f, 0.07f, 0.07f), IronM);
        BoxM("Cage", ln, new Vector3(-0.45f, -0.12f, 0f), new Vector3(0.22f, 0.36f, 0.22f), IronM);
        Glow("Glow", ln, new Vector3(-0.45f, -0.12f, 0f), new Vector3(0.14f, 0.26f, 0.14f),
             new Color(1f, 0.8f, 0.4f), new Color(1f, 0.7f, 0.3f) * 3f);
        var ll = AddLight("LanternLight", ln, new Vector3(-0.45f, -0.12f, 0f), new Color(1f, 0.72f, 0.38f), 2.2f, 6f);
        ll.gameObject.AddComponent<FireFlicker>();

        // planted window boxes under the two side-wall windows
        foreach (var wp in new[] { new Vector3(0f, 1.3f, hd + 0.05f), new Vector3(2.4f, 1.3f, -hd - 0.05f) })
        {
            var wb = new GameObject("WindowBox").transform; wb.SetParent(lux, false); wb.localPosition = wp;
            BoxM("Trough", wb, Vector3.zero, new Vector3(1.5f, 0.28f, 0.4f), Oak);
            for (int i = 0; i < 4; i++)
            {
                var col = i % 2 == 0 ? new Color(0.82f, 0.20f, 0.26f) : new Color(0.92f, 0.76f, 0.22f);
                Ball("Bloom", wb, new Vector3(-0.55f + i * 0.37f, 0.22f, 0f), 0.17f, MatPBR(col, 0f, 0.2f));
            }
        }

        // roof-end finials
        float rise = RoofRise(hd);
        foreach (float rx in new[] { -hw - 0.35f, hw + 0.35f })
        {
            CylM("Finial", lux, new Vector3(rx, CH + rise - 0.6f, 0f), new Vector3(0.12f, 0.5f, 0.12f), IronM);
            Ball("FinialBall", lux, new Vector3(rx, CH + rise - 0.2f, 0f), 0.24f, BrassM);
        }

        // a hanging sign board on an iron bracket by the door
        var sg = new GameObject("Sign").transform; sg.SetParent(lux, false);
        sg.localPosition = new Vector3(-hw - 0.2f, 2.7f, CGap * 0.5f + 0.7f);
        BoxM("Post", sg, new Vector3(0f, 0f, 0f), new Vector3(0.12f, 1.0f, 0.12f), IronM);
        BoxM("Bracket", sg, new Vector3(0f, 0.42f, 0.35f), new Vector3(0.08f, 0.08f, 0.8f), IronM);
        BoxM("Board", sg, new Vector3(0f, 0.0f, 0.7f), new Vector3(0.1f, 0.6f, 0.95f), Oak);
        BoxM("BoardTrim", sg, new Vector3(0f, 0.0f, 0.7f), new Vector3(0.08f, 0.68f, 1.05f), BrassM);

        StripCols(lux.gameObject);
    }

    static void Window(Transform root, Vector3 p, bool onXWall)
    {
        var w = new GameObject("Window").transform; w.SetParent(root, false); w.localPosition = p;
        Vector3 frame = onXWall ? new Vector3(0.16f, 1.1f, 1.3f) : new Vector3(1.3f, 1.1f, 0.16f);
        Vector3 pane  = onXWall ? new Vector3(0.10f, 0.8f, 1.0f) : new Vector3(1.0f, 0.8f, 0.10f);
        Box("Frame", w, Vector3.zero, frame, Timber);
        Box("Pane", w, Vector3.zero, pane, Pane);
    }

    // Solid plank door hung on a hinge ("DoorPivot") at the doorway's -Z edge, so
    // rotating the pivot +Y swings the leaf inward (HouseEntrance animates it).
    static Transform PlankDoor(Transform parent, Vector3 localPos, float extraYaw)
    {
        var pivot = new GameObject("DoorPivot").transform; pivot.SetParent(parent, false);
        pivot.localPosition = localPos + new Vector3(0f, 0f, -CGap * 0.5f);
        pivot.localRotation = Quaternion.Euler(0f, extraYaw, 0f);
        var d = new GameObject("Door").transform; d.SetParent(pivot, false);
        d.localPosition = new Vector3(0f, 0f, CGap * 0.5f);
        float h = DoorTop - 0.1f;
        Box("Leaf", d, new Vector3(0f, h * 0.5f + 0.1f, 0f), new Vector3(CT * 1.2f, h, CGap - 0.12f), WoodL);
        Box("PlankA", d, new Vector3(-0.05f, h * 0.5f + 0.1f, -0.55f), new Vector3(CT * 1.3f, h, 0.06f), Wood);
        Box("PlankB", d, new Vector3(-0.05f, h * 0.5f + 0.1f, 0.55f), new Vector3(CT * 1.3f, h, 0.06f), Wood);
        Box("BandT", d, new Vector3(-0.05f, h - 0.2f, 0f), new Vector3(CT * 1.4f, 0.14f, CGap - 0.2f), Stone);
        Box("BandB", d, new Vector3(-0.05f, 0.5f, 0f), new Vector3(CT * 1.4f, 0.14f, CGap - 0.2f), Stone);
        Box("Handle", d, new Vector3(-0.12f, h * 0.5f, 0.7f), new Vector3(0.12f, 0.12f, 0.12f), Stone);
        return pivot;
    }

    static void BuildGableRoof(Transform root, float hw, float hd)
    {
        var roof = new GameObject("Roof").transform; roof.SetParent(root, false);
        float pitch = RoofPitch(hd);
        float slope = hd / Mathf.Cos(pitch * Mathf.Deg2Rad) + 0.6f;   // + eave overhang
        float rise  = RoofRise(hd);
        float midY  = CH + rise * 0.5f - 0.05f;

        var rp = Box("Panel+Z", roof, new Vector3(0f, midY, hd * 0.5f), new Vector3(CW + 1.0f, 0.18f, slope), RoofCol);
        rp.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        var rn = Box("Panel-Z", roof, new Vector3(0f, midY, -hd * 0.5f), new Vector3(CW + 1.0f, 0.18f, slope), RoofCol);
        rn.transform.localRotation = Quaternion.Euler(-pitch, 0f, 0f);
        Box("Ridge", roof, new Vector3(0f, CH + rise - 0.1f, 0f), new Vector3(CW + 1.1f, 0.22f, 0.42f), RoofCol);

        // plaster gable-end fills so the roof reads as solid from every side
        Box("Gable+X", roof, new Vector3(hw - 0.05f, CH + rise * 0.5f, 0f), new Vector3(0.2f, rise, CD), Plaster);
        Box("Gable-X", roof, new Vector3(-hw + 0.05f, CH + rise * 0.5f, 0f), new Vector3(0.2f, rise, CD), Plaster);
    }

    // Fence, path, props, greenery around a cottage so it nests into the village.
    static void DressCottage(string name, Vector3 pos, float yaw)
    {
        var d = new GameObject(name + "_Yard").transform;
        d.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
        float hw = CW * 0.5f;

        // a short path prefab leading out from the door (door is on local -X)
        Spawn("Nature/Terrain/rpgpp_lt_terrain_path_01a.prefab", d, new Vector3(-hw - 2.4f, 0.03f, 0f), 90f);
        Spawn("Nature/Terrain/rpgpp_lt_terrain_path_01a.prefab", d, new Vector3(-hw - 5.6f, 0.03f, 0f), 90f);

        // low fence line along the front, with a WIDE clear gap for the path so
        // the player walks straight to the door instead of threading a narrow
        // funnel (that 1.6-unit gap was the main thing the character snagged on)
        foreach (int i in new[] { -4, -3, 3, 4 })
            Spawn("Exterior/Wood_path/rpgpp_lt_fence_wood_01a.prefab", d, new Vector3(-hw - 2.6f, 0f, i * 1.4f), 0f);
        Spawn("Exterior/Wood_path/rpgpp_lt_fence_wood_01_corner_a.prefab", d, new Vector3(-hw - 2.6f, 0f, 6.0f), 0f);
        Spawn("Exterior/Wood_path/rpgpp_lt_fence_wood_01_corner_a.prefab", d, new Vector3(-hw - 2.6f, 0f, -6.0f), -90f);

        // props tucked against the wall on the door side
        Spawn("Props/Containers/rpgpp_lt_barrel_01.prefab", d, new Vector3(-hw - 0.8f, 0f, 2.6f), 0f);
        Spawn("Props/Containers/rpgpp_lt_barrel_02.prefab", d, new Vector3(-hw - 0.8f, 0f, 3.4f), 20f);
        Spawn("Props/Containers/rpgpp_lt_crate_01.prefab", d, new Vector3(-hw - 0.8f, 0f, -2.8f), -15f);
        Spawn("Props/Benches/rpgpp_lt_bench_wood_01.prefab", d, new Vector3(-hw - 0.9f, 0f, -1.4f), 90f);

        // a banner over the doorway + greenery inside the little garden
        Spawn("Props/Banners/rpgpp_lt_banner_01a.prefab", d, new Vector3(-hw + 0.2f, 2.9f, 0.9f), 0f);
        Spawn("Nature/Vegetation/Bushes/rpgpp_lt_bush_01.prefab", d, new Vector3(-hw - 1.9f, 0f, 3.4f), 0f);
        Spawn("Nature/Vegetation/Flowers/rpgpp_lt_flower_02.prefab", d, new Vector3(-hw - 1.7f, 0f, 1.5f), 0f);
        Spawn("Nature/Vegetation/Flowers/rpgpp_lt_flower_01.prefab", d, new Vector3(-hw - 1.9f, 0f, -1.9f), 0f);
        float hd = CD * 0.5f;
        Spawn("Nature/Vegetation/Trees/rpgpp_lt_tree_01.prefab", d, new Vector3(3.0f, 0f, hd + 2.0f), 40f, 1.1f);

        // Extra greenery so the ground the cottages were cleared onto (ClearAround
        // in BuildVillageScene) doesn't read as bare. Kept off the front path/gate
        // lane (local |z| < 2 near the door) and the walk-through gap itself.
        Spawn("Nature/Vegetation/Trees/rpgpp_lt_tree_02.prefab", d, new Vector3(hw + 2.5f, 0f, 3.0f), -20f, 1.15f);
        Spawn("Nature/Vegetation/Trees/rpgpp_lt_tree_pine_01.prefab", d, new Vector3(hw + 3.5f, 0f, -3.5f), 15f, 1.0f);
        Spawn("Nature/Vegetation/Trees/rpgpp_lt_tree_01.prefab", d, new Vector3(1.0f, 0f, -hd - 2.0f), -30f, 0.95f);
        Spawn("Nature/Vegetation/Bushes/rpgpp_lt_bush_02.prefab", d, new Vector3(hw + 1.0f, 0f, 1.2f), 0f);
        Spawn("Nature/Vegetation/Bushes/rpgpp_lt_bush_01.prefab", d, new Vector3(hw + 0.8f, 0f, -1.6f), 0f);

        // a colourful flower bed either side of the gate, and a patch out back
        string[] flowers = {
            "Nature/Vegetation/Flowers/rpgpp_lt_flower_01.prefab",
            "Nature/Vegetation/Flowers/rpgpp_lt_flower_02.prefab",
            "Nature/Vegetation/Flowers/rpgpp_lt_flower_03.prefab",
        };
        var rng = new System.Random(name.GetHashCode());
        for (int i = 0; i < 6; i++)
        {
            float lz = 3.0f + (float)rng.NextDouble() * 1.6f;         // outside the fence corners, +side
            float lx = -hw - 1.0f - (float)rng.NextDouble() * 3.5f;
            Spawn(flowers[i % flowers.Length], d, new Vector3(lx, 0f, lz), (float)(rng.NextDouble() * 360));
        }
        for (int i = 0; i < 6; i++)
        {
            float lz = -3.0f - (float)rng.NextDouble() * 1.6f;        // -side
            float lx = -hw - 1.0f - (float)rng.NextDouble() * 3.5f;
            Spawn(flowers[i % flowers.Length], d, new Vector3(lx, 0f, lz), (float)(rng.NextDouble() * 360));
        }
        for (int i = 0; i < 5; i++)
        {
            float lx = hw + 1.0f + (float)rng.NextDouble() * 3.0f;    // out back, around the trees
            float lz = (float)(rng.NextDouble() * 6.0 - 3.0);
            Spawn(flowers[i % flowers.Length], d, new Vector3(lx, 0f, lz), (float)(rng.NextDouble() * 360));
        }
    }

    // Trees + flower clumps in the open ground between House1 (z=+7) and House2
    // (z=-7), flanking the z=0 walking lane rather than sitting on it.
    static void BeautifyBetweenCottages()
    {
        string[] flowers = {
            "Nature/Vegetation/Flowers/rpgpp_lt_flower_01.prefab",
            "Nature/Vegetation/Flowers/rpgpp_lt_flower_02.prefab",
            "Nature/Vegetation/Flowers/rpgpp_lt_flower_03.prefab",
        };
        var rng = new System.Random(99);

        // (+side kept below z~3.4: the enlarged House1's wall starts at z = 4)
        Spawn("Nature/Vegetation/Trees/rpgpp_lt_tree_02.prefab", null, new Vector3(-37.5f, 0f, 1.8f), 25f, 0.9f);
        Spawn("Nature/Vegetation/Trees/rpgpp_lt_tree_01.prefab", null, new Vector3(-35f, 0f, -3.5f), -35f, 1.05f);
        Spawn("Nature/Vegetation/Bushes/rpgpp_lt_bush_02.prefab", null, new Vector3(-33.5f, 0f, 2.0f), 0f);
        Spawn("Nature/Vegetation/Bushes/rpgpp_lt_bush_01.prefab", null, new Vector3(-34f, 0f, -2.4f), 0f);

        for (int i = 0; i < 8; i++)
        {
            float z = 1.4f + (float)rng.NextDouble() * 1.9f;          // +side patch
            float x = -37f + (float)rng.NextDouble() * 5.0f;
            Spawn(flowers[i % flowers.Length], null, new Vector3(x, 0f, z), (float)(rng.NextDouble() * 360));
        }
        for (int i = 0; i < 8; i++)
        {
            float z = -2.2f - (float)rng.NextDouble() * 3.2f;         // -side patch
            float x = -37f + (float)rng.NextDouble() * 5.0f;
            Spawn(flowers[i % flowers.Length], null, new Vector3(x, 0f, z), (float)(rng.NextDouble() * 360));
        }
    }

    static void BuildLockedCoinHouse(string name, Vector3 pos, float yaw)
    {
        var house = Cottage(name, pos, yaw, solidDoor: false).transform;
        float hw = CW * 0.5f;

        // swinging plank door on a hinge at one edge of the (local -X) doorway
        var pivot = new GameObject(name + "_DoorPivot").transform;
        pivot.SetParent(house, false);
        pivot.localPosition = new Vector3(-hw + CT * 0.5f, 0f, -CGap * 0.5f);
        float h = DoorTop - 0.1f;
        Box("Leaf", pivot, new Vector3(0f, h * 0.5f + 0.1f, CGap * 0.5f), new Vector3(CT * 1.2f, h, CGap - 0.08f), WoodL);
        Box("LeafBandT", pivot, new Vector3(-0.05f, h - 0.2f, CGap * 0.5f), new Vector3(CT * 1.4f, 0.14f, CGap - 0.2f), Stone);
        Box("LeafBandB", pivot, new Vector3(-0.05f, 0.5f, CGap * 0.5f), new Vector3(CT * 1.4f, 0.14f, CGap - 0.2f), Stone);

        var doorObj = new GameObject(name + "_LockedDoor");
        doorObj.transform.SetParent(house, false);
        doorObj.transform.localPosition = new Vector3(-hw - 0.2f, 1.2f, 0f);
        var ld = doorObj.AddComponent<LockedDoor>();
        ld.doorPivot = pivot;
        ld.creak = DoorCreak;

        // treasure inside: a crate with gold coins piled in and around it
        var coins = new GameObject(name + "_Coins").transform;
        // coins laid out in neat, evenly spaced rows over the rug (3 x 5), with
        // clear walking room between them - the gold hoards, open chest and
        // shelves along the walls (DecorateTreasury) make it read as full of gold
        for (int col = 0; col < 3; col++)
            for (int row = 0; row < 5; row++)
            {
                float lx = -1.0f + col * 1.1f;
                float lz = -1.8f + row * 0.9f;
                MakeCoin(house.TransformPoint(new Vector3(lx, 0f, lz)) + Vector3.up * 0.95f, coins);
            }
        DecorateTreasury(house);   // gold hoards, chest, torches, shields (Hw3Interiors.cs)
    }

    // The House 1 interior: a cosy tavern-style room using the same plaster /
    // stone / timber palette and warm lighting as the pub, with a small stone
    // fireplace, a rug and real RPGPP props.
    static void BuildRoom()
    {
        var root = new GameObject("Room").transform;
        const float W = 13f, D = 12f, H = 4.4f, t = 0.3f, gap = CGap;   // same doorway as House #1
        float hw = W * 0.5f, hd = D * 0.5f, seg = (W - gap) * 0.5f;

        Box("Floor", root, new Vector3(0f, -0.1f, 0f), new Vector3(W, 0.2f, D), Wood);
        Box("Ceiling", root, new Vector3(0f, H, 0f), new Vector3(W, t, D), Plaster);
        Box("Wall+X", root, new Vector3(hw, H * 0.5f, 0f), new Vector3(t, H, D), Plaster);
        Box("Wall-X", root, new Vector3(-hw, H * 0.5f, 0f), new Vector3(t, H, D), Plaster);
        Box("Wall+Z", root, new Vector3(0f, H * 0.5f, hd), new Vector3(W, H, t), Plaster);
        Box("Wall-Za", root, new Vector3(gap * 0.5f + seg * 0.5f, H * 0.5f, -hd), new Vector3(seg, H, t), Plaster);
        Box("Wall-Zb", root, new Vector3(-(gap * 0.5f + seg * 0.5f), H * 0.5f, -hd), new Vector3(seg, H, t), Plaster);
        // lintel bottom lines up with House #1's door top once the room is lifted onto its floor
        const float doorTop = DoorTop - 0.125f;
        Box("Lintel", root, new Vector3(0f, (H + doorTop) * 0.5f, -hd), new Vector3(gap, H - doorTop, t), Plaster);

        // stone base band + timber corner posts + ceiling beams (pub language)
        Box("Base+X", root, new Vector3(hw - 0.05f, 0.6f, 0f), new Vector3(t + 0.12f, 1.2f, D), Stone);
        Box("Base-X", root, new Vector3(-hw + 0.05f, 0.6f, 0f), new Vector3(t + 0.12f, 1.2f, D), Stone);
        Box("Base+Z", root, new Vector3(0f, 0.6f, hd - 0.05f), new Vector3(W, 1.2f, t + 0.12f), Stone);
        foreach (var xz in new[] { new Vector2(-hw, hd), new Vector2(hw, hd), new Vector2(-hw, -hd), new Vector2(hw, -hd) })
            Box("Post", root, new Vector3(xz.x, H * 0.5f, xz.y), new Vector3(0.34f, H, 0.34f), Timber);
        for (int i = 0; i < 5; i++)
            Box("Beam", root, new Vector3(0f, H - 0.25f, -4.4f + i * 2.2f), new Vector3(W, 0.32f, 0.32f), Timber);

        // small stone fireplace on the +Z wall with an emissive fire + firelight
        var fp = new GameObject("Fireplace").transform; fp.SetParent(root, false);
        float z = hd - 0.6f;
        Box("Surround", fp, new Vector3(0f, 1.6f, z), new Vector3(3.4f, 3.2f, 1.0f), Stone);
        Box("Breast", fp, new Vector3(0f, H - 0.8f, hd - 0.35f), new Vector3(2f, 2.6f, 0.7f), Stone);
        Box("Mantel", fp, new Vector3(0f, 2.3f, z - 0.2f), new Vector3(3.8f, 0.22f, 1.2f), Wood);
        Box("Hearth", fp, new Vector3(0f, 0.85f, z - 0.35f), new Vector3(2f, 1.5f, 0.5f), new Color(0.06f, 0.05f, 0.05f));
        Cyl("Log", fp, new Vector3(-0.35f, 0.35f, z - 0.35f), new Vector3(0.22f, 0.6f, 0.22f), new Color(0.3f, 0.2f, 0.12f), new Vector3(0f, 20f, 90f));
        Cyl("Log", fp, new Vector3(0.35f, 0.35f, z - 0.35f), new Vector3(0.22f, 0.6f, 0.22f), new Color(0.28f, 0.18f, 0.1f), new Vector3(0f, -20f, 90f));
        Glow("Fire", fp, new Vector3(0f, 0.55f, z - 0.35f), new Vector3(1.3f, 0.9f, 0.55f), new Color(1f, 0.5f, 0.15f), new Color(1f, 0.45f, 0.12f) * 4f);
        var fl = AddLight("FireLight", fp, new Vector3(0f, 1.1f, z - 1f), new Color(1f, 0.62f, 0.30f), 5.5f, 13f);
        fl.gameObject.AddComponent<FireFlicker>();

        // rug in front of the fire + props kept to the +X side so the lane from
        // the door to the chest (x -5..-2) stays clear
        Box("Rug", root, new Vector3(1.5f, 0.03f, hd - 3.6f), new Vector3(3.4f, 0.05f, 2.4f), RugCol);
        Box("RugTrim", root, new Vector3(1.5f, 0.02f, hd - 3.6f), new Vector3(3.8f, 0.03f, 2.8f), new Color(0.3f, 0.08f, 0.07f));
        Spawn("Props/Tables/rpgpp_lt_table_01.prefab", root, new Vector3(3.8f, 0f, -1.2f), 0f);
        Spawn("Props/Chairs/rpgpp_lt_chair_01a.prefab", root, new Vector3(3.8f, 0f, -2.6f), 0f);
        Spawn("Props/Chairs/rpgpp_lt_chair_01a.prefab", root, new Vector3(5.1f, 0f, -1.2f), 90f);
        Spawn("Props/Containers/rpgpp_lt_barrel_01.prefab", root, new Vector3(hw - 1.2f, 0f, -hd + 1.4f), 0f);
        Spawn("Props/Containers/rpgpp_lt_barrel_02.prefab", root, new Vector3(hw - 1.2f, 0f, -hd + 2.4f), 15f);
        Spawn("Props/Containers/rpgpp_lt_crate_01.prefab", root, new Vector3(hw - 1.3f, 0f, hd - 1.6f), -10f);
        Spawn("Props/Containers/rpgpp_lt_sack_01.prefab", root, new Vector3(hw - 2.4f, 0f, hd - 1.4f), 25f);
        Spawn("Props/Banners/rpgpp_lt_banner_01b.prefab", root, new Vector3(hw - 0.3f, H - 0.3f, -1f), 0f);

        var l = AddLight("RoomLight", root, new Vector3(0f, H - 0.7f, -1f), new Color(1f, 0.9f, 0.72f), 2.2f, 20f);
        l.shadows = LightShadows.None;

        BuildRoomLux(root, W, D, H, hw, hd);
        DecorateRoom(root, W, D, H, hw, hd);   // textures, furniture, windows (Hw3Interiors.cs)
    }

    // Turns the plain room into a warm, appointed study: oak floor, a great rug,
    // wainscoting, a candle chandelier, wall sconces, a bookcase and gilt-framed
    // portraits. Kept off the door -> chest lane (x -5..-2).
    static void BuildRoomLux(Transform root, float W, float D, float H, float hw, float hd)
    {
        root.Find("Floor").GetComponent<Renderer>().sharedMaterial = Oak;
        foreach (var b in new[] { "Base+X", "Base-X", "Base+Z" })
        {
            var tr = root.Find(b);
            if (tr != null) tr.GetComponent<Renderer>().sharedMaterial = Ebony;
        }
        BoxM("HallRugTrim", root, new Vector3(-1.3f, 0.03f, -0.5f), new Vector3(7.4f, 0.04f, 9.4f), BrassM);
        BoxM("HallRug", root, new Vector3(-1.3f, 0.04f, -0.5f), new Vector3(6.8f, 0.05f, 8.8f), Velvet);

        // candle chandelier hung from the ceiling centre
        var ch = new GameObject("Chandelier").transform; ch.SetParent(root, false);
        ch.localPosition = new Vector3(0f, H - 0.12f, 0f);
        CylM("Chain", ch, new Vector3(0f, -0.5f, 0f), new Vector3(0.05f, 1.0f, 0.05f), IronM);
        CylM("RingBrass", ch, new Vector3(0f, -1.05f, 0f), new Vector3(1.9f, 0.08f, 1.9f), BrassM);
        CylM("RingIron", ch, new Vector3(0f, -1.05f, 0f), new Vector3(1.5f, 0.10f, 1.5f), IronM);
        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI * 2f / 6f;
            var cp = new Vector3(Mathf.Cos(a) * 0.88f, -0.85f, Mathf.Sin(a) * 0.88f);
            CylM("Candle", ch, cp, new Vector3(0.10f, 0.18f, 0.10f), Wax);
            CylM("Flame", ch, cp + Vector3.up * 0.24f, new Vector3(0.07f, 0.13f, 0.07f), FlameM);
        }
        var chl = AddLight("ChandelierLight", ch, new Vector3(0f, -0.9f, 0f), new Color(1f, 0.82f, 0.55f), 4.2f, 16f);
        chl.gameObject.AddComponent<FireFlicker>();

        // wall sconces on both side walls (emissive only - keeps the additional
        // realtime-light count low so nothing pops under URP's per-object limit)
        foreach (var s in new[] { (hw - 0.18f, -3f, -1f), (hw - 0.18f, 3f, -1f),
                                  (-hw + 0.18f, -3f, 1f), (-hw + 0.18f, 3f, 1f) })
        {
            var sc = new GameObject("Sconce").transform; sc.SetParent(root, false);
            sc.localPosition = new Vector3(s.Item1, 2.6f, s.Item2);
            float dir = s.Item3;
            BoxM("Bracket", sc, new Vector3(0.18f * dir, 0f, 0f), new Vector3(0.4f, 0.14f, 0.14f), BrassM);
            CylM("Cup", sc, new Vector3(0.36f * dir, 0.06f, 0f), new Vector3(0.18f, 0.05f, 0.18f), BrassM);
            CylM("Flame", sc, new Vector3(0.36f * dir, 0.24f, 0f), new Vector3(0.10f, 0.16f, 0.10f), FlameM);
        }

        // a tall bookcase against the -X wall (well clear of the entry lane)
        var bs = new GameObject("Bookcase").transform; bs.SetParent(root, false);
        bs.localPosition = new Vector3(-hw + 0.5f, 0f, 3.4f);
        BoxM("Case", bs, new Vector3(0f, 1.9f, 0f), new Vector3(0.7f, 3.8f, 3.6f), Oak);
        for (int s = 0; s < 4; s++)
            BoxM("Shelf", bs, new Vector3(0.05f, 0.55f + s * 0.9f, 0f), new Vector3(0.6f, 0.06f, 3.4f), Ebony);
        var brng = new System.Random(11);
        var bcol = new[] { new Color(0.42f, 0.10f, 0.10f), new Color(0.12f, 0.20f, 0.36f),
                           new Color(0.14f, 0.30f, 0.15f), new Color(0.46f, 0.35f, 0.12f),
                           new Color(0.25f, 0.12f, 0.30f) };
        for (int s = 0; s < 4; s++)
            for (float z = -1.5f; z < 1.5f; z += 0.15f + (float)brng.NextDouble() * 0.07f)
                BoxM("Book", bs, new Vector3(0.12f, 1.0f + s * 0.9f, z),
                     new Vector3(0.42f, 0.66f + (float)brng.NextDouble() * 0.16f, 0.12f),
                     MatPBR(bcol[brng.Next(bcol.Length)], 0f, 0.2f));

        // gilt-framed portraits on the -Z wall segments
        foreach (float px in new[] { -3.6f, 3.6f })
        {
            BoxM("Frame", root, new Vector3(px, 2.7f, -hd + 0.16f), new Vector3(1.5f, 1.9f, 0.12f), BrassM);
            BoxM("Canvas", root, new Vector3(px, 2.7f, -hd + 0.20f), new Vector3(1.1f, 1.5f, 0.06f),
                 MatPBR(new Color(0.18f, 0.15f, 0.12f), 0f, 0.1f));
        }

        // mantel candlesticks + a clock + brass fire-dogs
        var fp = root.Find("Fireplace");
        if (fp != null)
        {
            float fz = hd - 0.6f;
            foreach (float mx in new[] { -1.5f, 1.5f })
            {
                CylM("MantelStick", fp, new Vector3(mx, 2.55f, fz - 0.1f), new Vector3(0.1f, 0.3f, 0.1f), BrassM);
                CylM("MantelFlame", fp, new Vector3(mx, 2.78f, fz - 0.1f), new Vector3(0.07f, 0.12f, 0.07f), FlameM);
            }
            BoxM("Clock", fp, new Vector3(0f, 2.72f, fz - 0.12f), new Vector3(0.5f, 0.62f, 0.26f), Oak);
            BoxM("ClockFace", fp, new Vector3(0f, 2.78f, fz - 0.26f), new Vector3(0.32f, 0.32f, 0.04f), BrassM);
            foreach (float dx in new[] { -0.55f, 0.55f })
                CylM("FireDog", fp, new Vector3(dx, 0.30f, fz - 0.35f), new Vector3(0.1f, 0.5f, 0.1f), IronM, new Vector3(90f, 0f, 0f));
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.24f, 0.20f, 0.18f);

        StripCols(root.Find("Chandelier").gameObject);
        foreach (Transform sc in root) if (sc.name == "Sconce") StripCols(sc.gameObject);
    }

    // -------------------------------------------------------------- helpers ---

    static void AddSun(float intensity)
    {
        var go = new GameObject("Sun");
        var l = go.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = intensity;
        l.color = new Color(1f, 0.96f, 0.86f);
        go.transform.rotation = Quaternion.Euler(48f, 40f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.5f, 0.52f, 0.55f);
    }

    static void DestroyAllCameras()
    {
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            Object.DestroyImmediate(c.gameObject);
        foreach (var a in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            Object.DestroyImmediate(a);
    }

    static void ClearAround(string[] groups, Vector3[] centers, float radius)
    {
        var village = GameObject.Find("Village");
        if (village == null) { Debug.LogWarning("ClearAround: no 'Village' root found"); return; }
        int removed = 0;
        foreach (var g in groups)
        {
            var grp = village.transform.Find(g);
            if (grp == null) continue;
            for (int i = grp.childCount - 1; i >= 0; i--)
            {
                var ch = grp.GetChild(i);
                foreach (var c in centers)
                {
                    if (new Vector2(ch.position.x - c.x, ch.position.z - c.z).magnitude < radius)
                    {
                        Object.DestroyImmediate(ch.gameObject);
                        removed++;
                        break;
                    }
                }
            }
        }
        Debug.Log($"ClearAround removed {removed} object(s) within {radius} of {centers.Length} point(s).");
    }

    // Guarantee a clear runway for the player: remove any RPGPP prop / tree /
    // fence whose ground position lands on one of the walking lanes. Overhead
    // stuff (banners, y >= 2.3) and the cottage shells themselves are left alone.
    // The HW2 bridge deck floats ~0.6 m above the grass with square ends - fine
    // for a fly-through camera, but a wall for the CharacterController (0.3 m
    // step). Add a gentle plank ramp (~14 deg) onto each end of the deck.
    static void RampUpBridge()
    {
        var bridge = GameObject.Find("Bridge");
        var deck = bridge != null ? bridge.transform.Find("Deck") : null;
        if (deck == null) { Debug.LogWarning("RampUpBridge: no Bridge/Deck found"); return; }

        Material wood = deck.GetComponent<Renderer>().sharedMaterial;
        float halfLen = deck.localScale.x * 0.5f;
        float top     = deck.localPosition.y + deck.localScale.y * 0.5f;   // deck walking height
        float width   = deck.localScale.z;
        const float run = 2.6f, thick = 0.2f;
        float angle = Mathf.Atan2(top, run) * Mathf.Rad2Deg;
        float slopeLen = Mathf.Sqrt(run * run + top * top) + 0.15f;       // slight overlap onto the deck

        foreach (float side in new[] { -1f, 1f })
        {
            // midpoint of the ramp's top surface, then push down half a plank along its normal
            var mid = new Vector3(side * (halfLen + run * 0.5f), top * 0.5f, deck.localPosition.z);
            // high end must touch the deck: +Z roll lifts local +X, so the west
            // ramp (side -1) needs +angle and the east ramp -angle
            var rot = Quaternion.Euler(0f, 0f, -side * angle);
            var ramp = BoxM(side < 0 ? "RampW" : "RampE", bridge.transform, mid + rot * Vector3.down * (thick * 0.5f),
                            new Vector3(slopeLen, thick, width), wood);
            ramp.transform.localRotation = rot;
            // low side boards so it reads as part of the bridge
            foreach (float zs in new[] { -1f, 1f })
            {
                var kerb = BoxM("Kerb", ramp.transform, new Vector3(0f, 0.9f, zs * 0.47f), new Vector3(1f, 1.2f, 0.06f), wood);
                Object.DestroyImmediate(kerb.GetComponent<Collider>());
            }
        }

        // keep the approach to each ramp free of stalls / benches / barrels
        var t = bridge.transform;
        float foot = halfLen + run, beyond = foot + 3.5f;
        var lanes = new[]
        {
            (t.TransformPoint(new Vector3(-halfLen, 0f, 0f)), t.TransformPoint(new Vector3(-beyond, 0f, 0f))),
            (t.TransformPoint(new Vector3( halfLen, 0f, 0f)), t.TransformPoint(new Vector3( beyond, 0f, 0f))),
        };
        var kill = new System.Collections.Generic.List<GameObject>();
        foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            CollectLaneBlockers(go.transform, lanes, 1.7f, kill);
        foreach (var g in kill) Object.DestroyImmediate(g);
        Debug.Log($"RampUpBridge: ramps added, {kill.Count} prop(s) cleared off the bridge approaches.");
    }

    static void ClearCorridors()
    {
        Vector3 door1 = House1 + new Vector3(H1W * 0.5f + 0.3f, 0f, 0f);   // House_1 entrance
        Vector3 door2 = House2 + new Vector3(4.2f, 0f, 0f);   // House_2 locked door
        Vector3 ret1  = House1 + new Vector3(H1W * 0.5f + 1.2f, 0f, 0f);   // doorstep
        var lanes = new[]
        {
            (PlayerStart, door1), (PlayerStart, door2),
            (ret1, door2), (ret1, door1),
        };
        const float clear = 2.2f;

        var kill = new System.Collections.Generic.List<GameObject>();
        foreach (var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            CollectLaneBlockers(go.transform, lanes, clear, kill);
        foreach (var g in kill) Object.DestroyImmediate(g);
        Debug.Log($"ClearCorridors removed {kill.Count} object(s) off the player's walking lanes.");
    }

    static void CollectLaneBlockers(Transform t, (Vector3 a, Vector3 b)[] lanes, float clear,
                                    System.Collections.Generic.List<GameObject> kill)
    {
        string n = t.name;
        bool isCottageShell = n == "House_1_Enterable" || n == "House_2_Locked";
        bool candidate = !isCottageShell
                         && n.StartsWith("rpgpp_lt_")
                         && !n.Contains("terrain_path")        // ground decal, no collider
                         && t.position.y < 2.3f;               // keep overhead banners
        if (candidate)
        {
            foreach (var lane in lanes)
                if (DistToSegmentXZ(t.position, lane.a, lane.b) < clear) { kill.Add(t.gameObject); return; }
        }
        if (isCottageShell) return;                            // never touch wall boxes
        for (int i = 0; i < t.childCount; i++)
            CollectLaneBlockers(t.GetChild(i), lanes, clear, kill);
    }

    static float DistToSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector2 P = new Vector2(p.x, p.z), A = new Vector2(a.x, a.z), B = new Vector2(b.x, b.z);
        Vector2 ab = B - A;
        float len2 = ab.sqrMagnitude;
        float u = len2 < 1e-4f ? 0f : Mathf.Clamp01(Vector2.Dot(P - A, ab) / len2);
        return Vector2.Distance(P, A + ab * u);
    }

    static void ScatterLine(string name, int count, Vector3 a, Vector3 b, float spread)
    {
        var root = new GameObject(name).transform;
        var rng = new System.Random(name.GetHashCode());
        for (int i = 0; i < count; i++)
        {
            float f = count == 1 ? 0.5f : (float)i / (count - 1);
            Vector3 p = Vector3.Lerp(a, b, f) + new Vector3(
                (float)(rng.NextDouble() * 2 - 1) * spread, 0f,
                (float)(rng.NextDouble() * 2 - 1) * spread);
            p.y = 1f;
            MakeCoin(p, root);
        }
    }

    static Material s_coinRim, s_coinFace;

    static GameObject MakeCoin(Vector3 pos, Transform parent)
    {
        var c = new GameObject("Coin");
        c.transform.SetParent(parent, false);
        c.transform.position = pos;

        var sc = c.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 0.7f;   // easy to walk into

        // minted disc: dark-gold rim + bright relief face + a struck emblem
        if (s_coinRim == null) s_coinRim = MatPBRE(new Color(0.70f, 0.50f, 0.14f), 1f, 0.72f, new Color(0.14f, 0.09f, 0.02f));
        if (s_coinFace == null) s_coinFace = MatPBRE(new Color(1f, 0.84f, 0.30f), 1f, 0.90f, new Color(0.60f, 0.42f, 0.12f));
        var disc = new GameObject("Disc").transform;
        disc.SetParent(c.transform, false);
        disc.localRotation = Quaternion.Euler(90f, 0f, 0f);
        CylM("Rim", disc, Vector3.zero, new Vector3(0.5f, 0.06f, 0.5f), s_coinRim);
        CylM("Face", disc, Vector3.zero, new Vector3(0.42f, 0.08f, 0.42f), s_coinFace);
        BoxM("Emblem", disc, new Vector3(0f, 0.05f, 0f), new Vector3(0.12f, 0.02f, 0.2f), s_coinRim);
        StripCols(disc.gameObject);

        c.AddComponent<Coin>().pickupSound = CoinSound;
        return c;
    }

    static Shader Lit()
    {
        var s = Shader.Find("Universal Render Pipeline/Lit");
        return s != null ? s : Shader.Find("Standard");
    }

    // Mixamo FBX exports reference a diffuse texture Unity can't resolve, so every
    // renderer on the model falls back to Unity's default (bright pink) material.
    // Give the player a pleasant, readable look instead - a warm tunic tone on the
    // body, a lighter skin tone if the mesh happens to have a second material slot.
    static readonly Color CharSkin = new Color(0.85f, 0.68f, 0.55f);
    static readonly Color CharTunic = new Color(0.55f, 0.36f, 0.20f);

    // Recolors the bare Mixamo mesh (skin/tunic fallback for the underlying
    // renderer, in case the avatar isn't humanoid) AND builds an actual outfit -
    // capsule "cloth" rigidly parented to each humanoid bone (torso tunic, short
    // sleeves, a belt, trousers on both legs, boots) - so characters read as
    // dressed people, not a solid-colored mannequin. The clothing pieces are
    // children of the bone that drives them, so they move correctly with every
    // animation (walk / idle / sit).
    public static void DressMixamoModel(GameObject go, Color? tunic = null)
    {
        var tunicCol = tunic ?? CharTunic;
        var skinMat = Mat(CharSkin);
        var tunicMat = Mat(tunicCol);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = mats.Length > 1 && i == 0 ? skinMat : tunicMat;
            r.sharedMaterials = mats;
        }

        var anim = go.GetComponent<Animator>();
        if (anim == null || anim.avatar == null || !anim.avatar.isHuman) return;

        var trouserMat = Mat(new Color(tunicCol.r * 0.5f, tunicCol.g * 0.5f, tunicCol.b * 0.5f));
        var bootMat = Mat(new Color(0.16f, 0.11f, 0.08f));
        var beltMat = Mat(new Color(0.30f, 0.19f, 0.10f));

        Transform Bone(HumanBodyBones b) => anim.GetBoneTransform(b);
        var hips   = Bone(HumanBodyBones.Hips);
        var spine  = Bone(HumanBodyBones.Spine) ?? hips;
        var chest  = Bone(HumanBodyBones.Chest) ?? spine;
        var neck   = Bone(HumanBodyBones.Neck) ?? Bone(HumanBodyBones.Head);
        var lShoulderArm = Bone(HumanBodyBones.LeftUpperArm);
        var rShoulderArm = Bone(HumanBodyBones.RightUpperArm);
        var lElbow = Bone(HumanBodyBones.LeftLowerArm);
        var rElbow = Bone(HumanBodyBones.RightLowerArm);
        var lUpLeg = Bone(HumanBodyBones.LeftUpperLeg);
        var rUpLeg = Bone(HumanBodyBones.RightUpperLeg);
        var lLoLeg = Bone(HumanBodyBones.LeftLowerLeg);
        var rLoLeg = Bone(HumanBodyBones.RightLowerLeg);
        var lFoot  = Bone(HumanBodyBones.LeftFoot);
        var rFoot  = Bone(HumanBodyBones.RightFoot);

        // tunic: torso capsule (hips -> neck) + short sleeves on the upper arms
        LimbCapsule("Tunic_Torso", spine, hips, neck, 0.20f, tunicMat, 1.15f);
        SleeveCap("Sleeve_L", lShoulderArm, lElbow, 0.078f, 0.22f, tunicMat);
        SleeveCap("Sleeve_R", rShoulderArm, rElbow, 0.078f, 0.22f, tunicMat);

        // belt
        if (hips != null)
        {
            var belt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            belt.name = "Belt";
            Object.DestroyImmediate(belt.GetComponent<Collider>());
            belt.transform.SetPositionAndRotation(hips.position, hips.rotation * Quaternion.Euler(90f, 0f, 0f));
            belt.transform.localScale = new Vector3(0.44f, 0.035f, 0.44f);
            belt.GetComponent<Renderer>().sharedMaterial = beltMat;
            belt.transform.SetParent(hips, true);
        }

        // trousers: thigh + shin, each rigidly parented to the bone that drives it
        LimbCapsule("Trouser_UpL", lUpLeg, lUpLeg, lLoLeg, 0.115f, trouserMat, 1.15f);
        LimbCapsule("Trouser_UpR", rUpLeg, rUpLeg, rLoLeg, 0.115f, trouserMat, 1.15f);
        LimbCapsule("Trouser_LoL", lLoLeg, lLoLeg, lFoot, 0.085f, trouserMat, 1.10f);
        LimbCapsule("Trouser_LoR", rLoLeg, rLoLeg, rFoot, 0.085f, trouserMat, 1.10f);

        // boots
        Boot("Boot_L", lFoot, bootMat);
        Boot("Boot_R", rFoot, bootMat);
    }

    // A capsule spanning two joints, parented to (and rigidly following) `parent`
    // - use this for a segment that should move as ONE rigid piece with its
    // driving bone (thigh with the hip, shin with the knee, torso with the spine).
    static void LimbCapsule(string name, Transform parent, Transform a, Transform b, float radius, Material mat, float extend = 1.15f)
    {
        if (parent == null || a == null || b == null) return;
        Vector3 wa = a.position, wb = b.position;
        Vector3 dir = wb - wa;
        float len = dir.magnitude;
        if (len < 0.02f) return;
        var g = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        g.name = name;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.position = (wa + wb) * 0.5f;
        g.transform.up = dir.normalized;
        g.transform.localScale = new Vector3(radius * 2f, (len * extend) * 0.5f, radius * 2f);
        g.GetComponent<Renderer>().sharedMaterial = mat;
        g.transform.SetParent(parent, true);
    }

    // A short capsule anchored AT `from`, pointing toward `towards`, for sleeves/
    // cuffs that shouldn't cover the whole limb.
    static void SleeveCap(string name, Transform from, Transform towards, float radius, float length, Material mat)
    {
        if (from == null) return;
        Vector3 dir = towards != null ? (towards.position - from.position) : Vector3.down;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.down;
        dir.Normalize();
        var g = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        g.name = name;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.position = from.position + dir * (length * 0.5f);
        g.transform.up = dir;
        g.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
        g.GetComponent<Renderer>().sharedMaterial = mat;
        g.transform.SetParent(from, true);
    }

    static void Boot(string name, Transform foot, Material mat)
    {
        if (foot == null) return;
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetPositionAndRotation(foot.position + Vector3.up * 0.03f, foot.rotation);
        g.transform.localScale = new Vector3(0.14f, 0.11f, 0.27f);
        g.GetComponent<Renderer>().sharedMaterial = mat;
        g.transform.SetParent(foot, true);
    }

    static Material Mat(Color c)
    {
        var m = new Material(Lit());
        m.color = c;
        m.SetColor("_BaseColor", c);
        return m;
    }

    // ---- premium PBR materials + primitive helpers ---------------------------
    static Material MatPBR(Color c, float metallic, float smoothness)
    {
        var m = Mat(c);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smoothness);
        return m;
    }
    static Material MatPBRE(Color c, float metallic, float smoothness, Color emis)
    {
        var m = MatPBR(c, metallic, smoothness);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emis);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return m;
    }

    static Material s_oak, s_iron, s_brass, s_velvet, s_goldM, s_wax, s_flameM, s_stoneP, s_ebony;
    static Material Oak    => (s_oak != null ? s_oak : (s_oak = MatPBR(new Color(0.21f, 0.13f, 0.07f), 0f, 0.30f)));
    static Material Ebony  => (s_ebony != null ? s_ebony : (s_ebony = MatPBR(new Color(0.12f, 0.07f, 0.04f), 0f, 0.22f)));
    static Material IronM  => (s_iron != null ? s_iron : (s_iron = MatPBR(new Color(0.09f, 0.09f, 0.10f), 1f, 0.42f)));
    static Material BrassM => (s_brass != null ? s_brass : (s_brass = MatPBRE(new Color(0.80f, 0.60f, 0.24f), 1f, 0.82f, new Color(0.16f, 0.11f, 0.03f))));
    static Material Velvet => (s_velvet != null ? s_velvet : (s_velvet = MatPBR(new Color(0.34f, 0.05f, 0.07f), 0f, 0.12f)));
    static Material GoldM  => (s_goldM != null ? s_goldM : (s_goldM = MatPBRE(new Color(1f, 0.80f, 0.30f), 1f, 0.90f, new Color(0.55f, 0.38f, 0.10f))));
    static Material Wax    => (s_wax != null ? s_wax : (s_wax = MatPBR(new Color(0.94f, 0.90f, 0.78f), 0f, 0.35f)));
    static Material FlameM => (s_flameM != null ? s_flameM : (s_flameM = MatPBRE(new Color(1f, 0.72f, 0.32f), 0f, 0f, new Color(1f, 0.55f, 0.18f) * 5f)));
    static Material StoneP => (s_stoneP != null ? s_stoneP : (s_stoneP = MatPBR(new Color(0.50f, 0.48f, 0.44f), 0f, 0.28f)));

    // doors open silently (the user didn't like the synthetic creak)
    static AudioClip DoorCreak => null;
    static AudioClip CoinSound => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/coin_pickup.wav");

    static GameObject BoxM(string n, Transform p, Vector3 pos, Vector3 size, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = n; g.transform.SetParent(p, false);
        g.transform.localPosition = pos; g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }
    static GameObject CylM(string n, Transform p, Vector3 pos, Vector3 size, Material m, Vector3 euler = default)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = n; g.transform.SetParent(p, false);
        g.transform.localPosition = pos; g.transform.localScale = size;
        g.transform.localRotation = Quaternion.Euler(euler);
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }
    static GameObject Ball(string n, Transform p, Vector3 pos, float d, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = n; g.transform.SetParent(p, false);
        g.transform.localPosition = pos; g.transform.localScale = Vector3.one * d;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }
    static void StripCols(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
    }

    static Material MatE(Color baseC, Color emis)
    {
        var m = new Material(Lit());
        m.color = baseC; m.SetColor("_BaseColor", baseC);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emis);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return m;
    }

    static GameObject Box(string n, Transform parent, Vector3 localPos, Vector3 size, Color c)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = n;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = Mat(c);
        return g;
    }

    static GameObject GlowBox(string n, Transform parent, Vector3 localPos, Vector3 size, Color gold)
    {
        var g = Box(n, parent, localPos, size, gold);
        g.GetComponent<Renderer>().sharedMaterial = MatE(gold, gold * 0.55f);
        return g;
    }

    static GameObject Cyl(string n, Transform parent, Vector3 localPos, Vector3 size, Color c, Vector3 euler = default)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = n;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localScale = size;
        g.transform.localRotation = Quaternion.Euler(euler);
        g.GetComponent<Renderer>().sharedMaterial = Mat(c);
        return g;
    }

    static GameObject Glow(string n, Transform parent, Vector3 localPos, Vector3 size, Color baseC, Color emis)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = n;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localScale = size;
        g.GetComponent<Renderer>().sharedMaterial = MatE(baseC, emis);
        return g;
    }

    static Light AddLight(string n, Transform parent, Vector3 localPos, Color c, float intensity, float range)
    {
        var go = new GameObject(n);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        return l;
    }
}
