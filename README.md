# Cámara de Gesell — Simulador clínico (UTPL XRlab)

Simulador en Unity 6 de una Cámara de Gesell para estudiantes de Psicología de la UTPL. El estudiante inicia sesión con su cuenta institucional, recorre el laboratorio y observa una entrevista clínica en tres fases (Inicial, Desarrollo y Final) de uno de dos casos (**Caso 1** o **Caso 4**, elegido al azar). Durante la entrevista responde preguntas, completa la ficha de criterios DSM‑5‑TR y puntúa el Inventario de Beck.

| Dato | Valor |
|---|---|
| Motor | Unity **6000.3.18f1** |
| Escenas en build | `Assets/Scenes/Iniciar sesion.unity` (0) → `Assets/Scenes/SampleScene.unity` (1) |
| Login | `https://campus3d.utpl.edu.ec/virtopsia-admin/api/authentication` (`LoginLab/`) |
| Contenido clínico | API Express en Render `https://api-labpsicologia.onrender.com/api` respaldada por **Firebase Firestore** |
| Serialización | Force Text (YAML) · Visible Meta Files |
| Control de versiones | Git + Git LFS (ver `.gitattributes`) |

## Puesta en marcha

```bash
git lfs install
git clone https://github.com/Cristhian23456/V2-Camara-de-Hessel.git
cd V2-Camara-de-Hessel
git lfs pull
```
1. Abrir con Unity Hub, usando exactamente **6000.3.18f1**.
2. Pulsar Play. `Assets/Editor/PlayModeStartSceneSetter.cs` fuerza el inicio desde la escena de login.
3. Herramientas de mantenimiento: **Tools ▸ Limpieza Proyecto** (ver `CHANGELOG_LimpiezaAssets.md`).

## Estructura relevante

```
Assets/
  LoginLab/            Login institucional (protegido: no modificar sin coordinación)
  Scripts/legacy/      Lógica en uso: ApiManager, DialogosManager, FichaDiagnostico, BeckInventory…
  Scripts/core|Systems|UI/  Arquitectura nueva SIN conectar a escenas (ver M9)
  Resources/           Audios de preguntas/retroalimentación y video del tutor (se cargan por ruta)
  Editor/              PlayModeStartSceneSetter, herramienta de limpieza
```

## Documentación de cambios

| Tema | Changelog | Notas de estudio |
|---|---|---|
| Limpieza de assets, Git LFS | `CHANGELOG_LimpiezaAssets.md` | `STUDY_NOTES_ManejoDependencias.md` |
| Errores HTTP en ApiManager | `CHANGELOG_ApiManagerErrores.md` | `STUDY_NOTES_ManejoErroresHTTP.md` |
| Pantalla de carga y error de la API | `CHANGELOG_CorreccionCargaDialogos.md` | `STUDY_NOTES_DiagnosticoBackend.md` |

---

## 🐞 Registro de bugs (QA) — auditoría del 2026-11-12

**Método:** se revisaron los 64 scripts C# propios del proyecto (todo `Assets/Scripts`, `LoginLab` y `Editor`), el YAML de `SampleScene` e `Iniciar sesion`, los prefabs relacionados, `ProjectSettings` y el estado de `.git`. Los endpoints de la API se probaron en vivo desde el equipo del laboratorio. Lo que **no** se pudo hacer fue ejecutar el simulador en Unity, así que cada bug indica cómo se verificó.

**Niveles:**
- 🔴 **Alto:** bloquea el uso con estudiantes, produce datos o calificaciones incorrectas, o es un riesgo de seguridad o de pérdida de trabajo.
- 🟠 **Medio:** falla en un flujo concreto, degrada la experiencia o el rendimiento, o es una fragilidad probable que acabará en un fallo.
- 🟢 **Bajo:** calidad de código, problemas cosméticos o código muerto sin impacto inmediato.

**Estado:** ✅ corregido · ⏳ pendiente · 🔒 fuera de Unity (backend, infraestructura o carpeta protegida)

