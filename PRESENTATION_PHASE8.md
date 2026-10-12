# Fase 8 — Presentation visual pass

Unity 2023.2.20f1 / URP. Escenas guardadas: `Hub_Test` y `RaidLoop_Test`. El cambio previo de la misión en Hub fue conservado primero en el commit separado `0644768` y publicado en main.

## Qué cambió

- Raid: calle con marcas, dos edificios jugables vestidos con `house_ruined`, techo sobre el edificio médico oeste, una casa abandonada exterior, coches, vallas, estufa y suministros. Se conserva el layout compacto, el spawn seguro y las posiciones de loot/infectados/extracción.
- Roots: `Gameplay_Geometry` conserva la geometría funcional; `Environment_Visuals` y `Props_Visuals` contienen decoración. `Characters_Visuals` agrupa el spawner existente y sus actores. Loot y pickups conservan sus roots/colliders/scripts, con children `VisualModel` independientes.
- Las ruinas importadas tienen una abertura posterior desplazada lateralmente. Se rotaron hacia la ruta de llegada y se ajustaron SOLO los cuatro bloques de entrada de los edificios para coincidir con esa abertura. El resto de los colliders originales se conserva. Dos proxies de malla de edificio y dos proxies simples de coche se incluyen en el NavMesh; los props pequeños son decorativos.
- El NavMesh se rebakeó en `Presentation/RaidPresentationNavigation.asset`. El updater verifica rutas completas desde el spawn a los seis spawn points, los cinco contenedores y extracción.
- Extracción: el renderer verde se oculta, con un checkpoint de postes, vallas, lámparas, señal y marcas de suelo. El transform/collider originales de ExtractionZone son idénticos a los de la escena anterior. Se mantienen sus 5 segundos automáticos y la UI existente.
- Loot: caja médica con First_Aid, cajas reforzadas/toolbox con asas y bandas, radio con Walkie_talkie. La caja médica y kit caben aproximadamente dentro del collider existente. No cambian tablas, seeds, cantidades, Medical Supplies, Take/Take All ni inventarios.
- Infectados: prefabs derivados `PresentationInfected` y `PresentationRunner`, con el modelo Humanoid de Kevin Iglesias como child del Visual existente. Los placeholders quedan sin renderer, manteniendo sus DamageHitbox. Se conserva salud, settings, AI, NavMeshAgent, drops y spawner. El normal y runner utilizan Walk, con velocidad de reproducción ajustada al movimiento; se eligió Walk acelerado para runner para evitar el riesgo/tiempo del retarget Adult_Run.
- Animación: controller mínimo de Idle/Walk/Attack/Damage/Death. `InfectedVisualAnimator` consume el estado y eventos de AI/health; no calcula ataques/daño. Root motion está desactivado. La hitbox Head sigue el hueso humano de cabeza; su receptor sigue siendo InfectedHealth. InfectedPresentation conserva el flash y retirada del cadáver tras 7 segundos, y omite la caída procedural cuando se usa el modelo animado.
- Armas: AKM y Pistol1 dentro de cada WeaponViewModel de esta Raid. Se preservan los objetos de lógica y sockets; se ocultan los placeholders. Sólo se usan meshes/materiales del proveedor, sin sus scripts ni colliders. Todas las piezas de arma usan Ignore Raycast, sin sombras sobre el mundo.
- AKM: rotación Y=180°, escala 0.042 (longitud aproximada 0.84m). Offset de modelo `(0,-0.064,0.10)`, hip `(0.18,-0.16,0.40)`, ADS `(0,-0.046,0.40)`. Muzzle local z=0.522. Base magazine vinculado a la pieza `Cube.003_low`.
- Pistol1: rotación Y=90°, escala 1. Offset de modelo `(0,-0.11,0)`, hip `(0.14,-0.16,0.42)`, ADS `(0,-0.044,0.42)`. Muzzle z=0.264. Base magazine vinculado a `Pistol1_Magazine`.
- Optic, muzzle, magazine, grip y ejection sockets se recolocan sobre esos modelos. Los cinco attachments conservan sus visuales/modificadores existentes; Red Dot y suppressor son visibles, extended mag oculta la pieza base correspondiente. Near clip de cámara intacto: 0.04m. Sway, ADSAnchor del optic, recoil, low-ready, wall avoidance, muzzle flash y casquillos siguen bajo los sistemas originales.
- Hub: estructura funcional conservada, con acabado de refugio, vigas/techo, mesas de estaciones, suministros y lámparas cálidas. Estaciones en las mismas posiciones; label MISSIONS pasa a COMMANDER. El label no cambia MissionStation ni su función.
- Raid más fría, niebla lineal moderada 35–110m y skybox existente; Hub sin niebla, luz más cálida. Cinco luces prácticas sin sombras en total entre las dos escenas, además de una directional por escena. No se añadió audio ni paquetes.

