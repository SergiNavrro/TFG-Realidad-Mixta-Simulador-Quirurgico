using UnityEngine;

public class ControladorFases : MonoBehaviour
{
    [Header("Paneles de Interfaz")]
    public GameObject panelFase1;
    public GameObject panelFase2;

    [Header("Referencia al Hueso")]
    [Tooltip("La cabeza del fémur")]
    public Renderer mallaCabeza;
    [Tooltip("El cuerpo del fémur")]
    public Renderer mallaCuerpo;

    [Header("Efecto Rayos X")]
    [Tooltip("Crea un Material en Unity, ponlo en Transparent, y arrástralo aquí")]
    public Material materialTransparente;

    // Variables internas para no perder los materiales originales
    private Material materialOriginalCabeza;
    private Material materialOriginalCuerpo;

    // Un "interruptor" para saber en qué estado estamos
    private bool estaTransparente = false;

    void Start()
    {
        // Guardamos la pintura (material) original de los huesos al empezar
        if (mallaCabeza != null) materialOriginalCabeza = mallaCabeza.material;
        if (mallaCuerpo != null) materialOriginalCuerpo = mallaCuerpo.material;

        // Empezamos en Fase 1 por seguridad
        if (panelFase1 != null) panelFase1.SetActive(true);
        if (panelFase2 != null) panelFase2.SetActive(false);
    }

    public void PasarAFase2()
    {
        if (panelFase1 != null) panelFase1.SetActive(false);
        if (panelFase2 != null) panelFase2.SetActive(true);
    }

    // El botón de la Fase 2 llamará a esto
    public void AlternarVisibilidadHueso()
    {
        // Invertimos el interruptor
        estaTransparente = !estaTransparente;

        if (estaTransparente)
        {
            // Ponemos el hueso en "Modo Rayos X"
            if (mallaCabeza != null && materialTransparente != null) mallaCabeza.material = materialTransparente;
            if (mallaCuerpo != null && materialTransparente != null) mallaCuerpo.material = materialTransparente;
        }
        else
        {
            // Devolvemos el hueso a su estado Sólido original
            if (mallaCabeza != null) mallaCabeza.material = materialOriginalCabeza;
            if (mallaCuerpo != null) mallaCuerpo.material = materialOriginalCuerpo;
        }
    }
}