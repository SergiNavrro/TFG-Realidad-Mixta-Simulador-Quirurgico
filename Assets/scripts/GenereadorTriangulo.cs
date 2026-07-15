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
        // Solo necesitamos el Raycast normal, el primero que toque será la cortical lateral
        bool tocoEntrada = Physics.Raycast(puntoBase, tornilloRef.up, out hitEntrada, 2.0f, mascaraHueso);

        if (tocoEntrada)
        {
            // 2. EL TRUCO DEL RAYO INVERSO (Para encontrar el fondo real)
            Vector3 puntoLejano = puntoBase + (tornilloRef.up * 2.0f);

            RaycastHit hitSalida;
            // Rayo hacia atrás
            bool tocoSalida = Physics.Raycast(puntoLejano, -tornilloRef.up, out hitSalida, 2.0f, mascaraHueso);

            if (tocoSalida)
            {
                // --- EL CÁLCULO CLÍNICO ---
                float distanciaEntrada = Vector3.Distance(puntoBase, hitEntrada.point);
                float distanciaSalida = Vector3.Distance(puntoBase, hitSalida.point);

                // El grosor total del hueso de pared a pared
                float huesoTotal = distanciaSalida - distanciaEntrada;

                // --- NUEVO: APLICACIÓN DEL MARGEN DE SEGURIDAD ---
                //float margenSeguridad = 0.015f; // 1.5 cm de margen de seguridad

                // Lo que realmente va a morder el tornillo dentro del hueso sin llegar al final
                float longitudAncladaSegura = huesoTotal - margenSeguridad;

                // Donde ya tienes esto:
                float tornilloTotalSeguro = distanciaEntrada + longitudAncladaSegura;
                profundidadCalculada = tornilloTotalSeguro;

                // Añade esto justo debajo: cilindro herramienta
                if (cilindroHerramienta != null)
                {
                    // 1. Guardamos la punta ANTES de escalar
                    float escalaYAnterior = cilindroHerramienta.localScale.y;
                    float nuevaEscalaY = (tornilloTotalSeguro / factorEscala) / 2f;

                    Vector3 puntaAntes = cilindroHerramienta.position -
                                         (cilindroHerramienta.up * (escalaYAnterior * 2f / 2f));

                    // 2. Escalamos
                    cilindroHerramienta.localScale = new Vector3(
                        cilindroHerramienta.localScale.x,
                        nuevaEscalaY,
                        cilindroHerramienta.localScale.z
                    );

                    // 3. Calculamos dónde ha quedado la punta después
                    Vector3 puntaDespues = cilindroHerramienta.position -
                                           (cilindroHerramienta.up * (nuevaEscalaY * 2f / 2f));

                    // 4. Compensamos para que la punta quede fija
                    cilindroHerramienta.position -= (puntaAntes - puntaDespues);

                    Debug.Log($"Cilindro ajustado a: {tornilloTotalSeguro * 1000f:F1} mm");
                }

                // Imprimimos la ficha clínica
                Debug.Log($"--- REPORTE CLÍNICO: {tornilloRef.name} ---");
                Debug.Log($"1. Hueco (Base -> Entrada): {distanciaEntrada * 1000f:F1} mm");
                Debug.Log($"2. Grosor total del hueso: {huesoTotal * 1000f:F1} mm");
                Debug.Log($"3. Longitud de tornillo SEGURA (-15mm): {tornilloTotalSeguro * 1000f:F1} mm");

                // --- NUEVO: REESCALADO DE LA HERRAMIENTA ---
                // Si hemos enlazado el cilindro de la herramienta en el Inspector, le cambiamos el tamaño
                
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

        //float distanciaMinimaPermitida = 0.005f; // 5 mm de seguridad
        float maximaViolacion = 0f; // Guardará cuánto nos hemos pasado del límite
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
                // Calculamos si este rayo está a menos de 5 mm de la pared
                if (hit.distance < distanciaMinimaPermitida)
                {
                    // Cuánto nos falta para llegar a los 5 mm de seguridad
                    float violacion = distanciaMinimaPermitida - hit.distance;
                    // Nos quedamos con la corrección más grande de todo el cilindro
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
        // Guardamos la distancia mínima en la variable estática correcta
        if (tornilloRef.name == "Tornillo_Superior_Izquierdo")
            distanciaParedIzq = distanciaMinima;
        else if (tornilloRef.name == "Tornillo_Superior_Derecho")
            distanciaParedDer = distanciaMinima;
        // --- REPOSICIONAMIENTO AUTOMÁTICO (CONSTRAINT SOLVER) ---
        if (maximaViolacion > 0f)
        {
            // Movemos el tornillo hacia atrás (restando la dirección) exactamente la distancia necesaria
            tornilloRef.position -= direccionRadial * maximaViolacion;

            // Imprimimos el ajuste por consola para verificar
            Debug.Log($"Reposicionado {tornilloRef.name}: empujado {maximaViolacion * 1000f:F1} mm hacia el centro por seguridad.");
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