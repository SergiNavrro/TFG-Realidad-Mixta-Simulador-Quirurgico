using TMPro;
//using UnityEditor.PackageManager;
using UnityEngine;

public class EvaluadorAngular : MonoBehaviour
{
    [Header("Herramienta")]
    public Transform ejeHerramienta;
    public Renderer cilindroHerramientaRenderer;
    public Transform cilindroHerramienta;

    [Header("Hueso (se asigna automáticamente al crear el tornillo)")]
    private Transform ejeHueso;
    //public Transform cilindroHueso;

    [Header("Feedback Visual")]
    public Material matRojo;
    public Material matVerde;
    public Material matBlanco;

    [Header("Configuración")]
    public float toleranciaGrados = 3.0f;
    public float toleranciaPosicionMm = 2.0f;
    public float frecuenciaEvaluacion = 0.1f;

    [Header("HUD")]
    //public TextMeshProUGUI textoErrorX;
    public TextMeshProUGUI textoErrorZ;
    public TextMeshProUGUI textoErrorPosicion;

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
            //if (textoErrorX != null) textoErrorX.text = "Error X: --";
            if (textoErrorZ != null) textoErrorZ.text = "Error Z: --";
            if (textoErrorPosicion != null) textoErrorPosicion.text = "Posición: --";
        }
        else
        {
            estaEnVerde = true;
            timerEvaluacion = frecuenciaEvaluacion;
        }
    }

    void Update()
    {
        if (!activo || ejeHueso == null || ejeHerramienta == null) return;

        // Calculamos los puntos base de ambos cilindros
        Vector3 entradaTornillo = Vector3.zero;
        Vector3 puntaHerramienta = Vector3.zero;
        bool puntosValidos = false;
      
        if (cilindroHerramienta != null)
        {
            float longitudHueso = ejeHueso.lossyScale.y * 2f;
            float longitudHerramienta = cilindroHerramienta.lossyScale.y * 2f;

            entradaTornillo = ejeHueso.position - (ejeHueso.up * (longitudHueso / 2f));
            puntaHerramienta = cilindroHerramienta.position + (cilindroHerramienta.up * (longitudHerramienta / 2f));

            puntosValidos = true;

            // Debug visual en tiempo real
            Debug.DrawLine(entradaTornillo, puntaHerramienta, Color.magenta);
            Debug.DrawRay(entradaTornillo, Vector3.up * 0.01f, Color.blue);
            Debug.DrawRay(puntaHerramienta, Vector3.up * 0.01f, Color.red);
        }

        timerEvaluacion += Time.deltaTime;
        if (timerEvaluacion < frecuenciaEvaluacion) return;
        timerEvaluacion = 0f;

        // Error angular
        /*float errorX = Vector3.Angle(
            ejeHerramienta.TransformDirection(Vector3.right),
            ejeHueso.TransformDirection(Vector3.right)
        );

        float errorZ = Vector3.Angle(
            ejeHerramienta.TransformDirection(Vector3.forward),
            ejeHueso.TransformDirection(Vector3.forward)
        );
        if (textoErrorX != null) textoErrorX.text = $"Error X: {errorX:F1}°";
        if (textoErrorZ != null) textoErrorZ.text = $"Error Z: {errorZ:F1}°";
        */
        float errorInclinacion = Vector3.Angle(ejeHerramienta.up, ejeHueso.up);
        if (textoErrorZ != null) textoErrorZ.text = $"Error Y: {errorInclinacion:F1}°";
        // Error de posición
        float errorPosicionMm = 0f;
        bool posicionOk = false;

        if (puntosValidos)
        {
            errorPosicionMm = Vector3.Distance(entradaTornillo, puntaHerramienta) * 1000f;
            posicionOk = errorPosicionMm <= toleranciaPosicionMm;

            if (textoErrorPosicion != null)
                textoErrorPosicion.text = $"Posición: {errorPosicionMm:F1} mm";
        }

        // Verde solo si ángulo Y posición están dentro de tolerancia
        //bool deberiaEstarEnVerde = errorX <= toleranciaGrados
        //&& errorZ <= toleranciaGrados
        //&& posicionOk;
        bool deberiaEstarEnVerde = errorInclinacion <= toleranciaGrados && posicionOk;
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
}