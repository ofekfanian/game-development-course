using UnityEngine;

// Flies the camera across the village and then into the pub for the combined
// "world then pub" video. Waypoints are public so they can be tuned in the
// Inspector without recompiling. The pub interior sits around world (24, 3, 0)
// with its open front facing -x (see CombinedBuilder).
public class CombinedCameraMove : MonoBehaviour
{
    public float duration = 26f;

    // Tuned for the real-asset village (houses ~17 units off the river centerline,
    // spanning roughly z = -50..50) with the pub placed at world x=40.
    // The pub is placed at world x=40, rotated yaw 90, so its (wide, doorway)
    // front faces -x and the doorway opening sits at world x=34, z in [-3,3].
    // The camera flies head-on (z=0) through the doorway and ends inside among
    // the tables, looking toward the fireplace on the back wall (world x=46).
    public Vector3[] positions = {
        new Vector3(-46f, 32f, -55f), // high, establishing the whole village
        new Vector3(-6f, 24f, -40f),  // pan across the village
        new Vector3(16f, 15f, -12f),  // drift toward the pub
        new Vector3(26f, 9f, 0f),     // line up head-on with the entrance
        new Vector3(33f, 6f, 0f),     // pass through the doorway
        new Vector3(37.5f, 5f, 0f),   // settle inside among the patrons
    };
    public Vector3[] lookAts = {
        new Vector3(0f, 4f, 0f),
        new Vector3(10f, 3f, 0f),
        new Vector3(32f, 4f, 0f),
        new Vector3(38f, 3f, 0f),
        new Vector3(42f, 3f, 0f),
        new Vector3(45f, 3f, 0f),
    };

    float _t;

    void Update()
    {
        _t += Time.deltaTime;
        float u = Mathf.Clamp01(_t / duration) * (positions.Length - 1);
        int i = Mathf.Min((int)u, positions.Length - 2);
        float f = Mathf.SmoothStep(0f, 1f, u - i);

        transform.position = Vector3.Lerp(positions[i], positions[i + 1], f);
        Vector3 look = Vector3.Lerp(lookAts[i], lookAts[i + 1], f);
        transform.rotation = Quaternion.LookRotation((look - transform.position).normalized, Vector3.up);
    }
}
