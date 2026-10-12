# Auditoría de imports para Fase 8

Unity 2023.2.20f1, URP 16.0.6. Auditoría de archivos, referencias e importación sin render ni montaje visual.

## Inventario

| Paquete | Prefabs | FBX | Materiales | Texturas | Animaciones |
|---|---:|---:|---:|---:|---|
| Abandoned buildings | 12 | 11 | 9 | 13 TGA | Ninguna |
| DelthorGames/AKM | 1 | 1 | 1 | 7 PNG | Ninguna |
| ithappy/Apocalypse_Free | 69 | 67 | 18 | 10 PNG | 41 .anim Humanoid/Generic, 1 controller de demostración |
| Kevin Iglesias/Zombie Animations | 1 | 7 | 2 | 2 TGA | 8 clips Humanoid en 6 FBX, 5 controllers |
| tec9 | 1 | 1 | 1 | 3 PNG | Ninguna |

El inventario técnico por archivo está en `ASSET_AUDIT_INVENTORY.txt`: shader, rig, avatar, clips/duración/loop, texturas/dimensiones, colliders y scripts faltantes. Se examinaron 319 archivos de assets. Los 84 prefabs cargan sin scripts faltantes.

## Recomendaciones concretas

| Uso | Asset recomendado | Observaciones verificadas |
|---|---|---|
| Edificios/environment | `Assets/Abandoned buildings/Prefab/abandoned_buildings/house_aband.prefab`, `house_ruined.prefab` | Un renderer y un collider por prefab; materiales convertidos a URP Lit. |
| Piezas de entorno | En esa misma carpeta: `house_enter.prefab`, `house_stairs.prefab`, `house_roof.prefab`, `junk_bridge.prefab`, `pole_wood.prefab` | Modelos estáticos. Revisar escala, UV y paso del CharacterController manualmente al montar. |
| Carretera/vegetación | `Assets/ithappy/Apocalypse_Free/Prefabs/Environment/Road_01.prefab`, `Road_Barrier_01.prefab`, `Pine_01.prefab` | URP; Road y Barrier no traen collider. Estilo low poly que deberá evaluarse junto a los edificios. |
| Props de suministros | `Assets/ithappy/Apocalypse_Free/Prefabs/Props/First_Aid.prefab`, `Fuel_Canister.prefab`, `Walkie_talkie.prefab`, `Map.prefab`, `Concrete_Fence.prefab`, `Apocalypse_Car_01.prefab` | Candidatos visuales; no son objetos de gameplay de OUTBREAK. First_Aid no trae collider. |
| Props de edificios | `Assets/Abandoned buildings/Prefab/abandoned_buildings/stove.prefab`, `stove_burned.prefab`, `birdhouse.prefab`, `junk_window.prefab`, `junk_wind_broke.prefab` | Reutilizar los prefabs existentes; materiales URP Lit. |
| Rifle | `Assets/DelthorGames/AKM/Prefabs/AKM.prefab` | 35 renderers/piezas, sin colliders ni clips; modelo preparado por su autor para FPS. Shader Autodesk Interactive convertido con el upgrader oficial de URP. Texturas albedo, normal, metallic/smoothness, roughness, AO, height y emission. No incluye manos/rig FPS/animaciones de arma. |
| Pistola | `Assets/ithappy/Apocalypse_Free/Prefabs/Props/Pistol1.prefab` | 4 renderers; Base, Bolt, Magazine y Trigger disponibles también por separado. Sin collider ni animaciones de arma. Materiales URP existentes. |
| Alternativa de arma | `Assets/tec9/prefab/Tec9.prefab` | Modelo estático Generic, un renderer, sin avatar, colliders ni clips. Tec9 es candidato para un arma tipo SMG; no corresponde directamente a la pistola semiautomática actual. Material convertido a URP Lit; albedo/normal/metallic-roughness disponibles. Interpretación roughness/smoothness debe revisarse manualmente, no se repaquetaron texturas. |
| Zombie | `Assets/Kevin Iglesias/Zombie Animations/Prefabs/Zombie.prefab` | Un SkinnedMeshRenderer; `Model/ZombieModel.fbx` tiene avatar Humanoid válido. Textura de 512×512. Sin colliders/hitboxes/AI de OUTBREAK; su Animator de demostración usa ahora el controller de idles reparado. |
| Animación zombie | `Assets/Kevin Iglesias/Zombie Animations/Animations/` | Clips listados abajo, compatibles con retarget Humanoid. |
| Carrera para runner | `Assets/ithappy/Apocalypse_Free/Animations/Adult_Animations/Adult_Run.anim` o `AC_Adult_Run_Forward.anim` | Humanoid y loop; candidato para retarget sobre ZombieModel. Ajuste de root motion, postura y velocidad queda para montaje/validación manual. |

