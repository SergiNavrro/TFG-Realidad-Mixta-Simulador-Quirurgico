using UnityEngine;
// ¡IMPORTANTE! Añadimos esta línea para que Unity reconozca los scripts de MRTK
using Microsoft.MixedReality.Toolkit.UI;

public class GestionHueso : MonoBehaviour
{
    [Header("1. El Padre (Movimiento)")]
    [Tooltip("Arrastra aquí el objeto PADRE (femur_roto)")]
    public GameObject femurPadre;

    [Header("2. Las dos texturas")]
    public Material texturaNormal;
    public Material texturaTransparente;

    [Header("3. Los Hijos (Visual)")]
    [Tooltip("Arrastra aquí la cabeza y el cuerpo del fémur")]
    public Renderer[] mallasHijas;

    // Estado inicial
    private bool estaDesbloqueado = true;

    // Variables internas para guardar los componentes de movimiento
    private ObjectManipulator manipulador;

    void Start()
    {
        // Al arrancar, buscamos el componente que permite agarrar el hueso
        if (femurPadre != null)
        {
            manipulador = femurPadre.GetComponent<ObjectManipulator>();
        }
    }

    public void AlternarBloqueo()
    {
        estaDesbloqueado = !estaDesbloqueado;

        // 1. APAGAR EL MOVIMIENTO DIRECTAMENTE
        if (manipulador != null)
        {
            manipulador.enabled = estaDesbloqueado; // Si es falso, ya no se puede agarrar ni escalar
        }
        else
        {
            Debug.LogWarning("¡Ojo! No se ha encontrado el ObjectManipulator en el fémur padre.");
        }

        // 2. Elegir qué textura toca
        Material texturaActual = estaDesbloqueado ? texturaNormal : texturaTransparente;

        // 3. Aplicar la textura a todas las piezas hijas
        foreach (Renderer malla in mallasHijas)
        {
            if (malla != null)
            {
                malla.material = texturaActual;
            }
        }

        Debug.Log(estaDesbloqueado ? "MODO DESBLOQUEADO: Hueso libre y normal." : "MODO BLOQUEO: Físicas de agarre desactivadas y transparente.");
    }
}