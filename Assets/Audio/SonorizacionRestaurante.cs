using System;
using UnityEngine;

[RequireComponent(typeof(AkGameObj))]
public sealed class SonorizacionRestaurante : MonoBehaviour
{
    private const string Bank = "Sonorizacion";
    private const int DialogueCount = 4;
    private const string BathroomDialogueEvent = "DialogoBano";
    private const float HandWashRadius = 18f;
    private const float WaterStopDelay = 1.5f;
    private const string KitchenAmbienceEvent = "AmbienteCocina";
    private const string DiningAmbienceEvent = "AmbienteRestaurante";
    private const string FinalDialogueEvent = "DialogoLadron";
    private const string PassingCarEvent = "AutoPasando";
    private const string DriveThruDialogueEvent = "DialogoRecepcion";
    private const string FryerDialogueEvent = "DialogoPapas";
    private const string FryerAmbienceEvent = "Fritura";
    private const float CarPassDuration = 1.8f;
    private const float CarSoundDuration = 3.422f;
    private const float CarHalfRoute = 230f;
    private const float CarGroundHeight = 30.3f;
    private static readonly Vector3 CarLaneOffset = Vector3.left * 55f;

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

        [NonSerialized] public GameObject emitter;
        [NonSerialized] public bool inside;
        [NonSerialized] public uint loop;
        [NonSerialized] public uint fadingSinkSound;
        [NonSerialized] public float stopWaterAt;
        [NonSerialized] public Point handWashPoint;
        [NonSerialized] public bool nearHandWash;
        [NonSerialized] public bool faucetManuallyClosed;
        [NonSerialized] public float gain;
        [NonSerialized] public bool useAlternative;
        [NonSerialized] public int interactionCount;
    }

    [SerializeField] private Point[] points;
    [SerializeField, Range(-60f, 12f)] private float restaurantVolumeDb;
    [SerializeField] private ZonaMusical zonaAmbienteComedor;
    [SerializeField, Min(1f)] private float distanciaFundidoAmbienteComedor = 120f;
    [SerializeField, Range(-60f, 12f)] private float driveThruVolumeDb;
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
    private ulong listenerId;
    private GameObject driveThruEmitter;
    private GameObject layeredFryingEmitter;
    private bool ownsBank;
    private uint ambience;
    private uint dialogue;
    private bool fryerDialoguePlaying;
    private float fryerEffectsGain = 1f;
    private uint driveThruReplay;
    private uint entranceDialogue;
    private bool entranceDialoguePlayed;
    private bool bathroomDialoguePlayed;
    private int nextDialogue;
    private string interaction;
    private GUIStyle instructionStyle;
    private Point car;
    private float carStarted;
    private GameObject carVisual;
    private float kitchenMix;
    private Point diningPoint;
    [SerializeField] private ZonaMusical[] zonasMusicales;
    [SerializeField, Min(1f)] private float distanciaFundidoMusical = 90f;
    [SerializeField, Min(0.1f)] private float segundosFundidoMusical = 2.5f;
    [SerializeField] private Vector3 ladoRitmoComedor = new(5135f, 70f, -245f);
    [SerializeField] private Vector3 ladoMelodiaComedor = new(5550f, 70f, -245f);

    private static readonly string[] ZonasPorBloque =
    {
        "MusicaComedor", "MusicaEstacionamiento", "MusicaComedorVacio", "MusicaCocina"
    };

    private static readonly string[] ParametrosMusicales =
    {
        "ProximidadBloque1", "ProximidadBloque2", "ProximidadBloque3", "ProximidadBloque4"
    };

    private readonly float[] distanciasMusicales = new float[ZonasPorBloque.Length];
    private readonly float[] proximidadesEnviadas = { -1f, -1f, -1f, -1f };
    private GameObject emisorMusical;
    private uint musica;
    private float mezclaLateralEnviada = -1f;

    private void Start()
    {
        if (points == null || points.Length < DialogueCount)
        {
            Debug.LogError("La sonorización necesita los cuatro puntos de la secuencia de diálogo.", this);
            enabled = false;
            return;
        }

        player = Camera.main;
        if (player == null || !AkUnitySoundEngine.IsInitialized())
        {
            Debug.LogError("La sonorización necesita la cámara del jugador y Wwise activos.", this);
            enabled = false;
            return;
        }

        var result = AkUnitySoundEngine.LoadBank(Bank, out _);
        if (result != AKRESULT.AK_Success && result != AKRESULT.AK_BankAlreadyLoaded)
        {
            Debug.LogError($"No se pudo cargar {Bank}.bnk: {result}", this);
            enabled = false;
            return;
        }

        ownsBank = result == AKRESULT.AK_Success;
        listenerId = AkUnitySoundEngine.GetAkGameObjectID(player.gameObject);
        accionesDeCocina = GetComponent<AccionesDeCocina>();
        diningPoint = Array.Find(points, point => point.audioEvent == DiningAmbienceEvent);
        var playerBody = player.GetComponentInParent<Rigidbody>();
        playerRoot = playerBody != null ? playerBody.transform : player.transform;
        foreach (var point in points)
        {
            point.emitter = CreateEmitter(point.label + " · " + point.audioEvent, point.position);
            if (point.audioEvent == PassingCarEvent && autoDriveThruPrefab != null)
            {
                carVisual = Instantiate(autoDriveThruPrefab, point.emitter.transform);
                carVisual.transform.localPosition = Vector3.up * (CarGroundHeight - point.position.y);
                carVisual.SetActive(false);
            }
            point.gain = DistanceGain(point, player.transform.position);
            SetVolume(point.emitter, point.volumeDb, point.gain);
            if (point.trigger == Trigger.ManualLoop && point.sinkNumber > 0)
                point.handWashPoint = Array.Find(points, candidate =>
                    candidate.sinkNumber == point.sinkNumber && candidate.audioEvent == "LavadoManos");
        }

        driveThruEmitter = CreateEmitter("Aviso de drive thru", transform.position);
        layeredFryingEmitter = CreateEmitter("Fritura en capas", points[1].position);
        if (zonasMusicales != null && zonasMusicales.Length > 0)
        {
            emisorMusical = CreateEmitter("Musica interactiva", transform.position);
            ActualizarMusica(player.transform.position);
            musica = Post("MusicaInteractiva", emisorMusical);
        }
        UpdateExtraVolumes();
        if (diningPoint == null)
            ambience = Post(DiningAmbienceEvent, gameObject);
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
        var diningDuck = Mathf.Lerp(1f, 0.2f, kitchenMix);
        if (diningPoint == null)
            SetVolume(gameObject, restaurantVolumeDb, diningDuck);
        else
            SetVolume(diningPoint.emitter, diningPoint.volumeDb, diningPoint.gain * diningDuck);
        SetVolume(driveThruEmitter, driveThruVolumeDb);
        SetVolume(layeredFryingEmitter, layeredFryingVolumeDb, points[1].gain * fryerEffectsGain);
    }

    private void Update()
    {
        var playerPosition = player.transform.position;
        ActualizarMusica(playerPosition);
        Point selected = null;
        var nearest = float.PositiveInfinity;
        interaction = null;
        var aim = player.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        var targetKitchenMix = 0f;
        fryerEffectsGain = Mathf.MoveTowards(fryerEffectsGain,
            fryerDialoguePlaying ? 0.25f : 1f, Time.deltaTime * 4f);

        for (var index = 0; index < points.Length; index++)
        {
            var point = points[index];
            var distance = Distance(playerPosition, point.position);
            var targetGain = DistanceGain(point, playerPosition);
            var usesDiningZone = point == diningPoint && zonaAmbienteComedor != null;
            var inside = usesDiningZone ? targetGain > 0f : distance <= point.radius;
            var entered = inside && !point.inside;
            var exited = !inside && point.inside;
            point.inside = inside;

            var isEntranceDialogue = point.audioEvent == FinalDialogueEvent;
            var startEntranceDialogue = isEntranceDialogue && entered && !entranceDialoguePlayed;
            point.gain = Mathf.MoveTowards(point.gain, targetGain, Time.deltaTime * 2f);
            if (point != diningPoint)
                SetVolume(point.emitter, point.volumeDb, point.gain *
                    (point.audioEvent == FryerAmbienceEvent ? fryerEffectsGain : 1f));
            if (point.audioEvent == KitchenAmbienceEvent)
                targetKitchenMix = Mathf.Max(targetKitchenMix, point.gain);

            if (isEntranceDialogue)
            {
                if (startEntranceDialogue)
                    StartEntranceDialogue(point);
                continue;
            }

            if (point.audioEvent == BathroomDialogueEvent)
            {
                if (inside && !bathroomDialoguePlayed)
                {
                    SetVolume(player.gameObject, point.volumeDb);
                    bathroomDialoguePlayed = Post(point.audioEvent, player.gameObject) != 0;
                }
                continue;
            }

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
                    else if (point.loop != 0 && (usesDiningZone ? point.gain <= 0f : exited))
                    {
                        AkUnitySoundEngine.StopPlayingID(point.loop, 650);
                        point.loop = 0;
                    }
                    break;
                case Trigger.Enter:
                    if (entered && car == null)
                    {
                        car = point;
                        carStarted = Time.unscaledTime;
                        point.emitter.transform.position = point.position + CarLaneOffset -
                            Vector3.forward * CarHalfRoute;
                        AkUnitySoundEngine.SetObjectPosition(point.emitter, point.emitter.transform);
                        if (point.audioEvent == PassingCarEvent && carVisual != null)
                            carVisual.SetActive(true);
                        Post(point.audioEvent, point.emitter);
                        Post(point.secondaryEvent, point.emitter);
                        SetVolume(driveThruEmitter, driveThruVolumeDb);
                        Post("NotificacionDriveThru", driveThruEmitter);
                    }
                    break;
                case Trigger.Door:
                    if (entered)
                        Post(point.audioEvent, point.emitter);
                    else if (exited)
                        Post(point.secondaryEvent, point.emitter);
                    break;
                case Trigger.Use:
                case Trigger.UseRightClick:
                    if (!inside || !CanInteract(point, aim, out var aimDistance))
                        break;

                    var aimed = point.aimRadius > 0f;
                    var selectedIsAimed = selected != null && selected.aimRadius > 0f;
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
                Vector3.forward * Mathf.LerpUnclamped(-CarHalfRoute, CarHalfRoute,
                    elapsed / CarPassDuration);
            AkUnitySoundEngine.SetObjectPosition(car.emitter, car.emitter.transform);
            if (elapsed >= CarPassDuration && car.audioEvent == PassingCarEvent &&
                carVisual != null && carVisual.activeSelf)
                carVisual.SetActive(false);
            if (elapsed >= CarSoundDuration)
                car = null;
        }

        kitchenMix = targetKitchenMix;
        UpdateExtraVolumes();
#if UNITY_EDITOR
        if (previewPoint != null)
            SetVolume(previewEmitter, previewPoint.volumeDb, DistanceGain(previewPoint, playerPosition));
#endif
        interaction = accionesDeCocina?.ActualizarInteraccion(player, aim);
        if (interaction != null || selected == null)
            return;

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
            interaction = "E o clic derecho · " + selected.label;
            if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(1))
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

        ActualizarCapasComedor(posicion);
        Array.Fill(distanciasMusicales, float.PositiveInfinity);
        var menorDistancia = float.PositiveInfinity;
        foreach (var zona in zonasMusicales)
        {
            if (zona == null)
                continue;

            var bloque = Array.IndexOf(ZonasPorBloque, zona.Evento);
            if (bloque < 0)
                continue;

            var distancia = zona.DistanciaAlBorde(posicion);
            distanciasMusicales[bloque] = Mathf.Min(distanciasMusicales[bloque], distancia);
            menorDistancia = Mathf.Min(menorDistancia, distancia);
        }

        var alcance = Mathf.Max(1f, distanciaFundidoMusical);
        var rampa = Mathf.RoundToInt(segundosFundidoMusical * 1000f);
        var sumaCercanias = 0f;
        for (var bloque = 0; bloque < distanciasMusicales.Length; bloque++)
        {
            var distancia = distanciasMusicales[bloque];
            var cercania = float.IsPositiveInfinity(distancia) ? 0f :
                Mathf.Clamp01(1f - (distancia - menorDistancia) / alcance);
            distanciasMusicales[bloque] = cercania;
            sumaCercanias += cercania;
        }

        var cobertura = Mathf.Clamp01(1f - Mathf.Max(0f, menorDistancia) / (2f * alcance));
        var escalaProximidad = 100f * cobertura / Mathf.Max(1f, sumaCercanias);
        for (var bloque = 0; bloque < distanciasMusicales.Length; bloque++)
        {
            var proximidad = distanciasMusicales[bloque] * escalaProximidad;

            if (Mathf.Abs(proximidadesEnviadas[bloque] - proximidad) < 0.05f)
                continue;

            var duracion = proximidadesEnviadas[bloque] < 0f ? 0 : rampa;
            AkUnitySoundEngine.SetRTPCValue(ParametrosMusicales[bloque], proximidad, emisorMusical, duracion);
            proximidadesEnviadas[bloque] = proximidad;
        }
    }

    private void ActualizarCapasComedor(Vector3 posicion)
    {
        var origen = new Vector2(ladoRitmoComedor.x, ladoRitmoComedor.z);
        var eje = new Vector2(ladoMelodiaComedor.x, ladoMelodiaComedor.z) - origen;
        var desplazamiento = new Vector2(posicion.x, posicion.z) - origen;
        var mezcla = eje.sqrMagnitude > 0.01f
            ? Mathf.Clamp01(Vector2.Dot(desplazamiento, eje) / eje.sqrMagnitude)
            : 0.5f;
        if (Mathf.Abs(mezcla - mezclaLateralEnviada) < 0.002f)
            return;

        var duracion = mezclaLateralEnviada < 0f ? 0 : 200;
        var melodia = Mathf.SmoothStep(0f, 100f, mezcla / 0.4f);
        var ritmo = Mathf.SmoothStep(0f, 100f, (1f - mezcla) / 0.4f);
        AkUnitySoundEngine.SetRTPCValue("MezclaMelodiaComedor", melodia, emisorMusical, duracion);
        AkUnitySoundEngine.SetRTPCValue("MezclaRitmoComedor", ritmo, emisorMusical, duracion);
        mezclaLateralEnviada = mezcla;
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
        if (advancesSequence || selected.audioEvent == FryerDialogueEvent)
        {
            if (dialogue == 0 && driveThruReplay == 0)
                StartDialogue(selected, advancesSequence);
        }
        else if (selected.audioEvent == DriveThruDialogueEvent)
        {
            if (dialogue != 0 || driveThruReplay != 0)
                return;

            Post(selected.secondaryEvent, selected.emitter);
            driveThruReplay = AkUnitySoundEngine.PostEvent("Play_" + selected.audioEvent,
                AkUnitySoundEngine.GetAkGameObjectID(selected.emitter), (uint)AkCallbackType.AK_EndOfEvent,
                (_, type, info) =>
                {
                    if (type == AkCallbackType.AK_EndOfEvent &&
                        info is AkEventCallbackInfo evt && evt.playingID == driveThruReplay)
                        driveThruReplay = 0;
                }, null);
        }
        else if (selected.secondaryEventEvery > 0 && !string.IsNullOrEmpty(selected.secondaryEvent))
        {
            Post(selected.audioEvent, selected.emitter);
            selected.interactionCount++;
            if (selected.interactionCount >= selected.secondaryEventEvery)
            {
                selected.interactionCount = 0;
                Post(selected.secondaryEvent, selected.emitter);
            }
        }
        else
        {
            var alternate = !string.IsNullOrEmpty(selected.secondaryEvent);
            Post(alternate && selected.useAlternative ? selected.secondaryEvent : selected.audioEvent,
                selected.emitter);
            selected.useAlternative = !selected.useAlternative;
        }
    }

    private void StartDialogue(Point point, bool advancesSequence = true)
    {
        var isFryerDialogue = point.audioEvent == FryerDialogueEvent;
        if (!string.IsNullOrEmpty(point.secondaryEvent))
            Post(point.secondaryEvent, point.emitter);

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
            fryerEffectsGain = 0.25f;
            UpdateExtraVolumes();
            Post("FrituraCapas", layeredFryingEmitter);
        }
    }

    private void StartEntranceDialogue(Point point)
    {
        SetVolume(player.gameObject, point.volumeDb);
        entranceDialogue = AkUnitySoundEngine.PostEvent("Play_" + point.audioEvent,
            listenerId, (uint)AkCallbackType.AK_EndOfEvent,
            (_, type, info) =>
            {
                if (type == AkCallbackType.AK_EndOfEvent &&
                    info is AkEventCallbackInfo evt && evt.playingID == entranceDialogue)
                    entranceDialogue = 0;
            }, null);

        if (entranceDialogue == 0)
            Debug.LogError($"Wwise no pudo reproducir {point.audioEvent}.", this);
        else
            entranceDialoguePlayed = true;
    }

    private uint Post(string eventName, GameObject emitter)
    {
        var id = AkUnitySoundEngine.PostEvent("Play_" + eventName,
            AkUnitySoundEngine.GetAkGameObjectID(emitter));
        if (id == 0)
            Debug.LogError($"Wwise no pudo reproducir {eventName}.", this);
        return id;
    }

    private static float Distance(Vector3 from, Vector3 to) =>
        Vector2.Distance(new Vector2(from.x, from.z), new Vector2(to.x, to.z));

    private float DistanceGain(Point point, Vector3 listenerPosition)
    {
        if (point.audioEvent == DiningAmbienceEvent && zonaAmbienteComedor != null)
        {
            var distanceOutside = Mathf.Max(0f, zonaAmbienteComedor.DistanciaAlBorde(listenerPosition));
            var progress = distanceOutside / Mathf.Max(1f, distanciaFundidoAmbienteComedor);
            return Mathf.SmoothStep(1f, 0f, progress);
        }

        var distance = Distance(listenerPosition, point.position);
        if (point.audioEvent == FryerDialogueEvent)
            return Mathf.Clamp01(1f - Mathf.Max(0f, distance - point.radius) /
                Mathf.Max(1f, point.radius * 0.65f));
        if (point.audioEvent == KitchenAmbienceEvent || point.audioEvent == DiningAmbienceEvent)
        {
            var blendWidth = Mathf.Max(1f, point.radius * 0.4f);
            return Mathf.SmoothStep(0f, 1f, (point.radius - distance) / blendWidth);
        }

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

    private void OnGUI()
    {
        if (player == null || !enabled)
            return;

        GUI.Label(new Rect(Screen.width / 2f - 4f, Screen.height / 2f - 10f, 12f, 20f), "+");
        if (interaction == null)
            return;

        instructionStyle ??= new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            padding = new RectOffset(12, 12, 4, 4)
        };
        var fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.024f), 18, 34);
        instructionStyle.fontSize = fontSize;
        var panelHeight = fontSize * 2.6f;
        var panelWidth = Mathf.Min(Screen.width - 24f, fontSize * 23f);
        GUI.Box(new Rect((Screen.width - panelWidth) / 2f, Screen.height - panelHeight - 24f,
            panelWidth, panelHeight), interaction, instructionStyle);
    }

    private void OnDrawGizmosSelected()
    {
        if (zonaAmbienteComedor != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(ladoRitmoComedor, ladoMelodiaComedor);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(ladoRitmoComedor, "Bajo y percusión");
            UnityEditor.Handles.Label(ladoMelodiaComedor, "Melodía");
#endif
        }

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
        if (entranceDialogue != 0)
            AkUnitySoundEngine.StopPlayingID(entranceDialogue);
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
