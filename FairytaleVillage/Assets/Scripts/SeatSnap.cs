using UnityEngine;

// HW2 - snaps a seated Mixamo character so its hips rest on the chair seat.
// Different real Mixamo characters have different leg lengths, so the same
// "Sitting Idle" clip leaves each one's hips at a slightly different height
// (some float, some sink). On the first frames of Play this shifts the whole
// character vertically so the hips bone lands at a fixed seat height, making
// every character sit correctly regardless of their proportions.
public class SeatSnap : MonoBehaviour
{
    public float seatTopY = 0.5f;       // bench seat height ABOVE the floor the character stands on
    public float hipAboveSeat = 0.06f;  // how far the hips sit above that surface

    Transform _hips;
    float _floorY;                      // world Y of the floor the character was placed on
    int _frames;

    void Start()
    {
        var a = GetComponent<Animator>();
        if (a != null && a.isHuman) _hips = a.GetBoneTransform(HumanBodyBones.Hips);
        _floorY = transform.position.y; // placed on the (possibly lifted) pub floor
    }

    void LateUpdate()
    {
        // wait a couple of frames for the Animator to apply the sit pose, then
        // correct height for a few frames until it settles, then stop.
        if (_hips == null || _frames > 6) { enabled = _frames <= 6; return; }
        _frames++;
        if (_frames < 2) return;
        float target = _floorY + seatTopY + hipAboveSeat;
        float delta = target - _hips.position.y;
        if (Mathf.Abs(delta) > 0.002f)
            transform.position += Vector3.up * delta;
    }
}
