using UnityEngine;

public class AmberExplosion : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float explosionRadius = 2.5f; // Tăng nhẹ bán kính vụ nổ cho hợp với đòn kết liễu diện rộng
    public float projectileSpeed = 8f;   // Tốc độ bay của quả cầu hổ phách về phía trước
    public float lifeTime = 3f;          // Tự biến mất sau 3 giây nếu không chạm trúng ai để tránh rác bộ nhớ

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Nếu quả cầu hổ phách có Rigidbody2D, ép nó không chịu ảnh hưởng bởi trọng lực (không bị rơi xuống đất)
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            // Cho quả cầu bay thẳng về phía trước dựa theo hướng quay mặt của Kairi khi bắn ra
            rb.linearVelocity = transform.right * projectileSpeed;
        }

        // Tự hủy sau một khoảng thời gian nếu bay ra ngoài bản đồ
        Destroy(gameObject, lifeTime);
    }

    // 🔥 SỬA LOGIC: Quả cầu chạm trúng BOSS hoặc QUÁI VẬT sẽ tự nổ tung lập tức
    void OnTriggerEnter2D(Collider2D other)
    {
        // Tránh tự kích nổ khi vừa bay ra khỏi người Kairi (bỏ qua Collider của người chơi)
        if (other.CompareTag("Player")) return;

        // Nếu chạm trúng đối tượng có Layer hoặc Component của Boss/Quái
        if (other.GetComponent<BossAI>() != null || other.CompareTag("Enemy"))
        {
            TriggerExplosion();
        }
    }

    void TriggerExplosion()
    {
        Debug.LogWarning("🔥 RESONANCE EXPLOSION! BỘC PHÁ ÁNH SÁNG HỔ PHÁCH DIỆN RỘNG!");

        // Tìm tất cả vật thể nằm trong bán kính vụ nổ
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

        foreach (Collider2D enemy in hitEnemies)
        {
            // KIỂM TRA NẾU CHẠM TRÚNG BOSS (HOẶC ẢO ẢNH)
            BossAI boss = enemy.GetComponent<BossAI>();
            if (boss != null)
            {
                boss.TakeDamage(50f); // Trừ 50 máu của Boss khi trúng vụ nổ
            }
        }

        // Xóa quả cầu hổ phách sau khi nổ thành công
        Destroy(gameObject);
    }

    // Vẽ vòng tròn đỏ trong cửa sổ Scene để bạn dễ căn chỉnh độ rộng vụ nổ
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}