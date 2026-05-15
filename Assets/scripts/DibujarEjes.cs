using UnityEngine;

public class DibujarEjes : MonoBehaviour
{
    [Header("Tamaño en Metros")]
    public float longitud = 0.06f; // 6 centímetros
    public float grosor = 0.002f;  // 2 milímetros

    // El contenedor que vivirá fuera del objeto para no deformarse
    private GameObject contenedorEjes;

    void Start()
    {
        // 1. Creamos un objeto vacío EN LA RAÍZ de la escena (sin padre)
        // Como no tiene padre, su escala siempre será 1, 1, 1 (Perfecta)
        contenedorEjes = new GameObject("EjesVisibles_" + gameObject.name);

        // 2. Fabricamos las flechas dentro de ese contenedor perfecto
        CrearFlecha("EjeX_Rojo", Color.red, Vector3.right, contenedorEjes.transform);
        CrearFlecha("EjeY_Verde", Color.green, Vector3.up, contenedorEjes.transform);
        CrearFlecha("EjeZ_Azul", Color.blue, Vector3.forward, contenedorEjes.transform);
    }

    void Update()
    {
        // 3. LA MAGIA: En cada frame, el contenedor perfecto se teletransporta 
        // a la posición y rotación exacta de tu objeto, pero SIN heredar su deformación.
        if (contenedorEjes != null)
        {
            contenedorEjes.transform.position = transform.position;
            contenedorEjes.transform.rotation = transform.rotation;
        }
    }

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

    // 4. Limpieza: Si el tornillo se borra, sus ejes fantasma también se borran
    void OnDestroy()
    {
        if (contenedorEjes != null)
        {
            Destroy(contenedorEjes);
        }
    }
}