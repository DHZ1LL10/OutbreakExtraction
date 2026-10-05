# OUTBREAK: EXTRACTION — Fase 3A

Implementación para Unity **2023.2.20f1 / URP**. Validación visual pendiente en Unity.

## Corrección de viewmodel — 2026-10-03

**Causa comprobada:** la escena guardada tenía `FirearmController.primary` y `secondary` en `{fileID: 0}`. Los viewmodels y sus materiales existían, pero `Awake` construía una selección sin armas; `LateUpdate → ShowViews(null)` desactivaba ambos. Esto explica la diferencia entre Scene View y Game View. La posible geometría gris mencionada no se identificó visualmente.

El generador cargaba las definiciones antes de `EditorSceneManager.NewScene(..., Single)`. El registro `Editor.log` muestra la descarga de assets sin uso justo después de importar las definiciones. Se movió la creación de escena antes de cargar shader/materiales/definiciones y se añadió validación de referencias persistentes antes de guardar. Se repararon directamente las referencias serializadas de `Gunplay_Test.unity`; no hace falta regenerarla para recibir la corrección.

- Hip local: `(0.16, -0.14, 0.46)` m. ADS local: `(0, -0.055, 0.46)` m. La punta del sight está a `+0.055` m, alineada con el eje de cámara en ADS sin recoil. Menor descenso durante cambio/recarga para conservar más geometría dentro de pantalla.
- Se mantiene `Player → ViewRoot → BodycamRig → Main Camera → WeaponRoot → DebugRifle / DebugPistol`. `PlayerLook` posee la rotación de ViewRoot; Motor, su altura; Bodycam, su rig/FOV; WeaponViewModel, solo la pose local del arma. Crouch/lean/bob/sway se heredan una sola vez. Firearm compone la pose en LateUpdate después de Bodycam.
- Near clip sigue en **0.04 m**; escalas de la cadena en `(1,1,1)`. Cámara y renderers URP incluyen la layer 2 de las primitivas; estas no tienen colliders. No se añadió otra cámara ni otro sistema de renderizado.
- La selección conserva los mismos WeaponRuntimeState. Se sincroniza la visibilidad al equipar y se comprueba explícitamente la identidad del slot secundario. Una configuración con viewmodels pero sin definiciones produce un aviso útil en vez de ocultarlos silenciosamente.
- Sin cambios en FPS_Test, PlayerMotor, PlayerInput, PlayerLook, BodycamController, ItemDefinition, Inventory, RaidSession ni guardado. Sin Fase 3B.

### Comprobación manual de presentación (ambas armas)

Abrir `Assets/Game/Scenes/Gunplay_Test.unity`, esperar la recompilación y entrar en Play. Para probar además el generador, salir de Play y ejecutar **Outbreak > Crear entorno de prueba Gunplay**; se abre una escena con nombre único. En Inspector, Player debe tener Primary = DebugRifle, Secondary = DebugPistol y ambos View asignados. Hacer clic en Game View para capturar cursor.

1. **Rifle:** pulsar `1`; debe verse el cuerpo largo, cañón y cargador inferior; overlay `DebugRifle`, inicialmente `30 / 180`.
2. **Pistola:** pulsar `2`, esperar 0.3 s; debe verse el cuerpo corto y empuñadura, sin el cañón largo/cargador del rifle; inicialmente `15 / 75`.
3. **Cambio/munición:** con el rifle en semi (`B` si marca Automatic), disparar tres veces; cambiar a pistola y disparar dos. Alternar `1/2`: deben conservar `27 / 180` y `13 / 75`. Bajo WeaponRoot, solo el objeto del arma equipada debe estar activo, incluso durante el cambio; el overlay debe coincidir.
4. **Hip:** soltar RMB; revisar ambas armas quieto y caminando. Deben quedar abajo/derecha, con silueta reconocible, sin un bloque enorme tapando el centro. Probar Game View a 16:9 y 4:3.
5. **ADS:** mantener RMB y soltar varias veces; transición suave en posición/FOV, punta del sight aproximadamente en el centro al estabilizarse. Sin saltos, desaparición ni paso detrás de cámara. Repetir al cambiar arma manteniendo RMB.
6. **ADS + lean:** mantener RMB y alternar `Q/E`, primero lejos de paredes. Cámara y arma deben inclinarse juntas, sin separación lateral de la mira; al soltar debe recuperar su pose.
7. **ADS + crouch:** mantener `Ctrl` y RMB, caminar y disparar; debe bajar el conjunto y conservarse la alineación. Soltar Ctrl debe recuperar altura sin saltos del arma.
8. **Recoil:** disparos individuales con pistola y ráfagas cortas con rifle en auto; debe haber kick atrás/arriba y recuperación. Repetir en ADS agachado; no debe atravesar el near plane ni acumular un desplazamiento permanente.
9. **Reload:** con cargador parcial, pulsar `R`; debe bajar/inclinarse suavemente, bloquear disparo, volver a su pose y transferir solo la munición necesaria. RMB sostenido debe recuperar ADS al terminar. Cambiar durante recarga debe cancelarla sin transferir munición.
10. **Clipping de cámara:** en espacio libre, combinar hip/ADS, mirar arriba/abajo, Q/E, Ctrl, recoil, recarga y cambios rápidos con ambas armas. No deben aparecer caras cortadas por el near plane ni desaparecer el cuerpo del arma. Verificar en Inspector que Near siga en 0.04 y WeaponRoot en posición/rotación cero y escala uno. Junto a paredes, comprobar aparte que el disparo obstruido no dañe a través de ellas; las primitivas todavía pueden intersectar geometría del mundo a contacto, como en el alcance original de 3A.

