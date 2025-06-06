﻿using TMPro;
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
    public void AplicarColorHQ()
    {
        Color colorFinal = Color.white;

        if (!(building is HeadquartersBuilding hq))
            return;

        if (hq.ownerId == 11)
            colorFinal = GameManager.Instance.disputaFuerteColor;
        else if (hq.IsFuerteNeutral())
            colorFinal = GameManager.Instance.neutralFuerteColor;
        else
            colorFinal = GameManager.Instance.GetOwnerColor(hq.ownerId);

        // Aplicar a overlay del HQ
        var hqOverlay = transform.Find("cellhq/colorlhq");
        if (hqOverlay != null)
        {
            var sr = hqOverlay.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = colorFinal;
        }

        // Aplicar a overlay del Fuerte
        var fuerteOverlay = transform.Find("cellfuerte/colorfuerte");
        if (fuerteOverlay != null)
        {
            var sr = fuerteOverlay.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = colorFinal;
        }

        // Fuerte: también pintar fondo Square
        if (hq.IsFuerteNeutral() || hq.ownerId == 11 || hq.ownerId >= 0)
        {
            var fondo = transform.Find("Square")?.GetComponent<SpriteRenderer>();
            if (fondo != null) fondo.color = colorFinal;
        }
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
                    if (hq.occupiedCells.Count >= 4)
                    {
                        Vector2Int minCoord = hq.GetCeldaInferiorIzquierda();
                        if (coordinates == minCoord)
                            hijoAActivar = 7; // HQ grande
                    }
                }

                AplicarColorHQ(); // ✅ aplicar color al overlay del HQ
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
            baseColor = new Color(0.4f, 0.6f, 0.3f); // ejemplo de verde neutro
            activarHierba = true;
        }


        baseColor.a = 1f;

        var sr = transform.Find("Square")?.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = baseColor;
            originalColor = baseColor;
        }

        if (activarHierba && transform.childCount > 0)
            transform.GetChild(0).gameObject.SetActive(true);

        if (hijoAActivar >= 1 && hijoAActivar < transform.childCount)
            transform.GetChild(hijoAActivar).gameObject.SetActive(true);
    }






}
