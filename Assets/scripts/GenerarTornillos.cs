using UnityEngine;

public class GeneradorTornillos : MonoBehaviour
{
    [Header("Configuración del Tornillo")]
    public GameObject prefabTornillo;

    [Header("Referencias del Hueso")]
    [Tooltip("El Padre para copiar la escala")]
    public Transform huesoPadre;

    [Tooltip("La cabeza del fémur (para calcular el ángulo)")]
    public Renderer mallaCabeza;
    [Tooltip("El cuerpo del fémur (para calcular el ángulo)")]
    public Renderer mallaCuerpo;

    public void Generar()
    {
        // 1. Posición de aparición (40 cm delante de la cara)
        Vector3 posicionAparicion = Camera.main.transform.position + (Camera.main.transform.forward * 0.4f);

        // 2. CALCULAR LA DIRECCIÓN DEL CUELLO DEL FÉMUR
        // Por defecto mirará hacia arriba, por si hay algún error
        Vector3 direccionCuello = Vector3.up;

        if (mallaCabeza != null && mallaCuerpo != null)
        {
            // Restamos el destino (cabeza) menos el origen (cuerpo) para sacar la flecha de dirección
            direccionCuello = (mallaCabeza.bounds.center - mallaCuerpo.bounds.center).normalized;
        }
        else
        {
            Debug.LogWarning("Falta asignar la cabeza o el cuerpo en el Inspector. El tornillo saldrá recto.");
        }

        // 3. Rotar el cilindro para que su eje "Arriba" apunte en esa dirección calculada
        Quaternion rotacionInicial = Quaternion.FromToRotation(Vector3.up, direccionCuello);

        // 4. Crear el tornillo con esa posición y rotación
        GameObject nuevoTornillo = Instantiate(prefabTornillo, posicionAparicion, rotacionInicial);
        nuevoTornillo.name = "Tornillo_Manual";

        // 5. Escalar el tornillo proporcionalmente al tamaño actual del hueso padre
        if (huesoPadre != null)
        {
            Vector3 escalaOriginal = prefabTornillo.transform.localScale;
            nuevoTornillo.transform.localScale = escalaOriginal * huesoPadre.localScale.x;
        }

        Debug.Log("<color=green>Tornillo Generado</color> con el ángulo del fémur.");
    }
}