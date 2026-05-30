using UnityEngine;

namespace RoboticsProject.Controllers.Inventory
{
    /// <summary>
    /// Hiệu ứng Billboard (luôn quay về phía Camera) và nhấp nháy (Shining) cho các vật phẩm nhặt được.
    /// Không cần Animation Controller, giúp tối ưu hiệu năng và dễ tùy chỉnh qua Inspector.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ShiningBillboard : MonoBehaviour
    {
        [Header("Shining (Alpha) Settings")]
        [Tooltip("Độ mờ tối thiểu")]
        [Range(0f, 1f)] [SerializeField] private float minAlpha = 0.2f;
        
        [Tooltip("Độ sáng tối đa")]
        [Range(0f, 1f)] [SerializeField] private float maxAlpha = 1.0f;
        
        [Tooltip("Tốc độ nhấp nháy")]
        [SerializeField] private float shineSpeed = 3.0f;

        [Header("Pulse (Scale) Settings")]
        [Tooltip("Có cho phép co giãn nhẹ theo nhịp nhấp nháy không?")]
        [SerializeField] private bool useScalePulse = true;
        
        [Tooltip("Hệ số Scale nhỏ nhất")]
        [SerializeField] private float minScaleMultiplier = 0.9f;
        
        [Tooltip("Hệ số Scale lớn nhất")]
        [SerializeField] private float maxScaleMultiplier = 1.1f;

        private SpriteRenderer spriteRenderer;
        private Vector3 originalScale;
        private Camera mainCamera;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            originalScale = transform.localScale;
        }

        private void Start()
        {
            mainCamera = Camera.main;
        }

        private void Update()
        {
            // 1. Billboard Effect: Mặt chính diện luôn hướng về phía Camera chính của Player
            if (mainCamera == null)
            {
                mainCamera = Camera.main; // Phòng trường hợp camera bị đổi hoặc khởi tạo trễ
            }

            if (mainCamera != null)
            {
                // Quay hoàn toàn về phía camera (phù hợp cho các hiệu ứng glow/sprite tròn)
                transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                                 mainCamera.transform.rotation * Vector3.up);
            }

            // 2. Nhấp nháy mượt mà sử dụng hàm Sin
            float sinWave = Mathf.Sin(Time.time * shineSpeed);
            float timeFactor = (sinWave + 1.0f) / 2.0f; // Chuẩn hóa từ [-1, 1] sang [0, 1]

            // Điều chỉnh độ mờ (Alpha) của SpriteRenderer
            if (spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(minAlpha, maxAlpha, timeFactor);
                spriteRenderer.color = color;
            }

            // Điều chỉnh kích thước (Scale) co giãn nhẹ tạo nhịp điệu sinh động
            if (useScalePulse)
            {
                float scaleFactor = Mathf.Lerp(minScaleMultiplier, maxScaleMultiplier, timeFactor);
                transform.localScale = originalScale * scaleFactor;
            }
        }
    }
}
