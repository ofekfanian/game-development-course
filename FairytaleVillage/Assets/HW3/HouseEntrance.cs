using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// HW3 - the door of House #1. The house's interior is defined as its OWN scene
// (HouseInterior). Instead of cutting to it, that scene is loaded ADDITIVELY when
// the village starts and placed exactly inside House #1's walls, so the player
// presses SPACE, the door swings open, and they simply walk in - no fade, no jump.
// Coins/key live on GameState, so everything carries over naturally.
public class HouseEntrance : MonoBehaviour
{
    public string sceneName = "HouseInterior";
    public float range = 4.5f;
    public float lookAngle = 65f;

    [Header("Door animation (set by the builder)")]
    public Transform doorPivot;
    public float openAngle = 90f;
    public float openSeconds = 0.8f;
    public AudioClip creak;

    [Header("Where the interior sits inside the house")]
    // the interior room is built with its door on -Z; the house's door is on
    // its local -X, so the room is turned 90 degrees and lifted onto the floor
    public float interiorYaw = 90f;
    public float floorLift = 0.125f;

    // things in the interior scene that only make sense when it is played on
    // its own (its own exit door + the patch of grass outside it)
    static readonly string[] StandaloneOnly =
        { "ExitDoorPivot", "ExitDoorway", "ExitFrameL", "ExitFrameR", "OutsideGrass", "OutsidePath" };

    bool _open;

    IEnumerator Start()
    {
        if (SceneManager.GetSceneByName(sceneName).isLoaded) yield break;
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (op == null) yield break;
        yield return op;
        PlaceInterior(SceneManager.GetSceneByName(sceneName));
    }

    void PlaceInterior(Scene scene)
    {
        var house = transform.parent;
        var anchor = new GameObject("InteriorAnchor");
        SceneManager.MoveGameObjectToScene(anchor, scene);
        anchor.transform.SetPositionAndRotation(house.TransformPoint(0f, floorLift, 0f),
                                                house.rotation * Quaternion.Euler(0f, interiorYaw, 0f));

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root == anchor) continue;
            if (StandaloneOnlyObject(root)) { Destroy(root); continue; }
            // keep each object's layout, now relative to the house
            root.transform.SetParent(anchor.transform, false);
        }
    }

    static bool StandaloneOnlyObject(GameObject root)
    {
        if (System.Array.IndexOf(StandaloneOnly, root.name) >= 0) return true;
        if (root.GetComponentInChildren<ThirdPersonController>(true) != null) return true;   // its own player
        if (root.GetComponentInChildren<Camera>(true) != null) return true;
        if (root.GetComponentInChildren<UnityEngine.Rendering.Volume>(true) != null) return true; // village has post-FX
        var l = root.GetComponent<Light>();
        if (l != null && l.type == LightType.Directional) return true;                        // village has a sun
        return false;
    }

    [Header("Closing")]
    public float autoCloseDistance = 4f;   // door swings shut once the player walks this far off

    bool _busy;
    float _openedAt;

    // The door starts CLOSED. SPACE opens it and SPACE closes it again, from
    // outside or from inside the room; it also closes by itself once the player
    // has walked away from the doorway.
    void Update()
    {
        var p = ThirdPersonController.Instance;
        if (p == null || _busy) return;

        if (_open && Time.time - _openedAt > 1.5f &&
            Vector3.Distance(p.transform.position, transform.position) > autoCloseDistance)
        {
            StartCoroutine(Move(false));
            return;
        }

        if (!InteractUtil.CanInteract(p, transform, range, lookAngle)) return;
        HudUI.Show(_open ? "Press SPACE to close the door" : "Press SPACE to open the door");
        if (p.InteractPressed()) StartCoroutine(Move(!_open));
    }

    IEnumerator Move(bool open)
    {
        _busy = true;
        _open = open;
        if (open) _openedAt = Time.time;
        yield return DoorAnim.Swing(doorPivot, open ? openAngle : 0f, openSeconds, creak);
        _busy = false;
    }
}
