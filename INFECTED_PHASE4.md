# OUTBREAK: EXTRACTION — Fase 4

Unity **2023.2.20f1 / URP**. Player health, infected/runner, navegación, percepción, ruido y muerte bodycam. Sin Fase 5.

## Crear y ejecutar

1. Abrir el proyecto en Unity 2023.2.20f1 y esperar la importación/compilación.
2. Fuera de Play: **Outbreak > Crear entorno de prueba Infected**.
3. El generador crea `Infected_Test.unity` con nombre único, los prefabs `DebugInfected` / `DebugRunner`, materiales y un NavMeshData en `Assets/Game/DebugContent/Infected/`.
4. El NavMesh se construye en Editor con **AI Navigation 2.0.0**, ya instalado. Solo se recoge la geometría del entorno, no el player, sus armas ni los infectados. Si no se genera NavMesh o un spawn no tiene soporte, el generador informa del fallo y no guarda una escena de prueba válida.
5. Se añade la nueva escena a Build Settings para permitir `SceneManager.LoadScene` con **F9**. Las entradas anteriores se conservan. Este es un cambio de tooling de la escena debug, no una modificación de las escenas existentes.
6. Pulsar Play y hacer clic en Game View para capturar cursor. `Escape` libera cursor. **F9** reinicia esta escena incluso después de morir.

No se sobrescriben FPS_Test, Gunplay_Test ni Gunplay_3B_Test. Los assets debug existentes se conservan al regenerar. No se crea GameManager ni un perfil/raid real en esta prueba: la muerte termina en DEAD y permite F9 sin tocar guardado/stash.

## Parámetros iniciales

| Parámetro | Normal (verde) | Runner (naranja) |
| --- | --- | --- |
| HP | 48 | 36 |
| Wander / Chase | 0.8 / 2.4 m/s | 1.1 / 4.3 m/s |
| Reacción visual | 0.25 s | 0.1 s |
| Visión | 18 m, cono horizontal 105° | 18 m, cono horizontal 105° |
| Memoria después de perder LOS | 2.5 s | 2.5 s |
| Búsqueda al llegar | 4 s; timeout global de investigación 12 s | Igual |
| Ataque | 15 daño, rango 1.35 m | 12 daño, rango 1.35 m |
| Windup / cooldown después del golpe | 0.45 / 1.35 s | 0.32 / 1.35 s |
| Stagger | Umbral 20 daño, duración 0.3 s, cooldown 1.5 s | Igual |

Player: 100 HP, sin regeneración ni resurrección por Heal. Hay cuatro normales y dos runners al iniciar; máximo seis. Respawn debug desactivado por defecto. Los cadáveres caen proceduralmente, desactivan hitboxes y desaparecen en 7 s; el spawner retira los objetos inactivos.

El rango auditivo máximo individual es 35 m. Rifle normal emite radio 40 m; pistola 24 m. Se multiplica por el snapshot de ruido efectivo de Fase 3B: suppressor ×0.25 → rifle 10 m / pistola 6 m; compensator ×1.2. Es un radio de gameplay, sin simulación acústica: paredes bloquean visión/golpes/disparos, pero no atenúan ruido.

Pasos: sprint 12 m, walk 5 m, crouch 3 m, slow walk 2 m, crouch + slow 0.8 m. Se emiten cada 1.5 m recorridos en suelo, con intervalo mínimo de 0.22 s. Sin emisiones por frame ni audio de pasos. No se emiten pasos quieto, muerto, en aire o durante mantle.

## Comprobación manual exacta

Hacer cada prueba aislada con **F9**, para no arrastrar la memoria auditiva/visual de la anterior. Controles de gunplay conservados: `1/2`, LMB, RMB, R, B, F1 optic, F2 muzzle, F3 magazine, F4 grip; Q/E lean, Ctrl crouch, Alt slow walk, Shift sprint.

