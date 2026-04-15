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

        // Pull the spawn back toward the grid along -slideDir so particles appear
        // at the grinder contact edge instead of floating past it. Without this,
        // the cone's backward-straying particles render inside the grid because
        // the caller passes the grinder's world center (which sits outside the
        // grid by grinderDepthOffset).
        float inset = Config != null ? Config.SpawnInsetFromGrinder : 0f;
        Vector3 spawnPos = slideDir.sqrMagnitude > 0.0001f
            ? worldPosition - slideDir.normalized * inset
            : worldPosition;

        var go = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
        if (parent != null) go.transform.SetParent(parent, true);
        go.transform.position = spawnPos;

        // Orient the cone shape to spray outward from the grid (same direction as slide).
        // LookRotation requires a non-zero vector — fall back to identity otherwise
        // so particles never fire toward a random axis (the "sometimes spawns
        // backwards" case when slideDir is degenerate).
        go.transform.rotation = slideDir.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(slideDir.normalized)
            : Quaternion.identity;

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

            // Tighten the cone so fewer particles stray sideways/backwards.
            // Only applies when Config authored a non-zero override.
            float coneAngle = Config != null ? Config.ConeAngleOverride : 0f;
            if (coneAngle > 0f && shape.shapeType == ParticleSystemShapeType.Cone)
                shape.angle = coneAngle;

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

            // Also tint materials. If the renderer shipped with no material
            // (prefab authored with m_Materials: [fileID: 0]) the editor falls
            // back to a default particle material, but a stripped build has
            // nothing — particles render invisible. Create one on the fly.
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && renderer.sharedMaterial == null)
            {
                var fallbackShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (fallbackShader == null) fallbackShader = Shader.Find("Particles/Standard Unlit");
                if (fallbackShader == null) fallbackShader = Shader.Find("Sprites/Default");
                if (fallbackShader != null)
                    renderer.material = new Material(fallbackShader);
            }
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
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        // Guard: if every candidate was stripped we still must not throw; the
        // caller depends on this method returning so the slide tween starts.
        if (shader != null)
        {
            renderer.material = new Material(shader);
            if (renderer.material.HasProperty("_Color"))     renderer.material.SetColor("_Color", color);
            if (renderer.material.HasProperty("_BaseColor")) renderer.material.SetColor("_BaseColor", color);
        }

        ps.Play();
        return go.transform;
    }
}
