# FPS movement polish — Unity 2023.2.20f1

## Preparación

Fuera de Play Mode, ejecuta **Outbreak > Crear entorno de prueba FPS**. Guarda la escena actual si Unity lo solicita. Se crea una escena con nombre único; no se sobrescribe la anterior. Pulsa Play y haz clic en Game. Escape libera el cursor.

La escena original `FPS_Test.unity` también recibe los nuevos valores de salto, bob, roll y transición de crouch, sin reemplazar su geometría. Los obstáculos nuevos aparecen al generar otro entorno.

## Cambios y diagnóstico del túnel

La cápsula agachada mide 1,15 m y el túnel deja 1,32 m: hay 0,17 m de holgura, pero el step offset anterior seguía en 0,28 m. El ascenso automático del CharacterController no tenía una comprobación de techo. Además, `OnControllerColliderHit` proyectaba **todos** los contactos no caminables al plano horizontal y normalizaba el resultado: un contacto de techo con una pequeña componente horizontal podía eliminar por completo esa componente de velocidad. No había una velocidad distinta para W dentro del túnel.

El motor ahora calcula cuánto step offset cabe sobre la cabeza, incluyendo el siguiente desplazamiento; separa los contactos de techo de los de pared y evita reasignar altura/centro cuando la postura ya está estable. No depende de nombres de objetos ni dimensiones particulares del túnel. La geometría y el skin width del escenario no se cambian.

Estos defectos están identificados en el código. **La atribución del síntoma observado y la corrección efectiva aún requieren ejecutar la regresión de física o la prueba manual:** la instancia de Unity de validación no pudo iniciar por un fallo de conexión con el cliente de licencias. No se presenta una reproducción ejecutada como si estuviera confirmada.

## Recorrido manual y resultados esperados

Las coordenadas son X/Z en la escena generada; el jugador empieza en `(0, -7)` mirando hacia +Z.

| Prueba | Procedimiento y resultado esperado |
| --- | --- |
| Slow walk | En suelo libre, mantén W y Left Alt. Tras acelerar, `Speed` debe rondar **1,4 m/s** y `SlowWalk` ser True. Suelta Alt: vuelve a **3,2 m/s**. Añade Shift manteniendo Alt: prevalece sprint, **5,4 m/s**, `SlowWalk` False. |
| Crouch slow walk | Mantén Ctrl + W: **1,6 m/s**. Añade Alt: **0,75 m/s**, `Crouching` y `SlowWalk` True. Shift no permite sprint agachado. |
| Salto | En suelo sin obstáculos delante, pulsa Space. La altura configurada es **1 m**, antes 0,85 m; la integración discreta puede producir unos centímetros adicionales. La gravedad sigue en **24 m/s²**. Mantener Space no encadena saltos. El salto normal sigue bloqueado al estar agachado, como antes. |
| Sprint jump | En una zona despejada, toma velocidad con W + Shift y pulsa Space. Compara con W + Space: misma altura, mayor distancia horizontal por la velocidad de despegue. Soltar Shift en el aire no debe frenar hacia 3,2 m/s. Girar en el aire debe tener efecto limitado. Al aterrizar vuelve la aceleración/frenada normal; pulsa Space inmediatamente para comprobar el mínimo de apoyo de **0,08 s** y cooldown de **0,3 s**. |
| Mantle válido | Ve a la caja de **0,8 m**, centro `(-6, -6)`. Mira perpendicularmente a una cara, con los pies a unos **0,4–0,6 m** de ella. Pulsa Space: `Mantling` True durante aproximadamente **0,48 s**, elevación y avance suaves. WASD no debe desviar el recorrido. Al terminar recuperas control. Repite con la caja de **1 m** en `(-10, -6)`. |
| Bordillo y barrera | Bordillo de **0,18 m** en `(3, -7)`: puedes subir caminando mediante step offset o pulsar Space justo antes para el movimiento contextual. Barrera delgada de **0,7 m** en `(3, -10)`: Space debe superar la barrera y apoyar detrás; requiere suelo y espacio libres. |
| Obstáculo no escalable | Frente al bloque de **2,4 m**, centro `(-14, -6)`, pulsa Space. `Mantling` debe permanecer False; se permite el salto normal, pero no alcanzar la parte superior ni escalar al repetirlo. |
| Destino bloqueado | La caja de `(-6, -10)` tiene techo. Intenta Space estando de pie: no debe comenzar mantle ni atravesar el techo. También comprueba que una pared o techo en el recorrido impide la acción. |
| Lean | Quieto, mantén Q y luego E. `Lean` progresa hacia **-1/+1**, con desplazamiento de **0,18 m** y roll de **7°** por defecto. Suelta: retorno suave a cero. Q + E se neutralizan. Repite agachado. El mouse sigue respondiendo inmediatamente. |
| Lean contra pared | Entra por el sur al pasillo entre **X=10 y X=11,2**, Z entre -8 y -4. Acércate a una pared y mantén Q/E hacia ella. La cámara debe detenerse antes de atravesarla y `Lean` quedar limitado. Prueba también moviéndote hacia la pared con lean ya aplicado y mirando arriba/abajo. Al alejarte, recupera el desplazamiento. |
| Bodycam | Camina y corre varios metros: bob vertical y horizontal **15 %** mayor. Haz strafe: roll sutil de **1,3°** antes del multiplicador de comodidad. Mueve el mouse y detente: el sway visual vuelve progresivamente, sin retardar yaw/pitch principal. Puedes reducir `Effect Strength` para comparar; el lean táctico es independiente. |
| Crouch y techo | Alterna Ctrl en suelo libre: transición aproximada **0,155 s**, conservando interpolación. Dentro del túnel, suelta Ctrl: no debes poder levantarte. Al salir con Ctrl suelto, vuelve la altura de pie. |
| Entrada frontal al túnel | Acércate al centro del túnel, entrada en **Z=1**, agáchate completamente antes de entrar y mantén W. `Speed` debe permanecer alrededor de **1,6 m/s**, incluida la entrada y el interior. Repite mirando 90° a un lado y usando A/D para recorrer la misma trayectoria mundial. Compara también Ctrl + Alt: **0,75 m/s** en ambas orientaciones. No compares mientras todavía baja la cápsula. |
| Regresión | Prueba rampa verde de 20°, escalones y aterrizaje desde plataforma; la rampa de 55° sigue sin ser caminable. Escape elimina los nuevos inputs; comprueba que no queda un lean retenido. |