### 🔴 Alto

| ID | Bug | Dónde | Verificación | Estado |
|---|---|---|---|---|
| A1 | **La API devuelve HTTP 500 en todas las lecturas de Firestore.** `get-dialogos` responde *"Error al obtener los diálogos ordenados."* y `get-questionsId` responde *"Error al obtener la pregunta y respuestas."* en unos 300 ms. Express sí está vivo (404 normales en rutas inexistentes). Como también falla la lectura más simple (una pregunta por ID), se descarta un índice faltante o un parámetro mal enviado: el backend **no logra acceder a Firestore**. Sin esto **no se puede ejecutar ningún caso**. | Backend (Render + Firebase) | Prueba en vivo desde el navegador del laboratorio, casos 1 y 4, fases Inicial/Desarrollo/Final | 🔒 |
| A2 | **El progreso y las calificaciones nunca se guardan.** `SaveData.writeNewUser`, `updatePartidaUser` y `updateUserIntentEntry` solo hacen `print(...)`; no hay ninguna petición HTTP. Además `updateUserIntentEntry` ignora su parámetro `fecha`. El historial (`get-user-history`) nunca recibirá datos nuevos. | `Scripts/legacy/SaveData.cs` | Lectura de código | ⏳ requiere endpoint |
| A3 | **La retroalimentación de la ficha DSM del Caso 4 muestra los textos del Caso 1.** `VistaFicha.presentarLista` siempre usa `FichaC1` (p. ej. "no mencionó pérdida de peso…"), aunque en el Caso 4 se esperan 8 criterios presentes. Es feedback clínico **contradictorio** para el estudiante. | `VistaFicha.cs:78` | Código + `listRespuestaC4 = 1,1,1,1,0,1,1,1,1` en la escena | ⏳ falta redactar `FichaC4` |
| A4 | **Lip-sync incorrecto.** `ConfigureLipSync` conecta terapeuta **y** paciente al **mismo** `AudioSource`, por lo que ambos mueven la boca con cualquier audio (preguntas, retroalimentación, alerta). Además `AvatarLipSync.Update` pone `hablar=false` en cada frame sin audio y anula el `SetBool("hablar", true)` de `DialogosManager` en los diálogos sin voz. | `DialogosManager.cs` (`ConfigureLipSync`), `core/Utilities/AvatarLipSync.cs` | Lectura de código | ⏳ |
| A5 | **"Cargando…" bloqueado para siempre** si falla cualquiera de las 3 fases (no existía camino de error). | `ApiManager.cs`, `DialogosManager.cs` | Reproducido con el 500 actual | ✅ |
| A6 | **El porcentaje de avance se calculaba mal.** `calcularCantidadP()` se ejecutaba al llegar solo la fase Inicial, pero las 3 se cargan en paralelo, así que se contaban 0 preguntas de Desarrollo y Final y el % podía pasar de 100. | `DialogosManager.OnDialogosInicialCargados` | Lectura de código | ✅ |
| A7 | **Credenciales expuestas.** Hay un token `Authorization` fijo en el código, y la **contraseña del estudiante se guarda en texto plano** en `PlayerPrefs` (registro de Windows). | `LoginLab/WebService.cs` | Lectura de código | 🔒 carpeta protegida |
| A8 | **`main` en GitHub apunta a un commit vacío** (`a65de49` borra todos los archivos). Quien clone recibe un proyecto vacío. | `.git` | Objeto commit inspeccionado | ⏳ ver `CHANGELOG_LimpiezaAssets.md` §0 |
| A9 | **Sin soporte de VR.** No hay paquetes XR (ni OpenXR, ni Meta XR, ni XR Interaction Toolkit). La cámara y el movimiento usan ratón y teclado (`Input.GetAxis`, `Mouse X/Y`), y la UI es *screen-space*. Es un riesgo de alcance para un "simulador VR". | `Packages/manifest.json`, `PlayerController`, `CameraController` | Manifest + código | ⏳ decisión de alcance |

