# Informe de ubicación y activación de los sonidos

**Proyecto:** Sonorización de una escena · **Escena:** `SampleScene` · **Motor de audio:** Wwise.

La escena representa un restaurante con comedor, cocina, baños y un sector de atención vehicular. La cámara en primera persona funciona como punto de escucha. El controlador de la escena carga el SoundBank `Sonorizacion`. Los sonidos de objetos se reproducen desde sus emisores y se atenúan con la distancia. Las notificaciones de interfaz son 2D. El diálogo del ladrón se dispara en la entrada y se reproduce desde la cámara del jugador.

La configuración reúne **25 eventos de Wwise en 23 puntos de la escena**. Algunos puntos disparan varios eventos y varios objetos reutilizan un mismo audio. La tabla describe cada evento según la configuración guardada. Los nombres de evento llevan el prefijo `Play_`.

## Diálogos

| Audio | Evento de Wwise | Ubicación y activación |
|---|---|---|
| Recepción, «Bienvenido a McDonald's» | `Play_DialogoRecepcion` | Ventanilla del drive thru. Acercarse, apuntar a la marca y pulsar **E o clic derecho**. Inicia la secuencia y se acompaña del botón físico. |
| Orden de papas | `Play_DialogoPapas` | Freidora en la cocina. Se activa por cercanía después de la recepción. Dispara también el cronómetro, la fritura en capas y el aviso de pedido. |
| Diálogo y música de radio | `Play_DialogoRadio` | Radio sobre la mesada de cocina. Apuntar a la radio y pulsar **E o clic derecho**. En su turno, el diálogo avanza la secuencia al finalizar. |
| Diálogo de televisión | `Play_DialogoTelevision` | Televisor suspendido del techo del comedor. Apuntar al televisor y pulsar **E o clic derecho**. En su turno, el diálogo avanza la secuencia al finalizar. |
| Diálogo del baño | `Play_DialogoBano` | Emisor del cubículo elegido. Se activa por cercanía dentro de la zona de 65 unidades cuando termina el diálogo de la TV en la secuencia. Suena una vez por partida. |
| Diálogo del ladrón | `Play_DialogoLadron` | La zona de activación está en la puerta principal, con un radio de 120 unidades. Se dispara automáticamente al entrar por primera vez en esa zona. La voz se reproduce desde la cámara del jugador y suena una vez por partida. |

La secuencia principal avanza en este orden: **recepción → papas → radio → televisión → baño**. La radio y la TV también se pueden escuchar fuera de turno. Para avanzar la secuencia hay que activarlas cuando les corresponde. El ladrón tiene un disparador independiente en la entrada. En pantalla se muestra la mira y el aviso de interacción cuando corresponde.

## Efectos del entorno y avisos

| Audio | Evento de Wwise | Ubicación y activación |
|---|---|---|
| Botón físico | `Play_BotonFisico` | Ventanilla del drive thru. Suena al activar el diálogo de recepción. |
| Cronómetro | `Play_Cronometro` | Freidora. Suena al dispararse la orden de papas. |
| Fritura constante | `Play_Fritura` | Emisor propio en la freidora. Entra en loop al acercarse y se desvanece al salir de la zona. |
| Fritura en capas | `Play_FrituraCapas` | En la freidora, desde la posición del pedido de papas. Suena junto a ese diálogo. |
| Agua de las canillas | `Play_AguaLavabo` | Dos emisores, uno por canilla. Apuntar al lavamanos correspondiente y pulsar **E o clic derecho** para abrir el agua en loop. Pulsar otra vez inicia el cierre después de **1,5 s**. |
| Lavado de manos | `Play_LavadoManos` | Dos lavamanos. Apuntar a uno y hacer **clic izquierdo** para oír el lavado desde ese punto. |
| Descarga del inodoro | `Play_CadenaBano` | Tres inodoros, con un emisor cada uno. Apuntar al elegido y pulsar **E o clic derecho**. |
| Auto pasando | `Play_AutoPasando` | Carril exterior del drive thru, separado de la ventanilla. Se activa al entrar en la zona del carril. El emisor recorre el eje Z. |
| Bocina del auto | `Play_AutoBocina` | Mismo emisor móvil del carril. Suena junto al auto al entrar en esa zona. |
| Apertura de puerta | `Play_PuertaAbrir` | Umbral de la puerta principal. Suena cada vez que el jugador entra en su zona. |
| Cierre de puerta | `Play_PuertaCerrar` | Mismo umbral. Suena cada vez que el jugador sale de la zona. |
| Switch de caja | `Play_SwitchFisico` | Tres cajas registradoras. Apuntar a la caja correspondiente y pulsar **E o clic derecho**. |
| Corte en capas | `Play_CorteCapas` | Mesada de cocina. Acercarse y pulsar **E o clic izquierdo**. Es la primera variante. |
| Corte de vegetales o carne | `Play_CorteVegetales` | Misma mesada. Se alterna con el corte en capas en cada interacción. |
| Aviso de nuevo pedido | `Play_NotificacionPedido` | Aviso 2D de interfaz. Suena al activarse la orden de papas. |
| Aviso del drive thru | `Play_NotificacionDriveThru` | Aviso 2D de interfaz. Suena al entrar en la zona del carril, junto al auto y la bocina. |

## Ambientes

| Audio | Evento de Wwise | Ubicación y activación |
|---|---|---|
| Ambiente de restaurante | `Play_AmbienteRestaurante` | Comedor, radio de **280 unidades**. Suena en loop dentro de su zona. Su nivel base en Unity es **−6 dB** y disminuye a medida que sube el ambiente de cocina. |
| Ambiente de cocina | `Play_AmbienteCocina` | Zona de cocción, radio de **150 unidades**. Entra en loop y aumenta de volumen al adentrarse en la cocina. |
| Ambiente de drive thru | `Play_AmbienteDriveThru` | Exterior, radio de **140 unidades**. Se reproduce en loop al entrar en la zona exterior. |

## Referencia de ubicación en Unity

En `SampleScene`, los puntos se editan desde **Sonorización del restaurante → Inspector → Points**. **Position** define la posición del emisor o el centro de su zona. **Radius** indica el alcance horizontal. **Radio de puntería** delimita la esfera que debe tocar la mira en las interacciones que requieren apuntar. **Mover en Scene** permite alinear el punto con el objeto. **Probar audio** reproduce su evento desde esa posición para revisar la escucha.

Las tablas documentan la implementación guardada. La validación de mezcla y activación en el recorrido final se hace en **Play**.
