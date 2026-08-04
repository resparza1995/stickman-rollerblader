using UnityEngine;

namespace ObstaclesSystem
{
    public class GrindRail : MonoBehaviour, IRailObstacle
    {
        [Header("Rail Geometry")]
        public Transform startPoint;
        public Transform endPoint;

        [Header("Physics Settings")]
        public float railFriction = 0.05f;

        public Vector3 GetClosestPointOnRail(Vector3 position)
        {
            if (startPoint == null || endPoint == null)
                return transform.position;

            Vector3 line = endPoint.position - startPoint.position;
            float len = line.magnitude;
            line.Normalize();

            Vector3 v = position - startPoint.position;
            float d = Vector3.Dot(v, line);
            d = Mathf.Clamp(d, 0f, len);

            return startPoint.position + line * d;
        }

        public Vector3 GetRailDirection(Vector3 position)
        {
            if (startPoint == null || endPoint == null)
                return transform.right;

            return (endPoint.position - startPoint.position).normalized;
        }

        public float GetFriction()
        {
            return railFriction;
        }

        private void OnDrawGizmos()
        {
            if (startPoint != null && endPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(startPoint.position, endPoint.position);
                Gizmos.DrawSphere(startPoint.position, 0.1f);
                Gizmos.DrawSphere(endPoint.position, 0.1f);
            }
        }
    }
}
