using System;
using Newtonsoft.Json;

/// <summary>
/// Thin facade around Newtonsoft.Json so the rest of the codebase never sees
/// the serializer directly. Centralizing the settings here makes it trivial
/// to tweak JSON conventions later (naming, null handling, converters) without
/// touching every call site.
/// </summary>
public static class LevelJsonSerializer
{
    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.Indented
    };

    /// <summary>
    /// Parses the given JSON text into a <see cref="LevelPayload"/>. Throws on
    /// empty input or a null result so callers can fail fast with a clear site.
    /// </summary>
    public static LevelPayload Deserialize(string json)
    {
        if (string.IsNullOrEmpty(json))
            throw new ArgumentException("Level JSON is null or empty.", nameof(json));

        var payload = JsonConvert.DeserializeObject<LevelPayload>(json, Settings);
        if (payload == null)
            throw new InvalidOperationException("Level JSON deserialized to null.");
        return payload;
    }

    /// <summary>
    /// Writes a payload back out to JSON. Used by editor tooling and tests.
    /// </summary>
    public static string Serialize(LevelPayload payload)
    {
        if (payload == null) throw new ArgumentNullException(nameof(payload));
        return JsonConvert.SerializeObject(payload, Settings);
    }
}
