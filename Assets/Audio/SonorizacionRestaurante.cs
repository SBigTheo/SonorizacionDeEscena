using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AkGameObj))]
public sealed class SonorizacionRestaurante : MonoBehaviour
{
    private const string Bank = "Sonorizacion";
    private const int DialogueCount = 4;
    private const string BathroomDialogueEvent = "DialogoBano";
    private const string ToiletFlushEvent = "CadenaBano";
    private const float HandWashRadius = 18f;
    private const float WaterStopDelay = 1.5f;
    private const string DriveThruAmbienceEvent = "AmbienteDriveThru";
    private const string KitchenAmbienceEvent = "AmbienteCocina";
    private const string DiningAmbienceEvent = "AmbienteRestaurante";
    private const string FinalDialogueEvent = "DialogoLadron";
    private const string PassingCarEvent = "AutoPasando";
    private const string DriveThruDialogueEvent = "DialogoRecepcion";
    private const string FryerDialogueEvent = "DialogoPapas";
    private const string FryerAmbienceEvent = "Fritura";
    private const string TelevisionEvent = "DialogoTelevision";
    private const string LayeredFryingEvent = "FrituraCapas";
    private const string FryerTimerEvent = "Cronometro";
    private const float CarPassDuration = 3.15f;
    private const float CarSoundDuration = 3.422f;
    private const float CarHalfRoute = 580f;
    private const float CarGroundHeight = 38.9f;
    private static readonly Vector3 CarLaneOffset = new(0f, 0f, 82f);
    private static readonly Vector3 CarDirection = Vector3.left;

    private enum Trigger { Near, Use, Loop, Enter, Door, ManualLoop, UseRightClick }

    [Serializable]
    private sealed class Point
    {
        public string label;
        public string audioEvent;
        public Vector3 position;
        public float radius;
        [InspectorName("Volumen (dB)"), Range(-60f, 12f)]
        public float volumeDb;
        public Trigger trigger;
        public string secondaryEvent;
        public int secondaryEventEvery;
        [InspectorName("Radio de puntería"), Min(0f)]
        public float aimRadius;
        [InspectorName("Número de lavamanos"), Min(0)]
        public int sinkNumber;
        public string doorLeaf;
        public string doorSecondLeaf;
        public float doorWidth;
        public bool doorNormalZ;
        public bool doorOpensPositive;
        public bool doorHingeAtPositiveEnd;
        public float doorBottom;
        public float doorHeight;
        public BoxCollider doorInteractionCollider;

        [NonSerialized] public GameObject emitter;
        [NonSerialized] public Transform puertaA;
        [NonSerialized] public Transform puertaB;
        [NonSerialized] public float apertura;
        [NonSerialized] public bool puertaAbierta;
        [NonSerialized] public BoxCollider pasoPuerta;
        [NonSerialized] public bool inside;
        [NonSerialized] public uint loop;
        [NonSerialized] public uint fadingSinkSound;
        [NonSerialized] public float stopWaterAt;
        [NonSerialized] public Point handWashPoint;
        [NonSerialized] public bool nearHandWash;
        [NonSerialized] public bool faucetManuallyClosed;
        [NonSerialized] public float gain;
        [NonSerialized] public bool useAlternative;
        [NonSerialized] public float oclusion = -1f;
        [NonSerialized] public int interactionCount;
    }

    [SerializeField] private Point[] points;
    [SerializeField, Range(-60f, 12f)] private float restaurantVolumeDb;
    [SerializeField] private ZonaMusical zonaAmbienteComedor;
    [SerializeField, Min(1f)] private float distanciaFundidoAmbienteComedor = 52f;
    [SerializeField] private Collider[] superficiesDriveThru;
    [SerializeField] private GameObject autoDriveThruPrefab;
    [SerializeField, Range(-60f, 12f)] private float layeredFryingVolumeDb;
    private Camera player;
    private AccionesDeCocina accionesDeCocina;
#if UNITY_EDITOR
    private GameObject previewEmitter;
    private uint previewPlayingId;
    private Point previewPoint;
    private bool ownsPreviewBank;
#endif
    private Transform playerRoot;
    private FirstPersonMovement movimiento;
    private StarterAssets.ThirdPersonController jugadorStarter;
    private int paredesAcusticas;
    private int paredExterior;
    private int cristalAcustico;
    private float siguienteConsultaAcustica;
    private readonly RaycastHit[] obstaculosAcusticos = new RaycastHit[16];
    private GroundCheck sueloJugador;
    private Vector3 ultimaPosicionPasos;
    private float tiempoPasos;
    private DetectorHamburguesa estacionHamburguesa;
    private float mezclaPrimerPlano;
    private float mezclaEstacion;
    private const float SegundosBajadaMusicaHamburguesa = 1.35f;
    private const float SegundosRegresoMusicaHamburguesa = 1.6f;
    private int efectosActivos;
    private ulong listenerId;
    private Point fryerPoint;
    private Point televisionPoint;
    [SerializeField] private Renderer pantallaTelevisor;
    private MaterialPropertyBlock aspectoPantalla;
    private static readonly int ColorPantalla = Shader.PropertyToID("_BaseColor");
    private GameObject layeredFryingEmitter;
    private Point entrancePoint;
    private bool ownsBank;
    private uint ambience;
    private uint dialogue;
    private bool fryerDialoguePlaying;
    private float fryerEffectsGain = 1f;
    private uint driveThruReplay;
    private Point puntoVozBano;
    [System.NonSerialized] private GUIStyle estiloCartelVoz;
    private int nextDialogue;
    private string interaction;
    [System.NonSerialized] private GUIStyle instructionStyle;
    [NonSerialized] private Point car;
    private float carStarted;
    private GameObject carVisual;
    private bool autoMostrado;
    private bool ladronAparecio;
    private float kitchenMix;
    private Point diningPoint;
    private readonly bool[] lugaresExplorados = new bool[6];
    private static readonly string[] Lugares =
        { "Drive thru", "Cocina", "Radio", "Baño", "Televisión", "Puerta" };
    [System.NonSerialized] private GUIStyle estiloRecorrido;
    [System.NonSerialized] private GUIStyle estiloPuntoRecorrido;
    private GUIStyle estiloAvisoPedido;
    private Material materialGuiaSuelo;
    private Mesh mallaGuiaSuelo;
    private const float DuracionAvisoPedido = 1.15f;
    private readonly List<(Vector3 origen, float inicio)> avisosPedido = new();
    [SerializeField] private ZonaMusical[] zonasMusicales;
    [SerializeField, Min(1f)] private float distanciaFundidoMusical = 65f;
    [SerializeField, Min(0.1f)] private float segundosFundidoMusical = 0.6f;

    private static readonly string[] ZonasPorBloque =
    {
        "MusicaEstacionamiento", "MusicaBano", "MusicaComedor", "MusicaCocina"
    };

    private static readonly string[] ParametrosMusicales =
    {
        "ProximidadBloque1", "ProximidadBloque2", "ProximidadBloque3", "ProximidadBloque4"
    };

    private int bloqueActivo = -1;
    private const float GananciaBordeMusical = 0.14f;
    private const int FundidoSalidaBloqueMs = 220;
    private readonly HashSet<(string evento, ulong emisor)> efectosDeInteraccion = new();
    private float coberturaMusical;
    private GameObject emisorMusical;
    private uint musica;

