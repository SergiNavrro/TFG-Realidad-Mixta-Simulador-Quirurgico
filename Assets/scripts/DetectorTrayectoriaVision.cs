using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Porta el algoritmo Python de vision artificial directamente a Unity.
/// Detecta la cabeza femoral (RANSAC esfera), calcula el punto de entrada
/// en la corteza lateral y traza la trayectoria sugerida del tornillo inferior.
///
/// SETUP:
/// 1. Arrastra este script al GameObject VisualizadorVision (hijo de femur_roto)
/// 2. Asigna el MeshFilter del hueso en el Inspector
/// 3. Llama a CalcularTrayectoria() desde un boton de la HMI
/// </summary>
public class DetectorTrayectoriaVision : MonoBehaviour
{
    [Header("Referencia al hueso")]
    [Tooltip("MeshFilter del modelo 3D del femur (femur cortical smth0.002 o similar)")]
    public MeshFilter meshHueso;

    [Tooltip("Contenedor padre del hueso")]
    public Transform huesoContenedor;

    [Header("RANSAC - Cabeza femoral")]
    [Tooltip("Numero de iteraciones RANSAC para detectar la esfera")]
    public int iteracionesRANSAC = 2000;

    [Tooltip("Umbral de distancia para considerar un punto inlier (metros)")]
    public float umbralRANSAC = 0.0015f; // 1.5mm

    [Tooltip("Radio minimo anatomico de la cabeza femoral (metros)")]
    public float radioCabezaMin = 0.018f; // 18mm

    [Tooltip("Radio maximo anatomico de la cabeza femoral (metros)")]
    public float radioCabezaMax = 0.032f; // 32mm

    [Tooltip("Intentos de RANSAC para encontrar una esfera valida")]
    public int intentosRANSAC = 10;

    [Header("Aislamiento zona del cuello")]
    [Tooltip("Distancia desde el centro de la cabeza donde empieza el corte (metros)")]
    public float distanciaInicio = 0.020f; // 20mm

    [Tooltip("Distancia desde el centro de la cabeza donde termina el corte (metros)")]
    public float distanciaFin = 0.070f;   // 70mm

    [Header("Calculo punto de entrada")]
    [Tooltip("Porcentaje top de puntos mas alejados que se usan como corteza lateral (0.05 = 5%)")]
    public float porcentajeCorteza = 0.05f;

    [Tooltip("Desplazamiento del destino hacia abajo desde el centro de la cabeza (metros)")]
    public float desplazamientoInferior = 0.007f; // 7mm

    [Header("Visualizacion")]
    public float tamañoBolas = 0.005f;
    public float grosorLinea = 0.002f;
    public Color colorLinea = Color.yellow;

    // Resultados calculados (accesibles desde otros scripts)
    [HideInInspector] public Vector3 centroCabeza;
    [HideInInspector] public float radioCabeza;
    [HideInInspector] public Vector3 puntoEntrada;
    [HideInInspector] public Vector3 puntoDestino;
    [HideInInspector] public bool trayectoriaCalculada = false;

    // Objetos visuales
    private GameObject objetoEntrada;
    private GameObject objetoDestino;
    private GameObject objetoLinea;
    private LineRenderer lineaRenderer;

