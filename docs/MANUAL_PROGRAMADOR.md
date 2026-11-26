# Manual del programador — Cámara de Gesell V2

Este manual es para quien continúe el desarrollo del simulador. Explica cómo abrir el proyecto, cómo está organizado el código, cómo fluye una sesión y qué hay que tener en cuenta al cambiarlo.

Estado del código: rama `main` al 26 de noviembre de 2026. Los cambios de los PR #21 y #22, todavía sin fusionar, se marcan como **(PR #21)** o **(PR #22)**.

---

## 1. Qué es el simulador

Es un simulador en Unity de una Cámara de Gesell para estudiantes de Psicología de la UTPL. El estudiante:

1. Inicia sesión con su usuario institucional.
2. Recorre el laboratorio en primera persona.
3. Observa una entrevista clínica entre un terapeuta y un paciente, en tres fases: **Inicial**, **Desarrollo** y **Final**.
4. Responde preguntas durante la entrevista, completa la ficha de criterios DSM‑5‑TR y puntúa el Inventario de Beck.

Hay dos casos clínicos, el **Caso 1** y el **Caso 4**. El caso se elige al azar al entrar a la escena.

## 2. Requisitos y puesta en marcha

| Requisito | Valor |
| --- | --- |
| Unity | **6000.3.18f1**, exactamente esta versión (`ProjectSettings/ProjectVersion.txt`) |
| Git | Git + **Git LFS**: los modelos, texturas y audios están en LFS |
| Sistema de entrada | Input Manager clásico (`Input.GetKeyDown`, etc.) |
| Serialización | Force Text (YAML) y Visible Meta Files |

```bash
git lfs install
git clone https://github.com/Cristhian23456/V2-Camara-de-Hessel.git
cd V2-Camara-de-Hessel
git lfs pull
```

