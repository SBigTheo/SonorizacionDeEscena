using UnityEngine;

public sealed class JugadorInteraccion : MonoBehaviour
{
    [SerializeField] private float distanciaMaxima = 60f;
    [SerializeField] private LayerMask capaIngredientes;
    [SerializeField] private Transform puntoMano;
    [SerializeField] private Camera camara;

    private Agarrable agarrado;
    private DetectorHamburguesa estacion;
    [System.NonSerialized] private GUIStyle cartel;

    private void Start()
    {
        estacion = FindAnyObjectByType<DetectorHamburguesa>();
        if (camara == null || !camara.isActiveAndEnabled)
            camara = Camera.main;
        if (camara != null && puntoMano == null)
        {
            puntoMano = new GameObject("Punto de agarre").transform;
            puntoMano.SetParent(camara.transform, false);
            puntoMano.position = camara.transform.position +
                camara.transform.rotation * new Vector3(7f, -9f, 25f);
        }
    }

    private void OnGUI()
    {
        if (camara == null || estacion == null ||
            (agarrado == null && Vector3.Distance(camara.transform.position, estacion.Centro) > 105f))
            return;

        if (cartel == null)
        {
            cartel = CartelesInformativos.CrearTexto();
            cartel.wordWrap = true;
            cartel.padding = new RectOffset(12, 12, 4, 4);
        }
        cartel.fontSize = Mathf.Max(13, Screen.height / 55);
        cartel.richText = true;
        var ancho = Mathf.Min(760f, Screen.width - 28f);
        var alto = Mathf.Max(106f,
            cartel.CalcHeight(new GUIContent(CartelesInformativos.ResaltarControles(estacion.Instruccion)), ancho) + 14f);
        var y = Mathf.Min(Screen.height * 0.72f, Screen.height - alto - 20f);
        CartelesInformativos.Dibujar(new Rect((Screen.width - ancho) / 2f, y, ancho, alto),
            estacion.Instruccion, cartel);
    }

    private void Update()
    {
        if (camara == null || puntoMano == null || !Input.GetKeyDown(KeyCode.Q))
            return;

        var mira = camara.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        var capas = agarrado == null ? Physics.DefaultRaycastLayers : Physics.DefaultRaycastLayers & ~capaIngredientes.value;
        var tieneImpacto = Physics.Raycast(mira, out var hit, distanciaMaxima, capas, QueryTriggerInteraction.Ignore);

        if (agarrado == null)
        {
            if (!tieneImpacto || (capaIngredientes.value & (1 << hit.collider.gameObject.layer)) == 0)
                return;
            agarrado = hit.collider.GetComponentInParent<Agarrable>();
            if (agarrado != null)
                agarrado.Agarrar(puntoMano);
            return;
        }

        var tabla = tieneImpacto ? hit.collider.GetComponentInParent<DetectorHamburguesa>() : null;
        if (tabla != null)
        {
            if (!tabla.Colocar(agarrado))
                return;
        }
        else
            agarrado.Soltar();

        agarrado = null;
    }
}
