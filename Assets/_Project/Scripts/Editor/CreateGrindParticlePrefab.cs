using UnityEngine;
using UnityEditor;

/// <summary>
/// Creates a polished grind particle effect prefab with 3 layers:
///   1. Chunky cube debris (main system)
///   2. Bright sparks (child system)
///   3. Dust cloud puffs (child system)
///
/// Run via BlockFlow → Create Grind Particle Prefab.
/// </summary>
public static class CreateGrindParticlePrefab
{
    [MenuItem("BlockFlow/Create Grind Particle Prefab")]
    static void Create()
    {
        var go = new GameObject("GrindParticleEffect");

        // ═══════════════════════════════════════
        //  LAYER 1: Chunky cube debris
        // ═══════════════════════════════════════
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 2f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        main.startColor = Color.white;
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 2.5f;
        main.playOnAwake = false;
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.startRotationY = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);

        var emission = ps.emission;
        emission.rateOverTime = 60f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.2f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(0.6f, 0.7f), new Keyframe(1f, 0f)));

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.x = new ParticleSystem.MinMaxCurve(-300f, 300f);
        rot.y = new ParticleSystem.MinMaxCurve(-300f, 300f);
        rot.z = new ParticleSystem.MinMaxCurve(-300f, 300f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
        col.color = gradient;

        // Cube mesh renderer
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        var tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        renderer.mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(tempCube);

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var debrisMat = new Material(shader);
        debrisMat.name = "M_GrindDebris";
        debrisMat.SetColor("_BaseColor", Color.white);
        renderer.material = debrisMat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        // ═══════════════════════════════════════
        //  LAYER 2: Bright sparks
        // ═══════════════════════════════════════
        var sparkGo = new GameObject("Sparks");
        sparkGo.transform.SetParent(go.transform, false);
        var sparkPs = sparkGo.AddComponent<ParticleSystem>();

        var sparkMain = sparkPs.main;
        sparkMain.duration = 2f;
        sparkMain.loop = true;
        sparkMain.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
        sparkMain.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
        sparkMain.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        sparkMain.startColor = Color.white;
        sparkMain.maxParticles = 80;
        sparkMain.simulationSpace = ParticleSystemSimulationSpace.World;
        sparkMain.gravityModifier = 0.3f;
        sparkMain.playOnAwake = false;

        var sparkEmission = sparkPs.emission;
        sparkEmission.rateOverTime = 50f;

        var sparkShape = sparkPs.shape;
        sparkShape.shapeType = ParticleSystemShapeType.Cone;
        sparkShape.angle = 55f;
        sparkShape.radius = 0.1f;

        var sparkSol = sparkPs.sizeOverLifetime;
        sparkSol.enabled = true;
        sparkSol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

        var sparkCol = sparkPs.colorOverLifetime;
        sparkCol.enabled = true;
        var sparkGrad = new Gradient();
        sparkGrad.SetKeys(
            new[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(1f, 0.85f, 0.4f), 0.5f),
                new GradientColorKey(new Color(1f, 0.5f, 0.1f), 1f) },
            new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.7f, 0.5f),
                new GradientAlphaKey(0f, 1f) });
        sparkCol.color = sparkGrad;

        var sparkRenderer = sparkGo.GetComponent<ParticleSystemRenderer>();
        var sparkShader = Shader.Find("Particles/Standard Unlit");
        if (sparkShader == null) sparkShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var sparkMat = new Material(sparkShader);
        sparkMat.name = "M_GrindSparks";
        sparkMat.SetColor("_Color", Color.white);
        sparkRenderer.material = sparkMat;

        // ═══════════════════════════════════════
        //  LAYER 3: Dust cloud puffs
        // ═══════════════════════════════════════
        var dustGo = new GameObject("DustCloud");
        dustGo.transform.SetParent(go.transform, false);
        var dustPs = dustGo.AddComponent<ParticleSystem>();

        var dustMain = dustPs.main;
        dustMain.duration = 2f;
        dustMain.loop = true;
        dustMain.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        dustMain.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        dustMain.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        dustMain.startColor = new Color(1f, 1f, 1f, 0.35f);
        dustMain.maxParticles = 25;
        dustMain.simulationSpace = ParticleSystemSimulationSpace.World;
        dustMain.gravityModifier = -0.3f; // float upward
        dustMain.playOnAwake = false;

        var dustEmission = dustPs.emission;
        dustEmission.rateOverTime = 15f;

        var dustShape = dustPs.shape;
        dustShape.shapeType = ParticleSystemShapeType.Sphere;
        dustShape.radius = 0.3f;

        var dustSol = dustPs.sizeOverLifetime;
        dustSol.enabled = true;
        dustSol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.4f), new Keyframe(0.4f, 1f), new Keyframe(1f, 0.2f)));

        var dustCol = dustPs.colorOverLifetime;
        dustCol.enabled = true;
        var dustGrad = new Gradient();
        dustGrad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.3f, 0f), new GradientAlphaKey(0.15f, 0.5f), new GradientAlphaKey(0f, 1f) });
        dustCol.color = dustGrad;

        var dustRenderer = dustGo.GetComponent<ParticleSystemRenderer>();
        var dustMat = new Material(sparkShader);
        dustMat.name = "M_GrindDust";
        dustMat.SetColor("_Color", Color.white);
        dustRenderer.material = dustMat;

        // ═══════════════════════════════════════
        //  Save prefab
        // ═══════════════════════════════════════
        string dir = "Assets/_Project/Prefabs/Gameplay";
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
            AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Gameplay");

        string path = dir + "/GrindParticleEffect.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        AssetDatabase.SaveAssets();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Debug.Log("[BlockFlow] Grind particle prefab saved at: " + path);
    }
}
