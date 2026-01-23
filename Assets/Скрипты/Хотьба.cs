using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Хотьба : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 5f;

    [Tooltip("Если хотите ограничить движение только объектами с этим tag (опционально).")]
    [SerializeField] private string requiredTag = "Player";

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Tooltip("Триггер начала движения (например: 'Идти' или 'ХодьбаStart').")]
    [SerializeField] private string moveStartTrigger = "Идти";

    [Tooltip("Триггер остановки/перехода в idle (например: 'Ходьба' или 'Idle').")]
    [SerializeField] private string stopTrigger = "Ходьба";

    [Header("Flip")]
    [Tooltip("Если true — флип делаем через localScale.x (классический Flip по X).")]
    [SerializeField] private bool flipByScaleX = true;

    private Rigidbody2D rb;
    private Vector2 input;
    private bool isMoving;
    private int lastFaceX = 1; // 1 = вправо, -1 = влево

    private void Awake()
    {
        if (!string.IsNullOrEmpty(requiredTag) && !CompareTag(requiredTag))
            Debug.LogWarning($"{name}: Tag объекта не '{requiredTag}'. Скрипт всё равно работает, но проверьте настройку.");

        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Изначально смотрит вправо (положительный X)
        if (flipByScaleX)
        {
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x);
            transform.localScale = s;
        }
    }

    private void Update()
    {
        // Считываем input (WASD + стрелки, если оси настроены стандартно)
        float x = 0f;
        float y = 0f;

        if (Input.GetKey(KeyCode.A)) x -= 1f;
        if (Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.W)) y += 1f;
        if (Input.GetKey(KeyCode.S)) y -= 1f;

        input = new Vector2(x, y);
        if (input.sqrMagnitude > 1f) input.Normalize(); // чтобы по диагонали не было быстрее

        // Flip только по направлению X
        if (input.x > 0.01f) Face(1);
        else if (input.x < -0.01f) Face(-1);

        // Анимации по триггерам
        bool nowMoving = input.sqrMagnitude > 0.0001f;
        if (nowMoving != isMoving)
        {
            isMoving = nowMoving;

            if (animator != null)
            {
                // Важно: триггеры одноразовые, поэтому сбрасываем противоположный.
                if (isMoving)
                {
                    SafeResetTrigger(stopTrigger);
                    SafeSetTrigger(moveStartTrigger);
                }
                else
                {
                    SafeResetTrigger(moveStartTrigger);
                    SafeSetTrigger(stopTrigger);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        // Движение через Rigidbody2D
        rb.velocity = input * speed;
    }

    private void Face(int dirX)
    {
        if (dirX == lastFaceX) return;
        lastFaceX = dirX;

        if (!flipByScaleX) return;

        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * dirX; // dirX: 1 вправо, -1 влево
        transform.localScale = s;
    }

    private void SafeSetTrigger(string triggerName)
    {
        if (string.IsNullOrWhiteSpace(triggerName) || animator == null) return;
        animator.SetTrigger(triggerName);
    }

    private void SafeResetTrigger(string triggerName)
    {
        if (string.IsNullOrWhiteSpace(triggerName) || animator == null) return;
        animator.ResetTrigger(triggerName);
    }

    // Удобно дергать из других скриптов, если нужно
    public void SetSpeed(float newSpeed) => speed = Mathf.Max(0f, newSpeed);
}