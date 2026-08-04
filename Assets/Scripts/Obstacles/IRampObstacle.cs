using UnityEngine;

namespace ObstaclesSystem
{
    public interface IRampObstacle
    {
        Vector2 GetLaunchImpulse(Vector2 entryVelocity, bool isFacingRight);
        float GetBoostWindowDuration();
    }
}
