using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;

public class TrianguloRayos : MonoBehaviour
{
    [Header("Configuración Principal")]
    public GameObject prefabTornillo;
    public Transform huesoContenedor;
    public MeshCollider colliderHueso;

    public enum EjeHueso { X_Right, Y_Up, Z_Forward, MenosX_Left, MenosY_Down, MenosZ_Back }
    [Tooltip("El eje que apunta hacia la cabeza del fémur")]
    public EjeHueso ejeParaSubir = EjeHueso.Z_Forward;

    [Header("Parámetros Biomecánicos")]
    public float radioTornillo = 0.0035f; // 3.5mm
    public float margenSeguridad = 0.002f; // 2mm

    [Header("Configuración del Escáner")]
    public float longitudTornillo = 0.1f; // 10 cm
    [Tooltip("Más cortes = más precisión y más líneas de debug")]
    public int resolucionEscaneo = 20;

    public void GenerarSuperiores()
    {
        GameObject guia = GameObject.Find("Tornillo_Manual");
        if (guia == null || colliderHueso == null) return;

        // 1. OBTENER VECTORES NIVELADOS
        Vector3 ejeSubirHueso = ObtenerVectorEje(ejeParaSubir);
        Vector3 ejeTornillo = guia.transform.up;

        Vector3 vectorLado = Vector3.Cross(ejeTornillo, ejeSubirHueso).normalized;
        Vector3 vectorArriba = Vector3.Cross(vectorLado, ejeTornillo).normalized;

        // 2. INICIAR EL "RADAR" DE TÚNEL
        float minDistArriba = float.MaxValue;
        float minDistLado = float.MaxValue;
        float minDistDiagonal = float.MaxValue;

        // --- INICIO DE BUCLE DE ESCANEO ---
        for (int i = 0; i <= resolucionEscaneo; i++)
        {
            float porcentaje = (float)i / resolucionEscaneo;
            Vector3 puntoEnTrayectoria = guia.transform.position + (ejeTornillo * (longitudTornillo * porcentaje));

            // Escaneamos y DIBUJAMOS las 5 direcciones
            // Pasamos colores distintos para diferenciarlos en la Scene
            float dArriba = LanzarRayoInversoVisual(puntoEnTrayectoria, vectorArriba, Color.cyan);
            float dDer = LanzarRayoInversoVisual(puntoEnTrayectoria, vectorLado, Color.yellow);
            float dIzq = LanzarRayoInversoVisual(puntoEnTrayectoria, -vectorLado, Color.yellow);

            // Diagonales
            Vector3 diagDer = (vectorArriba + vectorLado).normalized;
            Vector3 diagIzq = (vectorArriba - vectorLado).normalized;
            float dDiagDer = LanzarRayoInversoVisual(puntoEnTrayectoria, diagDer, Color.gray);
            float dDiagIzq = LanzarRayoInversoVisual(puntoEnTrayectoria, diagIzq, Color.gray);

            // Guardamos el cuello de botella
            minDistArriba = Mathf.Min(minDistArriba, dArriba);
            minDistLado = Mathf.Min(minDistLado, dDer, dIzq);
            minDistDiagonal = Mathf.Min(minDistDiagonal, dDiagDer, dDiagIzq);
        }
        // --- FIN DE BUCLE DE ESCANEO ---

        // (El resto del script de cálculo y generación es idéntico al anterior y se omite por brevedad)
        // ... (Pasos 3, 4, 5, 6 y 7 del script anterior) ...

        // Simplemente copio la parte del cálculo final para que el script compile y funcione
        float techoReal = minDistArriba - margenSeguridad - radioTornillo;
        float paredReal = minDistLado - margenSeguridad - radioTornillo;
        float diagReal = minDistDiagonal - margenSeguridad - radioTornillo;

        if (techoReal <= 0 || paredReal <= 0) return;

        float maxLadoPorTecho = techoReal * 1.1547f;
        float maxLadoPorPared = paredReal * 2f;
        float maxLadoPorDiag = diagReal;

        float separacionOptima = Mathf.Min(maxLadoPorTecho, maxLadoPorPared, maxLadoPorDiag);
        float alturaTriangulo = separacionOptima * (Mathf.Sqrt(3) / 2f);
        float mitadBase = separacionOptima / 2f;

        Vector3 centroIzq = guia.transform.position + (vectorArriba * alturaTriangulo) - (vectorLado * mitadBase);
        Vector3 centroDer = guia.transform.position + (vectorArriba * alturaTriangulo) + (vectorLado * mitadBase);

        GameObject tornilloIzq = Instantiate(prefabTornillo, centroIzq, guia.transform.rotation);
        tornilloIzq.name = "Tornillo_Optimo_Izq";
        GameObject tornilloDer = Instantiate(prefabTornillo, centroDer, guia.transform.rotation);
        tornilloDer.name = "Tornillo_Optimo_Der";

        if (huesoContenedor != null)
        {
            tornilloIzq.transform.SetParent(huesoContenedor, true);
            tornilloDer.transform.SetParent(huesoContenedor, true);
        }
        tornilloIzq.transform.localScale = guia.transform.localScale;
        tornilloDer.transform.localScale = guia.transform.localScale;

        Debug.Log($"<color=green>ESCÁNER COMPLETADO:</color> Separación óptima: {separacionOptima * 1000:F1}mm.");
    }

    // EL TRUCO DEL INGENIERO CON DIBUJO DE DEBUG
    private float LanzarRayoInversoVisual(Vector3 centro, Vector3 direccion, Color colorDebug)
    {
        Vector3 origenFuera = centro + (direccion * 0.15f); // 15 cm fuera
        RaycastHit hit;

        // Disparamos hacia adentro (-direccion)
        if (colliderHueso.Raycast(new Ray(origenFuera, -direccion), out hit, 0.15f))
        {
            // --- AQUÍ ESTÁ EL DIBUJO ---
            // Dibujamos una línea desde el punto de impacto (la pared) hasta el centro
            // La línea durará 2 segundos en pantalla para que te dé tiempo a verla
            Debug.DrawLine(hit.point, centro, colorDebug, 2.0f);

            // Opcional: Dibuja una crucecita roja en el punto exacto del impacto en la malla
            Debug.DrawRay(hit.point, Vector3.up * 0.002f, Color.red, 2.0f);
            Debug.DrawRay(hit.point, Vector3.right * 0.002f, Color.red, 2.0f);

            return Vector3.Distance(centro, hit.point);
        }

        // Si no choca, dibuja una línea roja larga (significa que el rayo falló el tiro)
        Debug.DrawLine(origenFuera, centro, Color.red, 2.0f);
        return 999f;
    }

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
}