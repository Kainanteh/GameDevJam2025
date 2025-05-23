using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private HeadquartersBuilding enemyHQ;
    private HeadquartersBuilding fuerteNeutral;

    void Start()
    {
        Invoke(nameof(LanzarAtaqueAlFuerte), 2f);
    }

    void LanzarAtaqueAlFuerte()
    {
        GridGenerator grid = GameManager.Instance.gridGenerator;

        foreach (var cell in grid.allCells.Values)
        {
            if (cell.building is HeadquartersBuilding hq && hq.ownerId == 1)
            {
                enemyHQ = hq;
                break;
            }
        }

        foreach (var cell in grid.allCells.Values)
        {
            if (cell.building is HeadquartersBuilding hq && hq.ownerId == 10 && hq.type == BuildingType.FuerteNeutral)
            {
                fuerteNeutral = hq;
                break;
            }
        }

        if (enemyHQ == null || fuerteNeutral == null)
        {
            Debug.LogWarning("❌ No se encontró HQ enemigo o FuerteNeutral");
            return;
        }

        Vector3 hasta = fuerteNeutral.occupiedCells[0].transform.position;

        // Buscar una celda libre adyacente al HQ enemigo
        Vector3 desde = Vector3.zero;
        bool encontrado = false;

        foreach (var cell in enemyHQ.occupiedCells)
        {
            Vector2Int[] offsets = new Vector2Int[]
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };

            foreach (var offset in offsets)
            {
                Vector2Int adyacenteCoord = cell.coordinates + offset;
                CellData adyacente = grid.GetCellAt(adyacenteCoord.x, adyacenteCoord.y);
                if (adyacente != null && adyacente.isWalkable)
                {
                    desde = adyacente.transform.position;
                    encontrado = true;
                    break;
                }
            }

            if (encontrado) break;
        }

        if (!encontrado)
        {
            Debug.LogWarning("❌ No se encontró celda caminable adyacente al HQ enemigo");
            return;
        }

        List<Vector3> camino = new List<Vector3> { desde, hasta };
        enemyHQ.RegisterActiveSoldado(desde, camino, fuerteNeutral, 1);

        
        //Debug.Log("🤖 IA enemiga ha lanzado un ataque al fuerte neutral");
    }
}
