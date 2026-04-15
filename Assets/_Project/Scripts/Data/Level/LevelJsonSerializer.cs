using System;
using Newtonsoft.Json;

public static class LevelJsonSerializer
{
    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.Indented
    };

    public static LevelPayload Deserialize(string json)
    {
        if (string.IsNullOrEmpty(json))
            throw new ArgumentException("Level JSON is null or empty.", nameof(json));

        var payload = JsonConvert.DeserializeObject<LevelPayload>(json, Settings);
        if (payload == null)
            throw new InvalidOperationException("Level JSON deserialized to null.");
        return payload;
    }

    public static string Serialize(LevelPayload payload)
    {
        if (payload == null) throw new ArgumentNullException(nameof(payload));
        return JsonConvert.SerializeObject(payload, Settings);
    }
}
