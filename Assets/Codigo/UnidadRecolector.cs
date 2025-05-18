using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UnidadRecolector : MonoBehaviour
{
    private HeadquartersBuilding origenHQ;
    private List<Vector3> camino = new List<Vector3>();
    private int index = 0;
    private bool yendo = true;
    private float speed = 2f;

    private bool yaSumado = false;
    private CellData celdaRecurso;
    private TMPro.TextMeshProUGUI label;

    // ✅ Init clásico: directo origen → recurso
    public void Init(HeadquartersBuilding origenHQ, Vector3 origen, Vector3 destino, CellData celdaRecurso)
    {
        this.origenHQ = origenHQ;
        this.celdaRecurso = celdaRecurso;

        camino.Clear();
        camino.Add(origen);
        camino.Add(destino);

        transform.position = origen;
        SetupLabel();

        StartCoroutine(RutinaCicloRecolector());
    }

    // ✅ Init con camino completo (explorador)
    public void InitConCamino(HeadquartersBuilding origenHQ, List<Vector3> caminoCompleto, CellData celdaRecurso)
    {
        this.origenHQ = origenHQ;
        this.celdaRecurso = celdaRecurso;

        // Copiar el camino exacto (ya incluye el recurso al final)
        camino = new List<Vector3>(caminoCompleto);

        // Posicionar al recolector en el último punto (el recurso)
        transform.position = camino[camino.Count - 1];

        SetupLabel();
        StartCoroutine(RutinaCicloRecolector());
    }



    void SetupLabel()
    {
        label = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (label == null)
            Debug.LogWarning("❌ NO se encontró el TextMeshProUGUI en la unidad.");
        else
            Debug.Log("✅ Label encontrado correctamente: " + label.text);
    }

    IEnumerator RutinaCicloRecolector()
    {
        while (true)
        {
            // 🔁 PRIMER TRAMO: del recurso hacia HQ (inverso)
            for (int i = camino.Count - 2; i >= 0; i--)
                yield return MoverA(camino[i]);

            if (!yaSumado)
            {
                yaSumado = true;
                origenHQ.OnRecolectorSuccess();
                Debug.Log("⬆️ Recolector sube pasiva");
            }

            yield return new WaitForSeconds(1f);

            // 🔁 VUELTA: de HQ al recurso (normal)
            for (int i = 1; i < camino.Count; i++)
                yield return MoverA(camino[i]);

            yield return new WaitForSeconds(1f);
        }
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
                Debug.Log($"☠️ Recolector murió en obstáculo en {gridPos}");
                Destroy(gameObject);
                yield break;
            }

            yield return null;
        }
    }
}
