using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class HeadquartersBuilding : Building
{
    public int soldierCount = 1;
    public int maxSoldiers = 5;

    public float generationInterval = 5f;
    public int generationRate = 1;

    private float generationTimer = 0f;

    private List<(Vector3 origen, Vector3 destino, Building target, bool isRecolectar, bool isAtaque, List<Vector3> camino, int daño)> activePaths = new();

    private TextMeshPro debugLabel;

    public HeadquartersBuilding(string name, int ownerId) : base(name, BuildingType.Headquarters)
    {
        this.ownerId = ownerId;
    }

    private void LaunchRecolector(Vector3 origen, CellData celdaRecurso, List<Vector3> camino)
    {
        GameObject unidad = Object.Instantiate(GameManager.Instance.unidadPrefab, origen, Quaternion.identity);
        unidad.name = "Recolector";
        unidad.AddComponent<UnidadRecolector>().InitConCamino(this, camino, celdaRecurso);
    }

    private void LaunchSoldado(Vector3 origen, List<Vector3> camino, Building objetivo, int daño)
    {
        GameObject unidad = Object.Instantiate(GameManager.Instance.unidadPrefab, origen, Quaternion.identity);
        unidad.name = "Soldado";
        unidad.AddComponent<UnidadSoldado>().Init(this, camino, objetivo, daño);
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
                    if (soldierCount <= 0)
                    {
                        Debug.LogWarning("❌ No hay soldados para recolectar.");
                        activePaths.RemoveAt(i);
                        continue;
                    }

                    soldierCount--;
                    LaunchRecolector(path.origen, GameManager.Instance.gridGenerator.GetCellAt(1, 7), path.camino);
                    activePaths.RemoveAt(i);
                    UpdateLabel();
                    usable--;
                }
                else if (path.isAtaque)
                {
                    if (soldierCount <= 0)
                    {
                        i++;
                        continue;
                    }

                    LaunchSoldado(path.origen, path.camino, path.target, path.daño);
                    usable--;
                    i++;
                }
                else
                {
                    activePaths.RemoveAt(i);
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

    public void OnRecolectorSuccess()
    {
        generationRate += 1;
        Debug.Log($"Nueva generación del cuartel: {generationRate}");
    }

    public void RegisterActiveRecolector(Vector3 origen, List<Vector3> camino, CellData celdaRecurso)
    {
        activePaths.Add((origen, camino[camino.Count - 1], null, true, false, camino, 0));
    }

    public void RegisterActiveSoldado(Vector3 origen, List<Vector3> camino, Building hqObjetivo, int daño)
    {
        activePaths.Add((origen, camino[camino.Count - 1], hqObjetivo, false, true, camino, daño));
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

    private Vector3 GetCenter()
    {
        Vector3 center = Vector3.zero;
        foreach (var cell in occupiedCells)
            center += cell.transform.position;

        return center / occupiedCells.Count;
    }

    public void UnidadReachedTarget(Vector3 destino)
    {
        activePaths.RemoveAll(p => Vector3.Distance(p.destino, destino) < 0.1f);
    }

    public void RecibirDaño(int daño)
    {
        soldierCount -= daño;
        if (soldierCount < 0) soldierCount = 0;
        UpdateLabel();
        Debug.Log($"💥 HQ {buildingName} recibe {daño} de daño");
    }


}