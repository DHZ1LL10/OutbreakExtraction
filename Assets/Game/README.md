# OUTBREAK: EXTRACTION — arquitectura lógica

Unity 2023.2.20f1. Esta fase contiene lógica, definiciones de contenido y herramientas de prueba.

## Estructura

- `Runtime/Core`: `GameFlow` coordina estados y operaciones; `PlayerProfile` contiene el progreso del Hub; `GameManager` inicializa ambos desde Unity.
- `Runtime/Items`: `ItemDefinition`, tipos, rarezas y catálogo que detecta IDs duplicados.
- `Runtime/Inventory`: stacks inmutables e inventarios de slots con capacidad. `AddItem`, `RemoveItem` y `TryAddRange` son completos o no modifican nada.
- `Runtime/Loadouts`: nueve slots (dos armas, armadura, mochila, rig y cuatro de equipo). Los de equipo aceptan medicina, munición y attachments.
- `Runtime/Raids`: equipo desplegado y loot separados en `RaidSession`; métodos para consumir/perder equipo o retirar loot.
- `Runtime/Persistence`: DTOs JSON, conversión y lectura/escritura en `Application.persistentDataPath`.
- `Runtime/Loot`, `Maps`, `Economy`, `Quests`: tablas ponderadas, mapas, compra/venta y progreso básico de misiones.
- `Runtime/Debugging`: acciones por menú de contexto, logs y dos recorridos completos con comprobaciones.
- `Editor`: creación opcional de assets y objeto de prueba mediante las APIs de Unity.
- `Tests/Editor`: pruebas EditMode con el Test Framework ya instalado.

El dominio usa clases C# normales. Los ScriptableObjects definen contenido; el único arranque de runtime es `GameManager`. No hay singleton ni cambios automáticos de escena.

## Validación rápida en Unity

1. Abre el proyecto y deja que termine la importación/compilación. Si hace falta, usa `Assets > Refresh`.
2. Fuera de Play Mode, abre cualquier escena y ejecuta `Outbreak > Crear entorno de prueba logico`.
3. Se crean `Assets/Game/DebugContent` y el objeto `Outbreak Architecture Debug`, con referencias ya asignadas. Guarda la escena desde Unity si quieres conservar ese objeto.
4. Entra en Play Mode. Mantén seleccionado el objeto y abre `Window > General > Console`.
5. En el Inspector, abre el menú de tres puntos del componente **Architecture Debug** (también sirve clic derecho en su encabezado).
6. Ejecuta `Tests - Secuencia completa de extraccion`. Debe aparecer `[OUTBREAK][PASS] Extraccion`. La prueba crea partida, agrega $5000 e items, equipa, inicia raid, genera loot, extrae y comprueba el guardado/carga. Termina en Hub.
7. Ejecuta `Tests - Secuencia completa de muerte`. Debe aparecer `[OUTBREAK][PASS] Muerte`. Verifica también que cargar no recupera lo perdido. Termina en Hub.

Estas secuencias reinician el perfil de prueba. El objeto creado utiliza **architecture-debug.json**, separado del nombre normal `profile.json`. La ruta completa se imprime al comenzar Play Mode. El generador no sobrescribe assets de debug existentes; las secuencias esperan sus valores iniciales.

### Recorrido manual, paso a paso

Usa las acciones del mismo componente:

`01 Crear partida` → `02 Agregar $5000` → `03 Agregar items al stash` → `04 Preparar loadout` → `05 Iniciar raid` → `06 Generar loot` → `07 Extraer exitosamente` → `09 Volver al Hub` → `10 Guardar` → `11 Cargar`.

En cada paso se imprime el estado, dinero, stash y loadout. `12 Mostrar estado` vuelve a imprimirlos. En extracción debe volver el arma, deben quedar tres medicinas y deben aparecer valuables. El loadout del Hub queda vacío: el equipo vuelve al stash y debe prepararse de nuevo.

Para probar muerte desde ese Hub: `04 Preparar loadout` → `05 Iniciar raid` → `06 Generar loot` → `08 Simular muerte` → `09 Volver al Hub` → `11 Cargar`. El arma y las dos medicinas equipadas desaparecen; los valuables de esta nueva raid nunca llegan al stash. El loot de extracciones anteriores que quedó en el stash se conserva.

