using UnityEngine;

public class BossAI : MonoBehaviour
{
    public enum BossState { Idle, Chasing, Attacking, Phase3_QTE }
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
        if (kairi == null || isQTEActive) return;

        // --- CƠ CHẾ KIỂM TRA CHẠM ĐẤT ---
        HandleGroundCheck();

        // --- QUẢN LÝ TRẠNG THÁI AI (CHỈ CẬP NHẬT KHI KHÔNG ĐANG CHÉM) ---
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
        if (rb == null || isQTEActive || kairi == null)
        {
            if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // Nếu đang bận tấn công (isAttacking = true), ép vận tốc trục X về 0 để đứng im vung kiếm
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
        // 1. LUÔN XOAY MẶT VỀ PHÍA KAIRI (Trừ khi đang bận QTE)
        if (kairi.position.x > transform.position.x)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        // 2. LOGIC ÉP HOẠT ẢNH RUN THÔNG MINH
        if (anim != null)
        {
            float distanceToKairi = Vector3.Distance(transform.position, kairi.position);

            // Kiểm tra xem Boss có đang thực sự di chuyển theo trục X hay không (vận tốc tuyệt đối > 0.2f)
            // Điều này đúng cho cả lúc tự chạy lẫn lúc bị lực hút (PullForce) kéo lê đi trên sàn.
            bool isMovingHorizontally = Mathf.Abs(rb.linearVelocity.x) > 0.2f;

            // ĐIỀU KIỆN BẬT RUN: 
            // Đang ở trạng thái đuổi theo (Chasing) HOẶC (Đang ngoài tầm đánh VÀ đang bị lực kéo di chuyển trục X)
            bool shouldRun = (currentState == BossState.Chasing && grounded) ||
                             (distanceToKairi > attackRadius && isMovingHorizontally);

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
        if (currentState == BossState.Attacking && Time.time >= nextAttackTime && grounded)
        {
            ExecuteAttack();
        }
    }

    void ExecuteAttack()
    {
        isAttacking = true; // Khóa trạng thái lại
        nextAttackTime = Time.time + attackRate;

        Debug.LogWarning($"<color=red>[BOSS ATTACK]</color> Boss kích hoạt hoạt ảnh vung kiếm!");

        if (anim != null)
        {
            anim.SetTrigger("bossAttack");
        }
    }

    // CỰC KỲ QUAN TRỌNG: Hãy gọi hàm này thông qua Animation Event ở cuối Animation "bossAttack"
    // Giống hệt hàm ResetAttackStatus() bên KairiController của bạn!
    public void ResetBossAttackStatus()
    {
        isAttacking = false;
        currentState = BossState.Idle; // Trả về Idle để tính toán lại khoảng cách ở khung hình sau
        Debug.Log("[ANIMATION EVENT] Boss đã chém xong! Giải phóng khóa di chuyển.");
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

    public void TakeDamage(float damage)
    {
        if (currentPhase == 2 && !isRealBoss)
        {
            Debug.Log("Chém nhầm Ảo ảnh rồi!");
            Destroy(gameObject);
            return;
        }

        currentHP -= damage;
        Debug.Log("<color=red>Boss HP: </color>" + currentHP);

        if (currentHP <= 400f && currentPhase == 1) StartPhase2();
        if (currentHP <= 0 && currentPhase == 2 && isRealBoss) StartPhase3QTE();
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

        Debug.LogWarning(">>>> PHÁT ĐỘNG GIAI ĐOẠN 3: BẢN HÒA TẤU CUỐI CÙNG! CHUẨN BỊ BẤM QTE! <<<<");
        Debug.Log("<color=cyan>NÚT 1: Nhấn [SPACE] để Kairi vung kiếm chém!</color>");
    }

    // Vẽ vòng tròn kiểm tra chạm đất trong Scene để dễ debug
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }
    }
}