## Rigs y animaciones

`ZombieModel.fbx`: Human, avatar válido, un renderer skinned. Los FBX de animación de Kevin Iglesias contienen curvas Humanoid, sin mesh/renderer y sin avatar independiente; se aplican sobre el avatar del modelo.

| FBX | Clips importados | Duración | Loop |
|---|---|---:|---|
| Zombie@Idle01.fbx | Zombie@Idle01 | 2.53 s | Sí |
| Zombie@Idle01_Action01.fbx | Zombie@Idle01_Action01 | 2.83 s | No |
| Zombie@Walk01.fbx | Zombie@Walk01, Zombie@Walk01 [RM] | 1.47 s cada uno | Sí |
| Zombie@Attack01.fbx | Zombie@Attack01 | 1.33 s | No |
| Zombie@Damage01.fbx | Zombie@Damage01 | 0.50 s | No |
| Zombie@Death01_A.fbx | Zombie@Death01_A, Zombie@Death01_B | 1.33 s cada uno | No |

`ithappy/Meshes/Adult_Base.fbx` tiene avatar Humanoid válido. Adult_Woodcutter y Adult_Survivor son modelos skinned, sus prefabs usan el avatar de Adult_Base. Sus personajes no son zombies. Los clips Adult son Humanoid; Doberman utiliza otra jerarquía, no es material para zombies. `Adult_Death_Backward` y `Adult_Hit_Reaction_Light` vienen marcados loop: requerirían revisar ese ajuste antes de usarlos como one-shot. No se modificaron aquí.

## Reparaciones y URP

1. `Zombie@Idles.controller`: se eliminó exclusivamente la transición huérfana `9161741989736662082`, cuyo destino `-813125703811010857` no existe en el YAML. Ningún estado la referenciaba. Los clips Idle01 y Action01 sí existen y sus GUID/fileID coinciden; se conservaron los dos estados y sus transiciones válidas. No se eliminó el controller ni su meta.
2. `Zombie.prefab`: su Animator apuntaba a un GUID de controller ausente (`f03a05fbd64202e44821a0031ef897d2`). Ahora apunta a `Zombie@Idles.controller`; se preservó el avatar y el resto del prefab.
3. Diez materiales Standard (nueve de Abandoned buildings y uno Tec9) convertidos con el upgrader oficial de Unity a URP Lit, conservando propiedades/texturas compatibles.
4. `AKM_Mat.mat` usa Autodesk Interactive, no Standard. Convertido con su upgrader oficial a `Universal Render Pipeline/Autodesk Interactive/AutodeskInteractive`, disponible con Shader Graph 16.0.6.
5. `ZombieMaterial.mat` y `ZombieFloor.mat`: Unlit/Texture → URP/Unlit, con `_MainTex` trasladado a `_BaseMap`. Mantienen la presentación unlit original.
6. ithappy ya tiene URP Lit y `Ithappy/Blood Overlay/URP`; el shader declara `UniversalPipeline`, Forward, ShadowCaster y DepthOnly. Su Skybox/Cubemap puede mantenerse en URP. No se importaron los unitypackages de conversión Built-In/HDRP.