## Archivos

- `Assets/Game/Editor/FPS/PresentationVisualPass.cs` (+meta): updater explícito de escenas, authoring de prefabs derivados/controller/modelos, rebake y validación de referencias/rutas.
- `HubTestEnvironment.cs`, `RaidLoopTestEnvironment.cs`: los generators aplican este pase antes de guardar su layout. El updater es la opción para conservar la integración de la escena existente.
- `Assets/Game/Runtime/Infected/InfectedVisualAnimator.cs` (+meta): adaptador de animación y seguimiento de cabeza, sin nuevas reglas de combate.
- `InfectedPresentation.cs`: flag opcional `useAnimatedPose`, false por defecto, para que la caída procedural no compita con Death del Animator. Los prefabs de pruebas anteriores mantienen la opción false.
- `HubSceneFlowTests.cs`: verifica que la Raid presentada tiene seis actores Humanoid con root motion desactivado, receptor/hitbox válidos y modelos de arma, además del ciclo de misión/extracción/muerte/persistencia existente.
- `Assets/Game/DebugContent/Presentation/` (+meta): 12 materiales simples compartidos, dos prefabs derivados, controller y NavMesh, con sus metas.
- `Assets/Game/Scenes/Hub_Test.unity`, `RaidLoop_Test.unity`: integración guardada, todos los IDs originales de objetos/componentes conservados.

Los assets originales de proveedores, WeaponDefinition/AttachmentDefinition, GameFlow/RaidSession, inventarios, misión, PlayerMotor/Look/Input y Bodycam no se modificaron. FPS_Test, Gunplay_Test, Gunplay_3B_Test e Infected_Test no se modificaron.

## Tooling

Fuera de Play, abrir una de las dos escenas y usar **Outbreak > Actualizar escena a presentación Fase 8**. Este comando recrea sólo los visuales que pertenecen al pase, vuelve a usar los objetos de gameplay existentes, valida y guarda. Puede reemplazar ajustes manuales hechos dentro de Environment_Visuals/Props_Visuals/VisualModel: guardar esos ajustes antes en Git. Los prefabs derivados y materiales existentes mantienen el tuning de Inspector; editar estos assets directamente para ajustar animaciones/colores.

Los generators completos conservan sus diálogos previos de reemplazo de escena. Para trabajar sobre estas escenas, usar el updater. Nada se regenera durante Play.

## Validación realizada

