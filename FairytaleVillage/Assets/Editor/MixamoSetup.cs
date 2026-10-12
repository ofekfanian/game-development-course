using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

// HW2 upgrade - imports the Mixamo FBX files as Humanoid, builds an Animator
// Controller with real states + transitions (Idle / Walk / Sit / StairUp /
// StairDown), and replaces the primitive characters with animated Mixamo ones.
//   Menu:  Tools > Pub Interior > Setup Mixamo Characters
public static class MixamoSetup
{
    const string DIR = "Assets/Mixamo/";
    static readonly string[] Files =
        { "Breathing Idle", "Sitting Idle", "Walking Animation", "Walking Up The Stairs", "Descending Stairs" };

    [MenuItem("Tools/Pub Interior/Setup Mixamo Characters")]
    public static void Setup()
    {
        AssetDatabase.Refresh();

        // 1) import each FBX as Humanoid + loop its clip
        foreach (var name in Files)
        {
            string path = DIR + name + ".fbx";
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) { Debug.LogError("Missing FBX (import may still be running): " + path); return; }
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            var clips = imp.clipAnimations;
            if (clips == null || clips.Length == 0) clips = imp.defaultClipAnimations;
            foreach (var c in clips) c.loopTime = true;
            imp.clipAnimations = clips;
            imp.SaveAndReimport();
        }

        // 2) load the animation clips
        var idle = LoadClip("Breathing Idle");
        var walk = LoadClip("Walking Animation");
        var sit = LoadClip("Sitting Idle");
        var up = LoadClip("Walking Up The Stairs");
        var down = LoadClip("Descending Stairs");
        if (idle == null || walk == null || sit == null || up == null || down == null)
        { Debug.LogError("Could not load one or more Mixamo clips."); return; }

