using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// HW2 - builds a single scene with the HW1 village AND the pub, then a camera
// path that flies over the village and into the pub. The pub is placed beside the
// village (world x=24, rotated so its open front faces the village).
public static class CombinedBuilder
{
    [MenuItem("Tools/Pub Interior/Build Combined World + Pub")]
    public static void BuildCombined()
    {
        foreach (var n in new[] { "Village", "Pub" })
        {
            var g = GameObject.Find(n);
            if (g != null) Object.DestroyImmediate(g);
        }

        // 1) pub at origin, with Mixamo characters + audio
        PubBuilder.Build();
        MixamoSetup.Setup();
        PubAudio.AddAudio();

        // 2) move the pub beside the village; drop its own grass plane
        var pub = GameObject.Find("Pub").transform;
        var grass = pub.Find("Shell/Ground");
        if (grass != null) Object.DestroyImmediate(grass.gameObject);
        pub.position = new Vector3(40f, 0.2f, 0f); // clear of the (much larger) real-asset houses; lifted to avoid z-fighting
        pub.rotation = Quaternion.Euler(0f, 90f, 0f);

        // 3) village at origin (does not clear the pub) - real-asset version
        RealVillageBuilder.Build();

        // 4) camera: single continuous path over the village and into the pub
        var cam = Camera.main;
        if (cam != null)
        {
            Remove<CameraFlythrough>(cam.gameObject);
            Remove<PubCameraMove>(cam.gameObject);
            // Always replace (not reuse) so edits to the waypoint fields in code
            // actually take effect - an existing component keeps its stale
            // serialized values across rebuilds otherwise.
            Remove<CombinedCameraMove>(cam.gameObject);
            cam.gameObject.AddComponent<CombinedCameraMove>();
        }

        // 5) professional lighting + post-processing pass (warm sun, soft
        // shadows, fog, tonemapping/bloom/color-grade/vignette)
        ScenePolish.Polish();

        Selection.activeGameObject = GameObject.Find("Pub");
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Combined world + pub built. Enter Play (or record) for the fly-through.");
    }

    static void Remove<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c != null) Object.DestroyImmediate(c);
    }
}
