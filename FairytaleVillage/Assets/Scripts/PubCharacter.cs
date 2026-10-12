using UnityEngine;

// Procedural character for the HW2 pub. A small state machine switches between
// animations (Stand-idle, Sit, Walk, Stairs) and each state is produced by posing
// the character's joints in code. This demonstrates animation switching, e.g. when
// a character stops it goes to idle, when it reaches a chair it sits, and on the
// staircase it switches between walking up and down.
public class PubCharacter : MonoBehaviour
{
    public enum Mode { StandIdle, Sit, Patrol, Stairs }
    public Mode mode = Mode.StandIdle;

    // joints (assigned by PubCharacters builder)
    public Transform hips, torso, head, shoulderL, shoulderR, hipL, hipR, kneeL, kneeR;

    // behaviour data
    public Vector3[] patrolPoints;   // for Patrol
    public Vector3 stairBottom, stairTop; // for Stairs
    public float moveSpeed = 1.6f;
    public float walkFreq = 8f;

    public string State { get; private set; } = "";

    float _baseHipsY;
    int _wp;
    bool _paused;
    float _pauseTimer;
    bool _goingUp = true;
    float _phase;

    void Start()
    {
        if (hips != null) _baseHipsY = hips.localPosition.y;
        if (mode == Mode.Stairs) transform.position = stairBottom;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        switch (mode)
        {
            case Mode.StandIdle: SetState("Idle"); ResetPose(); IdlePose(); break;
            case Mode.Sit: SetState("Sit"); SitPose(); break;
            case Mode.Patrol: Patrol(dt); break;
            case Mode.Stairs: Stairs(dt); break;
        }
    }

    void SetState(string s) { State = s; }

    // ---- movement behaviours ---------------------------------------------

    void Patrol(float dt)
    {
        if (patrolPoints == null || patrolPoints.Length == 0) { SetState("Idle"); IdlePose(); return; }
        if (_paused)
        {
            SetState("Idle"); ResetPose(); IdlePose();
            _pauseTimer -= dt;
            if (_pauseTimer <= 0f) { _paused = false; _wp = (_wp + 1) % patrolPoints.Length; }
            return;
        }

        SetState("Walk");
        Vector3 target = patrolPoints[_wp];
        Vector3 to = target - transform.position; to.y = 0;
        if (to.magnitude < 0.15f) { _paused = true; _pauseTimer = 1.6f; return; }

        Vector3 dir = to.normalized;
        transform.position += dir * moveSpeed * dt;
        FaceDir(dir, dt);
        _phase += dt * walkFreq;
        WalkPose(_phase, 1f);
    }

    void Stairs(float dt)
    {
        SetState(_goingUp ? "StairsUp" : "StairsDown");
        Vector3 target = _goingUp ? stairTop : stairBottom;
        Vector3 to = target - transform.position;
        if (to.magnitude < 0.2f) { _goingUp = !_goingUp; return; }

        Vector3 dir = to.normalized;
        transform.position += dir * (moveSpeed * 0.7f) * dt;
        Vector3 face = new Vector3(dir.x, 0, dir.z);
        if (face.sqrMagnitude > 0.001f) FaceDir(face.normalized, dt);
        _phase += dt * walkFreq * 0.8f;
        WalkPose(_phase, 1.6f); // exaggerated knee lift for stairs
    }

    void FaceDir(Vector3 dir, float dt)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion want = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, want, dt * 10f);
    }

    // ---- poses ------------------------------------------------------------

    void ResetPose()
    {
        SetJoint(hipL, 0); SetJoint(hipR, 0); SetJoint(kneeL, 0); SetJoint(kneeR, 0);
        SetJoint(shoulderL, 0); SetJoint(shoulderR, 0);
        if (hips != null) hips.localPosition = new Vector3(hips.localPosition.x, _baseHipsY, hips.localPosition.z);
    }

    void IdlePose()
    {
        float t = Time.time;
        if (hips != null) hips.localPosition = new Vector3(hips.localPosition.x, _baseHipsY + Mathf.Sin(t * 2f) * 0.02f, hips.localPosition.z);
        float sway = Mathf.Sin(t * 1.5f) * 4f;
        SetJoint(shoulderL, sway); SetJoint(shoulderR, -sway);
    }

    void SitPose()
    {
        // lower onto the seat, thighs forward, shins down
        if (hips != null) hips.localPosition = new Vector3(hips.localPosition.x, _baseHipsY - 0.5f, hips.localPosition.z);
        SetJoint(hipL, 85); SetJoint(hipR, 85);
        SetJoint(kneeL, -80); SetJoint(kneeR, -80);
        float t = Time.time;
        SetJoint(shoulderL, 10 + Mathf.Sin(t * 1.7f) * 3f);
        SetJoint(shoulderR, 10 - Mathf.Sin(t * 1.7f) * 3f);
    }

    void WalkPose(float phase, float lift)
    {
        float s = Mathf.Sin(phase);
        SetJoint(hipL, s * 32f * lift); SetJoint(hipR, -s * 32f * lift);
        SetJoint(shoulderL, -s * 26f); SetJoint(shoulderR, s * 26f);
        // knee bends when its leg swings back
        SetJoint(kneeL, -Mathf.Max(0, -s) * 45f * lift);
        SetJoint(kneeR, -Mathf.Max(0, s) * 45f * lift);
        if (hips != null) hips.localPosition = new Vector3(hips.localPosition.x, _baseHipsY + Mathf.Abs(Mathf.Sin(phase)) * 0.04f, hips.localPosition.z);
    }

    void SetJoint(Transform j, float xDeg) { if (j != null) j.localRotation = Quaternion.Euler(xDeg, 0, 0); }
}
