using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// HW2 polish - the single biggest "make it look professional / realistic" lever
// in a stylized URP scene is lighting + post-processing, not more props. This
// sets up a warm sun with soft shadows, gentle atmospheric fog for depth, a
// richer ambient sky, and a Global Volume post-processing profile (tonemapping,
// bloom on the fire/lamps, color grading, a soft vignette). Called at the end of
// CombinedBuilder; also runnable on its own.
//   Menu: Tools > Pub Interior > Polish Look (lighting + post FX)
public static class ScenePolish
{
    const string ProfilePath = "Assets/Settings/VillagePostFX.asset";

    [MenuItem("Tools/Pub Interior/Polish Look (lighting + post FX)")]
    public static void Polish()
    {
        SetupLighting();
        SetupFog();
        SetupPostFX();
        MarkCameraPostFX();
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("Scene polished: warm sun + soft shadows, fog, ambient, and post-processing volume.");
    }

    // Just the Global Volume post-processing + camera flag (no sun/fog/ambient
    // changes) - for indoor scenes like the HW3 house interior.
    public static void ApplyPostFXOnly()
    {
        SetupPostFX();
        MarkCameraPostFX();
    }

    static void SetupLighting()
    {
        var sun = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        Light dir = null;
        foreach (var l in sun) if (l.type == LightType.Directional) { dir = l; break; }
        if (dir == null)
        {
            var go = new GameObject("Directional Light");
            dir = go.AddComponent<Light>();
            dir.type = LightType.Directional;
        }
        // warm late-afternoon key light, angled for long readable shadows
        dir.color = new Color(1f, 0.93f, 0.78f);
        dir.intensity = 1.35f;
        dir.transform.rotation = Quaternion.Euler(45f, 35f, 0f);
        dir.shadows = LightShadows.Soft;
        dir.shadowStrength = 0.65f;             // soft, not pitch-black shadows
        dir.shadowBias = 0.05f;
        dir.shadowNormalBias = 0.4f;

        // a warm sky / cool ground gradient reads more natural than flat ambient
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.66f, 0.72f);
        RenderSettings.ambientEquatorColor = new Color(0.52f, 0.52f, 0.46f);
        RenderSettings.ambientGroundColor = new Color(0.30f, 0.30f, 0.26f);
    }

    static void SetupFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.74f, 0.80f, 0.86f);   // hazy horizon
        RenderSettings.fogStartDistance = 45f;
        RenderSettings.fogEndDistance = 160f;                       // only distant edges fade
    }

    static void SetupPostFX()
    {
        var dir = System.IO.Path.GetDirectoryName(ProfilePath);
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets", "Settings");

        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        // tonemapping - the key to a filmic, non-washed-out image
        var tm = GetOrAdd<Tonemapping>(profile);
        tm.mode.overrideState = true; tm.mode.value = TonemappingMode.Neutral;

        // bloom - makes the fire, chandelier and lanterns glow warmly
        var bloom = GetOrAdd<Bloom>(profile);
        bloom.threshold.overrideState = true; bloom.threshold.value = 1.05f;
        bloom.intensity.overrideState = true; bloom.intensity.value = 0.55f;
        bloom.scatter.overrideState = true; bloom.scatter.value = 0.65f;
        bloom.tint.overrideState = true; bloom.tint.value = new Color(1f, 0.95f, 0.85f);

        // color grading - a touch more contrast + saturation + warmth
        var ca = GetOrAdd<ColorAdjustments>(profile);
        ca.postExposure.overrideState = true; ca.postExposure.value = 0.15f;
        ca.contrast.overrideState = true; ca.contrast.value = 12f;
        ca.saturation.overrideState = true; ca.saturation.value = 14f;
        ca.colorFilter.overrideState = true; ca.colorFilter.value = new Color(1f, 0.98f, 0.94f);

        // white balance a little warmer
        var wb = GetOrAdd<WhiteBalance>(profile);
        wb.temperature.overrideState = true; wb.temperature.value = 12f;

        // gentle vignette to focus the eye
        var vig = GetOrAdd<Vignette>(profile);
        vig.intensity.overrideState = true; vig.intensity.value = 0.26f;
        vig.smoothness.overrideState = true; vig.smoothness.value = 0.6f;

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        // attach the profile to a global Volume in the scene
        var volGo = GameObject.Find("Global Volume");
        if (volGo == null) volGo = new GameObject("Global Volume");
        var vol = volGo.GetComponent<Volume>();
        if (vol == null) vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 1f;
        vol.sharedProfile = profile;
    }

    static T GetOrAdd<T>(VolumeProfile p) where T : VolumeComponent
    {
        if (p.TryGet<T>(out var c)) return c;
        return p.Add<T>(true);
    }

    static void MarkCameraPostFX()
    {
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = true;
        }
    }
}
