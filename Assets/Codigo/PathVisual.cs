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

        // Visual
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = 0.15f;
        lr.endWidth = 0.15f;
        lr.startColor = Color.black;
        lr.endColor = Color.black;
        lr.material = new Material(Shader.Find("Sprites/Default"));

        // Registro de celdas atravesadas
        affectedCells = GetCellsBetween(start, end, grid);

        originBuilding = origin;
        targetBuilding = target;

        // Nombre origen
        originName = origin != null ? origin.buildingName : "null";

        if (target != null)
        {
            targetName = target.buildingName;
        }
        else if (targetCell != null)
        {
            if (targetCell.hasResource)
                targetName = $"{targetCell.coordinates} {targetCell.resourceType}";
            else
                targetName = $"{targetCell.coordinates}";
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
        int steps = Mathf.CeilToInt(distance / 0.1f); // precisión

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
}
