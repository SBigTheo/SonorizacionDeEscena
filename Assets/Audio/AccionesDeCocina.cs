using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SonorizacionRestaurante))]
public sealed class AccionesDeCocina : MonoBehaviour
{
    [SerializeField] private Vector3 tabla;
    [SerializeField] private Vector3 tacho;
    [SerializeField] private Vector3 salidaRata;
    [SerializeField, Min(1f)] private float radioEncuentro = 65f;

    private Rigidbody carne;
    private Rigidbody cuchillo;
    private Collider objetivoCarne;
    private Collider objetivoTabla;
    private Transform cuerpoTacho;
    private Transform tapa;
    private GameObject rata;
    private GameObject emisorTacho;
    private SonorizacionRestaurante sonorizacion;
    private bool carneSuelta;
    private bool cortando;
    private bool encuentroActivado;

    private void Start()
    {
        sonorizacion = GetComponent<SonorizacionRestaurante>();
        var madera = MaterialDe(new Color(0.55f, 0.29f, 0.12f));
        var acero = MaterialDe(new Color(0.63f, 0.66f, 0.68f));
        var oscuro = MaterialDe(new Color(0.15f, 0.17f, 0.18f));
        var rojo = MaterialDe(new Color(0.53f, 0.11f, 0.12f));
        var gris = MaterialDe(new Color(0.29f, 0.25f, 0.23f));
        objetivoTabla = Pieza(PrimitiveType.Cube, "Tabla de picar", tabla, new Vector3(32, 2, 23), madera, true).GetComponent<Collider>();
        var hoja = Pieza(PrimitiveType.Cube, "Cuchillo", tabla + new Vector3(-8, 1.8f, 3),
            new Vector3(3.5f, 0.8f, 12f), acero, true);
        var mango = Pieza(PrimitiveType.Cube, "Mango del cuchillo", tabla + new Vector3(-8, 2f, -7f),
            new Vector3(2.6f, 1.5f, 8f), oscuro, true);
        mango.transform.SetParent(hoja.transform, true);
        cuchillo = PrepararCaida(hoja, true);
        var aceite = MaterialDe(new Color(0.48f, 0.29f, 0.055f));
        aceite.SetFloat("_Smoothness", 0.91f);
        Pieza(PrimitiveType.Cube, "Aceite de la freidora · primera cuba",
            new Vector3(4917.3f, 72.8f, 338.5f), new Vector3(12f, 0.35f, 14f), aceite);
        Pieza(PrimitiveType.Cube, "Aceite de la freidora · segunda cuba",
            new Vector3(4917.3f, 72.8f, 355.5f), new Vector3(12f, 0.35f, 14f), aceite);
        hoja.AddComponent<AkGameObj>();
        Pieza(PrimitiveType.Cube, "Pinzas", tabla + new Vector3(-2, 2, 0), new Vector3(2, 1, 15), acero);

        var trozo = Pieza(PrimitiveType.Sphere, "Carne", tabla + new Vector3(8, 3.6f, 6),
            new Vector3(13, 5, 9), rojo, true);
        rojo.SetFloat("_Smoothness", 0.38f);
        var grasa = MaterialDe(new Color(0.91f, 0.65f, 0.56f));
        var borde = Pieza(PrimitiveType.Sphere, "Borde de grasa", trozo.transform.position + new Vector3(3.6f, 0.9f, 0),
            new Vector3(5, 2.8f, 7.6f), grasa);
        borde.transform.SetParent(trozo.transform);
        for (var veta = 0; veta < 3; veta++)
        {
            var marmoleo = Pieza(PrimitiveType.Sphere, "Veta de grasa",
                trozo.transform.position + new Vector3(veta * 1.6f - 2.5f, 2.05f, (veta - 1) * 1.1f),
                new Vector3(0.48f, 0.55f, 3.2f), grasa);
            marmoleo.transform.rotation = Quaternion.Euler(0, 28 + veta * 15, 0);
            marmoleo.transform.SetParent(trozo.transform);
        }
        objetivoCarne = trozo.GetComponent<Collider>();
        carne = PrepararCaida(trozo, false);

        emisorTacho = new GameObject("Tacho de basura", typeof(AkGameObj));
        emisorTacho.transform.SetParent(transform);
        emisorTacho.transform.position = tacho;
        cuerpoTacho = Pieza(PrimitiveType.Cylinder, "Cuerpo del tacho", tacho + Vector3.up * 14,
            new Vector3(23, 14, 23), oscuro).transform;
        tapa = Pieza(PrimitiveType.Cylinder, "Tapa del tacho", tacho + Vector3.up * 29,
            new Vector3(25, 1.5f, 25), acero).transform;

        rata = new GameObject("Rata", typeof(AkGameObj));
        rata.transform.SetParent(transform);
        rata.transform.position = tacho;
        var cuerpo = Pieza(PrimitiveType.Sphere, "Cuerpo", tacho + Vector3.up * 3, new Vector3(6, 5, 12), gris);
        cuerpo.transform.SetParent(rata.transform);
        Pieza(PrimitiveType.Sphere, "Cabeza", tacho + new Vector3(0, 3, 6), new Vector3(5, 4, 5), gris).transform.SetParent(rata.transform);
        Pieza(PrimitiveType.Sphere, "Oreja izquierda", tacho + new Vector3(-2, 5, 6), new Vector3(2, 2, 2), madera).transform.SetParent(rata.transform);
        Pieza(PrimitiveType.Sphere, "Oreja derecha", tacho + new Vector3(2, 5, 6), new Vector3(2, 2, 2), madera).transform.SetParent(rata.transform);
        Pieza(PrimitiveType.Cube, "Cola", tacho + new Vector3(0, 1, -8), new Vector3(1, 1, 8), madera).transform.SetParent(rata.transform);
        Pieza(PrimitiveType.Sphere, "Hocico", tacho + new Vector3(0, 2.8f, 8.5f),
            new Vector3(1.5f, 1.2f, 1.5f), oscuro).transform.SetParent(rata.transform);
        for (var lado = -1; lado <= 1; lado += 2)
            Pieza(PrimitiveType.Sphere, "Ojo", tacho + new Vector3(lado * 1.65f, 4f, 7.4f),
                Vector3.one * 0.85f, oscuro).transform.SetParent(rata.transform);
        rata.SetActive(false);
    }

