using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class CardFanOpen : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] GameObject cardPrefab;   // CardUI 预制体

    [Header("布局参数")]
    [SerializeField] int cardCount = 5;                  // 卡牌数量
    [SerializeField] float fanAngle = 70f;               // 扇形总张角（度）
    [SerializeField] float maxStep = 15f;                // 相邻两张卡的最大角度差：
    [SerializeField] float pivotDrop = 30f;              // 轴心比卡片底边再下沉多少：
    [SerializeField] Vector3 riseFrom = new Vector3(0f, -400f, 0f); // 升起点
    [SerializeField] Vector3 riseStop = new Vector3(0f, 0f, 0f);    // 叠放点

    [Header("动画参数")]
    [SerializeField] float riseDuration = 0.5f;          // 单张升起用时
    [SerializeField] float riseStagger = 0.1f;           // 升起间隔
    [SerializeField] float fanDuration = 0.6f;           // 单张旋转展开用时
    [SerializeField] float fanStagger = 0.12f;           // 旋转展开间隔
    [SerializeField] float startDelay = 0f;
    [SerializeField] AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] bool playOnStart = true;

    readonly List<Transform> axes = new List<Transform>();
    Coroutine running;

    void Start()
    {
        if (cardPrefab != null && playOnStart) Play();
    }


    public void Play()
    {
        if (cardPrefab == null)
        {
            Debug.LogWarning($"[{name}] cardPrefab 未赋值，无法播放开屏动画。", this);
            return;
        }
        if (running != null) StopCoroutine(running);
        ClearAxes();
        SpawnCards();
        running = StartCoroutine(FanRoutine());
    }

    void ClearAxes()
    {
        foreach (var a in axes)
            if (a != null) Destroy(a.gameObject);
        axes.Clear();
    }

    void SpawnCards()
    {
        for (int i = 0; i < cardCount; i++)
        {
            GameObject card = Instantiate(cardPrefab, transform);
            RectTransform rt = card.transform as RectTransform;
            if (rt == null)
            {
                Debug.LogError($"[{name}] cardPrefab 根节点不是 RectTransform，请使用 UI 版预制体。", this);
                Destroy(card);
                continue;
            }
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;


            GameObject axis = new GameObject($"CardAxis_{i}");
            axis.transform.SetParent(transform, false);
            axis.transform.localPosition = riseStop;
            rt.SetParent(axis.transform, false);
            rt.localPosition = new Vector3(0f, pivotDrop + rt.rect.height * 0.5f, 0f);

            axes.Add(axis.transform);
        }

      
        var byOuterFirst = Enumerable.Range(0, axes.Count)
            .OrderByDescending(idx => Mathf.Abs(idx - (axes.Count - 1) * 0.5f));
        int sibling = 0;
        foreach (var idx in byOuterFirst)
            axes[idx].SetSiblingIndex(sibling++);
    }

    IEnumerator FanRoutine()
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        int n = axes.Count;
        if (n == 0) { running = null; yield break; }

       
        float maxOrder = (n - 1) * 0.5f;
        for (int i = 0; i < n; i++)
            StartCoroutine(CardRoutine(i, n));

        yield return new WaitForSeconds(riseStagger * maxOrder + riseDuration
                                       + fanStagger * maxOrder + fanDuration + 0.05f);
        running = null;
    }

    IEnumerator CardRoutine(int i, int n)
    {
        Transform axis = axes[i];
        if (axis == null) yield break;

  
        float order = Mathf.Abs(i - (n - 1) * 0.5f);

        
        yield return new WaitForSeconds(order * riseStagger);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, riseDuration);
            axis.localPosition = Vector3.Lerp(riseFrom, riseStop, curve.Evaluate(Mathf.Clamp01(t)));
            yield return null;
        }
        axis.localPosition = riseStop;


        yield return new WaitForSeconds(order * fanStagger);
        float targetAngle = TargetAngle(i, n);
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, fanDuration);
            axis.localRotation = Quaternion.Euler(0f, 0f, targetAngle * curve.Evaluate(Mathf.Clamp01(t)));
            yield return null;
        }
        axis.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
    }


    float TargetAngle(int i, int n)
    {
        float offset = i - (n - 1) * 0.5f;
        float step = Mathf.Min(fanAngle / Mathf.Max(1, n - 1), maxStep);
        return -offset * step;
    }
}