Para verificar archivo faltante o corrupto, sal de Play Mode, respalda/mueve el JSON de debug indicado en Console y vuelve a entrar. También puedes reemplazar su contenido por `{bad json`. Debe aparecer un mensaje y un perfil vacío utilizable. La carga no reescribe el archivo inválido; una operación posterior de guardado sí lo reemplaza.

### Pruebas automatizadas

`Window > General > Test Runner` → `EditMode` → ejecuta `Outbreak.Tests.ArchitectureTests`.

Las pruebas usan assets temporales y una carpeta única de `Application.temporaryCachePath`; no tocan la partida del jugador. Cubren stacks, operaciones atómicas, extracción, muerte, reinicio durante raid, stash lleno, fallo de escritura, JSON inválido, persistencia, economía, loadout, loot, IDs y misiones.

## Crear contenido

En Project, clic derecho → `Create > Outbreak`:

- `Items > Item`: configura ID estable, nombre, descripción, tipo, rareza, compra/venta y stack máximo. El ID se genera al crear; al duplicar un asset, asigna un ID nuevo al duplicado. No cambies IDs que ya están en partidas guardadas.
- `Items > Catalog`: registra todos los items que pueden aparecer en stash, loadout o loot. Asigna el catálogo a `GameManager`.
- `Loot > Loot Table`: entradas con item, peso y cantidades mín/máx; `dropChance` se evalúa por tirada y después se elige una entrada por peso. Peso cero excluye una entrada. La restricción de rareza es inclusiva.
- `Maps > Raid Map`: dificultad 1–5, multiplicadores y puntos de extracción básicos. `LootMultiplier` escala cantidades con redondeo probabilístico; `RareLootMultiplier` escala pesos de Rare a Mythic. Cero loot produce cero cantidad. Los datos de enemigos y condiciones de extracción quedan preparados para futuros sistemas, sin ejecutarlos todavía.
- `Quests > Quest`: tipo, objetivo por ID y cantidad. `QuestProgress` controla desbloqueo, activación, avance y reclamación; no se conectan eventos ni recompensas automáticas en esta fase.

## Reglas de propiedad y persistencia

- Equipar en Hub **retira** los items del stash. Desequipar los devuelve, si caben.
- `StartRaid(map)` copia el loadout a una sesión independiente y escribe primero un checkpoint del perfil sin ese equipo. Si no puede guardar, permanece en Hub con el equipo intacto.
- `LoadingRaid` es una transición síncrona lógica, lista para incorporar una carga de escena posteriormente.
- Cerrar la aplicación durante una raid equivale a perder lo desplegado. Esta fase no guarda ni reanuda raids.
- `ExtractSuccessfully()` transfiere equipo superviviente y loot al stash y guarda antes de cerrar la sesión. Si no caben o falla la escritura, conserva la raid para reintentar. Puedes descartar loot mediante `RemoveLoot` o equipo mediante `ConsumeEquipment`.
- `FailRaid()` vacía y cierra la sesión. El checkpoint de inicio ya excluye todo lo desplegado. El stash del Hub no se pierde.
- Guardar/cargar manualmente, preparar equipamiento y comprar/vender se permiten desde Hub. Tras un resultado usa `ReturnToHub()`.
- `NewGame()` crea el perfil en memoria; `Save()` lo persiste. El arranque carga o crea un perfil vacío en memoria.
- Armas y attachments poseídos se guardan como resúmenes derivados de stash + loadout, incluyendo cantidades. No representan copias adicionales ni desbloqueos permanentes.
- El guardado contiene versión y IDs, nunca referencias serializadas a ScriptableObjects. Datos incompatibles o IDs desconocidos rechazan el guardado completo, con mensaje y sin sobrescritura durante la carga.
- Las escrituras usan archivo temporal y reemplazo. No hay recuperación automática de partidas anteriores que pudiera devolver equipo perdido.
- Capacidad de mochila inicial: 16 slots, configurable en `StartRaid`; stash inicial: 64. El item Backpack es equipo; aún no aporta modificadores de capacidad. XP y nivel se persisten, sin una curva de progresión impuesta.

Las operaciones que sustituyen `GameFlow.Profile` producen un nuevo perfil validado; consulta la propiedad actual y evita conservar referencias viejas en futuros controladores/HUD. Las referencias a definiciones se comparten como contenido de solo lectura durante gameplay.
