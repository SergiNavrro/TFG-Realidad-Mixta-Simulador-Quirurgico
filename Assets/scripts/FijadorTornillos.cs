using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;

public class FijadorTornillo : MonoBehaviour
{
    [Header("Referencias del Hueso")]
    public Transform huesoPadre;
    public Renderer mallaCabezaFemur;
    public float offsetZ = -0.02f;

    [Header("Ajuste Fino (Slider Ángulo)")]
    public PinchSlider sliderAngulo;

    // --- NUEVA SECCIÓN: Visualización del Punto ---
    [Header("Visualización de la Diana")]
    [Tooltip("Arrastra aquí el PREFAB del marcador (la bolita pequeña)")]
    public GameObject marcadorDianaPrefab;

    // Variable interna para guardar la bolita que dibujamos en la escena
    private GameObject marcadorInstanciado;
    // ----------------------------------------------

    private bool tornilloBloqueado = false;
    private GameObject tornilloActual;
    private Quaternion rotacionBasePerfecta;
    private Vector3 posicionBasePerfecta;

    public void AlternarFijacion()
    {
        tornilloActual = GameObject.Find("Tornillo_Manual");
        if (tornilloActual == null) return;

        ObjectManipulator manipulador = tornilloActual.GetComponent<ObjectManipulator>();
        tornilloBloqueado = !tornilloBloqueado;

        if (tornilloBloqueado)
        {
            // --- MODO FIJAR ---

            // A. Si ya había un marcador de antes (de otra prueba), lo borramos para limpiar
            if (marcadorInstanciado != null) Destroy(marcadorInstanciado);

            if (mallaCabezaFemur != null)
            {
                // 1. Cálculos de la bisagra (idénticos a los de antes)
                float mitadAltura = tornilloActual.transform.lossyScale.y;
                Vector3 puntoBaseFijo = tornilloActual.transform.position - (tornilloActual.transform.up * mitadAltura);
                Vector3 centroCabeza = mallaCabezaFemur.bounds.center;

                // 2. Calculamos EL PUNTO DIANA
                Vector3 puntoDestino = centroCabeza + new Vector3(0, 0, offsetZ);

                // --- NUEVO: DIBUJAR LA DIANA ---
                if (marcadorDianaPrefab != null)
                {
                    // Creamos la bolita en el punto exacto calculated
                    marcadorInstanciado = Instantiate(marcadorDianaPrefab, puntoDestino, Quaternion.identity);
                    marcadorInstanciado.name = "Marcador_Punto_Diana";

                    // ¡IMPORTANTE! Lo hacemos hijo del fémur padre.
                    // Así, si mueves el hueso, la diana se mueve con él y sigue marcando el sitio correcto.
                    if (huesoPadre != null)
                    {
                        marcadorInstanciado.transform.SetParent(huesoPadre, true);
                    }
                }
                // -------------------------------

                // 3. Aplicamos la rotación bisagra (idéntico a antes)
                Vector3 direccionPerfecta = (puntoDestino - puntoBaseFijo).normalized;
                tornilloActual.transform.up = direccionPerfecta;
                tornilloActual.transform.position = puntoBaseFijo + (tornilloActual.transform.up * mitadAltura);
            }

            if (manipulador != null) manipulador.enabled = false;
            if (huesoPadre != null) tornilloActual.transform.SetParent(huesoPadre, true);

            rotacionBasePerfecta = tornilloActual.transform.rotation;
            posicionBasePerfecta = tornilloActual.transform.position;
            if (sliderAngulo != null) sliderAngulo.SliderValue = 0.5f;

            Debug.Log("<color=green>TORNILLO FIJADO</color> y Diana dibujada.");
        }
        else
        {
            // --- MODO DESBLOQUEAR ---
            if (manipulador != null) manipulador.enabled = true;
            tornilloActual.transform.SetParent(null, true);

            // --- NUEVO: BORRAR LA DIANA ---
            // Cuando soltamos el tornillo, limpiamos la escena borrando el marcador
            if (marcadorInstanciado != null) Destroy(marcadorInstanciado);
            // ------------------------------

            Debug.Log("<color=yellow>TORNILLO DESBLOQUEADO</color>");
        }
    }

    public void AjustarAnguloConSlider(SliderEventData eventData)
    {
        if (!tornilloBloqueado || tornilloActual == null) return;
        float gradosGiro = (eventData.NewValue - 0.5f) * 30f;
        tornilloActual.transform.rotation = rotacionBasePerfecta;
        tornilloActual.transform.position = posicionBasePerfecta;
        float mitadAltura = tornilloActual.transform.lossyScale.y;
        Vector3 puntoBaseFijo = tornilloActual.transform.position - (tornilloActual.transform.up * mitadAltura);
        tornilloActual.transform.RotateAround(puntoBaseFijo, tornilloActual.transform.up, gradosGiro);
    }
}