**Cierre:** código corregido y compilado; presentación lista para validar. No marcar la validación manual como aprobada hasta completar estos diez puntos y revisar Console. El cierre visual depende de esa comprobación, no de la compilación.

## Abrir y probar

1. Abrir este proyecto en Unity 2023.2.20f1. Esperar a que termine la importación/compilación.
2. Fuera de Play: **Outbreak > Crear entorno de prueba Gunplay**. Si hay cambios sin guardar en otra escena, atender el diálogo normal de Unity.
3. Se genera y abre `Assets/Game/Scenes/Gunplay_Test.unity`. Si ya existe, se usa un nombre único. `FPS_Test` se conserva.
4. Pulsar **Play**. Ambas armas comienzan equipadas en sus slots; no hay que recoger objetos. El rifle es el arma inicial.
5. `Escape` libera el cursor; clic izquierdo lo captura. El clic de captura no dispara.

| Control | Acción |
| --- | --- |
| Mouse izquierdo | Fuego; mantener para automático |
| Mouse derecho, mantener | ADS |
| R | Recarga |
| 1 | Rifle / Primary |
| 2 | Pistola / Secondary |
| B | Alternar semi/auto del rifle; pistola permanece semi |
| WASD / Shift / Ctrl / Alt | Movimiento / sprint / crouch / slow walk existentes |
| Q / E / Space | Lean / salto o mantle contextual existentes |

### Secuencia manual

1. **Pistola (`2`)**: esperar el cambio de 0.3 s. Disparar desde hip; mantener LMB debe producir un solo disparo. Soltar y volver a pulsar permite otro. Hacer ADS: comprobar transición de FOV, alineación de sights, menor spread/bob/sway y retorno a hip. Cada clic debe consumir exactamente una bala y mostrar kick del arma y del aim.
2. Disparar 15 veces, respetando la cadencia. En `0 / 75`, más clics deben mostrar `EMPTY MAGAZINE`, sin daño ni recoil. `R`: esperar 1.65 s; resultado `15 / 60`. Disparar unas balas y repetir: solo se descuenta la cantidad necesaria de reserve.
3. **Rifle (`1`)**: mantener fuego en auto, desde hip y ADS. El recoil se acumula moderadamente y permite compensación con mouse. `B` cambia a semi; mantener LMB ya no repite. Volver a auto. Vaciar cargador, recargar 2.2 s y verificar 30 balas.
4. Disparar quieto, caminando, con `Ctrl`, `Alt`, `Q/E` y durante salto. Verificar el aumento de spread en movimiento/aire y reducción agachado. Ambas armas bloquean fuego durante sprint.
5. A la izquierda hay cobertura vertical para lean y una caja de **0.8 m** para mantle. Acercarse a la caja de frente y usar `Space` con intención de avanzar, como en FPS_Test. Mantener fuego con rifle durante el mantle: no debe consumir munición mientras el overlay FPS marque `Mantling: True`.
6. A la derecha hay cobertura baja y techo con **1.32 m** de paso para crouch. Recargar mientras caminas, haces crouch/lean y sprint: el movimiento sigue disponible y la recarga continúa. Cambiar de arma durante recarga la cancela sin transferir munición.
7. Las dianas tienen cuerpo azul y cabeza naranja. Probar cerca (~6 m), medio (~18 m), lejos (~42 m) y el carril lateral. Pistola: **28 body / 56 head**. Rifle: **24 body / 48 head**. El texto y Console muestran daño, HP y `HEADSHOT`; el hitmarker de cabeza es amarillo. HP inicial 150; tras llegar a cero se reinicia a los 2 s.
8. Disparar parcialmente con cada arma, alternar `1/2` y verificar que cada una conserva cargador, reserva y modo. Durante `Switching: True` no debe disparar.
9. Pegar el arma a una cobertura y apuntar junto a su borde: aunque la cámara alcance a ver el target, el muzzle bloqueado no debe dañarlo a través de la pared.
10. Revisar Console: se esperan logs de daño, sin errores/excepciones. Detener Play para restaurar la munición de debug.

