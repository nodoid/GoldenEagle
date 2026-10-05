using System;
using System.Collections.Generic;
using GoldenEagle.Core.World;

namespace GoldenEagle.Core.Engine;

/// <summary>Walking around trapdoors: finds a corner to head for when the straight line crosses one.</summary>
public static class Pathing
{
    public const float MarginX = 0.12f;
    public const float MarginZ = 0.07f;

    /// <summary>Waypoint that skirts the first pit in the way, or null when the way is clear.</summary>
    public static (float x, float z)? Detour(float x0, float z0, float x1, float z1, IReadOnlyList<Pit> pits)
    {
        foreach (var pit in pits)
        {
            float ax = pit.X0 - MarginX, az = pit.Z0 - MarginZ, bx = pit.X1 + MarginX, bz = pit.Z1 + MarginZ;
            if (!SegmentHitsRect(x0, z0, x1, z1, ax, az, bx, bz))
                continue;
            // Corners a little further out, so the legs to and from them clear the box.
            float cx0 = ax - 0.02f, cz0 = az - 0.02f, cx1 = bx + 0.02f, cz1 = bz + 0.02f;
            var corners = new (float x, float z)[] { (cx0, cz0), (cx1, cz0), (cx0, cz1), (cx1, cz1) };
            (float x, float z)? best = null;
            float bestCost = float.MaxValue;
            foreach (var c in corners)
            {
                if (c.x < Adventure.XMin || c.x > Adventure.XMax || c.z < Adventure.ZMin || c.z > Adventure.ZMax)
                    continue;
                if (SegmentHitsRect(x0, z0, c.x, c.z, ax, az, bx, bz))
                    continue;
                float cost = Adventure.Distance(x0, z0, c.x, c.z) + Adventure.Distance(c.x, c.z, x1, z1);
                if (SegmentHitsRect(c.x, c.z, x1, z1, ax, az, bx, bz))
                    cost += 1f; // needs a second corner, reached from this one next time
                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = c;
                }
            }
            return best;
        }
        return null;
    }

    /// <summary>Liang-Barsky test: does the segment touch the rectangle?</summary>
    public static bool SegmentHitsRect(float x0, float z0, float x1, float z1, float ax, float az, float bx, float bz)
    {
        float t0 = 0, t1 = 1;
        float dx = x1 - x0, dz = z1 - z0;
        float[] p = [-dx, dx, -dz, dz];
        float[] q = [x0 - ax, bx - x0, z0 - az, bz - z0];
        for (int i = 0; i < 4; i++)
        {
            if (Math.Abs(p[i]) < 1e-9f)
            {
                if (q[i] < 0)
                    return false;
                continue;
            }
            float t = q[i] / p[i];
            if (p[i] < 0)
                t0 = Math.Max(t0, t);
            else
                t1 = Math.Min(t1, t);
            if (t0 > t1)
                return false;
        }
        return true;
    }
}
