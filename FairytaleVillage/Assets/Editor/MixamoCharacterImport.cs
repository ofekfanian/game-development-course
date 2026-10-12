using System.IO;
using UnityEngine;
using UnityEditor;

// HW2 fix - imports the real Mixamo CHARACTER models (Assets/Mixamo/Characters/
// Char1..Char5.fbx, downloaded clothed + faced from mixamo.com) as Humanoid so
// they drive the existing PubAnimator controller, extracts their embedded
// textures, and converts their materials to URP/Lit so they render correctly
// (a raw Mixamo FBX imports with Built-in Standard materials, which show up
// magenta under URP).
//   Menu: Tools > Pub Interior > Import Mixamo Characters
public static class MixamoCharacterImport
{
    const string DIR = "Assets/Mixamo/Characters";
    public static readonly string[] CharFiles =
        { "Char1", "Char2", "Char3", "Char4", "Char5" };

    [MenuItem("Tools/Pub Interior/Import Mixamo Characters")]
    public static void ImportCharacters()
    {
        string texDir = DIR + "/Textures";
        string matDir = DIR + "/Materials";
        Directory.CreateDirectory(texDir);
        Directory.CreateDirectory(matDir);
        AssetDatabase.Refresh();

        foreach (var name in CharFiles)
        {
            string path = DIR + "/" + name + ".fbx";
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) { Debug.LogError("Missing character FBX: " + path); continue; }

            // rig as Humanoid so the PubAnimator (Idle/Walk/Sit/Stairs) retargets
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            imp.importAnimation = false;             // these are model-only downloads
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            imp.SaveAndReimport();

            // pull the embedded textures out onto disk, then remap materials to
            // external assets so we can retarget their shader to URP
            imp.ExtractTextures(texDir);
            AssetDatabase.Refresh();

            imp.materialLocation = ModelImporterMaterialLocation.External;
            imp.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName,
                                        ModelImporterMaterialSearch.Local);
            imp.SaveAndReimport();
        }
        AssetDatabase.Refresh();

        // convert every generated material to URP/Lit, carrying the diffuse map
        FixMaterialsToURP();
        AssetDatabase.SaveAssets();
        Debug.Log("Mixamo characters imported as Humanoid + materials converted to URP. " +
                  "Now run Tools > Pub Interior > Build Combined World + Pub.");
    }

    static void FixMaterialsToURP()
    {
        var urp = Shader.Find("Universal Render Pipeline/Lit");
        if (urp == null) { Debug.LogWarning("URP/Lit shader not found - skipping material conversion."); return; }

        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { DIR }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m == null) continue;

            // read the Built-in diffuse BEFORE swapping the shader (the property
            // disappears once the shader changes)
            Texture diffuse = null;
            foreach (var prop in new[] { "_MainTex", "_BaseMap", "_Diffuse" })
                if (m.HasProperty(prop) && m.GetTexture(prop) != null) { diffuse = m.GetTexture(prop); break; }
            Color baseCol = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;

            m.shader = urp;
            if (diffuse != null) m.SetTexture("_BaseMap", diffuse);
            m.SetColor("_BaseColor", baseCol);
            m.SetFloat("_Smoothness", 0.1f);   // skin/cloth, not shiny
            EditorUtility.SetDirty(m);
        }
    }
}
