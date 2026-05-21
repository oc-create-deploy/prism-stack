using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteByPoints : MonoBehaviour
{
    [Header("Sprites Progressius")]
    public Sprite[] baseSprites = new Sprite[80];

    [Header("Overlay Sprites")]
    public Sprite[] overlaySprites = new Sprite[80];

    [Header("Punts")]
    public string pointsKey = "Points";
    public int pointsPerSprite = 500;

    private SpriteRenderer sr;
    private int lastBaseIndex = -1;
    private bool overlayApplied = false;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        UpdateBaseSprite();
    }

    void Update()
    {
        if (overlayApplied) return;
        UpdateBaseSprite();
    }

    private void UpdateBaseSprite()
    {
        int points = PlayerPrefs.GetInt(pointsKey, 0);
        int idx = ComputeIndexByPoints(points);

        if (idx != lastBaseIndex)
        {
            if (idx >= 0 && idx < baseSprites.Length && baseSprites[idx] != null)
            {
                sr.sprite = baseSprites[idx];
                Debug.Log($"[SpriteByPoints] Assignant baseSprites[{idx}] segons {points} punts");
            }
            else
            {
                Debug.LogWarning($"[SpriteByPoints] Índex {idx} fora de rang o sprite nul!");
            }
            lastBaseIndex = idx;
        }
    }

    public void SetNewSprite()
    {
        int points = PlayerPrefs.GetInt(pointsKey, 0);
        int idx = ComputeIndexByPoints(points);

        if (idx >= 0 && idx < overlaySprites.Length && overlaySprites[idx] != null)
        {
            sr.sprite = overlaySprites[idx];
            overlayApplied = true;
            Debug.Log($"[SpriteByPoints] SetNewSprite() overlaySprites[{idx}] assignat segons {points} punts.");
        }
        else
        {
            Debug.LogWarning($"[SpriteByPoints] SetNewSprite() fallida: idx {idx} fora de rang o sprite nul.");
        }
    }

    private int ComputeIndexByPoints(int points)
    {
        int idx = points / pointsPerSprite;
        return Mathf.Clamp(idx, 0, baseSprites.Length - 1);
    }
}