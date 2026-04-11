using System;
using UnityEngine;

/// <summary>
/// Reads a <see cref="LevelConfig"/>, pulls its JSON payload, and hands back
/// a fully deserialized <see cref="LevelPayload"/>. Sits between disk (or a
/// <see cref="TextAsset"/>) and the simulation layer so the rest of the
/// gameplay code never has to touch JSON or file paths.
///
/// Applies the designer-friendly <see cref="LevelConfig.TimeLimitOverride"/>
/// on the way out so inspector tweaks can shadow the authored JSON value
/// without round-tripping through the file.
/// </summary>
public sealed class LevelLoader
{
    /// <summary>
    /// Deserializes the JSON payload referenced by <paramref name="config"/>.
    /// Returns null (and logs an error) on missing asset or parse failure so
    /// upstream orchestration can abort cleanly without try/catch ceremony.
    /// </summary>
    public LevelPayload Load(LevelConfig config)
    {
        if (config == null)
        {
            Debug.LogError("[LevelLoader] Cannot load a null LevelConfig.");
            return null;
        }

        if (config.LevelJson == null)
        {
            Debug.LogError($"[LevelLoader] LevelConfig '{config.name}' has no JSON TextAsset assigned.");
            return null;
        }

        LevelPayload payload;
        try
        {
            payload = LevelJsonSerializer.Deserialize(config.LevelJson.text);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LevelLoader] Failed to deserialize '{config.name}': {e.Message}");
            return null;
        }

        // Designer override: a non-positive override keeps whatever the JSON says.
        if (config.TimeLimitOverride > 0f)
            payload.TimeLimit = config.TimeLimitOverride;

        return payload;
    }
}