Total: 13 materiales convertidos; se conservaron sus metas/GUID. Los scripts del juego no cambiaron. Los paquetes añadidos previamente por el import (`postprocessing` 3.5.0 y dependencia explícita `shadergraph` 16.0.6) y los defines `UNITY_POST_PROCESSING_STACK_V2` se conservaron. Postprocessing v2 es contenido de demostración Built-In, no reemplaza los Volumes URP del juego.

## Limitaciones reales de los paquetes

- `M_house_burned.mat` referencia una textura albedo no incluida (GUID `500fb532dc0bebe4e87ed11d0f18674d`). No se inventó una textura sustituta; no recomendar este material todavía. No hay un house_burned.prefab en el paquete importado.
- `ithappy/Animations/Character_Movement.controller` tiene cuatro referencias a clips de FBX ausentes (GUID `c93e73bba3372ed4e98e1a42dd360121` y `f97a25e05a234654ab9d4e11eb9e2e26`). No usar su controller de demostración como locomoción del juego; los .anim recomendados sí cargan y contienen curvas Humanoid.
- Algunos remaps de material y el campo histórico `lastHumanDescriptionAvatarSource` de los FBX de personajes ithappy apuntan a archivos del proyecto fuente no incluidos. Los prefabs usan Adult_Base como avatar; el inventario distingue los modelos con y sin avatar propio. Para zombies se recomienda el modelo completo de Kevin Iglesias.
- `Abandoned buildings/Scene/assets_preview.unity` tiene un script de cámara de demo ausente. Los demos HDRP de ithappy tienen componentes/profile HDRP ausentes, porque este proyecto utiliza URP. Son referencias incompletas en demos opcionales, no errores C# ni dependencias del juego. No se añadió HDRP para esos demos ni se modificaron sus escenas. Usar los prefabs/materiales auditados y, si se consulta un demo, preferir `ithappy/Scenes/URP_Scenes`.
- Dos warnings CS0618 en `ithappy/.../DemonstrationSceneManager.cs` por FindObjectsOfType obsoleto. El script original se conservó; no impiden compilar.
- Compatibilidad comprobada por importación, resolución de shaders/materiales y datos de rig, sin render automático. Escala percibida, transparencia, sombreado, root motion y alineación FPS deben validarse manualmente cuando se autorice el montaje.

## Validación y Git

Importación/auditoría en copia temporal del proyecto mediante Unity 2023.2.20f1 en batchmode/nographics: salida 0; controllers de Kevin Iglesias resuelven clips; 84 prefabs sin scripts faltantes; sin Broken text PPtr, errores C# ni mensajes de error de shader reportados en esa importación. Compilaron las seis assemblies runtime/editor/tests de OUTBREAK y el assembly del paquete ithappy. Pruebas EditMode de OUTBREAK: 125/125 aprobadas. Logs completos en `Logs/ImportedAssetAudit*.log` y `Logs/ImportedAssetTests.xml` (ignorados por Git).

El proceso de auditoría temporal tuvo un fallo de su propio lector de texturas al asumir Texture2D para una textura cubemap; se corrigió a Texture, se repitió y terminó correctamente. No fue un error del paquete ni se añadió ese helper al juego.

Las entradas rojas históricas de la Console del editor abierto no se pueden borrar desde esta auditoría batch; Clear en Console permite verificar que el error reparado no reaparece tras Refresh. No se afirma haber controlado esa ventana.

El cambio previo de una línea en `Assets/Game/Scenes/Hub_Test.unity` (referencia serializada a la misión de Fase 7) se conservó intacto y queda fuera del commit de imports. No se modificaron RaidLoop_Test, FPS_Test ni gameplay. No se montaron assets ni se sustituyeron armas/infectados.

`git diff --check` del cambio previo no señala problemas. El check del contenido staged señala espacios al final de línea propios de los archivos originales importados (YAML/metas/scripts de proveedor); se preservan sin reformatear todo el paquete. Repitiendo el check con `core.whitespace=-blank-at-eol` no queda ninguna incidencia.
