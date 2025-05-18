using UnityEngine;

public class UnidadMover : MonoBehaviour
{
    private Vector3 start;
    private Vector3 end;
    private float t = 0f;

    private HeadquartersBuilding origen;

    private int coste = -1;

    public void Init(HeadquartersBuilding origen, Vector3 start, Vector3 end, int coste)
    {
        this.origen = origen;
        this.start = start;
        this.end = end;
        this.coste = coste;

        MostrarTexto();
    }

    void Update()
    {
        float speed = GameManager.Instance.unidadSpeed;

        t += Time.deltaTime * speed;
        t = Mathf.Clamp01(t);

        transform.position = Vector3.Lerp(start, end, t);

        Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
        var cell = GameManager.Instance.gridGenerator.GetCellAt(gridPos.x, gridPos.y);

        if (cell != null && !cell.isWalkable)
        {
            Debug.Log($"❌ Unidad murió en obstáculo en {gridPos}");
            Destroy(gameObject);
            return;
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    void MostrarTexto()
    {
        var label = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (label != null)
            label.text = coste.ToString();
    }
}
