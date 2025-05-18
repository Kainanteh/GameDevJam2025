
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

    public void Init(HeadquartersBuilding origenHQ, List<Vector3> caminoCompleto, Building hqObjetivo, int daño)
    {
        this.origenHQ = origenHQ;
        this.camino = new List<Vector3>(caminoCompleto);
        this.hqObjetivo = hqObjetivo;
        this.daño = daño;

        if (camino.Count > 0)
            transform.position = camino[0];

        StartCoroutine(RutinaAvanzarYAtacar());
    }

    IEnumerator RutinaAvanzarYAtacar()
    {
        for (int i = 1; i < camino.Count; i++)
        {
            yield return MoverA(camino[i]);
        }

        if (hqObjetivo != null && hqObjetivo is HeadquartersBuilding objetivo)
        {
            objetivo.RecibirDaño(daño);
            Debug.Log($"⚔️ Soldado impacta al HQ enemigo y causa {daño} de daño");
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

}
