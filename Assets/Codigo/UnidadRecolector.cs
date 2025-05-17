using UnityEngine;
using System.Collections;

public class UnidadRecolector : MonoBehaviour
{
    private HeadquartersBuilding origenHQ;
    private Vector3 origen;
    private Vector3 destino;
    private float speed = 2f;

    private bool yaSumado = false;

    private CellData celdaRecurso;
    private TMPro.TextMeshProUGUI label;

    public void Init(HeadquartersBuilding origenHQ, Vector3 origen, Vector3 destino, CellData celdaRecurso)
    {
        this.origenHQ = origenHQ;
        this.origen = origen;
        this.destino = destino;
        this.celdaRecurso = celdaRecurso;

        label = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);

        if (label == null)
            Debug.LogWarning("❌ NO se encontró el TextMeshProUGUI en la unidad.");
        else
            Debug.Log("✅ Label encontrado correctamente: " + label.text);

        StartCoroutine(RutinaCicloRecolector());
    }



    IEnumerator RutinaCicloRecolector()
    {
        while (true)
        {
            // Ir hasta el recurso
            yield return MoverA(destino);

            if (label != null && celdaRecurso != null)
            {
                label.text = celdaRecurso.resourceAmount.ToString();
                Debug.Log("Recolector llegó a recurso " + celdaRecurso.coordinates + " con amount: " + celdaRecurso.resourceAmount);
            }
            else
            {
                Debug.LogWarning("Fallo en mostrar texto. Label o celda nula.");
            }


            yield return new WaitForSeconds(1f);  // espera
            yield return MoverA(origen);          // vuelve

            if (!yaSumado)
            {
                yaSumado = true;
                origenHQ.OnRecolectorSuccess();   // sube pasiva
            }

            yield return new WaitForSeconds(1f);  // pausa en cuartel
        }
    }

    IEnumerator MoverA(Vector3 objetivo)
    {
        while (Vector3.Distance(transform.position, objetivo) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, objetivo, speed * Time.deltaTime);
            yield return null;
        }
    }
}
