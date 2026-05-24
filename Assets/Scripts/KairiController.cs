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

    [Header("Double Jump Settings (MỚI)")]
    public int maxJumps = 2;            // Số lần nhảy tối đa (2 tức là nhảy đôi)
    private int jumpsRemaining;         // Số lần nhảy còn lại

    [Header("Combat Settings")]
    public float attackCooldown = 0.25f; // Thời gian tối thiểu giữa 2 lần nhận lệnh gốc
    public float comboResetTime = 0.8f;  // Thời gian tối đa để bấm đòn tiếp theo trước khi reset chuỗi

    private int comboCount = 0;
    private float lastAttackTime;
    private bool isAttacking = false;   // Biến cờ chặn đứng di chuyển khi đang chém dưới đất

    // --- CƠ CHẾ KIỂM SOÁT ĐỆM COMBO (BUFFER) MƯỢT MÀ ---
    private bool inputReceived = false;  // Nhận biết người chơi có bấm gối đòn hay không

    [Header("VFX Settings")]
    public GameObject purpleSlashPrefab;
    public GameObject amberPrefab;       // Đưa Prefab Amber vào Combat Settings gốc cho đồng bộ
    public Transform attackPoint;       // KHÔNG ĐƯỢC để trống (None) trong Inspector

    [Header("Combat Sát Thương")]
    public float attackRange = 1.8f;     // Tầm chém xa của lưỡi kiếm Kairi
    public LayerMask enemyLayer;        // Layer của Boss (Chọn Layer "Enemy")
    public float attackDamage = 25f;     // Lượng sát thương gây ra mỗi cú chém

    [Header("Dash Settings")]
    public float dashForce = 12f;
    private bool isDashing = false;

    [Header("Air Attack Settings")]
    public float airAttackGravity = 50f;     // Tăng trọng lực cực đại để lao xuống thật nhanh
    public float airAttackDownForce = 15f;   // Lực ép lao thẳng xuống dưới theo trục Y
    private bool isAirAttacking = false;     // Biến cờ kiểm tra xem có đang bổ củi từ trên không hay không

    [Tooltip("Đòn 1 = Index 0, Đòn 2 = Index 1, Đòn 3 = Index 2, Đòn 4 (Air) = Index 3")]
    public float[] comboDamages = new float[4] { 20f, 25f, 40f, 60f }; // Mảng chứa sát thương riêng cho từng đòn

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

        jumpsRemaining = maxJumps; // Khởi tạo số lần nhảy ban đầu
    }

    void Update()
    {
        if (isDashing) return;

        // 1. CƠ CHẾ KIỂM TRA CHẠM ĐẤT THÔNG MINH MỚI (Cập nhật để nhận diện LandAirAttack)
        if (!grounded)
        {
            if (groundCheck != null)
            {
                bool hitGround = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
                if (hitGround && rb.linearVelocity.y <= 0.1f)
                {
                    grounded = true;
                    jumpsRemaining = maxJumps; // RESET số lần nhảy khi chạm đất thành công!

                    // Nếu đang trong trạng thái lao xuống chém (Air Attack) mà chạm đất
                    if (isAirAttacking)
                    {
                        LandAirAttack();
                    }

                    Debug.Log("<color=yellow>[GROUNDED]</color> Nhân vật đã tiếp đất an toàn. Khôi phục trạng thái và lượt nhảy.");
                }
            }
        }
        else
        {
            if (rb.linearVelocity.y < -1f && groundCheck != null)
            {
                bool hitGround = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
                if (!hitGround)
                {
                    grounded = false;
                    // Nếu nhân vật tự rơi khỏi rìa block (không phải chủ động bấm nhảy), trừ bớt 1 lượt nhảy đầu dưới đất
                    if (jumpsRemaining == maxJumps)
                    {
                        jumpsRemaining--;
                    }
                }
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

        // 2. DI CHUYỂN NGANG VỚI PHÍM A / D
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

        // 3. LOGIC PHÍM NHẢY W (Cải tiến hỗ trợ Nhảy Đôi - Double Jump)
        if (Input.GetKeyDown(KeyCode.W))
        {
            // Trường hợp 1: Nhảy từ mặt đất lên
            if (grounded && !isAttacking)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                grounded = false;
                jumpsRemaining--; // Tiêu hao 1 lượt nhảy

                if (anim != null)
                {
                    anim.SetBool("isRunning", false);
                    anim.SetBool("isGrounded", false);
                    anim.SetTrigger("jump");
                }
                Debug.Log("<color=green>[JUMP 1 SUCCESS]</color> Kairi đã cất cánh từ mặt đất!");
            }
            // Trường hợp 2: Đang ở trên không và còn lượt nhảy đôi (Không kích hoạt khi đang bổ củi)
            else if (!grounded && jumpsRemaining > 0 && !isAirAttacking)
            {
                // Reset lại vận tốc Y và đẩy lên lần nữa để cú nhảy đạt đủ độ cao đồng đều
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpsRemaining--; // Tiêu hao lượt nhảy đôi cuối cùng

                if (anim != null)
                {
                    anim.speed = 1f; // Mở khóa tốc độ animator nếu frame lơ lửng cũ đang bị đóng băng (speed = 0)
                    anim.ResetTrigger("jump"); // Xóa trigger cũ đề phòng bị dồn lệnh
                    anim.SetTrigger("jump");   // Kích hoạt lại để phát lại hoạt ảnh nhảy
                }
                Debug.Log("<color=cyan>[DOUBLE JUMP SUCCESS]</color> Kairi đã kích hoạt nhảy đôi trên không trung!");
            }
        }

        // 4. LOGIC ĐÒN TẤN CÔNG BẰNG CHUỘT TRÁI (Chia làm 2 trường hợp: Đất vs Không)
        if (Input.GetMouseButtonDown(0))
        {
            if (grounded)
            {
                // Ở dưới đất: Chém Combo liên hoàn đòn 1 -> 2 -> 3
                AttackCombo();
            }
            else if (!grounded && !isAirAttacking && !isDashing)
            {
                // Ở trên không: Kích hoạt Không kích bổ củi lao thẳng xuống (Đòn số 4)
                StartAirAttack();
            }
        }

        // Tự động reset chuỗi combo nếu ngừng bấm quá lâu hoặc hết thời gian chờ
        if (Time.time - lastAttackTime > comboResetTime)
        {
            if (comboCount != 0 || isAttacking)
            {
                // Chỉ tự động reset nếu không phải đang trong trạng thái Không Kích lao xuống
                if (!isAirAttacking)
                {
                    Debug.Log("[COMBO TIMEOUT] Tự động giải phóng trạng thái và reset combo!");
                    ResetComboEntirely();
                }
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

        // TỐI ƯU: Chỉ đặt gravityScale = 1f nếu KHÔNG phải đang thực hiện đòn bổ củi từ trên không.
        // Điều này ngăn chặn việc FixedUpdate ghi đè và làm mất lực rơi mạnh (airAttackGravity) của đòn đánh số 4.
        if (!isAirAttacking)
        {
            rb.gravityScale = 1f;
        }

        rb.linearVelocity = new Vector2(moveX * currentSpeed, rb.linearVelocity.y);
    }

    void AttackCombo()
    {
        if (attackPoint == null) return;

        // Nếu đang chém đòn trước đó mà người chơi bấm tiếp chuột
        if (isAttacking)
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                inputReceived = true;
                Debug.Log("<color=orange>[COMBO BUFFER]</color> Đã ghi nhớ đòn đánh tiếp theo!");
            }
            return;
        }

        isAttacking = true;
        lastAttackTime = Time.time;
        comboCount++;

        if (comboCount > 3) comboCount = 1; // Giới hạn chuỗi chém đất chỉ từ đòn 1 đến 3

        Debug.Log($"<color=cyan>[COMBAT]</color> Kích hoạt đòn chém Combo số: {comboCount}");

        if (anim != null)
        {
            anim.SetInteger("comboCount", comboCount);
            anim.SetTrigger("attack");
        }

        if (comboCount >= 1 && comboCount <= 3 && comboCount <= comboDamages.Length)
        {
            float currentDamage = comboDamages[comboCount - 1];
            CheckHitDamage(currentDamage);
        }

        if (comboCount <= 3)
        {
            SpawnSlashVFX(purpleSlashPrefab);
        }

        if (comboCount <= 3)
        {
            SpawnSlashVFX(purpleSlashPrefab);
        }
    }

    // Sửa hàm: Thêm tham số đầu vào 'damage' để xử lý sát thương riêng biệt
    void CheckHitDamage(float damage)
    {
        // Quét tất cả các Collider nằm trong tầm chém thuộc layer "Enemy"
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            // Tìm script BossAI gắn trên đối tượng bị chém (hoặc cha của nó)
            BossAI boss = enemy.GetComponent<BossAI>();
            if (boss == null)
            {
                boss = enemy.GetComponentInParent<BossAI>();
            }

            // Nếu tìm thấy BossAI, gây sát thương lên nó theo lượng damage được truyền vào
            if (boss != null)
            {
                boss.TakeDamage(damage);
                Debug.Log($"<color=lime>[HIT SUCCESS]</color> Đòn đánh gây: {damage} damage lên Boss!");
            }
        }
    }

    public void CheckComboBuffer()
    {
        if (inputReceived)
        {
            inputReceived = false;
            isAttacking = false;
            AttackCombo();
        }
    }

    public void ResetAttackStatus()
    {
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
        ResetComboEntirely();
        if (anim != null) anim.SetBool("isDashing", true);

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        float facingDirection = transform.localScale.x;
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

        if (attackPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }

    void StartAirAttack()
    {
        isAirAttacking = true;
        isAttacking = true;
        comboCount = 4;

        Debug.Log("<color=red>[AIR ATTACK]</color> Kairi kích hoạt đòn bổ củi từ trên không!");

        if (anim != null)
        {
            anim.speed = 1f; // Trả lại tốc độ hoạt ảnh gốc cho đòn chém
            anim.SetInteger("comboCount", 4);
            anim.SetTrigger("attack");
        }

        rb.gravityScale = airAttackGravity;
        rb.linearVelocity = new Vector2(0f, -airAttackDownForce);
    }

    void LandAirAttack()
    {
        isAirAttacking = false;
        rb.gravityScale = 1f;

        Debug.Log("<color=orange>[AIR ATTACK LAND]</color> Chạm đất! Tạo chấn động vật lý và VFX.");

        if (comboDamages.Length >= 4)
        {
            float airDamage = comboDamages[3]; // Lấy sát thương đòn 4
            CheckHitDamage(airDamage);
        }
        else
        {
            CheckHitDamage(50f); // Sát thương mặc định dự phòng nếu bạn quên điền mảng ngoài Inspector
        }

        if (amberPrefab != null && attackPoint != null)
        {
            Instantiate(amberPrefab, attackPoint.position, transform.rotation);
        }

        Invoke("ResetAttackStatus", 0.15f);
    }
}