    // ====================================================================
    // BOTÓN PRINCIPAL: Calcular y mostrar la trayectoria
    // ====================================================================
    public void CalcularTrayectoria()
    {
        if (meshHueso == null)
        {
            Debug.LogError("[VisionAI] Asigna el MeshFilter del hueso en el Inspector.");
            return;
        }

        // 1. Obtener vertices en espacio MUNDO
        Vector3[] vertices = ObtenerVerticesMundo();
        Debug.Log($"[VisionAI] Vertices totales: {vertices.Length}");

        // 2. Detectar cabeza femoral con RANSAC
        bool cabezaDetectada = DetectarCabeza(vertices, out centroCabeza, out radioCabeza);
        if (!cabezaDetectada)
        {
            Debug.LogError("[VisionAI] No se pudo detectar la cabeza femoral.");
            return;
        }
        Debug.Log($"[VisionAI] Cabeza: centro={centroCabeza}, radio={radioCabeza * 1000f:F1}mm");

        // 3. Aislar la zona del cuello/trocanter
        Vector3[] zonaAislada = AislarZona(vertices, centroCabeza);
        Debug.Log($"[VisionAI] Puntos en zona aislada: {zonaAislada.Length}");

        if (zonaAislada.Length < 50)
        {
            Debug.LogError("[VisionAI] Muy pocos puntos en la zona. Ajusta distanciaInicio/distanciaFin.");
            return;
        }

        // 4. Calcular punto de entrada
        puntoEntrada = CalcularPuntoEntrada(zonaAislada, centroCabeza);
        Debug.Log($"[VisionAI] Punto entrada: {puntoEntrada}");

        // 5. Calcular punto destino (cabeza inferior)
        // "Abajo" = direccion opuesta a la trayectoria, componente vertical
        Vector3 dirTrayectoria = (centroCabeza - puntoEntrada).normalized;
        Vector3 arriba = Vector3.up;
        Vector3 perpendicular = (arriba - Vector3.Dot(arriba, dirTrayectoria) * dirTrayectoria).normalized;
        puntoDestino = centroCabeza - perpendicular * desplazamientoInferior;
        Debug.Log($"[VisionAI] Punto destino: {puntoDestino}");

        trayectoriaCalculada = true;

        // 6. Visualizar
        VisualizarTrayectoria();

        float longitud = Vector3.Distance(puntoEntrada, puntoDestino);
        Debug.Log($"[VisionAI] Longitud trayectoria: {longitud * 1000f:F1}mm");
    }

    // ====================================================================
    // OBTENER vértices del mesh en espacio mundo
    // ====================================================================
    private Vector3[] ObtenerVerticesMundo()
    {
        Mesh mesh = meshHueso.sharedMesh != null ? meshHueso.sharedMesh : meshHueso.mesh;
        Vector3[] verticesLocales = mesh.vertices;
        Vector3[] verticesMundo = new Vector3[verticesLocales.Length];

        for (int i = 0; i < verticesLocales.Length; i++)
        {
            verticesMundo[i] = meshHueso.transform.TransformPoint(verticesLocales[i]);
        }

        return verticesMundo;
    }

    // ====================================================================
    // RANSAC - Detectar la esfera de la cabeza femoral
    // ====================================================================
    private bool DetectarCabeza(Vector3[] vertices, out Vector3 mejorCentro, out float mejorRadio)
    {
        mejorCentro = Vector3.zero;
        mejorRadio = 0f;
        int mejorInliers = 0;

        for (int intento = 0; intento < intentosRANSAC; intento++)
        {
            // 1. Seleccionar 4 puntos al azar
            int[] indices = Seleccionar4Aleatorios(vertices.Length);
            Vector3 p1 = vertices[indices[0]];
            Vector3 p2 = vertices[indices[1]];
            Vector3 p3 = vertices[indices[2]];
            Vector3 p4 = vertices[indices[3]];

            // 2. Calcular la esfera que pasa por los 4 puntos
            Vector3 centro;
            float radio;
            if (!FitEsfera(p1, p2, p3, p4, out centro, out radio))
                continue;

            // 3. Validacion anatomica
            if (radio < radioCabezaMin || radio > radioCabezaMax)
                continue;

            // 4. Contar inliers
            int inliers = ContarInliersEsfera(vertices, centro, radio, umbralRANSAC);

            Debug.Log($"  RANSAC intento {intento + 1}: radio={radio * 1000f:F1}mm, inliers={inliers}");

            if (inliers > mejorInliers)
            {
                mejorInliers = inliers;
                mejorCentro = centro;
                mejorRadio = radio;
            }
        }

        return mejorInliers > 0;
    }

