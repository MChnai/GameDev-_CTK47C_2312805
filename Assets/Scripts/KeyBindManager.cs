using UnityEngine;

public class KeyBindManager : MonoBehaviour
{
    public static KeyBindManager Instance;

    // Các phím bấm mặc định ban đầu của game
    public KeyCode MoveLeftKey = KeyCode.A;
    public KeyCode MoveRightKey = KeyCode.D;
    public KeyCode JumpKey = KeyCode.W;
    public KeyCode DashKey = KeyCode.LeftShift;

    void Awake()
    {
        // Giữ bộ quản lý phím này không bị xóa khi chuyển sang Scene Gameplay
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}