    private void Start()
    {
        car = null;
        carStarted = 0f;

        if (points == null || points.Length < DialogueCount)
        {
            Debug.LogError("La sonorización necesita los cuatro puntos de la secuencia de diálogo.", this);
            enabled = false;
            return;
        }

        player = Camera.main;
        if (player == null || !player.TryGetComponent<AkAudioListener>(out var oyente) ||
            !oyente.isActiveAndEnabled || !AkUnitySoundEngine.IsInitialized())
        {
            Debug.LogError("La sonorización necesita un AkAudioListener activo en la cámara principal.", this);
            enabled = false;
            return;
        }

        listenerId = oyente.GetAkGameObjectID();
        var resultadoOyente = AkUnitySoundEngine.SetDefaultListeners(new[] { listenerId }, 1);
        if (resultadoOyente != AKRESULT.AK_Success)
        {
            Debug.LogError($"Wwise no pudo usar la cámara principal como oyente: {resultadoOyente}.", this);
            enabled = false;
            return;
        }
        AkUnitySoundEngine.SetObjectPosition(player.gameObject, player.transform);

        var result = AkUnitySoundEngine.LoadBank(Bank, out _);
        if (result != AKRESULT.AK_Success && result != AKRESULT.AK_BankAlreadyLoaded)
        {
            Debug.LogError($"No se pudo cargar {Bank}.bnk: {result}", this);
            enabled = false;
            return;
        }

        ownsBank = result == AKRESULT.AK_Success;
        accionesDeCocina = GetComponent<AccionesDeCocina>();
        diningPoint = Array.Find(points, point => point.audioEvent == DiningAmbienceEvent);
        fryerPoint = Array.Find(points, point => point.audioEvent == FryerAmbienceEvent);
        televisionPoint = Array.Find(points, point => point.audioEvent == TelevisionEvent);
        ActualizarPantallaTelevisor(false);
        entrancePoint = Array.Find(points, point => point.audioEvent == FinalDialogueEvent);
        jugadorStarter = player.GetComponentInParent<StarterAssets.ThirdPersonController>();
        var playerBody = player.GetComponentInParent<Rigidbody>();
        playerRoot = jugadorStarter != null ? jugadorStarter.transform :
            playerBody != null ? playerBody.transform : player.transform;
        movimiento = playerRoot.GetComponent<FirstPersonMovement>();
        sueloJugador = playerRoot.GetComponentInChildren<GroundCheck>();
        ultimaPosicionPasos = playerRoot.position;
        paredesAcusticas = LayerMask.GetMask("ParedExterior", "ParedInterior", "CristalAcustico");
        paredExterior = LayerMask.NameToLayer("ParedExterior");
        cristalAcustico = LayerMask.NameToLayer("CristalAcustico");
        estacionHamburguesa = FindAnyObjectByType<DetectorHamburguesa>();
        foreach (var point in points)
        {
            point.emitter = CreateEmitter(point.label + " · " + point.audioEvent, point.position);
            if (point.trigger == Trigger.Door)
                PrepararPuerta(point);
            if (point.audioEvent == PassingCarEvent && autoDriveThruPrefab != null)
            {
                carVisual = Instantiate(autoDriveThruPrefab, point.emitter.transform);
                carVisual.transform.localPosition = Vector3.up * (CarGroundHeight - point.position.y);
                carVisual.transform.rotation = Quaternion.LookRotation(CarDirection);
                carVisual.SetActive(false);
            }
            point.gain = DistanceGain(point, player.transform.position);
            SetVolume(point.emitter, point.volumeDb, point.gain);
            if (point.trigger == Trigger.ManualLoop && point.sinkNumber > 0)
                point.handWashPoint = Array.Find(points, candidate =>
                    candidate.sinkNumber == point.sinkNumber && candidate.audioEvent == "LavadoManos");
        }
        puntoVozBano = Array.Find(points, point => point.audioEvent == BathroomDialogueEvent);
        if (puntoVozBano != null && accionesDeCocina != null)
        {
            accionesDeCocina.CrearPersonajeBano(puntoVozBano.emitter);
            puntoVozBano.position = puntoVozBano.emitter.transform.position;
        }
        var puntoVozFreidora = Array.Find(points, point => point.audioEvent == FryerDialogueEvent);
        if (puntoVozFreidora != null && accionesDeCocina != null)
        {
            accionesDeCocina.CrearPersonajeFreidora(puntoVozFreidora.emitter);
            puntoVozFreidora.position = puntoVozFreidora.emitter.transform.position;
        }

        if (fryerPoint != null)
            layeredFryingEmitter = CreateEmitter("Freidora · fritura en capas", fryerPoint.position);
        if (zonasMusicales != null && zonasMusicales.Length > 0)
        {
            emisorMusical = CreateEmitter("Musica interactiva", transform.position);
            ActualizarMusica(player.transform.position);
        }
        UpdateExtraVolumes();
        if (diningPoint == null)
            ambience = Post(DiningAmbienceEvent, gameObject);
        CrearGuiaRecorrido();
    }

    private GameObject CreateEmitter(string emitterName, Vector3 position)
    {
        var emitter = new GameObject(emitterName, typeof(AkGameObj));
        emitter.transform.SetParent(transform);
        emitter.transform.position = position;
        AkUnitySoundEngine.SetObjectPosition(emitter, emitter.transform);
        return emitter;
    }

    private void SetVolume(GameObject emitter, float decibels, float distanceGain = 1f)
        => SetVolume(emitter, listenerId, decibels, distanceGain);

    private static void SetVolume(GameObject emitter, ulong listener, float decibels, float distanceGain)
    {
        var gain = decibels <= -60f ? 0f : Mathf.Pow(10f, decibels / 20f) * distanceGain;
        AkUnitySoundEngine.SetGameObjectOutputBusVolume(
            AkUnitySoundEngine.GetAkGameObjectID(emitter), listener, gain);
    }

    private void UpdateExtraVolumes()
    {
        var diningDuck = Mathf.Lerp(1f, 0.45f, kitchenMix) *
            Mathf.Lerp(1f, 0.4f, mezclaPrimerPlano) * Mathf.Lerp(1f, 0.55f, mezclaEstacion);
        if (diningPoint == null)
            SetVolume(gameObject, restaurantVolumeDb, diningDuck);
        else
            SetVolume(diningPoint.emitter, diningPoint.volumeDb, diningPoint.gain * diningDuck);
        if (layeredFryingEmitter != null)
            SetVolume(layeredFryingEmitter, layeredFryingVolumeDb, fryerPoint.gain *
                Mathf.Lerp(1f, 0.79f, CercaniaEfecto(player.transform.position, fryerPoint.position)));
    }