    // ====================================================================
    // FIT ESFERA con 4 puntos (sistema de ecuaciones)
    // ====================================================================
    private bool FitEsfera(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, out Vector3 centro, out float radio)
    {
        centro = Vector3.zero;
        radio = 0f;

        // Sistema lineal: cada punto da una ecuación
        // (x-cx)² + (y-cy)² + (z-cz)² = r²
        // Expandiendo y restando ecuaciones consecutivas para eliminar r²:
        // 2(x2-x1)cx + 2(y2-y1)cy + 2(z2-z1)cz = x2²-x1² + y2²-y1² + z2²-z1²

        float[,] A = new float[3, 3];
        float[] b = new float[3];

        Vector3[] pts = { p1, p2, p3, p4 };

        for (int i = 0; i < 3; i++)
        {
            A[i, 0] = 2f * (pts[i + 1].x - pts[i].x);
            A[i, 1] = 2f * (pts[i + 1].y - pts[i].y);
            A[i, 2] = 2f * (pts[i + 1].z - pts[i].z);

            b[i] = (pts[i + 1].x * pts[i + 1].x - pts[i].x * pts[i].x)
                 + (pts[i + 1].y * pts[i + 1].y - pts[i].y * pts[i].y)
                 + (pts[i + 1].z * pts[i + 1].z - pts[i].z * pts[i].z);
        }

        // Resolver el sistema 3x3 con eliminación gaussiana
        float[] solucion;
        if (!ResolverSistema3x3(A, b, out solucion))
            return false;

        centro = new Vector3(solucion[0], solucion[1], solucion[2]);
        radio = Vector3.Distance(centro, p1);

        // Verificar que el radio no sea ridículo
        if (radio < 0.001f || radio > 1.0f || float.IsNaN(radio))
            return false;

        return true;
    }

