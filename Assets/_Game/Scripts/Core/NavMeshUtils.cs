using UnityEngine;
using UnityEngine.AI;

namespace TacticsGame.Core
{
    public static class NavMeshUtils
    {
        public static float CalculatePathLength(NavMeshPath path)
        {
            float length = 0f;
            for (int i = 1; i < path.corners.Length; i++)
            {
                length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
            return length;
        }

        public static Vector3 GetPointAlongPath(NavMeshPath path, float distance)
        {
            float accumulated = 0f;
            for (int i = 1; i < path.corners.Length; i++)
            {
                float segLength = Vector3.Distance(path.corners[i - 1], path.corners[i]);
                if (accumulated + segLength >= distance)
                {
                    float t = (distance - accumulated) / segLength;
                    return Vector3.Lerp(path.corners[i - 1], path.corners[i], t);
                }
                accumulated += segLength;
            }
            return path.corners.Length > 0 ? path.corners[path.corners.Length - 1] : Vector3.zero;
        }
    }
}
