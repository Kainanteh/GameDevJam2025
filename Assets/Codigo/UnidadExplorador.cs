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
 
                Destroy(gameObject);
                return;
            }

            if (t >= 1f && !enTransicionARecolector)
            {
                esperandoInput = true;
            }


            return;
        }

        // Detección de clic prolongado
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
            // Cuando llegue al recurso: crear recolector
            origen.OnRecolectorSuccess();
            GameObject unidad = Object.Instantiate(GameManager.Instance.unidadPrefab, transform.position, Quaternion.identity);
            unidad.name = "Recolector";

            unidad.AddComponent<UnidadRecolector>().InitConCamino(
                origen,
                new List<Vector3>(caminoRecorrido), // ya incluye el punto final
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
        else if (targetCell.building != null && targetCell.building.ownerId != 0)
        {
            ConvertirseEnAtaque(targetCell.building);
        }
        else
        {
            GameManager.Instance.exploradorSeleccionado = this;
            GameManager.Instance.CreatePathVisual(celdaActual, targetCell, false);
            // GameManager.Instance.exploradorSeleccionado = null; ← se borra internamente después
        }
    }


    public CellData GetCeldaActual()
    {
        return celdaActual;
    }

    public void ConvertirseEnRecolector(CellData recurso)
    {
        // Guardar info y preparar movimiento normal (ya gestionado en Update)
        recursoDestino = recurso;

        // Añadir destino al camino
        caminoRecorrido.Add(recurso.transform.position);

        // ⚠️ Dibujar visual desde la celda actual (donde está parado) hasta el recurso
        GameObject pathGO = Object.Instantiate(GameManager.Instance.pathLinePrefab);
        PathVisual visual = pathGO.GetComponent<PathVisual>();

        visual.Init(
            transform.position,                      // desde donde está el explorador parado (celda libre)
            recurso.transform.position,             // hasta el recurso
            null,                                   // sin building origen
            null,                                   // sin building destino
            GameManager.Instance.gridGenerator,
            recurso                                 // para que registre nombre del recurso
        );
        visual.isRecolectar = true;


        // Preparar para movimiento
        inicio = transform.position;
        destino = recurso.transform.position;
        t = 0f;

        esperandoInput = false;
        enTransicionARecolector = true;
    }


    public void ConvertirseEnAtaque(Building enemigo)
    {
        Vector3 origenVisual = transform.position;
        Vector3 destinoVisual = enemigo.occupiedCells[0].transform.position;

        GameObject pathGO = Object.Instantiate(GameManager.Instance.pathLinePrefab);
        PathVisual visual = pathGO.GetComponent<PathVisual>();

        visual.Init(origenVisual, destinoVisual, null, enemigo, GameManager.Instance.gridGenerator);
        visual.isRecolectar = false;

        origen.RegisterActivePath(origenVisual, destinoVisual, enemigo, false);
        Destroy(gameObject);
    }


    void MostrarTexto()
    {
        var label = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (label != null)
        {
            label.text = coste.ToString();
        }
    }

}
