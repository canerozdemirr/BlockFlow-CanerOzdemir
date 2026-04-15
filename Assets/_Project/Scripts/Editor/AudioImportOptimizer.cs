using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Paths containing "Music" are treated as music (streamed, native sample rate).
// Everything else is SFX: forced mono, 22050Hz, Vorbis q=0.6, load type picked by length.
public static class AudioImportOptimizer
{
    private const string MenuItem = "BlockFlow/Audio/Optimize Import Settings";
    private const string SearchRoot = "Assets/_Project";

    [MenuItem(MenuItem)]
    private static void OptimizeAll()
    {
        var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { SearchRoot });
        if (guids.Length == 0)
        {
            Debug.Log($"[AudioImportOptimizer] No AudioClips found under {SearchRoot}.");
            return;
        }

        var touched = new List<string>();
        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (Apply(path)) touched.Add(path);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log($"[AudioImportOptimizer] Reimported {touched.Count}/{guids.Length} clip(s) with mobile-friendly settings.");
    }

    private static bool Apply(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null) return false;

        bool isMusic = path.Contains("/Music/") || path.Contains("\\Music\\")
                    || path.ToLowerInvariant().Contains("music");

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        float length = clip != null ? clip.length : 0f;

        var target = new AudioImporterSampleSettings
        {
            loadType            = PickLoadType(length, isMusic),
            compressionFormat   = AudioCompressionFormat.Vorbis,
            quality             = isMusic ? 0.5f : 0.6f,
            sampleRateSetting   = isMusic ? AudioSampleRateSetting.PreserveSampleRate
                                          : AudioSampleRateSetting.OverrideSampleRate,
            sampleRateOverride  = 22050,
            preloadAudioData    = !isMusic,
        };

        bool changed = false;

        if (!importer.forceToMono && !isMusic) { importer.forceToMono = true; changed = true; }
        if (importer.loadInBackground) { importer.loadInBackground = false; changed = true; }

        if (!SampleSettingsEqual(importer.defaultSampleSettings, target))
        {
            importer.defaultSampleSettings = target;
            changed = true;
        }

        // Apply to iOS + Android overrides so platform builds don't fall back to heavier defaults.
        changed |= ApplyOverride(importer, "iOS", target);
        changed |= ApplyOverride(importer, "Android", target);

        if (changed)
        {
            AssetDatabase.WriteImportSettingsIfDirty(path);
            importer.SaveAndReimport();
        }
        return changed;
    }

    private static AudioClipLoadType PickLoadType(float seconds, bool isMusic)
    {
        if (isMusic) return AudioClipLoadType.Streaming;
        if (seconds < 2f) return AudioClipLoadType.DecompressOnLoad;
        if (seconds < 10f) return AudioClipLoadType.CompressedInMemory;
        return AudioClipLoadType.Streaming;
    }

    private static bool ApplyOverride(AudioImporter importer, string platform, AudioImporterSampleSettings target)
    {
        var existing = importer.GetOverrideSampleSettings(platform);
        if (SampleSettingsEqual(existing, target) && importer.ContainsSampleSettingsOverride(platform)) return false;
        importer.SetOverrideSampleSettings(platform, target);
        return true;
    }

    private static bool SampleSettingsEqual(AudioImporterSampleSettings a, AudioImporterSampleSettings b)
    {
        return a.loadType == b.loadType
            && a.compressionFormat == b.compressionFormat
            && Mathf.Approximately(a.quality, b.quality)
            && a.sampleRateSetting == b.sampleRateSetting
            && a.sampleRateOverride == b.sampleRateOverride
            && a.preloadAudioData == b.preloadAudioData;
    }
}
