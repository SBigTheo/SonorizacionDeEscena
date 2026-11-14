using UnityEditor;
using UnityEngine;

public sealed class PruebaSonorizacion : EditorWindow
{
    private const string BankName = "Sonorizacion";
    private const int DialogueCount = 6;
    private static readonly string[] Events =
    {
        "DialogoRecepcion",
        "DialogoPapas",
        "DialogoRadio",
        "DialogoTelevision",
        "DialogoBano",
        "DialogoLadron",
        "Caida",
        "SonidosDeRatita",
        "TachoDeBasura",
        "CorteCapas",
        "FrituraCapas",
        "AmbienteDriveThru",
        "AmbienteRestaurante",
        "AmbienteCocina",
        "AutoPasando",
        "AutoBocina",
        "BotonFisico",
        "SwitchFisico",
        "CorteVegetales",
        "Cronometro",
        "LavadoManos",
        "AguaLavabo",
        "NotificacionPedido",
        "NotificacionDriveThru",
        "PuertaAbrir",
        "PuertaCerrar",
        "Fritura",
        "CadenaBano",
    };

    private GameObject emitter;
    private bool ownsBank;
    private uint playingId;
    private int selection;
    private string status = "Entrá en Play para iniciar la prueba.";

    [MenuItem("Tools/Sonorización/Probar audios Wwise")]
    private static void Open() => GetWindow<PruebaSonorizacion>("Prueba de audio");

    private void OnEnable() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        Release();
    }

    private void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingPlayMode)
            Release();

        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(status, MessageType.Info);

        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
        {
            selection = EditorGUILayout.Popup("Evento", selection, Events);

            if (GUILayout.Button("Reproducir evento"))
                Play(selection, false);
            if (GUILayout.Button("Reproducir los seis diálogos en orden"))
                Play(0, true);
            if (GUILayout.Button($"Verificar los {Events.Length} eventos"))
                VerifyEvents();
            if (GUILayout.Button("Detener"))
                Stop();
        }
    }

    private bool Prepare()
    {
        if (!AkUnitySoundEngine.IsInitialized() || Camera.main == null)
        {
            status = "Wwise y la cámara del jugador deben estar activos.";
            return false;
        }

        if (emitter != null)
            return true;

        var result = AkUnitySoundEngine.LoadBank(BankName, out _);
        if (result != AKRESULT.AK_Success && result != AKRESULT.AK_BankAlreadyLoaded)
        {
            status = $"No se pudo cargar {BankName}.bnk: {result}";
            Debug.LogError(status);
            return false;
        }

        ownsBank = result == AKRESULT.AK_Success;
        emitter = new GameObject("Emisor temporal de prueba", typeof(AkGameObj));
        emitter.hideFlags = HideFlags.DontSave;
        return true;
    }

    private void Play(int index, bool sequence)
    {
        Stop();
        if (!Prepare())
            return;

        var cameraTransform = Camera.main.transform;
        emitter.transform.position = cameraTransform.position + cameraTransform.forward * 2f;
        AkUnitySoundEngine.SetObjectPosition(emitter, emitter.transform);
        var objectId = AkUnitySoundEngine.GetAkGameObjectID(emitter);
        var eventName = "Play_" + Events[index];
        playingId = AkUnitySoundEngine.PostEvent(eventName, objectId, (uint)AkCallbackType.AK_EndOfEvent,
            (_, callbackType, info) =>
            {
                if (callbackType != AkCallbackType.AK_EndOfEvent ||
                    !(info is AkEventCallbackInfo eventInfo) || eventInfo.playingID != playingId)
                    return;

                playingId = 0;
                if (sequence && index + 1 < DialogueCount)
                    Play(index + 1, true);
                else
                    status = "Reproducción terminada.";
                Repaint();
            }, null);

        status = playingId == 0 ? $"Wwise rechazó {eventName}." : $"Reproduciendo {eventName}.";
        if (playingId == 0)
            Debug.LogError(status);
    }

    private void VerifyEvents()
    {
        Stop();
        if (!Prepare())
            return;

        var objectId = AkUnitySoundEngine.GetAkGameObjectID(emitter);
        var accepted = 0;
        foreach (var name in Events)
        {
            var id = AkUnitySoundEngine.PostEvent("Play_" + name, objectId);
            if (id == 0)
            {
                Debug.LogError($"Wwise rechazó Play_{name}.");
                continue;
            }

            accepted++;
            AkUnitySoundEngine.StopPlayingID(id);
        }

        status = $"Wwise aceptó {accepted}/{Events.Length} eventos.";
        Debug.Log(status);
    }

    private void Stop()
    {
        if (playingId != 0 && AkUnitySoundEngine.IsInitialized())
            AkUnitySoundEngine.StopPlayingID(playingId, 80);

        playingId = 0;
        status = "Prueba detenida.";
    }

    private void Release()
    {
        Stop();
        if (emitter != null)
            DestroyImmediate(emitter);
        if (ownsBank && AkUnitySoundEngine.IsInitialized())
            AkUnitySoundEngine.UnloadBank(BankName, System.IntPtr.Zero);

        ownsBank = false;
    }
}
