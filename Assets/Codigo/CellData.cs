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

    [HideInInspector] public TextMeshProUGUI label;

    public bool resourceAlreadyCollected = false;

    void Awake()
    {
        borderTop = transform.Find("Borders/BorderTop")?.gameObject;
        borderBottom = transform.Find("Borders/BorderBottom")?.gameObject;
        borderLeft = transform.Find("Borders/BorderLeft")?.gameObject;
        borderRight = transform.Find("Borders/BorderRight")?.gameObject;

        label = GetComponentInChildren<TextMeshProUGUI>();
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
        bool activarHierba = false;
        int hijoAActivar = -1;

        if (building != null)
        {
            baseColor = GameManager.Instance.GetOwnerColor(building.ownerId);

            if (building is HeadquartersBuilding hq)
            {
                if (hq.IsFuerteNeutral())
                {
                    hijoAActivar = 6; // Fuerte neutral
                }
                else
                {
                    // Mostrar HQ (2x2) solo en la celda inferior izquierda
                    Vector2Int minCoord = hq.GetCeldaInferiorIzquierda();
                    if (coordinates == minCoord)
                    {
                        hijoAActivar = 7; // HQ
                    }
                }
            }
        }
        else if (hasResource)
        {
            if (resourceType == "Comida")
            {
                baseColor = new Color(1f, 0.5f, 0.8f);
                hijoAActivar = Random.Range(1, 4); // hijos [1–3] = comida
            }
            else if (resourceType == "Oro")
            {
                baseColor = Color.yellow;
            }
            else if (resourceType == "Madera")
            {
                baseColor = new Color(0.6f, 0.4f, 0.2f);
            }
            else
            {
                baseColor = Color.white;
            }
        }
        else if (!isWalkable)
        {
            baseColor = GameManager.Instance.obstacleColor;
            hijoAActivar = Random.Range(4, 6); // hijos [4–5] = no caminable
        }
        else
        {
            baseColor = Color.Lerp(GameManager.Instance.walkableCellBorderColor, Color.black, GameManager.Instance.baseColorIntensity);
            activarHierba = true;
        }

        baseColor.a = 1f;

        var sr = transform.Find("Square")?.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = baseColor;
            originalColor = baseColor;
        }

        // Activar hierba base si corresponde
        if (activarHierba && transform.childCount > 0)
        {
            transform.GetChild(0).gameObject.SetActive(true);
        }

        // Activar decorado correspondiente
        if (hijoAActivar >= 1 && hijoAActivar < transform.childCount)
        {
            transform.GetChild(hijoAActivar).gameObject.SetActive(true);
        }
    }




}
