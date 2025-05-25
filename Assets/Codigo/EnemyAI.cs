using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class EnemyAI : MonoBehaviour
{
    private HeadquartersBuilding enemyHQ;
    private HeadquartersBuilding playerHQ;
    private HeadquartersBuilding fuerteNeutral;
    private CellData celdaRecurso;

    private bool jugadorAmenazaFuerte = false;
    private bool recursoRecolectado = false;
    private bool recolectorEnviado = false;
    private bool refuerzosEnviados = false;
    private bool fuerteConquistado = false;
    private bool intentoFallido = false;

    bool recolectorLanzado1 = false;
    bool ataque1Fuerte1 = false;
    bool ataque1Fuerte2 = false;
    bool ataque1Jugador = false;
    bool ataque1Fuerte3 = false;
    bool ataque1HQEnemigo = false;

    bool recolectorLanzado2 = false;
    bool ataque2Fuerte1 = false;
    bool ataque2Fuerte2 = false;
    bool ataque2Jugador = false;
    bool ataque2Fuerte3 = false;
    bool ataque2HQEnemigo = false;

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "Pantalla1")
            StartCoroutine(IA_Pantalla1());
        if (sceneName == "Pantalla2")
            StartCoroutine(IA_Pantalla2());
        if (sceneName == "Pantalla3")
            StartCoroutine(IA_Pantalla3());

    }

    IEnumerator<WaitForSeconds> IA_Pantalla1()
    {
        yield return new WaitForSeconds(2f);

        GridGenerator grid = GameManager.Instance.gridGenerator;

        foreach (var cell in grid.allCells.Values)
        {
            if (cell.building is HeadquartersBuilding hq)
            {
                if (hq.ownerId == 1)
                    enemyHQ = hq;
                else if (hq.ownerId == 0)
                    playerHQ = hq;
                else if (hq.ownerId == 10 && hq.type == BuildingType.FuerteNeutral)
                    fuerteNeutral = hq;
            }

            if (cell.hasResource && cell.resourceType == "Comida")
                celdaRecurso = cell;
        }

        if (enemyHQ == null || playerHQ == null || fuerteNeutral == null || celdaRecurso == null)
        {
            Debug.LogWarning("❌ Faltan elementos clave para IA en Pantalla1");
            yield break;
        }

        LanzarAtaqueAlFuerte();

        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (!jugadorAmenazaFuerte)
            {
                foreach (var camino in playerHQ.GetCaminosActivos())
                {
                    if (camino.isAtaque && camino.objetivo == fuerteNeutral)
                    {
                        jugadorAmenazaFuerte = true;
                        break;
                    }
                }
            }

            if (jugadorAmenazaFuerte && !recolectorEnviado)
            {
                recolectorEnviado = true;
                LanzarRecolectorAlRecurso();
            }

            if (jugadorAmenazaFuerte && !recursoRecolectado && celdaRecurso.resourceAlreadyCollected)
            {
                recursoRecolectado = true;
            }

            if (fuerteNeutral.ownerId == 11 && !refuerzosEnviados)
            {
                refuerzosEnviados = true;
                LanzarSegundoCaminoSiFuerteEnDisputa();
            }

            if (!fuerteConquistado && fuerteNeutral.ownerId == 1)
            {
                fuerteConquistado = true;
                LanzarAtaqueDesdeFuerteAlHQJugador();
            }

            if (!fuerteConquistado && refuerzosEnviados && fuerteNeutral.ownerId != 1 && fuerteNeutral.ownerId != 11 && !intentoFallido)
            {
                intentoFallido = true;
                CancelarAtaquesAFuerte();
                LanzarAtaqueDirectoAlHQJugador();
            }
        }
    }

    IEnumerator<WaitForSeconds> IA_Pantalla2()
    {
        GridGenerator grid = GameManager.Instance.gridGenerator;
        CellData recursoArriba = null;
        CellData recursoAbajo = null;

        foreach (var cell in grid.allCells.Values)
        {
            if (cell.building is HeadquartersBuilding hq)
            {
                if (hq.ownerId == 1)
                    enemyHQ = hq;
                else if (hq.ownerId == 0)
                    playerHQ = hq;
            }

            if (cell.hasResource && cell.resourceType == "Comida" && cell.coordinates.x > 5)
            {
                if (cell.coordinates.y <= 4)
                    recursoAbajo = cell;
                else
                    recursoArriba = cell;
            }
        }

        if (enemyHQ == null || playerHQ == null || recursoArriba == null || recursoAbajo == null)
        {
            Debug.LogWarning("❌ Faltan elementos clave para IA en Pantalla2");
            yield break;
        }

        HashSet<Vector2Int> usadas = new();

        yield return new WaitForSeconds(10f);
        Vector3 desde1 = GetCeldaAdyacenteLibre(enemyHQ, usadas);
        usadas.Add(Vector2Int.RoundToInt(desde1));
        LanzarRecolectorPantalla2(recursoArriba);

        yield return new WaitForSeconds(5f);
        LanzarAtaquePantalla2(new Vector2Int(5, 5), desde1);

        yield return new WaitForSeconds(5f);
        Vector3 desde2 = GetCeldaAdyacenteLibre(enemyHQ, usadas);
        usadas.Add(Vector2Int.RoundToInt(desde2));
        LanzarRecolectorPantalla2(recursoAbajo);

        yield return new WaitForSeconds(10f);
        LanzarAtaquePantalla2(new Vector2Int(5, 6), desde2);
    }



    IEnumerator IA_Pantalla3()
    {
        GridGenerator grid = GameManager.Instance.gridGenerator;

        HeadquartersBuilding enemigo1 = null;
        HeadquartersBuilding enemigo2 = null;
        HeadquartersBuilding jugador = null;
        List<HeadquartersBuilding> fuertes = new();
        List<CellData> recursos = new();

        foreach (var cell in grid.allCells.Values)
        {
            if (cell.building is HeadquartersBuilding hq)
            {
                if (hq.ownerId == 0) jugador = hq;
                else if (hq.ownerId == 1) enemigo1 = hq;
                else if (hq.ownerId == 2) enemigo2 = hq;
                else if (hq.type == BuildingType.FuerteNeutral)
                    fuertes.Add(hq);
            }

            if (cell.hasResource && cell.resourceType == "Comida")
                recursos.Add(cell);
        }

        if (enemigo1 == null || enemigo2 == null || jugador == null || fuertes.Count < 3 || recursos.Count < 2)
        {
            Debug.LogWarning("⚠️ IA Pantalla3: faltan elementos");
            yield break;
        }

        // Recursos y fuertes más cercanos
        CellData recurso1 = recursos.OrderBy(r => Vector2Int.Distance(r.coordinates, enemigo1.occupiedCells[0].coordinates)).First();
        CellData recurso2 = recursos.OrderBy(r => Vector2Int.Distance(r.coordinates, enemigo2.occupiedCells[0].coordinates)).First();
        List<HeadquartersBuilding> fuertes1 = fuertes.OrderBy(f => Vector2Int.Distance(f.occupiedCells[0].coordinates, enemigo1.occupiedCells[0].coordinates)).ToList();
        List<HeadquartersBuilding> fuertes2 = fuertes.OrderBy(f => Vector2Int.Distance(f.occupiedCells[0].coordinates, enemigo2.occupiedCells[0].coordinates)).ToList();

        CellData recursoExtra1 = recurso2;
        CellData recursoExtra2 = recurso1;

        StartCoroutine(IA_Enemigo(enemigo1, jugador, enemigo2, recurso1, recursoExtra1, fuertes1, true, 0f));
        StartCoroutine(IA_Enemigo(enemigo2, jugador, enemigo1, recurso2, recursoExtra2, fuertes2, false, 1f));
        yield break;
    }


    IEnumerator IA_Enemigo(
    HeadquartersBuilding hq,
    HeadquartersBuilding jugador,
    HeadquartersBuilding otroEnemigo,
    CellData recursoPropio,
    CellData recursoExtra,
    List<HeadquartersBuilding> fuertes,
    bool derecha,
    float delay)
    {
        Debug.Log($"🧠 IA Enemigo {hq.ownerId} inicializada con delay {delay}");

        yield return new WaitForSeconds(10f + delay);

        // Recolector
        Vector3 desdeRecolector;
        if (derecha)
        {
            Vector2Int derechaCoord = hq.occupiedCells[0].coordinates + new Vector2Int(2, 0);
            desdeRecolector = GameManager.Instance.gridGenerator.GetCellAt(derechaCoord.x, derechaCoord.y)?.transform.position ?? GetCeldaAdyacenteLibre(hq);
        }
        else
        {
            desdeRecolector = GetCeldaAdyacenteLibre(hq);
        }

        Debug.Log($"🚀 Recolector desde {desdeRecolector} hacia {recursoPropio.coordinates}");
        hq.RegisterActiveRecolector(desdeRecolector, new List<Vector3> { desdeRecolector, recursoPropio.transform.position }, recursoPropio);

        yield return new WaitForSeconds(5f);

        Vector3 desdeHQ = GetCeldaAdyacenteLibre(hq);
        var fuerte1 = fuertes[0];
        Debug.Log($"⚔️ Ataque a fuerte 1 (ownerId={fuerte1.ownerId})");
        hq.RegisterActiveSoldado(desdeHQ, new List<Vector3> { desdeHQ, fuerte1.occupiedCells[0].transform.position }, fuerte1, hq.ownerId);

        yield return new WaitForSeconds(5f);

        var fuerte2 = fuertes[1];
        Debug.Log($"⚔️ Ataque a fuerte 2 (ownerId={fuerte2.ownerId})");
        hq.RegisterActiveSoldado(desdeHQ, new List<Vector3> { desdeHQ, fuerte2.occupiedCells[0].transform.position }, fuerte2, hq.ownerId);

        yield return new WaitForSeconds(5f);

        var fuerteActivo = fuertes.FindLast(f => f.ownerId == hq.ownerId) ?? hq;
        Vector3 desdeFuerte = GetCeldaAdyacenteLibre(fuerteActivo);
        Debug.Log($"⚔️ Ataque al jugador desde fuerte (ownerId={fuerteActivo.ownerId})");
        hq.RegisterActiveSoldado(desdeFuerte, new List<Vector3> { desdeFuerte, jugador.occupiedCells[0].transform.position }, jugador, hq.ownerId);

        yield return new WaitForSeconds(5f);

        var fuerte3 = fuertes[2];
        Debug.Log($"⚔️ Ataque a fuerte 3 (ownerId={fuerte3.ownerId})");
        hq.RegisterActiveSoldado(desdeFuerte, new List<Vector3> { desdeFuerte, fuerte3.occupiedCells[0].transform.position }, fuerte3, hq.ownerId);

        yield return new WaitForSeconds(5f);

        Debug.Log($"⚔️ Ataque al HQ enemigo {otroEnemigo.ownerId}");
        hq.RegisterActiveSoldado(desdeFuerte, new List<Vector3> { desdeFuerte, otroEnemigo.occupiedCells[0].transform.position }, otroEnemigo, hq.ownerId);

        yield return new WaitForSeconds(5f);

        Vector3 desdeExtra = GetCeldaAdyacenteLibre(fuerteActivo);
        Debug.Log($"🥷 Recolector extra al recurso en {recursoExtra.coordinates}");
        hq.RegisterActiveRecolector(desdeExtra, new List<Vector3> { desdeExtra, recursoExtra.transform.position }, recursoExtra);
    }









    void LanzarRecolectorPantalla2(CellData recurso)
    {
        Vector3 desde = GetCeldaAdyacenteLibre(enemyHQ);
        Vector3 hasta = recurso.transform.position;

        if (desde != Vector3.zero)
            enemyHQ.RegisterActiveRecolector(desde, new List<Vector3> { desde, hasta }, recurso);
    }

    void LanzarAtaquePantalla2(Vector2Int puntoMedio, Vector3 desde)
    {
        Vector3 hasta = GetCeldaAdyacenteLibre(playerHQ);
        CellData punto = GameManager.Instance.gridGenerator.GetCellAt(puntoMedio.x, puntoMedio.y);

        if (desde != Vector3.zero && hasta != Vector3.zero && punto != null)
        {
            List<Vector3> camino = new List<Vector3> { desde, punto.transform.position, hasta };
            enemyHQ.RegisterActiveSoldado(desde, camino, playerHQ, 1);
        }
    }






    void LanzarAtaqueAlFuerte()
    {
        Vector3 desde = GetCeldaAdyacenteLibre(enemyHQ);
        Vector3 hasta = fuerteNeutral.occupiedCells[0].transform.position;

        if (desde != Vector3.zero)
            enemyHQ.RegisterActiveSoldado(desde, new List<Vector3> { desde, hasta }, fuerteNeutral, 1);
    }

    void LanzarSegundoCaminoSiFuerteEnDisputa()
    {
        Vector3 desde = GetCeldaAdyacenteLibre(enemyHQ);
        Vector3 hasta = fuerteNeutral.occupiedCells[0].transform.position;

        if (desde == Vector3.zero) return;

        Vector3 medio = (desde + hasta) * 0.5f;
        Vector3 offset = new Vector3(-1.5f, 0.5f, 0f);
        Vector3 desvio = medio + offset;

        List<Vector3> camino = new List<Vector3> { desde, desvio, hasta };

        CaminoActivo caminoActivo = new CaminoActivo(false, true, fuerteNeutral, null);
        GameObject grupoVisual = new GameObject("RefuerzoDisputa");
        grupoVisual.transform.position = Vector3.zero;
        caminoActivo.tramosParent = grupoVisual;

        for (int i = 0; i < camino.Count - 1; i++)
        {
            GameObject tramo = Object.Instantiate(GameManager.Instance.pathLinePrefab, grupoVisual.transform);
            PathVisual visual = tramo.GetComponent<PathVisual>();
            visual.Init(camino[i], camino[i + 1], enemyHQ, fuerteNeutral, GameManager.Instance.gridGenerator);
            visual.isRecolectar = false;
            caminoActivo.tramos.Add(visual);
        }

        enemyHQ.GetCaminosActivos().Add(caminoActivo);
    }

    void LanzarRecolectorAlRecurso()
    {
        Vector3 desde = GetCeldaAdyacenteLibre(enemyHQ);
        Vector3 hasta = celdaRecurso.transform.position;

        if (desde != Vector3.zero)
            enemyHQ.RegisterActiveRecolector(desde, new List<Vector3> { desde, hasta }, celdaRecurso);
    }

    void LanzarAtaqueDesdeFuerteAlHQJugador()
    {
        Vector3 desde = GetCeldaAdyacenteLibre(fuerteNeutral);
        Vector3 hasta = playerHQ.occupiedCells[0].transform.position;

        if (desde != Vector3.zero)
            fuerteNeutral.RegisterActiveSoldado(desde, new List<Vector3> { desde, hasta }, playerHQ, 1);
    }

    void LanzarAtaqueDirectoAlHQJugador()
    {
        Vector3 desde = GetCeldaAdyacenteLibre(enemyHQ);
        Vector3 hasta = playerHQ.occupiedCells[0].transform.position;

        if (desde != Vector3.zero)
            enemyHQ.RegisterActiveSoldado(desde, new List<Vector3> { desde, hasta }, playerHQ, 1);
    }

    void CancelarAtaquesAFuerte()
    {
        var caminos = enemyHQ.GetCaminosActivos();
        caminos.RemoveAll(c =>
        {
            if (c.objetivo == fuerteNeutral)
            {
                foreach (var u in c.unidadesVinculadas)
                    if (u != null) Destroy(u);
                foreach (var t in c.tramos)
                    if (t != null) Destroy(t.gameObject);
                if (c.tramosParent != null) Destroy(c.tramosParent);
                return true;
            }
            return false;
        });
    }

    Vector3 GetCeldaAdyacenteLibre(HeadquartersBuilding hq, HashSet<Vector2Int> yaUsadas = null)
    {
        foreach (var cell in hq.occupiedCells)
        {
            Vector2Int[] offsets = new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };

            foreach (var offset in offsets)
            {
                Vector2Int adyCoord = cell.coordinates + offset;
                if (yaUsadas != null && yaUsadas.Contains(adyCoord))
                    continue;

                CellData ady = GameManager.Instance.gridGenerator.GetCellAt(adyCoord.x, adyCoord.y);
                if (ady != null && ady.isWalkable)
                    return ady.transform.position;
            }
        }

        return Vector3.zero;
    }

}
