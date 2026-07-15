using UnityEngine;

/// <summary>
/// Toggle para mostrar u ocultar el canvas de metricas Y los ejes de los cilindros.
/// El tornillo inferior se busca dinamicamente porque se crea durante el flujo.
/// </summary>
public class ModoEntrenamiento : MonoBehaviour
{
    [Header("Canvas de metricas")]
    [Tooltip("Arrastra aqui el Canvas con el HUD de metricas")]
    public GameObject canvasMetricas;

    [Header("Ejes de la herramienta (existe desde el inicio)")]
    [Tooltip("DibujarEjes del cilindro de la herramienta")]
    public DibujarEjes ejesHerramienta;

    [Header("Tornillo inferior - busqueda dinamica")]
    [Tooltip("Nombre del GameObject del tornillo inferior que se crea durante el flujo")]
    public string nombreTornilloInferior = "Tornillo_Manual";

    [Header("Estado inicial")]
    [Tooltip("True = canvas + ejes visibles al arrancar (modo entrenamiento)")]
    public bool visibleAlArrancar = true;

    private bool elementosVisibles;

    void Start()
    {
        elementosVisibles = visibleAlArrancar;
        AplicarEstado();
    }

    // Llama a este metodo desde el boton de la HMI
    public void ToggleCanvas()
    {
        elementosVisibles = !elementosVisibles;
        AplicarEstado();

        Debug.Log($"[ModoEntrenamiento] {(elementosVisibles ? "ENTRENAMIENTO (visible)" : "EVALUACION (oculto)")}");
    }

    private void AplicarEstado()
    {
        // Canvas
        if (canvasMetricas != null)
            canvasMetricas.SetActive(elementosVisibles);

        // Ejes herramienta (referencia directa)
        if (ejesHerramienta != null)
        {
            if (elementosVisibles) ejesHerramienta.MostrarEjes();
            else ejesHerramienta.OcultarEjes();
        }

        // Ejes tornillo inferior (busqueda dinamica)
        DibujarEjes ejesTornillo = BuscarEjesTornilloInferior();
        if (ejesTornillo != null)
        {
            if (elementosVisibles) ejesTornillo.MostrarEjes();
            else ejesTornillo.OcultarEjes();
        }
    }

    private DibujarEjes BuscarEjesTornilloInferior()
    {
        GameObject tornillo = GameObject.Find(nombreTornilloInferior);
        if (tornillo == null) return null;
        return tornillo.GetComponent<DibujarEjes>();
    }

    public void MostrarCanvas()
    {
        elementosVisibles = true;
        AplicarEstado();
    }

    public void OcultarCanvas()
    {
        elementosVisibles = false;
        AplicarEstado();
    }
}