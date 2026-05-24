using UnityEngine;
public class DestroyVFX : MonoBehaviour
{
    void Start() { Destroy(gameObject, 0.2f); } // Tự hủy sau 0.2 giây
}