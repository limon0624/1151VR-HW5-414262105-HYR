// 收服特效：打中 → 閃光、寶可夢變紅被吸進球裡 → 球掉地上搖三下 → 星星 + 收服成功
// 放在 Assets/Scripts/，跟 PokeballThrow 一起掛在 Pokeball 物件上
// 特效（粒子、光、文字）全部由程式產生，不需要額外素材

using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(PokeballThrow))]
public class CaptureEffect : MonoBehaviour
{
    [Header("打中後球往回彈")]
    public float bounceBack = 0.6f;
    public float bounceUp = 0.7f;

    [Header("吸進球裡")]
    [Tooltip("寶可夢被吸進去要幾秒")]
    public float absorbTime = 0.8f;
    public Color glowColor = new Color(1f, 0.25f, 0.25f);

    [Header("搖晃")]
    public int wobbleCount = 3;
    public float wobbleAngle = 25f;

    [Header("收服成功文字（空白 = 自動用「寶可夢名字 + 收服成功！」）")]
    public string captureMessage = "";
    public Color textColor = new Color(1f, 0.9f, 0.3f);

    [Header("音效（可選，沒有就留空）")]
    public AudioClip hitSound;
    public AudioClip absorbSound;
    public AudioClip wobbleSound;
    public AudioClip successSound;

    PokeballThrow thrower;
    AudioSource audioSource;
    Quaternion uprightRotation;
    Vector3 localCenter;
    float radius;

    Texture2D glowTex, starTex;
    Material particleMat, starMat;

    bool showText;
    float textStartTime;
    string message;

    void Awake()
    {
        thrower = GetComponent<PokeballThrow>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        uprightRotation = transform.rotation;   // 一開始擺的方向當作「正面」
    }

    void Start()
    {
        Vector3 c = PokeballThrow.GetVisualCenter(transform);
        localCenter = transform.InverseTransformPoint(c);
        radius = GetBounds(transform).extents.y;

        glowTex = MakeSoftCircle(64);
        starTex = MakeStar(64);
        particleMat = MakeParticleMaterial(glowTex);
        starMat = MakeParticleMaterial(starTex);

        thrower.Hit += OnHit;
    }

    void OnDestroy()
    {
        if (thrower != null) thrower.Hit -= OnHit;
    }

    void OnHit()
    {
        StartCoroutine(CaptureRoutine());
    }

    // ==================================================================
    //  主流程
    // ==================================================================
    IEnumerator CaptureRoutine()
    {
        Transform target = thrower.target;
        if (target == null) yield break;

        message = string.IsNullOrEmpty(captureMessage) ? target.name + " 收服成功！" : captureMessage;

        // 飛行方向（水平），用來決定往回彈的方向
        Vector3 dirFlat = Vector3.right;
        if (thrower.trainer != null)
        {
            Vector3 d = target.position - thrower.trainer.transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.001f) dirFlat = d.normalized;
        }

        // ---------- 1) 往回彈，停在半空中，同時轉回正面 ----------
        Vector3 hit = Center();
        Vector3 hover = hit - dirFlat * bounceBack + Vector3.up * bounceUp;
        Quaternion rot0 = transform.rotation;
        yield return Animate(0.35f, u =>
        {
            float e = 1f - (1f - u) * (1f - u);                  // 先快後慢
            transform.rotation = Quaternion.Slerp(rot0, uprightRotation, e);
            SetCenter(Vector3.Lerp(hit, hover, e));
        });

        // ---------- 2) 閃光 ----------
        Vector3 targetCenter = PokeballThrow.GetVisualCenter(target);
        Play(hitSound);
        Burst(targetCenter, Color.white, glowColor, 50, 3.5f, 0.35f, 0.7f, particleMat, 0f, 0.2f);
        StartCoroutine(FlashLight(targetCenter, Color.white, 8f, 0.5f));

