# Fase 2 — FPS Controller y Bodycam

**Polish de movimiento:** consulta [FPS_POLISH.md](FPS_POLISH.md) para los controles Left Alt/Q/E, mantle contextual, nuevos valores, diagnóstico del túnel y recorrido de validación actualizado. Esa guía prevalece sobre los valores y el alcance originales de Fase 2 descritos aquí.

Compatible con Unity 2023.2.20f1 y URP 16.0.6. La Fase 1 no se modifica ni se conecta automáticamente al jugador de pruebas.

## Archivos añadidos

| Archivo dentro de Assets/Game | Responsabilidad |
| --- | --- |
| `Runtime/Player/PlayerInput.cs` | Lectura centralizada del Input Manager clásico; teclado, mouse, foco y cursor. |
| `Runtime/Player/PlayerLook.cs` | Yaw del jugador y pitch de ViewRoot; sensibilidad, inversión Y y límites. |
| `Runtime/Player/PlayerMotor.cs` | CharacterController, gravedad, suelo, pendientes, salto, velocidades, aceleración, crouch y eventos. |
| `Runtime/Player/PlayerDebugOverlay.cs` | Overlay OnGUI opcional con información del motor. |
| `Runtime/Camera/BodycamController.cs` | Composición de offsets, landing, FOV, lente y cesión de control para muerte. |
| `Runtime/Camera/Outbreak.Bodycam.asmdef` | Referencias a Game y a los assemblies URP ya instalados. |
| `Editor/FPS/FPSTestEnvironment.cs` | Generación de escena y materiales mediante APIs de Unity. |
| `Editor/FPS/Outbreak.FPS.Editor.asmdef` | Assembly Editor para el generador FPS. |
| `Tests/PlayMode/FPSControllerTests.cs` | Tres pruebas de física/API: espacio para levantarse, aterrizaje y cesión de cámara. |
| `Tests/PlayMode/Outbreak.FPS.Tests.asmdef` | Assembly de pruebas PlayMode, separado de las 16 pruebas EditMode existentes. |
| `FPS_README.md` | Esta guía. |

No se cambian paquetes, ajustes de input, escenas existentes ni los assemblies de la Fase 1. Unity genera los `.meta` al importar.

## Preparar y probar manualmente

1. Espera la importación/compilación; usa `Assets > Refresh` si el menú aún no aparece.
2. Sal de Play Mode y ejecuta **Outbreak > Crear entorno de prueba FPS**.
3. Si tu escena actual tiene cambios sin guardar, Unity muestra su diálogo habitual para conservarlos o cancelar.
4. La herramienta crea, guarda y abre **Assets/Game/Scenes/FPS_Test.unity**. El jugador queda seleccionado y listo para Play. Si el archivo ya existe, se genera un nombre único, por ejemplo `FPS_Test 1.unity`; la ruta exacta aparece en Console.
5. Presiona **Play** y enfoca la pestaña **Game**. Haz clic dentro de Game si hace falta capturar el cursor.

La jerarquía generada es:

```text
Player                         [CharacterController, Input, Look, Motor, DebugOverlay]
├── ViewRoot                   [altura: Motor; rotación: Look]
│   └── BodycamRig              [posición y rotación de efectos: BodycamController]
│       └── Main Camera        [Camera, AudioListener, URP Camera Data]
└── Bodycam Lens Volume        [perfil temporal privado durante Play]
```

El suelo y las paredes son primitivas. Los materiales de prueba se crean una sola vez en `Assets/Game/DebugContent/FPS/`; volver a ejecutar el menú no sobrescribe materiales existentes.

### Recorrido de validación

| Prueba | Acción y resultado esperado |
| --- | --- |
| Caminar | WASD: aceleración breve y frenada rápida. W+D no debe aumentar la velocidad respecto de W. |
| Mirada | Mouse: yaw inmediato y pitch limitado; el cuerpo no se inclina físicamente. |
| Saltar | Space: salto de aproximadamente 0.85 m. Mantener Space no encadena saltos. Hay 0.3 s de cooldown y un mínimo de apoyo antes de volver a saltar. |
| Sprint | W + Left Shift: velocidad cercana a 5.4 m/s. Atrás + Shift no activa sprint. La diagonal hacia delante sí puede hacerlo. |
| Crouch | Mantener Left Ctrl: altura pasa suavemente de 1.8 m a 1.15 m, velocidad 1.6 m/s, sin sprint. |
| Techo bajo | Directamente delante del inicio está el túnel de 1.32 m de altura libre. Entra agachado y suelta Ctrl debajo del techo: debes permanecer agachado. Al salir con Ctrl suelto, te levantas. |
| Pendientes | A la izquierda hay una rampa verde de 20° hasta una plataforma de 3 m. Puedes subir y bajar. A la derecha hay una rampa de 55°, superior al límite de 45° del controller, que no debes poder escalar. |
| Escalones/obstáculos | A la derecha hay cinco escalones de 0.2 m. Cerca del inicio hay obstáculos de 0.35 y 0.65 m para saltar. |
| Aterrizajes | Baja de los escalones o sal de la plataforma alta. La respuesta crece con la velocidad de caída y desaparece al estabilizarte; no debe repetirse estando quieto. |
| Bodycam | Quieto: respiración casi imperceptible. Caminar/correr: bob leve; strafe: inclinación pequeña; mirar: sway limitado. |
| FOV | Sprint: transición suave de 75° a 79° verticales; vuelve al parar. |
| Cursor | Escape libera el cursor y elimina input de movimiento/mirada. Un clic en Game lo captura de nuevo. La postura agachada se conserva al liberar el cursor; la gravedad sigue funcionando. |
| Console | No deben aparecer errores. El overlay muestra Speed, Grounded, Sprinting, Crouching y Vertical Velocity. |