### 🟠 Medio

| ID | Bug | Dónde | Estado |
|---|---|---|---|
| M1 | `GameObject.Find("panelLoading")` no encuentra objetos inactivos. Como el panel se oculta al iniciar, la pantalla de carga **nunca volvía a mostrarse**. | `DialogosManager` (3 sitios) | ✅ |
| M2 | `darFuncionBtnAceptar` accedía a `dialogosList[contador]` sin validar → `IndexOutOfRangeException` con una lista vacía. | `DialogosManager.cs` | ✅ |
| M3 | **El puntaje puede bajar sin límite**: cada respuesta incorrecta resta (preguntas, ficha DSM, Beck, pregunta final) sin tope ni número de intentos. En `FinalizarCaso.opcionIncorrecta` el *guard* `estado` se vuelve a poner en `true` dentro del propio `if`, así que no limita nada. | `DialogosManager`, `FichaDiagnostico`, `BeckInventory`, `FinalizarCaso:176-179` | ⏳ |
| M4 | La cinemática de recorrido se auto-inicia a los 2 s **también en partidas cargadas desde el historial**, si `ControladorCinematica.Start` corre antes que `ManagerData.Start` (el orden no está garantizado). Desactivar el componente no detiene la corrutina ya iniciada. | `ControladorCinematica.cs`, `ManagerData.cs` | ⏳ |
| M5 | `LoadData.getHistoryForUser` no notifica los errores → el panel de carga del historial queda bloqueado (mismo patrón que A5). También depende de la API caída (A1). | `LoadData.cs` | ⏳ |
| M6 | `GameObject.Find("LoginController").GetComponent<…>()` sin comprobar null → `NullReferenceException` si `SampleScene` se abre sin pasar por el login. | `ManagerData`, `SceneChange`, `FinalizarCaso`, `FichaDiagnostico`, `BeckInventory` | ⏳ |
| M7 | `HistoryManager.CargarHistorial` añade un listener nuevo en cada recarga sin quitar los anteriores. Los ítems reutilizados acumulan *closures* con historiales viejos, así que se puede cargar la **fase equivocada**. | `HistoryManager.cs:73` | ⏳ |
| M8 | `Resources.FindObjectsOfTypeAll<GameObject>()` desactiva cualquier objeto llamado `"uiAlerta (1)"`, **incluidos los prefabs/assets** (en el Editor modifica el asset). | `FichaDiagnostico.Start`, `BeckInventory.Start` | ⏳ |
| M9 | **Arquitectura nueva sin conectar:** 17 scripts de `Scripts/core`, `Systems` y `UI` (UIManager, SessionManager, AssessmentSystem, RetryManager, CameraDirector, UIRouter, TypewriterPresenter…) **no están en ninguna escena ni prefab**. Tampoco existen assets `CaseSetupSO`: todos los `GetCaseSetup()` devuelven `null` y se usan los *fallbacks legacy*. El informe de fase 1 los describe como activos. | `Scripts/core`, `Systems`, `UI` | ⏳ decidir: conectar o archivar |
| M10 | `CaseSetupSO` guarda `GameObject personajeLlorando/Normal`, pero un ScriptableObject **no puede referenciar objetos de una escena** (solo prefabs). Si se crea el asset, `ToggleLlorando` activaría o desactivaría el prefab y no el personaje en escena. | `core/Data/CaseSetupSO.cs` | ⏳ |
| M11 | El flujo depende de **textos e IDs literales** de la base de datos: la frase exacta que activa el llanto, las respuestas `"Frente al terapeuta"` y `"Dar contención emocional a la paciente "` (con espacio final), IDs Firestore (`8YLueiNVSGXZ9LBFzS6r`, `OWCRdkqf2t37Y4hrXtXl`) y posiciones fijas (`contador == 6, 8, 1, 16, 27`). Si cambia el contenido de la BD, se rompen las animaciones. | `DialogosManager.cs` | ⏳ |
| M12 | **Rendimiento** (crítico si se pasa a VR): `Calificacion.Update` reescribe 4 textos TMP en cada frame; `PlayerController.Update` llama a `GetComponentsInChildren<Collider>()` y desactiva/activa colliders en cada frame; hay muchos `Debug.Log` en bucles (`ManejadorCamara`, `PlayerController`). | Varios | ⏳ |
| M13 | `UIPanelStyler` y `ScrollSensitivityAdjuster` (`RuntimeInitializeOnLoad`) recorren **todos** los objetos en cada carga de escena y **re-emparentan botones por nombre**. Es frágil (si se renombra un objeto deja de funcionar sin avisar) y costoso. | `Scripts/UI/` | ⏳ |

