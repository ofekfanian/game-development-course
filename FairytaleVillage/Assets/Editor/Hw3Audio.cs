using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// HW3 sounds - adds the user's real recordings to the playable game WITHOUT
// rebuilding the scenes:
//   * player footsteps (both scenes)          - Assets/Audio/Real/step_real_*.wav
//   * a crackling fire at the house fireplace  - Assets/Audio/Real/fireplace_loop.wav
//   * the tavern crowd, heard only near the pub - Assets/Audio/Real/tavern_crowd_loop.wav
// and removes the old looping background music (the user asked for no music).
//   Menu: Tools > Homework 3 > Add Sounds (footsteps, fire, crowd)
public static class Hw3Audio
{
    const string VILLAGE = "Assets/Scenes/Village.unity";
    const string INTERIOR = "Assets/Scenes/HouseInterior.unity";
    const string FIRE = "Assets/Audio/Real/fireplace_loop.wav";
    const string CROWD = "Assets/Audio/Real/tavern_crowd_loop.wav";

    static AudioClip Clip(string p) => AssetDatabase.LoadAssetAtPath<AudioClip>(p);

    static AudioClip[] Steps()
    {
        var list = new List<AudioClip>();
        for (int i = 1; i <= 10; i++)
        {
            var c = Clip("Assets/Audio/Real/step_real_" + i.ToString("00") + ".wav");
            if (c != null) list.Add(c);
        }
        if (list.Count == 0)   // fallback: the Asset Store wood-floor steps
            for (int i = 1; i <= 10; i++)
            {
                var c = Clip("Assets/Footsteps - Essentials/Footsteps_Wood/Footsteps_Wood_Walk/Footsteps_Wood_Walk_" + i.ToString("00") + ".wav");
                if (c != null) list.Add(c);
            }
        return list.ToArray();
    }

    [MenuItem("Tools/Homework 3/Add Sounds (footsteps, fire, crowd)")]
    public static void AddToBothScenes()
    {
        EditorSceneManager.SaveOpenScenes();

        var s = EditorSceneManager.OpenScene(VILLAGE, OpenSceneMode.Single);
        AddVillageAudio();
        EditorSceneManager.MarkSceneDirty(s);
        EditorSceneManager.SaveScene(s);

        s = EditorSceneManager.OpenScene(INTERIOR, OpenSceneMode.Single);
        AddInteriorAudio();
        EditorSceneManager.MarkSceneDirty(s);
        EditorSceneManager.SaveScene(s);

        EditorSceneManager.OpenScene(VILLAGE, OpenSceneMode.Single);
        Debug.Log("HW3 sounds added: player footsteps (both scenes), house fireplace, pub crowd; music removed.");
    }

    static void AddPlayerFootsteps()
    {
        var p = GameObject.Find("Player");
        if (p == null) { Debug.LogWarning("HW3 audio: no Player in " + EditorSceneManager.GetActiveScene().name); return; }
        var old = p.GetComponent<PlayerFootsteps>();
        if (old != null) Object.DestroyImmediate(old);
        var src = p.GetComponent<AudioSource>();
        if (src == null) src = p.AddComponent<AudioSource>();
        src.playOnAwake = false; src.loop = false;
        src.spatialBlend = 0f;                   // the player's own feet
        var fs = p.AddComponent<PlayerFootsteps>();
        fs.clips = Steps();
    }

    static AudioSource LoopSource(string name, Transform parent, Vector3 localPos, AudioClip clip,
                                  float volume, float minD, float maxD)
    {
        var old = parent != null ? parent.Find(name) : null;
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var a = go.AddComponent<AudioSource>();
        a.clip = clip; a.loop = true; a.playOnAwake = true; a.volume = volume;
        a.spatialBlend = 1f; a.rolloffMode = AudioRolloffMode.Linear;
        a.minDistance = minD; a.maxDistance = maxD;
        return a;
    }

    public static void AddVillageAudio()
    {
        AddPlayerFootsteps();

        // no background music in the game
        int removed = 0;
        foreach (var a in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include))
            if (a != null && a.clip != null && a.clip.name.Contains("pub_music"))
            { Object.DestroyImmediate(a.gameObject); removed++; }

        // the pub: real fire + the tavern crowd, audible only when you're near/inside
        var pub = GameObject.Find("Pub");
        if (pub != null)
        {
            var audio = pub.transform.Find("Audio");
            if (audio == null) { audio = new GameObject("Audio").transform; audio.SetParent(pub.transform, false); }
            LoopSource("FireSound", audio, new Vector3(0f, 1f, 5.4f), Clip(FIRE), 0.6f, 2f, 14f);
            LoopSource("CrowdMurmur", audio, new Vector3(0f, 1.5f, 0f), Clip(CROWD), 0.6f, 4f, 22f);
        }
        Debug.Log("HW3 village audio: footsteps + pub fire/crowd, music sources removed: " + removed);
    }

    public static void AddInteriorAudio()
    {
        AddPlayerFootsteps();
        var room = GameObject.Find("Room");
        // the house fireplace is on the back (+Z) wall at x=0, z ~ 5.4
        LoopSource("FireSound", room != null ? room.transform : null, new Vector3(0f, 1f, 5.0f), Clip(FIRE), 0.75f, 2f, 12f);
        Debug.Log("HW3 interior audio: footsteps + fireplace.");
    }
}
