using UnityEngine;
using UnityEditor;

// HW2 - sets up the Main Camera for the pub video: removes the HW1 village fly-through
// (if present) and adds the slow interior camera move.
public static class PubCameraSetup
{
    [MenuItem("Tools/Pub Interior/Setup Camera")]
    public static void Setup()
    {
        var cam = Camera.main;
        if (cam == null) { Debug.LogError("No Main Camera found."); return; }

        var fly = cam.GetComponent<CameraFlythrough>();
        if (fly != null) Object.DestroyImmediate(fly);

        if (cam.GetComponent<PubCameraMove>() == null) cam.gameObject.AddComponent<PubCameraMove>();
        Selection.activeGameObject = cam.gameObject;
        Debug.Log("Pub camera ready. Enter Play (or record) to run the slow interior move.");
    }
}
