using UnityEngine;

public class Agarrable : MonoBehaviour
{
    [HideInInspector] public bool estaAgarrado = false;

    private Rigidbody rb;
    private Transform origenPadre;
    private Vector3 origenPosicion;
    private Quaternion origenRotacion;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Agarrar(Transform puntoMano)
    {
        origenPadre = transform.parent;
        origenPosicion = transform.position;
        origenRotacion = transform.rotation;
        estaAgarrado = true;

        // Desactivar física
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Pegar al punto de la mano
        transform.SetParent(puntoMano);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Soltar()
    {
        estaAgarrado = false;

        // Despegar
        transform.SetParent(origenPadre, true);
        transform.SetPositionAndRotation(origenPosicion, origenRotacion);

        // Reactivar física
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void Colocar(Vector3 posicion)
    {
        estaAgarrado = false;
        transform.SetParent(null, true);
        transform.SetPositionAndRotation(posicion, Quaternion.identity);
        foreach (var collider in GetComponentsInChildren<Collider>())
            collider.enabled = false;
    }
}
