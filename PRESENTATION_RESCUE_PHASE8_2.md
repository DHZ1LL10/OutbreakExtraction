# Fase 8.2 — Presentation rescue pass

## Auditoría y decisiones

- Unity 2023.2.20f1 / URP 16.0.6. PC usa High Fidelity: Render Scale **1**, MSAA **4×**, HDR, anisotrópico forzado, texturas a resolución completa, sombras suaves de 4096 con cuatro cascadas y distancia 150 m. Los otros dos perfiles también usan Render Scale 1, pero sin MSAA. No hay código propio reduciendo resolución/calidad.
- Hub/Raid tienen una cámara base, sin stack, sin resolución dinámica, near clip **0.04 m**. Postprocesado estaba habilitado, AA de cámara deshabilitado. El renderer ya incluye SSAO. Se añade **SMAA High** en ambas cámaras; se conservan los perfiles globales para no cambiar las otras escenas.
- No se encontró una causa de submuestreo interno que justifique atribuir toda la pixelación a URP. La resolución efectiva y el zoom de la ventana Game View no se han inspeccionado visualmente. Para presentar: **1920×1080, escala de Game View 1×**, o maximizar Play; no ampliar una salida pequeña. Las texturas fuente, en especial el zombie de 512 px, siguen limitando detalle cercano.
- FOV vertical hip antes 75°, ahora **79°**; sprint conserva +4°, ADS conserva **55° rifle / 58° pistola**. Bodycam conserva sus transiciones, lean, impactos y caída. Hub conserva distorsión leve (-0.06); Raid la tenía desactivada y permanece así.
- AKM tenía ~0.84 m de longitud y raíz a 0.40 m, acercando demasiado la culata. Modelo y sockets ahora se escalan juntos a **82%**, con raíz hip/ADS a **0.52 m**. Rifle hip (0.20,-0.18,0.52); pistola (0.15,-0.17,0.52). Se conservan raíz a escala 1, anchors, rotaciones de importación, visor, muzzle efectivo y casquillos. FirearmController y WeaponViewModel no se modifican.
- Infectados: escala y animación actuales conservadas, sin root motion. Sus variantes de presentación ahora usan un material URP Lit derivado, con la textura original, para recibir luz y sombra; material vendor intacto. Head hitbox sigue al hueso como antes.

## Presentación y archivos

- `Assets/Game/Runtime/PresentationMode.cs`: F10 alterna diagnóstico; OFF al entrar en Hub/Raid. Las escenas de diagnóstico independientes mantienen su comportamiento.
- `Assets/Game/Runtime/TacticalUI.cs`: tema oscuro, semitransparente, tipografía y escala proporcional a referencia 1280×720. Implementado con IMGUI estilizado para reutilizar los adaptadores existentes, sin añadir frameworks ni un segundo inventario. Traduce nombres de presentación AKM/Pistol1/attachments sin cambiar IDs ni saves.
- `HubDebugUI.cs`: pantallas propias de Stash (con loadout lateral), Loadout, Quartermaster BUY/SELL con feedback, Workbench, Commander y Deploy; TAB consulta equipo. Los nombres de clase se conservan para mantener referencias serializadas.
- `RaidLoopDebugUI.cs`: TAB Inventory/Loadout, inspección/descarte, lista de contenedor con scroll, misión compacta, extracción/resultados. ESC/TAB cierra. Acciones de retorno permanecen por encima del fade de muerte.
- `WeaponDebugOverlay.cs`, `PlayerDebugOverlay.cs`, `InfectedTestDebug.cs`: diagnóstico condicionado por Presentation Mode. Crosshair y hitmarker conservados; arma/HUD no se dibuja sobre menús o tras deshabilitar control.
- `PlayerInput.cs`: única dependencia de control: opción serializada `blockMovementWhileSuppressed`, activa solamente en Hub/Raid. La supresión existente sigue controlando ratón/disparo/cursor. Las escenas anteriores conservan la opción por defecto false.
- `PresentationVisualPass.cs`: guarda cámara/poses/modo, reduce carteles, recompone bordes de calle, reparaciones de asfalto, perímetro continuo, señal de suministros y props apoyados en suelo. No reconstruye gameplay. Ambientación Trilight, gradación Neutral, contraste +8/saturación -7/exposición +0.12; sin añadir bloom, blur ni efectos intensos.
- Escenas guardadas: `Hub_Test.unity`, `RaidLoop_Test.unity`. Assets en `Assets/Game/DebugContent/Presentation`: perfiles SafehouseGrading/RaidGrading, materiales Road repairs/Infected lit, variantes PresentationInfected/Runner y NavMesh regenerado. Incluyen sus `.meta`.
- `HubSceneFlowTests.cs`: amplía la integración existente con Presentation Mode/F10, supresión del menú y FOV inicial. No se crean decenas de tests.