1. Abre la carpeta con Unity Hub usando la versión 6000.3.18f1.
2. **(PR #21)** Copia `Assets/LoginLab/WebServiceConfig.example.json` a `Assets/Resources/Config/WebServiceConfig.json` y pon el token de campus3d. Git ignora ese archivo. Sin él, el login responde "error del servidor".
3. Pulsa Play. `Assets/Editor/PlayModeStartSceneSetter.cs` hace que el juego siempre empiece en la escena de login, aunque tengas abierta otra.

> Si después de clonar ves modelos rosados o audios vacíos, falta `git lfs pull`.

## 3. Escenas

| Orden en el build | Escena | Qué contiene |
| --- | --- | --- |
| 0 | `Assets/Scenes/Iniciar sesion.unity` | Login, menú de opciones e historial de partidas |
| 1 | `Assets/Scenes/SampleScene.unity` | Laboratorio, salas, entrevista, ficha DSM, Inventario de Beck y cierre |

El cambio de escena se hace con `SceneChange.changeScena(nombre)` o, tras un login correcto, con `SceneManager.LoadScene("SampleScene")` en `LoginUsuario`.

## 4. Organización del código

```
Assets/
  LoginLab/              Login institucional (campus3d)
  Scripts/
    legacy/              Lógica EN USO por las escenas
      Entidades/         Clases de datos que llegan de la API (Dialogos, Preguntas, Historial…)
    Escenario/           Sala de control: cámara en vivo, consentimiento con firma, áreas restringidas
    core/                Utilidades y datos (parte en uso, parte sin conectar)
    Systems/, UI/        Arquitectura nueva SIN conectar a las escenas (ver 4.4)
  Editor/                Herramientas de Editor (menú Tools)
  Resources/             Audios de preguntas y retroalimentación (se cargan por ruta)
  _Archivo_Obsoleto/     Assets retirados en la limpieza del PR #19. No usar.
docs/                    Documentación (este manual)
```

### 4.1 Login (`Assets/LoginLab/`)

| Script | Función |
| --- | --- |
| `LoginUsuario` | Pantalla de login: valida los campos, evita el envío doble, muestra los errores y carga `SampleScene` |
| `WebService` | `POST` a `https://campus3d.utpl.edu.ec/virtopsia-admin/api/authentication`. Devuelve `LoginResultado`: `Ok`, `CredencialesIncorrectas`, `SinConexion` o `ErrorServidor`. Timeout de 15 s |
| `DataUsers` | Singleton con `DontDestroyOnLoad` que guarda nombre, usuario y correo del estudiante entre escenas |
| `Storage` | Claves de `PlayerPrefs` |
| `Model` | Clases JSON de la respuesta de campus3d (`UserData`, `Role`, `Permissions`) |

El usuario se acepta como `usuario` o como `usuario@utpl.edu.ec`, y se envía sin el dominio. **(PR #21)** La contraseña ya no se guarda en `PlayerPrefs`, y el token se lee de `Resources/Config/WebServiceConfig.json`.

### 4.2 Simulación (`Assets/Scripts/legacy/`)

| Script | Función |
| --- | --- |
| `ApiManager` | Elige el caso (1 o 4), activa los personajes y descarga los diálogos de las tres fases. Tiene reintentos con espera creciente y diagnóstico de errores HTTP |
| `DialogosManager` | Corazón de la entrevista: muestra los diálogos, la cámara, las animaciones y el lip-sync; lanza las preguntas, la retroalimentación y los eventos especiales de cada fase |
| `FichaDiagnostico` / `VistaFicha` | Ficha DSM‑5‑TR: el estudiante marca los criterios y escribe cuántos hay. `VistaFicha` muestra la retroalimentación por criterio (`FichaC1` o `FichaC4`) |
| `BeckInventory` | Inventario de Depresión de Beck |
| `Calificacion` | Acumula el puntaje: `incrementar`, `decrementar`, `valorPregunta`, `valorIncorrecto` |
| `FinalizarCaso` | Pantalla de cierre del caso |
| `IntroController`, `PresentarInfoSalas` | Textos de bienvenida e información de cada sala |
| `ManejadorCamara`, `CameraController`, `ControladorCinematica` | Cámaras: general, terapeuta, paciente y cinemática de entrada |
| `PlayerController`, `MovimientoInteractivo`, `ColisionTutorial` | Movimiento en primera persona y tutorial |
| `LoadData`, `HistoryManager`, `ManagerData` | Historial de partidas del usuario y reanudación desde una fase |
| `SaveData` | Registro de partida e intentos (**hoy solo imprime en consola: no guarda nada**) |
| `OpcionesGame`, `LogicaBrillo`, `LogicaVolumen`, `ToggleHandler` | Menú de opciones |

### 4.3 Escenario (`Assets/Scripts/Escenario/`)

| Script | Función |
| --- | --- |
| `CamaraEnVivo` | Renderiza la sala de entrevista en una `RenderTexture` para el monitor de la sala de control |
| `ConsentimientoFirma` + `LienzoFirma` | Documento de consentimiento informado que el estudiante firma dibujando. La UI se construye en tiempo de ejecución |
| `AreaRestringida` + `AvisoUI` | Avisos tipo toast al acercarse a una puerta restringida |

### 4.4 Arquitectura nueva sin conectar

`Scripts/core`, `Scripts/Systems` y `Scripts/UI` tienen una arquitectura nueva (`UIManager`, `SessionManager`, `AssessmentSystem`, `RetryManager`, `CameraDirector`, `UIRouter`, `EventBus`…). **Casi nada de esto está en una escena ni en un prefab** (issue #16). Tampoco existen assets `CaseSetupSO`, así que `ApiManager.GetCaseSetup()` devuelve `null` y el código usa la configuración "legacy" (la lista `casoConfigs` de `DialogosManager` y los arreglos del Inspector).

Antes de usar algo de esa carpeta, comprueba con *Find References In Scene* que de verdad se ejecuta.

Sí están en uso desde `core/Utilities`:
- `AvatarLipSync`: mueve la boca solo del avatar que habla (`SetSpeaking`).
- **(PR #22)** `TextoEscrito`: efecto máquina de escribir común. Se puede saltar con clic, Espacio o Enter, el tiempo de lectura depende del largo y el texto se achica si no cabe.

## 5. Flujo de una sesión

1. **Login** (`Iniciar sesion`): `LoginUsuario.IniciarSesion` → `WebService.Login` → `WebServiceCallback(LoginResultado)`. Si es `Ok`, llena `DataUsers` y carga `SampleScene`.
2. **Historial (opcional):** `HistoryManager.funBtnHistorial` → `LoadData.getHistory` → el estudiante elige una partida → `LoadData.tieneHistorial = true` y `LoadData.fase`.
3. **Inicio de `SampleScene`:** `ApiManager.Start` elige el caso y activa sus personajes (`personajesCasos`: 0‑1 son del Caso 1, 2‑3 del Caso 4). `ManagerData.Start` decide si se reanuda desde el historial (salta la cinemática) o se empieza desde cero.
4. **Carga de diálogos:** `ApiManager.CargarRecursosCaso` pide en paralelo las fases Inicial, Desarrollo y Final y avisa con `DialogosCargadosEvent`, `DialogosCargadosDesarrolladoEvent` y `DialogosCargadosFinalEvent`. Si falla, se dispara `DialogosErrorEvent` y `ReintentarCarga()` permite volver a intentarlo.
5. **Entrevista:** `DialogosManager` recorre `dialogosList[contador]`. En cada diálogo:
   - si el personaje es el psicólogo o terapeuta, activa su cámara y su lip-sync; si no, los del paciente;
   - si `tienePregunta`, carga la pregunta (`get-questionsId`) y muestra las respuestas;
   - si `esImportante`, espera a que el estudiante pulse "Siguiente"; si no, avanza solo.
6. **Eventos por fase** (`DialogosManager`, alrededor de la línea 700):
   - Inicial, `contador == counterAtencion`: alerta de "¡Atención!".
   - Inicial, `contador == 6`: consentimiento informado (firma).
   - Desarrollo, `contador == 1`: el paciente se sienta.
   - Desarrollo, `contador == 8`: aplicación del test.
7. **Evaluación:** ficha DSM (`FichaDiagnostico`), Inventario de Beck (`BeckInventory`) y puntaje (`Calificacion`).
8. **Cierre:** `FinalizarCaso`.

> Los eventos dependen del **número de orden** del diálogo. Si se agregan o quitan diálogos en Firestore, hay que revisar estos índices.

## 6. API de contenido clínico

Los diálogos y las preguntas vienen de una API Express en Render respaldada por Firebase Firestore. **El código del backend no está en este repositorio.**

| Endpoint (`https://api-labpsicologia.onrender.com/api`) | Lo usa | Devuelve |
| --- | --- | --- |
| `GET /get-dialogos?caso={1\|4}&fase={Inicial\|Desarrollo\|Final}` | `ApiManager` | Lista de `Dialogos` |
| `GET /get-questionsId/{preguntaId}` | `ApiManager` | Una `Preguntas` con sus `Respuestas` |
| `GET /get-user-history/{usuario}` | `LoadData` | `HistorialContainer` |

La URL base está en dos sitios, `ApiManager.apiUrl` y `LoadData.apiUrl` (issue #18). Si cambia, hay que actualizar ambos.

Ajustes de red en el Inspector de `ApiManager`: `timeoutSegundos` (60), `maxIntentos` (3) y `esperaInicialSegundos` (2, se duplica en cada reintento). El plan gratuito de Render "duerme" el servicio, así que la primera petición puede tardar.

**Modelo de datos** (`Scripts/legacy/Entidades/`):
- `Dialogos`: `orden`, `contenido`, `personaje`, `tienePregunta`, `preguntaId`, `esImportante`, `caso`.
- `Preguntas`: `id`, `pregunta`, `calificacion`, `srcAudio`, `respuestas[]`.
- `Respuestas`: `respuesta`, `esCorrecta`, `retroalimentacion`, `srcAudio`.
- `Historial`: `fecha`, `fase`, `progreso`.

Los audios se cargan con `Resources.Load<AudioClip>(srcAudio sin extensión)`. El archivo tiene que estar dentro de `Assets/Resources` con esa misma ruta.

## 7. Herramientas de Editor (menú Tools)

| Menú | Archivo | Para qué |
| --- | --- | --- |
| Tools ▸ Escenario ▸ … | `Editor/Escenario/EscenarioSetup.cs` | Monta la cámara en vivo y los avisos de área restringida en la escena |
| Tools ▸ Limpieza Proyecto ▸ … | `Editor/Limpieza/ProjectCleanupTool.cs` | Analiza dependencias, re-vincula duplicados y mueve huérfanos a `_Archivo_Obsoleto` (con opción de revertir) |
| Tools ▸ Lab Psicología ▸ … | `Editor/Limpieza/LabPsicologiaUnifier.cs` | Unifica las versiones duplicadas de LabPsicologia |
| **(PR #21)** Tools ▸ UI ▸ Aplicar estilos a escenas del Build | `Editor/UI/UIStyleBaker.cs` | Aplica una vez los colores de los paneles, la ubicación de los botones y la sensibilidad del scroll. Reemplaza a `UIPanelStyler` y `ScrollSensitivityAdjuster` |

## 8. Cómo hacer cambios frecuentes

**Agregar o cambiar diálogos y preguntas:** se hace en Firestore, no en Unity. Después revisa los índices de eventos de la sección 5 y que existan los audios en `Resources`.

**Agregar un caso clínico nuevo (por ejemplo, Caso 5):**
1. Crea el contenido en Firestore con `caso = 5`.
2. Agrega el terapeuta y el paciente a la escena y a `ApiManager.personajesCasos`.
3. Cambia el sorteo en `ApiManager.Start`, que hoy solo elige entre 1 y 4.
4. Agrega el caso a la lista `casoConfigs` de `DialogosManager`, junto a las de los casos 1 y 4: nombres de objetos y `counterAtencion`, que hoy es 16 en el Caso 1 y 27 en el Caso 4.
5. Agrega `listRespuestaC5` en `FichaDiagnostico` y `FichaC5` en `VistaFicha`. Sin ellos, la ficha usa un texto genérico.
6. Revisa `BeckInventory` y `FinalizarCaso`, que también tienen ramas por caso.

**Cambiar la velocidad del texto (PR #22):** Inspector de `DialogosManager`, sección "Diálogos (#4)".

**Cambiar colores de la UI (PR #21):** edita las constantes de `UIStyleBaker`, ejecuta el menú y haz commit de las escenas.

## 9. Flujo de trabajo en git

- `main` es la rama estable. Nadie hace push directo a `main`.
- Una rama por tarea con prefijo: `fix/…`, `feature/…`, `docs/…`, `release/…`.
- Commits con el número del issue: `fix(#13): lip-sync solo en el avatar que habla`.
- Cada rama entra por **Pull Request** con revisión del otro integrante. El PR usa `Closes #N` para cerrar el issue al fusionar, o `Refs #N` si el issue sigue abierto.
- Antes de fusionar: abrir el proyecto en Unity, comprobar que no hay errores en la consola y probar el flujo afectado.
- Escenas y prefabs: no los editen dos personas a la vez, porque el YAML de Unity se fusiona mal.
- No subir secretos. `WebServiceConfig.json` está en `.gitignore`.

## 10. Problemas conocidos

| Problema | Issue | Estado |
| --- | --- | --- |
| La API responde HTTP 500 en todas las lecturas de Firestore (falla en el backend de Render, no en Unity) | #11 | Abierto: revisar los logs y las variables de entorno en Render |
| Arquitectura nueva sin conectar; no existen assets `CaseSetupSO` | #16 | Abierto |
| `SaveData` no persiste nada: solo imprime en consola | — | Sin issue |
| El token de campus3d quedó en el historial de git | #14 | Hay que pedir uno nuevo a campus3d |
| Los textos de `FichaC4` son un borrador | #12 | Validar con Psicología |
| Faltan capturas de antes y después de las mejoras de diálogos | #4 | PR #22 |

## 11. Depuración

- Los logs de red tienen prefijo: `[ApiManager]`, `[WebService]`, `[Login]`, `[LipSync]`, `[UIStyleBaker]`. Filtra la consola por ese texto.
- Si los diálogos no cargan, `ApiManager` escribe un diagnóstico con el código HTTP y la causa probable (404 = ruta incorrecta, 5xx = backend, timeout = Render dormido o sin red).
- Si un personaje no aparece, revisa que `personajesCasos` tenga sus 4 elementos asignados en el Inspector de `ApiManager`.
