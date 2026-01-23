using UnityEngine;
using UnityEngine.SceneManagement;

public class Переходмеждусценами : MonoBehaviour
{
    [Header("Player detection")]
    [Tooltip("Тег игрока, который может активировать дверь.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Scene переход")]
    [Tooltip("Имя сцены, в которую переходим (должна быть добавлена в Build Settings).")]
    [SerializeField] private string targetSceneName;

    [Tooltip("Загрузить сцену асинхронно (обычно да).")]
    [SerializeField] private bool loadAsync = true;

    [Header("Camera")]
    [Tooltip("Какая камера будет перемещаться. Если пусто — Camera.main.")]
    [SerializeField] private Camera cam;

    [Tooltip("Плавность перемещения камеры к точке (в секундах). 0 = мгновенно.")]
    [SerializeField] private float cameraMoveDuration = 0.35f;

    [Tooltip("Тег точки, куда поставить камеру в НОВОЙ сцене. (Создай пустой объект и поставь ему этот tag).")]
    [SerializeField] private string cameraSpawnTag = "CameraSpawn";

    [Tooltip("Если true — попробуем найти точку по тегу и выставить камеру в новой сцене.")]
    [SerializeField] private bool snapCameraToSpawnInNewScene = true;

    private bool playerInside;
    private bool isTransitioning;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
            playerInside = false;
    }

    private void Update()
    {
        if (isTransitioning) return;

        if (playerInside && Input.GetKeyDown(interactKey))
        {
            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                Debug.LogWarning($"{name}: targetSceneName пустой. Укажи сцену в инспекторе.");
                return;
            }

            StartCoroutine(DoTransition());
        }
    }

    private System.Collections.IEnumerator DoTransition()
    {
        isTransitioning = true;

        // 1) (Опционально) небольшая "подводка" камеры к двери перед загрузкой (в текущей сцене)
        if (cam != null && cameraMoveDuration > 0f)
        {
            Vector3 start = cam.transform.position;
            Vector3 end = new Vector3(transform.position.x, transform.position.y, start.z);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / cameraMoveDuration;
                cam.transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            cam.transform.position = end;
        }

        // 2) Загрузка сцены
        if (loadAsync)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
            while (!op.isDone) yield return null;
        }
        else
        {
            SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
            yield return null;
        }

        // 3) После загрузки — выставляем камеру на точку в новой сцене (если включено)
        if (snapCameraToSpawnInNewScene)
        {
            // Обновим ссылку на камеру (в новой сцене Camera.main может быть другой)
            cam = Camera.main;

            if (cam != null)
            {
                GameObject spawn = GameObject.FindGameObjectWithTag(cameraSpawnTag);
                if (spawn != null)
                {
                    Vector3 p = cam.transform.position;
                    cam.transform.position = new Vector3(spawn.transform.position.x, spawn.transform.position.y, p.z);
                }
                else
                {
                    Debug.LogWarning($"В сцене '{targetSceneName}' не найден объект с тегом '{cameraSpawnTag}'.");
                }
            }
        }

        isTransitioning = false;
    }
}