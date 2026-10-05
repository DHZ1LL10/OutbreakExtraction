# OUTBREAK: EXTRACTION — Fase 3B

Unity **2023.2.20f1 / URP**. Implementación de weapon feel y attachments; sin zombies ni Fase 4. Validación visual manual pendiente.

## Abrir el entorno

1. Abrir el proyecto en Unity 2023.2.20f1 y esperar a que termine de importar/compilar.
2. Fuera de Play, ejecutar **Outbreak > Crear entorno de prueba Gunplay 3B**.
3. Se crea y abre `Assets/Game/Scenes/Gunplay_3B_Test.unity` (nombre único si ya existe). Se generan los cinco attachments, sus ItemDefinitions, materiales y prefabs de primitivas en `Assets/Game/DebugContent/Gunplay/Attachments/`.
4. Pulsar Play y hacer clic en Game View. `Escape` libera el cursor.

La generación conserva las definiciones/materiales/prefabs que ya existan, para respetar los ajustes del Inspector. No sobrescribe `Gunplay_Test` ni `FPS_Test`. El menú Gunplay original sigue disponible. La escena 3A existente funciona con sus referencias actuales; para sockets, F1–F4 y efectos, usar la nueva escena 3B.

## Controles y resultados

| Control | Acción |
| --- | --- |
| 1 / 2 | Rifle / pistola; estado independiente por arma |
| LMB / RMB | Disparar / mantener ADS |
| R / B | Recargar / alternar semi-auto en rifle |
| WASD / Shift / Ctrl / Alt | Movimiento / sprint / crouch / slow walk |
| Q / E | Lean existente |
| F1 | Toggle Red Dot |
| F2 | None → Suppressor → Compensator → None |
| F3 | Toggle Extended Magazine |
| F4 | Toggle Vertical Grip; compatible solo con rifle |

F1–F4 pertenecen a `DebugAttachmentController`, añadido únicamente al entorno 3B. Rechaza cambios durante recarga/switch y muestra el motivo. PlayerInput no fue modificado.

| Attachment | Modificadores por defecto | Visual / ancla |
| --- | --- | --- |
| Red Dot | ADS spread ×0.85 | Marco, punto rojo sin lente y `ADSAnchor` |
| Suppressor | Noise ×0.25; flash ×0.12; recoil vertical ×0.95 | Cilindro de 18 cm y `MuzzleTip` en su extremo |
| Compensator | Recoil vertical ×0.72; noise ×1.2; flash ×1.3 | Dispositivo corto y su propio `MuzzleTip` |
| Extended Magazine | Capacidad ×1.5; duración de recarga ×1.1 | Cargador largo; sustituye visualmente el cargador base del rifle |
| Vertical Grip | Recoil vertical ×0.85; horizontal ×0.8; hip spread ×0.9; ADS speed ×0.92 | Empuñadura delantera, solo rifle |

Capacidad: rifle 30 → 45; pistola 15 → 22 (redondeo al entero más cercano, empate al par). Equipar no rellena el cargador. Quitar devuelve únicamente el excedente a reserva. El conjunto compensator + grip produce recoil vertical ×0.612 respecto a base.

## Validación manual exacta

Usar primero 16:9 y repetir presentación a 4:3. El overlay de gunplay está arriba/derecha; el de movimiento permanece a la izquierda. Los datos son de debug, no HUD final.

