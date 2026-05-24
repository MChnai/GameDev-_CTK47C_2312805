using UnityEngine;
public class ShioriFollow : MonoBehaviour  
{
    [Header("Target Settings")]
    public Transform target;        // Kéo thả GameObject Kairi vào đây trên Inspector

    [Header("Follow Settings")]
    public Vector3 offset = new Vector3(-0.8f, 0.6f, 0f); // Khoảng cách bay phía sau/trên vai Kairi
    public float followSpeed = 3f;  // Độ mượt mà khi bay theo (càng cao bay càng bám sát)

    [Header("Hover Effect")]
    public float hoverAmplitude = 0.15f; // Độ cao của nhịp bay dập dềnh tự nhiên
    public float hoverFrequency = 2f;    // Tốc độ dập dềnh nhanh hay chậm

    private Vector3 velocity = Vector3.zero;
    private float startY;

    void Start()
    {
        // Khóa vật lý của Lyra để không bị va chạm làm lệch hướng
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic; // Biến thành dạng chuyển động không trọng lực
            rb.simulated = true;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true; // Cho phép đi xuyên tường/đất để bám theo Kairi
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. TÍNH TOÁN VỊ TRÍ BAY THEO (Dựa theo hướng nhìn của Kairi)
        float currentOffsetDirection = target.localScale.x;
        // Nếu Kairi quay mặt sang trái, offset trục X sẽ tự động đảo chiều ra phía sau lưng
        Vector3 targetOffset = new Vector3(offset.x * currentOffsetDirection, offset.y, offset.z);
        Vector3 targetPosition = target.position + targetOffset;

        // 2. TẠO HIỆU ỨNG BAY DẬP DỀNH (Hovering) NHƯ TINH LINH THỰC SỰ
        float hoverY = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        targetPosition.y += hoverY;

        // 3. DI CHUYỂN MƯỢT MÀ (Smooth Damp) TRÁNH GIẬT KHUNG HÌNH
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 1f / followSpeed);

        // 4. ĐỒNG BỘ HƯỚNG QUAY MẶT THEO KAIRI
        transform.localScale = new Vector3(target.localScale.x, 1f, 1f);
    }
}