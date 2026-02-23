using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class BotSpawner : NetworkBehaviour
{
    [SerializeField] private Transform playerPrefab;

    private HashSet<ulong> spawnedBotIds = new HashSet<ulong>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        Debug.Log("BOT SPAWNER: Đang kiểm tra danh sách người chơi để spawn bot...");

        KitchenGameMultiplayer.Instance.OnPlayerDataNetworkListChanged += OnPlayerListChanged;

        // ✅ THÊM: Kiểm tra xem Bot đã tồn tại trong scene chưa
        CheckExistingBotsInScene();

        SpawnExistingBots();
    }

    // ✅ HÀM MỚI: Kiểm tra Bot đã có sẵn trong scene
    private void CheckExistingBotsInScene()
    {
        Player[] allPlayers = FindObjectsOfType<Player>();
        foreach (Player player in allPlayers)
        {
            // Nếu là Bot (có KitchenAI component), đánh dấu đã spawn
            if (player.TryGetComponent(out KitchenAI _))
            {
                ulong botId = player.GetPlayerDataId();
                if (botId >= 9000)
                {
                    spawnedBotIds.Add(botId);
                    Debug.Log($"BOT SPAWNER: Phát hiện Bot {botId} đã tồn tại trong scene.");
                }
            }
        }
    }

    private void OnPlayerListChanged(object sender, System.EventArgs e)
    {
        if (!IsServer) return;
        SpawnExistingBots();
    }

    private void SpawnExistingBots()
    {
        var playerList = KitchenGameMultiplayer.Instance.GetPlayerDataNetworkList();

        foreach (var playerData in playerList)
        {
            if (playerData.clientId >= 9000 && !spawnedBotIds.Contains(playerData.clientId))
            {
                Debug.Log($"BOT SPAWNER: Tìm thấy Bot mới ID {playerData.clientId}");
                SpawnBotInstance(playerData);
                spawnedBotIds.Add(playerData.clientId);
            }
        }
    }

    private void SpawnBotInstance(PlayerData botData)
    {
        // 1. Instantiate Bot ở vị trí "An toàn" (Cao hơn mặt đất 1 chút để không kẹt)
        // Lưu ý: Vector3.up * 2 nghĩa là cao 2 mét
        Transform botTransform = Instantiate(playerPrefab, Vector3.up * 2f, Quaternion.identity);

        NetworkObject networkObject = botTransform.GetComponent<NetworkObject>();
        networkObject.Spawn(true);

        // 2. Tắt NavMeshAgent ngay lập tức để tránh nó tự tìm đường lung tung khi chưa setup xong
        if (botTransform.TryGetComponent(out UnityEngine.AI.NavMeshAgent agent))
        {
            agent.enabled = false;
        }

        StartCoroutine(SetupBotAfterSpawn(botTransform, botData));
    }

    private System.Collections.IEnumerator SetupBotAfterSpawn(Transform botTransform, PlayerData botData)
    {
        // Đợi 1 frame để các script khởi chạy xong
        yield return null;

        if (botTransform != null && botTransform.TryGetComponent(out Player playerScript))
        {
            playerScript.SetupAsBot(botData.clientId);

            // Sau khi Setup xong vị trí (trong Player.cs), ta mới bật lại NavMeshAgent
            yield return null;
            if (botTransform.TryGetComponent(out UnityEngine.AI.NavMeshAgent agent))
            {
                agent.enabled = true;
            }
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (KitchenGameMultiplayer.Instance != null)
        {
            KitchenGameMultiplayer.Instance.OnPlayerDataNetworkListChanged -= OnPlayerListChanged;
        }
    }
}