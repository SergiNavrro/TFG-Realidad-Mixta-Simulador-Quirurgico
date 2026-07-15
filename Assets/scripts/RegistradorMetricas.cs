using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class RegistradorMetricas : MonoBehaviour
{
    [Header("Referencias")]
    public EvaluadorAngular evaluador;

    [Header("HUD - Estado de Grabación")]
    public TextMeshProUGUI textoGrabacion;

    [Header("HUD - Ruta de Guardado")]
    public TextMeshProUGUI textoRuta;

    [Header("Configuración")]
    [Tooltip("Frecuencia de muestreo en Hz")]
    public float frecuenciaMuestreo = 30f;

    [Header("Tolerancia ESTRICTA para inicio de grabación")]
    [Tooltip("Solo arrancará a grabar si el error angular es menor que esto (grados)")]
    public float toleranciaInicioGrados = 1.0f;

    [Tooltip("Solo arrancará a grabar si los errores X y Z son menores que esto (mm)")]
    public float toleranciaInicioMm = 1.0f;

    // Estados internos
    private bool grabando = false;
    private float timerMuestreo = 0f;
    private float tiempoInicio = 0f;
    private int numeroInsercion = 0;

    // Buffer en memoria
    private List<string> muestrasTrayectoria;

    // Rutas
    private string RutaDocuments => System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
    private string RutaPictures => System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures);

    void Start()
    {
        muestrasTrayectoria = new List<string>(600);
        numeroInsercion = ContarInsercionesPrevias();
        Debug.Log($"[RegistradorMetricas] Carpeta Documents: {RutaDocuments}");
        Debug.Log($"[RegistradorMetricas] Carpeta Pictures: {RutaPictures}");
        Debug.Log($"[RegistradorMetricas] Próxima inserción: #{numeroInsercion + 1}");

        if (textoGrabacion != null)
        {
            textoGrabacion.text = "● Esperando inserción";
        }

        if (textoRuta != null)
        {
            textoRuta.text = $"DOC: {RutaDocuments}\nPIC: {RutaPictures}";
        }
    }

    void Update()
    {
        if (evaluador == null || !evaluador.evaluacionActiva) return;

        // Detectar inicio de inserción con TOLERANCIA ESTRICTA
        // Solo arranca si: profundidad >= 0 Y errores dentro de tolerancia estricta
        if (!grabando && evaluador.ultimaProfundidad >= 0f && CumpleToleranciaInicio())
        {
            IniciarGrabacion();
        }

        // Grabar muestras durante la inserción
        if (grabando)
        {
            timerMuestreo += Time.deltaTime;
            float intervaloMuestreo = 1f / frecuenciaMuestreo;

            if (timerMuestreo >= intervaloMuestreo)
            {
                timerMuestreo = 0f;
                GuardarMuestra();
            }

            // Actualizar HUD durante grabación
            if (textoGrabacion != null)
            {
                float t = Time.time - tiempoInicio;
                textoGrabacion.text = $"● INICIO REC {t:F1}s ({muestrasTrayectoria.Count})";
            }

            // Detectar fin de inserción
            if (evaluador.ultimoPorcentaje >= 100f)
            {
                FinalizarGrabacion();
            }
        }
    }

    /// <summary>
    /// Verifica si en este instante se cumple la tolerancia ESTRICTA para arrancar la grabacion.
    /// Mas exigente que la del estado OK visual (5mm/5°). 
    /// Garantiza que la insercion empieza bien apuntada.
    /// </summary>
    private bool CumpleToleranciaInicio()
    {
        return evaluador.ultimoErrorAngular <= toleranciaInicioGrados
            && Mathf.Abs(evaluador.ultimoErrorX) <= toleranciaInicioMm
            && Mathf.Abs(evaluador.ultimoErrorZ) <= toleranciaInicioMm;
    }

    private void IniciarGrabacion()
    {
        grabando = true;
        tiempoInicio = Time.time;
        muestrasTrayectoria.Clear();
        numeroInsercion++;

        if (textoGrabacion != null)
            textoGrabacion.text = $"● INICIO inserción #{numeroInsercion}";

        Debug.Log($"[RegistradorMetricas] INICIO inserción #{numeroInsercion} " +
                  $"(errores iniciales: ang={evaluador.ultimoErrorAngular:F2}°, " +
                  $"X={evaluador.ultimoErrorX:F2}mm, Z={evaluador.ultimoErrorZ:F2}mm)");
    }

    private void GuardarMuestra()
    {
        float t = Time.time - tiempoInicio;
        string linea = $"{t:F3};" +
                       $"{evaluador.ultimoErrorX:F2};" +
                       $"{evaluador.ultimoErrorZ:F2};" +
                       $"{evaluador.ultimoErrorAngular:F2};" +
                       $"{evaluador.ultimaProfundidad:F2};" +
                       $"{evaluador.ultimoPorcentaje:F1};" +
                       $"{(evaluador.ultimoEstadoOk ? "OK" : "NOK")}";

        muestrasTrayectoria.Add(linea);
    }

    private void FinalizarGrabacion()
    {
        grabando = false;

        string carpetaDocs = Path.Combine(RutaDocuments, $"insercion_{numeroInsercion:D3}");
        string carpetaPics = Path.Combine(RutaPictures, $"insercion_{numeroInsercion:D3}");

        string resultadoDocs = "";
        string resultadoPics = "";

        try
        {
            Directory.CreateDirectory(carpetaDocs);
            EscribirTrayectoria(carpetaDocs);
            EscribirPlanificacion(carpetaDocs);
            resultadoDocs = "OK";
            Debug.Log($"[RegistradorMetricas] Guardado en Documents: {carpetaDocs}");
        }
        catch (System.Exception e)
        {
            resultadoDocs = $"ERR: {e.Message}";
            Debug.LogWarning($"[RegistradorMetricas] Error guardando en Documents: {e.Message}");
        }

        try
        {
            Directory.CreateDirectory(carpetaPics);
            EscribirTrayectoria(carpetaPics);
            EscribirPlanificacion(carpetaPics);
            resultadoPics = "OK";
            Debug.Log($"[RegistradorMetricas] Guardado en Pictures: {carpetaPics}");
        }
        catch (System.Exception e)
        {
            resultadoPics = $"ERR: {e.Message}";
            Debug.LogWarning($"[RegistradorMetricas] Error guardando en Pictures: {e.Message}");
        }

        if (textoGrabacion != null)
            textoGrabacion.text = $"✓ FIN #{numeroInsercion} ({muestrasTrayectoria.Count} muestras)\nEsperando próxima inserción...";

        if (textoRuta != null)
            textoRuta.text = $"DOC: {resultadoDocs}\nPIC: {resultadoPics}\n{carpetaPics}";

        Debug.Log($"[RegistradorMetricas] FIN inserción #{numeroInsercion}");
    }

    private void EscribirTrayectoria(string carpeta)
    {
        string ruta = Path.Combine(carpeta, "trayectoria.csv");
        using (StreamWriter writer = new StreamWriter(ruta))
        {
            writer.WriteLine("timestamp_s;errorX_mm;errorZ_mm;errorAngular_deg;profundidad_mm;porcentaje;estado");
            foreach (string linea in muestrasTrayectoria)
            {
                writer.WriteLine(linea);
            }
        }
    }

    private void EscribirPlanificacion(string carpeta)
    {
        string ruta = Path.Combine(carpeta, "planificacion.csv");
        using (StreamWriter writer = new StreamWriter(ruta))
        {
            writer.WriteLine("parametro;valor_mm");
            writer.WriteLine($"profundidad_tornillo;{GeneradorTriangulo.profundidadCalculada * 1000f:F2}");
            writer.WriteLine($"distancia_pared_izq;{GeneradorTriangulo.distanciaParedIzq * 1000f:F2}");
            writer.WriteLine($"distancia_pared_der;{GeneradorTriangulo.distanciaParedDer * 1000f:F2}");
            writer.WriteLine($"separacion_triangulo;{GeneradorTriangulo.separacionFinal * 1000f:F2}");
        }
    }

    private int ContarInsercionesPrevias()
    {
        int count = 0;
        if (Directory.Exists(RutaDocuments))
            count = Directory.GetDirectories(RutaDocuments, "insercion_*").Length;
        if (count == 0 && Directory.Exists(RutaPictures))
            count = Directory.GetDirectories(RutaPictures, "insercion_*").Length;
        return count;
    }
}