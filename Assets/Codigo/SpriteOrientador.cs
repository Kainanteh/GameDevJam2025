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

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            sr.sprite = dir.x > 0 ? spriteEste : spriteOeste;
        }
        else
        {
            sr.sprite = dir.y > 0 ? spriteNorte : spriteSur;
        }
    }
}
