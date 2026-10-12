using UnityEngine;

// HW3 - decides where the player stands when the Village scene loads:
//  * first time            -> the start point near the bridge
//  * back from the house    -> right next to House #1, looking forward into the
//                              village (that's the "מבט קדימה" requirement)
public class VillageSpawns : MonoBehaviour
{
    public Vector3 startPos = new Vector3(24f, 0f, 0f);
    public float startYaw = 90f;

    public Vector3 houseReturnPos = new Vector3(29f, 0f, 8f);
    public float houseReturnYaw = 270f;

    void Start()
    {
        var p = ThirdPersonController.Instance;
        if (p == null) return;

        bool returning = GameState.ReturningFromHouse;
        GameState.ReturningFromHouse = false;

        Vector3 pos = returning ? houseReturnPos : startPos;
        float yaw = returning ? houseReturnYaw : startYaw;

        // CharacterController fights direct transform writes - disable it for the teleport.
        var cc = p.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        p.transform.SetPositionAndRotation(pos + Vector3.up * 0.15f, Quaternion.Euler(0f, yaw, 0f));
        if (cc != null) cc.enabled = true;
    }
}