### 🟢 Bajo

| ID | Bug | Dónde |
|---|---|---|
| B1 | `asignarMetodos()` captura la variable de bucle `i` dentro del *lambda* → `IndexOutOfRange` al pulsar (método no usado). | `FinalizarCaso.cs:192` |
| B2 | Las dos ramas del `if` hacen `panel.SetActive(false)`. | `ToggleHandler.cs` |
| B3 | Archivos en **ISO-8859-1** (no UTF-8): las tildes salen corruptas en el Inspector. Los logs de "Psicólogo" imprimen datos del paciente (copiar/pegar). | `ManejadorCamara.cs`, `ToggleHandler.cs` |
| B4 | El icono de silencio se evalúa al inicio con `sliderValue` sin inicializar. | `LogicaVolumen.cs` |
| B5 | `changeScena("Iniciar Sesion")` frente a la escena `Iniciar sesion`: funciona porque `LoadScene` no distingue mayúsculas, pero es frágil; usar una constante o el índice de build. | `FinalizarCaso.cs` |
| B6 | `pausePanel?.SetActive` usa `?.` con `UnityEngine.Object` (salta el null de Unity). Script no usado. | `UI/UIRouter.cs` |
| B7 | Script vacío (0 bytes). | `core/Data/CaseDataValidator.cs` |
| B8 | `using` innecesarios: `Unity.VisualScripting`, `System.ComponentModel.Design`, `System.Xml.Linq`. | `PresentarInfoSalas`, `MovimientoInteractivo`, `VistaFicha` |
| B9 | `LImpiar()` nunca vacía la lista `elements`, que crece en cada uso. | `VistaFicha.cs` |
| B10 | `other.tag == "Player"` en lugar de `CompareTag` (genera basura de memoria). | `ColisionTutorial`, `PresentarInfoSalas` |
| B11 | `OnDisable` registra `Environment.StackTrace` completo; logs de diagnóstico cada 0,5 s. | `PlayerController.cs` |
| B12 | URL de la API duplicada en dos sitios (`LoadData.apiUrl` y `ApiManager.apiUrl`). | `LoadData.cs` |
| B13 | `PauseGame()` sin método para reanudar (`Time.timeScale` queda en 0). Script no usado en escenas. | `OpcionesGame.cs` |
| B14 | `llamarUiDialogos` inicia un *typewriter* nuevo sin detener el anterior. Hoy no ocurre por el orden del flujo, pero sí si se añade un botón "saltar". | `DialogosManager.cs` |
| B15 | `PresentarInfoSalas.escribirInformacion` añade un listener en cada entrada al *trigger* sin limpiar los anteriores. | `PresentarInfoSalas.cs:184` |

### Prioridad sugerida para el siguiente ciclo de QA
1. **A1** (backend): sin él no se puede probar nada de los casos. Ver `STUDY_NOTES_DiagnosticoBackend.md` §3.
2. **A8** (repo vacío) y **A7** (credenciales).
3. **A2** (persistencia de notas), **A3** (feedback del Caso 4) y **A4** (lip-sync): son el núcleo pedagógico de los casos 1 y 4.
4. **M3, M4, M6, M7**: flujos de evaluación e historial.
