using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

// 1. CAMBIAMOS LA INTERFAZ A TÁCTIL (TouchHandler)
public class SelectorPunto : MonoBehaviour, IMixedRealityTouchHandler
{
    [Header("Configuración del Tornillo")]
    [Tooltip("El Prefab del Cilindro")]
    public GameObject prefabTornillo;
    [Tooltip("El objeto de la cabeza del fémur")]
    public Renderer mallaCabezaFemur;

    [Header("Configuración del Triángulo")]
    [Tooltip("Distancia en metros entre los tornillos (0.015 = 1.5 cm)")]
    public float separacion = 0.015f;

    // 2. NUEVA VARIABLE PARA QUE SOBRESALGA
    [Tooltip("Cuánto asoma el tornillo por fuera del hueso (0.01 = 1 cm)")]
    public float cuantoSobresale = 0.01f;

    // 3. NUEVO EVENTO: Cuando el dedo toca la malla
    public void OnTouchStarted(HandTrackingInputEventData eventData)
    {
        // 0. COMPROBACIONES DE SEGURIDAD
        if (mallaCabezaFemur == null || prefabTornillo == null)
        {
            Debug.LogError("Faltan asignar variables en el Inspector de SelectorPunto");
            return;
        }

        // 1. ORIGEN (PUNTA DE TU DEDO) Y DESTINO (CENTRO DE LA BOLA)
        Vector3 puntoOrigen = eventData.InputData; // Coordenada exacta del toque
        Vector3 puntoDestino = mallaCabezaFemur.bounds.center;
        Vector3 direccion = (puntoDestino - puntoOrigen).normalized;

        // 2. CREAR UNA "PLANTILLA" PERFECTA QUE NUNCA GIRA
        Quaternion rotacionGuia = Quaternion.LookRotation(direccion);
        Vector3 ejeDerecha = rotacionGuia * Vector3.right;
        Vector3 ejeArriba = rotacionGuia * Vector3.up;

        // 3. LA ROTACIÓN DEL CILINDRO
        Quaternion rotacionCilindro = Quaternion.FromToRotation(Vector3.up, direccion);
        float mitadAltura = prefabTornillo.transform.localScale.y;

        // --- CÁLCULO DEL TORNILLO 1 (INFERIOR PRINCIPAL) ---
        // Magia aquí: Le RESTAMOS dirección * cuantoSobresale para tirar de él hacia afuera
        Vector3 posTornillo1 = puntoOrigen + (direccion * mitadAltura) - (direccion * cuantoSobresale);
        GameObject tornillo1 = Instantiate(prefabTornillo, posTornillo1, rotacionCilindro);
        tornillo1.name = "Tornillo_1_Inferior";

        // --- MATEMÁTICAS DEL TRIÁNGULO ---
        float subida = separacion * 0.866f;
        float lado = separacion * 0.5f;

        RaycastHit hit;

        // === TORNILLO 2 (SUPERIOR IZQUIERDO) ===
        Vector3 puntoTeorico2 = puntoOrigen + (ejeArriba * subida) - (ejeDerecha * lado);
        Vector3 origenRayo2 = puntoTeorico2 - (direccion * 0.05f);

        if (Physics.Raycast(origenRayo2, direccion, out hit, 10.0f))
        {
            // Aplicamos el mismo efecto de sobresalir al tornillo 2
            Vector3 posFinal2 = hit.point + (direccion * mitadAltura) - (direccion * cuantoSobresale);
            GameObject tornillo2 = Instantiate(prefabTornillo, posFinal2, rotacionCilindro);
            tornillo2.name = "Tornillo_2_Superior_Izquierdo";
        }

        // === TORNILLO 3 (SUPERIOR DERECHO) ===
        Vector3 puntoTeorico3 = puntoOrigen + (ejeArriba * subida) + (ejeDerecha * lado);
        Vector3 origenRayo3 = puntoTeorico3 - (direccion * 0.05f);

        if (Physics.Raycast(origenRayo3, direccion, out hit, 10.0f))
        {
            // Aplicamos el mismo efecto de sobresalir al tornillo 3
            Vector3 posFinal3 = hit.point + (direccion * mitadAltura) - (direccion * cuantoSobresale);
            GameObject tornillo3 = Instantiate(prefabTornillo, posFinal3, rotacionCilindro);
            tornillo3.name = "Tornillo_3_Superior_Derecho";
        }

        Debug.Log("<color=cyan>¡Toque detectado!</color> Tornillos colocados y asomando.");
    }

    // Funciones obligatorias de la interfaz (las dejamos vacías porque solo nos importa cuando *empieza* el toque)
    public void OnTouchCompleted(HandTrackingInputEventData eventData) { }
    public void OnTouchUpdated(HandTrackingInputEventData eventData) { }
}