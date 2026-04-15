using System;
using UnityEngine;

public sealed class LevelLoader
{
    // Returns null on missing asset or parse failure; upstream aborts cleanly.
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

        // Non-positive override means "keep whatever JSON says".
        if (config.TimeLimitOverride > 0f)
            payload.TimeLimit = config.TimeLimitOverride;

        return payload;
    }
}