1. **Rifle:** `1`, comprobar cuerpo largo, cañón y cargador. Overlay inicial `DebugRifle`, `30 / 180`, cap 30. Disparar al target de enfrente: daño, headshots e hitmarker siguen funcionando.
2. **Pistola:** `2`, comprobar silueta corta y overlay `15 / 75`, cap 15. Mantener LMB debe disparar una vez; soltar/pulsar permite el siguiente disparo.
3. **Sway hip:** mover mouse horizontal/vertical, hacer A/D, avanzar y frenar. El arma debe tener una inercia pequeña y suave mientras la cámara responde inmediatamente. Detenerse: vuelve a reposo sin deriva.
4. **ADS:** mantener RMB y repetir movimientos. El sway debe reducirse mucho; las miras base se centran al estabilizarse. Soltar RMB vuelve suavemente a hip.
5. **Sprint:** W + Shift. El arma baja/rota a low-ready; LMB no consume ammo. Soltar Shift: el arma recupera pose; el fuego sigue bloqueado durante la recuperación breve. En semi, soltar y volver a pulsar LMB después del bloqueo.
6. **Pared:** acercarse a la cobertura vertical de la izquierda o a una pared lateral. El arma retrocede/baja progresivamente; `Obstructed` cambia a True y no se consume ammo. Retroceder: recupera pose y False sin oscilación continua.
7. **Pared + ADS:** mantener RMB mientras se aproxima. ADS debe desactivarse cuando no hay espacio; FOV/pose recuperan hip. Al alejarse, RMB sostenido vuelve a ADS. Repetir con Ctrl y Q/E junto a un borde. No debe dañarse un target a través de la pared.
8. **Red Dot:** en espacio libre, `F1`. Debe aparecer una sola mira en OpticSocket; RMB alinea el punto rojo con el centro al estabilizarse. Q/E y Ctrl no separan cámara y arma. `F1` retira la mira y devuelve ADS a sights base. Alternar también mientras se mantiene RMB: transición suave de altura.
9. **Suppressor:** `F2` desde None. Debe aparecer el cilindro y marcar Noise ×0.25 / Flash ×0.12. Disparar: flash mucho menor y situado al extremo del suppressor. La expulsión sigue saliendo del costado del receiver. Acercarse a pared: el arma larga debe empezar a evitarla antes.
10. **Compensator:** otro `F2`. Debe desaparecer el suppressor y aparecer el dispositivo corto. Recoil vertical ×0.72, Noise ×1.2 y Flash ×1.3; comprobar con ráfagas comparables. Otro `F2` vuelve a None.
11. **Extended mag sin crear balas:** comenzar un Play nuevo para usar números exactos. Con rifle intacto `30 / 180`, `F3` cambia cap a 45 pero sigue `30 / 180`. `R`: después de 2.42 s debe quedar `45 / 165`.
12. **Retirar extended mag:** `F3` después de esa recarga: vuelve a cap 30 y `30 / 180`. Alternar varias veces no incrementa el total de balas. Con cargador parcial menor de 30, retirarlo no añade reserva.
13. **Pistola extended:** `2`, `F3`: sigue `15 / 75`, cap 22. Recargar: `22 / 68`; quitar: `15 / 75`. El rifle conserva su configuración.
14. **Grip:** rifle, `F4`: aparece debajo del guardamanos; baja recoil V/H y ADS entra un poco más lento. `F4` retira y restaura stats. En pistola `F4` muestra incompatibilidad, sin alterar el arma.
15. **Configuraciones independientes:** dejar rifle con Red Dot + Suppressor y pistola con Compensator + Extended Magazine. Alternar `1/2` varias veces: conservar attachments, ammo/reserva y modo de cada arma. En reposo solo el viewmodel seleccionado está activo.
16. **Flash:** disparar en hip y ADS, con None/Suppressor/Compensator. Debe durar aproximadamente 0.035 s, ser pequeño, variar levemente y no quedar prendido. Para apreciar la luz, reducir manualmente la intensidad del Directional Light durante Play; no se altera el asset de iluminación.
17. **Casquillos:** observar a la derecha al disparar. Salen desde EjectionPoint, giran, caen y desaparecen en unos 2.2 s. El rifle usa casquillo más largo. En Hierarchy, `Gunplay temporary effects (bounded pool)` mantiene 24 shells, 24 impactos y un flash; las ráfagas no crean objetos permanentes adicionales.
18. **Impactos:** disparar contra pared/suelo desde suficiente distancia: marca/burst amarillo breve. Disparar a los targets: cyan y hitmarker existente. Desaparecen en 0.4 s. Los casquillos son balísticos simples con Rigidbody/gravedad, sin rebotes ni colisiones de gameplay.
19. **Reload/switch polish:** recargar de pie/agachado, en hip y manteniendo RMB. El arma baja/inclina, hace un movimiento pequeño y regresa sin saltos. `1/2`: la anterior baja durante la primera mitad, cambia la visibilidad una vez y la nueva sube durante la segunda; tiempo total 0.3 s. Durante la primera mitad, el overlay identifica el slot de destino mientras aún baja el visual anterior; no puede disparar.
20. **Regresión:** cambiar durante reload cancela sin transferir ammo. Probar sprint, mantle, lean y crouch existentes. Mirar arriba/abajo combinando ADS/recoil/recarga/obstrucción: revisar que el receiver no cruce el near plane (sigue 0.04 m). A contacto, la parte delantera puede bajar fuera de pantalla como low-ready; la evitación es aproximada, no una simulación de colisión de malla.
21. **Audio nulo y Console:** los cinco AudioClips son null por defecto. Disparar, vaciar cargador, recargar y cambiar arma deben permanecer silenciosos y sin NullReferenceException. No hay sonidos generados.

## Arquitectura y frontera futura