1. **Inicio:** comprobar HP 100/100, rifle/pistola y attachments de 3B. Aparecen cuatro `DebugInfected` y dos `DebugRunner`, con labels de State/HP/target/destino. La pared frontal oculta al jugador. Esperar sin caminar/disparar: no deberían entrar en Chase por verte a través de ella. La zona inicial es cobertura, no invulnerabilidad ni barrera invisible.
2. **Visión:** rodear el borde izquierdo o derecho de la pared inicial (más allá de x ±6). Entrar delante de un infectado a menos de 18 m: pasa a Chase tras su reacción. No debería detectarte solo por distancia si estás fuera de su cono y no haces ruido audible.
3. **Persecución:** correr con W + Shift por el corredor. El normal persigue más lento; el runner se distingue por color, cuerpo más estrecho y mayor velocidad. Los destinos se actualizan cada 0.3 s, no cada frame.
4. **Romper LOS:** girar detrás de los divisores de los corredores o del obstáculo de búsqueda del fondo. Quedarse quieto y sin disparar. El infectado conserva brevemente el último punto visto, luego Investiga, gira al llegar para buscar y finalmente vuelve a Idle/Wander. Si sigues generando ruido o vuelves a entrar en su visión, puede renovar la búsqueda.
5. **Ruido normal sin visión:** F9, permanecer detrás de la pared y disparar una vez con el rifle hacia la pared. Los infectados en radio auditivo deben entrar en Investigate aunque no puedan verte. Se acercan a la posición del ruido proyectada sobre NavMesh; pueden pasar a Chase cuando alcancen un punto con LOS.
6. **Suppressor:** F9, mismo punto inicial, F2 una vez para Suppressor, disparar. Overlay 3B: Noise ×0.25; overlay de combate: último radio 10 m para rifle. Desde esa posición deberían reaccionar menos infectados —puede no reaccionar ninguno si todos están fuera de 10 m—. Comparar con un nuevo F9 sin suppressor. No comparar contra enemigos que ya te habían detectado.
7. **Pasos:** acercarse a un infectado oculto por cobertura sin disparar. Comparar caminar, sprint, Alt y Ctrl + Alt desde posiciones similares. El overlay muestra radio del último evento; no cambia hasta emitir un nuevo paso. Sprint debe poder alertar desde más lejos. Slow/crouch no reducen mágicamente la visión: el cono/LOS sigue aplicándose.
8. **Daño corporal:** F9, disparar al cuerpo de un normal. Rifle base hace 24: HP 48 → 24 → 0. Pistola base hace 28: 48 → 20 → 0. Comprobar reacción breve al impacto y mantenimiento del hitmarker.
9. **Headshot:** F9, disparar a la cabeza separada. Rifle 48 / pistola 56 de daño ya calculado por gunplay: mata al normal de 48 HP con un disparo. Runner 36 HP también cae de un headshot. No se multiplica el headshot una segunda vez en InfectedHealth.
10. **Stagger:** disparar un impacto corporal al normal de 48 HP: se detiene brevemente y después continúa. Para observar ráfagas sin matarlo inmediatamente, puede aumentarse temporalmente Max Health en el prefab antes de Play; el cooldown de stagger impide renovarlo continuamente. Restaurar el valor al terminar la prueba si se cambió.
11. **Muerte del infectado:** AI queda Dead, Agent deja de controlar, no vuelve a atacar, hitboxes se desactivan, cuerpo se inclina/cae y se vuelve gris. Desaparece después de unos segundos. No hay loot ni ragdoll humanoide.
12. **Ataque al jugador:** dejar acercarse a un normal. Debe parar, hacer windup y golpear; HP baja en bloques de 15, sin daño continuo por frame. Alejarse durante el windup o interponer una pared debe impedir el daño. Repetir con runner: bloques de 12.
13. **Feedback recibido:** comprobar pequeño kick de Bodycam y un flash neutro muy tenue (sin rojo intenso). No hay sonidos por defecto; solo hooks.
14. **Muerte del jugador:** dejar que HP llegue a cero. Input, motor, look y Firearm se bloquean inmediatamente, se cancela la recarga y el arma deja de estar utilizable. La cámara cae/inclina durante unos 2 s, termina baja y ladeada; fade desde mitad de secuencia hasta negro a los 3.2 s. Debe mostrar DEAD y la instrucción F9. Intentar disparar, cambiar arma, mover mouse, saltar, lean o recapturar cursor: no debe recuperar control.
15. **Colisiones de cámara:** repetir muerte de pie/agachado y cerca de coberturas. La caída es vertical desde la posición actual y cada tramo usa un spherecast; si hay un obstáculo debajo, debe apoyarse en él en vez de atravesarlo. En suelo libre termina aproximadamente a 18 cm del suelo; no hay teleport instantáneo.
16. **Reinicio:** pulsar F9 después de DEAD. Se recarga únicamente la escena debug, HP vuelve a 100, spawns/munición/attachments vuelven a sus valores iniciales. Repetir varias veces y revisar Console por referencias destruidas, agentes sin NavMesh o suscripciones duplicadas.
17. **Regresión 3B:** probar ADS + Q/E + Ctrl, low-ready de sprint, pared, flash, casquillos, impacts, reload y switching; siguen siendo los mismos componentes de Fase 3B.

No se automatiza la validación visual del NavMesh, cámara, efectos ni comportamiento en las esquinas. Seleccionar un infectado en Scene View y activar Gizmos permite revisar cono/rango visual, rango de ataque, radio auditivo y destino.

## Integración con raid real

`PlayerDeathSequence` acepta una referencia opcional a `GameManager`; si no está asignada busca el manager existente al comenzar la muerte. Captura la sesión activa mediante `RaidDeathCompletion`.

Al completar el fade, solo llama `GameFlow.FailRaid()` si sigue existiendo exactamente la misma sesión activa y el estado sigue siendo Raid. Es una operación protegida contra duplicados: no falla una sesión nueva ni vuelve a cerrar una raid. No escribe inventario, stash, loadout ni JSON directamente. GameFlow conserva la responsabilidad sobre la pérdida de lo desplegado.

