using UnityEngine;

public class JugadorInteraccion : MonoBehaviour
{
    [Header("Configuración")]
    public float distanciaMaxima = 3f;
    public LayerMask capaIngredientes;
    public Transform puntoMano;      // Empty hijo de la cámara
    public Camera camara;            // La cámara en primera persona

    private Agarrable agarrado = null;

    void Start()
    {
        if (camara == null) camara = Camera.main;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (agarrado == null)
                IntentarAgarrar();
            else
                Soltar();
        }
    }

    private void IntentarAgarrar()
    {
        Ray ray = new Ray(camara.transform.position, camara.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, distanciaMaxima, capaIngredientes))
        {
            Agarrable a = hit.collider.GetComponent<Agarrable>();
            if (a != null && !a.estaAgarrado)
            {
                agarrado = a;
                agarrado.Agarrar(puntoMano);
                Debug.Log($"Agarrado: {a.name}");
            }
        }
    }

    private void Soltar()
    {
        agarrado.Soltar();
        Debug.Log($"Soltado: {agarrado.name}");
        agarrado = null;
    }
}