using UnityEngine;

public class DebugDestroy : MonoBehaviour
{
    // Script này sẽ báo cáo ai là người gọi lệnh Destroy
    void OnDestroy()
    {
        // Lấy StackTrace (dấu vết) của lệnh gọi hàm
        string stackTrace = System.Environment.StackTrace;

        // In ra console cảnh báo đỏ rực
        Debug.LogError($"🚨 PHÁT HIỆN ĐỐI TƯỢNG {gameObject.name} BỊ HỦY!");
        Debug.LogError($"🔍 NGUYÊN NHÂN TỪ ĐÂU:\n{stackTrace}");
    }
}