## Parámetros y API

En `PlayerMotor`: `slowWalkSpeed = 1.4`, `crouchSlowWalkSpeed = 0.75`, `jumpHeight = 1`, `heightChangeSpeed = 4.2`.

Mantle: `minimumMantleHeight = 0.12`, `maximumMantleHeight = 1.05`, `mantleDuration = 0.48`, `mantleForwardDistance = 0.45`, `mantleDetectionDistance = 0.65`. La detección se mide desde el borde frontal de la cápsula. El avance se mide desde la cara detectada, añadiendo el radio para dejar sitio al cuerpo. Se valida una superficie caminable, apoyo central y periférico, cápsula libre y barridos de elevación/avance/descenso. Una barrera delgada puede terminar sobre suelo detrás. Un hueco sin apoyo o un techo que no deje pasar la postura actual se rechazan. Una ventana baja solo es válida si permite pasar la cápsula completa. Durante mantle se conserva la postura y se revalida cada tramo; una obstrucción nueva cancela y devuelve control/gravedad. El CharacterController permanece activo.

En `BodycamController`: `leanAngle`, `leanDistance`, `leanSpeed`, `leanCollisionRadius`, `leanCollisionMask`, `swaySmoothTime`. La comprobación de lean incluye el radio del plano cercano de cámara y el desplazamiento final combinado con bob; ignora triggers y colliders del jugador. La máscara debe incluir las paredes. Si el centro de cámara ya está dentro de una pared, se bloquea el desplazamiento extra; esta comprobación no sustituye la resolución física del jugador.

`PlayerMotor.IsWalkingSlow`: modo lento solicitado mientras hay input de movimiento y apoyo, sin sprint ni mantle. Para ruido futuro, combinarlo con movimiento real (`Speed`/`IsMoving`). `PlayerMotor.IsMantling`: transición contextual activa. `BodycamController.LeanAmount`: lean efectivo firmado tras limitar por colisión; `IsLeaning`: magnitud mayor a 0,01. `IPlayerLeanState` permite al overlay consultar lean sin crear una dependencia circular entre assemblies.

Toda lectura de teclado sigue en `PlayerInput`: Left Alt, Q, E y Space. La jerarquía se conserva: Motor escribe altura en ViewRoot, Look escribe yaw/pitch, Bodycam compone los efectos y lean exclusivamente en BodycamRig.

## Validación de código

Compilación con Roslyn y referencias locales de **Unity 2023.2.20f1**: runtime, bodycam, herramienta Editor y assembly de pruebas, sin errores ni advertencias emitidas. No se realizó validación visual automática.

Se conservaron las tres pruebas anteriores y se añadieron **13 casos** de física: túnel frontal/lateral, velocidades/prioridad, salto caminando/corriendo, cinco destinos de mantle, barrera delgada, cancelación por obstrucción y colisión/trigger de lean. Se ejecutan en **Window > General > Test Runner > PlayMode > Outbreak.Tests.FPSControllerTests**. No renderizan la cámara.

La ejecución automática intentada en un proyecto aislado abortó antes de cargar pruebas: `IPC channel to LicensingClient doesn't exist`, salida **199**. Log: `Temp/FPSPolishChecks/unity-tests.log`. Las pruebas añadidas compilan, pero sus resultados físicos quedan pendientes de ejecutar desde el Editor con licencia activa.
