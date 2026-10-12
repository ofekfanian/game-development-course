using UnityEngine;

// HW3 - the tiny bit of state that must SURVIVE a scene change.
// A plain static class already persists across SceneManager.LoadScene, so the
// coins the player carries into the house (and any collected inside it) stay on
// the counter when he walks back out to the village.
public static class GameState
{
    public static int Coins;            // total coins collected, village + house
    public static bool HasKey;          // took the key out of the chest
    public static bool ChestOpen;       // the chest lid has been opened
    public static bool ReturningFromHouse; // set when leaving the interior scene

    // Reset every time Play starts, even with "Reload Domain" turned off.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetForPlay()
    {
        Coins = 0;
        HasKey = false;
        ChestOpen = false;
        ReturningFromHouse = false;
    }
}
