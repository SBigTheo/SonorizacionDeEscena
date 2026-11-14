using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class ZonaMusical : MonoBehaviour
{
    [SerializeField] private string seccion;
    [SerializeField] private int prioridad;

    public string Evento => "Musica" + seccion;
    public int Prioridad => prioridad;

    private BoxCollider caja;

    internal float DistanciaAlBorde(Vector3 posicion)
    {
        if (!isActiveAndEnabled)
            return float.PositiveInfinity;
        if (caja == null)
            caja = GetComponent<BoxCollider>();
        if (!caja.enabled)
            return float.PositiveInfinity;

        var local = transform.InverseTransformPoint(posicion) - caja.center;
        var mitad = caja.size * 0.5f;
        var escala = transform.lossyScale;
        var borde = new Vector3(
            (Mathf.Abs(local.x) - mitad.x) * Mathf.Abs(escala.x),
            (Mathf.Abs(local.y) - mitad.y) * Mathf.Abs(escala.y),
            (Mathf.Abs(local.z) - mitad.z) * Mathf.Abs(escala.z));
        var fuera = Vector3.Max(borde, Vector3.zero);
        return fuera.magnitude + Mathf.Min(Mathf.Max(borde.x, Mathf.Max(borde.y, borde.z)), 0f);
    }

    private void OnDrawGizmosSelected()
    {
        var collider = GetComponent<BoxCollider>();
        if (collider == null)
            return;

        var matrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.25f, 0.85f, 1f, 0.12f);
        Gizmos.DrawCube(collider.center, collider.size);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(collider.center, collider.size);
        Gizmos.matrix = matrix;
    }
}
