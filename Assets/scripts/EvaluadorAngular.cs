using TMPro;
using UnityEngine;

public class EvaluadorAngular : MonoBehaviour
{
    [Header("Herramienta")]
    public Transform ejeHerramienta;
    public Renderer cilindroHerramientaRenderer;
    public Transform cilindroHerramienta;

    [Header("Hueso (se asigna automáticamente al crear el tornillo)")]
    private Transform ejeHueso;

    [Header("Feedback Visual")]
    public Material matRojo;
    public Material matVerde;
    public Material matBlanco;

    [Header("Configuración")]
    public float toleranciaGrados = 5.0f;
    public float toleranciaPosicionMm = 3.0f;
    public float frecuenciaEvaluacion = 0.1f;

    [Header("HUD - Angular")]
    public TextMeshProUGUI textoErrorAngular;

    [Header("HUD - Posición Lateral")]
    public TextMeshProUGUI textoErrorX;
    public TextMeshProUGUI textoErrorZ;

    [Header("HUD - Profundidad")]
    public TextMeshProUGUI textoProfundidad;

    [Header("HUD - Estado General")]
    public TextMeshProUGUI textoEstado;

    // --- DATOS EXPUESTOS PARA EL REGISTRADOR ---
    [HideInInspector] public float ultimoErrorAngular = 0f;
    [HideInInspector] public float ultimoErrorX = 0f;
    [HideInInspector] public float ultimoErrorZ = 0f;
    [HideInInspector] public float ultimaProfundidad = 0f;
    [HideInInspector] public float ultimoPorcentaje = 0f;
    [HideInInspector] public bool ultimoEstadoOk = false;
    [HideInInspector] public bool evaluacionActiva => activo;

    private bool activo = false;
    private bool estaEnVerde = false;
    private float timerEvaluacion = 0f;

    public void AsignarEjeHueso(Transform nuevoEje)
    {
        ejeHueso = nuevoEje;
        Debug.Log("Eje hueso asignado: " + nuevoEje.name);
    }

    public void ConmutarEvaluacion()
    {
        if (ejeHueso == null)
        {
            Debug.LogWarning("Aún no hay tornillo creado. Crea el tornillo primero.");
            return;
        }

        activo = !activo;

        if (!activo)
        {
            AplicarMaterial(matBlanco);
            estaEnVerde = false;
            LimpiarHUD();
        }
        else
        {
            timerEvaluacion = frecuenciaEvaluacion;
        }
    }

    void Update()
    {
        if (!activo || ejeHueso == null || ejeHerramienta == null || cilindroHerramienta == null) return;

        // --- PUNTOS DE REFERENCIA ---
        float longitudHueso = ejeHueso.lossyScale.y * 2f;
        float longitudHerramienta = cilindroHerramienta.lossyScale.y * 2f;

        // Base del tornillo planificado (entrada al hueso, corteza lateral)
        Vector3 entradaTornillo = ejeHueso.position - (ejeHueso.up * (longitudHueso / 2f));
        // Punta de la herramienta (up apunta al mango, por eso -)
        Vector3 puntaHerramienta = cilindroHerramienta.position - (cilindroHerramienta.up * (longitudHerramienta / 2f));

        // Debug visual
        Debug.DrawLine(entradaTornillo, puntaHerramienta, Color.magenta);
        Debug.DrawRay(entradaTornillo, Vector3.up * 0.01f, Color.blue);
        Debug.DrawRay(puntaHerramienta, Vector3.up * 0.01f, Color.red);

        timerEvaluacion += Time.deltaTime;
        if (timerEvaluacion < frecuenciaEvaluacion) return;
        timerEvaluacion = 0f;

        // --- ERROR ANGULAR ---
        float errorInclinacion = Vector3.Angle(ejeHerramienta.up, ejeHueso.up);

        // --- ERRORES DE POSICIÓN (en coordenadas locales del tornillo) ---
        Vector3 diff = puntaHerramienta - entradaTornillo;

        float errorX_mm = Vector3.Dot(diff, ejeHueso.right) * 1000f;
        float errorZ_mm = Vector3.Dot(diff, ejeHueso.forward) * 1000f;
        float profundidad_mm = Vector3.Dot(diff, ejeHueso.up) * 1000f;

        // --- PROFUNDIDAD E INSERCIÓN ---
        float longitudHerramienta_mm = longitudHerramienta * 1000f;
        float porcentajeInsercion = Mathf.Clamp01(profundidad_mm / longitudHerramienta_mm) * 100f;

        

        // --- ACTUALIZAR HUD ---
        if (textoErrorAngular != null)
            textoErrorAngular.text = $"Ángulo: {errorInclinacion:F1}°";

        if (textoErrorX != null)
            textoErrorX.text = $"Error X: {errorX_mm:F1} mm";

        if (textoErrorZ != null)
            textoErrorZ.text = $"Error Z: {errorZ_mm:F1} mm";

        if (textoProfundidad != null)
        {
            if (profundidad_mm <= 0)
                textoProfundidad.text = $"Prof: {Mathf.Abs(profundidad_mm):F1} mm (Fuera)";
            else if (porcentajeInsercion >= 100f)
                textoProfundidad.text = "✓ INSERTADO COMPLETAMENTE";
            else
                textoProfundidad.text = $"Inserción: {porcentajeInsercion:F1}%";
        }

        // --- LÓGICA VERDE/ROJO ---
        bool deberiaEstarEnVerde = errorInclinacion <= toleranciaGrados
                                && Mathf.Abs(errorX_mm) <= toleranciaPosicionMm
                                && Mathf.Abs(errorZ_mm) <= toleranciaPosicionMm;
        ultimoErrorAngular = errorInclinacion;
        ultimoErrorX = errorX_mm;
        ultimoErrorZ = errorZ_mm;
        ultimaProfundidad = profundidad_mm;
        ultimoPorcentaje = porcentajeInsercion;
        ultimoEstadoOk = deberiaEstarEnVerde;   

        if (textoEstado != null)
            textoEstado.text = deberiaEstarEnVerde ? "✓ ALINEADO" : "✗ AJUSTAR";

        if (deberiaEstarEnVerde != estaEnVerde)
        {
            estaEnVerde = deberiaEstarEnVerde;
            AplicarMaterial(estaEnVerde ? matVerde : matRojo);
        }
    }

    private void AplicarMaterial(Material mat)
    {
        if (cilindroHerramientaRenderer != null && mat != null)
            cilindroHerramientaRenderer.sharedMaterial = mat;
    }

    private void LimpiarHUD()
    {
        if (textoErrorAngular != null) textoErrorAngular.text = "Ángulo: --";
        if (textoErrorX != null) textoErrorX.text = "Error X: --";
        if (textoErrorZ != null) textoErrorZ.text = "Error Z: --";
        if (textoProfundidad != null) textoProfundidad.text = "Prof: --";
        if (textoEstado != null) textoEstado.text = "-- INACTIVO --";
    }
}