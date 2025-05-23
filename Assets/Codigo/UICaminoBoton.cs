using UnityEngine;
using UnityEngine.UI;

public class UICaminoBoton : MonoBehaviour
{
    private HeadquartersBuilding hq;
    private CaminoActivo camino;

    // UICaminoBoton.cs
    public void Init(CaminoActivo camino, HeadquartersBuilding hq)
    {
        this.camino = camino;
        this.hq = hq;

        Button boton = GetComponentInChildren<Button>();
        boton.onClick.RemoveAllListeners();
        boton.onClick.AddListener(() =>
        {
            hq.GetCaminosActivos().Remove(camino);

            foreach (GameObject unidad in camino.unidadesVinculadas)
            {
                if (unidad != null)
                    Destroy(unidad);
            }

            if (camino.isRecolectar)
                hq.generationRate = Mathf.Max(1, hq.generationRate - 1);

            foreach (var tramo in camino.tramos)
                if (tramo != null)
                    Destroy(tramo.gameObject);

            if (camino.tramosParent != null)
                Destroy(camino.tramosParent);

            Destroy(gameObject);
        });
    }



}