## Validación técnica

- Authoring headless en copia aislada: compilación runtime/editor/tests y guardado de escenas, salida 0. Log final `Logs/RescueFinalAuthoring.log`, sin errores de compilación ni excepciones.
- **125/125 EditMode**: `Logs/RescueEditTests.xml`.
- **1/1 integración PlayMode**: `Logs/RescueSceneTests.xml`. Hub → equipar/optic → raid → 6 infectados con Animator y head hitboxes → misión/extracción → recompensa → muerte/fallo → Hub → persistencia. También comprueba F10 y apertura/cierre de TAB mediante su adaptador.
- NavMesh rebakeado; rutas completas comprobadas desde spawn a todos los puntos de infectados, cinco contenedores y extracción. No se automatiza validación visual.
- Los logs headless pueden incluir mensajes de shaders incompatibles con el dispositivo gráfico nulo y el aviso inicial conocido del cliente de licencias; las ejecuciones resolvieron entitlements y terminaron correctamente. No se ha borrado la Console de la instancia interactiva.
- FPS_Test, Gunplay_Test/Gunplay_3B, Infected_Test, contratos de daño, economía, quests, AI, loot, FirearmController y persistencia permanecen sin cambios.

## Comprobación manual para la presentación

1. Fuera de Play, reabrir `Assets/Game/Scenes/Hub_Test.unity` para cargar la versión guardada. No hace falta regenerar. Game View 1920×1080, escala 1× o maximizado; Play.
2. Confirmar vista abierta y ausencia de paneles debug. F10 muestra diagnóstico; F10 lo oculta. Caminar, sprint, Q/E y crouch conservan bodycam.
3. TAB: Inventory/Loadout legibles; WASD, ratón y disparo bloqueados mientras está abierto. TAB/ESC cierra y restaura control sin disparo involuntario. El mundo no se pausa.
4. Stash con E: inspeccionar, equipar AKM en Primary/Pistol1 en Secondary y devolver a stash. Ver cantidad, rareza, valor y capacidad.
5. Quartermaster: BUY y SELL, comprobar saldo y mensajes de compra/venta; sin fondos o con stash lleno muestra motivo. Las transacciones guardan con la lógica anterior.
6. Workbench: elegir AKM/Pistol; cambiar Red Dot, Suppressor/Compensator, Extended Magazine y Grip compatible. Commander: aceptar misión disponible o revisar su estado. Deploy: verificar objetivo/Normal y START RAID.
7. En raid, 1/2: ambas armas dejan más espacio. RMB: verificar alineación con/sin Red Dot; Q/E y crouch en ADS. Disparar, recargar y cambiar arma; comprobar flash, casquillos y suppressor. Acercarse a pared confirma low-ready/obstrucción. No debe haber clipping de cámara.
8. Recorrer calle → edificio médico/loot → infectados → checkpoint. Revisar entradas, props apoyados, escala humana, sombras, bordes sin dientes excesivos y lectura del zombie/runner. Ajuste visual fino pendiente de esta revisión humana.
9. E para loot; TAB inspeccionar/descartar, volver a recoger suministros y extraer. Quest compacta, countdown y resultado deben ser legibles. Volver al Hub y entregar misión.
10. Probar una muerte: caída/fade y botón Return To Hub visibles. Confirmar pérdida de equipo y loot según reglas existentes.

No se añade contenido nuevo ni fase posterior. No crear tag phase-8 hasta la validación visual manual.

## Micro polish 8.3

The existing tactical buttons now use a slightly stronger olive hover state so cards and actions remain readable during keyboard/mouse navigation. No data flow or gameplay contract changed. No suitable first-person arms asset was found among the imported humanoid characters; they remain untouched.
