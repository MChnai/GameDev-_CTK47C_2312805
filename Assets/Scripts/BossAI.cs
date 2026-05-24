using UnityEngine;

public class BossAI : MonoBehaviour
{
    public enum BossState { Idle, Chasing, Attacking, Phase3_QTE, GetAttack }

    [Header("AI State")]
    public BossState currentState = BossState.Idle;

    [Header("Boss Stats")]
    public float maxHP = 500f;
    public float currentHP;
    public bool isRealBoss = true;

    [Header("References")]
    public Transform kairi;
    public GameObject[] allClones;

    [Header("Movement & Combat AI Settings")]
    public float moveSpeed = 3f;
    public float detectionRadius = 12f;
    public float attackRadius = 2.5f;
    public float attackRate = 1.5f;
    private float nextAttackTime = 0f;
    private bool isAttacking = false; // BIẾN CỜ: Ngăn chặn spam đòn đánh và khóa di chuyển khi đang vung kiếm

    [Header("Ground Check Settings")]
    public Transform groundCheck;
    public float checkRadius = 0.3f;
    public LayerMask groundLayer;
    private bool grounded = true;

    [Header("Phase Settings")]
    public float pullForce = 15f;
    private int currentPhase = 1;

    [Header("Phase 3 (QTE) Settings")]
    private bool isQTEActive = false;
    private int qteStep = 0;
    private float qteTimer = 0f;
    public float qteTimeLimit = 2.5f;

    private Rigidbody2D rb;
    private Animator anim;

    [Header("Hurt Settings")]
    public float hurtDuration = 0.4f; // Thời gian Boss bị khựng khi dính đòn
    private float hurtTimer;

    void Start()
    {
        currentHP = maxHP;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        if (groundCheck == null) Debug.LogError($"[CẢNH BÁO] Bạn chưa kéo ô Ground Check!");
    }

    void Update()
    {
        if (kairi == null) return;

        // 1. ƯU TIÊN SỐ 1: VÒNG LẶP QTE KHI ĐANG HOẠT ĐỘNG
        if (isQTEActive)
        {
            HandleQTE();
            return;
        }

        // 2. ƯU TIÊN SỐ 2: ĐÓNG BĂNG HOÀN TOÀN KHI ĐANG BỊ TRÚNG ĐÒN (Đã đưa lên trên đầu)
        if (currentState == BossState.GetAttack)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); // Ép Boss đứng im tại chỗ

