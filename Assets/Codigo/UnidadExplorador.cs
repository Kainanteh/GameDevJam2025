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

        MostrarTexto();
    }

    void Update()
    {
        if (t < 1f)
        {
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

        if (esperandoInput)
        {
            if (Input.GetMouseButton(0))
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

        if (enTransicionARecolector && recursoDestino != null)
        {
            GameObject unidad = Object.Instantiate(GameManager.Instance.unidadPrefab, transform.position, Quaternion.identity);
            unidad.name = "Recolector";

            unidad.AddComponent<UnidadRecolector>().InitConCamino(
                origen,
                new List<Vector3>(caminoRecorrido),
                recursoDestino
            );

            Destroy(gameObject);
            return;
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

        visual.Init(
            transform.position,
            recurso.transform.position,
            null,
            null,
            GameManager.Instance.gridGenerator,
            recurso
        );
        visual.isRecolectar = true;

        inicio = transform.position;
        destino = recurso.transform.position;
        t = 0f;

        esperandoInput = false;
        enTransicionARecolector = true;
    }

    /*
    public void ConvertirseEnCaminoDeAtaque(Building hqEnemigo)
    {
        Vector3 puntoInicio = transform.position;
        Vector3 puntoFinal = hqEnemigo.occupiedCells[0].transform.position;

        caminoRecorrido = new List<Vector3> { puntoInicio, puntoFinal };

        GameObject pathGO = Object.Instantiate(GameManager.Instance.pathLinePrefab);
        PathVisual visual = pathGO.GetComponent<PathVisual>();

        visual.Init(
            puntoInicio,
            puntoFinal,
            null,
            hqEnemigo,
            GameManager.Instance.gridGenerator
        );
        visual.isRecolectar = false;

        // Registro en el HQ el camino REAL que deben seguir los soldados
        origen.RegisterActiveSoldado(
            origen.occupiedCells[0].transform.position,
            new List<Vector3>(caminoRecorrido),
            hqEnemigo,
            coste
        );

        // El explorador no se destruye ni genera unidades
    }
    */


    /*
    public void ConvertirseEnCaminoDeAtaque(Building hqEnemigo)
    {
        caminoRecorrido.Add(hqEnemigo.occupiedCells[0].transform.position);

        GameObject pathGO = Object.Instantiate(GameManager.Instance.pathLinePrefab);
        PathVisual visual = pathGO.GetComponent<PathVisual>();
        visual.Init(
            transform.position,
            hqEnemigo.occupiedCells[0].transform.position,
            null,
            hqEnemigo,
            GameManager.Instance.gridGenerator
        );
        visual.isRecolectar = false;

        // Eliminar comportamiento del explorador inmediatamente
        UnidadExplorador thisExplorador = GetComponent<UnidadExplorador>();
        Destroy(thisExplorador);

        // Convertir en soldado y continuar
        UnidadSoldado soldado = gameObject.AddComponent<UnidadSoldado>();
        soldado.Init(
            origen,
            new List<Vector3>(caminoRecorrido),
            hqEnemigo,
            coste
        );

        origen.RegisterActiveSoldado(
            origen.occupiedCells[0].transform.position,
            new List<Vector3>(caminoRecorrido),
            hqEnemigo,
            coste
        );
    }
    */
    
    // Este codigo el explorador desaparece lo que esta mal y las unidades se crean bien en el hq recorren el camino para atacar al hq enemigo
    public void ConvertirseEnCaminoDeAtaque(Building hqEnemigo)
    {
        caminoRecorrido.Add(hqEnemigo.occupiedCells[0].transform.position);

        GameObject pathGO = Object.Instantiate(GameManager.Instance.pathLinePrefab);
        PathVisual visual = pathGO.GetComponent<PathVisual>();

        visual.Init(
            transform.position,
            hqEnemigo.occupiedCells[0].transform.position,
            null,
            hqEnemigo,
            GameManager.Instance.gridGenerator
        );
        visual.isRecolectar = false;

        origen.RegisterActiveSoldado(
            caminoRecorrido[0],
            new List<Vector3>(caminoRecorrido),
            hqEnemigo,
            coste
        );

        //Destroy(gameObject);
        // En lugar de Destroy(gameObject), haz que continúe moviéndose al HQ enemigo
        inicio = transform.position;
        destino = hqEnemigo.occupiedCells[0].transform.position;
        t = 0f;
        esperandoInput = false;

    }


    /*
    // Este codigo el explorador ataca desde su posicion al hq enemigo lo que esta bien pero no se generan unidades en el hq sino en el punto intermedio lo que esta mal
    
    public void ConvertirseEnCaminoDeAtaque(Building hqEnemigo)
    {
        Vector3 origenPos = transform.position;
        Vector3 destinoPos = hqEnemigo.occupiedCells[0].transform.position;

        caminoRecorrido = new List<Vector3> { origenPos, destinoPos };

        GameObject pathGO = Object.Instantiate(GameManager.Instance.pathLinePrefab);
        PathVisual visual = pathGO.GetComponent<PathVisual>();

        visual.Init(
            origenPos,
            destinoPos,
            null,
            hqEnemigo,
            GameManager.Instance.gridGenerator
        );
        visual.isRecolectar = false;

        GameObject soldado = Object.Instantiate(GameManager.Instance.unidadPrefab, origenPos, Quaternion.identity);
        soldado.name = "Soldado";
        Debug.Log("✅ Soldado creado en: " + origenPos);
        soldado.AddComponent<UnidadSoldado>().Init(
            origen,
            new List<Vector3>(caminoRecorrido),
            hqEnemigo,
            coste
        );

        origen.RegisterActiveSoldado(
            origen.occupiedCells[0].transform.position,
            new List<Vector3>(caminoRecorrido),
            hqEnemigo,
            coste
        );


        Destroy(gameObject);
    }
    */


    void MostrarTexto()
    {
        var label = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (label != null)
            label.text = coste.ToString();
    }
}
