using UnityEditor;
using UnityEngine;

// Debug helper: poses the chest lid open (as Chest.cs does at runtime) and
// frames it in the Scene view, so the open look can be checked without playing.
// Does NOT save the scene.
public static class ChestPreviewDebug
{
    [MenuItem("Tools/Homework 3/Debug: Preview Chest Open")]
    public static void Open() => Pose(-110f);

    [MenuItem("Tools/Homework 3/Debug: Preview Chest Closed")]
    public static void Close() => Pose(0f);

    static void Pose(float angle)
    {
        if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name != "HouseInterior")
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/HouseInterior.unity");
        var chest = GameObject.Find("Chest");
        if (chest == null) { Debug.LogWarning("No Chest in the open scene (open HouseInterior)."); return; }
        var lid = chest.GetComponentInChildren<Transform>().Find("Coffer/LidPivot");
        if (lid == null) { Debug.LogWarning("LidPivot not found"); return; }
        lid.localRotation = Quaternion.Euler(angle, 0f, 0f);
        Selection.activeGameObject = null;
        var sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            // look at the chest from where the player walks up (the -Z side),
            // slightly from above, like the third-person camera does
            var pivot = chest.transform.position + new Vector3(0f, 0.9f, 0f);
            sv.LookAt(pivot, Quaternion.Euler(22f, 0f, 0f), 3.2f);
            sv.Repaint();
        }
    }
}
