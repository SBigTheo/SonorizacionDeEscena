using System;
using UnityEngine;

[RequireComponent(typeof(AkGameObj))]
public sealed class SonorizacionRestaurante : MonoBehaviour
{
    private const string Bank = "Sonorizacion";
    private const int DialogueCount = 5;
    private const float WaterStopDelay = 1.5f;
    private const string KitchenAmbienceEvent = "AmbienteCocina";
    private const string DiningAmbienceEvent = "AmbienteRestaurante";
    private const string FinalDialogueEvent = "DialogoLadron";

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
        [InspectorName("Radio de puntería"), Min(0f)]
        public float aimRadius;
        [InspectorName("Número de lavamanos"), Min(0)]
        public int sinkNumber;

        [NonSerialized] public GameObject emitter;
        [NonSerialized] public bool inside;
        [NonSerialized] public uint loop;
        [NonSerialized] public float stopWaterAt;
        [NonSerialized] public float gain;
        [NonSerialized] public bool useAlternative;
    }

    [SerializeField] private Point[] points;
    [SerializeField, Range(-60f, 12f)] private float restaurantVolumeDb;
    [SerializeField, Range(-60f, 12f)] private float orderVolumeDb;
    [SerializeField, Range(-60f, 12f)] private float driveThruVolumeDb;
    [SerializeField, Range(-60f, 12f)] private float layeredFryingVolumeDb;
    private Camera player;
#if UNITY_EDITOR
    private GameObject previewEmitter;
    private uint previewPlayingId;
    private Point previewPoint;
    private bool ownsPreviewBank;
