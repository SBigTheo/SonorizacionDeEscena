using UnityEngine;

public class ZonaInteraccion : MonoBehaviour
{
    [Header("Resultado")]
    public GameObject hamburguesaCompleta;
    public Transform puntoAparicion;

    [Header("Ingredientes a eliminar")]
    public string[] nombresIngredientes = { "panbajo", "pansuperior", "carne", "cheese" };

    private bool yaActivada = false;

    void Update()
    {
        if (yaActivada) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Collider[] dentro = Physics.OverlapBox(transform.position, transform.localScale / 2f);
            foreach (Collider col in dentro)
            {
                if (col.CompareTag("Player"))
                {
                    ActivarHamburguesa();
                    return;
                }
            }
        }
    }

    private void ActivarHamburguesa()
    {
        yaActivada = true;
        Debug.Log("¡Zona activada! Eliminando ingredientes y creando hamburguesa...");

        GameObject[] todos = FindObjectsByType<GameObject>();
        foreach (GameObject obj in todos)
        {
            foreach (string nombre in nombresIngredientes)
            {
                if (obj.name.ToLower().StartsWith(nombre.ToLower()))
                {
                    Destroy(obj);
                    break;
                }
            }
        }

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

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawCube(transform.position, transform.localScale);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}
