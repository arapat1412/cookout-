using TMPro;
using UnityEngine;

public class TeamScoreUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;

    private void Start()
    {
        GameMode currentMode = KitchenGameMultiplayer.Instance.GetGameMode();

        // Chỉ hiện khi chơi PvP (2 đội) hoặc PvP_3Team (3 đội)
        // Nếu là Coop -> Tắt UI này đi
        if (currentMode != GameMode.PvP && currentMode != GameMode.PvP_3Team)
        {
            gameObject.SetActive(false);
            return;
        }

        KitchenGameManager.Instance.OnTeamScoreChanged += UpdateScore;
        UpdateScore(null, System.EventArgs.Empty);
    }

    private void UpdateScore(object sender, System.EventArgs e)
    {
        var scores = KitchenGameManager.Instance.GetTeamScores();
        GameMode currentMode = KitchenGameMultiplayer.Instance.GetGameMode();

        if (currentMode == GameMode.PvP)
        {
            // --- CHẾ ĐỘ 2 ĐỘI: Chỉ hiện Xanh | Đỏ ---
            scoreText.text = $"<color=blue>{scores[Team.Blue]}</color> | <color=red>{scores[Team.Red]}</color>";
        }
        else
        {
            // --- CHẾ ĐỘ 3 ĐỘI: Hiện Xanh | Đỏ | Vàng ---
            scoreText.text = $"<color=blue>{scores[Team.Blue]}</color> | <color=red>{scores[Team.Red]}</color> | <color=yellow>{scores[Team.Yellow]}</color>";
        }
    }

    private void OnDestroy()
    {
        if (KitchenGameManager.Instance != null)
        {
            KitchenGameManager.Instance.OnTeamScoreChanged -= UpdateScore;
        }
    }
}