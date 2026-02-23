
using UnityEngine;

public class DeliveryCounterVisual : MonoBehaviour
{
    [SerializeField] private DeliveryCounter deliveryCounter;
    [SerializeField] private MeshRenderer counterMeshRenderer;

    [Header("Team Colors")]
    [SerializeField] private Color blueTeamColor = new Color(0.3f, 0.5f, 1f);
    [SerializeField] private Color redTeamColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color yellowTeamColor = new Color(1f, 0.92f, 0.016f);

    private void Start()
    {
        ApplyTeamColor();
    }

    private void ApplyTeamColor()
    {
        if (counterMeshRenderer == null || deliveryCounter == null)
            return;

        Team team = deliveryCounter.GetTeam();

        if (team == Team.Blue)
        {
            counterMeshRenderer.material.color = blueTeamColor;
        }
        else if (team == Team.Red)
        {
            counterMeshRenderer.material.color = redTeamColor;
        }
        else if (team == Team.Yellow)
        {
            counterMeshRenderer.material.color = yellowTeamColor;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Apply Color")]
    private void TestApplyColor()
    {
        ApplyTeamColor();
    }
#endif
}