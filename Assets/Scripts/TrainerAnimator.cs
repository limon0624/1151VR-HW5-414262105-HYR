// 訓練家動畫：待機呼吸 + 丟球動作
// 放在 Assets/Scripts/（不要放在 Editor 資料夾）
// 由 HW5TrainerGenerator 自動掛在 Trainer 上，關節欄位會自動填好

using System.Collections;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TrainerAnimator : MonoBehaviour
{
    [Header("關節（產生器會自動填好，不用動）")]
    public Transform hips;
    public Transform spine, head;
    public Transform shoulderR, elbowR, shoulderL, elbowL;
    public Transform legR, kneeR, legL, kneeL;

    [Tooltip("右手的位置：精靈球從這裡丟出去")]
    public Transform handR;

    [Header("丟球設定")]
    public bool throwOnStart = true;
    public float startDelay = 1.5f;
    public bool spaceToThrow = true;
    [Tooltip("整個丟球動作的秒數")]
    public float throwDuration = 1.4f;

    [Header("出手瞬間要做的事（第 5 步會用到）")]
    public UnityEvent onRelease;
    public event System.Action Released;

    public bool IsThrowing { get; private set; }

    // 關節順序：0 spine, 1 head, 2 肩R, 3 肘R, 4 肩L, 5 肘L, 6 腿R, 7 膝R, 8 腿L, 9 膝L
    Transform[] joints;
    Vector3[] baseEuler;
    Vector3 hipsBasePos;

    // 各個姿勢：每個關節要「額外轉幾度」(X, Y, Z)
    static readonly Vector3[] PoseIdle = {
        V(0,0,0), V(0,0,0), V(0,0,0), V(-10,0,0), V(0,0,0), V(-10,0,0), V(0,0,0), V(0,0,0), V(0,0,0), V(0,0,0) };
    static readonly Vector3[] PoseWindUp = {   // 蓄力：右手舉到頭後面，身體往後扭，左腳踏出
        V(0,30,0), V(0,-25,0), V(150,0,0), V(-100,0,0), V(-70,0,0), V(-20,0,0), V(15,0,0), V(10,0,0), V(-20,0,0), V(0,0,0) };
    static readonly Vector3[] PoseRelease = {  // 出手：右手往前甩
        V(10,-15,0), V(0,10,0), V(-80,0,0), V(-10,0,0), V(-20,0,0), V(-30,0,0), V(20,0,0), V(15,0,0), V(-25,0,0), V(5,0,0) };
    static readonly Vector3[] PoseFollow = {   // 收尾：手順勢往下、身體前傾
        V(15,-25,0), V(-5,15,0), V(-35,0,-25), V(-20,0,0), V(10,0,0), V(-15,0,0), V(20,0,0), V(20,0,0), V(-25,0,0), V(5,0,0) };

    static readonly float[] KeyTimes = { 0f, 0.35f, 0.45f, 0.60f, 0.70f, 1f };
    static Vector3[][] KeyPoses => new[] { PoseIdle, PoseWindUp, PoseRelease, PoseFollow, PoseFollow, PoseIdle };
    const float ReleaseTime = 0.45f;

    static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

    void Awake()
    {
        joints = new[] { spine, head, shoulderR, elbowR, shoulderL, elbowL, legR, kneeR, legL, kneeL };
        baseEuler = new Vector3[joints.Length];
        for (int i = 0; i < joints.Length; i++)
            if (joints[i] != null) baseEuler[i] = joints[i].localEulerAngles;
        if (hips != null) hipsBasePos = hips.localPosition;
    }

    void Start()
    {
        if (throwOnStart) StartCoroutine(ThrowAfter(startDelay));
    }

    void Update()
    {
        if (spaceToThrow && SpacePressed()) Throw();
        if (!IsThrowing) ApplyIdle(Time.time);
    }

    bool SpacePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    /// <summary>做一次丟球動作。其他程式也可以呼叫它。</summary>
    public void Throw()
    {
        if (IsThrowing) return;
        StartCoroutine(ThrowRoutine());
    }

    IEnumerator ThrowAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        Throw();
    }

    IEnumerator ThrowRoutine()
    {
        IsThrowing = true;
        bool released = false;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / throwDuration);
            ApplyPose(Sample(t), 0f);
            if (!released && t >= ReleaseTime)
            {
                released = true;
                onRelease?.Invoke();
                Released?.Invoke();
            }
            yield return null;
        }
        IsThrowing = false;
    }

    Vector3[] Sample(float t)
    {
        var poses = KeyPoses;
        int k = 0;
        while (k < KeyTimes.Length - 2 && t > KeyTimes[k + 1]) k++;
        float u = Mathf.InverseLerp(KeyTimes[k], KeyTimes[k + 1], t);
        // 蓄力→出手 用加速曲線（甩得比較有力），其他段用平滑曲線
        u = (k == 1) ? u * u : u * u * (3f - 2f * u);

        var result = new Vector3[joints.Length];
        for (int i = 0; i < joints.Length; i++)
            result[i] = Vector3.Lerp(poses[k][i], poses[k + 1][i], u);
        return result;
    }

    void ApplyIdle(float time)
    {
        float b = Mathf.Sin(time * 2f);                 // 呼吸
        var pose = (Vector3[])PoseIdle.Clone();
        pose[0] += V(b * 1.5f, 0, 0);
        pose[1] += V(-b * 1.0f, Mathf.Sin(time * 0.7f) * 4f, 0);
        pose[2] += V(0, 0, b * 2f);
        pose[4] += V(0, 0, -b * 2f);
        ApplyPose(pose, b * 0.006f);
    }

    void ApplyPose(Vector3[] pose, float hipsBob)
    {
        for (int i = 0; i < joints.Length; i++)
            if (joints[i] != null) joints[i].localRotation = Quaternion.Euler(baseEuler[i] + pose[i]);
        if (hips != null) hips.localPosition = hipsBasePos + new Vector3(0f, hipsBob, 0f);
    }
}