        // ---------- 3) 寶可夢變紅、被紅光吸進球裡 ----------
        Material glowMat = MakeGlowMaterial(glowColor);
        foreach (var r in target.GetComponentsInChildren<Renderer>())
        {
            var mats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = glowMat;
            r.materials = mats;
        }
        var anim = target.GetComponentInChildren<Animator>();
        if (anim != null) anim.enabled = false;

        LineRenderer beam = MakeBeam();
        Play(absorbSound);

        yield return new WaitForSeconds(0.25f);

        Vector3 startPos = target.position;
        Vector3 startScale = target.localScale;
        Vector3 pivotToCenter = targetCenter - target.position;
        yield return Animate(absorbTime, u =>
        {
            float e = u * u;                                      // 越吸越快
            target.localScale = Vector3.Lerp(startScale, Vector3.zero, e);
            // 讓寶可夢的中心點往球的方向移動
            Vector3 c = Vector3.Lerp(targetCenter, hover, e);
            target.position = c - pivotToCenter * (1f - e);
            // 紅白閃爍
            glowMat.color = (Mathf.Repeat(u * 8f, 1f) < 0.5f) ? glowColor : Color.white;
            beam.SetPosition(0, Center());
            beam.SetPosition(1, c);
            beam.widthMultiplier = 0.08f * (1f - u * 0.7f);
        });

        target.gameObject.SetActive(false);
        Destroy(beam.gameObject);
        Burst(hover, glowColor, Color.white, 20, 1.5f, 0.2f, 0.4f, particleMat, 0f, 0.05f);
        StartCoroutine(FlashLight(hover, glowColor, 5f, 0.3f));

        yield return new WaitForSeconds(0.25f);

        // ---------- 4) 掉到地上，彈一下 ----------
        float groundY = GroundHeight(hover) + radius;
        Vector3 land = new Vector3(hover.x, groundY, hover.z);
        yield return Animate(0.45f, u =>
        {
            SetCenter(Vector3.Lerp(hover, land, u * u));          // 像重力一樣加速
        });
        yield return Animate(0.35f, u =>
        {
            SetCenter(land + Vector3.up * (0.25f * Mathf.Sin(u * Mathf.PI)));
        });
        SetCenter(land);

        yield return new WaitForSeconds(0.5f);

        // ---------- 5) 左右搖晃三下 ----------
        Vector3 axis = Vector3.forward;
        if (Camera.main != null)
        {
            axis = Camera.main.transform.forward; axis.y = 0f;
            if (axis.sqrMagnitude < 0.001f) axis = Vector3.forward;
            axis.Normalize();
        }
        for (int i = 0; i < wobbleCount; i++)
        {
            Play(wobbleSound);
            float prev = 0f;
            yield return Animate(0.6f, u =>
            {
                float angle = wobbleAngle * Mathf.Sin(u * Mathf.PI * 2f);   // 0 → 右 → 左 → 0
                Vector3 bottom = Center() - Vector3.up * radius;           // 以碰地的點為支點
                transform.RotateAround(bottom, axis, angle - prev);
                prev = angle;
            });
            transform.rotation = uprightRotation;
            SetCenter(land);
            yield return new WaitForSeconds(0.55f);
        }

