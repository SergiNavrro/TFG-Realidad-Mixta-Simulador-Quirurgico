using UnityEngine;

public class RestaurarEscala : MonoBehaviour
{
    [Tooltip("Arrastra aquí el objeto PADRE del fémur")]
    public Transform huesoPadre;

    private Vector3 escalaSegura = new Vector3(1f, 1f, 1f);

    void Start()
    {
        if (huesoPadre != null)
        {
            escalaSegura = huesoPadre.localScale;

            // Sistema de seguridad antifallos
            if (escalaSegura.x <= 0.01f || escalaSegura.y <= 0.01f || escalaSegura.z <= 0.01f)
            {
                escalaSegura = new Vector3(1f, 1f, 1f);
            }
        }
    }

    public void Resetear()
    {
        if (huesoPadre != null)
        {
            // 1. Guardamos cuánto medía el hueso justo ANTES de encogerlo
            Vector3 escalaVieja = huesoPadre.localScale;

            // 2. Reseteamos el hueso a su tamaño original anatómico
            huesoPadre.localScale = escalaSegura;

            // 3. ¡LA SOLUCIÓN A TU DUDA! Buscamos el tornillo en la escena
            GameObject tornillo = GameObject.Find("Tornillo_Manual");

            if (tornillo != null)
            {
                // Solo tenemos que arreglarlo a mano si NO es hijo del hueso (si está suelto).
                // Si es hijo, Unity ya lo ha encogido automáticamente en el paso 2.
                if (tornillo.transform.parent != huesoPadre)
                {
                    // Regla de 3: Calculamos la proporción (ej: si pasa de 2 a 1, el factor es 0.5)
                    float factorDeProporcion = escalaSegura.x / escalaVieja.x;

                    // Aplicamos ese multiplicador al tornillo suelto
                    tornillo.transform.localScale = tornillo.transform.localScale * factorDeProporcion;
                }
            }

            Debug.Log("<color=cyan>ESCALA RESETEADA:</color> Hueso y tornillos ajustados a escala proporcional 1:1.");
        }
    }
}