using UnityEngine;

namespace ObstaclesSystem
{
    public interface IRailObstacle
    {
        Vector3 GetClosestPointOnRail(Vector3 position);
        Vector3 GetRailDirection(Vector3 position);
        float GetFriction();
    }
}
