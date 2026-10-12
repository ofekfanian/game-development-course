using UnityEngine;

// Smoothly orbits the Main Camera around the village during Play mode so the
// whole scene (both banks, bridge, pub, stalls) is shown for a recording.
// Attach it to the Main Camera (Tools > Fairytale Village > Setup Recording Camera).
public class CameraFlythrough : MonoBehaviour
{
    public float duration = 18f;
    public Vector3 target = new Vector3(0f, 2f, 0f);
    public float startAngle = 210f, endAngle = 330f; // degrees around the village
    public float startRadius = 37f, endRadius = 27f;
    public float startHeight = 22f, endHeight = 12f;

    float t;

    void Update()
    {
        t += Time.deltaTime;
        float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
        float ang = Mathf.Lerp(startAngle, endAngle, u) * Mathf.Deg2Rad;
        float radius = Mathf.Lerp(startRadius, endRadius, u);
        float height = Mathf.Lerp(startHeight, endHeight, u);

        transform.position = new Vector3(target.x + Mathf.Cos(ang) * radius, height, target.z + Mathf.Sin(ang) * radius);
        transform.LookAt(target);
    }
}