## Archivos y responsabilidades

Todos los archivos runtime nuevos están en `Assets/Game/Runtime/Weapons/`:

| Archivo | Responsabilidad |
| --- | --- |
| `WeaponDefinition.cs` | ScriptableObject de configuración; referencia al `ItemDefinition` existente y a un item `Ammo`. Enums de clase y modo. |
| `WeaponRuntimeState.cs` | Munición, trigger semi/auto, cooldown, recarga, ADS y eventos. Incluye `WeaponSelection` para conservar estados entre Primary/Secondary. |
| `FirearmController.cs` | Integración con input/motor/look, bloqueos, selección, spread, hitscan y feedback. `EquipFromRaid` resuelve `RaidSession.Equipment` por el ID existente. |
| `DamageInfo.cs` | Contratos `IDamageable`, `DamageInfo`, `HitZone` e `IWeaponCameraFeedback`. |
| `DamageHitbox.cs` | Zona Body/Head/Limb y resolución del receptor en el padre. |
| `WeaponViewModel.cs` | Pose hip/ADS, kick visual, pose debug de recarga/cambio y muzzle. |
| `TargetDummy.cs` | HP de diana, flash, texto/log y reset opcional. No es Health del jugador. |
| `WeaponDebugOverlay.cs` | Arma/ammo/modo/ADS/recarga, crosshair, hitmarker y dry fire; con interruptores en Inspector. |

También se crearon:

- `Assets/Game/Editor/FPS/GunplayTestEnvironment.cs`: menú, escena, materiales, definiciones y geometría mediante APIs de Editor.
- `Assets/Game/Tests/Editor/GunplayTests.cs`: **21 casos deterministas** de munición, cadencia, semi/auto, recarga, bloqueos, selección y daño por zona.
- Metadatos `.meta` de los scripts/carpeta nuevos.

Cambios acotados a sistemas existentes:

- `PlayerInput.cs`: concentra todos los controles nuevos; limpia sus estados al perder control/foco.
- `PlayerLook.cs`: `AddAimRecoil`, offset recuperable compuesto con el mouse. Sigue siendo el único dueño de la orientación del aim.
- `BodycamController.cs`: implementa `IWeaponCameraFeedback`; compone ADS, FOV, reducción de bob/sway y microkick en su propio cálculo.
- `FPSTestEnvironment.cs`: extrae su construcción de jugador a `CreatePlayer(position)` para reutilizar exactamente el mismo setup. Su menú y escenario se conservan.

`PlayerMotor`, items, inventario, Loadout, RaidSession y JSON no se modificaron.

## Arquitectura y decisiones

