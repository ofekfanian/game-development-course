using System.IO;
using UnityEditor;
using UnityEngine;

// Imports Asset Store sound packs that were downloaded via Package Manager
// (they are cached as .unitypackage under ~/Library/Unity/Asset Store-5.x),
// silently - no import dialog.
//   Menu: Tools > Pub Interior > Import Downloaded Sound Packs
public static class SoundPackImport
{
    [MenuItem("Tools/Pub Interior/Import Downloaded Sound Packs")]
    public static void ImportAll()
    {
        string root = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
                                   "Library/Unity/Asset Store-5.x");
        if (!Directory.Exists(root)) { Debug.LogError("No Asset Store cache at " + root); return; }
        int n = 0;
        foreach (var pkg in Directory.GetFiles(root, "*.unitypackage", SearchOption.AllDirectories))
        {
            string dir = Path.GetDirectoryName(pkg);
            if (!dir.Contains("Audio")) continue;          // only the sound packs
            Debug.Log("Importing sound pack: " + pkg);
            AssetDatabase.ImportPackage(pkg, false);
            n++;
        }
        Debug.Log("Sound packs imported: " + n);
    }
}
