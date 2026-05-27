using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("--- MAIN MENU ---")]
    public RectTransform mainArrow;
    public float mainYOffset = 278.237f;
    public string gameplaySceneName = "GamePlay";
    private int mainIndex = 0;
    private int maxMainButtons = 4;
    private Vector2 startMainArrowPos;

    [Header("--- SETTINGS OVERLAY ---")]
    public GameObject settingsPanel;
    public RectTransform settingsArrow;
    public float settingsYOffset = 60f;

    // ĐƯA CÁC TEXT HIỂN THỊ PHÍM VÀO ĐÂY
    public TextMeshProUGUI moveLeftText;
    public TextMeshProUGUI moveRightText;
    public TextMeshProUGUI jumpText;
    public TextMeshProUGUI dashText;
    public TextMeshProUGUI resolutionText;

    private int settingsIndex = 0;
    private int maxSettingsButtons = 6;  // Tổng cộng 6 dòng: Left, Right, Jump, Dash, Resolution, Back
    private Vector2 startSettingsArrowPos;

    private Vector2Int[] resolutions = new Vector2Int[] { new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(3840, 2160) };
    private int currentResIndex = 0;

    private bool isSettingsOpen = false;
    private bool isWaitingForKey = false; // Cờ kiểm tra xem có phải đang đợi người chơi ấn phím mới hay không

    void Start()
    {
        if (mainArrow != null) startMainArrowPos = mainArrow.anchoredPosition;
        if (settingsArrow != null) startSettingsArrowPos = settingsArrow.anchoredPosition;
        if (settingsPanel != null) settingsPanel.SetActive(false);

        UpdateResolutionUI();
        UpdateKeyTextsUI(); // Hiển thị phím mặc định lên màn hình
    }

    void Update()
    {
        // NẾU ĐANG TRONG TRẠNG THÁI ĐỢI ẤN PHÍM MỚI ĐỂ ĐỔI
        if (isWaitingForKey)
        {
            ListenForNewKey();
            return; // Khóa không cho di chuyển mũi tên đi chỗ khác
        }

        if (isSettingsOpen)
        {
            HandleSettingsNavigation();
        }
        else
        {
            HandleMainMenuNavigation();
        }
    }

    // --- ĐIỀU HƯỚNG MENU (Giữ nguyên logic cũ của bạn) ---
    void HandleMainMenuNavigation()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) { if (mainIndex < maxMainButtons - 1) { mainIndex++; UpdateMainArrow(); } }
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) { if (mainIndex > 0) { mainIndex--; UpdateMainArrow(); } }
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)) { ExecuteMainMenuAction(); }
    }
    void UpdateMainArrow() { 
        float newY = startMainArrowPos.y - (mainIndex * mainYOffset); mainArrow.anchoredPosition = new Vector2(mainArrow.anchoredPosition.x, newY); 
    }
    void ExecuteMainMenuAction() {
        switch (mainIndex) {
            case 0: 
                SceneManager.LoadScene(gameplaySceneName); 
                break;
            case 2: 
                OpenSettings(); 
                break;
            case 3:
                Debug.Log("[HỆ THỐNG] Người chơi đã chọn thoát game hoàn toàn!");

                Application.Quit();

                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #endif
                break;
        } 
    }
    void OpenSettings() { 
        isSettingsOpen = true; settingsIndex = 0;
        if (settingsPanel != null) 
            settingsPanel.SetActive(true); 
        UpdateSettingsArrow(); UpdateKeyTextsUI(); 
    }
    void CloseSettings() {
        isSettingsOpen = false; 
        if (settingsPanel != null) 
            settingsPanel.SetActive(false);
    }

    void HandleSettingsNavigation()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) { if (settingsIndex < maxSettingsButtons - 1) { settingsIndex++; UpdateSettingsArrow(); } }
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) { if (settingsIndex > 0) { settingsIndex--; UpdateSettingsArrow(); } }
        if (Input.GetKeyDown(KeyCode.Escape)) CloseSettings();
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)) ExecuteSettingsAction();
    }
    void UpdateSettingsArrow() {
        float newY = startSettingsArrowPos.y - (settingsIndex * settingsYOffset); settingsArrow.anchoredPosition = new Vector2(settingsArrow.anchoredPosition.x, newY); 
    }

    // --- LOGIC XỬ LÝ ĐỔI PHÍM BẰNG ENTER ---
    void ExecuteSettingsAction()
    {
        switch (settingsIndex)
        {
            case 0: StartRebinding("MOVE LEFT"); break;
            case 1: StartRebinding("MOVE RIGHT"); break;
            case 2: StartRebinding("JUMP"); break;
            case 3: StartRebinding("DASH"); break;
            case 4: ToggleResolution(); break; // Dòng đổi độ phân giải
            case 5: CloseSettings(); break;    // Dòng quay lại
        }
    }

    void StartRebinding(string actionName)
    {
        isWaitingForKey = true;
        // Gợi ý cho người chơi biết là hệ thống đang chờ nhận nút bấm mới
        if (settingsIndex == 0) moveLeftText.text = "MOVE LEFT: [ PRESS ANY KEY ]";
        if (settingsIndex == 1) moveRightText.text = "MOVE RIGHT: [ PRESS ANY KEY ]";
        if (settingsIndex == 2) jumpText.text = "JUMP: [ PRESS ANY KEY ]";
        if (settingsIndex == 3) dashText.text = "DASH: [ PRESS ANY KEY ]";
    }

    // Hàm liên tục lắng nghe xem người chơi ấn nút gì để nạp vào hệ thống
    void ListenForNewKey()
    {
        if (Input.anyKeyDown)
        {
            // Quét qua toàn bộ danh sách nút trên bàn phím để tìm nút vừa ấn
            foreach (KeyCode kcode in System.Enum.GetValues(typeof(KeyCode)))
            {
                // Tránh nhận nhầm nút Enter hoặc phím mũi tên điều hướng Menu
                if (Input.GetKeyDown(kcode) && kcode != KeyCode.Return && kcode != KeyCode.DownArrow && kcode != KeyCode.UpArrow)
                {
                    if (KeyBindManager.Instance != null)
                    {
                        if (settingsIndex == 0) KeyBindManager.Instance.MoveLeftKey = kcode;
                        if (settingsIndex == 1) KeyBindManager.Instance.MoveRightKey = kcode;
                        if (settingsIndex == 2) KeyBindManager.Instance.JumpKey = kcode;
                        if (settingsIndex == 3) KeyBindManager.Instance.DashKey = kcode;
                    }

                    isWaitingForKey = false; // Kết thúc trạng thái chờ
                    UpdateKeyTextsUI();     // Vẽ lại chữ nút mới lên màn hình
                    break;
                }
            }
        }
    }

    // Hàm vẽ chữ hiển thị các phím hiện tại lên màn hình
    void UpdateKeyTextsUI()
    {
        if (KeyBindManager.Instance == null) return;

        if (moveLeftText != null) moveLeftText.text = $"MOVE LEFT: {KeyBindManager.Instance.MoveLeftKey}";
        if (moveRightText != null) moveRightText.text = $"MOVE RIGHT: {KeyBindManager.Instance.MoveRightKey}";
        if (jumpText != null) jumpText.text = $"JUMP: {KeyBindManager.Instance.JumpKey}";
        if (dashText != null) dashText.text = $"DASH: {KeyBindManager.Instance.DashKey}";
    }

    void ToggleResolution()
    {
        currentResIndex = (currentResIndex + 1) % resolutions.Length;
        Vector2Int targetRes = resolutions[currentResIndex];
        Screen.SetResolution(targetRes.x, targetRes.y, FullScreenMode.FullScreenWindow);
        UpdateResolutionUI();
    }

    void UpdateResolutionUI()
    {
        if (resolutionText != null)
        {
            Vector2Int currentRes = resolutions[currentResIndex];
            string resName = (currentRes.x == 1920) ? "Full HD" : (currentRes.x == 2560) ? "QHD" : "4K UHD";
            resolutionText.text = $"RESOLUTION: {currentRes.x}x{currentRes.y} ({resName})";
        }
    }
}