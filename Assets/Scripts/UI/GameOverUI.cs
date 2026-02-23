using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
   [SerializeField] private TextMeshProUGUI recipesDeliveredText;
    [SerializeField] private Button playAgainButton;


    private void Awake()
    {
        playAgainButton.onClick.AddListener(() => {
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenuScene);
        });
    }


    private void Start()
    {
        KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
        Hide();

    }

    private void KitchenGameManager_OnStateChanged(object sender, System.EventArgs e)
    {
        if (KitchenGameManager.Instance.IsGameOver())
        {
            Show();
            GameMode currentMode = KitchenGameMultiplayer.Instance.GetGameMode();

            // --- XỬ LÝ HIỂN THỊ CHO CHẾ ĐỘ PVP (ĐỐI KHÁNG) ---
            if (currentMode == GameMode.PvP || currentMode == GameMode.PvP_3Team)
            {
                var scores = KitchenGameManager.Instance.GetTeamScores();
                int blueScore = scores[Team.Blue];
                int redScore = scores[Team.Red];
                int yellowScore = scores[Team.Yellow];

                string resultText = "";
                string scoreDetail = "";

                // --- LOGIC CHO 2 ĐỘI ---
                if (currentMode == GameMode.PvP)
                {
                    scoreDetail = $"{blueScore} - {redScore}";

                    if (blueScore > redScore)
                        resultText = $"<color=blue>BLUE TEAM WINS!</color>";
                    else if (redScore > blueScore)
                        resultText = $"<color=red>RED TEAM WINS!</color>";
                    else
                        resultText = "DRAW!";
                }
                // --- LOGIC CHO 3 ĐỘI (XỬ LÝ MỌI TRƯỜNG HỢP HÒA) ---
                else
                {
                    scoreDetail = $"{blueScore} - {redScore} - {yellowScore}";

                    // 1. Tìm điểm cao nhất
                    int maxScore = Mathf.Max(blueScore, Mathf.Max(redScore, yellowScore));

                    // 2. Xác định ai đạt điểm cao nhất đó
                    bool blueWon = (blueScore == maxScore);
                    bool redWon = (redScore == maxScore);
                    bool yellowWon = (yellowScore == maxScore);

                    // 3. Hiển thị kết quả chi tiết
                    if (blueWon && redWon && yellowWon)
                    {
                        resultText = "ALL TEAMS DRAW!";
                    }
                    else if (blueWon && redWon)
                    {
                        resultText = "<color=blue>BLUE</color> & <color=red>RED</color> WIN!";
                    }
                    else if (blueWon && yellowWon)
                    {
                        resultText = "<color=blue>BLUE</color> & <color=yellow>YELLOW</color> WIN!";
                    }
                    else if (redWon && yellowWon)
                    {
                        resultText = "<color=red>RED</color> & <color=yellow>YELLOW</color> WIN!";
                    }
                    else if (blueWon)
                    {
                        resultText = "<color=blue>BLUE TEAM WINS!</color>";
                    }
                    else if (redWon)
                    {
                        resultText = "<color=red>RED TEAM WINS!</color>";
                    }
                    else if (yellowWon)
                    {
                        resultText = "<color=yellow>YELLOW TEAM WINS!</color>";
                    }
                }

                // Gán text kết quả + điểm số
                recipesDeliveredText.text = $"{resultText}\n{scoreDetail}";
            }
            // --- XỬ LÝ CHO CHẾ ĐỘ COOP ---
            else
            {
                int goldEarned = DeliveryManager.Instance.GetSessionGoldEarned();
                string coopText = "RECIPES DELIVERED: " + DeliveryManager.Instance.GetSuccessfulRecipesAmount().ToString();
                coopText += $"\n\n<color=yellow>+{goldEarned} GOLD EARNED!</color>";
                recipesDeliveredText.text = coopText;
            }
        }
        else
        {
            Hide();
        }
    }


    private void Show()
    {
        gameObject.SetActive(true);
        playAgainButton.Select();

    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    
}
