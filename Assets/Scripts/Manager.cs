using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class Manager : NetworkBehaviour
{
    public static Manager Instance;
    public GameObject PlayerPrefab;
    public Canvas Canvas;
    public Canvas HUDCanvas;
    public Text WinningText;
    public Text VictoryText;
    [SyncVar(hook = "ScoreUpdated")]
    public int RedPoints;
    [SyncVar(hook = "ScoreUpdated")]
    public int BluePoints;

    [Header("Sounds")]
    public AudioSource VictorySound;
    public AudioSource DefeatSound;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void ShowFinalScore(TeamColor color)
    {
        TeamColor winningTeamColor = RedPoints > BluePoints ? TeamColor.Red : TeamColor.Blue;
        if (winningTeamColor == TeamColor.Red)
        {
            WinningText.text = "Red Wins";
            WinningText.color = Color.red;
        }
        else
        {
            WinningText.text = "Blue Wins";
            WinningText.color = Color.blue;
        }
        if (color == winningTeamColor)
        {
            VictoryText.text = "Victory!";
            VictoryText.color = new Color(255 / 255, 197f / 255, 37f / 255);
            VictorySound.Play();
        }
        else
        {
            VictoryText.text = "Defeat!";
            VictoryText.color = Color.red;
            DefeatSound.Play();
        }

        Canvas.enabled = true;
        HUDCanvas.renderMode = RenderMode.WorldSpace;
    }

    public void HideFinalScore()
    {
        Canvas.enabled = false;
        HUDCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
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
