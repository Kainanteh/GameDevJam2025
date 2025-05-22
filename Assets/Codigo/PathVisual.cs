using System.Collections.Generic;
using UnityEngine;

public class PathVisual : MonoBehaviour
{
    public Building originBuilding;
    public Building targetBuilding;

    [SerializeField] private string originName;
    [SerializeField] private string targetName;

    public List<CellData> affectedCells = new List<CellData>();
    public bool isRecolectar = false;

    public void Init(Vector3 start, Vector3 end, Building origin, Building target, GridGenerator grid, CellData targetCell = null)
    {
        LineRenderer lr = GetComponent<LineRenderer>();
        if (lr == null) lr = gameObject.AddComponent<LineRenderer>();

        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = 0.15f;
        lr.endWidth = 0.15f;
        lr.startColor = Color.white;
        lr.endColor = Color.white;
        lr.alignment = LineAlignment.TransformZ;
        lr.textureMode = LineTextureMode.Tile;

        Material caminoMaterial = Resources.Load<Material>("CaminoMaterial");
        if (caminoMaterial == null)
        {
            Debug.LogError("❌ Material 'CaminoMaterial' no encontrado en Resources.");
            return;
        }

        Vector2 dir = (end - start).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x);

        // Corregir si es casi horizontal (|y| muy bajo)
        if (Mathf.Abs(dir.y) < 0.8f)
        {
            float fuerza = 1.2f; // cuanto más alto, más inclinado (π/4 = 0.785, π/2 = 1.57)
            angle += Mathf.Sign(dir.y == 0 ? 1 : dir.y) * fuerza;
        }


        caminoMaterial = new Material(caminoMaterial); // evitar que se modifique globalmente
        caminoMaterial.SetFloat("_Rotation", angle);
        lr.material = caminoMaterial;

        float distancia = Vector3.Distance(start, end);
        float ppu = 100f;
        float texturaAltoEnUnidades = caminoMaterial.mainTexture != null ? caminoMaterial.mainTexture.height / ppu : 1f;
        float tiles = distancia / texturaAltoEnUnidades;

        // Obtener cuánto se estira el eje de textura tras la rotación
        float sin = Mathf.Abs(Mathf.Sin(angle));
        float cos = Mathf.Abs(Mathf.Cos(angle));
        float escalaY = sin > cos ? sin : cos;

        lr.material.mainTextureScale = new Vector2(tiles, escalaY);


        affectedCells = GetCellsBetween(start, end, grid);
        originBuilding = origin;
        targetBuilding = target;

        originName = origin != null ? origin.buildingName : "null";

        if (target != null)
        {
            targetName = target.buildingName;
        }
        else if (targetCell != null)
        {
            targetName = targetCell.hasResource
                ? $"{targetCell.coordinates} {targetCell.resourceType}"
                : $"{targetCell.coordinates}";
        }
        else
        {
            targetName = "Sin destino";
        }
    }


    public void Init(List<Vector3> puntos, Building origin, Building target, GridGenerator grid, CellData targetCell = null)
    {
        LineRenderer lr = GetComponent<LineRenderer>();
        if (lr == null) lr = gameObject.AddComponent<LineRenderer>();

        lr.positionCount = puntos.Count;
        lr.SetPositions(puntos.ToArray());
        lr.startWidth = 0.15f;
        lr.endWidth = 0.15f;
        lr.startColor = Color.black;
        lr.endColor = Color.black;
        lr.material = new Material(Shader.Find("Sprites/Default"));

        affectedCells = new List<CellData>();
        foreach (var punto in puntos)
        {
            Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(punto.x), Mathf.RoundToInt(punto.y));
            CellData cell = grid.GetCellAt(gridPos.x, gridPos.y);
            if (cell != null && !affectedCells.Contains(cell))
                affectedCells.Add(cell);
        }

        originBuilding = origin;
        targetBuilding = target;

        originName = origin != null ? origin.buildingName : "null";

        if (target != null)
        {
            targetName = target.buildingName;
        }
        else if (targetCell != null)
        {
            targetName = targetCell.hasResource ? $"{targetCell.coordinates} {targetCell.resourceType}" : $"{targetCell.coordinates}";
        }
        else
        {
            targetName = "Sin destino";
        }
    }

    private List<CellData> GetCellsBetween(Vector3 start, Vector3 end, GridGenerator grid)
    {
        List<CellData> result = new List<CellData>();

        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);
        int steps = Mathf.CeilToInt(distance / 0.1f);

        for (int i = 0; i <= steps; i++)
        {
            Vector3 point = Vector3.Lerp(start, end, i / (float)steps);
            Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y));

            CellData cell = grid.GetCellAt(gridPos.x, gridPos.y);
            if (cell != null && !result.Contains(cell))
            {
                result.Add(cell);
            }
        }

        return result;
    }

    public string GetOriginName()
    {
        return originName;
    }

    public string GetTargetName()
    {
        return targetName;
    }

}
