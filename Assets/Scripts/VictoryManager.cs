using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryManager : MonoBehaviour
{
    public static VictoryManager Instance;

    [Header("Cấu hình UI")]
    public GameObject victoryPanel; // Kéo VictoryPanel vào đây
    public string mainMenuSceneName = "MainMenu"; // Tên scene Menu chính của bạn

    private bool isVictoryActive = false;

    void Awake()
    {
        // Tạo Singleton để dễ dàng gọi từ script Boss
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }

    void Update()
    {
        // Nếu màn hình Victory đang hiện, đợi người chơi ấn Enter để về Menu
        if (isVictoryActive)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                Time.timeScale = 1f; // Trả lại thời gian bình thường trước khi chuyển Scene
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }
    }

    // Hàm kích hoạt màn hình chiến thắng (Sẽ gọi từ BossAI)
    public void TriggerVictory()
    {
        isVictoryActive = true;
        victoryPanel.SetActive(true);

        // Mẹo ngầu: Làm chậm thời gian (Slow Motion) bằng 1/3 bình thường để tăng độ kịch tính!
        Time.timeScale = 0.3f;
        Debug.Log("[VICTORY] Màn hình chiến thắng đã hiển thị!");
    }
}