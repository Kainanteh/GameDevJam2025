﻿using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public enum TipoCamino
{
    Recolectar,
    Exploracion,
    Ataque
}


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GridGenerator gridGenerator;

    [Header("Prefabs")]
    public GameObject pathLinePrefab;

    [Header("Unidades")]
    public GameObject unidadPrefab;
    public float unidadSpeed = 1f;

    [Header("ADMIN")]
    public bool enableTestMode = true;
    public Color walkableCellBorderColor = Color.yellow;
    public Color nonWalkableCellBorderColor = Color.red;
    public Color buildingBorderColor = Color.green;
    public Color obstacleColor = new Color(0f, 0.3f, 0f);

    [Header("Colores por jugador (según ownerId)")]
    public Color playerColor = Color.blue;
    public Color enemy1Color = Color.red;
    public Color enemy2Color = Color.magenta;
    public Color enemy3Color = Color.yellow;

    [Header("Auto Color Fill (Test Mode Only)")]
    [Range(0f, 1f)] public float baseColorIntensity = 0.3f;

    private readonly List<CellData> selectedCells = new();
    private bool isDraggingPath = false;
    private CellData pathStartCell = null;

    public UnidadExplorador exploradorSeleccionado = null;
    public int UltimoAtacanteOwnerId = -1;

    public Transform contenedorCaminos;
    public GameObject prefabBotonCamino;

    public Color neutralFuerteColor = Color.gray;
    public Color disputaFuerteColor = new Color(0.6f, 0f, 0.8f); // púrpura

    public GameObject uiResultadoPanel;
    public TextMeshProUGUI uiResultadoTexto;


    // GameManager.cs
    public void MostrarBotonCancelar(CaminoActivo camino, HeadquartersBuilding hq)
    {
        foreach (Transform child in contenedorCaminos)
            Destroy(child.gameObject);

        GameObject botonGO = Instantiate(prefabBotonCamino, contenedorCaminos);
        UICaminoBoton script = botonGO.GetComponent<UICaminoBoton>();

        if (camino.tramos.Count > 0 && camino.tramos[0] != null)
        {
            GameObject visualGO = camino.tramos[0].gameObject;
            script.Init(camino, hq); // ✅ llamado correcto según el método actual
        }
        else
        {
            script.Init(camino, hq);
        }

    }



    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        GenerateGame();
        PlaceTestBuilding();
        ApplyTestColors();
        Camera.main.GetComponent<CameraController>()?.SetBounds();
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;

        UpdateUnidadGeneration(deltaTime);

        if (Input.GetMouseButtonDown(0))
        {
            if (exploradorSeleccionado == null)
            {
                DetectCellClick();
                StartPathAction();

                // 👇 Nuevo: detectar clic en camino
                DetectPathClick();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            EndPathAction();
        }
    }
    void DetectPathClick()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);

        if (hit.collider != null)
        {
            PathVisual path = hit.collider.GetComponent<PathVisual>();
            if (path != null && path.originBuilding is HeadquartersBuilding hq && hq.ownerId == 0)
            {
                CaminoActivo camino = hq.GetCaminosActivos().Find(c => c.tramos.Contains(path));
                if (camino != null)
                {
                    Debug.Log("✅ Clic detectado sobre camino (PathVisual)");
                    MostrarBotonCancelar(camino, hq);
                }
            }
        }
    }

    void UpdateUnidadGeneration(float deltaTime)
    {
        foreach (var cell in gridGenerator.allCells.Values)
        {
            if (cell.building is HeadquartersBuilding hq)
            {
                hq.Tick(deltaTime);
            }
        }
    }

    public void GenerateGame()
    {
        if (gridGenerator != null)
            gridGenerator.GenerateGrid();

        ToggleCellLabels(enableTestMode);
    }

    public Color GetOwnerColor(int ownerId)
    {
        return ownerId switch
        {
            0 => playerColor,
            1 => enemy1Color,
            2 => enemy2Color,
            3 => enemy3Color,
            10 => new Color(0.8f, 0.8f, 0.8f),
            _ => Color.gray
        };
    }

    void StartPathAction()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

        if (hit.collider != null)
        {
            CellData cell = hit.collider.GetComponentInParent<CellData>();
            if (cell != null && cell.building != null && cell.building.isHeadquarters && cell.building.ownerId == 0)
            {
                isDraggingPath = true;
                pathStartCell = cell;
            }
        }
    }

    public void IniciarDesdeExplorador(UnidadExplorador explorador)
    {
        exploradorSeleccionado = explorador;
        pathStartCell = explorador.GetCeldaActual();
        isDraggingPath = true;
    }

    void EndPathAction()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

        if (hit.collider == null) return;

        CellData targetCell = hit.collider.GetComponentInParent<CellData>();
        if (targetCell == null) return;

        if (exploradorSeleccionado != null)
        {
            exploradorSeleccionado.ConvertirSegunDestino(targetCell);
            exploradorSeleccionado = null;
            isDraggingPath = false;
            return;
        }

        if (!isDraggingPath || pathStartCell == null) return;
        isDraggingPath = false;

        if (targetCell.hasResource && targetCell.resourceType == "Comida")
        {
            CreatePathVisual(pathStartCell, targetCell, true);
        }
        else
        {
            CreatePathVisual(pathStartCell, targetCell, false);
        }
    }


    public void CreatePathVisual(CellData fromCell, CellData toCell, bool isRecolectar)
    {
        if (pathLinePrefab == null || unidadPrefab == null) return;

        GameObject pathGO = Instantiate(pathLinePrefab);
        PathVisual visual = pathGO.GetComponent<PathVisual>();
        if (visual == null) return;

        Building fromBuilding = fromCell.building;
        bool esDesdeExplorador = exploradorSeleccionado != null;

        Vector3 from;
        Vector3 to;

        if (toCell.building != null)
        {
            from = GetEdgeExitPoint(fromBuilding, toCell.building);
            to = GetEdgeExitPoint(toCell.building, fromBuilding);
        }
        else
        {
            Vector3 toPos = toCell.transform.position;
            Vector3 fromCenter = Vector3.zero;

            if (fromBuilding != null)
            {
                foreach (var c in fromBuilding.occupiedCells)
                    fromCenter += c.transform.position;
                fromCenter /= fromBuilding.occupiedCells.Count;
            }
            else
            {
                fromCenter = fromCell.transform.position;
            }

            Vector3 dir = (toPos - fromCenter).normalized;
            float extraOffset = 0.6f;
            float exitDistance = Mathf.Sqrt(1f) * 0.5f + extraOffset;

            from = fromCenter + dir * exitDistance;
            to = toPos;
        }

        TipoCamino tipo = isRecolectar ? TipoCamino.Recolectar :
            (toCell.building != null && toCell.building.isHeadquarters && toCell.building.ownerId != 0)
            ? TipoCamino.Ataque : TipoCamino.Exploracion;

        visual.isRecolectar = (tipo == TipoCamino.Recolectar);

        List<Vector3> camino = new List<Vector3> { from, to };

        if (tipo == TipoCamino.Recolectar && fromBuilding is HeadquartersBuilding hqRecolector)
        {
            hqRecolector.RegisterActiveRecolector(from, camino, toCell);
        }
        else if (tipo == TipoCamino.Ataque && fromBuilding is HeadquartersBuilding hqAtacante)
        {
            hqAtacante.RegisterActiveSoldado(from, camino, toCell.building, 1);
        }
        else if (tipo == TipoCamino.Exploracion)
        {
            visual.Init(from, to, fromBuilding, null, gridGenerator, toCell);

            if (exploradorSeleccionado == null)
            {
                GameObject unidad = Instantiate(unidadPrefab, from, Quaternion.identity);
                unidad.name = "Explorador";

                var comp = unidad.AddComponent<UnidadExplorador>();
                comp.Init(fromBuilding as HeadquartersBuilding, from, to, toCell, 1);
                comp.AgregarTramo(visual);
                exploradorSeleccionado = comp;

                CaminoActivo caminoActivo = new CaminoActivo(false, false, null, toCell);
                caminoActivo.tramos.Add(visual);
                caminoActivo.tramosParent = pathGO;
                caminoActivo.unidadesVinculadas.Add(unidad);

                if (fromBuilding is HeadquartersBuilding hq)
                    hq.GetCaminosActivos().Add(caminoActivo);
            }
            else
            {
                exploradorSeleccionado.caminoRecorrido.Add(to);
                exploradorSeleccionado.AgregarTramo(visual);

                if (exploradorSeleccionado.origen != null)
                {
                    var caminoActivo = exploradorSeleccionado.origen.GetCaminosActivos()
                        .Find(c => c.unidadesVinculadas.Contains(exploradorSeleccionado.gameObject));
                    if (caminoActivo != null)
                        caminoActivo.tramos.Add(visual);
                }
            }
        }
        else if (exploradorSeleccionado != null)
        {
            exploradorSeleccionado.inicio = exploradorSeleccionado.transform.position;
            exploradorSeleccionado.destino = toCell.transform.position;
            exploradorSeleccionado.celdaActual = toCell;
            exploradorSeleccionado.t = 0f;

            exploradorSeleccionado.esperandoInput = false;
            exploradorSeleccionado.caminoRecorrido.Add(toCell.transform.position);
            exploradorSeleccionado = null;
        }
    }



    Vector3 GetEdgeExitPoint(Building from, Building to)
    {
        Vector3 fromCenter = Vector3.zero;
        foreach (var cell in from.occupiedCells)
            fromCenter += cell.transform.position;
        fromCenter /= from.occupiedCells.Count;

        if (to == null || to.occupiedCells == null || to.occupiedCells.Count == 0)
            return fromCenter;

        Vector3 toCenter = Vector3.zero;
        foreach (var cell in to.occupiedCells)
            toCenter += cell.transform.position;
        toCenter /= to.occupiedCells.Count;

        Vector3 direction = (toCenter - fromCenter).normalized;
        float extraOffset = 0.6f;
        float exitDistance = Mathf.Sqrt(from.occupiedCells.Count) * 0.5f + extraOffset;

        return fromCenter + direction * exitDistance;
    }

    void ToggleCellLabels(bool visible)
    {
        foreach (var cell in gridGenerator.allCells.Values)
            cell.ShowLabel(visible);
    }

    void ApplyTestColors()
    {
        if (!enableTestMode) return;

        foreach (var cell in gridGenerator.allCells.Values)
            cell.ApplyDebugColor();
    }

    void DetectCellClick()
    {
        if (!enableTestMode) return;

        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

        if (hit.collider != null)
        {
            CellData cell = hit.collider.GetComponentInParent<CellData>();
            if (cell != null)
            {
                ClearSelection();
                if (cell.building != null)
                {
                    HighlightBuildingOutline(cell.building);
                    cell.building.PrintInfo();
                }
                else
                {
                    HighlightCell(cell);
                }
            }
        }
    }

    void HighlightCell(CellData cell)
    {
        if (!enableTestMode) return;

        cell.ClearAllBorders();
        Color color = cell.isWalkable ? walkableCellBorderColor : nonWalkableCellBorderColor;

        cell.ShowBorder("top", true, color);
        cell.ShowBorder("bottom", true, color);
        cell.ShowBorder("left", true, color);
        cell.ShowBorder("right", true, color);

        selectedCells.Add(cell);
    }

    void HighlightBuildingOutline(Building building)
    {
        if (!enableTestMode) return;

        foreach (var cell in building.occupiedCells)
        {
            cell.ClearAllBorders();
            Vector2Int pos = cell.coordinates;

            bool hasTop = building.occupiedCells.Exists(c => c.coordinates == pos + Vector2Int.up);
            bool hasBottom = building.occupiedCells.Exists(c => c.coordinates == pos + Vector2Int.down);
            bool hasLeft = building.occupiedCells.Exists(c => c.coordinates == pos + Vector2Int.left);
            bool hasRight = building.occupiedCells.Exists(c => c.coordinates == pos + Vector2Int.right);

            Color borderColor = buildingBorderColor;

            if (!hasTop) cell.ShowBorder("top", true, borderColor);
            if (!hasBottom) cell.ShowBorder("bottom", true, borderColor);
            if (!hasLeft) cell.ShowBorder("left", true, borderColor);
            if (!hasRight) cell.ShowBorder("right", true, borderColor);

            selectedCells.Add(cell);
        }
    }

    void ClearSelection()
    {
        if (!enableTestMode) return;

        foreach (var cell in selectedCells)
            cell.ClearAllBorders();

        selectedCells.Clear();
    }



    public void PlaceTestBuilding()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (sceneName == "Pantalla1")
        {
            HeadquartersBuilding playerHQ = new HeadquartersBuilding("Cuartel Azul", 0);
            Vector2Int[] playerPositions = new Vector2Int[]
            {
            new Vector2Int(1, 1), new Vector2Int(1, 2),
            new Vector2Int(2, 1), new Vector2Int(2, 2)
            };

            foreach (var pos in playerPositions)
            {
                CellData cell = gridGenerator.GetCellAt(pos.x, pos.y);
                if (cell != null)
                {
                    cell.building = playerHQ;
                    cell.isWalkable = false;
                    cell.hasResource = false;
                    playerHQ.occupiedCells.Add(cell);
                }
            }

            playerHQ.FinalizeSetup();
            foreach (var cell in playerHQ.occupiedCells)
                cell.ApplyDebugColor();

            HeadquartersBuilding enemyHQ = new HeadquartersBuilding("Cuartel Rojo", 1);
            int maxX = gridGenerator.width - 1;
            int maxY = gridGenerator.height - 1;

            Vector2Int[] enemyPositions = new Vector2Int[]
            {
            new Vector2Int(maxX - 2, maxY - 2), new Vector2Int(maxX - 2, maxY - 1),
            new Vector2Int(maxX - 1, maxY - 2), new Vector2Int(maxX - 1, maxY - 1)
            };

            foreach (var pos in enemyPositions)
            {
                CellData cell = gridGenerator.GetCellAt(pos.x, pos.y);
                if (cell != null)
                {
                    cell.building = enemyHQ;
                    cell.isWalkable = false;
                    cell.hasResource = false;
                    enemyHQ.occupiedCells.Add(cell);
                }
            }

            enemyHQ.FinalizeSetup();
            foreach (var cell in enemyHQ.occupiedCells)
                cell.ApplyDebugColor();

            CellData celdaComida = gridGenerator.GetCellAt(1, 7);
            if (celdaComida != null)
            {
                celdaComida.hasResource = true;
                celdaComida.resourceType = "Comida";
                celdaComida.resourceAmount = 2;
                celdaComida.ApplyDebugColor();
            }

            Vector2Int[] obstaculos = new Vector2Int[]
            {
            new Vector2Int(4, 4), new Vector2Int(4, 5),
            new Vector2Int(5, 4), new Vector2Int(5, 5)
            };

            foreach (var pos in obstaculos)
            {
                CellData cell = gridGenerator.GetCellAt(pos.x, pos.y);
                if (cell != null)
                {
                    cell.isWalkable = false;
                    cell.ApplyDebugColor();
                }
            }

            HeadquartersBuilding fuerteNeutral = new HeadquartersBuilding("Fuerte Neutral", 10, true);
            CellData celdaFuerte = gridGenerator.GetCellAt(8, 1);
            if (celdaFuerte != null)
            {
                celdaFuerte.building = fuerteNeutral;
                celdaFuerte.isWalkable = false;
                celdaFuerte.hasResource = false;
                fuerteNeutral.occupiedCells.Add(celdaFuerte);
            }

            fuerteNeutral.soldierCount = 5;
            fuerteNeutral.FinalizeSetup();
            foreach (var cell in fuerteNeutral.occupiedCells)
                cell.ApplyDebugColor();
        }
        else if (sceneName == "Pantalla2")
        {
            // Línea infranqueable en x = 5, excepto (5,5) y (5,6)
            for (int y = 0; y < gridGenerator.height; y++)
            {
                if (y == 5 || y == 6) continue; // dejar (5,5) y (5,6) libres
                CellData cell = gridGenerator.GetCellAt(5, y);
                if (cell != null)
                {
                    cell.isWalkable = false;
                    cell.ApplyDebugColor();
                }
            }

            // HQ jugador (2x2) en (1,4), (1,5), (2,4), (2,5)
            HeadquartersBuilding playerHQ = new HeadquartersBuilding("Cuartel Azul", 0);
            Vector2Int[] playerHQPos = new Vector2Int[]
            {
        new Vector2Int(1, 4), new Vector2Int(1, 5),
        new Vector2Int(2, 4), new Vector2Int(2, 5)
            };
            foreach (var pos in playerHQPos)
            {
                CellData cell = gridGenerator.GetCellAt(pos.x, pos.y);
                if (cell != null)
                {
                    cell.building = playerHQ;
                    cell.isWalkable = false;
                    playerHQ.occupiedCells.Add(cell);
                }
            }
            playerHQ.FinalizeSetup();
            foreach (var cell in playerHQ.occupiedCells)
                cell.ApplyDebugColor();

            // Recursos jugador en (1,1) y (1,8)
            Vector2Int[] playerRecursos = new Vector2Int[]
            {
        new Vector2Int(1, 1), new Vector2Int(1, 8)
            };
            foreach (var pos in playerRecursos)
            {
                CellData cell = gridGenerator.GetCellAt(pos.x, pos.y);
                if (cell != null)
                {
                    cell.hasResource = true;
                    cell.resourceType = "Comida";
                    cell.resourceAmount = 1;
                    cell.ApplyDebugColor();
                }
            }

            // HQ enemigo (2x2) en (7,4), (7,5), (8,4), (8,5)
            HeadquartersBuilding enemyHQ = new HeadquartersBuilding("Cuartel Rojo", 1);
            Vector2Int[] enemyHQPos = new Vector2Int[]
            {
        new Vector2Int(7, 4), new Vector2Int(7, 5),
        new Vector2Int(8, 4), new Vector2Int(8, 5)
            };
            foreach (var pos in enemyHQPos)
            {
                CellData cell = gridGenerator.GetCellAt(pos.x, pos.y);
                if (cell != null)
                {
                    cell.building = enemyHQ;
                    cell.isWalkable = false;
                    enemyHQ.occupiedCells.Add(cell);
                }
            }
            enemyHQ.FinalizeSetup();
            foreach (var cell in enemyHQ.occupiedCells)
                cell.ApplyDebugColor();

            // Recursos enemigo en (8,1) y (8,8)
            Vector2Int[] enemyRecursos = new Vector2Int[]
            {
        new Vector2Int(8, 1), new Vector2Int(8, 8)
            };
            foreach (var pos in enemyRecursos)
            {
                CellData cell = gridGenerator.GetCellAt(pos.x, pos.y);
                if (cell != null)
                {
                    cell.hasResource = true;
                    cell.resourceType = "Comida";
                    cell.resourceAmount = 1;
                    cell.ApplyDebugColor();
                }
            }
        }
        else if (sceneName == "Pantalla3")
        {
            // === JUGADOR (abajo centro) ===
            HeadquartersBuilding jugadorHQ = new HeadquartersBuilding("Cuartel Azul", 0);
            Vector2Int[] jugadorPos = new Vector2Int[]
            {
        new Vector2Int(4, 1), new Vector2Int(4, 2),
        new Vector2Int(5, 1), new Vector2Int(5, 2)
            };
            foreach (var pos in jugadorPos)
            {
                var cell = gridGenerator.GetCellAt(pos.x, pos.y);
                cell.building = jugadorHQ;
                cell.isWalkable = false;
                jugadorHQ.occupiedCells.Add(cell);
            }
            jugadorHQ.FinalizeSetup();
            foreach (var c in jugadorHQ.occupiedCells) c.ApplyDebugColor();

            // Fuerte del jugador (una celda más arriba → 4,5)
            HeadquartersBuilding fuerteJugador = new HeadquartersBuilding("Fuerte Azul", 10, true);
            CellData fj = gridGenerator.GetCellAt(4, 5);
            fj.building = fuerteJugador;
            fj.isWalkable = false;
            fuerteJugador.occupiedCells.Add(fj);
            fuerteJugador.soldierCount = 5;
            fuerteJugador.FinalizeSetup();
            fj.ApplyDebugColor();

            // Recursos jugador (2 celdas a izquierda y derecha del HQ)
            Vector2Int[] recursosJugador = new Vector2Int[] { new Vector2Int(2, 1), new Vector2Int(7, 1) };
            foreach (var pos in recursosJugador)
            {
                var cell = gridGenerator.GetCellAt(pos.x, pos.y);
                cell.hasResource = true;
                cell.resourceType = "Comida";
                cell.resourceAmount = 1;
                cell.ApplyDebugColor();
            }

            // === ENEMIGO 1 (arriba izquierda, subido 1 celda) ===
            HeadquartersBuilding enemigo1HQ = new HeadquartersBuilding("Cuartel Rojo", 1);
            Vector2Int[] enemigo1Pos = new Vector2Int[]
            {
        new Vector2Int(1, 7), new Vector2Int(1, 8),
        new Vector2Int(2, 7), new Vector2Int(2, 8)
            };
            foreach (var pos in enemigo1Pos)
            {
                var cell = gridGenerator.GetCellAt(pos.x, pos.y);
                cell.building = enemigo1HQ;
                cell.isWalkable = false;
                enemigo1HQ.occupiedCells.Add(cell);
            }
            enemigo1HQ.FinalizeSetup();
            foreach (var c in enemigo1HQ.occupiedCells) c.ApplyDebugColor();

            // Fuerte enemigo1 (una celda más abajo → 1,4)
            HeadquartersBuilding fuerte1 = new HeadquartersBuilding("Fuerte Rojo", 10, true);
            CellData f1 = gridGenerator.GetCellAt(1, 4);
            f1.building = fuerte1;
            f1.isWalkable = false;
            fuerte1.occupiedCells.Add(f1);
            fuerte1.soldierCount = 5;
            fuerte1.FinalizeSetup();
            f1.ApplyDebugColor();

            // Recurso enemigo1 (a la derecha, +1 distancia = 4,9)
            CellData r1 = gridGenerator.GetCellAt(4, 9);
            r1.hasResource = true;
            r1.resourceType = "Comida";
            r1.resourceAmount = 1;
            r1.ApplyDebugColor();

            // === ENEMIGO 2 (arriba derecha, subido 1 celda) ===
            HeadquartersBuilding enemigo2HQ = new HeadquartersBuilding("Cuartel Magenta", 2);
            Vector2Int[] enemigo2Pos = new Vector2Int[]
            {
        new Vector2Int(7, 7), new Vector2Int(7, 8),
        new Vector2Int(8, 7), new Vector2Int(8, 8)
            };
            foreach (var pos in enemigo2Pos)
            {
                var cell = gridGenerator.GetCellAt(pos.x, pos.y);
                cell.building = enemigo2HQ;
                cell.isWalkable = false;
                enemigo2HQ.occupiedCells.Add(cell);
            }
            enemigo2HQ.FinalizeSetup();
            foreach (var c in enemigo2HQ.occupiedCells) c.ApplyDebugColor();

            // Fuerte enemigo2 (una celda más abajo → 8,4)
            HeadquartersBuilding fuerte2 = new HeadquartersBuilding("Fuerte Magenta", 10, true);
            CellData f2 = gridGenerator.GetCellAt(8, 4);
            f2.building = fuerte2;
            f2.isWalkable = false;
            fuerte2.occupiedCells.Add(f2);
            fuerte2.soldierCount = 5;
            fuerte2.FinalizeSetup();
            f2.ApplyDebugColor();

            // Recurso enemigo2 (a la izquierda, +1 distancia = 5,9)
            CellData r2 = gridGenerator.GetCellAt(5, 9);
            r2.hasResource = true;
            r2.resourceType = "Comida";
            r2.resourceAmount = 1;
            r2.ApplyDebugColor();
        }




    }

    public void MostrarMensajeResultado(string texto)
    {
        if (uiResultadoPanel != null && uiResultadoTexto != null)
        {
            uiResultadoPanel.SetActive(true);
            uiResultadoTexto.text = texto;
        }
    }

    public void IniciarTransicionEscena(string nombreEscena, float delay)
    {
        StartCoroutine(TransicionEscena(nombreEscena, delay));
    }

    private IEnumerator TransicionEscena(string nombreEscena, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(nombreEscena);
    }

    public void IrASiguientePantalla(float delay)
    {
        string actual = SceneManager.GetActiveScene().name;
        string siguiente = "";

        if (actual == "Pantalla1") siguiente = "Pantalla2";
        else if (actual == "Pantalla2") siguiente = "Pantalla3";
        else return;

        IniciarTransicionEscena(siguiente, delay);
    }

}