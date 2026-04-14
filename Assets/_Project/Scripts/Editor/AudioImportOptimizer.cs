using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bulk-applies mobile-friendly import settings to every <see cref="AudioClip"/>
/// under <c>Assets/_Project</c>. Run once after dropping new SFX into the
/// project and every time the audio folder grows; the menu is idempotent —
/// clips already matching the target settings are skipped without reimport.
///
/// <para>
/// <b>Policy.</b> The puzzle is 2D and runs on phones, so:
/// <list type="bullet">
///   <item><b>Force To Mono.</b> Halves memory; stereo is wasted at spatialBlend=0.</item>
///   <item><b>22050 Hz sample rate override.</b> Percussive UI SFX don't need 44.1k. Music keeps its native rate.</item>
///   <item><b>Vorbis compression</b> at quality 0.6 for SFX / 0.5 for music.</item>
///   <item><b>Load type</b> picked by length: &lt;2s decompressOnLoad (tiny, zero runtime cost), &lt;10s CompressedInMemory, longer = Streaming (music).</item>
/// </list>
/// </para>
///
/// <para>
/// Clips whose asset path contains <c>Music</c> are treated as music and keep
/// their native sample rate + stream. Everything else is treated as SFX.
/// </para>
/// </summary>
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

        // Apply the same settings to iOS + Android overrides so the platform
        // build doesn't silently fall back to a heavier default.
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
