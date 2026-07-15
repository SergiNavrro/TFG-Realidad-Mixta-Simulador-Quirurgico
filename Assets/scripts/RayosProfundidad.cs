using UnityEngine;
using TMPro;
using System.Collections;

public class RayosProfundidad : MonoBehaviour
{
    [Header("Configuración del Láser Físico")]
    public LayerMask capaHueso;
    private LineRenderer renderizadorLaser;

    [Header("Interfaz y Lógica")]
    public TMP_Text textoResultado;
    public float margenSeguridadMM = 5.0f;
    public float alcanceLaser = 1.0f;
    public float tiempoVisible = 3.0f;

    void Start()
    {
        renderizadorLaser = gameObject.AddComponent<LineRenderer>();
        renderizadorLaser.startWidth = 0.002f;
        renderizadorLaser.endWidth = 0.002f;
        renderizadorLaser.material = new Material(Shader.Find("Sprites/Default"));
        renderizadorLaser.startColor = Color.red;
        renderizadorLaser.endColor = Color.red;
        renderizadorLaser.enabled = false;
    }

    public void DispararLaser()
    {
        GameObject tornillo = GameObject.Find("Tornillo_Manual");

        if (tornillo == null)
        {
            Debug.LogError("No hay ningún tornillo. Dale primero al botón de Generar.");
            return;
        }

        Vector3 origen = tornillo.transform.position;
        Vector3 direccion = -tornillo.transform.up;
        Vector3 puntoLejano = origen + (direccion * alcanceLaser);

        Debug.DrawRay(origen, direccion * alcanceLaser, Color.red, 3f);
        Debug.DrawRay(puntoLejano, -direccion * alcanceLaser, Color.blue, 3f);

        RaycastHit hitEntrada;
        RaycastHit hitSalida;

        bool hayEntrada = Physics.Raycast(origen, direccion, out hitEntrada, alcanceLaser, capaHueso);
        bool haySalida = Physics.Raycast(puntoLejano, -direccion, out hitSalida, alcanceLaser, capaHueso);

        // --- ZONA DE DEBUGS EXTREMOS ---
        Debug.Log("--- INICIANDO ESCÁNER LÁSER ---");

        if (hayEntrada)
        {
            Debug.Log($"<color=yellow>[LÁSER 1 - ENTRADA]</color> ¡CHOCÓ! Ha tocado el objeto llamado: <b>{hitEntrada.collider.gameObject.name}</b>");
        }
        else
        {
            Debug.LogError("<color=red>[LÁSER 1 - ENTRADA]</color> FALLO. El láser rojo no ha tocado nada de la capa Hueso.");
        }

        if (haySalida)
        {
            Debug.Log($"<color=cyan>[LÁSER 2 - SALIDA]</color> ¡CHOCÓ! Ha tocado el objeto llamado: <b>{hitSalida.collider.gameObject.name}</b>");
        }
        else
        {
            Debug.LogError("<color=red>[LÁSER 2 - SALIDA]</color> FALLO. El láser azul de vuelta no ha tocado nada.");
        }
        Debug.Log("-------------------------------");
        // -------------------------------

        renderizadorLaser.enabled = true;
        renderizadorLaser.SetPosition(0, origen);

        if (hayEntrada)
            renderizadorLaser.SetPosition(1, hitEntrada.point);
        else
            renderizadorLaser.SetPosition(1, puntoLejano);

        StopAllCoroutines();
        StartCoroutine(ApagarLaserVisual());

        if (hayEntrada && haySalida)
        {
            float profundidadMM = Vector3.Distance(hitEntrada.point, hitSalida.point) * 1000f;
            float tornilloRecomendado = Mathf.Floor((profundidadMM - margenSeguridadMM) / 5f) * 5f;

            if (textoResultado != null)
            {
                textoResultado.text = $"Profundidad: {profundidadMM:F1} mm\nTornillo: {tornilloRecomendado} mm";
            }
        }
        else
        {
            if (textoResultado != null)
            {
                textoResultado.text = "Error de lectura. Revisa la consola de Unity.";
            }
        }
    }

    IEnumerator ApagarLaserVisual()
    {
        yield return new WaitForSeconds(tiempoVisible);
        renderizadorLaser.enabled = false;
    }
}