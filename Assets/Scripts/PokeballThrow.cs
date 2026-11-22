// 精靈球丟出：拿在訓練家手上 → 出手瞬間沿拋物線飛向寶可夢
// 放在 Assets/Scripts/，掛在 Pokeball 物件上

using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PokeballThrow : MonoBehaviour
{
    [Header("把 Hierarchy 裡的物件拖進來")]
    [Tooltip("場景裡的 Trainer")]
    public TrainerAnimator trainer;
    [Tooltip("場景裡的寶可夢（例如 Pikachu）")]
    public Transform target;

    [Header("飛行設定")]
    [Tooltip("飛到寶可夢要幾秒")]
    public float flightTime = 0.9f;
    [Tooltip("拋物線最高點比直線高出幾公尺")]
    public float arcHeight = 1.8f;
    [Tooltip("每秒旋轉幾度")]
    public float spinSpeed = 720f;
    [Tooltip("沒有指定訓練家時，幾秒後自動丟出")]
    public float fallbackDelay = 1.5f;

    [Header("測試用")]
    [Tooltip("執行時按 R 重新開始，方便重複錄影")]
    public bool pressRToRestart = true;

    [Header("打中寶可夢時要做的事（第 6 步會用到）")]
    public UnityEvent onHit;
    public event System.Action Hit;

    public bool HasThrown { get; private set; }

    Vector3 localCenter;   // 球的「視覺中心」在自己座標系的位置（模型的軸心不一定在正中間）

    void Start()
    {
        localCenter = transform.InverseTransformPoint(GetVisualCenter(transform));

        if (trainer != null && trainer.handR != null)
        {
            trainer.Released += OnTrainerReleased;
            SnapCenterTo(trainer.handR.position);
        }
        else
        {
            StartCoroutine(ThrowAfter(fallbackDelay));
        }
    }

    void OnDestroy()
    {
        if (trainer != null) trainer.Released -= OnTrainerReleased;
    }

    void Update()
    {
        if (pressRToRestart && RPressed())
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // 動畫在 Update 更新完手的位置後，LateUpdate 再把球貼到手上
    void LateUpdate()
    {
        if (!HasThrown && trainer != null && trainer.handR != null)
            SnapCenterTo(trainer.handR.position);
    }

    void OnTrainerReleased()
    {
        Throw();
    }

    /// <summary>把球丟出去（其他程式也可以呼叫）</summary>
    public void Throw()
    {
        if (HasThrown) return;
        StartCoroutine(Fly());
    }

    IEnumerator ThrowAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        Throw();
    }

    IEnumerator Fly()
    {
        HasThrown = true;

        Vector3 start = transform.TransformPoint(localCenter);
        Vector3 end = (target != null) ? GetVisualCenter(target) : start + Vector3.right * 8f;

        Vector3 dir = end - start; dir.y = 0f;
        Vector3 spinAxis = dir.sqrMagnitude > 0.001f ? Vector3.Cross(dir.normalized, Vector3.up) : Vector3.forward;

        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / flightTime);

            // 直線位置 + 拋物線高度：4t(1-t) 在 t=0.5 時剛好等於 1
            Vector3 p = Vector3.Lerp(start, end, t) + Vector3.up * (arcHeight * 4f * t * (1f - t));

            transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.World);
            SnapCenterTo(p);
            yield return null;
        }

        onHit?.Invoke();
        Hit?.Invoke();
    }

    // 讓球的視覺中心對準某個點
    void SnapCenterTo(Vector3 worldPoint)
    {
        Vector3 c = transform.TransformPoint(localCenter);
        transform.position += worldPoint - c;
    }

    // 用所有 Renderer 的範圍算出模型的中心點
    public static Vector3 GetVisualCenter(Transform t)
    {
        var renderers = t.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return t.position;
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b.center;
    }

    bool RPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }
}
