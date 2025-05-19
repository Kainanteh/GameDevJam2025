
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

        if (camino.Count > 0)
            transform.position = camino[0];

        var label = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (label != null)
            label.text = $"S{daño}";

        StartCoroutine(RutinaAvanzarYAtacar());
    }


    IEnumerator RutinaAvanzarYAtacar()
    {
        for (int i = 1; i < camino.Count; i++)
            yield return MoverA(camino[i]);

        if (hqObjetivo != null && hqObjetivo is HeadquartersBuilding objetivo)
        {
            // Si el objetivo ya es aliado, no hacer daño
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
                Debug.Log($"⚔️ Soldado impacta al HQ enemigo y causa {daño} de daño");
            }
        }

        Destroy(gameObject);
    }



    IEnumerator MoverA(Vector3 objetivo)
    {
        while (Vector3.Distance(transform.position, objetivo) > 0.05f)
        {
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
