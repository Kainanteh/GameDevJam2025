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
                if (cell.building != null && cell.building.isHeadquarters && cell.building.ownerId != origen.ownerId)
                {
                    HeadquartersBuilding hqEnemigo = (HeadquartersBuilding)cell.building;
                    hqEnemigo.RecibirDaño(coste);
                    Debug.Log($"💥 Explorador impacta HQ enemigo en celda {cell.coordinates} y causa {coste} de daño");
                }

                Destroy(gameObject);
                return;
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

        if (enTransicionARecolector && recursoDestino != null)
        {
            GameObject unidad = Object.Instantiate(GameManager.Instance.unidadPrefab, transform.position, Quaternion.identity);
            unidad.name = "Recolector";

            unidad.AddComponent<UnidadRecolector>().InitConCamino(
                origen,
                new List<Vector3>(caminoRecorrido),
                recursoDestino,
                null,
                false
            );

            Destroy(gameObject);
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
            GameManager.Instance.exploradorSeleccionado = this;
            GameManager.Instance.CreatePathVisual(celdaActual, targetCell, false);
        }
    }

    public CellData GetCeldaActual()
    {
        return celdaActual;
    }

    public void ConvertirseEnRecolector(CellData recurso)
    {
        recursoDestino = recurso;
        caminoRecorrido.Add(recurso.transform.position);

        GameObject pathGO = Object.Instantiate(GameManager.Instance.pathLinePrefab);
        PathVisual visual = pathGO.GetComponent<PathVisual>();

        visual.Init(transform.position, recurso.transform.position, null, null, GameManager.Instance.gridGenerator, recurso);
        visual.isRecolectar = true;

        inicio = transform.position;
        destino = recurso.transform.position;
        t = 0f;

        esperandoInput = false;
        enTransicionARecolector = true;
    }

    public void ConvertirseEnCaminoDeAtaque(Building hqEnemigo)
    {
        caminoRecorrido.Add(transform.position);
        caminoRecorrido.Add(hqEnemigo.occupiedCells[0].transform.position);

        GameObject pathGO = Object.Instantiate(GameManager.Instance.pathLinePrefab);
        PathVisual visual = pathGO.GetComponent<PathVisual>();

        visual.Init(transform.position, hqEnemigo.occupiedCells[0].transform.position, null, hqEnemigo, GameManager.Instance.gridGenerator);
        visual.isRecolectar = false;

        origen.RegisterActiveSoldado(
            caminoRecorrido[0],
            new List<Vector3>(caminoRecorrido),
            hqEnemigo,
            coste
        );

        inicio = transform.position;
        destino = hqEnemigo.occupiedCells[0].transform.position;
        t = 0f;
        esperandoInput = false;
    }
}