`Infected_Test` no inicia una raid ni crea GameManager para evitar tocar el perfil real. Para validar esa frontera en Unity sin usar el perfil del jugador, ejecutar el test `RaidFailureRunsOnceAfterDeathAndNeverFailsANewSession`: crea su propio catálogo y guardado temporal. En una escena de raid real, añadir/asignar PlayerHealth y PlayerDeathSequence al jugador existente y comprobar GameManager.Flow.State = RaidFailed al finalizar.

## Archivos y responsabilidades

Nuevos en `Assets/Game/Runtime/Combat/`:

- `HealthState.cs`: vida, daño/heal, límites y eventos; sin dependencia de reloj/escena.
- `DamageableHealth.cs`, `PlayerHealth.cs`, `InfectedHealth.cs`: componentes compatibles con IDamageable. InfectedHealth no recalcula daño ni headshots.
- `DeathLifecycle.cs`: temporizador de muerte único y adaptación `RaidDeathCompletion` a GameFlow.
- `NoiseSystem.cs`: bus global con suscripciones liberables, filtro por radios y reglas de ruido.
- `PlayerNoiseEmitter.cs`: adaptador de los eventos de disparo 3B y estado/velocidad de PlayerMotor.

Nuevos en `Assets/Game/Runtime/Infected/`:

- `InfectedBrain.cs`: máquina de estados y parámetros comunes, tiempos explícitos y decisiones testeables.
- `InfectedController.cs`: visión por rango/FOV/LOS, último punto conocido, navegación y validación final del golpe.
- `InfectedPresentation.cs`: reacción al impacto, gesto de ataque y caída/desactivación placeholder.
- `InfectedSpawner.cs`: puntos de spawn, cantidad/límite y respawn opcional debug, sin EncounterDirector.
- `InfectedTestDebug.cs`: HP, labels de AI, radio del último ruido y reinicio F9 de la escena de prueba.

Otros nuevos:

- `Assets/Game/Runtime/Camera/PlayerDeathSequence.cs`: feedback de daño, cesión de control a través de `BodycamController.EnterDeathState/SetDeathPose`, caída con colisión, fade y hooks `Hurt`, `FinalImpact`, `TinnitusMuffle`, `DeathComplete`.
- `Assets/Game/Editor/FPS/InfectedTestEnvironment.cs`: prefab normal/runner, entorno, bake de navegación, wiring de componentes y registro de escena debug para F9.
- `Assets/Game/Tests/Editor/InfectedCombatTests.cs`: tests deterministas de salud, AI, ruido, ataque y muerte/raid.

Modificados, con alcance limitado:

- `DamageInfo.cs`: constructor para daño melee ya resuelto; se conserva el constructor de armas y su multiplicador de headshot.
- `GunplayTestEnvironment.cs`: nueva factory `CreateArmedPlayer` que reutiliza el player/viewmodels/attachments existentes sin abrir ni guardar otras escenas.
- `Outbreak.FPS.Editor.asmdef`: referencia a Unity.AI.Navigation ya instalada para el tooling de NavMeshSurface.

Sin cambios en PlayerMotor, PlayerInput, PlayerLook, BodycamController, FirearmController, contratos de attachments/stats efectivos, GameFlow, RaidSession, inventario ni persistencia. Las escenas validadas y Package/ProjectVersion se conservan. Commit `0a0b56c` y tag `phase-3b` no se modifican.

## Validación de código

Se compilaron runtime/editor/tests con Roslyn y las referencias de Unity 2023.2.20f1: Outbreak.Game, Outbreak.Bodycam, Outbreak.Game.Editor, Outbreak.FPS.Editor, Outbreak.Game.Tests y Outbreak.FPS.Tests. **Las seis compilaciones finales terminaron con código 0, sin errores ni advertencias**, con nivel de warnings 4 y logs de diagnóstico vacíos. Resultados, logs y script reproducible en `Temp/InfectedChecks/` (Unity puede limpiar Temp).

Además, `LogicSmoke` ejecuta código real de HealthState/InfectedBrain/NoiseBus/DeathLifecycle con Mono, sin iniciar Unity Editor ni construir escenas nativas. Pasaron **20 comprobaciones** de lógica gestionada. Esto no valida física, NavMesh, el componente de cámara ni la importación/generación de la escena.

Los **21 tests NUnit nuevos**, compilados pero pendientes de ejecución en Unity, incluyen daño/heal/clamp, muerte única, no daño postmortem, headshots sin doble multiplicación, transiciones AI, memoria, windup/cooldown, cancelación por LOS, stagger/cooldown, radios/suppressor/movimiento, ataque a PlayerHealth, secuencia única y FailRaid idempotente/protegido contra sesiones nuevas.

**Pendiente en la instancia funcional de Unity:** Test Runner → EditMode → Run All, y PlayMode → Run All para regresión FPS. No se ejecutó el Test Runner ni se abrió otra instancia del Editor por el antecedente de fallo de licencia. La compilación y las pruebas puramente gestionadas no equivalen a aprobar estas pruebas de Unity ni la validación manual anterior.

No se usó navegador, Playwright/Puppeteer, Blender, downloads, audio final, modelos finales, gore, loot de cadáveres, humanos hostiles, hordas ni sistemas fuera de Fase 4.
