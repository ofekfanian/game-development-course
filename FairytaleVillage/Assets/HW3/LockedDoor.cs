using UnityEngine;

// HW3 - the door of House #2. It only opens if the player is carrying a key
// (GameState.HasKey). Otherwise it shows: "You need a key to open this door".
public class LockedDoor : MonoBehaviour
{
    [Header("Refs (set by the builder)")]
    public Transform doorPivot;

    [Header("Tuning")]
    public float openAngle = 100f;
    public float range = 4.5f;
    public float lookAngle = 65f;
    public float doorSpeed = 2f;
    public AudioClip creak;

    bool _open;
    float _t;

    void Update()
    {
        var p = ThirdPersonController.Instance;
        // The "you need a key" hint uses a tight look-cone so it doesn't nag from
        // across the village. Once the player actually HAS the key, standing at
        // the door and pressing SPACE is enough - no precise aiming required.
        bool inHintCone = InteractUtil.CanInteract(p, transform, range, lookAngle);
        bool atDoor     = InteractUtil.CanInteract(p, transform, range, 180f);

        if (!_open)
        {
            if (!GameState.HasKey)
            {
                if (inHintCone) HudUI.Show("You need a key to open this door");
            }
            else if (atDoor)
            {
                HudUI.Show("Press SPACE to unlock the door");
                if (p.InteractPressed())
                {
                    _open = true;
                    if (creak != null && doorPivot != null)
                        AudioSource.PlayClipAtPoint(creak, doorPivot.position, 1f);
                    HudUI.Show("The door is unlocked!", 2.5f);
                }
            }
        }

        _t = Mathf.MoveTowards(_t, _open ? 1f : 0f, doorSpeed * Time.deltaTime);
        if (doorPivot != null)
            doorPivot.localRotation = Quaternion.Euler(0f, Mathf.Lerp(0f, openAngle, _t), 0f);
    }
}
