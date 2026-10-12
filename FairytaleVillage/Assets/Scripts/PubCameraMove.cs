using UnityEngine;

// Slow cinematic camera move across the open front of the pub, for the HW2 video.
public class PubCameraMove : MonoBehaviour
{
    public float duration = 22f;
    public Vector3 lookAt = new Vector3(0f, 3f, -1f);
    public Vector3 startPos = new Vector3(-5.5f, 5.2f, -13.5f);
    public Vector3 endPos = new Vector3(5.5f, 4.6f, -12f);

    float _t;

    void Update()
    {
        _t += Time.deltaTime;
        float u = Mathf.PingPong(_t / duration, 1f);
        u = Mathf.SmoothStep(0f, 1f, u);
        transform.position = Vector3.Lerp(startPos, endPos, u);
        transform.LookAt(lookAt);
    }
}