- Identidad: `WeaponDefinition.WeaponId` y `DisplayName` provienen del `ItemDefinition` referenciado. No hay ID de arma paralelo ni segundo inventario. Los slots de raid se resuelven a definiciones mediante un catálogo suministrado al controlador.
- Los ScriptableObjects contienen configuración. Cada slot tiene su propio `WeaponRuntimeState`, incluso si ambos usan la misma definición. No se recrea el estado al alternar.
- Munición de reserva local para esta fase. La relación con un item de tipo Ammo y la inicialización por raid permiten agregar después un adaptador de consumo/persistencia; no se consume ni escribe el inventario/JSON actualmente. La identidad de tipo ya persiste mediante el sistema existente; la munición por instancia aún no se serializa.
- Las futuras modificaciones por attachments pueden resolverse sobre la definición referenciada sin cambiar identidad ni estado del inventario. No se añadieron attachments, slots de accesorios ni estadísticas de accesorios.
- `Update`: acciones después del motor; `LateUpdate`: disparo después de Bodycam, usando la vista final de ese frame. Aim ray con spread → target → muzzle hacia target → raycast definitivo. Primero se comprueba el segmento cámara–muzzle para evitar cruzar una pared cercana. Se ignoran colliders del propio jugador y triggers.
- `Player → ViewRoot → BodycamRig → Camera → WeaponRoot → DebugRifle/DebugPistol`. El viewmodel solo escribe su propio transform; Bodycam controla FOV/cámara. El recoil real entra exclusivamente por `PlayerLook.AddAimRecoil`.
- ADS reduce bob a 65% y sway a 40%; se interpola hip/ADS spread según la transición. El movimiento multiplica spread según velocidad, crouch lo reduce y aire lo incrementa.
- Recoil horizontal de oscilación pequeña determinista y vertical progresivo del rifle; no patrón avanzado. La compensación del mouse es independiente de la recuperación del offset.
- Recargar baja el arma y sale de ADS. Disparo bloqueado durante recarga. Sprint no cancela recarga. Cambiar/desequipar sí la cancela sin transferencia. Mantle bloquea fuego, ADS e inicio de recarga; una recarga ya activa puede continuar.
- Pistola: 15, 360 RPM semi, 1.65 s, 75 reserve. Rifle: 30, 600 RPM auto/semi, 2.2 s, 180 reserve. Una caída de FPS no genera ráfagas de catch-up para recuperar disparos atrasados.
- No se necesita pickup en la escena: se suministran ambas armas equipadas para aislar la prueba de gunplay.
- La distorsión de lente se desactiva solo en el nuevo entorno para alinear sights/crosshair exactamente. El resto de Bodycam permanece activo.
- Assets configurables generados en `Assets/Game/DebugContent/Gunplay/`. Regenerar escena conserva los valores que hayas ajustado en esos assets. `FirearmController` expone reservas y tiempo de cambio en Inspector. No hay modelos, sonidos ni assets externos.
- Los viewmodels son placeholders y pueden recortar visualmente una pared a muy corta distancia; el trayecto de daño sí verifica la obstrucción. Su apariencia, comodidad y sensación requieren la revisión manual indicada arriba.

## Validación realizada y pendiente

Se revisaron los scripts y se compilaron con Roslyn y las referencias instaladas de Unity 2023.2.20f1:

- `Outbreak.Game` — runtime, incluido gunplay.
- `Outbreak.Bodycam` — runtime URP.
- `Outbreak.Game.Editor` — setup existente.
- `Outbreak.FPS.Editor` — generadores FPS y Gunplay.
- `Outbreak.Game.Tests` — arquitectura, los 21 casos originales de gunplay y cuatro casos de regresión de escena/viewmodel.
- `Outbreak.FPS.Tests` — tests FPS existentes.

Las seis compilaciones de esta corrección terminaron sin errores ni advertencias, con Roslyn de Unity y nivel de warnings 4. Respuestas de compilación, DLLs y script reproducible en `Temp/GunplayViewmodelChecks/` (artefactos temporales; Unity puede limpiar Temp). Las referencias entre ensamblados Outbreak apuntan a las DLLs recién compiladas, no a sus versiones anteriores en Library.

La auditoría no visual `python Temp/GunplayViewmodelChecks/audit_scene.py` pasó: referencias de assets y scripts, IDs únicos, poses serializadas, estado activo inicial, jerarquía, escalas, layer incluida y near clip. Los nuevos tests EditMode cubren referencias persistentes, correspondencia entre selección/visibilidad y conservación de ammo, y composición local ADS con lean/crouch/recoil para ambas armas. Fueron compilados, **no ejecutados**.

**No se ejecutaron los tests ni Play en esta sesión.** El registro previo `Temp/FPSPolishChecks/unity-tests.log` muestra un fallo de licencia/IPC (salida 199). No se lanzó otra instancia de Unity. Compilar no equivale a pasar los tests ni verificar la importación/escena visual.

En la instancia funcional de Unity: **Window > General > Test Runner → EditMode → Run All** (arquitectura + Gunplay); luego **PlayMode → Run All** (FPS). Ejecutar después la secuencia manual anterior. Los 16/16 originales son el estado validado que proporcionó el usuario, no una nueva ejecución en esta sesión.

No se usó navegador, no se descargaron assets y no se hizo validación visual automática. En esta corrección se editaron únicamente las dos referencias de definición y las poses de los dos viewmodels en el YAML de Gunplay_Test. No se modificó FPS_Test ni se avanzó a Fase 3B.
