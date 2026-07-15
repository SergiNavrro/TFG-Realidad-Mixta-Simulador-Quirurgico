using UnityEngine;

public class FijadorHueso : MonoBehaviour
{
    [Header("Objetos a enlazar")]
    public GameObject modeloHueso3D;
    public GameObject vuforiaModelTarget;

    private bool estaFijado = false;
    private Transform padreOriginal;
    private Vector3 posOriginal;
    private Quaternion rotOriginal;

    void Start()
    {
        if (modeloHueso3D != null)
        {
            padreOriginal = modeloHueso3D.transform.parent;
            posOriginal = modeloHueso3D.transform.localPosition;
            rotOriginal = modeloHueso3D.transform.localRotation;
        }
    }

    public void AlternarFijacion()
    {
        estaFijado = !estaFijado;

        if (estaFijado)
            Fijar();
        else
            Rastrear();
    }

    private void Fijar()
    {
        // 1. Guardar posición MUNDIAL antes de tocar nada
        Vector3 posMundial = modeloHueso3D.transform.position;
        Quaternion rotMundial = modeloHueso3D.transform.rotation;

        // 2. Desconectar el hueso de toda la jerarquía de Vuforia
        modeloHueso3D.transform.SetParent(null, worldPositionStays: true);

        // 3. Sobrescribir la posición mundial explícitamente
        //    (por si SetParent no la conservó al 100%)
        modeloHueso3D.transform.position = posMundial;
        modeloHueso3D.transform.rotation = rotMundial;

        // 4. Apagar TODOS los scripts del Model Target de golpe
        //    para que Vuforia no pueda tocar nada más
        foreach (MonoBehaviour script in vuforiaModelTarget.GetComponentsInChildren<MonoBehaviour>(true))
            script.enabled = false;

        // 5. Ahora sí, desactivar el GameObject completo (ya no hay scripts que reaccionen)
        vuforiaModelTarget.SetActive(false);

        // 6. Forzar que el hueso y todas sus mallas estén encendidas
        modeloHueso3D.SetActive(true);
        foreach (Renderer r in modeloHueso3D.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;

        Debug.Log("FIJADO en " + posMundial);
    }

    private void Rastrear()
    {
        // Despertar Vuforia
        vuforiaModelTarget.SetActive(true);

        foreach (MonoBehaviour script in vuforiaModelTarget.GetComponentsInChildren<MonoBehaviour>(true))
            script.enabled = true;

        // Devolver el hueso a su sitio en la jerarquía
        modeloHueso3D.transform.SetParent(padreOriginal);
        modeloHueso3D.transform.localPosition = posOriginal;
        modeloHueso3D.transform.localRotation = rotOriginal;

        Debug.Log("Rastreando de nuevo.");
    }
}