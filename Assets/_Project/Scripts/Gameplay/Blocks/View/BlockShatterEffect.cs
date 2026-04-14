using UnityEngine;

/// <summary>
/// Spawns the GrindParticleEffect prefab at a contact point.
/// Tints all particle systems to match the block's color.
/// Returns the transform so the caller can reposition it.
/// </summary>
public static class BlockShatterEffect
{
    private static GameObject cachedPrefab;

    /// <summary>Assigned at bootstrap. When null, fallback constants are used.</summary>
    public static ShatterFeelConfig Config { get; set; }

    /// <summary>
    /// Instantiates the grind particle prefab at the given position,
    /// tints all particle systems to the block color, and starts playing.
    /// </summary>
    public static Transform SpawnContinuous(Vector3 worldPosition, Color color,
        Vector3 slideDir, Transform parent = null, int grinderWidth = 1)
    {
        var prefab = GetPrefab();
        if (prefab == null)
        {
            // Fallback: no prefab found, create minimal particles
            return SpawnFallback(worldPosition, color, parent);
        }

        var go = Object.Instantiate(prefab, worldPosition, Quaternion.identity);
        if (parent != null) go.transform.SetParent(parent, true);
        go.transform.position = worldPosition;

        // Orient the cone shape to spray outward from the grid (same direction as slide)
        go.transform.rotation = Quaternion.LookRotation(slideDir);

        // Scale particle shape and intensity based on grinder width
        float widthScale = Mathf.Max(1f, grinderWidth);

        // Tint all particle systems to the block's color
        var systems = go.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            // Scale shape X and emission rate by grinder width
            var shape = ps.shape;
            var shapeScale = shape.scale;
            shapeScale.x = widthScale;
            shape.scale = shapeScale;

            var emission = ps.emission;
            emission.rateOverTime = emission.rateOverTime.constant * widthScale;
            var main = ps.main;
            var currentColor = main.startColor;

            // For the main debris: use block color directly
            // For sparks/dust: tint toward block color but keep some of their original character
            if (ps.gameObject == go)
            {
                main.startColor = color;
            }
            else if (ps.gameObject.name == "Sparks")
            {
                // Sparks: blend block color with warm highlight
                Color highlight = Config != null ? Config.SparkHighlightColor : new Color(1f, 0.9f, 0.6f);
                float blend = Config != null ? Config.SparkBlendRatio : 0.3f;
                var sparkColor = Color.Lerp(color, highlight, blend);
                main.startColor = sparkColor;
            }
            // DustCloud keeps its subtle white/transparent look

            // Also tint materials
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var mat = renderer != null ? renderer.material : null;
            if (mat != null)
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);
            }

            ps.Play();
        }

        return go.transform;
    }

    private static GameObject GetPrefab()
    {
        if (cachedPrefab != null) return cachedPrefab;
        cachedPrefab = Resources.Load<GameObject>("GrindParticleEffect");
        if (cachedPrefab == null)
        {
            // Try loading from asset path via direct reference
            // In builds, this won't work — but for now it's fine for editor testing
            #if UNITY_EDITOR
            cachedPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Gameplay/GrindParticleEffect.prefab");
            #endif
        }
        return cachedPrefab;
    }

    /// <summary>Minimal fallback if prefab is missing.</summary>
    private static Transform SpawnFallback(Vector3 worldPosition, Color color, Transform parent)
    {
        var go = new GameObject("GrindParticles_Fallback");
        go.transform.position = worldPosition;
        if (parent != null) go.transform.SetParent(parent, true);

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 3f;
        main.startSize = 0.15f;
        main.startColor = color;
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 2f;
        main.loop = true;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 40f;
        shape.radius = 0.2f;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        var shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        renderer.material = new Material(shader);
        renderer.material.SetColor("_Color", color);

        ps.Play();
        return go.transform;
    }
}
