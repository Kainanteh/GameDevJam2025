using UnityEngine;
using System.Collections.Generic;
using TMPro;

public enum TipoCamino
{
    Ataque,
    Recolectar,
    Exploracion
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
    public Color obstacleColor = new Color(0f, 0.3f, 0f); // Verde oscuro


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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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
            }
        }


        if (Input.GetMouseButtonUp(0))
        {
            EndPathAction();
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
        pathStartCell = explorador.GetCeldaActual(); // ← esto no puede ser null
        isDraggingPath = true;
    }

    void EndPathAction()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

        if (hit.collider == null) return;

        CellData targetCell = hit.collider.GetComponentInParent<CellData>();
        if (targetCell == null) return;

        // 🧭 Explorador activo: tiene prioridad absoluta
        if (exploradorSeleccionado != null)
        {
            Debug.Log("🧭 EndPathAction con explorador activo");
            if (targetCell.hasResource && targetCell.resourceType == "Comida")
            {
                exploradorSeleccionado.ConvertirseEnRecolector(targetCell);
            }
            else if (targetCell.building != null && targetCell.building.ownerId != 0)
            {
                exploradorSeleccionado.ConvertirseEnAtaque(targetCell.building);
            }
            else
            {
                CreatePathVisual(exploradorSeleccionado.GetCeldaActual(), targetCell, false);
            }

            exploradorSeleccionado = null;
            isDraggingPath = false;
            return;
        }

        // 🧱 Flujo normal desde HQ
        if (!isDraggingPath || pathStartCell == null) return;
        isDraggingPath = false;

        if (targetCell.hasResource && targetCell.resourceType == "Comida")
        {
            Debug.Log("Recolectar comida");
            CreatePathVisual(pathStartCell, targetCell, true);
        }
        else if (targetCell.building != null)
        {
            Debug.Log(targetCell.building.ownerId == 0 || targetCell.building.ownerId == -1 ? "Refuerzo" : "Ataque");
            CreatePathVisual(pathStartCell, targetCell, false);
        }
        else
        {
            Debug.Log("Exploración a celda libre");
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

        bool esDesdeExplorador = GameManager.Instance.exploradorSeleccionado != null;

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

        visual.Init(from, to, fromBuilding, toCell.building, gridGenerator, toCell);

        TipoCamino tipo;
        if (isRecolectar)
            tipo = TipoCamino.Recolectar;
        else if (toCell.building != null)
            tipo = TipoCamino.Ataque;
        else
            tipo = TipoCamino.Exploracion;

        visual.isRecolectar = (tipo == TipoCamino.Recolectar);

        if (fromBuilding is HeadquartersBuilding hq)
        {
            hq.RegistrarCaminoPorTipo(from, to, toCell.building, tipo, toCell);
        }
        else if (GameManager.Instance.exploradorSeleccionado != null)
        {
            Debug.Log("🔁 Ejecutando desde explorador activo");
            GameManager.Instance.exploradorSeleccionado.inicio = GameManager.Instance.exploradorSeleccionado.transform.position;
            GameManager.Instance.exploradorSeleccionado.destino = toCell.transform.position;
            GameManager.Instance.exploradorSeleccionado.celdaActual = toCell;
            GameManager.Instance.exploradorSeleccionado.t = 0f;

            GameManager.Instance.exploradorSeleccionado.esperandoInput = false;
            GameManager.Instance.exploradorSeleccionado = null;
        }
        else
        {
            Debug.Log("❌ Camino cancelado: no hay building y no es explorador");
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


        CellData celdaComida = gridGenerator.GetCellAt(1, 7);
        if (celdaComida != null)
        {
            celdaComida.hasResource = true;
            celdaComida.resourceType = "Comida";
            celdaComida.resourceAmount = 1;
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
                cell.ApplyDebugColor(); // para que se vea
            }
        }




    }
}