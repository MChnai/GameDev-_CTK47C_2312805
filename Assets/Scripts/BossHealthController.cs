using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthController : MonoBehaviour
{
    public static BossHealthController Instance;

    [Header("---- UI Elements ----")]
    public Slider healthSlider;
    public TextMeshProUGUI bossNameText;
    public TextMeshProUGUI healthText;
    public Image phaseDot1;
    public Image ImagePhaseDot2;

    [Header("---- Boss UI Settings ----")]
    public string bossName = "The Traitor";
    public float maxHealthPhase1 = 500f; // ĐÃ SỬA THÀNH 500 CHO KHỚP VỚI MAXHP CỦA BOSSAI
    public float maxHealthPhase2 = 1000f; // Máu của Boss khi bước sang Phase 2 phân thân
    public float refillSpeed = 400f;      // Tốc độ chạy đầy thanh máu khi đổi phase

    [Header("---- Phase Colors ----")]
    public Color dotActiveColor = Color.white;
    public Color dotDisabledColor = new Color(0.15f, 0f, 0f, 1f);

    private float currentHealthDisplay;
    private float currentMaxHealthDisplay;
    private int currentPhase = 1;
    private bool isRefilling = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        bossNameText.text = bossName;
        currentPhase = 1;
        currentMaxHealthDisplay = maxHealthPhase1;
        currentHealthDisplay = currentMaxHealthDisplay;

        HideHealthBar();
        UpdateUI();
    }

    // Hàm nhận lệnh đồng bộ lượng máu giảm trực tiếp từ BossAI gửi qua
    public void SyncBossDamage(float damage, float actualBossHP)
    {
        if (isRefilling) return;

        // Cập nhật lượng máu hiển thị khớp với máu logic của BossAI
        currentHealthDisplay = actualBossHP;
        if (currentHealthDisplay < 0) currentHealthDisplay = 0;

        UpdateUI();
    }

    // Hàm nhận lệnh chuyển Phase 2 từ BossAI
    public void NotifyPhase2Transition()
    {
        if (currentPhase == 1)
        {
            StartCoroutine(TransitionToPhase2Routine());
        }
    }

    IEnumerator TransitionToPhase2Routine()
    {
        isRefilling = true;
        currentPhase = 2;

        yield return new WaitForSeconds(1f); // Khựng nhẹ tạo hiệu ứng gầm rú đổi phase

        if (phaseDot1 != null) phaseDot1.color = dotDisabledColor;
        if (ImagePhaseDot2 != null) ImagePhaseDot2.color = dotActiveColor;

        currentMaxHealthDisplay = maxHealthPhase2;
        currentHealthDisplay = 0f; // Bắt đầu chạy refill từ 0 lên đầy cây máu mới

        // Tìm đối tượng Boss thực tế trong màn chơi để reset máu logic của nó lên Phase 2
        BossAI bossLogic = FindObjectOfType<BossAI>();
        if (bossLogic != null && bossLogic.isRealBoss)
        {
            bossLogic.currentHP = maxHealthPhase2; // Gán máu thật của Boss bằng max máu phase 2
        }

        // Tạo hiệu ứng thanh máu tự động chạy tăng dần lên Max cực đẹp mắt
        while (currentHealthDisplay < currentMaxHealthDisplay)
        {
            currentHealthDisplay += refillSpeed * Time.deltaTime;
            if (currentHealthDisplay > currentMaxHealthDisplay) currentHealthDisplay = currentMaxHealthDisplay;

            UpdateUI();
            yield return null;
        }

        isRefilling = false;
        Debug.Log("[UI BOSS] Thanh máu Phase 2 đã sẵn sàng chiến đấu!");
    }

    void UpdateUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = currentMaxHealthDisplay;
            healthSlider.value = currentHealthDisplay;
        }

        if (healthText != null)
        {
            healthText.text = Mathf.RoundToInt(currentHealthDisplay) + " / " + currentMaxHealthDisplay;
        }

        if (currentPhase == 1)
        {
            if (phaseDot1 != null) phaseDot1.color = dotActiveColor;
            if (ImagePhaseDot2 != null) ImagePhaseDot2.color = dotActiveColor;
        }
    }

    public void TriggerBossDeath()
    {
        currentHealthDisplay = 0;
        UpdateUI();
        Debug.Log("[UI BOSS] Boss đã cạn sạch máu!");
    }
    // Hàm ẩn hoàn toàn giao diện thanh máu (gọi khi Boss chết hẳn)
    public void HideHealthBar()
    {
        // Tắt GameObject chứa script này (ẩn toàn bộ cụm thanh máu, tên boss, chấm phase)
        gameObject.SetActive(false);
        Debug.Log("[UI BOSS] Đã ẩn thanh máu khỏi màn hình.");
    }
    // Hàm hiện lại thanh máu (gọi khi Boss lọt vào tầm mắt Camera)
    public void ShowHealthBar()
    {
        // Chỉ hiện lại nếu Boss chưa chết (tránh trường hợp Boss đang tan biến mà Camera nhìn vào lại hiện lên)
        if (currentHealthDisplay > 0)
        {
            gameObject.SetActive(true);
            UpdateUI(); // Cập nhật lại chỉ số cho chính xác
            Debug.Log("[UI BOSS] Boss đã xuất hiện! Hiện thanh máu.");
        }
    }
}