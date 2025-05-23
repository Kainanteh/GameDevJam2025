// CaminoActivo.cs
using System.Collections.Generic;
using UnityEngine;

public class CaminoActivo
{
    public List<PathVisual> tramos = new();
    public GameObject tramosParent;

    public bool isRecolectar;
    public bool isAtaque;
    public Building objetivo;
    public CellData celdaRecurso;

    public bool esRefuerzoPasivo = false;

    public List<GameObject> unidadesVinculadas = new(); // ✅ se añaden aquí las unidades que usan este camino

    public CaminoActivo(bool isRecolectar, bool isAtaque, Building objetivo, CellData celdaRecurso)
    {
        this.isRecolectar = isRecolectar;
        this.isAtaque = isAtaque;
        this.objetivo = objetivo;
        this.celdaRecurso = celdaRecurso;
    }

    public void PrintCeldasAfectadas()
    {
        Debug.Log("📌 Celdas afectadas por el camino:");
        for (int i = 0; i < tramos.Count; i++)
        {
            var tramo = tramos[i];
            string celdas = "";
            foreach (var cell in tramo.affectedCells)
            {
                celdas += $"{cell.coordinates}, ";
            }
            Debug.Log($"🔸 Tramo {i}: {celdas}");
        }
    }
}
