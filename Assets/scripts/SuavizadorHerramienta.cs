using UnityEngine;
using Vuforia;

public class SuavizadorHerramienta : MonoBehaviour
{
    [Range(1, 20)]
    public int bufferFrames = 8;

    private Vector3[] bufferPosicion;
    private Quaternion[] bufferRotacion;
    private int indice = 0;
    private bool bufferLleno = false;
    private ObserverBehaviour observer;

    void Start()
    {
        bufferPosicion = new Vector3[bufferFrames];
        bufferRotacion = new Quaternion[bufferFrames];
        observer = GetComponent<ObserverBehaviour>();
    }

    void Update()
    {
        if (observer == null || observer.TargetStatus.Status != Status.TRACKED) return;

        bufferPosicion[indice] = transform.position;
        bufferRotacion[indice] = transform.rotation;
        indice = (indice + 1) % bufferFrames;
        if (indice == 0) bufferLleno = true;

        int cantidad = bufferLleno ? bufferFrames : indice;
        if (cantidad == 0) return;

        Vector3 posMedia = Vector3.zero;
        for (int i = 0; i < cantidad; i++)
            posMedia += bufferPosicion[i];
        posMedia /= cantidad;

        Quaternion rotMedia = bufferRotacion[0];
        for (int i = 1; i < cantidad; i++)
            rotMedia = Quaternion.Slerp(rotMedia, bufferRotacion[i], 1f / (i + 1));

        transform.position = posMedia;
        transform.rotation = rotMedia;
    }
}