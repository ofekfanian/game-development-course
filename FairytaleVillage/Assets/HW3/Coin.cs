using UnityEngine;

// HW3 - a spinning gold coin. Walk into it (its trigger) and it adds to
// GameState.Coins, which is what carries between the village and the house.
[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    public int value = 1;
    public float spinDegPerSec = 140f;
    public float bobHeight = 0.12f;
    public float bobSpeed = 2f;
    public AudioClip pickupSound;

    float _baseY;

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    void Start()
    {
        // local space, so the bob still works if the coin's group is moved
        // (the interior scene is repositioned inside House #1 when it loads)
        _baseY = transform.localPosition.y;
    }

    void Update()
    {
        transform.Rotate(0f, spinDegPerSec * Time.deltaTime, 0f, Space.World);
        var p = transform.localPosition;
        p.y = _baseY + Mathf.Sin(Time.time * bobSpeed + _baseY) * bobHeight;
        transform.localPosition = p;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<ThirdPersonController>() == null) return;
        GameState.Coins += value;
        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, Camera.main != null ? Camera.main.transform.position : transform.position, 0.8f);
        HudUI.Show("Coin!  Total: " + GameState.Coins, 1f);
        Destroy(gameObject);
    }
}
