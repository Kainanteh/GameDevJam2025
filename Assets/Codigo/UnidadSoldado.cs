using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnidadSoldado : MonoBehaviour
{
    private HeadquartersBuilding origenHQ;
    private List<Vector3> camino = new();
    private int index = 0;
    private float speed = 2f;
    private int daño;
    private Building hqObjetivo;
    public CaminoActivo caminoAsociado;

    public void Init(HeadquartersBuilding origenHQ, List<Vector3> caminoCompleto, Building hqObjetivo, int daño, CaminoActivo caminoAsociado)
    {
        this.origenHQ = origenHQ;
        this.camino = new List<Vector3>(caminoCompleto);
        this.hqObjetivo = hqObjetivo;
        this.daño = daño;
        this.caminoAsociado = caminoAsociado;

        // ✅ solo Soldado33 aparece en su posición actual (celda vacía del explorador)
        if (gameObject.name != "Soldado33" && camino.Count > 0)
            transform.position = camino[0];

        var label = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (label != null)
            label.text = $"S{daño}";

        caminoAsociado.unidadesVinculadas.Add(gameObject);

        if (gameObject.name == "Soldado33")
        {
            // solo hace el último tramo directamente
            StartCoroutine(MoverSoloUltimoTramo(camino[^1]));
        }
        else
        {
            StartCoroutine(RutinaAvanzarYAtacar());
        }
    }

    private IEnumerator MoverSoloUltimoTramo(Vector3 destino)
    {
        var orientador = transform.Find("GnomoSprite")?.GetComponent<SpriteOrientador>();

        while (Vector3.Distance(transform.position, destino) > 0.05f)
        {
            orientador?.ActualizarDireccion(transform.position, destino);
            transform.position = Vector3.MoveTowards(transform.position, destino, 2f * Time.deltaTime);

            Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
            var cell = GameManager.Instance.gridGenerator.GetCellAt(gridPos.x, gridPos.y);

            if (cell != null && !cell.isWalkable)
            {
                Debug.Log($"☠️ Soldado33 murió en obstáculo en {gridPos}");
                Destroy(gameObject);
                yield break;
            }

            yield return null;
        }

        if (hqObjetivo is HeadquartersBuilding objetivo)
        {
            if (objetivo.ownerId != origenHQ?.ownerId && !caminoAsociado.esRefuerzoPasivo)
            {
                GameManager.Instance.UltimoAtacanteOwnerId = origenHQ.ownerId;
                objetivo.RecibirDaño(daño);
            }
        }

        Destroy(gameObject);
    }



    IEnumerator RutinaAvanzarYAtacar()
    {
        for (int i = 1; i < camino.Count; i++)
            yield return MoverA(camino[i]);

        if (hqObjetivo is HeadquartersBuilding objetivo)
        {
            if (objetivo.ownerId == origenHQ.ownerId)
            {
                Debug.Log($"🚫 Soldado ignora impacto: {objetivo.buildingName} ya es del mismo owner ({objetivo.ownerId})");
            }
            else if (caminoAsociado != null && caminoAsociado.esRefuerzoPasivo)
            {
                Debug.Log($"🚶 Soldado de refuerzo llega a {objetivo.buildingName}, sin efecto directo");
            }
            else
            {
                GameManager.Instance.UltimoAtacanteOwnerId = origenHQ.ownerId;
                objetivo.RecibirDaño(daño);
            }
        }

        Destroy(gameObject);
    }

    IEnumerator MoverA(Vector3 objetivo)
    {
        var orientador = transform.Find("GnomoSprite")?.GetComponent<SpriteOrientador>();

        while (Vector3.Distance(transform.position, objetivo) > 0.05f)
        {
            orientador?.ActualizarDireccion(transform.position, objetivo);
    

            transform.position = Vector3.MoveTowards(transform.position, objetivo, speed * Time.deltaTime);

            Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
            var cell = GameManager.Instance.gridGenerator.GetCellAt(gridPos.x, gridPos.y);

            if (cell != null && !cell.isWalkable)
            {
                Debug.Log($"☠️ Soldado murió en obstáculo en {gridPos}");
                Destroy(gameObject);
                yield break;
            }

            yield return null;
        }
    }


    public int GetOwnerId()
    {
        return origenHQ != null ? origenHQ.ownerId : -1;
    }
}
