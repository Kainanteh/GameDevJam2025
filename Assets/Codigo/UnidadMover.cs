using UnityEngine;

public class UnidadMover : MonoBehaviour
{
    private Vector3 start;
    private Vector3 end;
    private float t = 0f;

    private HeadquartersBuilding origen;
    private Building destino;

    private int coste = -1;

    public void Init(HeadquartersBuilding origen, Building destino, Vector3 start, Vector3 end, int coste)
    {
        this.origen = origen;
        this.destino = destino;
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

        if (t >= 1f)
        {
            //origen.UnidadReachedTarget(end);

            if (destino is HeadquartersBuilding hqDestino)
                hqDestino.soldierCount = Mathf.Max(0, hqDestino.soldierCount - 1);

            Destroy(gameObject);
        }
    }

    void MostrarTexto()
    {
        var label = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (label != null)
        {
            label.text = coste.ToString();
        }
    }

}
