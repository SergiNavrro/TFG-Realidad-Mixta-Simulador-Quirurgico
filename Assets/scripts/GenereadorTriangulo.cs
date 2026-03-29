using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;

public class GeneradorTriangulo : MonoBehaviour
{
    [Header("Configuración del Triángulo")]
    public GameObject prefabTornillo;
    public Transform huesoContenedor;
    public float separacion = 0.015f;

    // Magia pura: Menú desplegable para no tocar el código nunca más
    public enum EjeHueso { X_Right, Y_Up, Z_Forward, MenosX_Left, MenosY_Down, MenosZ_Back }

    [Header("Ajuste de Ejes (Selector)")]
    [Tooltip("El eje que hace que los tornillos suban hacia la cabeza")]
    public EjeHueso ejeParaSubir = EjeHueso.Z_Forward; // Por defecto el que te funcionó

    [Tooltip("El eje que hace que los tornillos se separen a los lados")]
    public EjeHueso ejeParaLados = EjeHueso.Y_Up;      // El eje que nos faltaba

    public void GenerarSuperiores()
    {
        GameObject guia = GameObject.Find("Tornillo_Manual");
        if (guia == null)
        {
            Debug.LogWarning("¡Falta el tornillo guía!");
            return;
        }

        // 1. Encontramos la base exacta del tornillo guía
        float largo = guia.transform.lossyScale.y;
        Vector3 baseGuia = guia.transform.position - (guia.transform.up * largo);

        // 2. Obtenemos los vectores según lo que hayas elegido en el Inspector de Unity
        Vector3 ejeArribaFijo = ObtenerVectorEje(ejeParaSubir);
        Vector3 ejeLadosFijo = ObtenerVectorEje(ejeParaLados);

        float subida = separacion * 0.866f;
        float lado = separacion * 0.5f;

        // 3. Calculamos las bases subiendo y separando
        Vector3 baseIzq = baseGuia + (ejeArribaFijo * subida) - (ejeLadosFijo * lado);
        Vector3 baseDer = baseGuia + (ejeArribaFijo * subida) + (ejeLadosFijo * lado);

        // 4. Clavamos los centros exactos
        Vector3 centroIzq = baseIzq + (guia.transform.up * largo);
        Vector3 centroDer = baseDer + (guia.transform.up * largo);

        // 5. Instanciamos copiando la rotación (PARALELISMO PERFECTO)
        GameObject tornilloIzq = Instantiate(prefabTornillo, centroIzq, guia.transform.rotation);
        tornilloIzq.name = "Tornillo_Superior_Izquierdo";

        GameObject tornilloDer = Instantiate(prefabTornillo, centroDer, guia.transform.rotation);
        tornilloDer.name = "Tornillo_Superior_Derecho";

        // 6. Emparentar
        if (huesoContenedor != null)
        {
            tornilloIzq.transform.SetParent(huesoContenedor, true);
            tornilloDer.transform.SetParent(huesoContenedor, true);
        }

        ApagarAgarre(tornilloIzq);
        ApagarAgarre(tornilloDer);

        Debug.Log("<color=green>TRIÁNGULO COMPLETADO:</color> Ejes aplicados desde el Inspector.");
    }

    // Traductor del menú desplegable a vectores reales de Unity
    private Vector3 ObtenerVectorEje(EjeHueso eje)
    {
        if (huesoContenedor == null) return Vector3.up;

        switch (eje)
        {
            case EjeHueso.X_Right: return huesoContenedor.right;
            case EjeHueso.Y_Up: return huesoContenedor.up;
            case EjeHueso.Z_Forward: return huesoContenedor.forward;
            case EjeHueso.MenosX_Left: return -huesoContenedor.right;
            case EjeHueso.MenosY_Down: return -huesoContenedor.up;
            case EjeHueso.MenosZ_Back: return -huesoContenedor.forward;
            default: return huesoContenedor.up;
        }
    }

    private void ApagarAgarre(GameObject tornillo)
    {
        ObjectManipulator manipulador = tornillo.GetComponent<ObjectManipulator>();
        if (manipulador != null) manipulador.enabled = false;
    }
}