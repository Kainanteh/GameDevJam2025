using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class HeadquartersBuilding : Building
{
    public int soldierCount = 1;
    public int maxSoldiers = 99;

    public float generationInterval = 5f;
    public int generationRate = 1;

    private float generationTimer = 0f;

    private List<(Vector3 origen, Vector3 destino, Building target, bool isRecolectar)> activePaths = new();


    private TextMeshPro debugLabel;

    public HeadquartersBuilding(string name, int ownerId) : base(name, BuildingType.Headquarters)
    {
        this.ownerId = ownerId;
    }

    public void Tick(float deltaTime)
    {
        generationTimer += deltaTime;

        while (generationTimer >= generationInterval)
        {
            generationTimer -= generationInterval;

            int usable = generationRate;

            for (int i = 0; i < activePaths.Count && usable > 0;)
            {
                var path = activePaths[i];

                if (path.isRecolectar)
                {
                    // Solo enviar uno, luego eliminar el camino
                    LaunchRecolector(path.origen);
                    activePaths.RemoveAt(i); // Elimina camino de recolecta tras primer uso
                    continue; // No incrementar i, ya que la lista se ha modificado
                }
                else
                {
                    LaunchUnidad(path.origen, path.destino, path.target);
                    usable--;
                    i++; // Solo avanzar si no eliminamos
                }
            }


            if (usable > 0 && soldierCount < maxSoldiers)
            {
                soldierCount += usable;
                soldierCount = Mathf.Min(soldierCount, maxSoldiers);
            }

            UpdateLabel();
        }
    }

    private void LaunchRecolector(Vector3 origen)
    {
        // Obtener la celda de recurso directamente (fija o elegida)
        CellData celdaRecurso = GameManager.Instance.gridGenerator.GetCellAt(1, 7); // Recurso en (1,7)

        // Usar la posición exacta de la celda como destino
        Vector3 destino = celdaRecurso.transform.position;

        // Instanciar unidad y pasar toda la info necesaria
        GameObject unidad = Object.Instantiate(GameManager.Instance.unidadPrefab, origen, Quaternion.identity);
        unidad.name = "Recolector";

        unidad.AddComponent<UnidadRecolector>().Init(this, origen, destino, celdaRecurso);
    }




    public void OnRecolectorSuccess()
    {
        generationRate += 1;
        Debug.Log($"Nueva generación del cuartel: {generationRate}");
    }


    public void RegisterActivePath(Vector3 origenVisual, Vector3 destino, Building target, bool isRecolectar)
    {
        activePaths.Add((origenVisual, destino, target, isRecolectar));
    }


    public void UnregisterActivePath(Vector3 destino)
    {
        activePaths.RemoveAll(p => Vector3.Distance(p.destino, destino) < 0.1f);
    }

    private void LaunchUnidad(Vector3 origen, Vector3 destino, Building target)
    {
        GameObject unidad = Object.Instantiate(GameManager.Instance.unidadPrefab, origen, Quaternion.identity);
        unidad.name = "Unidad";

        int coste = -1;

        UnidadMover mover = unidad.AddComponent<UnidadMover>();
        mover.Init(this, target, origen, destino, coste);
    }



    private Vector3 GetCenter()
    {
        Vector3 center = Vector3.zero;
        foreach (var cell in occupiedCells)
            center += cell.transform.position;

        return center / occupiedCells.Count;
    }

    public void UnidadReachedTarget(Vector3 destino)
    {
        UnregisterActivePath(destino);
    }

    public void UpdateLabel()
    {
        if (debugLabel != null)
            debugLabel.text = $"<b>{soldierCount}</b>";
    }

    public override void PrintInfo()
    {
        Debug.Log($"[HQ] {buildingName} - Owner: {ownerId} - Soldados: {soldierCount} - Caminos activos: {activePaths.Count}");
    }

    public void FinalizeSetup()
    {
        if (!GameManager.Instance.enableTestMode) return;
        if (occupiedCells.Count == 0) return;

        Vector3 center = GetCenter();

        GameObject labelGO = new GameObject($"{buildingName}_Label");
        labelGO.transform.position = new Vector3(center.x, center.y, -0.2f);

        var text = labelGO.AddComponent<TextMeshPro>();
        text.text = $"<b>{soldierCount}</b>";
        text.fontSize = 4;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;

        debugLabel = text;
    }
}