    private Rigidbody PrepararCaida(GameObject objeto, bool reproducirSonido)
    {
        var cuerpo = objeto.AddComponent<Rigidbody>();
        cuerpo.isKinematic = true;
        cuerpo.useGravity = false;
        cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        objeto.AddComponent<ImpactoCarne>().Preparar(tabla.y - 24f, reproducirSonido ? SonarCaida : null);
        return cuerpo;
    }

    private void FixedUpdate()
    {
        AplicarGravedad(carne);
        AplicarGravedad(cuchillo);
    }

    private static void AplicarGravedad(Rigidbody cuerpo)
    {
        if (cuerpo != null && !cuerpo.isKinematic && !cuerpo.IsSleeping())
            cuerpo.AddForce(Physics.gravity * 6f, ForceMode.Acceleration);
    }

    public string ActualizarInteraccion(Camera jugador, Ray mira)
    {
        if (carne == null)
            return null;

        var posicion = jugador.transform.position;
        var distanciaTacho = posicion - tacho;
        if (!encuentroActivado && Mathf.Abs(distanciaTacho.y) < 130f &&
            new Vector2(distanciaTacho.x, distanciaTacho.z).sqrMagnitude <= radioEncuentro * radioEncuentro)
        {
            encuentroActivado = true;
            StartCoroutine(Aparicion(posicion));
        }

        if (carneSuelta)
            return null;

        var alcance = 115f;
        var apuntaCarne = objetivoCarne.Raycast(mira, out var impactoCarne, alcance);
        var apuntaTabla = objetivoTabla.Raycast(mira, out var impactoTabla, alcance);
        if (!apuntaCarne && !apuntaTabla)
            return null;

        var distancia = Mathf.Min(apuntaCarne ? impactoCarne.distance : alcance,
            apuntaTabla ? impactoTabla.distance : alcance);
        var cuerpoJugador = jugador.GetComponentInParent<Rigidbody>();
        foreach (var obstaculo in Physics.RaycastAll(mira, distancia,
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (obstaculo.collider != objetivoCarne && obstaculo.collider != objetivoTabla &&
                (cuerpoJugador == null || obstaculo.rigidbody != cuerpoJugador))
                return null;
        }

        if (cortando)
            return "Cortando en la tabla…";

        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(1))
        {
            if (apuntaTabla && (!apuntaCarne || impactoTabla.distance < impactoCarne.distance))
            {
                StartCoroutine(CortarEnTabla());
                return "Cortando en la tabla…";
            }
            carneSuelta = true;
            var avance = Vector3.ProjectOnPlane(jugador.transform.position - carne.position, Vector3.up).normalized;
            var lateral = Vector3.Cross(Vector3.up, avance);
            var desplazamientoCuchillo = Vector3.ProjectOnPlane(carne.position - cuchillo.position, Vector3.up);
            if (BuscarAterrizaje(avance, lateral, out var destinoCarne, out var destinoCuchillo))
                StartCoroutine(CaidaSincronizada(avance, destinoCarne, destinoCuchillo));
            else
            {
                carne.isKinematic = false;
                carne.linearVelocity = avance * 100f + Vector3.up * 30f;
                carne.angularVelocity = new Vector3(2f, 1f, 3f);
                cuchillo.isKinematic = false;
                // Compensar la separación inicial y dejar espacio entre ambas piezas al aterrizar.
                cuchillo.linearVelocity = carne.linearVelocity + (desplazamientoCuchillo + lateral * 9f) / 1.8f;
                cuchillo.angularVelocity = new Vector3(4f, 2f, -3f);
            }
        }
        return apuntaTabla && (!apuntaCarne || impactoTabla.distance < impactoCarne.distance)
            ? "E o clic derecho · Cortar en la tabla" : "E o clic derecho · Tirar la carne";
    }

