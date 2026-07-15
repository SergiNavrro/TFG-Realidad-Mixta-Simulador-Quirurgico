using UnityEngine;
using Vuforia;

public class SuavizadorHerramienta : MonoBehaviour
{
    [Range(0.05f, 1f)]
    [Tooltip("Cuánto peso al frame nuevo. Bajo=más suave/lento. Alto=más responsivo/jittery. Recomendado 0.3-0.5")]
    public float suavizado = 0.4f;

    private Vector3 posSuavizada;
    private Quaternion rotSuavizada;
    private bool primeraVez = true;
    private ObserverBehaviour observer;

    void Start()
    {
        observer = GetComponent<ObserverBehaviour>();
    }

    void Update()
    {
        if (observer == null || observer.TargetStatus.Status != Status.TRACKED) return;

        if (primeraVez)
        {
            posSuavizada = transform.position;
            rotSuavizada = transform.rotation;
            primeraVez = false;
            return;
        }

        // EMA: peso suavizado al nuevo, (1-suavizado) al anterior
        posSuavizada = Vector3.Lerp(posSuavizada, transform.position, suavizado);
        rotSuavizada = Quaternion.Slerp(rotSuavizada, transform.rotation, suavizado);

        transform.position = posSuavizada;
        transform.rotation = rotSuavizada;
    }
}