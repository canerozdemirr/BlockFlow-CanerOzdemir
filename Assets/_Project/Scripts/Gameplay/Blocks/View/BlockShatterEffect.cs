using UnityEngine;

public static class BlockShatterEffect
{
    private static GameObject cachedPrefab;

    // Assigned at bootstrap. When null, fallback constants are used.
    public static ShatterFeelConfig Config { get; set; }

    public static Transform SpawnContinuous(Vector3 worldPosition, Color color,
        Vector3 slideDir, Transform parent = null, int grinderWidth = 1)
    {
        var prefab = GetPrefab();
        if (prefab == null)
            return SpawnFallback(worldPosition, color, parent);

        // Pull spawn back along -slideDir so particles appear at the grinder contact
        // edge; the caller passes the grinder's world center which sits past the grid.
        float inset = Config != null ? Config.SpawnInsetFromGrinder : 0f;
        Vector3 spawnPos = slideDir.sqrMagnitude > 0.0001f
            ? worldPosition - slideDir.normalized * inset
            : worldPosition;

        var go = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
        if (parent != null) go.transform.SetParent(parent, true);
        go.transform.position = spawnPos;

        // LookRotation requires non-zero; fall back to identity so degenerate slideDir
        // doesn't fire particles toward a random axis.
        go.transform.rotation = slideDir.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(slideDir.normalized)
            : Quaternion.identity;

        float widthScale = Mathf.Max(1f, grinderWidth);

        var systems = go.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var shape = ps.shape;
            var shapeScale = shape.scale;
            shapeScale.x = widthScale;
            shape.scale = shapeScale;

            float coneAngle = Config != null ? Config.ConeAngleOverride : 0f;
            if (coneAngle > 0f && shape.shapeType == ParticleSystemShapeType.Cone)
                shape.angle = coneAngle;

            var emission = ps.emission;
            emission.rateOverTime = emission.rateOverTime.constant * widthScale;
            var main = ps.main;
            var currentColor = main.startColor;

            if (ps.gameObject == go)
            {
                main.startColor = color;
            }
            else if (ps.gameObject.name == "Sparks")
            {
                Color highlight = Config != null ? Config.SparkHighlightColor : new Color(1f, 0.9f, 0.6f);
                float blend = Config != null ? Config.SparkBlendRatio : 0.3f;
                var sparkColor = Color.Lerp(color, highlight, blend);
                main.startColor = sparkColor;
            }
            // DustCloud keeps its authored white/transparent look.

            // Prefab may ship with m_Materials: [fileID: 0]; editor falls back to a
            // default particle material, but stripped builds have none — create one on the fly.
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
            // Editor-only fallback for prefabs not yet moved under Resources/.
            #if UNITY_EDITOR
            cachedPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Gameplay/GrindParticleEffect.prefab");
            #endif
        }
        return cachedPrefab;
    }

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
        // Must not throw even if every shader candidate was stripped — caller depends on return.
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
