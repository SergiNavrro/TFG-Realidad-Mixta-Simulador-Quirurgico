using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class GeneradorTriangulo : MonoBehaviour
{
    [Header("Configuración del Triángulo")]
    public GameObject prefabTornillo;
    public Transform huesoContenedor;

    [Header("Control de Separación (Slider)")]
    [Tooltip("Separación actual en metros")]
    public float separacion = 0.015f; // Por defecto 15mm
    public float separacionMinima = 0.005f; // 5mm
    public float separacionMaxima = 0.030f; // 30mm

    [Header("Ajuste de Proporción")]
    [Tooltip("La escala X original del tornillo de tu Prefab (ej: 0.007)")]
    public float escalaOriginalX = 0.007f;
    [Header("Referencia de la Herramienta")]
    public Transform cilindroHerramienta; // Arrastra aquí el cilindro que sigue al ArUco
    public enum EjeHueso { X_Right, Y_Up, Z_Forward, MenosX_Left, MenosY_Down, MenosZ_Back }

    [Header("Ajuste de Ejes")]
    public EjeHueso ejeParaSubir = EjeHueso.Z_Forward;
    [Header("Ajuste de Láseres")]
    public float alcanceDelRayo = 2.0f; // Pon aquí 5, 10 o lo que necesites en el Inspector
    [Header("Interfaz UI")]
    [Tooltip("Arrastra aquí el PinchSlider desde el Inspector")]
    public PinchSlider sliderUI;

    // Añade esto junto a las otras variables privadas
    [HideInInspector] public static float profundidadCalculada = 0f;
    [HideInInspector] public static float distanciaParedIzq = 0f;
    [HideInInspector] public static float distanciaParedDer = 0f;
    [HideInInspector] public static float separacionFinal = 0f;

    // Cerrojo de seguridad para evitar bucles infinitos al mover el slider por código
    private bool actualizandoSliderManual = false;

    // Guardamos las referencias de los tornillos para poder moverlos después de crearlos
    private GameObject tornilloIzqGenerado;
    private GameObject tornilloDerGenerado;
    // Guardará la separación máxima permitida en el momento en que se crean los tornillos
    private float limiteSeparacionFijo = 0.030f;
    // BOTÓN: Crea los tornillos si no existen y los coloca
    public void GenerarSuperiores()
    {
        GameObject guia = GameObject.Find("Tornillo_Manual");
        if (guia == null)
        {
            Debug.LogWarning("¡Falta el tornillo guía!");
            return;
        }

        // Si no existen, los instanciamos
        if (tornilloIzqGenerado == null)
        {
            tornilloIzqGenerado = Instantiate(prefabTornillo, guia.transform.position, guia.transform.rotation);
            tornilloIzqGenerado.name = "Tornillo_Superior_Izquierdo";
            if (huesoContenedor != null) tornilloIzqGenerado.transform.SetParent(huesoContenedor, true);
            tornilloIzqGenerado.transform.localScale = guia.transform.localScale;
            ApagarAgarre(tornilloIzqGenerado);
        }   

        if (tornilloDerGenerado == null)
        {
            tornilloDerGenerado = Instantiate(prefabTornillo, guia.transform.position, guia.transform.rotation);
            tornilloDerGenerado.name = "Tornillo_Superior_Derecho";
            if (huesoContenedor != null) tornilloDerGenerado.transform.SetParent(huesoContenedor, true);
            tornilloDerGenerado.transform.localScale = guia.transform.localScale;
            ApagarAgarre(tornilloDerGenerado);
        }

        // Calculamos y aplicamos la posición exacta
        // Calculamos y aplicamos la posición exacta (y los láseres los empujan si chocan)
        RecalcularPosiciones();

        // --- NUEVO: ESTABLECER EL BLOQUE ---
        // Medimos la distancia real a la que han quedado tras crearse
        float distanciaReal = Vector3.Distance(tornilloIzqGenerado.transform.position, tornilloDerGenerado.transform.position);
        float factorDeProporcion = guia.transform.lossyScale.x / escalaOriginalX;

        // Guardamos esta distancia como el tope máximo inamovible
        limiteSeparacionFijo = distanciaReal / factorDeProporcion;
        separacionFinal = separacion;
    }

    // SLIDER MRTK: Este es el método que tienes que enlazar en el PinchSlider de MRTK
    public void AlMoverSliderMRTK(SliderEventData eventData)
    {
        // 1. Calculamos lo que el usuario QUIERE hacer
        float separacionDeseada = Mathf.Lerp(separacionMinima, separacionMaxima, eventData.NewValue);

        // 2. Si intenta abrirlo más del tope inicial, lo bloqueamos en ese tope
        if (separacionDeseada > limiteSeparacionFijo)
        {
            separacion = limiteSeparacionFijo;
        }
        else
        {
            // Si lo está haciendo más pequeño (cerrando el triángulo), le dejamos
            separacion = separacionDeseada;
        }
        separacionFinal = separacion;
        // 3. Movemos los tornillos
        if (tornilloIzqGenerado != null && tornilloDerGenerado != null)
        {
            RecalcularPosiciones();
        }
    }
    // Función temporal para visualizar los rayos radiales del tornillo
    // Ahora recibe directamente el vector de desplazamiento

    private void LanzarLaseresProfundida(Transform tornilloRef)
    {
        int mascaraHueso = LayerMask.GetMask("Hueso");

        float factorEscala = tornilloRef.lossyScale.x / escalaOriginalX;
        float margenSeguridad = 0.015f * factorEscala;

        float longitudDelCilindro = tornilloRef.lossyScale.y * 2f;
        float duracionLinea = 30.0f;
        Vector3 puntoBase = tornilloRef.position - (tornilloRef.up * (longitudDelCilindro / 2f));

        // 1. EL RAYO DE ENTRADA (Hacia adelante)
        RaycastHit hitEntrada;
        bool tocoEntrada = Physics.Raycast(puntoBase, tornilloRef.up, out hitEntrada, 2.0f, mascaraHueso);

        if (tocoEntrada)
        {
            // 2. EL TRUCO DEL RAYO INVERSO (Para encontrar el fondo real)
            Vector3 puntoLejano = puntoBase + (tornilloRef.up * 2.0f);

            RaycastHit hitSalida;
            bool tocoSalida = Physics.Raycast(puntoLejano, -tornilloRef.up, out hitSalida, 2.0f, mascaraHueso);

            if (tocoSalida)
            {
                // --- EL CALCULO CLINICO ---
                float distanciaEntrada = Vector3.Distance(puntoBase, hitEntrada.point);
                float distanciaSalida = Vector3.Distance(puntoBase, hitSalida.point);

                // Grosor real del hueso de pared a pared
                float huesoTotal = distanciaSalida - distanciaEntrada;

                // Longitud de tornillo anclada en hueso, parada 15 mm antes del fondo.
                // ESTA es la profundidad optima de insercion (lo que se reporta).
                float longitudAncladaSegura = huesoTotal - margenSeguridad;

                // Longitud TOTAL del cilindro virtual desde su base (incluye el tramo base->hueso).
                // Solo se usa para escalar el cilindro, NO se reporta.
                float tornilloTotalSeguro = distanciaEntrada + longitudAncladaSegura;

                // CORREGIDO: guardamos la profundidad clinica (en hueso), no la del cilindro
                profundidadCalculada = longitudAncladaSegura;

                // Escalado del cilindro herramienta (sigue usando la longitud total desde la base)
                if (cilindroHerramienta != null)
                {
                    float escalaYAnterior = cilindroHerramienta.localScale.y;
                    float nuevaEscalaY = (tornilloTotalSeguro / factorEscala) / 2f;

                    Vector3 puntaAntes = cilindroHerramienta.position -
                                         (cilindroHerramienta.up * (escalaYAnterior * 2f / 2f));

                    cilindroHerramienta.localScale = new Vector3(
                        cilindroHerramienta.localScale.x,
                        nuevaEscalaY,
                        cilindroHerramienta.localScale.z
                    );

                    Vector3 puntaDespues = cilindroHerramienta.position -
                                           (cilindroHerramienta.up * (nuevaEscalaY * 2f / 2f));

                    cilindroHerramienta.position -= (puntaAntes - puntaDespues);

                    Debug.Log($"Cilindro (base->punta): {tornilloTotalSeguro * 1000f:F1} mm");
                }

                // Ficha clinica por consola
                Debug.Log($"--- REPORTE CLINICO: {tornilloRef.name} ---");
                Debug.Log($"1. Hueco (Base -> Entrada): {distanciaEntrada * 1000f:F1} mm");
                Debug.Log($"2. Grosor total del hueso: {huesoTotal * 1000f:F1} mm");
                Debug.Log($"3. PROFUNDIDAD OPTIMA (anclada en hueso, -15mm): {longitudAncladaSegura * 1000f:F1} mm");
            }
        }
        else
        {
            Debug.LogWarning($"El rayo de {tornilloRef.name} no ha tocado el hueso.");
        }
    }
    private void DispararLáseresVisuales(Transform tornilloRef, Vector3 direccionDesplazamiento)
    {
        Physics.queriesHitBackfaces = true;
        int mascaraHueso = LayerMask.GetMask("Hueso");

        float factorEscala = tornilloRef.lossyScale.x / escalaOriginalX;
        float longitudRayo = 0.05f * factorEscala;
        float longitudTornillo = 0.08f * factorEscala;
        float paso = 0.01f * factorEscala;
        float distanciaMinimaPermitida = 0.005f * factorEscala;

        float duracionLinea = 1.0f;
        float inicio = -longitudTornillo / 2f;
        float fin = longitudTornillo / 2f;

        Vector3 direccionRadial = direccionDesplazamiento.normalized;

        float maximaViolacion = 0f;
        float distanciaMinima = float.MaxValue;

        for (float offsetY = inicio; offsetY <= fin; offsetY += paso)
        {
            Vector3 origenNivel = tornilloRef.position + (tornilloRef.up * offsetY);
            Vector3 finDelLaser = origenNivel + (direccionRadial * longitudRayo);

            RaycastHit hit;

            if (Physics.Raycast(origenNivel, direccionRadial, out hit, longitudRayo, mascaraHueso))
            {
                Debug.DrawLine(origenNivel, finDelLaser, Color.green, duracionLinea);

                float tamañoCruz = 0.002f;
                Debug.DrawRay(hit.point, Vector3.up * tamañoCruz, Color.red, duracionLinea);
                Debug.DrawRay(hit.point, Vector3.down * tamañoCruz, Color.red, duracionLinea);
                Debug.DrawRay(hit.point, Vector3.right * tamañoCruz, Color.red, duracionLinea);
                Debug.DrawRay(hit.point, Vector3.left * tamañoCruz, Color.red, duracionLinea);

                if (hit.distance < distanciaMinima)
                {
                    distanciaMinima = hit.distance;
                }
                if (hit.distance < distanciaMinimaPermitida)
                {
                    float violacion = distanciaMinimaPermitida - hit.distance;
                    if (violacion > maximaViolacion)
                    {
                        maximaViolacion = violacion;
                    }
                }
            }
            else
            {
                Debug.DrawLine(origenNivel, finDelLaser, Color.yellow, duracionLinea);
            }
        }

        if (tornilloRef.name == "Tornillo_Superior_Izquierdo")
            distanciaParedIzq = distanciaMinima;
        else if (tornilloRef.name == "Tornillo_Superior_Derecho")
            distanciaParedDer = distanciaMinima;

        // --- REPORTE POR PANTALLA (distancia a la pared) ---
        Debug.Log($"--- PARED: {tornilloRef.name} ---");
        if (distanciaMinima < float.MaxValue)
            Debug.Log($"Distancia minima a la pared: {distanciaMinima * 1000f:F1} mm " +
                      $"(margen minimo: {distanciaMinimaPermitida * 1000f:F1} mm)");
        else
            Debug.LogWarning("Ningun laser toco la pared (el tornillo esta fuera del alcance del rayo).");

        // --- REPOSICIONAMIENTO AUTOMATICO (CONSTRAINT SOLVER) ---
        if (maximaViolacion > 0f)
        {
            tornilloRef.position -= direccionRadial * maximaViolacion;
            Debug.Log($"CORRECCION: {tornilloRef.name} empujado {maximaViolacion * 1000f:F1} mm " +
                      $"hacia el centro por seguridad.");
        }
        else
        {
            Debug.Log($"{tornilloRef.name}: sin correccion (distancia segura).");
        }
    }

    // Toda tu matemática perfecta de antes, ahora en su propia función
    private void RecalcularPosiciones()
    {
        GameObject guia = GameObject.Find("Tornillo_Manual");
        if (guia == null) return;

        // 1. REGLA DE TRES
        float factorDeProporcion = guia.transform.lossyScale.x / escalaOriginalX;
        float ladoTriangulo = separacion * factorDeProporcion;

        // 2. MATEMÁTICA DEL TRIÁNGULO EQUILÁTERO PERFECTO
        float alturaTriangulo = ladoTriangulo * (Mathf.Sqrt(3) / 2f);
        float mitadBase = ladoTriangulo / 2f;

        // 3. LA MAGIA: EL NIVEL DE ALBAÑIL
        Vector3 ejeSubirHueso = ObtenerVectorEje(ejeParaSubir);
        Vector3 ejeTornillo = guia.transform.up;

        Vector3 vectorNiveladoLados = Vector3.Cross(ejeTornillo, ejeSubirHueso).normalized;
        Vector3 vectorNiveladoSubida = Vector3.Cross(vectorNiveladoLados, ejeTornillo).normalized;

        // 4. POSICIONES FINALES
        Vector3 centroIzq = guia.transform.position + (vectorNiveladoSubida * alturaTriangulo) - (vectorNiveladoLados * mitadBase);
        Vector3 centroDer = guia.transform.position + (vectorNiveladoSubida * alturaTriangulo) + (vectorNiveladoLados * mitadBase);

        // 5. MOVEMOS LOS TORNILLOS EXISTENTES
        tornilloIzqGenerado.transform.position = centroIzq;
        tornilloIzqGenerado.transform.rotation = guia.transform.rotation;

        tornilloDerGenerado.transform.position = centroDer;
        tornilloDerGenerado.transform.rotation = guia.transform.rotation;

        // --- NUEVO: CÁLCULO INFALIBLE DE DIRECCIÓN ---
        // Calculamos el vector exacto restando la posición de destino menos la de origen (el centro)
        // Esto crea una flecha que apunta estrictamente hacia afuera en la dirección que se han movido.
        Vector3 direccionAfueraIzq = (tornilloIzqGenerado.transform.position - guia.transform.position).normalized;
        Vector3 direccionAfueraDer = (tornilloDerGenerado.transform.position - guia.transform.position).normalized;

        DispararLáseresVisuales(tornilloIzqGenerado.transform, direccionAfueraIzq);
        DispararLáseresVisuales(tornilloDerGenerado.transform, direccionAfueraDer);
        LanzarLaseresProfundida(guia.transform);

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

    private void ApagarAgarre(GameObject tornillo)
    {
        ObjectManipulator manipulador = tornillo.GetComponent<ObjectManipulator>();
        if (manipulador != null) manipulador.enabled = false;
    }
}   