        // 3) build the Animator Controller with parameters + transitions
        string cpath = DIR + "PubAnimator.controller";
        AssetDatabase.DeleteAsset(cpath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(cpath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Sit", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("StairUp", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("StairDown", AnimatorControllerParameterType.Bool);

        var sm = ctrl.layers[0].stateMachine;
        var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
        var sWalk = sm.AddState("Walk"); sWalk.motion = walk;
        var sSit = sm.AddState("Sit"); sSit.motion = sit;
        var sUp = sm.AddState("StairUp"); sUp.motion = up;
        var sDown = sm.AddState("StairDown"); sDown.motion = down;
        sm.defaultState = sIdle;

        Any(sm, sSit, t => t.AddCondition(AnimatorConditionMode.If, 0, "Sit"));
        Any(sm, sUp, t => t.AddCondition(AnimatorConditionMode.If, 0, "StairUp"));
        Any(sm, sDown, t => t.AddCondition(AnimatorConditionMode.If, 0, "StairDown"));
        Any(sm, sWalk, t =>
        {
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "Sit");
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "StairUp");
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "StairDown");
        });
        Any(sm, sIdle, t =>
        {
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "Sit");
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "StairUp");
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "StairDown");
        });
        AssetDatabase.SaveAssets();

        // 4) place the characters in the pub
        var pub = GameObject.Find("Pub");
        if (pub == null) { Debug.LogError("Build the pub first (Tools > Pub Interior > Build)."); return; }
        var prim = pub.transform.Find("Characters");
        if (prim != null) prim.gameObject.SetActive(false); // hide the old primitive cast
        var oldM = pub.transform.Find("MixamoCharacters");
        if (oldM != null) Object.DestroyImmediate(oldM.gameObject);
        var root = new GameObject("MixamoCharacters").transform; root.SetParent(pub.transform, false);

        // Real clothed + faced Mixamo character models (imported via
        // Tools > Pub Interior > Import Mixamo Characters). One per NPC for
        // variety. Falls back to the plain mannequin (with code-built clothing)
        // for any slot whose character file is missing, so the pub still builds.
        var fallback = AssetDatabase.LoadAssetAtPath<GameObject>(DIR + "Breathing Idle.fbx");
        var chars = new GameObject[MixamoCharacterImport.CharFiles.Length];
        for (int i = 0; i < chars.Length; i++)
            chars[i] = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Mixamo/Characters/" + MixamoCharacterImport.CharFiles[i] + ".fbx");
        GameObject Model(int idx) => chars[idx] != null ? chars[idx] : fallback;
        bool NeedsDress(int idx) => chars[idx] == null;   // real chars are already clothed/faced

        // Seat two patrons on the FRONT (-Z) bench of the first two ground tables,
        // facing the table (+Z = Mixamo forward). Benches have no backrest, so the
        // sit pose lands cleanly; a SeatSnap fixes the hip height per character.
        float benchZ = 0.95f;   // matches PubBuilder BenchZ
        var patron1 = Make(Model(0), ctrl, "Patron1",
            PubBuilder.GroundTables[0] + new Vector3(0, 0, -benchZ), 0, root, NeedsDress(0));
        patron1.mode = MixamoActor.Mode.Sit; SeatIt(patron1);
        var patron2 = Make(Model(1), ctrl, "Patron2",
            PubBuilder.GroundTables[1] + new Vector3(0, 0, -benchZ), 0, root, NeedsDress(1));
        patron2.mode = MixamoActor.Mode.Sit; SeatIt(patron2);

        // more patrons on the BACK benches (facing the table, yaw 180) + table 3,
        // so the pub feels busy and full of people
        var patron3 = Make(Model(2), ctrl, "Patron3",
            PubBuilder.GroundTables[0] + new Vector3(0, 0, benchZ), 180, root, NeedsDress(2));
        patron3.mode = MixamoActor.Mode.Sit; SeatIt(patron3);
        var patron4 = Make(Model(3), ctrl, "Patron4",
            PubBuilder.GroundTables[1] + new Vector3(0, 0, benchZ), 180, root, NeedsDress(3));
        patron4.mode = MixamoActor.Mode.Sit; SeatIt(patron4);
        var patron5 = Make(Model(4), ctrl, "Patron5",
            PubBuilder.GroundTables[2] + new Vector3(0, 0, -benchZ), 0, root, NeedsDress(4));
        patron5.mode = MixamoActor.Mode.Sit; SeatIt(patron5);

        // three customers standing at the bar (facing the counter on the left, -X => yaw 270)
        Make(Model(0), ctrl, "BarPatron1", new Vector3(-4.7f, 0, -2f), 270, root, NeedsDress(0)).mode = MixamoActor.Mode.StandIdle;
        Make(Model(3), ctrl, "BarPatron2", new Vector3(-4.7f, 0, 1.6f), 270, root, NeedsDress(3)).mode = MixamoActor.Mode.StandIdle;
        Make(Model(1), ctrl, "BarPatron3", new Vector3(-4.7f, 0, 3.8f), 270, root, NeedsDress(1)).mode = MixamoActor.Mode.StandIdle;

        // a barmaid working BEHIND the bar (facing the guests, +X => yaw 90)
        Make(Model(4), ctrl, "Barmaid_Bar", new Vector3(-7.0f, 0, 2.6f), 90, root, NeedsDress(4)).mode = MixamoActor.Mode.StandIdle;

        // two more patrons seated UPSTAIRS on the mezzanine table benches
        // (placed on the mezzanine floor, y = H1 + 0.15; SeatSnap lifts to the bench)
        float upY = 4.35f, benchZup = 0.95f;
        var pUp1 = Make(Model(2), ctrl, "Patron6", new Vector3(-2.5f, upY, 3.2f - benchZup), 0, root, NeedsDress(2));
        pUp1.mode = MixamoActor.Mode.Sit; SeatIt(pUp1);
        var pUp2 = Make(Model(0), ctrl, "Patron7", new Vector3(2.5f, upY, 4.2f - benchZup), 0, root, NeedsDress(0));
        pUp2.mode = MixamoActor.Mode.Sit; SeatIt(pUp2);

        // a couple of patrons standing and chatting by the fireplace / floor
        Make(Model(3), ctrl, "Patron8", new Vector3(-1.5f, 0, 3.8f), 20, root, NeedsDress(3)).mode = MixamoActor.Mode.StandIdle;
        Make(Model(1), ctrl, "Patron9", new Vector3(1.2f, 0, 3.6f), -30, root, NeedsDress(1)).mode = MixamoActor.Mode.StandIdle;
        // behind the bar counter (counter front spans x -6.65..-5.65), not in front of it
        Make(Model(2), ctrl, "Bartender", new Vector3(-7.0f, 0, 0), 90, root, NeedsDress(2)).mode = MixamoActor.Mode.StandIdle;

        // A straight lane at x=0: the gap between table1/table3 (x up to -0.75)
        // and table2 (x from 0.45), clear of the bar and stairs too - verified
        // against the actual table/chair footprints, not just endpoint checks
        // (a diagonal path between distant "safe" points can still cut through
        // furniture in the middle, which was the previous bug).
        // footstep / stair-creak SFX clips (background music was removed per the
        // instructor's feedback; these situational sounds replace it)
        // Real recorded wood-floor footsteps from the Asset Store pack
        // "Footsteps - Essentials" (Nox_Sound, free). Falls back to the old
        // synthesized taps if the pack isn't imported.
        var stepList = new System.Collections.Generic.List<AudioClip>();
        // 1st choice: the user's own footsteps (Pixabay), sliced into single steps
        for (int i = 1; i <= 10; i++)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Real/step_real_" + i.ToString("00") + ".wav");
            if (c != null) stepList.Add(c);
        }
        // 2nd choice: the Asset Store wood-floor footsteps
        const string WoodWalk = "Assets/Footsteps - Essentials/Footsteps_Wood/Footsteps_Wood_Walk/Footsteps_Wood_Walk_";
        if (stepList.Count == 0)
            for (int i = 1; i <= 10; i++)
            {
                var c = AssetDatabase.LoadAssetAtPath<AudioClip>(WoodWalk + i.ToString("00") + ".wav");
                if (c != null) stepList.Add(c);
            }
        if (stepList.Count == 0)
            for (int i = 1; i <= 4; i++)
                stepList.Add(AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/step_" + i + ".wav"));
        var steps = stepList.ToArray();
        // on the stairs: heavier "run" wood steps (reads as climbing wooden stairs)
        var creak = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Footsteps - Essentials/Footsteps_Wood/Footsteps_Wood_Run/Footsteps_Wood_Run_03.wav")
            ?? AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/stair_creak.wav");

        var maid = Make(Model(3), ctrl, "Barmaid", new Vector3(0f, 0, -5.3f), 0, root, NeedsDress(3));
        maid.mode = MixamoActor.Mode.Patrol; maid.moveSpeed = 1.4f;
        maid.patrolPoints = new[] { new Vector3(0f, 0, -5.3f), new Vector3(0f, 0, 3.0f) };
        AddFootsteps(maid, steps, null);

        var climber = Make(Model(4), ctrl, "Climber", new Vector3(5.8f, 0, -4.4f), 0, root, NeedsDress(4));
        climber.mode = MixamoActor.Mode.Stairs; climber.moveSpeed = 1.7f;
        climber.stairBottom = new Vector3(5.8f, 0, -4.4f);
        climber.stairTop = new Vector3(5.8f, 4.2f, 0.2f);
        AddFootsteps(climber, steps, creak);

        Selection.activeGameObject = root.gameObject;
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Mixamo characters set up: " + root.childCount + " (Animator Controller at " + cpath + ")");
    }

    static void Any(AnimatorStateMachine sm, AnimatorState to, System.Action<AnimatorStateTransition> addConds)
    {
        var t = sm.AddAnyStateTransition(to);
        t.hasExitTime = false; t.duration = 0.15f; t.canTransitionToSelf = false;
        addConds(t);
    }

    static AnimationClip LoadClip(string name)
    {
        string path = DIR + name + ".fbx";
        foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
        {
            var c = o as AnimationClip;
            if (c != null && !c.name.StartsWith("__preview")) return c;
        }
        Debug.LogError("No AnimationClip found in " + path);
        return null;
    }

    static readonly Color[] Tunics =
    {
        new Color(0.35f, 0.28f, 0.18f), new Color(0.30f, 0.42f, 0.28f),
        new Color(0.45f, 0.22f, 0.20f), new Color(0.28f, 0.30f, 0.42f),
        new Color(0.55f, 0.40f, 0.30f)
    };
    static int s_tunicIdx;

    // Adds a SeatSnap so the seated character's hips rest on the bench top,
    // regardless of the individual character's leg length.
    static void SeatIt(MixamoActor patron)
    {
        var snap = patron.gameObject.AddComponent<SeatSnap>();
        snap.seatTopY = PubBuilder.BenchTopY;
        snap.hipAboveSeat = 0.06f;
    }

    // Adds a 3D AudioSource to a moving character and hands its clips to the
    // MixamoActor, which triggers them in step with the character's travel.
    static void AddFootsteps(MixamoActor actor, AudioClip[] steps, AudioClip creak)
    {
        var src = actor.gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 1f;       // 3D - louder when the camera is near
        src.minDistance = 2f;
        src.maxDistance = 18f;
        src.rolloffMode = AudioRolloffMode.Linear;
        actor.footAudio = src;
        actor.stepClips = steps;
        actor.stairCreakClip = creak;
    }

    static MixamoActor Make(GameObject model, AnimatorController ctrl, string name, Vector3 pos, float yaw, Transform parent, bool dress)
    {
        var go = (GameObject)Object.Instantiate(model);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        // Only the bare fallback mannequin needs code-built clothing; the real
        // Mixamo character models already ship clothed and faced.
        if (dress) Hw3Builder.DressMixamoModel(go, Tunics[s_tunicIdx++ % Tunics.Length]);
        var anim = go.GetComponent<Animator>();
        if (anim == null) anim = go.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;
        return go.AddComponent<MixamoActor>();
    }
}