#endif
    private Transform playerRoot;
    private ulong listenerId;
    private GameObject orderEmitter;
    private GameObject driveThruEmitter;
    private GameObject layeredFryingEmitter;
    private bool ownsBank;
    private uint ambience;
    private uint dialogue;
    private uint entranceDialogue;
    private bool entranceDialoguePlayed;
    private int nextDialogue;
    private string interaction;
    private GUIStyle instructionStyle;
    private Point car;
    private float carStarted;
    private float kitchenMix;
    private Point diningPoint;

    private void Start()
    {
        if (points == null || points.Length < DialogueCount)
        {
            Debug.LogError("La sonorización necesita los cinco puntos de la secuencia de diálogo.", this);
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
        diningPoint = Array.Find(points, point => point.audioEvent == DiningAmbienceEvent);
        var playerBody = player.GetComponentInParent<Rigidbody>();
        playerRoot = playerBody != null ? playerBody.transform : player.transform;
        foreach (var point in points)
        {
            point.emitter = CreateEmitter(point.label + " · " + point.audioEvent, point.position);
            point.gain = DistanceGain(point, player.transform.position);
            SetVolume(point.emitter, point.volumeDb, point.gain);
        }

        orderEmitter = CreateEmitter("Aviso de nuevo pedido", transform.position);
        driveThruEmitter = CreateEmitter("Aviso de drive thru", transform.position);
        layeredFryingEmitter = CreateEmitter("Fritura en capas", points[1].position);
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
        SetVolume(orderEmitter, orderVolumeDb);
        SetVolume(driveThruEmitter, driveThruVolumeDb);
        SetVolume(layeredFryingEmitter, layeredFryingVolumeDb, points[1].gain);
    }

    private void Update()
    {
        var playerPosition = player.transform.position;
        Point selected = null;
        var nearest = float.PositiveInfinity;
        interaction = null;
        var aim = player.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        var targetKitchenMix = 0f;

        for (var index = 0; index < points.Length; index++)
        {
            var point = points[index];
            var distance = Distance(playerPosition, point.position);
            var inside = distance <= point.radius;
            var entered = inside && !point.inside;
            var exited = !inside && point.inside;
            point.inside = inside;

            var isEntranceDialogue = point.audioEvent == FinalDialogueEvent;
            var startEntranceDialogue = isEntranceDialogue && entered && !entranceDialoguePlayed;
            var targetGain = DistanceGain(point, playerPosition);
            point.gain = Mathf.MoveTowards(point.gain, targetGain, Time.deltaTime * 2f);
            if (point != diningPoint)
                SetVolume(point.emitter, point.volumeDb, point.gain);
            if (point.audioEvent == KitchenAmbienceEvent)
                targetKitchenMix = Mathf.Max(targetKitchenMix, point.gain);

            if (isEntranceDialogue)
            {
                if (startEntranceDialogue)
                    StartEntranceDialogue(point);
                continue;
            }

            if (index < DialogueCount && (index == 0 || point.trigger != Trigger.UseRightClick))
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
                    if (point.stopWaterAt > 0f && Time.time >= point.stopWaterAt)
                    {
                        AkUnitySoundEngine.StopPlayingID(point.loop);
                        point.loop = 0;
                        point.stopWaterAt = 0f;
                    }
                    break;
                case Trigger.Loop:
                    if (inside && point.loop == 0)
                        point.loop = Post(point.audioEvent, point.emitter);
                    else if (exited && point.loop != 0)
                    {
                        AkUnitySoundEngine.StopPlayingID(point.loop, 650);
                        point.loop = 0;
                    }
                    break;
                case Trigger.Enter:
                    if (entered)
                    {
                        car = point;
                        carStarted = Time.time;
                        point.emitter.transform.position = point.position - Vector3.forward * 100f;
                        AkUnitySoundEngine.SetObjectPosition(point.emitter, point.emitter.transform);
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
            var progress = Mathf.Clamp01((Time.time - carStarted) / 5f);
            car.emitter.transform.position = car.position + Vector3.forward * Mathf.Lerp(-100f, 100f, progress);
            if (progress >= 1f)
                car = null;
        }

        kitchenMix = targetKitchenMix;
        UpdateExtraVolumes();
#if UNITY_EDITOR
        if (previewPoint != null)
            SetVolume(previewEmitter, previewPoint.volumeDb, DistanceGain(previewPoint, playerPosition));
#endif
        if (selected == null)
            return;

        if (selected.sinkNumber > 0)
        {
            var faucet = Array.Find(points,
                point => point.trigger == Trigger.ManualLoop && point.sinkNumber == selected.sinkNumber);
            var action = faucet == null ? "Sin canilla vinculada" :
                faucet.stopWaterAt > 0f ? "Cancelar cierre del agua" :
                faucet.loop == 0 ? "Abrir agua" : "Cerrar agua";
            interaction = "E o clic derecho · " + action + " · " + selected.label;
            if (faucet != null && (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(1)))
                ToggleWater(faucet);
            if (Input.GetMouseButtonDown(0))
                Interact(selected);
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

    private void ToggleWater(Point faucet)
    {
        if (faucet.stopWaterAt > 0f)
            faucet.stopWaterAt = 0f;
        else if (faucet.loop != 0)
            faucet.stopWaterAt = Time.time + WaterStopDelay;
        else
            faucet.loop = Post(faucet.audioEvent, faucet.emitter);
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
        if (nextDialogue < DialogueCount && selected == points[nextDialogue])
        {
            if (dialogue == 0)
                StartDialogue(selected);
        }
        else
        {
            var alternate = !string.IsNullOrEmpty(selected.secondaryEvent);
            Post(alternate && selected.useAlternative ? selected.secondaryEvent : selected.audioEvent,
                selected.emitter);
            selected.useAlternative = !selected.useAlternative;
        }
    }

    private void StartDialogue(Point point)
    {
        if (!string.IsNullOrEmpty(point.secondaryEvent))
            Post(point.secondaryEvent, point.emitter);
        if (nextDialogue == 1)
        {
            UpdateExtraVolumes();
            Post("NotificacionPedido", orderEmitter);
            Post("FrituraCapas", layeredFryingEmitter);
        }

        dialogue = AkUnitySoundEngine.PostEvent("Play_" + point.audioEvent,
            AkUnitySoundEngine.GetAkGameObjectID(point.emitter), (uint)AkCallbackType.AK_EndOfEvent,
            (_, type, info) =>
            {
                if (type != AkCallbackType.AK_EndOfEvent ||
                    !(info is AkEventCallbackInfo evt) || evt.playingID != dialogue)
                    return;

                dialogue = 0;
                nextDialogue++;
            }, null);

        if (dialogue == 0)
            Debug.LogError($"Wwise no pudo reproducir {point.audioEvent}.", this);
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

    private static float DistanceGain(Point point, Vector3 listenerPosition)
    {
        var distance = Distance(listenerPosition, point.position);
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
        if (points == null)
            return;
        foreach (var point in points)
        {
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
        if (dialogue != 0)
            AkUnitySoundEngine.StopPlayingID(dialogue);
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
