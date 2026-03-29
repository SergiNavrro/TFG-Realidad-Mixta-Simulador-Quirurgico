using UnityEngine;
using UnityEngine.SceneManagement; // ¡Esta línea es la clave para recargar!

public class ReiniciarSistema : MonoBehaviour
{
    public void BotonReinicioTotal()
    {
        // 1. Averiguamos cómo se llama la escena actual (la tuya)
        string nombreEscena = SceneManager.GetActiveScene().name;

        // 2. La volvemos a cargar desde cero
        SceneManager.LoadScene(nombreEscena);

        Debug.Log("<color=red>SISTEMA REINICIADO:</color> Todo ha vuelto al estado original.");
    }
}