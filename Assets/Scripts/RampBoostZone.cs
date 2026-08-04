using UnityEngine;

public class RampBoostZone : MonoBehaviour
{
    [Header("Boost Settings")]
    [Tooltip("Impulse force applied to the player (X = horizontal forward, Y = vertical upward)")]
    public Vector2 boostImpulse = new Vector2(10f, 14f);

    [Tooltip("Time window in seconds after touching the lip where player can press jump to boost")]
    public float boostWindowDuration = 0.25f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryEnableBoost(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryEnableBoost(other);
    }

    private void TryEnableBoost(Collider2D other)
    {
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null)
        {
            player.EnableRampBoost(boostImpulse, boostWindowDuration);
        }
    }
}
