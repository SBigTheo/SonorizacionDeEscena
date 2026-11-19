using UnityEngine;

public class InteraccionTabla : MonoBehaviour
{
    [Header("Configuración")]
    public float distanciaMaxima = 60f;      // proporcional a tu escala
    public Camera camara;                    // arrastra la Main Camera
    public LayerMask capaTabla;              // layer de la madera

    [Header("Resultado")]
    public GameObject hamburguesaCompleta;   // prefab de la hamburguesa armada
    public Transform puntoAparicion;         // opcional, si null usa la madera

    [Header("Ingredientes a eliminar")]
    public string[] nombresIngredientes = { "panbajo", "pansuperior", "carne", "cheese" };

    private bool yaActivada = false;

    void Start()
    {
        if (camara == null) camara = Camera.main;
    }

    void Update()
    {
        if (yaActivada) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            IntentarActivar();
        }
    }

    private void IntentarActivar()
    {
        Ray ray = new Ray(camara.transform.position, camara.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, distanciaMaxima, capaTabla))
        {
            // Verifica que el rayo haya golpeado ESTA tabla (o cualquier objeto con este script)
            if (hit.collider.gameObject == gameObject || hit.collider.GetComponent<InteraccionTabla>() != null)
            {
                ActivarHamburguesa();
            }
        }
    }

    private void ActivarHamburguesa()
    {
        yaActivada = true;
        Debug.Log("¡Tabla activada! Eliminando ingredientes y creando hamburguesa...");

        // 1. Eliminar los ingredientes de la escena
        foreach (string nombre in nombresIngredientes)
        {
            // Busca todos los objetos cuyo nombre empiece por el ingrediente (maneja "carne(Clone)")
            GameObject[] todos = FindObjectsByType<GameObject>();
            foreach (GameObject obj in todos)
            {
                if (obj.name.ToLower().StartsWith(nombre.ToLower()))
                {
                    Destroy(obj);
                }
            }
        }

        // 2. Instanciar la hamburguesa completa
        if (hamburguesaCompleta != null)
        {
            Vector3 pos = puntoAparicion != null ? puntoAparicion.position : transform.position;
            Instantiate(hamburguesaCompleta, pos, Quaternion.identity);
            Debug.Log("Hamburguesa completa instanciada.");
        }
        else
        {
            Debug.LogWarning("No asignaste el prefab de la hamburguesa completa.");
        }
    }
}