- `AttachmentDefinition.item` referencia el `ItemDefinition` existente de tipo Attachment; `ItemId` se deriva de ese item. No hay inventario, identidad ni persistencia paralelos.
- `WeaponDefinition.supportedAttachmentSlots` declara compatibilidad. Un attachment puede restringir clases y una lista opcional de IDs de arma. Slots iniciales: Optic, Muzzle, Magazine, Grip. Los valores de enum son estables; otros slots se añadirán cuando exista su funcionalidad.
- Cada `WeaponRuntimeState` posee su `AttachmentSet` y snapshot `EffectiveWeaponStats`. Los datos base no se escriben. Fórmula: `(base + suma de aditivos) × producto de multiplicadores`; se recalcula desde cero, no acumulando resultados previos.
- APIs: `TryEquipAttachment`, `TryRemoveAttachment`, `TryApplyAttachments`. Aplicar un conjunto completo es atómico: si alguna entrada no es compatible o hay dos del mismo slot, no modifica nada. Todas reconcilian capacidad/ammo y notifican `OnAttachmentsChanged` / `OnAmmoChanged`.
- Para futuro RaidSession/workbench/save: `Attachments.ExportItemIds()` entrega IDs existentes. El adaptador externo resolverá esos IDs con su catálogo y llamará `TryApplyAttachments` en `PrimaryState` / `SecondaryState`. Esta fase no añade propiedad de items al inventario ni conecta guardado/stash.
- `WeaponViewModel` posee solamente su transform. Sockets y anclas son datos del prefab; no hay una longitud de suppressor hardcodeada en runtime. `AttachmentVisual.adsAnchor` dirige ADS; `muzzleTip` sustituye al muzzle base. Los prefabs pueden reemplazarse por modelos sin cambiar el cálculo de stats.
- La evitación usa dos spherecasts y overlaps filtrando el Player. Son consultas sencillas con buffers limitados; al saturarse se bloquea conservadoramente. Se sondea la pose no retraída para evitar ciclos de acercar/retirar arma. El bloqueo adicional consulta el muzzle efectivo real. No altera PlayerLook ni Bodycam.
- `OnShotFeedback` expone posición, muzzle, ejection pose, aim y snapshots de Noise/Flash. Es la frontera para futuro NoiseSystem/VFX. `OnImpact` diferencia mundo/damageable. `WeaponEffects` es un consumidor reemplazable con pools y limpieza al destruirse.
- `WeaponAudio.Resolve` resuelve fire/suppressedFire/dryFire/reload/equip. `OnAudioCue` ofrece cue/clip/state incluso cuando el clip es null. `PlayOptional` ignora fuente/clip null. Un AudioSource opcional reproduce únicamente clips que se asignen posteriormente.

## Archivos

Nuevos runtime, dentro de `Assets/Game/Runtime/Weapons/`:

- `AttachmentDefinition.cs`: slots, compatibilidad, referencia a ItemDefinition y modificadores.
- `AttachmentSet.cs`: conjunto runtime y EffectiveWeaponStats.
- `AttachmentVisual.cs`: anclas del prefab de attachment.
- `WeaponFeedback.cs`: contratos de feedback de disparo/impacto.
- `WeaponEffects.cs`: flash, luz breve, casquillos e impactos con pools limitados.
- `WeaponAudio.cs`: selección de clips y reproducción opcional segura.
- `DebugAttachmentController.cs`: F1–F4 exclusivamente para la prueba 3B.

Modificados runtime: `WeaponDefinition.cs`, `WeaponRuntimeState.cs`, `WeaponViewModel.cs`, `FirearmController.cs`, `WeaponDebugOverlay.cs`.

Editor: nuevo `Assets/Game/Editor/FPS/GunplayAttachmentContent.cs`; modificado `GunplayTestEnvironment.cs` para el menú 3B y los sockets. Tests: nuevo `Assets/Game/Tests/Editor/AttachmentTests.cs`. Los scripts nuevos incluyen sus `.meta`.

No se editaron las escenas existentes, PlayerMotor, PlayerInput, PlayerLook, BodycamController, FPSTestEnvironment, ItemDefinition, Inventory, RaidSession, persistencia ni ProjectVersion.

## Validación de código

Compilación externa con Roslyn y referencias instaladas de Unity 2023.2.20f1: Outbreak.Game, Outbreak.Bodycam, Outbreak.Game.Editor, Outbreak.FPS.Editor, Outbreak.Game.Tests y Outbreak.FPS.Tests. **Las seis compilaciones finales terminaron con código 0, sin errores ni advertencias** (seis logs de diagnóstico vacíos). Artefactos y script reproducible en `Temp/Gunplay3BChecks/`; nivel de warnings 4. Incluye todos los scripts nuevos y referencia las DLLs Outbreak recién compiladas.

Los **24 casos deterministas nuevos** cubren slots/clases/IDs, reemplazo por slot, equip/remove, recoil/spread aditivo y multiplicativo, ADS/reload efectivos, capacidad de ambas armas, conservación de ammo y toggles repetidos, suppressor/compensator/grip, selección independiente, aplicación atómica, valores numéricos inválidos, bloqueo de disparo y clips nulos.

No se ejecutaron tests dentro de Unity ni se inició otra instancia del editor, por el antecedente de fallo de licencia. En la instancia funcional: **Window > General > Test Runner → EditMode → Run All**, luego **PlayMode → Run All** para la regresión FPS. Compilar los tests no significa haberlos ejecutado. No hay tests visuales de partículas/sway ni validación visual automática.

No se usaron navegador, Playwright/Puppeteer, Blender ni assets externos. No hay audio final, manos, zombies, AI, scopes magnificados, flashlight, laser, penetración, armadura ni balística con proyectiles de daño.
