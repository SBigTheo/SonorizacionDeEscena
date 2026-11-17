using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AkGameObj))]
public sealed class DetectorHamburguesa : MonoBehaviour
{
    private const int PanInferior = 0;
    private const int Carne = 1;
    private const int Queso = 2;
    private const int PanSuperior = 3;
    private static readonly string[] Nombres = { "panbajo", "carne", "cheese", "pansuperior" };
    private static readonly float[] Alturas = { -0.9f, 1.5f, 2f, 4.1f };

    [SerializeField] private GameObject hamburguesaCompleta;
    [SerializeField] private Transform puntoDeAparicion;

    private readonly Agarrable[] ingredientes = new Agarrable[4];
    private int etapa;
    private uint musica;
    private uint victoria;
    private GameObject emisorMusical;
    private string mensaje;
    [System.NonSerialized] private GUIStyle estiloMensaje;
    private float mostrarHasta;
    private string Siguiente => etapa switch
    {
        0 => "pan inferior",
        1 => "carne o queso",
        2 => ingredientes[Carne] == null ? "carne" : "queso",
        _ => "pan superior"
    };

    public bool AudioEnCurso => musica != 0 || victoria != 0;
    public bool EstaCompleta => etapa == ingredientes.Length;
    public Vector3 Centro => puntoDeAparicion != null ? puntoDeAparicion.position : transform.position;
    public string Instruccion => etapa < ingredientes.Length
        ? "Q · Agarrar ingrediente | Apuntá a la tabla y pulsá Q para colocarlo\n" +
          "Q mirando fuera de la tabla · Devolver al lugar de origen\nSiguiente: " + Siguiente
        : "Hamburguesa completa · Escuchá la mezcla final";

    private void Awake()
    {
        if (GetComponent<AkGameObj>() == null)
            gameObject.AddComponent<AkGameObj>();
    }

    public bool Colocar(Agarrable ingrediente)
    {
        if (etapa >= ingredientes.Length || hamburguesaCompleta == null)
            return false;

        var tipo = System.Array.FindIndex(Nombres,
            nombre => ingrediente.name.StartsWith(nombre, System.StringComparison.OrdinalIgnoreCase));
        var permitido = etapa switch
        {
            0 => tipo == PanInferior,
            1 or 2 => tipo is Carne or Queso && ingredientes[tipo] == null,
            _ => tipo == PanSuperior
        };
        if (!permitido)
        {
            Mostrar("Falta colocar: " + Siguiente);
            return false;
        }

        var centro = Centro;
        var altura = tipo switch
        {
            Queso when etapa == 1 => 0.65f,
            Carne when etapa == 2 => 1.85f,
            _ => Alturas[tipo]
        };
        ingrediente.Colocar(centro + Vector3.up * altura);
        ingredientes[tipo] = ingrediente;
        etapa++;
        if (AkUnitySoundEngine.IsInitialized())
        {
            if (etapa == 1)
            {
                emisorMusical = new GameObject("Música de la hamburguesa", typeof(AkGameObj));
                emisorMusical.transform.SetParent(transform, true);
                emisorMusical.transform.position = centro;
                AkUnitySoundEngine.SetObjectPosition(emisorMusical, emisorMusical.transform);
            }
            if (emisorMusical != null)
            {
                AkUnitySoundEngine.SetRTPCValue("MezclaRitmoComedor", ingredientes[Carne] != null ? 100f : 0f, emisorMusical, 90);
                AkUnitySoundEngine.SetRTPCValue("MezclaMelodiaComedor", ingredientes[Queso] != null ? 100f : 0f, emisorMusical, 180);
                if (etapa == 1)
                    musica = AkUnitySoundEngine.PostEvent("Play_MusicaHamburguesa", emisorMusical);
            }
        }

        if (etapa == ingredientes.Length)
        {
            foreach (var pieza in ingredientes)
                Destroy(pieza.gameObject);
            var completa = Instantiate(hamburguesaCompleta, centro, Quaternion.identity);
            if (emisorMusical != null)
                emisorMusical.transform.SetParent(completa.transform, true);
            Finalizar();
            Mostrar("Hamburguesa completa");
        }
        else
            Mostrar("Siguiente ingrediente: " + Siguiente);

        return true;
    }

    private void Finalizar()
    {
        if (!AkUnitySoundEngine.IsInitialized())
            return;
        victoria = AkUnitySoundEngine.PostEvent("Play_HamburguesaCompleta",
            AkUnitySoundEngine.GetAkGameObjectID(emisorMusical != null ? emisorMusical : gameObject),
            (uint)AkCallbackType.AK_EndOfEvent,
            (_, tipo, _) =>
            {
                if (tipo == AkCallbackType.AK_EndOfEvent)
                    victoria = 0;
            }, null);
    }

    public float CercaniaHamburguesaCompleta(Vector3 oyente)
    {
        var posicion = emisorMusical != null ? emisorMusical.transform.position : Centro;
        var distancia = Vector3.ProjectOnPlane(oyente - posicion, Vector3.up).magnitude;
        return Mathf.SmoothStep(1f, 0f, (distancia - 26f) / 74f);
    }

    public void AjustarEscucha(Vector3 oyente, ulong idOyente)
    {
        if (emisorMusical == null || !AudioEnCurso)
            return;

        var distancia = Vector3.Distance(oyente, emisorMusical.transform.position);
        var ganancia = EstaCompleta ? CercaniaHamburguesaCompleta(oyente) :
            Mathf.SmoothStep(1f, 0f, (distancia - 45f) / 135f);
        AkUnitySoundEngine.SetGameObjectOutputBusVolume(
            AkUnitySoundEngine.GetAkGameObjectID(emisorMusical), idOyente, ganancia);
    }

    private void Mostrar(string texto)
    {
        mensaje = texto;
        mostrarHasta = Time.unscaledTime + 3f;
    }

    private void OnGUI()
    {
        if (Time.unscaledTime >= mostrarHasta)
            return;

        estiloMensaje ??= CartelesInformativos.CrearTexto();
        var ancho = Mathf.Min(300f, Screen.width - 20f);
        CartelesInformativos.Dibujar(new Rect((Screen.width - ancho) / 2f, Screen.height * 0.7f, ancho, 30f),
            mensaje, estiloMensaje);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (AkUnitySoundEngine.IsInitialized())
        {
            DetenerMusica();
            if (victoria != 0)
                AkUnitySoundEngine.StopPlayingID(victoria);
        }
        victoria = 0;
    }

    private void DetenerMusica(int fundidoMs = 150)
    {
        if (musica == 0)
            return;
        AkUnitySoundEngine.StopPlayingID(musica, fundidoMs, AkCurveInterpolation.AkCurveInterpolation_Sine);
        musica = 0;
    }
}
