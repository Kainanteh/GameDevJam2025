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

    private TextMeshProUGUI debugLabel;
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
            this.fueFuerteNeutral = true;
        }
        
        if (ownerId == 0) // 🧑 jugador
        {
            maxSoldiers = 10;
           
        }
    
    }


    public void Tick(float deltaTime)
    {
        for (int i = caminosActivos.Count - 1; i >= 0; i--)
        {
            var camino = caminosActivos[i];

            if (camino.tramos.Count == 0 || camino.tramos[0] == null || camino.tramos[^1] == null)
                continue;

            LineRenderer lrStart = camino.tramos[0].GetComponent<LineRenderer>();
            LineRenderer lrEnd = camino.tramos[^1].GetComponent<LineRenderer>();

            if (lrStart == null || lrEnd == null)
                continue;

            var key = (
                lrStart.GetPosition(0),
                lrEnd.GetPosition(1)
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

                    if (soldierCount > 1)
                    {
                        soldierCount--;
                        UpdateLabel();

                        List<Vector3> puntos = new();
                        foreach (var tramo in camino.tramos)
                        {
                            if (tramo == null) continue;
                            LineRenderer lr = tramo.GetComponent<LineRenderer>();
                            if (lr == null) continue;

                            if (puntos.Count == 0)
                                puntos.Add(lr.GetPosition(0));
                            puntos.Add(lr.GetPosition(1));
                        }

                        LaunchSoldado(puntos[0], puntos, camino.objetivo, 1, camino);
                    }
                    else
                    {
                        Debug.Log($"⚠️ HQ {buildingName} no lanza soldado: solo queda 1 soldado");
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

        if (ownerId == 11)
            EvaluarResolucionDisputa();
    }


    public Vector2Int GetCeldaInferiorIzquierda()
    {
        Vector2Int min = occupiedCells[0].coordinates;
        foreach (var c in occupiedCells)
        {
            if (c.coordinates.x < min.x || (c.coordinates.x == min.x && c.coordinates.y < min.y))
                min = c.coordinates;
        }
        return min;
    }


    public bool IsFuerteNeutral()
    {
        return esFuerteNeutral;
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
        if (soldierCount <= 1 && ownerId == 0)

        {
            Debug.Log($"⚠️ HQ {buildingName} no lanza recolector: solo queda 1 soldado");
            return;
        }

        GameObject unidad = Object.Instantiate(GameManager.Instance.unidadPrefab, origen, Quaternion.identity);
        unidad.name = "Recolector";
        unidad.AddComponent<UnidadRecolector>().InitConCamino(this, camino, celdaRecurso, caminoActivo);
        soldierCount--;
        UpdateLabel();
    }


    private void LaunchSoldado(Vector3 origen, List<Vector3> camino, Building objetivo, int daño, CaminoActivo caminoActivo)
    {

        if (soldierCount <= 1 && ownerId == 0)
        {
            Debug.Log($"⛔ HQ jugador no lanza soldado: se quedaría a 0.");
            return;
        }


        GameObject unidad = Object.Instantiate(GameManager.Instance.unidadPrefab, origen, Quaternion.identity);
        unidad.name = "Soldado";
        unidad.AddComponent<UnidadSoldado>().Init(this, camino, objetivo, daño, caminoActivo);
    }

    public void OnRecolectorSuccess(int recursoamount)
    {
        generationRate += recursoamount;
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

        GameObject labelGO = GameObject.Instantiate(Resources.Load<GameObject>("circle_black"));
        labelGO.transform.position = new Vector3(center.x, center.y + 1.5f, -0.2f);

        debugLabel = labelGO.GetComponentInChildren<TextMeshProUGUI>();

        debugLabel.text = $"<b>{soldierCount}</b>";



        //debugLabel = text;
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

        if (soldierCount == 0)
        {
            generationRate = 0;

            if (ownerId == 0 && !fueFuerteNeutral)
            {
                Debug.Log("❌ El jugador ha perdido su cuartel principal.");
                GameManager.Instance.MostrarMensajeResultado("DEFEAT");
                GameManager.Instance.IniciarTransicionEscena("Menu", 3f);
            }
            else if (ownerId > 0 && ownerId < 10)
            {
                Debug.Log($"✅ HQ enemigo {buildingName} ha sido destruido.");

                bool quedanEnemigos = false;
                foreach (var cell in GameManager.Instance.gridGenerator.allCells.Values)
                {
                    if (cell.building is HeadquartersBuilding hq)
                    {
                        if (hq.ownerId > 0 && hq.ownerId < 10 && hq != this && hq.soldierCount > 0)
                        {
                            quedanEnemigos = true;
                            break;
                        }
                    }
                }

                if (!quedanEnemigos)
                {
                    GameManager.Instance.MostrarMensajeResultado("VICTORY");

                    string actual = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                    if (actual == "Pantalla3")
                        GameManager.Instance.IniciarTransicionEscena("Menu", 3f);
                    else
                        GameManager.Instance.IrASiguientePantalla(3f);
                }

            }
        }


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

        // Evaluar disputa por caminos activos
        int presionJugador = 0;
        int presionEnemigo = 0;

        foreach (var cell in GameManager.Instance.gridGenerator.allCells.Values)
        {
            if (cell.building is HeadquartersBuilding origenHQ)
            {
                foreach (var camino in origenHQ.GetCaminosActivos())
                {
                    if (!camino.isAtaque || camino.esRefuerzoPasivo) continue;
                    if (camino.objetivo != this) continue;

                    if (origenHQ.ownerId == 0)
                        presionJugador++;
                    else if (origenHQ.ownerId > 0 && origenHQ.ownerId < 10)
                        presionEnemigo++;
                }
            }
        }

        if (presionJugador > 0 && presionEnemigo > 0)
        {
            ownerId = 11;
            generationRate = 0;
            ApplyColorDisputa();
            Debug.Log("⚖️ Fuerte entra en disputa (evaluado en ConquistarFuerte)");
            return;
        }

        // Conquista directa si no hay disputa
        string nuevoNombre = $"Cuartel {nuevoOwnerId}";

        ownerId = nuevoOwnerId;
        type = BuildingType.Headquarters;
        buildingName = nuevoNombre;
        soldierCount = 1;
        generationRate = 1;
        maxSoldiers = 5;
        generationTimer = 0f;
        esFuerteNeutral = false;
        fueFuerteNeutral = true;

        UpdateLabel();
        foreach (var celda in occupiedCells)
            celda.ApplyDebugColor();

        int refuerzos = 0;

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

    private void ApplyColorDisputa()
    {
        foreach (var celda in occupiedCells)
        {
            var sr = celda.transform.Find("Square")?.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(0.6f, 0f, 0.8f); // Púrpura
                celda.originalColor = sr.color;
            }
        }
    }

    public List<CaminoActivo> GetCaminosActivos()
    {
        return caminosActivos;
    }

    public void EvaluarResolucionDisputa()
    {
        if (ownerId != 11) return;

        int presionJugador = 0;
        int presionEnemigo = 0;

        foreach (var cell in GameManager.Instance.gridGenerator.allCells.Values)
        {
            if (cell.building is HeadquartersBuilding origenHQ)
            {
                foreach (var camino in origenHQ.GetCaminosActivos())
                {
                    if (!camino.isAtaque || camino.esRefuerzoPasivo) continue;
                    if (camino.objetivo != this) continue;

                    if (origenHQ.ownerId == 0)
                        presionJugador++;
                    else if (origenHQ.ownerId == 1)
                        presionEnemigo++;
                }
            }
        }

        if (presionJugador == 0 && presionEnemigo == 0) return;
        if (presionJugador == presionEnemigo) return;

        int ganador = (presionJugador > presionEnemigo) ? 0 : 1;

        ownerId = ganador;
        type = BuildingType.Headquarters;
        buildingName = $"Cuartel {ganador}";
        soldierCount = 3;

        int diff = Mathf.Abs(presionJugador - presionEnemigo);
        generationRate += diff;
        maxSoldiers = 5;
        generationTimer = 0f;
        esFuerteNeutral = false;
        fueFuerteNeutral = true;

        UpdateLabel();

        foreach (var celda in occupiedCells)
            celda.ApplyDebugColor();

        Debug.Log($"✅ Disputa resuelta: fuerte conquistado por jugador {ganador} (+{diff} generations)");
    }



}
