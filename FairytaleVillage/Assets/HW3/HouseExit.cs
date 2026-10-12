using System.Collections;
using UnityEngine;

// HW3 - the way out of House #1's interior scene. Press SPACE at the doorway (or
// just walk into it) -> the door swings open and stays open. The player walks
// out on foot; once past the threshold the screen fades back to the village.
// GameState.ReturningFromHouse tells the village to drop the player right next to
// this house, facing forward (handled by VillageSpawns).
public class HouseExit : MonoBehaviour
{
    public string sceneName = "Village";
    public float range = 3.6f;
    public float lookAngle = 70f;

    [Header("Door animation (set by the builder)")]
    public Transform doorPivot;
    public float openAngle = 100f;
    public float openSeconds = 0.8f;
    public AudioClip creak;

    [Header("Threshold (world Z; the door wall is at z = -6)")]
    public float outsideZ = -6.4f;

    bool _open, _leaving;

    void Update()
    {
        var p = ThirdPersonController.Instance;
        if (p == null || _leaving) return;

        if (_open)
        {
            if (p.transform.position.z < outsideZ)
            {
                _leaving = true;
                GameState.ReturningFromHouse = true;
                ScreenFade.FadeAndLoad(sceneName);
            }
            return;
        }

        if (!InteractUtil.CanInteract(p, transform, range, lookAngle)) return;
        HudUI.Show("Press SPACE to open the door");
        if (p.InteractPressed()) OpenDoor();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<ThirdPersonController>() != null) OpenDoor();
    }

    void OpenDoor()
    {
        if (_open) return;
        _open = true;
        StartCoroutine(OpenRoutine());
    }

    IEnumerator OpenRoutine()
    {
        yield return DoorAnim.Swing(doorPivot, openAngle, openSeconds, creak);
        HudUI.Show("Walk out to go back to the village", 1.5f);
    }
}
