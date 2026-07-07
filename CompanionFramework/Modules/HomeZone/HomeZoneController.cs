using UnityEngine;

namespace Modules.HomeZone
{
    public class HomeZoneController
    {
        public Vector3 HomePosition;
        public Vector3 Center;
        public float Radius;

        public bool IsSet;
        public void SetHomeZone(Vector3 center, float radius)
        {
            Center = center;
            Radius = radius;
            IsSet = true;
        }

        public bool IsInside(Vector3 position)
        {
            float distance = Vector3.Distance(position, HomePosition);
            return distance <= Radius;
        }
    }
}