        // ---------- 6) 收服成功 ----------
        Play(successSound);
        Burst(land + Vector3.up * 0.2f, new Color(1f, 0.95f, 0.4f), Color.white, 30, 3f, 0.35f, 1.2f, starMat, 0.4f, 0.1f);
        StartCoroutine(FlashLight(land + Vector3.up * 0.5f, new Color(1f, 0.9f, 0.5f), 5f, 0.6f));
        showText = true;
        textStartTime = Time.time;
    }

    // ==================================================================
    //  工具
    // ==================================================================
    delegate void Step(float u);

    IEnumerator Animate(float duration, Step step)
    {
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            step(t);
            yield return null;
        }
    }

    Vector3 Center() { return transform.TransformPoint(localCenter); }

    void SetCenter(Vector3 worldPoint) { transform.position += worldPoint - Center(); }

    static Bounds GetBounds(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(t.position, Vector3.one * 0.3f);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    float GroundHeight(Vector3 p)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null) return terrain.SampleHeight(p) + terrain.transform.position.y;
        if (Physics.Raycast(p + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 50f)) return hit.point.y;
        return 0f;
    }

    void Play(AudioClip clip)
    {
        if (clip != null) audioSource.PlayOneShot(clip);
    }

    IEnumerator FlashLight(Vector3 pos, Color color, float peak, float duration)
    {
        var go = new GameObject("CaptureFlash");
        go.transform.position = pos;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 8f;
        light.color = color;
        yield return Animate(duration, u => light.intensity = peak * (1f - u));
        Destroy(go);
    }

    LineRenderer MakeBeam()
    {
        var go = new GameObject("CaptureBeam");
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.material = particleMat;
        lr.startColor = Color.white;
        lr.endColor = glowColor;
        lr.widthMultiplier = 0.08f;
        lr.numCapVertices = 4;
        lr.shadowCastingMode = ShadowCastingMode.Off;
        lr.receiveShadows = false;
        return lr;
    }

    void Burst(Vector3 pos, Color c1, Color c2, int count, float speed, float size,
               float life, Material mat, float gravity, float shapeRadius)
    {
        var go = new GameObject("CaptureFX");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = new ParticleSystem.MinMaxGradient(c1, c2);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = shapeRadius;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-4f, 4f);

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.material = mat;
        rend.shadowCastingMode = ShadowCastingMode.Off;

        ps.Play();
        Destroy(go, life + 1.5f);
    }

    static Material MakeParticleMaterial(Texture2D tex)
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        var m = new Material(sh) { mainTexture = tex };
        return m;
    }

    static Material MakeGlowMaterial(Color c)
    {
        Shader sh = null;
        if (GraphicsSettings.currentRenderPipeline != null) sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        return new Material(sh) { color = c };
    }

    // 柔邊圓點（閃光用）
    static Texture2D MakeSoftCircle(int s)
    {
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = (x + 0.5f) / s * 2f - 1f, dy = (y + 0.5f) / s * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                px[y * s + x] = new Color(1f, 1f, 1f, a * a);
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    // 五角星（收服成功用）
    static Texture2D MakeStar(int s)
    {
        var poly = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
            float r = (i % 2 == 0) ? 0.95f : 0.42f;
            poly[i] = new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
        }
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[s * s];
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                int inside = 0;
                for (int sy = 0; sy < 3; sy++)              // 3x3 超取樣，邊緣比較平滑
                    for (int sx = 0; sx < 3; sx++)
                    {
                        Vector2 p = new Vector2((x + (sx + 0.5f) / 3f) / s * 2f - 1f, (y + (sy + 0.5f) / 3f) / s * 2f - 1f);
                        if (InPolygon(p, poly)) inside++;
                    }
                px[y * s + x] = new Color(1f, 1f, 1f, inside / 9f);
            }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    static bool InPolygon(Vector2 p, Vector2[] poly)
    {
        bool c = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                c = !c;
        }
        return c;
    }

    // ==================================================================
    //  收服成功文字
    // ==================================================================
    void OnGUI()
    {
        if (!showText) return;
        float a = Mathf.Clamp01((Time.time - textStartTime) / 0.4f);
        float pop = 1f + 0.25f * Mathf.Max(0f, 1f - (Time.time - textStartTime) / 0.3f);

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(Screen.height / 11f * pop),
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        Rect r = new Rect(0f, Screen.height * 0.08f, Screen.width, Screen.height * 0.2f);

        style.normal.textColor = new Color(0f, 0f, 0f, 0.6f * a);       // 陰影
        GUI.Label(new Rect(r.x + 4f, r.y + 4f, r.width, r.height), message, style);
        style.normal.textColor = new Color(textColor.r, textColor.g, textColor.b, a);
        GUI.Label(r, message, style);
    }
}
