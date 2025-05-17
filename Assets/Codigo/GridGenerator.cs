// GridGenerator.cs
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GridGenerator : MonoBehaviour
{
    public GameObject cellPrefab;
    public int width = 10;
    public int height = 10;
    public float cellSize = 1f;

    public void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 position = new Vector2(x * cellSize, y * cellSize);
                GameObject cell = Instantiate(cellPrefab, position, Quaternion.identity, transform);
                cell.name = $"Cell_{x}_{y}";

                TextMeshProUGUI tmp = cell.GetComponentInChildren<TextMeshProUGUI>();

                if (tmp != null)
                    tmp.text = $"{x},{y}";

                CellData data = cell.GetComponent<CellData>();
                if (data != null)
                {
                    data.coordinates = new Vector2Int(x, y);

                    // Opcional: setea datos por defecto aleatorios para test
                    data.isWalkable = true;
               
                    /*data.hasResource = Random.value < 0.2f;
                    if (data.hasResource)
                    {
                        data.resourceType = "Gold";
                        data.resourceAmount = Random.Range(5, 20);
                    }*/

                    Vector2Int coord = new Vector2Int(x, y);
                    data.coordinates = coord;
                    allCells[coord] = data;

                    SpriteRenderer sr = cell.transform.Find("Square")?.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                       
                        data.originalColor = sr.color;
                    }


                }



            }
        }
    }

    public Dictionary<Vector2Int, CellData> allCells = new();

    public CellData GetCellAt(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);
        return allCells.ContainsKey(pos) ? allCells[pos] : null;
    }


}

