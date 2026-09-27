using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class CardHandLayout : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] GameObject cardPrefab;   // CardUI 预制体）

    [Header("布局参数")]
    [SerializeField] int cardCount = 5;              
    [SerializeField] float maxRowWidth = 900f;       // 规定范围：间距 = maxRowWidth ÷ 卡数
    [SerializeField] float maxSpacing = 200f;        // 相邻卡的最大中心距
    [SerializeField] Vector3 dealFrom = new Vector3(0f, -600f, 0f); 

    [Header("动画参数")]
    [SerializeField] float smoothSpeed = 10f;        // 卡牌滑向槽位的平滑速度（越大越快）
    [SerializeField] float dealStagger = 0.12f;      // 发牌间隔
    [SerializeField] bool playOnStart = true;

    readonly List<RectTransform> cards = new List<RectTransform>();   // 从左到右排序

    void Start()
    {
        if (cardPrefab != null && playOnStart) Play();
    }


    void Update()
    {
        int n = cards.Count;
        if (n == 0) return;

        float spacing = SpacingFor(n);
        float k = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);  
        for (int i = 0; i < n; i++)
        {
            RectTransform card = cards[i];
            if (card == null) continue;
            Vector3 home = new Vector3((i - (n - 1) * 0.5f) * spacing, 0f, 0f);
            card.localPosition = Vector3.Lerp(card.localPosition, home, k);
        }
    }


    float SpacingFor(int n)
    {
        if (n <= 1) return 0f;
        return Mathf.Min(maxSpacing, maxRowWidth / n);
    }

   
    public void AddCard()
    {
        if (cardPrefab == null)
        {
            Debug.LogWarning($"[{name}] cardPrefab 未赋值。", this);
            return;
        }
        GameObject card = Instantiate(cardPrefab, transform);
        RectTransform rt = card.transform as RectTransform;
        if (rt == null)
        {
            Debug.LogError($"[{name}] cardPrefab 根节点不是 RectTransform，请使用 UI 版预制体。", this);
            Destroy(card);
            return;
        }
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.localPosition = dealFrom;  
        cards.Add(rt);
        ApplyOrder();
    }

  
    public void RemoveAt(int index = -1)
    {
        int n = cards.Count;
        if (n == 0) return;
        if (index < 0 || index >= n) index = n - 1;
        Destroy(cards[index].gameObject);
        cards.RemoveAt(index);
        ApplyOrder();
    }

   
    public void ClearCards()
    {
        foreach (var c in cards)
            if (c != null) Destroy(c.gameObject);
        cards.Clear();
    }

  
    public void Play()
    {
        if (cardPrefab == null)
        {
            Debug.LogWarning($"[{name}] cardPrefab 未赋值。", this);
            return;
        }
        ClearCards();
        StartCoroutine(DealSequence());
    }

    IEnumerator DealSequence()
    {
        for (int i = 0; i < cardCount; i++)
        {
            AddCard();
            yield return new WaitForSeconds(dealStagger);
        }
    }


    void ApplyOrder()
    {
        for (int i = 0; i < cards.Count; i++)
            cards[i].SetSiblingIndex(i);
    }
}
