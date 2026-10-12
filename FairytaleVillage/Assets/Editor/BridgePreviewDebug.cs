using UnityEditor;
using UnityEngine;

// Debug helpers for the village bridge (with its HW3 ramps).
public static class BridgePreviewDebug
{
    [MenuItem("Tools/Homework 3/Debug: Look At Bridge")]
    public static void Look()
    {
        var b = GameObject.Find("Bridge");
        var sv = SceneView.lastActiveSceneView;
        if (b == null || sv == null) { Debug.LogWarning("No Bridge / Scene view"); return; }
        sv.LookAt(b.transform.position + Vector3.up * 0.5f, Quaternion.Euler(12f, 20f, 0f), 7f);
        sv.Repaint();
    }

    // Walks a virtual player capsule along the bridge axis: prints the ground
    // height profile (raycast down) and any collider that would block a
    // 0.3m-radius / 1.8m capsule moving between consecutive samples.
    [MenuItem("Tools/Homework 3/Debug: Probe Bridge Path")]
    public static void Probe()
    {
        var b = GameObject.Find("Bridge");
        if (b == null) { Debug.LogWarning("No Bridge"); return; }
        Physics.SyncTransforms();
        var t = b.transform;
        var sb = new System.Text.StringBuilder("BRIDGE PROBE (bridge pos " + t.position + " rot " + t.eulerAngles + ")\n");
        Vector3 prev = Vector3.zero; bool hasPrev = false;
        for (float x = -11f; x <= 11.01f; x += 0.5f)
        {
            Vector3 top = t.TransformPoint(new Vector3(x, 0f, 0f)) + Vector3.up * 5f;
            if (!Physics.Raycast(top, Vector3.down, out var hit, 10f, ~0, QueryTriggerInteraction.Ignore))
            { sb.AppendLine($"x={x:0.0}: NO GROUND"); continue; }
            sb.Append($"x={x:0.0}: ground y={hit.point.y:0.00} on {hit.collider.name}");
            Vector3 feet = hit.point;
            if (hasPrev)
            {
                float rise = feet.y - prev.y;
                if (rise > 0.35f) sb.Append($"  <-- STEP {rise:0.00}m too high");
                Vector3 p1 = prev + Vector3.up * 0.65f, p2 = prev + Vector3.up * 1.5f;
                Vector3 dir = feet - prev; dir.y = 0f;
                foreach (var h in Physics.CapsuleCastAll(p1, p2, 0.28f, dir.normalized, dir.magnitude, ~0, QueryTriggerInteraction.Ignore))
                    sb.Append($"  BLOCKED by {Path(h.collider.transform)}");
            }
            sb.AppendLine();
            prev = feet; hasPrev = true;
        }
        Debug.Log(sb.ToString());
    }

    // Opens the door pivot of whichever HW3 scene is loaded, sweeps a player
    // capsule through the doorway, logs anything in the way, then closes it again.
    [MenuItem("Tools/Homework 3/Debug: Probe Doorway")]
    public static void ProbeDoorway()
    {
        Transform pivot; Vector3 a, b;
        var house = GameObject.Find("House_1_Enterable");
        if (house != null)
        {
            pivot = house.transform.Find("DoorPivot");
            float doorX = house.transform.Find("Entrance").localPosition.x;         // just outside the door
            a = house.transform.TransformPoint(new Vector3(doorX - 1.5f, 0f, 0f));  // on the step
            b = house.transform.TransformPoint(new Vector3(doorX + 4.5f, 0f, 0f));  // well inside
        }
        else
        {
            var p = GameObject.Find("ExitDoorPivot");
            pivot = p != null ? p.transform : null;
            a = new Vector3(0f, 0f, -4.4f); b = new Vector3(0f, 0f, -7.5f);
        }
        if (pivot == null) { Debug.LogWarning("DOORWAY PROBE: no door pivot in this scene"); return; }
        var old = pivot.localRotation;
        pivot.localRotation = Quaternion.Euler(0f, 90f, 0f);
        Physics.SyncTransforms();
        var sb = new System.Text.StringBuilder($"DOORWAY PROBE {a} -> {b}\n");
        Vector3 dir = b - a;
        int n = 0;
        foreach (var h in Physics.CapsuleCastAll(a + Vector3.up * 0.65f, a + Vector3.up * 1.5f, 0.28f, dir.normalized, dir.magnitude, ~0, QueryTriggerInteraction.Ignore))
        { sb.AppendLine("  BLOCKED by " + Path(h.collider.transform) + " at " + h.point); n++; }
        sb.AppendLine(n == 0 ? "  clear" : $"  {n} blocker(s)");
        pivot.localRotation = old;
        Physics.SyncTransforms();
        Debug.Log(sb.ToString());
    }

    // Play-mode helpers: put the player on House 1's doorstep (door open) or a
    // few steps inside, facing into the house, to eyeball the walk-in.
    [MenuItem("Tools/Homework 3/Debug: Player At House1 Door")]
    public static void AtDoor() => PlacePlayer(-1.6f);

    [MenuItem("Tools/Homework 3/Debug: Player Inside House1")]
    public static void Inside() => PlacePlayer(2.5f);

    [MenuItem("Tools/Homework 3/Debug: Turn Player 60")]
    public static void Turn()
    {
        var p = ThirdPersonController.Instance;
        if (Application.isPlaying && p != null) p.transform.Rotate(0f, 60f, 0f, Space.World);
    }

    [MenuItem("Tools/Homework 3/Debug: Player In House2")]
    public static void InHouse2()
    {
        var p = ThirdPersonController.Instance;
        var house = GameObject.Find("House_2_Locked");
        if (!Application.isPlaying || p == null || house == null) return;
        var cc = p.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        p.transform.SetPositionAndRotation(house.transform.TransformPoint(new Vector3(-3.0f, 0.2f, 0f)),
            Quaternion.LookRotation(house.transform.TransformDirection(Vector3.right), Vector3.up));
        if (cc != null) cc.enabled = true;
    }

    static void PlacePlayer(float offsetFromEntrance)
    {
        var p = ThirdPersonController.Instance;
        var house = GameObject.Find("House_1_Enterable");
        if (!Application.isPlaying || p == null || house == null) { Debug.LogWarning("Play the Village scene first"); return; }
        var ent = house.transform.Find("Entrance");
        var pivot = house.transform.Find("DoorPivot");
        if (pivot != null) pivot.localRotation = Quaternion.Euler(0f, 90f, 0f);
        Vector3 pos = house.transform.TransformPoint(new Vector3(ent.localPosition.x + offsetFromEntrance, 0.2f, 0f));
        Vector3 inward = house.transform.TransformDirection(Vector3.right);
        var cc = p.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        p.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(inward, Vector3.up));
        if (cc != null) cc.enabled = true;
    }

    static string Path(Transform t)
    {
        string p = t.name;
        for (var q = t.parent; q != null; q = q.parent) p = q.name + "/" + p;
        return p;
    }
}
