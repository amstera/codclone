using Mirror;
using UnityEngine;

public class Manager : NetworkBehaviour
{
    public static Manager Instance;
    public GameObject PlayerPrefab;
    [SyncVar(hook = "ScoreUpdated")]
    public int RedPoints;
    [SyncVar(hook = "ScoreUpdated")]
    public int BluePoints;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void ScoreUpdated(int oldValue, int newValue)
    {
        foreach (Player player in FindObjectsOfType<Player>())
        {
            if (player.isLocalPlayer)
            {
                player.UpdateScore();
                break;
            }
        }
    }
}
