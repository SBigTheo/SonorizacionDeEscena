using UnityEngine;

public class Agarrable : MonoBehaviour
{
    [HideInInspector] public bool estaAgarrado = false;

    private Rigidbody rb;
    private Collider col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void Agarrar(Transform puntoMano)
    {
        estaAgarrado = true;

        // Desactivar física
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // El collider se queda activo (para poder soltarlo sobre la madera)
        // pero si molesta, puedes desactivarlo:
        // if (col != null) col.enabled = false;

        // Pegar al punto de la mano
        transform.SetParent(puntoMano);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Soltar()
    {
        estaAgarrado = false;

        // Despegar
        transform.SetParent(null);

        // Reactivar física
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        // if (col != null) col.enabled = true;
    }
}