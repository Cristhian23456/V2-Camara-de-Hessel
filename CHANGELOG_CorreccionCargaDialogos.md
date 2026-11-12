# CHANGELOG — Pantalla de carga, error de API y conteo de preguntas

**Proyecto:** Cámara de Gesell · Unity 6000.3.18f1
**Fecha:** 2026-11-12
**Bugs del README que cubre:** A5, A6, M1, M2 (✅). A1 queda diagnosticado: es del backend.

---

## 1. Diagnóstico del error reportado

```
Origen probable : SERVIDOR (HTTP 500) …
Content-Type    : text/html; charset=utf-8  |  40 caracteres  |  0,5 s
Cuerpo          : Error al obtener los diálogos ordenados.
```

Prueba en vivo de los endpoints desde el equipo del laboratorio (2026-11-12, ~13:50):

| Petición | HTTP | Cuerpo |
|---|---|---|
| `GET /api/get-dialogos?caso=1&fase=Inicial` | 500 | `Error al obtener los diálogos ordenados.` |
| `GET /api/get-dialogos?caso=4&fase=Final` | 500 | `Error al obtener los diálogos ordenados.` |
| `GET /api/get-questionsId/OWCRdkqf2t37Y4hrXtXl` | 500 | `{"message":"Error al obtener la pregunta y respuestas."}` |
| `GET /api/get-questionsId/8YLueiNVSGXZ9LBFzS6r` | 503 → 500 | (primero Render despertando, luego el mismo 500) |
| `GET /` , `/api`, `/health` | 404 | `Cannot GET …` (Express responde con normalidad) |

**Conclusión:** el servidor Express está **activo**, pero **toda lectura de Firestore falla**, incluida la más simple (un documento por ID, sin `orderBy`). Eso descarta un índice compuesto faltante, parámetros mal enviados por Unity o un JSON mal formado. El fallo está en el acceso del backend a Firebase. **No se corrige desde Unity.** Los pasos para el backend están en `STUDY_NOTES_DiagnosticoBackend.md` §3.

Lo que sí se corrige en Unity: que el simulador **se quedaba congelado en "Cargando…"** ante ese fallo y otros tres defectos del mismo flujo.

---

## 2. Archivo modificado

| Archivo | Cambio |
|---|---|
| `Assets/Scripts/legacy/DialogosManager.cs` | Manejo del error de API, pantalla de carga fiable, conteo de preguntas, protección ante lista vacía |

`ApiManager.cs` ya expone `DialogosErrorEvent` y `ReintentarCarga()` desde el cambio anterior (`CHANGELOG_ApiManagerErrores.md`). No se tocan escenas ni prefabs.

### Qué cambia en `DialogosManager.cs`

| # | Antes | Ahora |
|---|---|---|
| 1 | No escuchaba los errores de `ApiManager`; si una fase fallaba, `d1 && d2 && d3` nunca se cumplía | Se suscribe a `apiManager.DialogosErrorEvent` y se desuscribe en `OnDestroy` |
| 2 | `GameObject.Find("panelLoading")` (3 veces): devuelve `null` si el panel está inactivo, y se ocultaba en `Start`, así que la pantalla de carga **nunca volvía a aparecer** | La referencia se cachea en `Start` **antes** de ocultarla (campo nuevo `panelLoading`, asignable en el Inspector; si está vacío se busca por nombre, también entre objetos inactivos) |
| 3 | Sin mensaje de error | El panel muestra el mensaje de `ApiManager` y un botón **"Reintentar"** que llama a `apiManager.ReintentarCarga()` y vuelve a "Cargando ....." |
| 4 | `calificacion.preguntasCant` se calculaba al llegar solo la fase Inicial (las 3 cargan en paralelo) | Se calcula en `CheckAllDialogosLoaded()`, cuando las 3 fases están listas (mínimo 1 para no dividir entre 0) |
| 5 | `darFuncionBtnAceptar` leía `dialogosList[contador]` sin validar | Valida el índice; si no hay diálogo, muestra el error con "Reintentar" en lugar de lanzar `IndexOutOfRangeException` |

**El botón "Reintentar" es un clon nuevo.** El botón original de `panelLoading` (`Button` → "Continuar") tiene en la escena llamadas persistentes a `GameObject.SetActive(false)` y `ManagerData.continuarFase()`. Reutilizarlo dispararía esas llamadas, así que se clona y se le asigna un `ButtonClickedEvent` nuevo, que elimina las llamadas persistentes copiadas. El original sigue oculto, como antes.

**Cuándo se muestra el error:**
- Si el estudiante **ya está esperando** (pulsó comenzar y ve "Cargando…"), el error aparece de inmediato.
- Si todavía está **recorriendo el laboratorio** (la carga empieza al entrar en `Entrada3`), el error se guarda y aparece cuando intente iniciar la sesión. No interrumpe el recorrido.
- Si fallan las 3 fases, se muestra **un único** mensaje.

---

## 3. Integración en el Editor

1. Abrir Unity y esperar a que compile (0 errores).
2. Recomendado: en `SampleScene`, seleccionar el objeto con **DialogosManager** → sección **"Pantalla de carga / error de API"** → arrastrar `panelLoading` al nuevo campo. Si se deja vacío funciona igual (se busca por nombre al iniciar), pero la referencia explícita es más robusta.
3. Guardar la escena.
4. **Prueba con la API caída (estado actual):** Play → login → recorrido → `Entrada3` → **Comenzar** → introducción → abrir puerta. Debe aparecer el mensaje *"El servidor de la UTPL tuvo un problema…"* con el botón **Reintentar**. Al pulsarlo vuelve "Cargando ....."; si la API sigue caída, tras los reintentos automáticos (unos 10 s) reaparece el mensaje. **Antes** se quedaba en "Cargando ....." para siempre.
5. **Prueba con la API funcionando** (cuando se arregle A1): el flujo normal no cambia y el % de avance de `Calificacion` ya no supera el 100 %.
6. Commit: `git add Assets/Scripts/legacy/DialogosManager.cs README.md CHANGELOG_CorreccionCargaDialogos.md STUDY_NOTES_DiagnosticoBackend.md`.

**Verificación hecha:** `DialogosManager.cs` + `ApiManager.cs` compilan sin errores contra las firmas de la API de Unity usadas (comprobación con Mono). La jerarquía de `panelLoading` (`Text (TMP)` + `Button`) y sus llamadas persistentes se verificaron en el YAML de `SampleScene`. **No se ejecutó en el Editor.**
