using UnityEngine;

public class TetherSystem : MonoBehaviour
{
    [Header("References")]
    public Transform kairi;
    public Transform shiori;
    private KairiController kairiController;
    private LineRenderer lineRenderer;

    [Header("Tether Settings")]
    public float maxDistance = 5f;        // Bán kính an toàn tối đa 5m
    public float blightDamage = 10f;      // Sát thương rút máu mỗi giây khi vượt khoảng cách
    public float baseKairiHP = 100f;      // Máu gốc của Kairi

    private float currentKairiHP;
    private float originalSpeed;

    void Start()
    {
        // Lấy các thành phần Component cần thiết
        kairiController = kairi.GetComponent<KairiController>();
        lineRenderer = GetComponent<LineRenderer>();

        currentKairiHP = baseKairiHP;
        if (kairiController != null)
        {
            originalSpeed = kairiController.moveSpeed;
        }

        // Cấu hình cơ bản cho sợi dây LineRenderer bằng Code để đỡ phải chỉnh tay
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.08f;
        lineRenderer.endWidth = 0.08f;
        // Tạo một màu đỏ mặc định cho dây
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.white;
    }

    void Update()
    {
        if (kairi == null || shiori == null) return;

        // 1. Cập nhật vị trí sợi xích nối liền 2 nhân vật
        lineRenderer.SetPosition(0, kairi.position);
        lineRenderer.SetPosition(1, shiori.position);

        // 2. Tính khoảng cách hiện tại giữa Kairi và Shiori
        float currentDistance = Vector3.Distance(kairi.position, shiori.position);

        // 3. Kiểm tra logic nếu khoảng cách vượt quá 5m (Vessel Logic)
        if (currentDistance > maxDistance)
        {
            // Đổi màu sợi xích sang màu đỏ cảnh báo căng thẳng
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;

            // Giảm 50% tốc độ chạy của Kairi
            if (kairiController != null)
            {
                kairiController.currentSpeed = originalSpeed * 0.5f;
            }

            // Trừ máu (HP) của Kairi theo thời gian do chướng khí nhựa đen
            currentKairiHP -= blightDamage * Time.deltaTime;
            Debug.LogWarning("Kairi đang nhiễm độc nhựa đen! HP còn: " + (int)currentKairiHP);

            if (currentKairiHP <= 0)
            {
                currentKairiHP = 0;
                Debug.LogError("Kairi đã gục ngã vì chướng khí!");
                // Bạn có thể thêm logic Chết (Reload màn chơi) ở đây sau
            }
        }
        else
        {
            // Nếu ở trong tầm an toàn, trả lại sợi xích màu trắng/vàng và tốc độ bình thường
            lineRenderer.startColor = Color.yellow;
            lineRenderer.endColor = Color.yellow;

            if (kairiController != null)
            {
                kairiController.currentSpeed = originalSpeed;
            }
        }
    }
}