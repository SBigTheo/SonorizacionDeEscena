# Contexto del proyecto de sonorización

Este documento reúne la consigna, las decisiones y el estado del proyecto `SonorizacionDeEscena` al 11 de noviembre de 2026.

## Consigna del trabajo práctico

- Sonorizar un espacio virtual en Unity con al menos dos habitaciones y un First Person Controller como punto de escucha.
- Implementar **14 SFX**. La consigna pide siete espacializados, cinco espacializados por acción y dos extradiegéticos. El ambiente y la música quedan fuera de ese conteo.
- Diseñar al menos **cinco SFX con cuatro capas cada uno**, con una capa de Foley grabada por el grupo. Incorporar también una grabación de voz propia y un ambiente general en loop.
- Componer e implementar en Wwise dos piezas originales. La primera usa **remezcla vertical** con capas que cambian durante el recorrido. La segunda usa **resecuenciación horizontal** con secciones musicales que cambian según el recorrido.
- Entregar un PDF con el detalle de cada SFX y su ubicación, capturas de las cuatro capas y del Foley, capturas de la composición y de la implementación en Wwise, y los nombres completos del grupo. Incluir el ejecutable de Unity y los audios exportados en WAV.
- Comprimir la entrega como `TPFinal_grupo1.zip` o `TPFinal_grupo1.rar`, según el número de grupo que corresponda. Si supera 50 MB, entregar un TXT con el enlace de acceso al archivo alojado.

## Proyecto e integración

- Unity **6000.6.7f1** y Wwise **2025.1.12.9287**. La escena activa para el trabajo es [SampleScene](Assets/Scenes/SampleScene.unity), un restaurante con comedor, cocina, baño y exterior de drive thru.
- El [proyecto de Wwise](SonorizacionDeEscena_WwiseProject/SonorizacionDeEscena_WwiseProject.wproj) está dentro del proyecto Unity. [WwiseSettings.xml](Assets/WwiseSettings.xml) define las rutas de integración.
- `WwiseGlobal` inicializa el motor. El `AkAudioListener` activo está en `First Person Camera`. La cámara fija `Main Camera` quedó desactivada.
- Los audios recibidos están en [Assets/Sonidos](Assets/Sonidos). Son los únicos materiales que el usuario tenía disponibles durante la implementación. Los MP3 se convirtieron a WAV para importar en Wwise y se conservaron los originales de Unity.
- Se crearon **25 eventos de reproducción** `Play_…` y se generaron `Init.bnk` y `Sonorizacion.bnk` para Windows. Los bancos y sus metadatos se encuentran en [GeneratedSoundBanks](SonorizacionDeEscena_WwiseProject/GeneratedSoundBanks).
- [SonorizacionRestaurante.cs](Assets/Audio/SonorizacionRestaurante.cs) carga el banco y maneja la reproducción en la escena. El objeto **Sonorización del restaurante** contiene los 16 puntos configurados.

## Inventario de audios implementados

Los seis diálogos se habilitan en el orden de la tabla. Al terminar uno, el siguiente se puede activar en su zona. La radio conserva el clip completo con música de aproximadamente **2 min 38 s**.

| Audio | Evento de Wwise | Activación |
|---|---|---|
| Recepción | `Play_DialogoRecepcion` | 1. Drive thru, con E o clic izquierdo |
| Orden de papas | `Play_DialogoPapas` | 2. Al acercarse a la freidora |
| Radio con música | `Play_DialogoRadio` | 3. Al acercarse a la radio de cocina |
| Televisión | `Play_DialogoTelevision` | 4. Al acercarse al televisor del comedor |
| Diálogo del baño | `Play_DialogoBano` | 5. Al acercarse al cubículo |
| Diálogo del ladrón | `Play_DialogoLadron` | 6. Al acercarse a la puerta principal |
| Ambiente del restaurante | `Play_AmbienteRestaurante` | Loop desde el inicio de la escena |
| Ambiente de cocina | `Play_AmbienteCocina` | Loop en la zona de cocción |
| Ambiente de drive thru | `Play_AmbienteDriveThru` | Loop en la zona exterior |
| Fritura constante | `Play_Fritura` | Loop cerca de la freidora |
| Fritura en capas | `Play_FrituraCapas` | Junto al pedido de papas |
| Cronómetro de cocina | `Play_Cronometro` | Junto al pedido de papas |
| Agua corriendo | `Play_AguaLavabo` | Loop cerca de la canilla |
| Auto pasando | `Play_AutoPasando` | Al entrar en la zona del carril |
| Bocina del auto | `Play_AutoBocina` | Junto al auto en el carril |
| Botón físico | `Play_BotonFisico` | Al activar la recepción |
| Switch físico | `Play_SwitchFisico` | Al interactuar con la caja registradora |
| Cadena del inodoro | `Play_CadenaBano` | Al interactuar con el inodoro |
| Puerta abriéndose | `Play_PuertaAbrir` | Al entrar en la zona de la puerta |
| Puerta cerrándose | `Play_PuertaCerrar` | Al salir de la zona de la puerta |
| Corte en capas | `Play_CorteCapas` | Al interactuar con la mesada |
| Corte de vegetales o carne | `Play_CorteVegetales` | Alterna con el corte en capas |
| Lavado de manos | `Play_LavadoManos` | Al interactuar con el lavamanos |
| Aviso de nuevo pedido | `Play_NotificacionPedido` | Aviso 2D al activarse el pedido de papas |
| Aviso de drive thru | `Play_NotificacionDriveThru` | Aviso 2D al entrar en la zona del carril |

