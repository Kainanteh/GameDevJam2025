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

    private List<CaminoActivo> caminosActivos = new();
    private Dictionary<(Vector3 origen, Vector3 destino), float> ataqueTimers = new();
    private bool fueFuerteNeutral = false;

    private TextMeshPro debugLabel;
    private bool esFuerteNeutral = false;

    public HeadquartersBuilding(string name, int ownerId, bool esFuerteNeutral = false)
        : base(name, esFuerteNeutral ? BuildingType.FuerteNeutral : BuildingType.Headquarters)
    {
        this.ownerId = ownerId;
        this.esFuerteNeutral = esFuerteNeutral;
        if (esFuerteNeutral)
        {
            generationRate = 0;
            maxSoldiers = 5;
            generationInterval = 2f;
            this.fueFuerteNeutral = esFuerteNeutral;

        }
    }

    public void Tick(float deltaTime)
    {


        foreach (var camino in caminosActivos)
        {
            var key = (
                camino.tramos[0].transform.GetComponent<LineRenderer>().GetPosition(0),
                camino.tramos[^1].transform.GetComponent<LineRenderer>().GetPosition(1)
            );

            if (!ataqueTimers.ContainsKey(key))
                ataqueTimers[key] = 0f;

            ataqueTimers[key] += deltaTime;

            // ⚔️ Camino de ataque normal: sigue lanzando soldados si no es refuerzo
            if (camino.isAtaque && !camino.esRefuerzoPasivo)
            {
                if (ataqueTimers[key] >= generationInterval)
                {
                    ataqueTimers[key] = 0f;

                    if (soldierCount > 0)
                    {
                        soldierCount--;
                        UpdateLabel();

                        List<Vector3> puntos = new();
                        foreach (var tramo in camino.tramos)
                        {
                            LineRenderer lr = tramo.GetComponent<LineRenderer>();
                            if (puntos.Count == 0)
                                puntos.Add(lr.GetPosition(0));
                            puntos.Add(lr.GetPosition(1));
                        }

                        LaunchSoldado(puntos[0], puntos, camino.objetivo, 1, camino);
                    }
                    else
                    {
                        Debug.Log($"❌ HQ {buildingName} no puede lanzar soldado: sin soldados disponibles");
                    }
                }
            }
        }

        // ✅ Generación pasiva estándar (usando generationRate)
        if (!esFuerteNeutral && soldierCount < maxSoldiers)
        {
            generationTimer += deltaTime;

            if (generationTimer >= generationInterval)
            {
                generationTimer = 0f;
                int prev = soldierCount;
                soldierCount += generationRate;
                soldierCount = Mathf.Min(soldierCount, maxSoldiers);
                if (soldierCount != prev)
                    UpdateLabel();
            }
        }
        else
        {
            generationTimer = 0f;
        }
    }



    public void UnidadReachedTarget(Vector3 destino)
    {
        caminosActivos.RemoveAll(p =>
        {
            LineRenderer lr = p.tramos[^1].GetComponent<LineRenderer>();
            return Vector3.Distance(lr.GetPosition(1), destino) < 0.1f;
        });
    }

    public void RegisterActiveRecolector(Vector3 origen, List<Vector3> camino, CellData celdaRecurso)
    {
        CaminoActivo caminoActivo = new CaminoActivo(true, false, null, celdaRecurso);
        GameObject grupoVisual = new GameObject("CaminoRecolector");
        grupoVisual.transform.position = Vector3.zero;

        for (int i = 0; i < camino.Count - 1; i++)
        {
            GameObject pathGO = Object.Instantiate(GameManager.Instance.pathLinePrefab, grupoVisual.transform);
            PathVisual visual = pathGO.GetComponent<PathVisual>();
            visual.Init(camino[i], camino[i + 1], this, null, GameManager.Instance.gridGenerator, celdaRecurso);
            visual.isRecolectar = true;
            caminoActivo.tramos.Add(visual);
        }

        caminosActivos.Add(caminoActivo);
        LaunchRecolector(origen, celdaRecurso, camino, caminoActivo);
    }

    public void RegisterActiveSoldado(Vector3 origen, List<Vector3> camino, Building hqObjetivo, int daño)
    {
        CaminoActivo caminoActivo = new CaminoActivo(false, true, hqObjetivo, null);
        GameObject grupoVisual = new GameObject("CaminoAtaque");
        grupoVisual.transform.position = Vector3.zero;

        for (int i = 0; i < camino.Count - 1; i++)
        {
            GameObject pathGO = Object.Instantiate(GameManager.Instance.pathLinePrefab, grupoVisual.transform);
            PathVisual visual = pathGO.GetComponent<PathVisual>();
            visual.Init(camino[i], camino[i + 1], this, hqObjetivo, GameManager.Instance.gridGenerator);
            visual.isRecolectar = false;
            caminoActivo.tramos.Add(visual);
        }

        caminosActivos.Add(caminoActivo);
    }



    private void LaunchRecolector(Vector3 origen, CellData celdaRecurso, List<Vector3> camino, CaminoActivo caminoActivo)
    {
        GameObject unidad = Object.Instantiate(GameManager.Instance.unidadPrefab, origen, Quaternion.identity);
        unidad.name = "Recolector";
        unidad.AddComponent<UnidadRecolector>().InitConCamino(this, camino, celdaRecurso, caminoActivo);
        soldierCount--;
        UpdateLabel();
    }

    private void LaunchSoldado(Vector3 origen, List<Vector3> camino, Building objetivo, int daño, CaminoActivo caminoActivo)
    {
        GameObject unidad = Object.Instantiate(GameManager.Instance.unidadPrefab, origen, Quaternion.identity);
        unidad.name = "Soldado";
        unidad.AddComponent<UnidadSoldado>().Init(this, camino, objetivo, daño, caminoActivo);
    }

    public void OnRecolectorSuccess()
    {
        generationRate += 1;
    }

    public void UpdateLabel()
    {
        if (debugLabel != null)
            debugLabel.text = $"<b>{soldierCount}</b>";
    }

    public override void PrintInfo()
    {
        Debug.Log($"[HQ] {buildingName} - Owner: {ownerId} - Soldados: {soldierCount} - Caminos activos: {caminosActivos.Count}");
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

    public void RecibirDaño(int daño)
    {
        soldierCount -= daño;
        if (soldierCount < 0) soldierCount = 0;
        UpdateLabel();
        Debug.Log($"💥 HQ {buildingName} recibe {daño} de daño");

        // Permitir reconquista solo si fue originalmente FuerteNeutral
        // y no es HQ principal del jugador (0) ni del enemigo (1)
        if (fueFuerteNeutral && soldierCount == 0)
        {
            ConquistarFuerte();
        }
    }



    private void ConquistarFuerte()
    {
        int nuevoOwnerId = GameManager.Instance.UltimoAtacanteOwnerId;
        if (nuevoOwnerId == -1)
        {
            Debug.Log("⚠️ No se encontró unidad atacante para conquistar el fuerte");
            return;
        }

        string nuevoNombre = $"Cuartel {nuevoOwnerId}";

        ownerId = nuevoOwnerId;
        type = BuildingType.Headquarters;
        buildingName = nuevoNombre;
        soldierCount = 1;
        generationRate = 1;
        maxSoldiers = 5;
        generationTimer = 0f;
        esFuerteNeutral = false;
        fueFuerteNeutral = true; // 🔁 ¡conservar propiedad reconquistable!


        UpdateLabel();
        foreach (var celda in occupiedCells)
            celda.ApplyDebugColor();

        int refuerzos = 0;

        // 🔁 Buscar caminos asociados a la conquista y aumentar generationRate
        foreach (var cell in occupiedCells)
        {
            Collider2D[] colisiones = Physics2D.OverlapCircleAll(cell.transform.position, 1.5f);
            foreach (var col in colisiones)
            {
                UnidadSoldado soldado = col.GetComponent<UnidadSoldado>();
                if (soldado != null && soldado.caminoAsociado != null)
                {
                    soldado.caminoAsociado.esRefuerzoPasivo = true;
                    refuerzos++;
                    Debug.Log("🔁 Camino de ataque convertido en refuerzo pasivo");
                }
            }
        }

        generationRate += refuerzos;

        Debug.Log($"🏳️ FuerteNeutral ha sido conquistado por jugador {ownerId} y ahora actúa como HQ con generationRate = {generationRate}");
    }


}
