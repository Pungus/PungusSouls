using Core.Agent;
using UnityEngine;

namespace Modules.HomeZone
{
    public static class HomeZoneVisuals
    {
        public static void Draw(AgentContext ctx)
        {
            if (ctx.HomeZone == null || !ctx.HomeZone.IsSet)
                return;

            DrawCircle(ctx.HomeZone.HomePosition, ctx.HomeZone.Radius);
        }

        private static void DrawCircle(Vector3 center, float radius, int segments = 32)
        {
            float angleStep = 360f / segments;

            Vector3 prev = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

                Debug.DrawLine(prev, next, Color.cyan);

                prev = next;
            }
        }
    }
}