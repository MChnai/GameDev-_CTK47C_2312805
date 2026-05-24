using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Cấu hình Di Chuyển Mũi Tên")]
    public RectTransform arrow;          // Kéo Object "arrow" trong Hierarchy vào đây
    public float yOffset = 278.237f;     // Khoảng cách dịch chuyển Y giữa các nút

    [Header("Cấu hình Tên Scene")]
    public string gameplaySceneName = "GameplayScene";

    private int currentIndex = 0;        // 0: Start, 1: Continue, 2: Setting, 3: Quit
    private int maxButtons = 4;          // Tổng số nút trong Menu công nhận
    private Vector2 startArrowPosition;  // Lưu tọa độ gốc ban đầu của mũi tên

    void Start()
    {
        if (arrow != null)
        {
            // Ghi nhớ vị trí ban đầu của mũi tên (đang chỉ vào nút Start Game)
            startArrowPosition = arrow.anchoredPosition;
        }
    }

    void Update()
    {
        // 1. BẮT SỰ KIỆN BẤM PHÍM MŨI TÊN XUỐNG (HOẶC PHÍM S)
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            if (currentIndex < maxButtons - 1)
            {
                currentIndex++;
                UpdateArrowPosition();
                Debug.Log($"[MENU] Xuống nút: {currentIndex}");
            }
        }

        // 2. BẮT SỰ KIỆN BẤM PHÍM MŨI TÊN LÊN (HOẶC PHÍM W)
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            if (currentIndex > 0)
            {
                currentIndex--;
                UpdateArrowPosition();
                Debug.Log($"[MENU] Lên nút: {currentIndex}");
            }
        }

        // 3. BẮT SỰ KIỆN BẤM ENTER (HOẶC SPACE) ĐỂ CHỌN MENU
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            ExecuteMenuAction();
        }
    }

    // Hàm tính toán và dịch chuyển tọa độ Y của mũi tên
    void UpdateArrowPosition()
    {
        if (arrow != null)
        {
            // Tọa độ mới = Tọa độ gốc Y trừ đi (Chỉ số nút * khoảng cách 278.237)
            float newY = startArrowPosition.y - (currentIndex * yOffset);
            arrow.anchoredPosition = new Vector2(arrow.anchoredPosition.x, newY);
        }
    }

    // Hàm kích hoạt tính năng tương ứng khi ấn Enter
    void ExecuteMenuAction()
    {
        switch (currentIndex)
        {
            case 0: // Nút Start Game
                Debug.Log("Enter -> Bắt đầu chơi game!");
                SceneManager.LoadScene(gameplaySceneName);
                break;

            case 1: // Nút Continue
                Debug.Log("Enter -> Tính năng Continue chưa phát triển.");
                break;

            case 2: // Nút Setting
                Debug.Log("Enter -> Mở bảng cài đặt.");
                break;

            case 3: // Nút Quit Game
                Debug.Log("Enter -> Thoát game!");
                Application.Quit();
                break;
        }
    }
}