    // ====================================================================
    // RESOLVER sistema 3x3 por eliminación gaussiana
    // ====================================================================
    private bool ResolverSistema3x3(float[,] A, float[] b, out float[] x)
    {
        x = new float[3];
        float[,] M = new float[3, 4];

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++) M[i, j] = A[i, j];
            M[i, 3] = b[i];
        }

        for (int col = 0; col < 3; col++)
        {
            // Pivote
            int pivote = -1;
            float maxVal = 0f;
            for (int row = col; row < 3; row++)
            {
                if (Mathf.Abs(M[row, col]) > maxVal)
                {
                    maxVal = Mathf.Abs(M[row, col]);
                    pivote = row;
                }
            }

            if (pivote < 0 || maxVal < 1e-10f) return false;

            // Intercambiar filas
            for (int j = 0; j <= 3; j++)
            {
                float tmp = M[col, j];
                M[col, j] = M[pivote, j];
                M[pivote, j] = tmp;
            }

            // Eliminar
            for (int row = 0; row < 3; row++)
            {
                if (row == col) continue;
                float factor = M[row, col] / M[col, col];
                for (int j = col; j <= 3; j++)
                    M[row, j] -= factor * M[col, j];
            }
        }

        for (int i = 0; i < 3; i++)
        {
            if (Mathf.Abs(M[i, i]) < 1e-10f) return false;
            x[i] = M[i, 3] / M[i, i];
        }

        return true;
    }

    // ====================================================================
    // CONTAR inliers de la esfera
    // ====================================================================
    private int ContarInliersEsfera(Vector3[] vertices, Vector3 centro, float radio, float umbral)
    {
        int count = 0;
        foreach (var v in vertices)
        {
            float dist = Mathf.Abs(Vector3.Distance(v, centro) - radio);
            if (dist < umbral) count++;
        }
        return count;
    }

    // ====================================================================
    // AISLAR la zona del cuello/trocánter
    // ====================================================================
    private Vector3[] AislarZona(Vector3[] vertices, Vector3 centroCabeza)
    {
        // Eje vertical = Y en Unity
        float limSuperior = centroCabeza.y - distanciaInicio;
        float limInferior = centroCabeza.y - distanciaFin;

        List<Vector3> zona = new List<Vector3>();
        foreach (var v in vertices)
        {
            if (v.y < limSuperior && v.y > limInferior)
                zona.Add(v);
        }

        return zona.ToArray();
    }

    // ====================================================================
    // CALCULAR punto de entrada (proyección sobre dirección lateral)
    // ====================================================================
    private Vector3 CalcularPuntoEntrada(Vector3[] zonaAislada, Vector3 centroCabeza)
    {
        // 1. Dirección lateral = desde cabeza hacia centroide de la zona
        Vector3 centroide = Vector3.zero;
        foreach (var v in zonaAislada) centroide += v;
        centroide /= zonaAislada.Length;

        Vector3 direccionLateral = (centroide - centroCabeza).normalized;
        Debug.Log($"[VisionAI] Direccion lateral: {direccionLateral}");

        // 2. Proyectar cada punto sobre la dirección lateral
        float[] proyecciones = new float[zonaAislada.Length];
        for (int i = 0; i < zonaAislada.Length; i++)
        {
            proyecciones[i] = Vector3.Dot(zonaAislada[i] - centroCabeza, direccionLateral);
        }

        // 3. Ordenar proyecciones y sacar el umbral del top 5%
        float[] proyeccionesOrdenadas = (float[])proyecciones.Clone();
        System.Array.Sort(proyeccionesOrdenadas);
        int indiceUmbral = Mathf.FloorToInt(proyeccionesOrdenadas.Length * (1f - porcentajeCorteza));
        float umbralProyeccion = proyeccionesOrdenadas[Mathf.Clamp(indiceUmbral, 0, proyeccionesOrdenadas.Length - 1)];

        // 4. Centroide del top 5% más alejado
        Vector3 sumaCorteza = Vector3.zero;
        int contadorCorteza = 0;
        for (int i = 0; i < zonaAislada.Length; i++)
        {
            if (proyecciones[i] >= umbralProyeccion)
            {
                sumaCorteza += zonaAislada[i];
                contadorCorteza++;
            }
        }

        Debug.Log($"[VisionAI] Puntos corteza lateral: {contadorCorteza}");
        return sumaCorteza / contadorCorteza;
    }

    // ====================================================================
    // VISUALIZAR la trayectoria
    // ====================================================================
    private void VisualizarTrayectoria()
    {
        // Bola entrada (roja)
        if (objetoEntrada == null)
        {
            objetoEntrada = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            objetoEntrada.name = "Vision_PuntoEntrada";
            Destroy(objetoEntrada.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Standard"));
            mat.color = Color.red;
            objetoEntrada.GetComponent<Renderer>().material = mat;
        }
        objetoEntrada.transform.position = puntoEntrada;
        objetoEntrada.transform.localScale = Vector3.one * tamañoBolas;
        objetoEntrada.SetActive(true);

        // Bola destino (azul)
        if (objetoDestino == null)
        {
            objetoDestino = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            objetoDestino.name = "Vision_PuntoDestino";
            Destroy(objetoDestino.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Standard"));
            mat.color = Color.blue;
            objetoDestino.GetComponent<Renderer>().material = mat;
        }
        objetoDestino.transform.position = puntoDestino;
        objetoDestino.transform.localScale = Vector3.one * tamañoBolas;
        objetoDestino.SetActive(true);

        // Línea trayectoria
        if (objetoLinea == null)
        {
            objetoLinea = new GameObject("Vision_Linea");
            lineaRenderer = objetoLinea.AddComponent<LineRenderer>();
        }
        lineaRenderer = objetoLinea.GetComponent<LineRenderer>();
        lineaRenderer.useWorldSpace = true;
        lineaRenderer.positionCount = 2;
        lineaRenderer.SetPosition(0, puntoEntrada);
        lineaRenderer.SetPosition(1, puntoDestino);
        lineaRenderer.startWidth = grosorLinea;
        lineaRenderer.endWidth = grosorLinea;
        lineaRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineaRenderer.startColor = colorLinea;
        lineaRenderer.endColor = colorLinea;
        objetoLinea.SetActive(true);
    }

    // ====================================================================
    // OCULTAR / MOSTRAR
    // ====================================================================
    public void OcultarTrayectoria()
    {
        if (objetoEntrada != null) objetoEntrada.SetActive(false);
        if (objetoDestino != null) objetoDestino.SetActive(false);
        if (objetoLinea != null) objetoLinea.SetActive(false);
    }

    public void ToggleTrayectoria()
    {
        if (objetoLinea != null && objetoLinea.activeSelf)
            OcultarTrayectoria();
        else if (trayectoriaCalculada)
            VisualizarTrayectoria();
        else
            CalcularTrayectoria();
    }

    // ====================================================================
    // HELPERS
    // ====================================================================
    private int[] Seleccionar4Aleatorios(int total)
    {
        int[] indices = new int[4];
        HashSet<int> usados = new HashSet<int>();
        int i = 0;
        while (i < 4)
        {
            int r = Random.Range(0, total);
            if (!usados.Contains(r))
            {
                indices[i++] = r;
                usados.Add(r);
            }
        }
        return indices;
    }

    // ====================================================================
    // GIZMOS en el Editor para previsualizar
    // ====================================================================
    void OnDrawGizmosSelected() 
    {
        if (!trayectoriaCalculada) return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(puntoEntrada, 0.005f);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(puntoDestino, 0.005f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(puntoEntrada, puntoDestino);

        Gizmos.color = new Color(0, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(centroCabeza, radioCabeza);
    }
}