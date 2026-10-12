using UnityEngine;

// Plays an ambience loop (the pub crowd) only while the listener/camera is
// INSIDE a box-shaped room, smoothly fading in when you walk/fly in through the
// door and fading out when you leave. The room box is given in the local space
// of `room` (the Pub root), so it keeps working after the pub is moved/rotated.
[RequireComponent(typeof(AudioSource))]
public class InteriorAmbience : MonoBehaviour
{
    public Transform room;                       // the Pub root
    public Vector3 roomMin = new Vector3(-7.5f, -1f, -6f);
    public Vector3 roomMax = new Vector3(7.5f, 8f, 6f);
    public float targetVolume = 0.6f;
    public float fadeSeconds = 1.5f;

    AudioSource _src;

    void Start()
    {
        _src = GetComponent<AudioSource>();
        _src.volume = 0f;
        if (!_src.isPlaying) _src.Play();
    }

    void Update()
    {
        var cam = Camera.main;
        if (cam == null || room == null) return;

        Vector3 p = room.InverseTransformPoint(cam.transform.position);
        bool inside = p.x > roomMin.x && p.x < roomMax.x &&
                      p.y > roomMin.y && p.y < roomMax.y &&
                      p.z > roomMin.z && p.z < roomMax.z;

        float goal = inside ? targetVolume : 0f;
        _src.volume = Mathf.MoveTowards(_src.volume, goal, targetVolume / fadeSeconds * Time.deltaTime);
    }
}