    private void Update()
    {
        var ahora = Time.time;
        avisosPedido.RemoveAll(aviso => ahora - aviso.inicio >= DuracionAvisoPedido);

        if (car != null && car.emitter == null)
        {
            car = null;
            if (carVisual != null)
                carVisual.SetActive(false);
        }

        var playerPosition = player.transform.position;
        ActualizarMusica(playerPosition);
        if (Time.unscaledTime >= siguienteConsultaAcustica)
        {
            ActualizarOclusion(playerPosition);
            siguienteConsultaAcustica = Time.unscaledTime + 0.10f;
        }
        var cocinaMusical = estacionHamburguesa != null && estacionHamburguesa.AudioEnCurso;
        var cercaniaEstacion = cocinaMusical ? Mathf.SmoothStep(1f, 0f,
            (Distance(playerPosition, estacionHamburguesa.Centro) - 105f) / 110f) : 0f;
        if (cocinaMusical && estacionHamburguesa.EstaCompleta)
            cercaniaEstacion = estacionHamburguesa.CercaniaHamburguesaCompleta(playerPosition);
        var segundosFundidoEstacion = cercaniaEstacion > mezclaEstacion
            ? SegundosBajadaMusicaHamburguesa : SegundosRegresoMusicaHamburguesa;
        mezclaEstacion = Mathf.MoveTowards(mezclaEstacion, cercaniaEstacion,
            Time.deltaTime / segundosFundidoEstacion);
        if (cocinaMusical)
            estacionHamburguesa.AjustarEscucha(playerPosition, listenerId);
        var primerPlano = efectosActivos > 0 || (dialogue != 0 && !fryerDialoguePlaying) || driveThruReplay != 0 ||
            Array.Exists(points, point => point.loop != 0 && point.audioEvent == "DialogoRadio");
        var cercaniaTelevisor = televisionPoint != null ? CercaniaEfecto(playerPosition, televisionPoint.position) : 0f;
        var mezclaLocal = televisionPoint != null && televisionPoint.loop != 0 ? 0.35f * cercaniaTelevisor : 0f;
        var cercaniaFreidora = fryerPoint != null ? CercaniaEfecto(playerPosition, fryerPoint.position) : 0f;
        if (fryerDialoguePlaying || efectosDeInteraccion.Contains((LayeredFryingEvent, 0UL)))
            mezclaLocal = Mathf.Max(mezclaLocal, 0.28f * cercaniaFreidora);
        var objetivoPrimerPlano = primerPlano ? 1f : mezclaLocal;
        mezclaPrimerPlano = Mathf.MoveTowards(mezclaPrimerPlano, objetivoPrimerPlano,
            Time.deltaTime * (objetivoPrimerPlano > mezclaPrimerPlano ? 2.5f : 1.35f));
        if (emisorMusical != null)
            SetVolume(emisorMusical, 2f, coberturaMusical *
                Mathf.SmoothStep(1f, 0.1f, mezclaEstacion) * Mathf.Lerp(1f, 0.53f, mezclaPrimerPlano));
        Point selected = null;
        Point puertaMarcada = null;
        var nearest = float.PositiveInfinity;
        interaction = null;
        var aim = player.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        var targetKitchenMix = 0f;
        fryerEffectsGain = Mathf.MoveTowards(fryerEffectsGain,
            fryerDialoguePlaying ? Mathf.Lerp(1f, 0.65f, cercaniaFreidora) : 1f, Time.deltaTime * 2f);

        for (var index = 0; index < points.Length; index++)
        {
            var point = points[index];
            var distance = Distance(playerPosition, point.position);
            var targetGain = DistanceGain(point, playerPosition);
            var zoneAmbience = IsZoneAmbience(point.audioEvent);
            var offset = playerPosition - point.position;
            var profundidadPuerta = point.doorNormalZ ? offset.z : offset.x;
            var lateralPuerta = point.doorNormalZ ? offset.x : offset.z;
            var inside = point.trigger == Trigger.Door
                ? Mathf.Abs(profundidadPuerta) <= 62f &&
                  Mathf.Abs(offset.y) <= 70f && Mathf.Abs(lateralPuerta) <= point.doorWidth + 12f
                : zoneAmbience ? targetGain > 0f : distance <= point.radius;
            if (point.audioEvent == PassingCarEvent)
                inside = Mathf.Abs(offset.x) <= point.radius && offset.z >= 0f && offset.z <= 70f;
            var entered = inside && !point.inside;
            point.inside = inside;

            var segundosRespuesta = targetGain > point.gain ? 0.45f : 0.75f;
            point.gain = point == diningPoint && zonaAmbienteComedor != null
                ? targetGain
                : zoneAmbience
                ? Mathf.Lerp(targetGain, point.gain, Mathf.Exp(-Time.deltaTime / segundosRespuesta))
                : Mathf.MoveTowards(point.gain, targetGain, Time.deltaTime * 2f);
            if (zoneAmbience && targetGain == 0f && point.gain < 0.002f)
                point.gain = 0f;
            var gananciaFuente = point.audioEvent switch
            {
                TelevisionEvent => Mathf.Lerp(1f, 0.79f, cercaniaTelevisor),
                FryerAmbienceEvent => fryerEffectsGain * Mathf.Lerp(1f, 0.89f, cercaniaFreidora),
                FryerDialogueEvent => Mathf.Lerp(1f, 0.89f, cercaniaFreidora),
                _ => 1f
            };
            if (point != diningPoint)
                SetVolume(point.emitter, point.volumeDb, point.gain * gananciaFuente *
                    (zoneAmbience ? Mathf.Lerp(1f, 0.4f, mezclaPrimerPlano) *
                        Mathf.Lerp(1f, 0.55f, mezclaEstacion) : 1f));
            if (point.audioEvent == KitchenAmbienceEvent)
                targetKitchenMix = Mathf.Max(targetKitchenMix, point.gain);

            var repeatableDriveThru = point.audioEvent == DriveThruDialogueEvent && index < nextDialogue;
            if (!repeatableDriveThru && index < DialogueCount &&
                (index == 0 || point.trigger != Trigger.UseRightClick))
            {
                if (index == nextDialogue && dialogue == 0 && inside)
                {
                    if (point.trigger == Trigger.Near)
                        StartDialogue(point);
                    else if (CanInteract(point, aim, out var dialogueAimDistance))
                    {
                        selected = point;
                        nearest = point.aimRadius > 0f ? dialogueAimDistance : distance;
                    }
                }
                continue;
            }

            switch (point.trigger)
            {
                case Trigger.ManualLoop:
                    UpdateHandWash(point, playerPosition);
                    if (point.stopWaterAt > 0f && Time.time >= point.stopWaterAt)
                    {
                        StopSinkSound(point, 250);
                        point.stopWaterAt = 0f;
                    }
                    break;
                case Trigger.Loop:
                    if (inside && point.loop == 0)
                        point.loop = Post(point.audioEvent, point.emitter);
                    else if (point.loop != 0 && point.gain <= 0f)
                    {
                        AkUnitySoundEngine.StopPlayingID(point.loop, 650);
                        point.loop = 0;
                    }
                    break;
                case Trigger.Enter:
                    if (entered && car == null && !autoMostrado)
                    {
                        autoMostrado = true;
                        car = point;
                        carStarted = Time.unscaledTime;
                        point.emitter.transform.position = point.position + CarLaneOffset -
                            CarDirection * CarHalfRoute;
                        AkUnitySoundEngine.SetObjectPosition(point.emitter, point.emitter.transform);
                        if (point.audioEvent == PassingCarEvent && carVisual != null)
                            carVisual.SetActive(true);
                        Post(point.audioEvent, point.emitter);
                        Post(point.secondaryEvent, point.emitter);
                    }
                    break;
                case Trigger.Door:
                {
                    ActualizarPuerta(point);
                    if (inside && point.puertaAbierta)
                        MostrarLadronSiCorresponde(point);
                    if (point.doorInteractionCollider != null)
                    {
                        if (SeleccionarHojaMarcada(point, aim))
                        {
                            puertaMarcada = point;
                            break;
                        }
                        if (!point.puertaAbierta)
                            break;
                    }

                    var plano = new Plane(point.doorNormalZ ? Vector3.forward : Vector3.right, point.position);
                    var semiancho = point.doorWidth * (point.puertaB != null ? 1f : 0.5f);
                    if (inside && plano.Raycast(aim, out var distanciaPuerta) && distanciaPuerta < nearest)
                    {
                        var impacto = aim.GetPoint(distanciaPuerta);
                        var local = impacto - point.position;
                        var lateral = point.doorNormalZ ? local.x : local.z;
                        var basePuerta = point.doorBottom > 0f ? point.doorBottom : 38.9f;
                        var alturaPuerta = point.doorHeight > 0f ? point.doorHeight : 100f;
                        var golpeEnHoja = Mathf.Abs(lateral) <= semiancho &&
                            impacto.y >= basePuerta && impacto.y <= basePuerta + alturaPuerta;
                        if (golpeEnHoja &&
                            PasoDePuertaVisible(point, aim, distanciaPuerta))
                        {
                            selected = point;
                            nearest = distanciaPuerta;
                        }
                    }
                    break;
                }
                case Trigger.Use:
                case Trigger.UseRightClick:
                    if (!inside || !CanInteract(point, aim, out var aimDistance))
                        break;

                    var aimed = point.aimRadius > 0f;
                    var selectedIsAimed = selected != null &&
                        (selected.aimRadius > 0f || selected.trigger == Trigger.Door);
                    var priority = aimed ? aimDistance : distance;
                    if (aimed != selectedIsAimed ? aimed : priority < nearest)
                    {
                        selected = point;
                        nearest = priority;
                    }
                    break;
            }

        }


        if (car != null)
        {
            var elapsed = Time.unscaledTime - carStarted;
            // Keep the sound receding after the car leaves the visible route.
            car.emitter.transform.position = car.position + CarLaneOffset +
                CarDirection * Mathf.LerpUnclamped(-CarHalfRoute, CarHalfRoute,
                    elapsed / CarPassDuration);
            AkUnitySoundEngine.SetObjectPosition(car.emitter, car.emitter.transform);
            if (elapsed >= CarPassDuration && car.audioEvent == PassingCarEvent &&
                carVisual != null && carVisual.activeSelf)
                carVisual.SetActive(false);
            if (elapsed >= CarSoundDuration)
                car = null;
        }

        kitchenMix = Mathf.Lerp(targetKitchenMix, kitchenMix, Mathf.Exp(-Time.deltaTime / 0.7f));
        UpdateExtraVolumes();
#if UNITY_EDITOR
        if (previewPoint != null)
            SetVolume(previewEmitter, previewPoint.volumeDb, DistanceGain(previewPoint, playerPosition));
#endif
        interaction = accionesDeCocina?.ActualizarInteraccion(player, aim);
        selected = puertaMarcada ?? selected;
        if (interaction != null || selected == null)
            return;

        if (selected.trigger == Trigger.Door)
        {
            var bloqueaCierre = selected.puertaAbierta && JugadorEnUmbral(selected);
            interaction = bloqueaCierre ? "Dejá libre el umbral para cerrar" :
                (selected.puertaAbierta ? "E · Cerrar · " : "E · Abrir · ") + selected.label;
            if (!bloqueaCierre && Input.GetKeyDown(KeyCode.E))
                CambiarPuerta(selected, !selected.puertaAbierta);
            return;
        }

        if (selected == entrancePoint || selected == puntoVozBano)
        {
            if (Input.GetKeyDown(KeyCode.E) && VozAccesible(selected) &&
                Post(selected.audioEvent, selected.emitter, sinSuperponer: true) != 0)
                MarcarExplorado(selected.audioEvent);
            return;
        }

        if (selected.sinkNumber > 0)
        {
            var faucet = Array.Find(points,
                point => point.trigger == Trigger.ManualLoop && point.sinkNumber == selected.sinkNumber);
            var action = faucet == null ? "Sin canilla vinculada" :
                faucet.stopWaterAt > 0f ? "Cancelar cierre del agua" :
                faucet.loop == 0 && selected.loop == 0 ? "Lavar manos" : "Cerrar agua";
            interaction = "E o clic derecho · " + action + " · " + selected.label;
            if (faucet != null && (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(1)))
                ToggleWater(faucet);
            return;
        }

        if (selected.trigger == Trigger.UseRightClick)
        {
            var freidora = selected.audioEvent == FryerDialogueEvent;
            interaction = (freidora ? "E, I o clic derecho · " : "E o clic derecho · ") + selected.label;
            if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(1) ||
                (freidora && Input.GetKeyDown(KeyCode.I)))
                Interact(selected);
            return;
        }

