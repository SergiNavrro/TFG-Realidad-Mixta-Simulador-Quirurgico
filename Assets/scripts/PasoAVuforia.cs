using UnityEngine;
using Vuforia;
using Microsoft.MixedReality.Toolkit.UI;

public class PasoAVuforia : MonoBehaviour
{
    [Header("Referencias de Vuforia")]
    public ObserverBehaviour componenteNaranja;
    public DefaultObserverEventHandler componenteAzul;

    [Header("Referencias del Hueso")]
    public Transform huesoVirtual;
    private MeshRenderer rendererHueso; // Para ocultar la textura

    [Header("Control de Interfaz (Fases)")]
    public GameObject contenedorFase1; // Arrastra aquí el objeto con todos los botones de ahora
    public GameObject contenedorFase2; // Arrastra aquí el nuevo botón de "Hueso"

    void Start()
    {
        // Buscamos el MeshRenderer automáticamente al empezar
        if (huesoVirtual != null)
        {
            rendererHueso = huesoVirtual.GetComponentInChildren<MeshRenderer>();
        }

        // Aseguramos que la Fase 2 empiece apagada
        if (contenedorFase2 != null) contenedorFase2.SetActive(false);
    }

    public void IniciarSuperposicion()
    {
        // 1. BLOQUEO Y RESET (Lo que ya tenías)
        ObjectManipulator manipulador = huesoVirtual.GetComponent<ObjectManipulator>();
        if (manipulador != null) manipulador.enabled = false;

        huesoVirtual.localPosition = Vector3.zero;
        huesoVirtual.localRotation = Quaternion.identity;

        // 2. ENCENDER VUFORIA
        if (componenteNaranja != null) componenteNaranja.enabled = true;
        if (componenteAzul != null) componenteAzul.enabled = true;

        // 3. CAMBIO DE INTERFAZ (¡NUEVO!)
        if (contenedorFase1 != null) contenedorFase1.SetActive(false); // Oculta Fase 1
        if (contenedorFase2 != null) contenedorFase2.SetActive(true);  // Muestra Fase 2

        Debug.Log("<color=magenta>FASE 2 INICIADA:</color> UI Cambiada y Vuforia buscando...");
    }

    // Función para el nuevo botón de la Fase 2
    public void AlternarVisibilidadHueso()
    {
        if (rendererHueso != null)
        {
            rendererHueso.enabled = !rendererHueso.enabled;
            Debug.Log("Visibilidad del fémur: " + rendererHueso.enabled);
        }
    }
}