using System;
using System.Collections.Generic;
using UnityEngine;

namespace AvoiderPlugin
{
    /// <summary>Original implementation of Bridson's 2D Poisson-disc algorithm.</summary>
    public sealed class PoissonDiscSampler
    {
        private readonly float width, height, radius;
        private readonly int seed;
        public PoissonDiscSampler(float width, float height, float radius, int seed = 12345)
        {
            if (!FinitePositive(width) || !FinitePositive(height) || !FinitePositive(radius))
                throw new ArgumentOutOfRangeException(nameof(radius), "Dimensions and spacing must be finite and positive.");
            this.width = width; this.height = height; this.radius = radius; this.seed = seed;
        }
        private static bool FinitePositive(float n) => n > 0 && !float.IsInfinity(n) && !float.IsNaN(n);

        public IEnumerable<Vector2> Samples(int maxSamples = 600)
        {
            if (maxSamples < 1) yield break;
            var random = new System.Random(seed);
            float cell = radius / Mathf.Sqrt(2);
            var grid = new Dictionary<Vector2Int, Vector2>();
            var active = new List<Vector2>();
            Vector2 first = new Vector2((float)random.NextDouble() * width, (float)random.NextDouble() * height);
            active.Add(first); grid[Cell(first, cell)] = first;
            yield return first;
            int count = 1;
            while (active.Count > 0 && count < maxSamples)
            {
                int index = random.Next(active.Count);
                bool found = false;
                for (int attempt = 0; attempt < 30; attempt++)
                {
                    float angle = (float)random.NextDouble() * Mathf.PI * 2;
                    // sqrt produces uniform area density in the annulus [r, 2r].
                    float distance = radius * Mathf.Sqrt(1 + 3 * (float)random.NextDouble());
                    Vector2 p = active[index] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                    if (p.x < 0 || p.y < 0 || p.x >= width || p.y >= height) continue;
                    Vector2Int key = Cell(p, cell);
                    bool valid = true;
                    for (int x = -2; x <= 2 && valid; x++)
                        for (int y = -2; y <= 2; y++)
                            if (grid.TryGetValue(key + new Vector2Int(x, y), out Vector2 neighbour) &&
                                (neighbour - p).sqrMagnitude < radius * radius) { valid = false; break; }
                    if (!valid) continue;
                    active.Add(p); grid[key] = p; count++; found = true;
                    yield return p;
                    break;
                }
                if (!found) { active[index] = active[active.Count - 1]; active.RemoveAt(active.Count - 1); }
            }
        }
        private static Vector2Int Cell(Vector2 p, float size) => new Vector2Int(Mathf.FloorToInt(p.x / size), Mathf.FloorToInt(p.y / size));
    }
}
