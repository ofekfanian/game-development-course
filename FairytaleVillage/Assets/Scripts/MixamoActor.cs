using UnityEngine;

// Drives a Mixamo character's Animator. A small state machine sets the Animator
// parameters (Speed / Sit / StairUp / StairDown); the Animator Controller then
// switches clips with transitions. Movement is scripted (root motion is off).
// Works in the parent's LOCAL space, so the whole pub can be moved/rotated into
// the village and the characters still behave correctly.
public class MixamoActor : MonoBehaviour
{
    public enum Mode { StandIdle, Sit, Patrol, Stairs }
    public Mode mode = Mode.StandIdle;

    public Vector3[] patrolPoints;   // local to the pub
    public Vector3 stairBottom, stairTop; // local to the pub
    public float moveSpeed = 1.4f;

    [Header("Footstep SFX (set by the builder)")]
    public AudioSource footAudio;
    public AudioClip[] stepClips;      // floor footstep taps, played while patrolling
    public AudioClip stairCreakClip;   // wood-creak, played while on the stairs
    public float stepStride = 0.85f;   // metres of travel between footstep sounds

    Animator _anim;
    int _wp;
    bool _paused;
    float _pauseTimer;
    bool _goingUp = true;
    bool _stairPaused;
    float _stairPauseTimer;
    float _stepAccum;

    void Start()
    {
        _anim = GetComponent<Animator>();
        if (_anim != null) _anim.applyRootMotion = false;
        if (mode == Mode.Stairs) transform.localPosition = stairBottom;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        switch (mode)
        {
            case Mode.StandIdle: Set(0, false, false, false); break;
            case Mode.Sit: Set(0, true, false, false); break;
            case Mode.Patrol: Patrol(dt); break;
            case Mode.Stairs: Stairs(dt); break;
        }
    }

    void Patrol(float dt)
    {
        if (patrolPoints == null || patrolPoints.Length == 0) { Set(0, false, false, false); return; }
        if (_paused)
        {
            Set(0, false, false, false);
            _pauseTimer -= dt;
            if (_pauseTimer <= 0f) { _paused = false; _wp = (_wp + 1) % patrolPoints.Length; }
            return;
        }
        Vector3 to = patrolPoints[_wp] - transform.localPosition; to.y = 0;
        if (to.magnitude < 0.2f) { _paused = true; _pauseTimer = 2.2f; return; }
        Vector3 dir = to.normalized;
        float step = moveSpeed * dt;
        transform.localPosition += dir * step;
        Face(dir, dt);
        Set(moveSpeed, false, false, false);
        AccumulateSteps(step, stepClips, stepStride, pitchJitter: 0.10f, volume: 0.5f);
    }

    void Stairs(float dt)
    {
        // pause and idle at the top/bottom landing before turning around, instead
        // of instantly reversing direction (which read as a glitchy bounce)
        if (_stairPaused)
        {
            Set(0, false, false, false);
            _stairPauseTimer -= dt;
            if (_stairPauseTimer <= 0f) { _stairPaused = false; _goingUp = !_goingUp; }
            return;
        }
        Vector3 target = _goingUp ? stairTop : stairBottom;
        Vector3 to = target - transform.localPosition;
        if (to.magnitude < 0.25f) { _stairPaused = true; _stairPauseTimer = 2.5f; return; }
        Vector3 dir = to.normalized;
        float step = (moveSpeed * 0.7f) * dt;
        transform.localPosition += dir * step;
        Vector3 face = new Vector3(dir.x, 0, dir.z);
        if (face.sqrMagnitude > 0.001f) Face(face.normalized, dt);
        Set(moveSpeed, false, _goingUp, !_goingUp);
        if (stairCreakClip != null)
            AccumulateSteps(step, new[] { stairCreakClip }, stepStride * 0.65f, pitchJitter: 0.14f, volume: 0.55f);
    }

    // Plays a footstep/creak sound roughly once per `stride` metres of travel -
    // ties the sound cadence to actual movement instead of a fixed timer, so it
    // doesn't drift out of sync if moveSpeed changes.
    void AccumulateSteps(float distanceMoved, AudioClip[] clips, float stride, float pitchJitter, float volume)
    {
        if (footAudio == null || clips == null || clips.Length == 0 || stride <= 0f) return;
        _stepAccum += distanceMoved;
        if (_stepAccum < stride) return;
        _stepAccum = 0f;
        footAudio.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
        footAudio.PlayOneShot(clips[Random.Range(0, clips.Length)], volume);
    }

    void Face(Vector3 localDir, float dt)
    {
        if (localDir.sqrMagnitude < 0.0001f) return;
        var want = Quaternion.LookRotation(localDir, Vector3.up);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, want, dt * 10f);
    }

    void Set(float speed, bool sit, bool up, bool down)
    {
        if (_anim == null) return;
        _anim.SetFloat("Speed", speed);
        _anim.SetBool("Sit", sit);
        _anim.SetBool("StairUp", up);
        _anim.SetBool("StairDown", down);
    }
}
