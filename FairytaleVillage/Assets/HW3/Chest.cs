using UnityEngine;

// HW3 - the chest inside House #1's interior scene.
//  * Player near + looking at it, press SPACE  -> the lid swings open.
//  * Lid open, press SPACE again              -> "take" the key (GameState.HasKey).
// State is mirrored into GameState so re-entering the house shows it already
// opened / the key already gone.
public class Chest : MonoBehaviour
{
    [Header("Refs (set by the builder)")]
    public Transform lidPivot;
    public GameObject key;

    [Header("Tuning")]
    public float openAngle = -110f;   // rotation of the lid pivot about X
    public float range = 4f;
    public float lookAngle = 60f;
    public float lidSpeed = 2.5f;

    bool _open;
    float _t;   // 0 = closed, 1 = open

    void Start()
    {
        if (GameState.ChestOpen) { _open = true; _t = 1f; }
        if (GameState.HasKey && key != null) key.SetActive(false);
        ApplyLid();
    }

    void Update()
    {
        var p = ThirdPersonController.Instance;
        bool looking = InteractUtil.CanInteract(p, transform, range, lookAngle);

        if (looking)
        {
            if (!_open)
            {
                HudUI.Show("Press SPACE to open the chest");
                if (p.InteractPressed())
                {
                    _open = true;
                    GameState.ChestOpen = true;
                }
            }
            else if (key != null && key.activeSelf && !GameState.HasKey)
            {
                HudUI.Show("Press SPACE to take the key");
                if (p.InteractPressed())
                {
                    GameState.HasKey = true;
                    key.SetActive(false);
                    HudUI.Show("You picked up a key!", 2.5f);
                }
            }
        }

        _t = Mathf.MoveTowards(_t, _open ? 1f : 0f, lidSpeed * Time.deltaTime);
        ApplyLid();

        if (key != null && key.activeSelf)
            key.transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
    }

    void ApplyLid()
    {
        if (lidPivot != null)
            lidPivot.localRotation = Quaternion.Euler(Mathf.Lerp(0f, openAngle, _t), 0f, 0f);
    }
}
