using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// HW2 - spawns simple humanoid characters into the pub and gives each a PubCharacter
// behaviour (sitting patrons, a bartender, a walking barmaid, and one character that
// walks up and down the stairs). Run after "Tools > Pub Interior > Build".
public static class PubCharacters
{
    static readonly Color Skin = new Color(0.85f, 0.68f, 0.55f);

    [MenuItem("Tools/Pub Interior/Add Characters")]
    public static void AddCharacters()
    {
        var pub = GameObject.Find("Pub");
        if (pub == null) { Debug.LogError("Build the pub first (Tools > Pub Interior > Build)."); return; }

        var old = pub.transform.Find("Characters");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var root = new GameObject("Characters").transform;
        root.SetParent(pub.transform, false);

        Color tunicA = new Color(0.35f, 0.28f, 0.18f);
        Color tunicB = new Color(0.30f, 0.42f, 0.28f);
        Color tunicC = new Color(0.45f, 0.22f, 0.20f);
        Color tunicD = new Color(0.28f, 0.30f, 0.42f);

        // seated patrons
        Make("Patron1", new Vector3(-2.5f, 0, -4.55f), 0, tunicA, root).mode = PubCharacter.Mode.Sit;
        Make("Patron2", new Vector3(-2.5f, 0, -1.85f), 180, tunicB, root).mode = PubCharacter.Mode.Sit;
        Make("Patron3", new Vector3(2.2f, 0, -4.85f), 0, tunicC, root).mode = PubCharacter.Mode.Sit;

        // bartender behind the bar (facing into the room)
        Make("Bartender", new Vector3(-6.2f, 0, 0), 90, tunicD, root).mode = PubCharacter.Mode.StandIdle;

        // musician in the corner
        Make("Musician", new Vector3(5.6f, 0, -5f), -25, tunicB, root).mode = PubCharacter.Mode.StandIdle;

        // walking barmaid (serves the room)
        var maid = Make("Barmaid", new Vector3(-2.5f, 0, -1.8f), 0, new Color(0.55f, 0.40f, 0.30f), root);
        maid.mode = PubCharacter.Mode.Patrol;
        maid.moveSpeed = 1.5f;
        maid.patrolPoints = new[]
        {
            new Vector3(-2.5f, 0, -1.8f), new Vector3(2.0f, 0, -1.8f),
            new Vector3(1.5f, 0, -4.6f), new Vector3(-3.5f, 0, -4.4f)
        };

        // character that walks up and down the stairs
        var climber = Make("Climber", new Vector3(5.8f, 0, -4.4f), 0, tunicA, root);
        climber.mode = PubCharacter.Mode.Stairs;
        climber.moveSpeed = 1.7f;
        climber.stairBottom = new Vector3(5.8f, 0, -4.4f);
        climber.stairTop = new Vector3(5.8f, 4.2f, 0.2f);

        Selection.activeGameObject = root.gameObject;
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Added " + root.childCount + " characters to the pub.");
    }

    static Material M(Color c) { var sh = Shader.Find("Universal Render Pipeline/Lit"); if (sh == null) sh = Shader.Find("Standard"); var m = new Material(sh); m.color = c; m.SetColor("_BaseColor", c); return m; }

    static GameObject Box(Vector3 p, Vector3 s, Color c, Transform par, string n = "b")
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = n;
        g.transform.SetParent(par, false); g.transform.localScale = s; g.transform.localPosition = p;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.GetComponent<Renderer>().sharedMaterial = M(c); return g;
    }
    static GameObject Sph(Vector3 p, float d, Color c, Transform par, string n = "s")
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); g.name = n;
        g.transform.SetParent(par, false); g.transform.localScale = Vector3.one * d; g.transform.localPosition = p;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.GetComponent<Renderer>().sharedMaterial = M(c); return g;
    }
    static Transform Empty(string n, Vector3 p, Transform par)
    {
        var g = new GameObject(n); g.transform.SetParent(par, false); g.transform.localPosition = p; return g.transform;
    }

    static PubCharacter Make(string name, Vector3 pos, float yaw, Color tunic, Transform parent)
    {
        Color pants = tunic * 0.6f;
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        root.localPosition = pos; root.localRotation = Quaternion.Euler(0, yaw, 0);

        var hips = Empty("Hips", new Vector3(0, 0.95f, 0), root);

        var torso = Box(new Vector3(0, 0.30f, 0), new Vector3(0.5f, 0.6f, 0.28f), tunic, hips, "Torso").transform;
        Sph(new Vector3(0, 0.45f, 0), 0.42f, Skin, torso, "Head");

        var shoulderL = Empty("ShoulderL", new Vector3(-0.33f, 0.22f, 0), torso);
        Box(new Vector3(0, -0.30f, 0), new Vector3(0.16f, 0.6f, 0.16f), tunic, shoulderL, "ArmL");
        var shoulderR = Empty("ShoulderR", new Vector3(0.33f, 0.22f, 0), torso);
        Box(new Vector3(0, -0.30f, 0), new Vector3(0.16f, 0.6f, 0.16f), tunic, shoulderR, "ArmR");

        var hipL = Empty("HipL", new Vector3(-0.16f, 0, 0), hips);
        var thighL = Box(new Vector3(0, -0.26f, 0), new Vector3(0.2f, 0.52f, 0.2f), pants, hipL, "ThighL").transform;
        var kneeL = Empty("KneeL", new Vector3(0, -0.26f, 0), thighL);
        Box(new Vector3(0, -0.24f, 0), new Vector3(0.18f, 0.48f, 0.18f), pants, kneeL, "ShinL");
        Box(new Vector3(0, -0.48f, 0.05f), new Vector3(0.2f, 0.12f, 0.32f), new Color(0.2f, 0.15f, 0.1f), kneeL, "ShoeL");

        var hipR = Empty("HipR", new Vector3(0.16f, 0, 0), hips);
        var thighR = Box(new Vector3(0, -0.26f, 0), new Vector3(0.2f, 0.52f, 0.2f), pants, hipR, "ThighR").transform;
        var kneeR = Empty("KneeR", new Vector3(0, -0.26f, 0), thighR);
        Box(new Vector3(0, -0.24f, 0), new Vector3(0.18f, 0.48f, 0.18f), pants, kneeR, "ShinR");
        Box(new Vector3(0, -0.48f, 0.05f), new Vector3(0.2f, 0.12f, 0.32f), new Color(0.2f, 0.15f, 0.1f), kneeR, "ShoeR");

        var pc = root.gameObject.AddComponent<PubCharacter>();
        pc.hips = hips; pc.torso = torso; pc.head = torso.Find("Head");
        pc.shoulderL = shoulderL; pc.shoulderR = shoulderR;
        pc.hipL = hipL; pc.hipR = hipR; pc.kneeL = kneeL; pc.kneeR = kneeR;
        return pc;
    }
}
