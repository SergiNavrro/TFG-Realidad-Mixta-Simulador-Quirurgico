using UnityEngine;
using System.Collections;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;

public class PosicionarInicio : MonoBehaviour
{
    private SolverHandler solverHandler;

    void Start()
    {
        // 1. Buscamos el componente que hace que el objeto te siga
        solverHandler = GetComponent<SolverHandler>();

        // 2. Iniciamos la cuenta atrás en cuanto arranca la app
        if (solverHandler != null)
        {
            StartCoroutine(ApagarSeguimiento());
        }
    }

    // Esta es la "cuenta atrás" invisible
    IEnumerator ApagarSeguimiento()
    {
        // Dejamos que el RadialView trabaje durante 2 segundos para que se ponga frente a ti
        yield return new WaitForSeconds(2.0f);

        // ¡ZAS! Lo apagamos. El objeto se queda anclado en esa posición.
        solverHandler.enabled = false;

        Debug.Log("<color=green>Posicionamiento inicial terminado.</color> El objeto ya es libre.");
    }
}