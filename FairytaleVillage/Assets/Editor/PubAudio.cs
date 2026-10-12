using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// HW2 - adds sound. Per instructor feedback the looping background music didn't
// fit the scene, so it's been dropped in favour of situational sound effects:
// a positional fire crackle at the hearth, plus footstep taps / a stair creak
// that MixamoActor triggers on the walking barmaid and the stair-climber (see
// Assets/Scripts/MixamoActor.cs). Run after building the pub + Mixamo cast.
// The .wav files live in Assets/Audio.
public static class PubAudio
{
    [MenuItem("Tools/Pub Interior/Add Audio")]
    public static void AddAudio()
    {
        var pub = GameObject.Find("Pub");
        if (pub == null) { Debug.LogError("Build the pub first (Tools > Pub Interior > Build)."); return; }

        // Real recordings chosen by the user (Pixabay), made into seamless loops;
        // the older synthesized clips are only a fallback.
        var fire = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Real/fireplace_loop.wav")
                   ?? AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/fire_crackle.wav");
        var chatter = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Real/tavern_crowd_loop.wav")
                      ?? AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/pub_chatter.wav");

        var old = pub.transform.Find("Audio");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var root = new GameObject("Audio").transform; root.SetParent(pub.transform, false);

        // NO music. Diegetic sounds only: a busy-tavern CROWD MURMUR (people noise,
        // as requested) filling the room, plus the fireplace crackle at the hearth.
        // Footstep/stair SFX are attached per-character in MixamoSetup.cs.
        if (chatter != null)
        {
            var cGo = new GameObject("CrowdMurmur"); cGo.transform.SetParent(root, false);
            cGo.transform.localPosition = new Vector3(0, 1.5f, 0);
            var c = cGo.AddComponent<AudioSource>();
            c.clip = chatter; c.loop = true; c.playOnAwake = true; c.volume = 0f;
            c.spatialBlend = 0f;   // fills the room evenly once you're inside
            // audible ONLY while the camera is inside the pub - fades in at the door
            var amb = cGo.AddComponent<InteriorAmbience>();
            amb.room = pub.transform;
            amb.targetVolume = 0.55f;
        }
        if (fire != null)
        {
            var fGo = new GameObject("FireSound"); fGo.transform.SetParent(root, false);
            fGo.transform.localPosition = new Vector3(0, 1f, 5.4f);   // by the back-wall hearth
            var f = fGo.AddComponent<AudioSource>();
            f.clip = fire; f.loop = true; f.playOnAwake = true; f.volume = 0.5f;
            f.spatialBlend = 1f; f.minDistance = 2f; f.maxDistance = 14f; f.rolloffMode = AudioRolloffMode.Linear;
        }

        Selection.activeGameObject = root.gameObject;
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Audio: crowd murmur + fireplace crackle (no music). Footsteps/stair SFX per-character.");
    }
}