            if (Time.time - hurtTimer >= hurtDuration)
            {
                currentState = BossState.Chasing; // Hết thời gian khựng, quay lại đuổi theo
                isAttacking = false; // Giải phóng cờ chặn hành động
                Debug.Log("[BOSSAI] Hết khựng đòn, quay lại trạng thái chiến đấu.");
            }
            return; // Thoát sớm, ngăn chặn tuyệt đối việc tính khoảng cách và ép đè hoạt ảnh Idle/Run
        }

        // --- CƠ CHẾ KIỂM TRA CHẠM ĐẤT ---
        HandleGroundCheck();

        // --- QUẢN LÝ TRẠNG THÁI AI (CHỈ CẬP NHẬT KHI KHÔNG TRONG HOẠT ẢNH CHÉM VÀ KHÔNG BỊ TRÚNG ĐÒN) ---
        if (!isAttacking)
        {
            float distanceToKairi = Vector3.Distance(transform.position, kairi.position);

            if (distanceToKairi <= attackRadius)
            {
                currentState = BossState.Attacking;
            }
            else if (distanceToKairi <= detectionRadius || currentPhase == 2)
            {
                currentState = BossState.Chasing;
            }
            else
            {
                currentState = BossState.Idle;
            }
        }

        HandleOrientationAndAnimations();
    }

    void FixedUpdate()
    {
        if (rb == null || isQTEActive || kairi == null || currentState == BossState.GetAttack)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // Nếu đang bận tấn công, ép vận tốc trục X về 0 để đứng im vung kiếm
        if (isAttacking)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // Di chuyển đuổi theo Kairi khi ở trạng thái Chasing
        if (currentState == BossState.Chasing && grounded)
        {
            float moveDirection = Mathf.Sign(kairi.position.x - transform.position.x);
            rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    void HandleOrientationAndAnimations()
    {
        // 1. LUÔN XOAY MẶT VỀ PHÍA KAIRI (Trừ khi đang bận QTE hoặc bị trúng đòn)
        if (currentState != BossState.GetAttack)
        {
            if (kairi.position.x > transform.position.x)
                transform.localScale = new Vector3(-1, 1, 1);
            else
                transform.localScale = new Vector3(1, 1, 1);
        }

        // 2. LOGIC ÉP HOẠT ẢNH RUN / IDLE THEO TRẠNG THÁI AI THỰC TẾ
        if (anim != null)
        {
            bool shouldRun = (currentState == BossState.Chasing && !isAttacking && grounded);

            if (shouldRun)
            {
                anim.SetBool("isBossRun", true);
                anim.SetBool("isBossIdle", false);
            }
            else
            {
                anim.SetBool("isBossRun", false);
                anim.SetBool("isBossIdle", true);
            }
        }

        // 3. KÍCH HOẠT TẤN CÔNG
        float distanceToKairiNow = Vector3.Distance(transform.position, kairi.position);
        if (distanceToKairiNow <= attackRadius && Time.time >= nextAttackTime && grounded && !isAttacking)
        {
            ExecuteAttack();
        }
    }

    void ExecuteAttack()
    {
        isAttacking = true;
        currentState = BossState.Attacking;
        nextAttackTime = Time.time + attackRate;

        Debug.LogWarning($"<color=red>[BOSS ATTACK]</color> Boss kích hoạt hoạt ảnh vung kiếm!");

        if (anim != null)
        {
            anim.SetBool("isBossRun", false);
            anim.SetBool("isBossIdle", false);
            anim.SetTrigger("bossAttack");
        }
    }

    public void ResetBossAttackStatus()
    {
        isAttacking = false;
        currentState = BossState.Idle;
        Debug.Log("[ANIMATION EVENT] Boss đã chém xong! Giải phóng khóa di chuyển.");
    }

    // --- HÀM TAKE DAMAGE ĐÃ ĐƯỢC ĐỒNG BỘ 100% SANG THANH MÁU UI ---
    public void TakeDamage(float damage)
    {
        // 1. Xử lý logic Ảo ảnh ở Phase 2
        if (currentPhase == 2 && !isRealBoss)
        {
            Debug.Log("Chém nhầm Ảo ảnh rồi!");
            Destroy(gameObject);
            return;
        }

        currentHP -= damage;
        Debug.Log($"<color=red>[BOSS HP]: </color> {currentHP} | Nhận sát thương: {damage}");

        // BỔ SUNG THẦN THÁNH: Báo cho thanh máu UI biết để tụt theo tương ứng
        if (BossHealthController.Instance != null && isRealBoss)
        {
            BossHealthController.Instance.SyncBossDamage(damage, currentHP);
        }

        if (currentHP <= 0)
        {
            currentHP = 0;
            if (currentPhase == 2 && isRealBoss)
            {
                StartPhase3QTE();
            }
            else
            {
                Debug.Log("[BOSS] Đã bị tiêu diệt hoàn toàn! Bắt đầu hiệu ứng tan biến...");

                if (BossHealthController.Instance != null && isRealBoss)
                {
                    BossHealthController.Instance.TriggerBossDeath();
                }
                if (BossHealthController.Instance != null)
                {
                    BossHealthController.Instance.HideHealthBar();
                }

                // GỌI HÀM TAN BIẾN Ở ĐÂY
                StartCoroutine(FadeOutAndDestroy());
            }
            return;
        }

        // 2. Kích hoạt trạng thái dính đòn khựng lại (Chỉ chạy khi Boss còn sống)
        currentState = BossState.GetAttack;
        hurtTimer = Time.time;
        isAttacking = true; // Khóa tạm thời không cho tự động chuyển trạng thái tấn công

        if (anim != null)
        {
            anim.SetBool("isBossRun", false);
            anim.SetBool("isBossIdle", false);
            anim.SetTrigger("getHit");
        }

        // Kiểm tra chuyển đổi Phase 2 dựa trên logic của BossAI
        if (currentHP <= 400f && currentPhase == 1)
        {
            StartPhase2();

            // Lệnh yêu cầu UI chuyển sang giao diện Phase 2
            if (BossHealthController.Instance != null && isRealBoss)
            {
                BossHealthController.Instance.NotifyPhase2Transition();
            }
        }
    }

    void StartPhase2()
    {
        currentPhase = 2;
        pullForce = 0;
        Debug.LogWarning(">>>> PHÁT ĐỘNG GIAI ĐOẠN 2: BOSS ẨN THÂN, PHÂN THÂN CHI THUẬT! <<<<");

        foreach (GameObject clone in allClones)
        {
            if (clone != null) clone.SetActive(true);
        }
    }

    void StartPhase3QTE()
    {
        currentPhase = 3;
        isQTEActive = true;
        qteStep = 1;
        qteTimer = qteTimeLimit;

        if (rb != null) rb.linearVelocity = Vector2.zero;

        Debug.LogWarning(">>>> PHÁT ĐỘNG GIAI ĐOẠN 3: BẢN HÒA TẤU CUỐI CÙNG! CHUẨN BỊ BẤM QTE! <<<<");
        Debug.Log("<color=cyan>NÚT 1: Nhấn [SPACE] để Kairi vung kiếm chém!</color>");
    }

    void HandleGroundCheck()
    {
        if (!grounded)
        {
            if (groundCheck != null)
            {
                bool hitGround = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
                if (hitGround && rb.linearVelocity.y <= 0.1f) grounded = true;
            }
        }
        else
        {
            if (rb.linearVelocity.y < -1f && groundCheck != null)
            {
                bool hitGround = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
                if (!hitGround) grounded = false;
            }
        }
        if (anim != null) anim.SetBool("isGrounded", grounded);
        if (currentPhase == 1 && !isAttacking) HandlePhase1Pull();
    }

    void HandlePhase1Pull()
    {
        if (isRealBoss)
        {
            Rigidbody2D kairiRb = kairi.GetComponent<Rigidbody2D>();
            if (kairiRb != null)
            {
                Vector3 targetPullPosition = transform.position + new Vector3(6f, 4f, 0f);
                Vector2 pullDirection = (targetPullPosition - kairi.position).normalized;
                kairiRb.AddForce(pullDirection * pullForce * Time.deltaTime, ForceMode2D.Force);
            }
        }
    }

    void HandleQTE()
    {
        qteTimer -= Time.deltaTime;

        if (qteTimer <= 0)
        {
            Debug.LogError("Bấm trượt nhịp QTE! Đang đặt lại thời gian.");
            qteTimer = qteTimeLimit;
        }

        if (qteStep == 1 && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.LogWarning("-> KAIRI CHÉM TOẠC LỚP VỎ PHÒNG THỦ! Nhịp cuối cùng:");
            Debug.Log("<color=magenta>NÚT 2: Tiếp tục nhấn [SPACE] một lần nữa để kết liễu hoàn toàn!</color>");
            qteStep = 2;
            qteTimer = qteTimeLimit;
        }
        else if (qteStep == 2 && Input.GetKeyDown(KeyCode.Space))
        {
            EndGameVictory();
        }
    }

    void EndGameVictory()
    {
        isQTEActive = false;
        Debug.LogWarning("=========================================");
        Debug.LogWarning("CHIẾN THẮNG! LỚP NHỰA ĐEN TAN VỠ, KAIRI THU KIẾM!");
        Debug.LogWarning("Hết Demo - Bạn đã hoàn thành bài tập lớn xuất sắc!");
        Debug.LogWarning("=========================================");
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }
    }
    // COROUTINE GIÚP BOSS MỜ DẦN VÀ TAN BIẾN
    System.Collections.IEnumerator FadeOutAndDestroy()
    {
        // 1. Khóa toàn bộ vật lý và hành động để Boss không di chuyển/trúng đòn nữa
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic; // Biến thành vô hình với vật lý
        }

        // Tắt Collider để Kairi không chém trúng xác Boss nữa
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Tắt cập nhật logic AI
        this.enabled = false;

        // 2. Lấy thành phần SpriteRenderer để chỉnh độ mờ (Alpha)
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            float fadeDuration = 1.5f; // Thời gian tan biến (1.5 giây), bạn có thể chỉnh tùy ý
            float currentTime = 0f;

            while (currentTime < fadeDuration)
            {
                currentTime += Time.deltaTime;
                // Tính toán tỷ lệ mờ dần từ 1 về 0
                float alpha = Mathf.Lerp(1f, 0f, currentTime / fadeDuration);

                // Áp dụng màu mới với alpha giảm dần cho Boss
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

                yield return null; // Chờ tới frame tiếp theo
            }
        }

        // 3. Sau khi đã tan biến hoàn toàn (Alpha = 0), tiến hành xóa Boss khỏi màn chơi
        Debug.Log("[BOSS] Đã tan biến hoàn toàn. Xóa GameObject.");
        Destroy(gameObject);
    }
    // Tự động chạy khi toàn bộ hoặc một phần cơ thể Boss lọt vào màn hình Camera
    private void OnBecameVisible()
    {
        // Chỉ kích hoạt khi Boss còn sống và không ở trạng thái QTE kết liễu
        if (currentHP > 0 && currentPhase != 3 && BossHealthController.Instance != null)
        {
            BossHealthController.Instance.ShowHealthBar();
        }
    }

    // Tự động chạy khi Boss đi hoàn toàn ra ngoài rìa màn hình Camera
    private void OnBecameInvisible()
    {
        if (BossHealthController.Instance != null)
        {
            BossHealthController.Instance.HideHealthBar();
        }
    }
}