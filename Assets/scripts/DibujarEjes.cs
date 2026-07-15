using UnityEngine;

public class DibujarEjes : MonoBehaviour
{
    [Header("Tamaño en Metros")]
    public float longitud = 0.06f; // 6 cm
    public float grosor = 0.002f;  // 2 mm

    [Header("Estado inicial")]
    [Tooltip("False = empieza oculto. Se activa desde fuera con MostrarEjes()")]
    public bool visibleAlArrancar = false;

    private GameObject contenedorEjes;
    private bool ejesVisibles = false;

    void Start()
    {
        // Crear contenedor en la raíz (sin padre, sin deformación)
        contenedorEjes = new GameObject("EjesVisibles_" + gameObject.name);

        CrearFlecha("EjeX_Rojo", Color.red, Vector3.right, contenedorEjes.transform);
        CrearFlecha("EjeY_Verde", Color.green, Vector3.up, contenedorEjes.transform);
        CrearFlecha("EjeZ_Azul", Color.blue, Vector3.forward, contenedorEjes.transform);

        // Arrancar oculto por defecto
        ejesVisibles = visibleAlArrancar;
        contenedorEjes.SetActive(ejesVisibles);
    }

    void Update()
    {
        // Solo actualizar posición/rotación si está visible
        if (contenedorEjes != null && ejesVisibles)
        {
            contenedorEjes.transform.position = transform.position;
            contenedorEjes.transform.rotation = transform.rotation;
        }
    }

    // ====================================================================
    // METODOS PUBLICOS para controlar desde otros scripts
    // ====================================================================
    public void MostrarEjes()
    {
        ejesVisibles = true;
        if (contenedorEjes != null) contenedorEjes.SetActive(true);
    }

    public void OcultarEjes()
    {
        ejesVisibles = false;
        if (contenedorEjes != null) contenedorEjes.SetActive(false);
    }

    public void ToggleEjes()
    {
        if (ejesVisibles) OcultarEjes();
        else MostrarEjes();
    }

    public bool EstanVisibles()
    {
        return ejesVisibles;
    }

    // ====================================================================
    // CREAR las flechas (sin cambios)
    // ====================================================================
    void CrearFlecha(string nombre, Color color, Vector3 direccion, Transform padreUniforme)
    {
        GameObject pivote = new GameObject(nombre);
        pivote.transform.SetParent(padreUniforme);
        pivote.transform.localPosition = Vector3.zero;

        if (direccion == Vector3.right) pivote.transform.localRotation = Quaternion.Euler(0, 0, -90);
        else if (direccion == Vector3.up) pivote.transform.localRotation = Quaternion.Euler(0, 0, 0);
        else if (direccion == Vector3.forward) pivote.transform.localRotation = Quaternion.Euler(90, 0, 0);

        GameObject cilindro = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(cilindro.GetComponent<Collider>());
        cilindro.transform.SetParent(pivote.transform, false);
        cilindro.transform.localScale = new Vector3(grosor, longitud / 2f, grosor);
        cilindro.transform.localPosition = new Vector3(0, longitud / 2f, 0);

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 0.5f);
        cilindro.GetComponent<Renderer>().material = mat;
    }

    void OnDestroy()
    {
        if (contenedorEjes != null)
        {
            Destroy(contenedorEjes);
        }
    }
}