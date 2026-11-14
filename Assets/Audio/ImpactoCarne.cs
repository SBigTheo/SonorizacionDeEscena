using UnityEngine;

public sealed class ImpactoCarne : MonoBehaviour
{
    private System.Action<GameObject> alAterrizar;
    private Rigidbody cuerpo;
    private bool impacto;
    private float alturaMaximaImpacto;

    internal void Preparar(float alturaMaxima, System.Action<GameObject> sonido)
    {
        cuerpo = GetComponent<Rigidbody>();
        alAterrizar = sonido;
        alturaMaximaImpacto = alturaMaxima;
    }

    private void OnCollisionEnter(Collision collision) => RevisarApoyo(collision);

    private void OnCollisionStay(Collision collision) => RevisarApoyo(collision);

    private void RevisarApoyo(Collision collision)
    {
        if (impacto || cuerpo == null || cuerpo.isKinematic || (collision.rigidbody != null && !collision.rigidbody.isKinematic))
            return;

        for (var index = 0; index < collision.contactCount; index++)
        {
            var contact = collision.GetContact(index);
            if (contact.normal.y < 0.45f || contact.point.y > alturaMaximaImpacto)
                continue;

            impacto = true;
            cuerpo.linearVelocity = Vector3.zero;
            cuerpo.angularVelocity = Vector3.zero;
            cuerpo.isKinematic = true;
            alAterrizar?.Invoke(gameObject);
            return;
        }
    }
}
