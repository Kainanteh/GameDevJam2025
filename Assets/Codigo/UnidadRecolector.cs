using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UnidadRecolector : MonoBehaviour
{
    private HeadquartersBuilding origenHQ;
    private List<Vector3> camino = new();
    private float speed = 2f;
    private bool yaSumado = false;
    private CellData celdaRecurso;
    private TMPro.TextMeshProUGUI label;
    public bool empiezaDesdeRecurso = false;
    private int coste = 1;
    public CaminoActivo caminoAsociado;

    public void InitConCamino(HeadquartersBuilding origenHQ, List<Vector3> caminoCompleto, CellData celdaRecurso, CaminoActivo caminoAsociado, bool empezarEnInicio = true)
    {
        this.origenHQ = origenHQ;
        this.celdaRecurso = celdaRecurso;
        this.camino = new List<Vector3>(caminoCompleto);
        this.empiezaDesdeRecurso = !empezarEnInicio;
        this.coste = 1;
        this.caminoAsociado = caminoAsociado;

        transform.position = empezarEnInicio ? camino[0] : camino[camino.Count - 1];

        SetupLabel();
        StartCoroutine(RutinaCicloRecolector());
    }

    void SetupLabel()
    {
        label = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (label != null)
            label.text = $"R{coste}";
    }

    IEnumerator RutinaCicloRecolector()
    {
        SpriteOrientador orientador = GetComponent<SpriteOrientador>();
        bool haLlegadoAlRecurso = empiezaDesdeRecurso;

        while (true)
        {
            if (!haLlegadoAlRecurso)
            {
                for (int i = 1; i < camino.Count; i++)
                    yield return MoverA(camino[i], orientador);

                haLlegadoAlRecurso = true;
                yield return new WaitForSeconds(1f);
            }

            for (int i = camino.Count - 2; i >= 0; i--)
                yield return MoverA(camino[i], orientador);

            if (!yaSumado)
            {
                yaSumado = true;
                origenHQ.OnRecolectorSuccess();
            }

            yield return new WaitForSeconds(1f);

            for (int i = 1; i < camino.Count; i++)
                yield return MoverA(camino[i], orientador);

            yield return new WaitForSeconds(1f);
        }
    }

    IEnumerator MoverA(Vector3 objetivo, SpriteOrientador _ = null)
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
                Debug.Log($"☠️ Recolector murió en obstáculo en {gridPos}");
                Destroy(gameObject);
                yield break;
            }

            yield return null;
        }
    }

}