La velocidad vertical del motor puede marcar `-3` en suelo: es la velocidad de adhesión al suelo, no una caída real. `Velocity` informa el desplazamiento real resuelto por CharacterController; `VerticalVelocity` informa la velocidad vertical interna del motor.

## Configuración

- **PlayerMotor**, en Player: velocidades, aceleración/frenada, control aéreo, gravedad, altura de salto, cooldown, ground snap, alturas, transición y máscara de colisiones. `CharacterController` expone radio, skin, step offset y slope limit.
- **PlayerLook**, en Player: sensibilidad, inversión Y y límites verticales. Los deltas del mouse no se multiplican por deltaTime.
- **BodycamController**, en BodycamRig: amplitudes y respuestas de respiración, bob, sway, roll, aceleración, landing y FOV. `Effect Strength = 0` elimina offsets de movimiento; FOV y lens distortion se configuran por separado.
- **PlayerDebugOverlay**: desactiva `Show Overlay` o el componente para ocultarlo.
- Para comparar una cámara completamente estable, desactiva BodycamController en Play: restablece pose local, FOV normal y desactiva su distorsión.
- Usa raíz del jugador vertical, sin rotación X/Z y escala `(1,1,1)`; el origen representa los pies. Evita escalar padres del jugador. La máscara de suelo/techo debe incluir las capas físicas que bloquean al controller; se ignoran triggers y colliders hijos del jugador.
- Las referencias se asignan automáticamente en la escena de prueba. Al crear otro jugador, mantén la misma jerarquía y asigna ViewRoot a Look/Motor y los cuatro componentes de gameplay a BodycamController.

La lente usa el sistema Volume de URP instalado: intensidad inicial `-0.06`, escala `1.02`. El generador habilita postprocesado en la cámara. Puedes desactivar `Enable Lens Distortion` o poner intensidad cero. El perfil se crea/clona en memoria y se destruye al salir; no se modifican perfiles de entorno. Si usas otra escena, asigna un Volume dedicado y confirma que su layer esté incluido en el Volume Layer Mask de la cámara.

## API para próximos sistemas

`PlayerMotor` expone `Velocity`, `VerticalVelocity`, `Speed`, `IsGrounded`, `IsMoving`, `IsSprinting`, `IsCrouching`, `MoveInput`, `LookInput`, `GroundNormal` y `CurrentHeight`.

Eventos:

- `OnJump`: salto aceptado.
- `OnLand(float fallSpeed)`: entrada en apoyo con velocidad de impacto positiva, en m/s. Puede emitirse al apoyar por primera vez tras crear el jugador; el bodycam ignora impactos inferiores a su umbral.
- `OnStartedSprinting` / `OnStoppedSprinting`: cambio efectivo de sprint, incluyendo fin por perder suelo o detenerse.
- `OnCrouchChanged(bool)`: cambio de postura; al levantarse, el estado sigue agachado hasta completar la altura.

El mouse/yaw se procesa antes del motor y el bodycam compone offsets en LateUpdate. PlayerMotor solo escribe la posición local de ViewRoot; PlayerLook solo su rotación; BodycamController solo escribe BodycamRig y el FOV de Camera. No se añade Rigidbody al jugador.

El input existente (`activeInputHandler = 0`) sigue centralizado en PlayerInput; una migración futura puede reemplazar su lector manteniendo las propiedades que consumen Look y Motor.

### Cesión para muerte

```csharp
bodycam.EnterDeathState();
// Desde el futuro director de muerte, en cada paso de su secuencia:
bodycam.SetDeathPose(worldPosition, worldRotation);
// También existe EnterDeathState(worldPosition, worldRotation).
```

`EnterDeathState` es idempotente: desactiva input, motor y mirada, detiene efectos normales y emite `OnDeathStateEntered`. `SetDeathPose` solo actúa tras esa transición y recibe la pose mundial de BodycamRig. La cámara hija debe conservar su pose local neutra. El CharacterController permanece presente, sin movimiento normal; el futuro director decidirá la colisión/caída. No hay física de cadáver, audio, fade ni animación de muerte en esta fase. Para volver a jugar, recrea el jugador; aún no hay flujo de respawn.

## Validación de código

Se compilan los scripts con el compilador y referencias locales de Unity 2023.2.20f1. La evaluación de movimiento y comodidad requiere la prueba manual anterior; no se han hecho capturas ni pruebas visuales automáticas.

Opcionalmente, con Play Mode detenido: `Window > General > Test Runner > PlayMode > Outbreak.Tests.FPSControllerTests`. Las tres pruebas ejercitan colliders y APIs sin renderizar su cámara. No se ejecutaron desde otra instancia de Unity. Las 16 pruebas EditMode de Fase 1 siguen en su assembly original.