El conteo de 25 corresponde a clips y eventos de implementación. En la propuesta para los 14 SFX, apertura y cierre son variantes del efecto de puerta. La propuesta cuenta como siete fuentes el televisor, la radio, el cronómetro, la fritura, la canilla, el auto y el altavoz. Los cinco efectos por acción son la cadena, la puerta, el switch de la caja, el corte y el lavado. Los dos avisos 2D son extradiegéticos. En el PDF conviene declarar expresamente cómo se agrupan los diálogos narrativos adicionales, la bocina y las variantes.

## Cómo probar y editar

1. Abrir `SampleScene` y entrar en **Play**. El ambiente general arranca automáticamente. La indicación en pantalla guía al siguiente diálogo y muestra **E o clic izquierdo** cuando corresponde interactuar.
2. Para probar clips por separado, usar **Tools → Sonorización → Probar audios Wwise**. La ventana permite reproducir un evento, lanzar los seis diálogos en orden o comprobar que Wwise acepte los 25 eventos. [Herramienta de prueba](Assets/Editor/PruebaSonorizacion.cs)
3. Fuera de Play, seleccionar **Sonorización del restaurante → Points**. Desplegar el punto y pulsar **Mover en Scene**, justo debajo de **Position**, para centrar la vista y usar las flechas X, Y y Z. También se puede editar `Position` numéricamente. Las coordenadas son del mundo y mover la fuente también mueve el centro de activación. `Radius` controla la distancia de activación y participa en la caída de volumen del punto. Guardar con **Ctrl+S**. [Inspector de emisores](Assets/Editor/SonorizacionRestauranteEditor.cs)
4. Mantener el orden de los primeros seis puntos de la lista. El controlador los usa como secuencia de diálogos.
5. Para ajustar un clip, abrir **Containers → Default Work Unit → Sonorizacion** en Wwise, cambiar **Voice Volume**, guardar y regenerar los SoundBanks para Windows. Salir y volver a entrar en Play para cargar el banco actualizado.

El modelo del restaurante agrupa mallas por material. Por eso los emisores usan puntos editables en coordenadas de la escena. Las posiciones y radios iniciales son aproximados. El usuario comentó que ya hizo algunos cambios y encontró sonidos para mover o bajar de volumen. Los valores guardados en la escena son la referencia vigente.

## Incidencias y comprobaciones

- El Launcher exigió iniciar sesión en Audiokinetic y cerrar Unity para instalar la integración.
- Tras la instalación faltaban `Init.bnk` y `ProjectInfo.json`. Generar los SoundBanks para Windows resolvió `AK_FileNotFound` y `Bank Load Failed`. También se corrigió el punto de escucha duplicado al desactivar la cámara fija y configurar la cámara del jugador.
- La consola mostró una vez `Voice Starvation`. La causa exacta quedó sin identificar. Si reaparece, reproducir el caso con Wwise Profiler y Capture Log para identificar la voz afectada.
- Unity advirtió que recibió una ruta absoluta a `dialogo ladron.wav` en Downloads. La copia utilizable desde el proyecto está en `Assets/Sonidos/dialogo ladron.wav`.
- El aviso de Input Manager obsoleto se relaciona con el controlador, que todavía usa la API de entrada antigua. El proyecto estaba configurado en **Both**.
- En Play, Wwise aceptó los **25 eventos**. Una prueba técnica posterior movió al jugador entre los **16 puntos** y detuvo los diálogos para comprobar sus transiciones. La secuencia llegó al final con **0 errores en consola** en esa prueba.
- Queda por verificar el recorrido caminando a ritmo normal, con la escucha de cada evento y el ajuste de ubicaciones, alcance y mezcla. El último cambio del Inspector, que movió el botón a cada punto, quedó pendiente de verificación visual y de arrastre en Unity.

## Trabajo pendiente para la entrega

- Confirmar con el grupo qué diálogo usa voz propia y qué grabaciones corresponden al Foley. Los WAV de fritura y corte identificados como mezclas en capas sirven como referencia, pero por sí solos no documentan las cuatro pistas ni la procedencia del Foley.
- Completar y documentar los cinco SFX de cuatro capas. Se propusieron fritura, corte, puerta, cadena y auto.
- Implementar y comprobar las dos piezas musicales originales en Wwise. El clip de radio mezcla música y voz en un solo archivo. Para las técnicas adaptativas hacen falta pistas o secciones separadas según la pieza.
- Generar y probar el ejecutable, reunir los WAV de entrega, sacar las capturas del proceso, completar el PDF con los nombres del grupo y armar el comprimido final.
