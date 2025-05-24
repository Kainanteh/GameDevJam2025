using UnityEngine;

public class SpriteOrientador : MonoBehaviour
{
    public Sprite spriteNorte;
    public Sprite spriteSur;
    public Sprite spriteEste;
    public Sprite spriteOeste;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();


        if (sr == null)
            Debug.LogError("❌ No se encontró SpriteRenderer en el primer hijo de " + gameObject.name);
    }


    public void ActualizarDireccion(Vector3 desde, Vector3 hacia)
    {
        if (sr == null) return;

        Vector3 dir = (hacia - desde).normalized;

        // Desactivar todos los gorros
        transform.parent.transform.Find("GnomoSpriteGorroNorte")?.gameObject.SetActive(false);
        transform.parent.transform.Find("GnomoSpriteGorroSur")?.gameObject.SetActive(false);
        transform.parent.transform.Find("GnomoSpriteGorroEste")?.gameObject.SetActive(false);
        transform.parent.transform.Find("GnomoSpriteGorroOeste")?.gameObject.SetActive(false);

        Transform gorro = null;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            if (dir.x > 0)
            {
                sr.sprite = spriteEste;
                gorro = transform.parent.transform.Find("GnomoSpriteGorroEste");
            }
            else
            {
                sr.sprite = spriteOeste;
                gorro = transform.parent.transform.Find("GnomoSpriteGorroOeste");
            }
        }
        else
        {
            if (dir.y > 0)
            {
                sr.sprite = spriteNorte;
                gorro = transform.parent.transform.Find("GnomoSpriteGorroNorte");
            }
            else
            {
                sr.sprite = spriteSur;
                gorro = transform.parent.transform.Find("GnomoSpriteGorroSur");
            }
        }

        if (gorro != null)
        {
            gorro.gameObject.SetActive(true);

            var srGorro = gorro.GetComponent<SpriteRenderer>();
            if (srGorro != null)
            {
                int ownerId = -1;

                var explorador = GetComponentInParent<UnidadExplorador>();
                if (explorador != null)
                    ownerId = explorador.origen?.ownerId ?? -1;

                var soldado = GetComponentInParent<UnidadSoldado>();
                if (soldado != null)
                    ownerId = soldado.GetOwnerId();

                var recolector = GetComponentInParent<UnidadRecolector>();
                if (recolector != null)
                    ownerId = recolector.origenHQ?.ownerId ?? -1;

                srGorro.color = GameManager.Instance.GetOwnerColor(ownerId);
            }
        }
    }






    // Dentro de SpriteOrientador.cs
    public void AplicarColorGorro(Color color)
    {
        Transform gorroNorte = transform.Find("GnomoSpriteGorroNorte");
        Transform gorroSur = transform.Find("GnomoSpriteGorroSur");
        Transform gorroEste = transform.Find("GnomoSpriteGorroEste");
        Transform gorroOeste = transform.Find("GnomoSpriteGorroOeste");

        if (gorroNorte != null) gorroNorte.GetComponent<SpriteRenderer>().color = color;
        if (gorroSur != null) gorroSur.GetComponent<SpriteRenderer>().color = color;
        if (gorroEste != null) gorroEste.GetComponent<SpriteRenderer>().color = color;
        if (gorroOeste != null) gorroOeste.GetComponent<SpriteRenderer>().color = color;
    }

}
