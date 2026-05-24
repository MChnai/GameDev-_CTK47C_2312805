using UnityEngine;

public class KairiController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    private float moveX;
    private Rigidbody2D rb;
    private Animator anim;

    [HideInInspector] public float currentSpeed;

    [Header("Ground Check Settings (Neo Hierarchy)")]
    public Transform groundCheck;       // PHẢI kéo GameObject con "GroundCheckPoint" vào đây
    public float checkRadius = 0.3f;    // Bán kính vòng tròn quét đất
    public LayerMask groundLayer;       // Chọn Layer "Midground"
    private bool grounded = true;

    [Header("Combat Settings")]
    public float attackCooldown = 0.25f; // Thời gian tối thiểu giữa 2 lần nhận lệnh gốc
    public float comboResetTime = 0.8f;  // Thời gian tối đa để bấm đòn tiếp theo trước khi reset chuỗi

    private int comboCount = 0;
    private float lastAttackTime;
    private bool isAttacking = false;   // Biến cờ chặn đứng di chuyển khi đang chém dưới đất

    // --- BỔ SUNG CƠ CHẾ ĐỆM COMBO (BUFFER) MƯỢT MÀ ---
    private bool inputReceived = false;  // Nhận biết người chơi có bấm gối đòn hay không

    [Header("VFX Settings")]
    public GameObject purpleSlashPrefab;
    public GameObject amberPrefab;       // Đưa Prefab Amber vào Combat Settings gốc cho đồng bộ
    public Transform attackPoint;       // KHÔNG ĐƯỢC để trống (None) trong Inspector

    [Header("Dash Settings")]
    public float dashForce = 12f;
    private bool isDashing = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = moveSpeed;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        isDashing = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (groundCheck == null) Debug.LogError("[CẢNH BÁO] Bạn chưa kéo GameObject chân vào ô Ground Check!");
        if (attackPoint == null) Debug.LogError("[CẢNH BÁO] Bạn chưa kéo vị trí chém vào ô Attack Point!");
    }

    void Update()
    {
        if (isDashing) return;

        // 1. CƠ CHẾ KIỂM TRA CHẠM ĐẤT THÔNG MINH MỚI
        if (!grounded)
        {
            if (groundCheck != null)
            {
                bool hitGround = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
                if (hitGround && rb.linearVelocity.y <= 0.1f)
                {
                    grounded = true;
                    Debug.Log("<color=yellow>[GROUNDED]</color> Nhân vật đã tiếp đất an toàn. Khôi phục trạng thái.");
                }
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

        // --- CƠ CHẾ KIỂM SOÁT VÒNG LẶP HOẠT ẢNH JUMP ---
        if (anim != null)
        {
            anim.SetBool("isGrounded", grounded);

            if (!grounded)
            {
                AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Jump"))
                {
                    if (stateInfo.normalizedTime >= 0.8f)
                    {
                        anim.speed = 0f; // Khóa frame lơ lửng
                    }
                }
            }
            else
            {
                anim.speed = 1f; // Mở khóa khi chạm đất
            }
        }

        // 2. DI CHUYỂN NGANG VỚI PHÍM A / D (ĐÃ LOẠI BỎ ĐOẠN TRÙNG LẶP CODE CŨ)
        moveX = 0f;

        if (!isAttacking)
        {
            if (Input.GetKey(KeyCode.A)) moveX = -1f;
            if (Input.GetKey(KeyCode.D)) moveX = 1f;
        }
        else
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
            {
                Debug.LogWarning($"[CONSOLE CHECK] Bấm di chuyển thất bại! Do isAttacking = {isAttacking} | grounded = {grounded}");
            }
        }

        // Xử lý hoạt họa di chuyển và lật mặt hướng chuẩn
        if (moveX != 0)
        {
            if (grounded && anim != null) anim.SetBool("isRunning", true);

            if (moveX > 0) transform.localScale = new Vector3(-1, 1, 1);
            else if (moveX < 0) transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            if (anim != null) anim.SetBool("isRunning", false);
        }

        // 3. LOGIC PHÍM NHẢY W
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (grounded && !isAttacking)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                grounded = false;

                if (anim != null)
                {
                    anim.SetBool("isRunning", false);
                    anim.SetBool("isGrounded", false);
                    anim.SetTrigger("jump");
                }
                Debug.Log("<color=green>[JUMP SUCCESS]</color> Kairi đã cất cánh!");
            }
        }

        // 4. LOGIC ĐÒN CHÉM LIÊN HOÀN J (TỐI ƯU HÓA BUFFER)
        if (Input.GetKeyDown(KeyCode.J))
        {
            AttackCombo();
        }

        // Tự động reset chuỗi combo nếu ngừng bấm quá lâu hoặc hết thời gian chờ
        if (Time.time - lastAttackTime > comboResetTime)
        {
            if (comboCount != 0 || isAttacking)
            {
                Debug.Log("[COMBO TIMEOUT] Tự động giải phóng trạng thái và reset combo!");
                ResetComboEntirely();
            }
        }

        // 5. LOGIC PHÍM LƯỚT LEFT SHIFT
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing)
        {
            StartCoroutine(PerformDash());
        }
    }

    void FixedUpdate()
    {
        if (isDashing) return;
        rb.gravityScale = 1f;
        rb.linearVelocity = new Vector2(moveX * currentSpeed, rb.linearVelocity.y);
    }

    void AttackCombo()
    {
        if (attackPoint == null) return;

        // Nếu đang chém đòn trước đó mà người chơi bấm tiếp J
        if (isAttacking)
        {
            // Nếu đòn đánh hiện tại đã trôi qua thời gian Cooldown tối thiểu, ghi nhận đệm lệnh
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                inputReceived = true;
                Debug.Log("<color=orange>[COMBO BUFFER]</color> Đã ghi nhớ đòn đánh tiếp theo!");
            }
            return;
        }

        // Kích hoạt phát đòn đánh thực tế
        isAttacking = true;
        lastAttackTime = Time.time;
        comboCount++;

        if (comboCount > 4) comboCount = 1;

        Debug.Log($"<color=cyan>[COMBAT]</color> Kích hoạt đòn chém Combo số: {comboCount}");

        if (anim != null)
        {
            anim.SetInteger("comboCount", comboCount);
            anim.SetTrigger("attack");
        }

        // Tạo hiệu ứng VFX tương ứng
        if (comboCount <= 3)
        {
            SpawnSlashVFX(purpleSlashPrefab);
        }
        else if (comboCount == 4)
        {
            if (amberPrefab != null)
            {
                Instantiate(amberPrefab, attackPoint.position, transform.rotation);
            }
            comboCount = 0; // Đòn cuối reset bộ đếm
        }
    }

    // --- CỰC KỲ QUAN TRỌNG: GỌI HÀM NÀY QUA ANIMATION EVENT Ở ĐIỂM CHUYỂN COMBO (THƯỜNG Ở 70%-80% HOẠT ẢNH) ---
    public void CheckComboBuffer()
    {
        if (inputReceived)
        {
            // Nếu có đệm lệnh từ trước, giải phóng cờ để đánh tiếp luôn đòn sau mượt mà
            inputReceived = false;
            isAttacking = false;
            AttackCombo();
        }
    }

    // CỰC KỲ QUAN TRỌNG: Gọi hàm này ở FRAME CUỐI CÙNG của cả 4 hoạt ảnh Attack để đóng đòn hoàn toàn nếu người chơi dừng bấm
    public void ResetAttackStatus()
    {
        // Chỉ reset khi không có đệm lệnh nào đang chờ chạy tiếp
        if (!inputReceived)
        {
            isAttacking = false;
            inputReceived = false;
            if (anim != null) anim.SetInteger("comboCount", 0);
            Debug.Log("[ANIMATION EVENT] Chuỗi đòn đánh kết thúc an toàn.");
        }
    }

    private void ResetComboEntirely()
    {
        comboCount = 0;
        isAttacking = false;
        inputReceived = false;
        if (anim != null) anim.SetInteger("comboCount", 0);
    }

    void SpawnSlashVFX(GameObject vfxPrefab)
    {
        if (vfxPrefab != null && attackPoint != null)
        {
            Instantiate(vfxPrefab, attackPoint.position, transform.rotation);
        }
    }

    System.Collections.IEnumerator PerformDash()
    {
        isDashing = true;
        ResetComboEntirely(); // Hủy combo khi lướt né đòn
        if (anim != null) anim.SetBool("isDashing", true);

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        float facingDirection = transform.localScale.x;
        // Chú ý: Đảo dấu lực lướt tương ứng với localScale ngược hướng của bạn
        rb.linearVelocity = new Vector2(-facingDirection * dashForce, 0f);

        yield return new WaitForSeconds(0.2f);

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = originalGravity;

        if (anim != null) anim.SetBool("isDashing", false);
        isDashing = false;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }
    }
}