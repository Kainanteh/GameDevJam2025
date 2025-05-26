using System.Collections.Generic;
using UnityEngine;

public class UnidadExplorador : MonoBehaviour
{
    public HeadquartersBuilding origen;
    public Vector3 inicio;
    public Vector3 destino;
    public float t = 0f;
    public float speed = 2f;
    public CellData celdaActual;
    public List<PathVisual> tramosExploracion = new List<PathVisual>();
    public int coste;
    public bool esperandoInput = false;

    public List<Vector3> caminoRecorrido = new List<Vector3>();

    private bool enTransicionARecolector = false;
    private CellData recursoDestino = null;

    public void Init(HeadquartersBuilding origen, Vector3 inicio, Vector3 destino, CellData celdaFinal, int coste)
    {
        this.origen = origen;
        this.inicio = inicio;
        this.destino = destino;
        this.celdaActual = celdaFinal;
        this.coste = coste;
        this.caminoRecorrido = new List<Vector3> { inicio, destino };



        var label = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (label != null)
            label.text = $"E{coste}";
    }


    void Update()
    {
        var orientador = transform.Find("GnomoSprite")?.GetComponent<SpriteOrientador>();

        if (t < 1f)
        {
            orientador?.ActualizarDireccion(inicio, destino);
      

            t += Time.deltaTime * speed;
            t = Mathf.Clamp01(t);
            transform.position = Vector3.Lerp(inicio, destino, t);

            Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
            var cell = GameManager.Instance.gridGenerator.GetCellAt(gridPos.x, gridPos.y);

            if (cell != null && !cell.isWalkable)
            {
                if (cell.building != null && cell.building.isHeadquarters)
                {
                    if (cell.building.ownerId != origen.ownerId)
                    {
                        HeadquartersBuilding hqEnemigo = (HeadquartersBuilding)cell.building;
                        hqEnemigo.RecibirDaño(coste);
                        Debug.Log($"💥 Explorador impacta HQ enemigo en celda {cell.coordinates} y causa {coste} de daño");
                        Destroy(gameObject);
                        return;
                    }
                    // Si es su propio HQ, no muere
                }
                else
                {
                    Debug.Log($"☠️ Explorador murió en obstáculo en celda {cell.coordinates}");
                    Destroy(gameObject);
                    return;
                }
            }

            if (t >= 1f && !enTransicionARecolector)
            {
                esperandoInput = true;
            }

            return;
        }

        if (esperandoInput && Input.GetMouseButton(0))
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0;

            if (Vector3.Distance(mouseWorld, transform.position) < 0.75f)
            {
                GameManager.Instance.IniciarDesdeExplorador(this);
                esperandoInput = false;
            }
        }
    }



    public void ConvertirSegunDestino(CellData targetCell)
    {
        if (targetCell.hasResource && targetCell.resourceType == "Comida")
        {
            ConvertirseEnRecolector(targetCell);
        }
        else if (targetCell.building != null && targetCell.building.isHeadquarters && targetCell.building.ownerId != 0)
        {
            ConvertirseEnCaminoDeAtaque(targetCell.building);
        }
        else
        {
            GameManager.Instance.CreatePathVisual(celdaActual, targetCell, false);

            // ⬇️ Esto activa el movimiento al nuevo destino
            destino = targetCell.transform.position;
            inicio = transform.position;
            t = 0f;
            esperandoInput = false;
            celdaActual = targetCell;

            GameManager.Instance.exploradorSeleccionado = this;
        }
    }


    public CellData GetCeldaActual()
    {
        return celdaActual;
    }


    public void ConvertirseEnRecolector(CellData recurso)
    {
        recursoDestino = recurso;

        Vector3 puntoInicio = transform.position;

        var caminoAnterior = origen.GetCaminosActivos().Find(c => c.unidadesVinculadas.Contains(gameObject));
        if (caminoAnterior != null)
            origen.GetCaminosActivos().Remove(caminoAnterior);

        List<Vector3> caminoFinal = new();

        foreach (var tramo in tramosExploracion)
        {
            if (tramo == null) continue;

            LineRenderer lr = tramo.GetComponent<LineRenderer>();
            Vector3 start = lr.GetPosition(0);
            Vector3 end = lr.GetPosition(1);

            if (caminoFinal.Count == 0)
                caminoFinal.Add(start);

            if (caminoFinal[caminoFinal.Count - 1] != end)
                caminoFinal.Add(end);
        }

        if (caminoFinal.Count == 0 || caminoFinal[caminoFinal.Count - 1] != recurso.transform.position)
            caminoFinal.Add(recurso.transform.position);

        int startIndex = 0;
        float minDist = float.MaxValue;

        for (int i = 0; i < caminoFinal.Count; i++)
        {
            float d = Vector3.Distance(puntoInicio, caminoFinal[i]);
            if (d < minDist)
            {
                minDist = d;
                startIndex = i;
            }
        }

        GameObject unidad = Object.Instantiate(GameManager.Instance.unidadPrefab, puntoInicio, Quaternion.identity);
        unidad.name = "Recolector";

        CaminoActivo camino = new CaminoActivo(true, false, null, recurso);
        camino.unidadesVinculadas.Add(unidad);

        GameObject grupoVisual = new GameObject("CaminoRecolector");
        grupoVisual.transform.position = Vector3.zero;
        camino.tramosParent = grupoVisual;

        for (int i = 0; i < caminoFinal.Count - 1; i++)
        {
            GameObject pathGO = Object.Instantiate(GameManager.Instance.pathLinePrefab, grupoVisual.transform);
            PathVisual visual = pathGO.GetComponent<PathVisual>();
            visual.Init(caminoFinal[i], caminoFinal[i + 1], origen, null, GameManager.Instance.gridGenerator, recurso);
            visual.isRecolectar = true;
            camino.tramos.Add(visual);
        }

        origen.GetCaminosActivos().Add(camino);

        unidad.AddComponent<UnidadRecolector>().InitConCamino(
            origen,
            caminoFinal,
            recurso,
            camino,
            startIndex
        );

        BorrarTramosExploracion();
        Destroy(gameObject);
    }


    public void ConvertirseEnCaminoDeAtaque(Building hqEnemigo)
    {
        List<Vector3> caminoFinal = new();

        foreach (var tramo in tramosExploracion)
        {
            if (tramo == null) continue;
            LineRenderer lr = tramo.GetComponent<LineRenderer>();
            if (lr == null) continue;

            Vector3 start = lr.GetPosition(0);
            Vector3 end = lr.GetPosition(1);

            if (caminoFinal.Count == 0)
                caminoFinal.Add(start);

            if (caminoFinal[caminoFinal.Count - 1] != end)
                caminoFinal.Add(end);
        }

        caminoFinal.Add(hqEnemigo.occupiedCells[0].transform.position);

        CaminoActivo camino = new CaminoActivo(false, true, hqEnemigo, null);
        GameObject grupoVisual = new GameObject("CaminoAtaque");
        grupoVisual.transform.position = Vector3.zero;
        camino.tramosParent = grupoVisual;

        for (int i = 0; i < caminoFinal.Count - 1; i++)
        {
            GameObject pathGO = Object.Instantiate(GameManager.Instance.pathLinePrefab, grupoVisual.transform);
            PathVisual visual = pathGO.GetComponent<PathVisual>();
            visual.Init(caminoFinal[i], caminoFinal[i + 1], origen, hqEnemigo, GameManager.Instance.gridGenerator);
            visual.isRecolectar = false;
            camino.tramos.Add(visual);
        }

        GameObject unidad = Object.Instantiate(GameManager.Instance.unidadPrefab, transform.position, Quaternion.identity);
        unidad.name = "Soldado33";
        unidad.AddComponent<UnidadSoldado>().Init(origen, caminoFinal, hqEnemigo, coste, camino);

        camino.unidadesVinculadas.Add(unidad);
        origen.GetCaminosActivos().Add(camino);

        BorrarTramosExploracion();
        Destroy(gameObject);
    }



    public void AgregarTramo(PathVisual visual)
    {
        tramosExploracion.Add(visual);
    }

    void BorrarTramosExploracion()
    {
        foreach (var tramo in tramosExploracion)
        {
            if (tramo != null)
                Destroy(tramo.gameObject);
        }
        tramosExploracion.Clear();
    }
}