- Compilación Unity de runtime/editor/tests correcta. Authoring final en copia temporal: exit 0.
- EditMode: 125/125 aprobados (`Logs/PresentationTests.xml`).
- PlayMode de integración: 1/1 aprobado (`Logs/PresentationSceneTests.xml`), incluido el check de seis infectados, avatares, head receivers y referencias de modelos.
- Rebake y rutas verificadas en cada aplicación del updater; loots y extracción accesibles en NavMesh.
- Sin scripts/materiales/shaders faltantes ni GUIDs faltantes en los assets/escenas nuevos. Sin Broken text PPtr ni excepciones del pase.
- Comparación de YAML: ningún ID original eliminado; scripts, colliders y poses de Player, cinco contenedores y tres pickups sin cambios; extracción conserva exactamente transform y collider. Añadir VisualModel cambia sólo sus listas de children/renderers.
- No se renderizó ni se hizo validación visual automática. Las poses, arte/iluminación y animaciones se deben validar manualmente. No se afirma haber borrado la Console histórica del editor abierto.
- Se normalizaron sólo los campos YAML vacíos con espacios finales de las escenas/assets de este pase, sin cambiar su significado serializado, para que diff-check quede limpio.

## Validación manual

1. Salir de Play y volver a abrir `Assets/Game/Scenes/Hub_Test.unity` desde Project, para cargar la versión guardada. Entrar en Play. Debe verse un refugio cálido con mesas/estaciones legibles y techo/vigas. Caminar y usar E en Stash, Loadout, Quartermaster, Workbench, Commander y Deploy; Escape cierra los paneles y devuelve el mouse.
2. En Loadout equipar rifle en Primary y pistola en Secondary. Preparar attachments en Workbench y comprobar que ambos sets se conservan. Para el ciclo conectado Hub→Raid, las teclas F1–F4 siguen deshabilitadas como antes: usar Workbench. En Raid standalone siguen siendo controles debug.
3. Deploy: aparecer en el mismo pocket seguro. Debe verse una calle fría/legible, ruinas, coches y vallas; no una noche oscura. Caminar por la calle y rutas laterales, comprobar puertas/colliders y ausencia de geometría que bloquee la vista inesperadamente.
4. Pulsar 1/2: AKM y Pistol1 visibles, sólo el arma equipada. RMB sin optic: comprobar sights cerca del centro; con Red Dot, comprobar reticle/ADSAnchor centrado. Comprobar hip, ADS+Q/E, ADS+crouch, recoil, sprint/low-ready y recuperación.
5. Acercarse a pared/coche: wall avoidance baja el arma, bloqueo de muzzle permanece. Disparar lejos de paredes: flash, casquillos, impacts y hitmarker. No debe haber clipping en la cámara al disparar/reload/cambiar, ni armas gigantes.
6. Red Dot y suppressor deben verse sobre el modelo y cambiar de arma conservando el set. Suppressor: flash reducido y salida al extremo del tubo. Extended mag: base magazine desaparece, reload usa mayor capacidad y quitarlo devuelve el exceso a reserve. Grip conserva su efecto. Comparar con los valores del debug overlay.
7. Salir de la zona segura: ver zombies con modelo y animación. Comprobar idle/wander, chase, ataque y damage/stagger. Runner debe desplazarse/animarse más rápido. Disparar cuerpo/cabeza: impactos, headshots, Death animada y retirada posterior. Revisar que el mesh de cabeza coincida con la hitbox durante movimiento.
8. Explorar el edificio oeste, abrir la caja Medical Bag y usar Take/Take All. Medical Supplies y el contador de la misión funcionan igual. Visitar Toolbox, Civilian Container, Locker, Military Crate y pickups. No deben quedar ítems o sus visuales flotando/inaccesibles.
9. Llegar al checkpoint al norte y quedarse en el trigger invisible: countdown existente de 5s, extracción y vuelta al Hub. Commander entrega recompensa una sola vez y save/reload conserva lo recuperado.
10. En otra raid, dejar que un infectado ataque hasta morir: HP baja, bodycam cae/fade y bloquea controles; Return to Hub mantiene el failure anterior. La animación de ataque no debe aplicar daño adicional ni modificar cooldown.

Si se requiere ajustar una pose/escala, hacerlo en los VisualModel/WeaponViewModel/Animator del pase, conservando roots y referencias de gameplay. No hay tag phase-8: falta la aceptación visual manual.
