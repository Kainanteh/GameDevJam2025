using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PathVisualDebug : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color originalColor;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
        sr.color = new Color(1f, 0f, 0f, 0.2f); // rojo semitransparente para ver el área
        sr.sortingOrder = 9999; // para que esté siempre visible encima
    }

    void OnMouseDown()
    {
        sr.color = new Color(0f, 1f, 0f, 0.4f); // se vuelve verde al clic
        Debug.Log("✅ PathVisualDebug: clic recibido en " + gameObject.name);
    }

    void OnMouseUp()
    {
        sr.color = originalColor;
    }
}
