using UnityEngine;

public class GlowingSlash : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    public float fadeSpeed = 5f;       // Tốc độ giảm dần độ sáng
    private float currentIntensity = 3f; // Cường độ phát sáng ban đầu (HDR)
    private Color originalColor;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Lưu lại màu gốc của vệt chém
            originalColor = spriteRenderer.color;
        }

        // Tự hủy vệt chém sau 0.25 giây để khớp với video animation của bạn
        Destroy(gameObject, 0.25f);
    }

    void Update()
    {
        if (spriteRenderer == null) return;

        // Giảm dần cường độ phát sáng theo thời gian (tạo hiệu ứng lịm dần)
        currentIntensity = Mathf.Lerp(currentIntensity, 0f, fadeSpeed * Time.deltaTime);

        // Áp dụng cường độ HDR vào màu sắc bằng công thức toán học cấp số nhân
        // Trong Unity, nhân màu sắc với một hệ số > 1 khi bật Bloom sẽ tạo ra hiệu ứng phát sáng rực rỡ
        Color glowColor = originalColor * currentIntensity;

        // Giữ nguyên thuộc tính suốt (Alpha) để vệt chém không bị lỗi hiển thị
        glowColor.a = originalColor.a;

        spriteRenderer.color = glowColor;
    }
}