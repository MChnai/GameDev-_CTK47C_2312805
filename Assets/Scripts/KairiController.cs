using UnityEngine;

public class KairiController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    private float moveX;
    private Rigidbody2D rb;

    [HideInInspector] public float currentSpeed;

    [Header("Ground Check Settings")]
    public Transform groundCheck;    // Kéo GameObject con "GroundCheckPoint" vào đây
    public float checkRadius = 0.3f; // Bán kính vòng tròn quét đất
    public LayerMask groundLayer;    // Chọn Layer nền đất (ví dụ: "Midground" hoặc "Ground")
    private bool grounded = true;

    [Header("Double Jump Settings")]
    public int maxJumps = 2;         // Số lần nhảy tối đa (2 = Nhảy đôi)
    private int jumpsRemaining;      // Số lần nhảy còn lại

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = moveSpeed;

        // Khóa xoay trục Z để nhân vật không bị ngã nghiêng khi va chạm
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        // Khởi tạo vận tốc ban đầu
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (groundCheck == null) 
            Debug.LogError("[CẢNH BÁO] Bạn chưa kéo GameObject chân vào ô Ground Check!");

        jumpsRemaining = maxJumps; // Khởi tạo số lần nhảy ban đầu
    }

    void Update()
    {
        // 1. CƠ CHẾ KIỂM TRA CHẠM ĐẤT
        if (!grounded)
        {
            if (groundCheck != null)
            {
                bool hitGround = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
                // Nếu chạm đất và nhân vật không còn xu hướng bay lên cao
                if (hitGround && rb.linearVelocity.y <= 0.1f)
                {
                    grounded = true;
                    jumpsRemaining = maxJumps; // RESET số lần nhảy khi chạm đất thành công!
                    Debug.Log("<color=yellow>[GROUNDED]</color> Nhân vật đã tiếp đất an toàn. Khôi phục lượt nhảy.");
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
                    // Nếu nhân vật tự đi/rơi khỏi rìa block, trừ bớt 1 lượt nhảy đầu dưới đất
                    if (jumpsRemaining == maxJumps)
                    {
                        jumpsRemaining--;
                    }
                }
            }
        }

        // 2. NHẬN INPUT DI CHUYỂN NGANG (Hỗ trợ KeyBindManager hoặc mặc định A / D)
        moveX = 0f;
        KeyCode left = (KeyBindManager.Instance != null) ? KeyBindManager.Instance.MoveLeftKey : KeyCode.A;
        KeyCode right = (KeyBindManager.Instance != null) ? KeyBindManager.Instance.MoveRightKey : KeyCode.D;

        if (Input.GetKey(left)) moveX = -1f;
        if (Input.GetKey(right)) moveX = 1f;

        // Xử lý lật mặt (Flip Sprite) dựa trên hướng di chuyển
        if (moveX > 0) transform.localScale = new Vector3(-1, 1, 1);
        else if (moveX < 0) transform.localScale = new Vector3(1, 1, 1);

        // 3. LOGIC PHÍM NHẢY W (Hỗ trợ Nhảy Đôi - Double Jump)
        KeyCode jump = (KeyBindManager.Instance != null) ? KeyBindManager.Instance.JumpKey : KeyCode.W;
        if (Input.GetKeyDown(jump))
        {
            // Trường hợp 1: Nhảy từ mặt đất lên
            if (grounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                grounded = false;
                jumpsRemaining--; // Tiêu hao 1 lượt nhảy
                Debug.Log("<color=green>[JUMP 1 SUCCESS]</color> Kairi đã cất cánh từ mặt đất!");
            }
            // Trường hợp 2: Đang ở trên không và còn lượt nhảy đôi
            else if (!grounded && jumpsRemaining > 0)
            {
                // Reset lại vận tốc Y trước khi đẩy lên để cú nhảy thứ 2 đạt độ cao đồng đều
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpsRemaining--; // Tiêu hao lượt nhảy đôi cuối cùng
                Debug.Log("<color=cyan>[DOUBLE JUMP SUCCESS]</color> Kairi đã kích hoạt nhảy đôi trên không trung!");
            }
        }
    }

    void FixedUpdate()
    {
        // Thực hiện di chuyển vật lý tại đây giúp nhân vật không bị giật lag
        rb.linearVelocity = new Vector2(moveX * currentSpeed, rb.linearVelocity.y);
    }
}