using UnityEngine;

public class ProfundidadTornilloHerramienta : MonoBehaviour
{
    [Header("Escala original del cilindro en Y")]
    public float escalaYOriginal = 1f; // ponla igual a la localScale.y actual del cilindro

    // Llama a esto desde el mismo botón que genera los tornillos superiores
    public void AplicarProfundidad()
    {
        float profundidad = GeneradorTriangulo.profundidadCalculada;

        if (profundidad <= 0f)
        {
            Debug.LogWarning("Aún no hay profundidad calculada. Genera los tornillos primero.");
            return;
        }

        // Un cilindro de Unity mide 2 unidades en Y por defecto
        float nuevaEscalaY = profundidad / 2f;

        Vector3 escala = transform.localScale;
        transform.localScale = new Vector3(escala.x, nuevaEscalaY, escala.z);

        Debug.Log($"Cilindro ajustado a profundidad: {profundidad * 1000f:F1} mm");
    }
}