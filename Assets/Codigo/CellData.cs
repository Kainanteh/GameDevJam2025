using TMPro;
using UnityEngine;

public class CellData : MonoBehaviour
{
    [HideInInspector] public Color originalColor;

    public Vector2Int coordinates;
    public bool isWalkable = true;
    public Building building;

    public bool hasResource = false;
    public string resourceType = "";
    public int resourceAmount = 0;

    [HideInInspector] public GameObject borderTop;
    [HideInInspector] public GameObject borderBottom;
    [HideInInspector] public GameObject borderLeft;
    [HideInInspector] public GameObject borderRight;

    [HideInInspector] public TextMeshProUGUI label;  // NUEVO

    public bool resourceAlreadyCollected = false;


    void Awake()
    {
        borderTop = transform.Find("Borders/BorderTop")?.gameObject;
        borderBottom = transform.Find("Borders/BorderBottom")?.gameObject;
        borderLeft = transform.Find("Borders/BorderLeft")?.gameObject;
        borderRight = transform.Find("Borders/BorderRight")?.gameObject;

        label = GetComponentInChildren<TextMeshProUGUI>(); // buscar el número
    }

    public void ShowBorder(string side, bool active, Color? colorOverride = null)
    {
        GameObject border = null;
        switch (side)
        {
            case "top": border = borderTop; break;
            case "bottom": border = borderBottom; break;
            case "left": border = borderLeft; break;
            case "right": border = borderRight; break;
        }

        if (border != null)
        {
            border.SetActive(active);
            if (active && colorOverride.HasValue)
            {
                var sr = border.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.color = colorOverride.Value;
            }
        }
    }

    public void ClearAllBorders()
    {
        borderTop?.SetActive(false);
        borderBottom?.SetActive(false);
        borderLeft?.SetActive(false);
        borderRight?.SetActive(false);
    }

    public void ShowLabel(bool visible)
    {
        if (label != null)
            label.enabled = visible;
    }

    public void ApplyDebugColor()
    {
        Color baseColor;

        if (building != null)
        {
            baseColor = GameManager.Instance.GetOwnerColor(building.ownerId);
        }
        else if (hasResource)
        {
            baseColor = resourceType switch
            {
                "Comida" => new Color(1f, 0.5f, 0.8f),   // rosa
                "Oro" => Color.yellow,
                "Madera" => new Color(0.6f, 0.4f, 0.2f), // marrón
                _ => Color.white
            };
        }
        else if (!isWalkable)
        {
            baseColor = GameManager.Instance.obstacleColor;
        }
        else
        {
            baseColor = Color.Lerp(GameManager.Instance.walkableCellBorderColor, Color.black, GameManager.Instance.baseColorIntensity);
        }

        baseColor.a = 1f;

        var sr = transform.Find("Square")?.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = baseColor;
            originalColor = baseColor;
        }
    }



}
