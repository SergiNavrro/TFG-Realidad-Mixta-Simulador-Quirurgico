using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;

public class GestionHueso : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El manipulador del fémur físico/virtual")]
    public ObjectManipulator manipuladorHueso;

    [Tooltip("El cerebro del botón de MRTK")]
    public ButtonConfigHelper configuradorBoton;

    [Header("Efecto Visual de Bloqueo")]
    [Tooltip("La cabeza del fémur")]
    public Renderer mallaCabeza;
    [Tooltip("El cuerpo del fémur")]
    public Renderer mallaCuerpo;

    [Tooltip("¡ARRASTRA AQUÍ TU MATERIAL DE BLOQUEO!")]
    public Material materialBloqueado;

    // Guardamos el estado actual
    private bool estaBloqueado = false;

    // Variables para memorizar los MATERIALES originales
    private Material materialOriginalCabeza;
    private Material materialOriginalCuerpo;

    void Start()
    {
        // Guardamos el material que tenga el hueso al empezar
        if (mallaCabeza != null) materialOriginalCabeza = mallaCabeza.material;
        if (mallaCuerpo != null) materialOriginalCuerpo = mallaCuerpo.material;
    }

    public void AlternarBloqueo()
    {
        // 1. Invertimos el estado
        estaBloqueado = !estaBloqueado;

        // 2. Activamos o desactivamos el movimiento
        if (manipuladorHueso != null)
        {
            manipuladorHueso.enabled = !estaBloqueado;
        }

        // 3. Cambiamos el texto del botón
        if (configuradorBoton != null)
        {
            if (estaBloqueado)
                configuradorBoton.MainLabelText = "Desbloquear Fémur";
            else
                configuradorBoton.MainLabelText = "Bloquear Fémur";
        }

        // 4. CAMBIAMOS EL MATERIAL COMPLETO
        if (estaBloqueado)
        {
            // Le ponemos tu material personalizado
            if (mallaCabeza != null && materialBloqueado != null) mallaCabeza.material = materialBloqueado;
            if (mallaCuerpo != null && materialBloqueado != null) mallaCuerpo.material = materialBloqueado;
        }
        else
        {
            // Le devolvemos su material original (el blanco normal)
            if (mallaCabeza != null) mallaCabeza.material = materialOriginalCabeza;
            if (mallaCuerpo != null) mallaCuerpo.material = materialOriginalCuerpo;
        }
    }
}