        var requiresAim = selected.aimRadius > 0f;
        interaction = (requiresAim ? "Clic izquierdo" : "E o clic izquierdo") + "  ·  " + selected.label;
        if (!Input.GetMouseButtonDown(0) && (requiresAim || !Input.GetKeyDown(KeyCode.E)))
            return;

        Interact(selected);
    }

    private void ActualizarMusica(Vector3 posicion)
    {
        if (emisorMusical == null)
            return;

        var siguiente = -1;
        var distanciaMasCorta = float.PositiveInfinity;
        var prioridad = int.MinValue;
        foreach (var zona in zonasMusicales)
        {
            if (zona == null)
                continue;

            var bloque = Array.IndexOf(ZonasPorBloque, zona.Evento);
            var distancia = zona.DistanciaAlBorde(posicion);
            if (bloque < 0 || float.IsPositiveInfinity(distancia))
                continue;

            var dentro = distancia <= 0f;
            var seleccionadoDentro = distanciaMasCorta <= 0f;
            if (dentro && seleccionadoDentro
                    ? zona.Prioridad < prioridad || (zona.Prioridad == prioridad && distancia >= distanciaMasCorta)
                    : distancia >= distanciaMasCorta)
                continue;
            siguiente = bloque;
            distanciaMasCorta = distancia;
            prioridad = zona.Prioridad;
        }

        var dentroDeZona = siguiente >= 0 && distanciaMasCorta <= 0f;
        if (dentroDeZona && (musica == 0 || siguiente != bloqueActivo))
            IniciarBloque(siguiente);

        var profundidad = -distanciaMasCorta;
        if (dentroDeZona)
        {
            // Una zona prioritaria también puede empezar antes de que termine la caja actual.
            foreach (var zona in zonasMusicales)
            {
                if (zona == null || zona.Prioridad <= prioridad)
                    continue;
                var bloque = Array.IndexOf(ZonasPorBloque, zona.Evento);
                if (bloque >= 0 && bloque != siguiente)
                    profundidad = Mathf.Min(profundidad, zona.DistanciaAlBorde(posicion));
            }
        }

        var objetivo = dentroDeZona
            ? Mathf.SmoothStep(GananciaBordeMusical, 1f, profundidad / distanciaFundidoMusical)
            : Mathf.SmoothStep(GananciaBordeMusical, 0f, distanciaMasCorta / distanciaFundidoMusical);

        coberturaMusical = Mathf.MoveTowards(coberturaMusical, objetivo,
            Time.deltaTime / (objetivo > coberturaMusical ? segundosFundidoMusical : 0.15f));
        if (musica != 0 && coberturaMusical <= 0f && objetivo <= 0f)
        {
            AkUnitySoundEngine.StopPlayingID(musica);
            musica = 0;
            bloqueActivo = -1;
        }
        if (musica == 0)
            coberturaMusical = 0f;
    }

    private void IniciarBloque(int bloque)
    {
        AkUnitySoundEngine.SetRTPCValue(ParametrosMusicales[bloque], 100f, emisorMusical);
        var nuevo = AkUnitySoundEngine.PostEvent($"Play_SeccionZona{bloque + 1}",
            AkUnitySoundEngine.GetAkGameObjectID(emisorMusical),
            (uint)AkCallbackType.AK_EndOfEvent,
            (_, tipo, info) =>
            {
                if (tipo == AkCallbackType.AK_EndOfEvent &&
                    info is AkEventCallbackInfo evento && evento.playingID == musica)
                    musica = 0;
            }, null);
        if (nuevo == 0)
        {
            Debug.LogError($"Wwise no pudo reproducir la sección {bloque + 1}.", this);
            return;
        }

        if (musica != 0)
            AkUnitySoundEngine.StopPlayingID(musica, FundidoSalidaBloqueMs, AkCurveInterpolation.AkCurveInterpolation_Sine);
        coberturaMusical = Mathf.Min(coberturaMusical, GananciaBordeMusical);
        musica = nuevo;
        bloqueActivo = bloque;
    }

    private void ToggleWater(Point faucet)
    {
        if (faucet.stopWaterAt > 0f)
        {
            faucet.stopWaterAt = 0f;
            faucet.faucetManuallyClosed = false;
            faucet.nearHandWash = false;
        }
        else if (faucet.loop != 0 || (faucet.handWashPoint?.loop ?? 0) != 0)
        {
            faucet.faucetManuallyClosed = true;
            if (faucet.handWashPoint != null)
                StopSinkSound(faucet.handWashPoint, 0);
            if (faucet.loop != 0)
                faucet.stopWaterAt = Time.time + WaterStopDelay;
        }
        else if (faucet.handWashPoint != null)
        {
            faucet.faucetManuallyClosed = false;
            faucet.nearHandWash = true;
            faucet.handWashPoint.loop = Post(faucet.handWashPoint.audioEvent, faucet.handWashPoint.emitter);
        }
        else
            faucet.loop = Post(faucet.audioEvent, faucet.emitter);
    }

    private void UpdateHandWash(Point faucet, Vector3 playerPosition)
    {
        var wash = faucet.handWashPoint;
        if (wash == null || faucet.faucetManuallyClosed)
            return;

        var nearby = Distance(playerPosition, wash.position) <= HandWashRadius;
        if (nearby == faucet.nearHandWash)
            return;

        faucet.nearHandWash = nearby;
        if (nearby)
        {
            StopSinkSound(faucet, 250);
            wash.loop = Post(wash.audioEvent, wash.emitter);
        }
        else if (!nearby && wash.loop != 0)
        {
            StopSinkSound(wash, 250);
            faucet.loop = Post(faucet.audioEvent, faucet.emitter);
        }
    }

    private void PrepararPuerta(Point punto)
    {
        if (string.IsNullOrEmpty(punto.doorLeaf))
            return;

        var doble = !string.IsNullOrEmpty(punto.doorSecondLeaf);
        var eje = punto.doorNormalZ ? Vector3.right : Vector3.forward;
        var semiancho = punto.doorWidth * (doble ? 1f : 0.5f);
        var sentidoBisagra = !doble && punto.doorHingeAtPositiveEnd ? 1f : -1f;
        punto.puertaA = CrearBisagra(punto.doorLeaf, punto.position + eje * (semiancho * sentidoBisagra),
            punto.doorInteractionCollider == null);
        if (doble)
            punto.puertaB = CrearBisagra(punto.doorSecondLeaf, punto.position + eje * semiancho);
        punto.puertaAbierta = false;
        punto.apertura = 0f;
        if (punto.puertaA == null || (doble && punto.puertaB == null))
            return;

        if (punto.doorInteractionCollider != null)
            punto.doorInteractionCollider.transform.SetParent(punto.puertaA, true);

        punto.pasoPuerta = punto.emitter.AddComponent<BoxCollider>();
        var alto = punto.doorHeight > 0f ? punto.doorHeight : 90f;
        var basePuerta = punto.doorBottom > 0f ? punto.doorBottom : 38.9f;
        punto.pasoPuerta.center = Vector3.up * (basePuerta + alto * 0.5f - punto.position.y);
        punto.pasoPuerta.size = punto.doorNormalZ
            ? new Vector3(2f * semiancho, alto, 3f)
            : new Vector3(3f, alto, 2f * semiancho);
    }

    private Transform CrearBisagra(string nombreHoja, Vector3 posicion, bool agruparDetalles = true)
    {
        var hoja = GameObject.Find(nombreHoja);
        if (hoja == null)
        {
            Debug.LogWarning($"No se encontró la hoja de puerta {nombreHoja}.", this);
            return null;
        }

        var bisagra = new GameObject("Bisagra · " + nombreHoja).transform;
        bisagra.SetParent(hoja.transform.parent);
        bisagra.SetPositionAndRotation(posicion, Quaternion.identity);
        hoja.transform.SetParent(bisagra, true);
        if (agruparDetalles)
        {
            foreach (var pieza in FindObjectsByType<Transform>())
                if (pieza.name.StartsWith(nombreHoja + " ", StringComparison.Ordinal))
                    pieza.SetParent(bisagra, true);
        }
        return bisagra;
    }

    private void CrearGuiaRecorrido()
    {
        var entradaCocina = Array.Find(points, punto => punto.doorLeaf == "Hoja animada Salida servicio");
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (entradaCocina == null || shader == null)
            return;

        materialGuiaSuelo = new Material(shader) { color = new Color(1f, 0.79f, 0.04f) };
        var guia = new GameObject("Guía de suelo · recorrido completo").transform;
        guia.SetParent(transform, false);
        var vertices = new List<Vector3>();
        var triangulos = new List<int>();
        var camino = new[]
        {
            // Drive thru y entrada exterior de la cocina.
            new Vector3(5296f, 74f, 883f), new Vector3(5315f, 74f, 894f),
            new Vector3(5130f, 74f, 850f), new Vector3(5107f, 74f, 803f),
            new Vector3(entradaCocina.position.x, 74f, entradaCocina.position.z + 20f),
            new Vector3(4975f, 74f, 660f),
            // Freidora y radio, por el hueco entre las mesadas.
            new Vector3(5075f, 74f, 535f), new Vector3(5075f, 74f, 520f),
            new Vector3(4970f, 74f, 365f), new Vector3(5120f, 74f, 270f),
            // Puerta del comedor y pasillo de los baños.
            new Vector3(5300f, 74f, 260f), new Vector3(5345f, 74f, 210f),
            new Vector3(5365f, 74f, 210f), new Vector3(5375f, 74f, 225f),
            new Vector3(5470f, 74f, 580f),
            // Regreso por el comedor, televisor y puerta principal.
            new Vector3(5460f, 74f, 510f), new Vector3(5435f, 74f, -55f),
            new Vector3(5535f, 74f, 20f), new Vector3(5566f, 74f, 20f),
            new Vector3(5605f, 74f, 20f), new Vector3(5650f, 74f, 20f)
        };
        for (var i = 1; i < camino.Length; i++)
        {
            var inicio = camino[i - 1];
            var delta = camino[i] - inicio;
            var pasos = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / 24f));
            var ultimoPaso = i == camino.Length - 1 ? pasos : pasos - 1;
            for (var j = 0; j <= ultimoPaso; j++)
            {
                var posicion = inicio + delta * (j / (float)pasos);
                const float alturaPavimentoVisible = 38.9f;
                const float separacionMarcas = 2f;
                var altura = alturaPavimentoVisible;
                if (Physics.Raycast(posicion, Vector3.down, out var suelo, 52f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && suelo.normal.y >= 0.8f)
                    altura = Mathf.Max(altura, suelo.point.y);
                var centro = new Vector3(posicion.x, altura + separacionMarcas, posicion.z);
                var primerVertice = vertices.Count;
                vertices.Add(guia.InverseTransformPoint(centro));
                const int lados = 10;
                for (var lado = 0; lado < lados; lado++)
                {
                    var angulo = lado * (2f * Mathf.PI / lados);
                    var borde = new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * 3.3f;
                    vertices.Add(guia.InverseTransformPoint(centro + borde));
                    triangulos.Add(primerVertice);
                    triangulos.Add(primerVertice + 1 + (lado + 1) % lados);
                    triangulos.Add(primerVertice + 1 + lado);
                }
            }
        }
        var malla = new Mesh { name = "Puntos del recorrido completo" };
        malla.SetVertices(vertices);
        malla.SetTriangles(triangulos, 0);
        guia.gameObject.AddComponent<MeshFilter>().sharedMesh = malla;
        guia.gameObject.AddComponent<MeshRenderer>().sharedMaterial = materialGuiaSuelo;
        mallaGuiaSuelo = malla;
    }

    private bool JugadorEnUmbral(Point punto)
    {
        var offset = player.transform.position - punto.position;
        var profundidad = punto.doorNormalZ ? offset.z : offset.x;
        var lateral = punto.doorNormalZ ? offset.x : offset.z;
        var semiancho = punto.doorWidth * (punto.puertaB != null ? 1f : 0.5f);
        return Mathf.Abs(profundidad) < 17f && Mathf.Abs(lateral) < semiancho + 14f &&
            Mathf.Abs(offset.y) < 72f;
    }

    private void CambiarPuerta(Point punto, bool abrir)
    {
        punto.puertaAbierta = abrir;
        if (abrir)
            Post(punto.audioEvent, punto.emitter);
        if (abrir && !string.IsNullOrEmpty(punto.doorSecondLeaf))
        {
            MarcarExplorado(FinalDialogueEvent);
            MostrarLadronSiCorresponde(punto);
        }
    }

    private static bool PasoDePuertaVisible(Point puerta, Ray mira, float distancia)
    {
        foreach (var impacto in Physics.RaycastAll(mira, distancia, Physics.DefaultRaycastLayers,
                     QueryTriggerInteraction.Ignore))
            if (impacto.collider != puerta.pasoPuerta && impacto.collider != puerta.doorInteractionCollider)
                return false;

        return true;
    }

    private static bool SeleccionarHojaMarcada(Point puerta, Ray mira)
    {
        if (puerta.puertaA == null)
            return false;

        return puerta.doorInteractionCollider.Raycast(mira, out var impacto, puerta.radius) &&
            PasoDePuertaVisible(puerta, mira, impacto.distance);
    }

    private void MostrarLadronSiCorresponde(Point puerta)
    {
        if (string.IsNullOrEmpty(puerta.doorSecondLeaf) || ladronAparecio || !lugaresExplorados[4] ||
            entrancePoint == null || accionesDeCocina == null)
            return;

        accionesDeCocina.MostrarPersonajeEntrada(puerta.position, entrancePoint.emitter);
        entrancePoint.position = entrancePoint.emitter.transform.position;
        ladronAparecio = true;
    }

    private void ActualizarPuerta(Point punto)
    {
        if (!punto.puertaAbierta && punto.apertura > 0f && JugadorEnUmbral(punto))
            CambiarPuerta(punto, true);

        var aperturaAnterior = punto.apertura;
        punto.apertura = Mathf.MoveTowards(punto.apertura, punto.puertaAbierta ? 1f : 0f, Time.deltaTime / 0.52f);
        var angulo = Mathf.SmoothStep(0f, 88f, punto.apertura) *
            (punto.doorOpensPositive ? 1f : -1f) * (punto.doorNormalZ ? -1f : 1f) *
            (punto.doorHingeAtPositiveEnd ? -1f : 1f);
        if (punto.pasoPuerta != null)
            punto.pasoPuerta.enabled = !punto.puertaAbierta && punto.apertura <= 0f;
        if (punto.puertaA != null)
            punto.puertaA.rotation = Quaternion.Euler(0f, angulo, 0f);
        if (punto.puertaB != null)
            punto.puertaB.rotation = Quaternion.Euler(0f, -angulo, 0f);
        if (aperturaAnterior > 0f && punto.apertura == 0f && !string.IsNullOrEmpty(punto.secondaryEvent))
            Post(punto.secondaryEvent, punto.emitter);
    }

    private static void StopSinkSound(Point point, int fadeMilliseconds)
    {
        if (point.fadingSinkSound != 0)
        {
            AkUnitySoundEngine.StopPlayingID(point.fadingSinkSound);
            point.fadingSinkSound = 0;
        }
        if (point.loop == 0)
            return;

        AkUnitySoundEngine.StopPlayingID(point.loop, fadeMilliseconds);
        if (fadeMilliseconds > 0)
            point.fadingSinkSound = point.loop;
        point.loop = 0;
    }

    private bool CanInteract(Point point, Ray aim, out float hitDistance)
    {
        hitDistance = 0f;
        if (point == entrancePoint && !ladronAparecio)
            return false;
        if ((point == puntoVozBano || point == entrancePoint) && !VozAccesible(point))
            return false;
        if (point.audioEvent == DriveThruDialogueEvent && superficiesDriveThru != null &&
            superficiesDriveThru.Length > 0)
        {
            if (!Physics.Raycast(aim, out var impacto, point.radius + 35f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ||
                Array.IndexOf(superficiesDriveThru, impacto.collider) < 0)
                return false;

            hitDistance = impacto.distance;
            return true;
        }

        if (point.aimRadius <= 0f)
            return true;

        var offset = point.position - aim.origin;
        var depth = Vector3.Dot(offset, aim.direction);
        if (depth <= 0f)
            return false;

        var missDistanceSquared = (offset - aim.direction * depth).sqrMagnitude;
        var radiusSquared = point.aimRadius * point.aimRadius;
        if (missDistanceSquared > radiusSquared)
            return false;

        hitDistance = Mathf.Max(0f, depth - Mathf.Sqrt(radiusSquared - missDistanceSquared));
        foreach (var hit in Physics.RaycastAll(aim, hitDistance, Physics.DefaultRaycastLayers,
                     QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(playerRoot))
                return false;
        }

        return true;
    }

    private void Interact(Point selected)
    {
        var advancesSequence = nextDialogue < DialogueCount && selected == points[nextDialogue];
        if (selected.audioEvent == "DialogoRadio" || selected.audioEvent == TelevisionEvent)
        {
            if (selected.loop != 0)
            {
                var playing = selected.loop;
                selected.loop = 0;
                AkUnitySoundEngine.StopPlayingID(playing);
                if (selected == televisionPoint)
                    ActualizarPantallaTelevisor(false);
                return;
            }

            selected.loop = AkUnitySoundEngine.PostEvent("Play_" + selected.audioEvent,
                AkUnitySoundEngine.GetAkGameObjectID(selected.emitter), (uint)AkCallbackType.AK_EndOfEvent,
                (_, type, info) =>
                {
                    if (type != AkCallbackType.AK_EndOfEvent ||
                        !(info is AkEventCallbackInfo evt) || evt.playingID != selected.loop)
                        return;
                    selected.loop = 0;
                    if (selected == televisionPoint)
                        ActualizarPantallaTelevisor(false);
                    if (advancesSequence && nextDialogue < DialogueCount && selected == points[nextDialogue])
                        nextDialogue++;
                }, null);
            if (selected.loop != 0)
            {
                if (selected == televisionPoint)
                    ActualizarPantallaTelevisor(true);
                MarcarExplorado(selected.audioEvent);
            }
            return;
        }
        if (advancesSequence || selected.audioEvent == FryerDialogueEvent)
        {
            if (dialogue == 0 && driveThruReplay == 0)
                StartDialogue(selected, advancesSequence);
        }
        else if (selected.audioEvent == DriveThruDialogueEvent)
        {
            if (dialogue != 0 || driveThruReplay != 0)
                return;

            Post(selected.secondaryEvent, selected.emitter, sinSuperponer: true);
            driveThruReplay = AkUnitySoundEngine.PostEvent("Play_" + selected.audioEvent,
                AkUnitySoundEngine.GetAkGameObjectID(selected.emitter), (uint)AkCallbackType.AK_EndOfEvent,
                (_, type, info) =>
                {
                    if (type == AkCallbackType.AK_EndOfEvent &&
                        info is AkEventCallbackInfo evt && evt.playingID == driveThruReplay)
                        driveThruReplay = 0;
                }, null);
            if (driveThruReplay != 0)
                MarcarExplorado(selected.audioEvent);
        }
        else if (selected.secondaryEventEvery > 0 && !string.IsNullOrEmpty(selected.secondaryEvent))
        {
            Post(selected.audioEvent, selected.emitter, sinSuperponer: true);
            selected.interactionCount = Mathf.Min(selected.interactionCount + 1, selected.secondaryEventEvery);
            if (selected.interactionCount == selected.secondaryEventEvery &&
                Post(selected.secondaryEvent, selected.emitter, sinSuperponer: true) != 0)
            {
                selected.interactionCount = 0;
                if (selected.secondaryEvent == "NotificacionPedido")
                    avisosPedido.Add((selected.position, Time.time));
            }
        }
        else
        {
            var alternate = !string.IsNullOrEmpty(selected.secondaryEvent);
            if (Post(alternate && selected.useAlternative ? selected.secondaryEvent : selected.audioEvent,
                    selected.emitter, sinSuperponer: true) != 0)
                selected.useAlternative = !selected.useAlternative;
        }
    }

    private void ActualizarPantallaTelevisor(bool encendida)
    {
        if (pantallaTelevisor == null)
            return;

        aspectoPantalla ??= new MaterialPropertyBlock();
        pantallaTelevisor.GetPropertyBlock(aspectoPantalla);
        aspectoPantalla.SetColor(ColorPantalla, encendida ? Color.white : Color.black);
        pantallaTelevisor.SetPropertyBlock(aspectoPantalla);
    }

    private void StartDialogue(Point point, bool advancesSequence = true)
    {
        var isFryerDialogue = point.audioEvent == FryerDialogueEvent;
        if (!string.IsNullOrEmpty(point.secondaryEvent))
            Post(point.secondaryEvent, point.emitter, sinSuperponer: true);

        dialogue = AkUnitySoundEngine.PostEvent("Play_" + point.audioEvent,
            AkUnitySoundEngine.GetAkGameObjectID(point.emitter), (uint)AkCallbackType.AK_EndOfEvent,
            (_, type, info) =>
            {
                if (type != AkCallbackType.AK_EndOfEvent ||
                    !(info is AkEventCallbackInfo evt) || evt.playingID != dialogue)
                    return;

                dialogue = 0;
                fryerDialoguePlaying = false;
                if (advancesSequence)
                    nextDialogue++;
            }, null);

        if (dialogue == 0)
            Debug.LogError($"Wwise no pudo reproducir {point.audioEvent}.", this);
        else if (isFryerDialogue)
        {
            fryerDialoguePlaying = true;
            if (layeredFryingEmitter != null)
                Post(LayeredFryingEvent, layeredFryingEmitter, sinSuperponer: true);
        }
        if (dialogue != 0)
            MarcarExplorado(point.audioEvent);
    }

    internal uint Post(string eventName, GameObject emitter, bool sinSuperponer = false)
    {
        var emitterId = AkUnitySoundEngine.GetAkGameObjectID(emitter);
        var clave = (eventName, eventName == ToiletFlushEvent ? emitterId : 0UL);
        if (sinSuperponer && !efectosDeInteraccion.Add(clave))
            return 0;

        var esEfecto = sinSuperponer || (Application.isPlaying && !IsZoneAmbience(eventName) &&
            eventName != "AguaLavabo" && eventName != "Fritura" && eventName != "MusicaInteractiva");
        var atenuaMezcla = esEfecto && eventName != LayeredFryingEvent && eventName != FryerTimerEvent;
        uint id;
        if (esEfecto)
        {
            id = AkUnitySoundEngine.PostEvent("Play_" + eventName, emitterId,
                (uint)AkCallbackType.AK_EndOfEvent, (_, type, _) =>
                {
                    if (type == AkCallbackType.AK_EndOfEvent)
                    {
                        if (atenuaMezcla)
                            efectosActivos = Mathf.Max(0, efectosActivos - 1);
                        if (sinSuperponer)
                            efectosDeInteraccion.Remove(clave);
                    }
                }, null);
            if (id != 0 && atenuaMezcla)
                efectosActivos++;
        }
        else
            id = AkUnitySoundEngine.PostEvent("Play_" + eventName, emitterId);
        if (id == 0)
        {
            if (sinSuperponer)
                efectosDeInteraccion.Remove(clave);
            Debug.LogError($"Wwise no pudo reproducir {eventName}.", this);
        }
        return id;
    }

    private void LateUpdate()
    {
        if (playerRoot == null || jugadorStarter != null)
            return;

        var posicion = playerRoot.position;
        var avance = Distance(posicion, ultimaPosicionPasos);
        ultimaPosicionPasos = posicion;
        var enSuelo = sueloJugador != null && sueloJugador.isGrounded;
        if (!enSuelo || avance < 0.02f || avance > 45f)
        {
            tiempoPasos = 0f;
            return;
        }

        tiempoPasos += Time.deltaTime;
        var corriendo = movimiento != null && movimiento.IsRunning;
        var intervalo = corriendo ? 0.32f : 0.46f;
        if (tiempoPasos < intervalo)
            return;
        tiempoPasos -= intervalo;
        AkUnitySoundEngine.PostEvent("Play_PasoJugador", player.gameObject);
    }

    private void ActualizarOclusion(Vector3 oyente)
    {
        var exterior = oyente.x > 5569.6f || oyente.x < 4894.2f ||
                       oyente.z < -423.8f || oyente.z > 838.2f;
        foreach (var punto in points)
        {
            var valor = punto == diningPoint && exterior ? 45f : 0f;
            var origen = punto.emitter.transform.position;
            var direccion = oyente - origen;
            var distancia = direccion.magnitude;
            if (distancia > 1f && distancia < 700f)
            {
                direccion /= distancia;
                var cantidad = Physics.RaycastNonAlloc(origen + direccion * 0.5f, direccion,
                    obstaculosAcusticos, distancia - 0.5f, paredesAcusticas, QueryTriggerInteraction.Ignore);
                for (var i = 0; i < cantidad; i++)
                {
                    var capa = obstaculosAcusticos[i].collider.gameObject.layer;
                    var material = capa == paredExterior ? 100f : capa == cristalAcustico ? 30f : 70f;
                    valor = Mathf.Max(valor, material);
                }
            }

            if (punto.oclusion == valor)
                continue;

            var duracion = punto.oclusion < 0f ? 0 : valor > punto.oclusion ? 650 : 950;
            AkUnitySoundEngine.SetRTPCValue("OclusionPared", valor, punto.emitter, duracion);
            if (punto == fryerPoint && layeredFryingEmitter != null)
                AkUnitySoundEngine.SetRTPCValue("OclusionPared", valor, layeredFryingEmitter, duracion);
            punto.oclusion = valor;
        }
    }

    private static bool IsZoneAmbience(string eventName) =>
        eventName == DiningAmbienceEvent || eventName == KitchenAmbienceEvent ||
        eventName == DriveThruAmbienceEvent;

    private static float Distance(Vector3 from, Vector3 to) =>
        Vector2.Distance(new Vector2(from.x, from.z), new Vector2(to.x, to.z));

    private static float CercaniaEfecto(Vector3 oyente, Vector3 emisor) =>
        Mathf.SmoothStep(1f, 0f, (Distance(oyente, emisor) - 22f) / 53f);

    private float DistanceGain(Point point, Vector3 listenerPosition)
    {
        if (point.audioEvent == DiningAmbienceEvent && zonaAmbienteComedor != null)
        {
            var profundidad = -zonaAmbienteComedor.DistanciaAlBorde(listenerPosition);
            return Mathf.SmoothStep(0f, 1f, profundidad / distanciaFundidoAmbienteComedor);
        }

        var distance = Distance(listenerPosition, point.position);
        if (point.audioEvent == FryerAmbienceEvent)
            return Mathf.SmoothStep(1f, 0f, (distance - 30f) / Mathf.Max(1f, point.radius - 30f));
        if (point.audioEvent == FryerDialogueEvent)
            return Mathf.Clamp01(1f - Mathf.Max(0f, distance - point.radius) /
                Mathf.Max(1f, point.radius * 0.65f));
        if (IsZoneAmbience(point.audioEvent))
            return Mathf.SmoothStep(1f, 0f, (distance - point.radius * 0.45f) /
                Mathf.Max(1f, point.radius * 1.25f));

        return Mathf.Clamp01(1f - distance / (Mathf.Max(point.radius, 0.01f) * 1.65f));
    }

#if UNITY_EDITOR
    public void PreviewPoint(int index, bool secondary)
    {
        if (!isActiveAndEnabled || index < 0 || index >= points.Length)
            return;

        if (!AkUnitySoundEngine.IsInitialized())
        {
            Debug.LogWarning("Activá Load Sound Engine in Edit Mode en los ajustes de Wwise o entrá en Play.", this);
            return;
        }

        var point = points[index];
        var eventName = secondary ? point.secondaryEvent : point.audioEvent;
        if (string.IsNullOrEmpty(eventName))
            return;

        StopPointPreview();
        if (!Application.isPlaying)
        {
            var view = UnityEditor.SceneView.lastActiveSceneView;
            if (view == null || view.camera == null || !AkSoundEngineController.Instance.EditorListenerIsInitialized())
            {
                Debug.LogWarning("Abrí la vista Scene para usar el punto de escucha de Wwise.", this);
                return;
            }

            var result = AkUnitySoundEngine.LoadBank(Bank, out _);
            if (result != AKRESULT.AK_Success && result != AKRESULT.AK_BankAlreadyLoaded)
            {
                Debug.LogError($"No se pudo cargar {Bank}.bnk para la prueba: {result}", this);
                return;
            }

            ownsPreviewBank = result == AKRESULT.AK_Success;
            previewEmitter = new GameObject("Prueba de emisor") { hideFlags = HideFlags.HideAndDontSave };
            previewEmitter.transform.position = point.position;
            if (AkUnitySoundEngine.RegisterGameObj(previewEmitter, previewEmitter.name) != AKRESULT.AK_Success)
            {
                Debug.LogError("Wwise no pudo registrar el emisor de prueba.", this);
                StopPointPreview();
                return;
            }

            previewPoint = point;
            UpdateEditPreview();
            WarnIfPreviewIsSilent(point, view.camera.transform.position);
            previewPlayingId = Post(eventName, previewEmitter);
            if (previewPlayingId == 0)
                StopPointPreview();
            return;
        }

        if (player == null)
            return;

        if (previewEmitter == null)
            previewEmitter = CreateEmitter("Prueba de emisor", point.position);
        else
        {
            previewEmitter.transform.position = point.position;
            AkUnitySoundEngine.SetObjectPosition(previewEmitter, previewEmitter.transform);
        }

        previewPoint = point;
        WarnIfPreviewIsSilent(point, player.transform.position);
        SetVolume(previewEmitter, point.volumeDb, DistanceGain(point, player.transform.position));
        previewPlayingId = Post(eventName, previewEmitter);
    }

    private void WarnIfPreviewIsSilent(Point point, Vector3 listenerPosition)
    {
        if (point.volumeDb <= -60f)
            Debug.LogWarning($"{point.label}: el volumen del punto está silenciado.", this);
        else if (DistanceGain(point, listenerPosition) <= 0f)
            Debug.LogWarning($"{point.label}: acercá la cámara al emisor para escuchar la prueba.", this);
    }

    public void StopPointPreview()
    {
        if (previewPlayingId != 0 && AkUnitySoundEngine.IsInitialized())
            AkUnitySoundEngine.StopPlayingID(previewPlayingId);
        previewPlayingId = 0;
        previewPoint = null;
        if (previewEmitter != null && !Application.isPlaying)
        {
            if (AkUnitySoundEngine.IsInitialized())
                AkUnitySoundEngine.UnregisterGameObj(previewEmitter);
            DestroyImmediate(previewEmitter);
            previewEmitter = null;
        }

        if (ownsPreviewBank && AkUnitySoundEngine.IsInitialized())
            AkUnitySoundEngine.UnloadBank(Bank, IntPtr.Zero);
        ownsPreviewBank = false;
    }

    public void UpdateEditPreview()
    {
        if (Application.isPlaying || previewPoint == null || previewEmitter == null ||
            !AkUnitySoundEngine.IsInitialized())
            return;

        var camera = UnityEditor.SceneView.lastActiveSceneView?.camera;
        var listener = AkInitializer.GetAkInitializerGameObject();
        if (camera == null || listener == null)
            return;

        previewEmitter.transform.position = previewPoint.position;
        AkUnitySoundEngine.SetObjectPosition(previewEmitter, previewEmitter.transform);
        SetVolume(previewEmitter, AkUnitySoundEngine.GetAkGameObjectID(listener),
            previewPoint.volumeDb, DistanceGain(previewPoint, camera.transform.position));
    }
#endif

    private static int IndiceRecorrido(string evento) => evento switch
    {
        DriveThruDialogueEvent => 0,
        FryerDialogueEvent => 1,
        "DialogoRadio" => 2,
        BathroomDialogueEvent => 3,
        "DialogoTelevision" => 4,
        FinalDialogueEvent => 5,
        _ => -1
    };

    private void MarcarExplorado(string evento)
    {
        var indice = IndiceRecorrido(evento);
        if (indice >= 0)
            lugaresExplorados[indice] = true;
    }

    private void DibujarAvisosPedido()
    {
        if (avisosPedido.Count == 0 || Event.current.type != EventType.Repaint)
            return;

        estiloAvisoPedido ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        estiloAvisoPedido.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.042f), 20, 36);
        var colorAnterior = GUI.color;
        foreach (var aviso in avisosPedido)
        {
            var progreso = Mathf.Clamp01((Time.time - aviso.inicio) / DuracionAvisoPedido);
            var posicion = aviso.origen + Vector3.up * Mathf.SmoothStep(6f, 25f, progreso);
            var pantalla = player.WorldToScreenPoint(posicion);
            if (pantalla.z <= 0f ||
                Physics.Linecast(player.transform.position, posicion, paredesAcusticas, QueryTriggerInteraction.Ignore))
                continue;

            GUI.color = new Color(0.4f, 1f, 0.45f, 1f - progreso * progreso);
            GUI.Label(new Rect(pantalla.x - 24f, Screen.height - pantalla.y - 24f, 48f, 48f),
                "$", estiloAvisoPedido);
        }
        GUI.color = colorAnterior;
    }

    private void DibujarRecorrido()
    {
        if (Event.current.type != EventType.Repaint)
            return;

        if (estiloRecorrido == null)
        {
            estiloRecorrido = CartelesInformativos.CrearTexto();
            estiloRecorrido.fontStyle = FontStyle.Bold;
            estiloRecorrido.padding = new RectOffset(12, 12, 4, 4);
        }
        if (estiloPuntoRecorrido == null)
        {
            estiloPuntoRecorrido = CartelesInformativos.CrearTexto();
            estiloPuntoRecorrido.fontStyle = FontStyle.Bold;
        }
        var ancho = Mathf.Min(820f, Screen.width - 24f);
        var anchoPunto = ancho / Lugares.Length;
        var izquierda = (Screen.width - ancho) * 0.5f;
        var rojo = new Color(0.76f, 0.02f, 0.025f, 0.98f);
        var amarillo = new Color(1f, 0.83f, 0.06f);
        var colorAnterior = GUI.color;
        GUI.color = Color.white;
        Forma(new Rect(izquierda, 12f, ancho, 82f), rojo, 28f);
        var primerPunto = izquierda + anchoPunto * 0.5f;
        Forma(new Rect(primerPunto, 53f, ancho - anchoPunto, 2f), amarillo);
        estiloRecorrido.fontSize = 17;
        CartelesInformativos.DibujarTexto(new Rect(izquierda, 16f, ancho, 24f),
            "Sitios que explorar hoy", estiloRecorrido, Color.white);
        estiloPuntoRecorrido.fontSize = Mathf.Clamp(Mathf.FloorToInt(anchoPunto / 11f), 8, 13);
        for (var i = 0; i < Lugares.Length; i++)
        {
            var x = primerPunto + anchoPunto * i;
            Forma(new Rect(x - 9f, 45f, 18f, 18f), amarillo, 9f);
            if (!lugaresExplorados[i])
                Forma(new Rect(x - 6.5f, 47.5f, 13f, 13f), rojo, 6.5f);
            CartelesInformativos.DibujarTexto(new Rect(x - anchoPunto * 0.5f, 65f, anchoPunto, 22f),
                Lugares[i], estiloPuntoRecorrido, Color.white);
        }
        GUI.color = colorAnterior;

        static void Forma(Rect rect, Color color, float radio = 0f)
            => GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, radio);
    }

    private void OnGUI()
    {
        if (player == null || !enabled)
            return;

        DibujarRecorrido();
        DibujarAvisosPedido();
        DibujarCartelVoz(entrancePoint);
        DibujarCartelVoz(puntoVozBano);
        GUI.Label(new Rect(Screen.width / 2f - 4f, Screen.height / 2f - 10f, 12f, 20f), "+");
        if (interaction == null)
            return;

        if (instructionStyle == null)
        {
            instructionStyle = CartelesInformativos.CrearTexto();
            instructionStyle.fontStyle = FontStyle.Bold;
            instructionStyle.wordWrap = true;
            instructionStyle.padding = new RectOffset(12, 12, 4, 4);
        }
        var fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.024f), 18, 34);
        instructionStyle.fontSize = fontSize;
        var panelHeight = fontSize * 2.6f;
        var panelWidth = Mathf.Min(Screen.width - 24f, fontSize * 23f);
        CartelesInformativos.Dibujar(new Rect((Screen.width - panelWidth) / 2f, Screen.height - panelHeight - 24f,
            panelWidth, panelHeight), interaction, instructionStyle);
    }

    private bool VozAccesible(Point punto) =>
        !Physics.Linecast(player.transform.position, punto.position, Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

    private void DibujarCartelVoz(Point punto)
    {
        if (punto == null || (punto == entrancePoint && !ladronAparecio) || !punto.inside || !VozAccesible(punto))
            return;

        var ancla = punto.position + Vector3.up * 20f;
        var pantalla = player.WorldToScreenPoint(ancla);
        if (pantalla.z <= 0f || pantalla.x < 90f || pantalla.x > Screen.width - 90f ||
            pantalla.y < 20f || pantalla.y > Screen.height - 20f ||
            Physics.Linecast(player.transform.position, ancla, paredesAcusticas, QueryTriggerInteraction.Ignore))
            return;

        if (estiloCartelVoz == null)
        {
            estiloCartelVoz = CartelesInformativos.CrearTexto();
            estiloCartelVoz.fontSize = 16;
            estiloCartelVoz.fontStyle = FontStyle.Bold;
        }
        var activo = efectosDeInteraccion.Contains((punto.audioEvent, 0UL));
        CartelesInformativos.Dibujar(new Rect(pantalla.x - 90f, Screen.height - pantalla.y - 16f, 180f, 32f),
            activo ? "En conversación" : "E · Hablar con " + punto.label, estiloCartelVoz);
    }

    private void OnDrawGizmosSelected()
    {
        if (points == null)
            return;
        foreach (var point in points)
        {
            if (point.audioEvent == DiningAmbienceEvent && zonaAmbienteComedor != null)
                continue;

            Gizmos.color = point.aimRadius > 0f ? Color.magenta :
                point.trigger == Trigger.Loop || point.trigger == Trigger.ManualLoop ? Color.cyan : Color.yellow;
            Gizmos.DrawWireSphere(point.position, point.aimRadius > 0f ? point.aimRadius : 12f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(point.position, point.label + " · " + point.audioEvent);
            if (point.audioEvent == KitchenAmbienceEvent || point.audioEvent == DiningAmbienceEvent)
            {
                using (new UnityEditor.Handles.DrawingScope(
                           point.audioEvent == KitchenAmbienceEvent ? Color.cyan : Color.green))
                    UnityEditor.Handles.DrawWireDisc(point.position, Vector3.up, point.radius);
            }
#endif
        }
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        StopPointPreview();
#endif
        Destroy(materialGuiaSuelo);
        Destroy(mallaGuiaSuelo);
        if (!AkUnitySoundEngine.IsInitialized())
            return;

        if (ambience != 0)
            AkUnitySoundEngine.StopPlayingID(ambience);
        if (musica != 0)
            AkUnitySoundEngine.StopPlayingID(musica);
        if (dialogue != 0)
            AkUnitySoundEngine.StopPlayingID(dialogue);
        if (driveThruReplay != 0)
            AkUnitySoundEngine.StopPlayingID(driveThruReplay);
        if (points != null)
        {
            foreach (var point in points)
                if (point.loop != 0)
                    AkUnitySoundEngine.StopPlayingID(point.loop);
        }
        if (ownsBank)
            AkUnitySoundEngine.UnloadBank(Bank, IntPtr.Zero);
    }
}