    private IEnumerator CortarEnTabla()
    {
        cortando = true;
        var inicio = cuchillo.transform.position;
        var giro = cuchillo.transform.rotation;
        var corte = tabla + new Vector3(7f, 5.4f, 5f);
        cuchillo.transform.position = corte + Vector3.up * 7f;
        Publicar("Play_CorteCapas", cuchillo.gameObject);
        const float duracion = 1.35f;
        for (var tiempo = 0f; tiempo < duracion; tiempo += Time.deltaTime)
        {
            var fase = Mathf.PingPong(tiempo / 0.17f, 1f);
            cuchillo.transform.SetPositionAndRotation(
                corte + new Vector3(-6f * tiempo / duracion, 7f * fase, 0f),
                Quaternion.Euler(Mathf.Lerp(5f, -25f, fase), 0f, -12f));
            yield return null;
        }
        cuchillo.transform.SetPositionAndRotation(inicio, giro);
        cortando = false;
    }

    private bool BuscarAterrizaje(Vector3 avance, Vector3 lateral, out Vector3 destinoCarne,
        out Vector3 destinoCuchillo)
    {
        destinoCarne = destinoCuchillo = default;
        for (var distancia = 48f; distancia <= 112f; distancia += 8f)
        {
            var centro = carne.position + avance * distancia;
            var origenCarne = centro - lateral * 8f;
            var origenCuchillo = centro + lateral * 8f;
            if (!Physics.Raycast(origenCarne, Vector3.down, out var sueloCarne, 120f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ||
                !Physics.Raycast(origenCuchillo, Vector3.down, out var sueloCuchillo, 120f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                continue;

            if (sueloCarne.point.y > tabla.y - 24f || sueloCuchillo.point.y > tabla.y - 24f ||
                sueloCarne.normal.y < 0.8f || sueloCuchillo.normal.y < 0.8f ||
                Mathf.Abs(sueloCarne.point.y - sueloCuchillo.point.y) > 2f)
                continue;

            destinoCarne = sueloCarne.point + Vector3.up * 2.5f;
            destinoCuchillo = sueloCuchillo.point + Vector3.up * 0.95f;
            return true;
        }
        return false;
    }

    private IEnumerator CaidaSincronizada(Vector3 avance, Vector3 destinoCarne, Vector3 destinoCuchillo)
    {
        var inicioCarne = carne.position;
        var inicioCuchillo = cuchillo.position;
        var giroFinal = Quaternion.LookRotation(avance);
        const float duracion = 0.9f;
        for (var tiempo = 0f; tiempo < duracion; tiempo += Time.deltaTime)
        {
            var progreso = tiempo / duracion;
            var descenso = progreso * progreso * progreso;
            var arco = Mathf.Sin(progreso * Mathf.PI) * 12f;
            var posicionCarne = Vector3.Lerp(inicioCarne, destinoCarne, progreso);
            posicionCarne.y = Mathf.Lerp(inicioCarne.y, destinoCarne.y, descenso) + arco;
            var posicionCuchillo = Vector3.Lerp(inicioCuchillo, destinoCuchillo, progreso);
            posicionCuchillo.y = Mathf.Lerp(inicioCuchillo.y, destinoCuchillo.y, descenso) + arco;
            carne.transform.position = posicionCarne;
            cuchillo.transform.SetPositionAndRotation(posicionCuchillo,
                giroFinal * Quaternion.Euler(progreso * 360f, 0f, 0f));
            yield return null;
        }

        carne.transform.position = destinoCarne;
        cuchillo.transform.SetPositionAndRotation(destinoCuchillo, giroFinal);
        SonarCaida(cuchillo.gameObject);
    }

    private IEnumerator Aparicion(Vector3 jugador)
    {
        var inicioAudio = Time.unscaledTime;
        Publicar("Play_TachoDeBasura", emisorTacho);
        yield return TemblarTacho();
        var direccion = Vector3.ProjectOnPlane(jugador - tacho, Vector3.up).normalized;
        if (direccion.sqrMagnitude < 0.01f)
            direccion = Vector3.forward;
        var destino = salidaRata;
        var frente = tacho + direccion * Mathf.Clamp(Vector3.ProjectOnPlane(
            jugador - tacho, Vector3.up).magnitude * 0.65f, 19f, 37f);
        var inicio = tacho + Vector3.up * 31f;
        StartCoroutine(CaidaDeTapa(inicioAudio, direccion));
        rata.transform.SetPositionAndRotation(inicio, Quaternion.LookRotation(direccion));
        rata.SetActive(true);
        Publicar("Play_SonidosDeRatita", rata);
        Publicar("Play_SustoRata", rata);

        const float salto = 0.55f;
        for (var tiempo = 0f; tiempo < salto; tiempo += Time.deltaTime)
        {
            var progreso = tiempo / salto;
            rata.transform.position = Vector3.Lerp(inicio, frente, progreso) +
                Vector3.up * (Mathf.Sin(progreso * Mathf.PI) * 13f);
            yield return null;
        }

        const float carrera = 1.25f;
        for (var tiempo = 0f; tiempo < carrera; tiempo += Time.deltaTime)
        {
            var progreso = tiempo / carrera;
            rata.transform.position = Vector3.Lerp(frente, destino, progreso) +
                Vector3.up * (Mathf.Abs(Mathf.Sin(progreso * Mathf.PI * 7f)) * 2.5f);
            var rumbo = Vector3.ProjectOnPlane(destino - frente, Vector3.up);
            if (rumbo.sqrMagnitude > 0.01f)
                rata.transform.rotation = Quaternion.LookRotation(rumbo);
            yield return null;
        }
        rata.transform.position = destino;
        foreach (var renderer in rata.GetComponentsInChildren<Renderer>())
            renderer.enabled = false;
    }

    private IEnumerator TemblarTacho()
    {
        var cuerpoInicio = cuerpoTacho.position;
        var tapaInicio = tapa.position;
        const float duracion = 0.38f;
        for (var tiempo = 0f; tiempo < duracion; tiempo += Time.unscaledDeltaTime)
        {
            var progreso = tiempo / duracion;
            var amplitud = Mathf.Sin(Mathf.PI * progreso);
            var desplazamiento = new Vector3(Mathf.Sin(progreso * 28f) * 1.8f,
                Mathf.Abs(Mathf.Sin(progreso * 33f)) * 0.65f, Mathf.Cos(progreso * 26f) * 1.3f) * amplitud;
            cuerpoTacho.position = cuerpoInicio + desplazamiento;
            tapa.position = tapaInicio + desplazamiento * 1.3f;
            yield return null;
        }
        cuerpoTacho.position = cuerpoInicio;
        tapa.position = tapaInicio;
    }

    internal void MostrarPersonajeEntrada(Vector3 puerta, GameObject voz)
    {
        var personaje = CrearRaizPersonaje("Ladrón", puerta + new Vector3(75f, 0f, -24f),
            Vector3.left, 1.24f);
        var ropa = MaterialDe(new Color(0.17f, 0.21f, 0.25f));
        var oscuro = MaterialDe(new Color(0.07f, 0.09f, 0.10f));
        var piel = MaterialDe(new Color(0.57f, 0.38f, 0.28f));
        var utileria = MaterialDe(new Color(0.23f, 0.25f, 0.28f));
        AgregarPieza(PrimitiveType.Capsule, "Torso", new Vector3(0f, 35f, 0f), new Vector3(18f, 14f, 11f), ropa);
        AgregarPieza(PrimitiveType.Sphere, "Cabeza", new Vector3(0f, 54f, 0f), new Vector3(12f, 14f, 11f), piel);
        AgregarPieza(PrimitiveType.Sphere, "Gorro", new Vector3(0f, 58f, -0.6f), new Vector3(13f, 8f, 12f), oscuro);
        for (var lado = -1; lado <= 1; lado += 2)
        {
            AgregarPieza(PrimitiveType.Capsule, "Pierna", new Vector3(lado * 4.1f, 13f, 0f),
                new Vector3(7f, 12f, 7f), oscuro);
            AgregarPieza(PrimitiveType.Cube, "Zapato", new Vector3(lado * 4.1f, 2f, 2f),
                new Vector3(7f, 4f, 11f), oscuro);
        }
        AgregarPieza(PrimitiveType.Capsule, "Brazo izquierdo", new Vector3(-9.4f, 32f, 1.8f),
            new Vector3(6f, 9.5f, 6f), ropa);
        AgregarPieza(PrimitiveType.Capsule, "Brazo derecho", new Vector3(9.4f, 38f, 7f),
            new Vector3(6f, 7f, 6f), ropa).localRotation = Quaternion.Euler(78f, 0f, 0f);
        AgregarPieza(PrimitiveType.Sphere, "Mano", new Vector3(9.4f, 39.4f, 14f),
            new Vector3(4.5f, 5.2f, 5.2f), piel);
        AgregarPieza(PrimitiveType.Cube, "Silueta de utilería", new Vector3(9.4f, 42.4f, 17.2f),
            new Vector3(3.2f, 2.8f, 8.4f), oscuro);
        AgregarPieza(PrimitiveType.Cube, "Acabado superior", new Vector3(9.4f, 43.9f, 17.2f),
            new Vector3(3.35f, 1.1f, 8f), utileria);
        AgregarPieza(PrimitiveType.Cube, "Empuñadura de utilería", new Vector3(9.4f, 39.8f, 14.8f),
            new Vector3(2.5f, 4.9f, 2.9f), oscuro).localRotation = Quaternion.Euler(-13f, 0f, 0f);
        AgregarPieza(PrimitiveType.Cube, "Marca de utilería", new Vector3(11.13f, 43.9f, 16f),
            new Vector3(0.14f, 0.5f, 1.3f), MaterialDe(new Color(0.55f, 0.5f, 0.39f)));
        voz.transform.SetParent(personaje, false);
        voz.transform.localPosition = new Vector3(0f, 51f, 5f);
        AkUnitySoundEngine.SetObjectPosition(voz, voz.transform);

        Transform AgregarPieza(PrimitiveType tipo, string nombre, Vector3 posicion, Vector3 escala, Material material)
            => PiezaPersonaje(personaje, tipo, nombre, posicion, escala, material);
    }

    internal void CrearPersonajeBano(GameObject voz) =>
        CrearEmpleada(voz, "Empleada del pasillo", new Vector3(5505f, voz.transform.position.y, 588f),
            new Vector3(-1f, 0f, -0.45f), new Color(0.54f, 0.17f, 0.23f));

    internal void CrearPersonajeFreidora(GameObject voz) =>
        CrearEmpleada(voz, "Empleada de la freidora", voz.transform.position + new Vector3(38f, 0f, 38f),
            Vector3.right, new Color(0.21f, 0.36f, 0.42f));

    private void CrearEmpleada(GameObject voz, string nombre, Vector3 posicion, Vector3 direccion, Color colorUniforme)
    {
        var personaje = CrearRaizPersonaje(nombre, posicion, direccion, 1.16f);
        var uniforme = MaterialDe(colorUniforme);
        var pantalon = MaterialDe(new Color(0.15f, 0.17f, 0.19f));
        var piel = MaterialDe(new Color(0.69f, 0.46f, 0.34f));
        var pelo = MaterialDe(new Color(0.17f, 0.095f, 0.065f));
        var placa = MaterialDe(new Color(0.93f, 0.79f, 0.48f));
        Agregar(PrimitiveType.Capsule, "Uniforme", new Vector3(0, 34, 0), new Vector3(16, 13, 10), uniforme);
        Agregar(PrimitiveType.Sphere, "Cabeza", new Vector3(0, 52, 0), new Vector3(11, 13, 11), piel);
        Agregar(PrimitiveType.Sphere, "Cabello", new Vector3(0, 55, -2), new Vector3(12, 10, 12), pelo);
        Agregar(PrimitiveType.Sphere, "Cola de pelo", new Vector3(0, 47, -6), new Vector3(5, 12, 5), pelo);
        Agregar(PrimitiveType.Cube, "Placa del uniforme", new Vector3(3.4f, 39, 4.7f), new Vector3(4, 2, 0.7f), placa);
        for (var lado = -1; lado <= 1; lado += 2)
        {
            Agregar(PrimitiveType.Capsule, "Pierna", new Vector3(lado * 4, 13, 0), new Vector3(6, 12, 6), pantalon);
            Agregar(PrimitiveType.Cube, "Zapato", new Vector3(lado * 4, 2, 1.5f), new Vector3(6, 4, 9), pantalon);
            Agregar(PrimitiveType.Capsule, "Brazo", new Vector3(lado * 8.3f, 33, 0.5f), new Vector3(5, 8, 5), uniforme);
            Agregar(PrimitiveType.Sphere, "Mano", new Vector3(lado * 8.3f, 24, 1), new Vector3(4, 6, 4), piel);
        }
        voz.transform.SetParent(personaje, false);
        voz.transform.localPosition = new Vector3(0, 49, 5);
        AkUnitySoundEngine.SetObjectPosition(voz, voz.transform);

        void Agregar(PrimitiveType tipo, string nombre, Vector3 posicion, Vector3 escala, Material material)
            => PiezaPersonaje(personaje, tipo, nombre, posicion, escala, material);
    }

    private Transform CrearRaizPersonaje(string nombre, Vector3 posicion, Vector3 direccion, float escala)
    {
        var suelo = posicion.y - 48f;
        if (Physics.Raycast(posicion, Vector3.down, out var impacto, 100f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && impacto.normal.y > 0.75f)
            suelo = impacto.point.y;
        posicion.y = suelo;

        var personaje = new GameObject(nombre).transform;
        personaje.SetParent(transform);
        personaje.SetPositionAndRotation(posicion, Quaternion.LookRotation(direccion));
        personaje.localScale = Vector3.one * escala;
        return personaje;
    }

    private Transform PiezaPersonaje(Transform personaje, PrimitiveType tipo, string nombre, Vector3 posicion,
        Vector3 escala, Material material)
    {
        var pieza = Pieza(tipo, nombre, Vector3.zero, escala, material).transform;
        pieza.SetParent(personaje, false);
        pieza.localPosition = posicion;
        return pieza;
    }

    private IEnumerator CaidaDeTapa(float inicioAudio, Vector3 direccionJugador)
    {
        // El WAV del tacho tiene dos transitorios al final, separados por unos 320 ms.
        const float primerGolpe = 3.32f;
        const float ultimoGolpe = 3.64f;
        const float duracionCaida = 0.48f;
        var lateral = Vector3.Cross(Vector3.up, direccionJugador);
        if (Vector3.Dot(lateral, salidaRata - tacho) > 0f)
            lateral = -lateral;

        var abierta = tacho + Vector3.up * 39f + lateral * 13f;
        var giroAbierta = Quaternion.AngleAxis(55f, direccionJugador);
        var reposo = tacho + lateral * 27f;
        if (Physics.Raycast(reposo + Vector3.up * 15f, Vector3.down, out var piso, 35f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && piso.normal.y > 0.8f)
            reposo = piso.point;
        reposo += Vector3.up * (Mathf.Abs(tapa.lossyScale.y) + 0.05f);
        tapa.SetPositionAndRotation(abierta, giroAbierta);

        while (Time.unscaledTime - inicioAudio < primerGolpe - duracionCaida)
            yield return null;

        while (Time.unscaledTime - inicioAudio < ultimoGolpe)
        {
            var tiempo = Time.unscaledTime - inicioAudio;
            if (tiempo < primerGolpe)
            {
                var progreso = Mathf.Clamp01((tiempo - primerGolpe + duracionCaida) / duracionCaida);
                tapa.SetPositionAndRotation(Vector3.Lerp(abierta, reposo, progreso * progreso),
                    Quaternion.Slerp(giroAbierta, Quaternion.identity, progreso));
            }
            else
            {
                var rebote = (tiempo - primerGolpe) / (ultimoGolpe - primerGolpe);
                tapa.SetPositionAndRotation(reposo + Vector3.up * (Mathf.Sin(rebote * Mathf.PI) * 4f),
                    Quaternion.identity);
            }
            yield return null;
        }

        tapa.SetPositionAndRotation(reposo, Quaternion.identity);
    }

    private void SonarCaida(GameObject objeto) => Publicar("Play_Caida", objeto);

    private void Publicar(string evento, GameObject emisor)
    {
        AkUnitySoundEngine.SetObjectPosition(emisor, emisor.transform);
        sonorizacion.Post(evento.Substring("Play_".Length), emisor);
    }

    private Material MaterialDe(Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
        materiales.Add(material);
        return material;
    }

    private readonly System.Collections.Generic.List<Material> materiales = new();

    private GameObject Pieza(PrimitiveType tipo, string nombre, Vector3 posicion, Vector3 escala,
        Material material, bool conservarCollider = false)
    {
        var pieza = GameObject.CreatePrimitive(tipo);
        pieza.name = nombre;
        pieza.transform.SetParent(transform);
        pieza.transform.position = posicion;
        pieza.transform.localScale = escala;
        pieza.GetComponent<Renderer>().sharedMaterial = material;
        if (!conservarCollider)
        {
            var collider = pieza.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
        }
        return pieza;
    }

    private void OnDestroy()
    {
        foreach (var material in materiales)
            Destroy(material);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(tacho + Vector3.up * 15f, new Vector3(25f, 30f, 25f));
#if UNITY_EDITOR
        UnityEditor.Handles.Label(tacho + Vector3.up * 35f, "Tacho de basura · aparece en Play");
#endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(tabla, new Vector3(32, 2, 23));
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(tacho, salidaRata);
    }
}
