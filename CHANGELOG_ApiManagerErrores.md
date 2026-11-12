# CHANGELOG — Manejo robusto de errores HTTP en ApiManager

**Proyecto:** Cámara de Gesell VR · Unity 6000.3.18f1
**Fecha:** 2026-11-12
**Error de origen:**
```
Error al obtener los dialogos: HTTP/1.1 500 Internal Server Error
ApiManager/<GetDialogosFromApi>d__24:MoveNext () (at Assets/Scripts/legacy/ApiManager.cs:121)
```

---

## 1. Diagnóstico

| # | Problema en el código anterior | Efecto en el simulador |
|---|---|---|
| 1 | Solo se imprimía `request.error` (`"HTTP/1.1 500 Internal Server Error"`); el **cuerpo de la respuesta** del servidor, que trae la causa real, se descartaba | Imposible saber si falló la BD, la consulta o el código del backend |
| 2 | Ante un error **no se invocaba el callback ni ningún evento** | `DialogosManager` espera `d1 && d2 && d3`: si una fase falla, el panel **"Cargando…" queda bloqueado para siempre** (fallo silencioso) |
| 3 | Sin reintentos ni *timeout* | Un 502/503 transitorio de Render (servicio despertando) arruina la sesión |
| 4 | `JsonUtility.FromJson` sin `try/catch` ni validación | JSON mal formado → `ArgumentException` no controlada; campos renombrados en el backend → valores vacíos sin ningún aviso |
| 5 | Si `GetPreguntaById` fallaba, el diálogo quedaba con `pregunta = null` y la fase se daba igual por cargada | `NullReferenceException` más tarde, en mitad del caso clínico |
| 6 | `fase` se concatenaba sin codificar en la URL | Riesgo si alguna fase contiene espacios o tildes |

> **Sobre el 500 en sí:** un HTTP 500 significa que la petición **sí llegó** al servidor y que **el backend lanzó una excepción** al procesarla. No es un problema de red ni del JSON que recibe Unity (en un 500 Unity aún no parsea nada). El nuevo log mostrará el cuerpo de la respuesta con el mensaje del backend. Con eso se determina si es la base de datos, la consulta por `caso`/`fase` o un bug del endpoint. Desde este entorno no se pudo consultar la API directamente (el dominio no está permitido aquí).

---

## 2. Archivo modificado

| Archivo | Cambio |
|---|---|
| `Assets/Scripts/legacy/ApiManager.cs` | Reescritura del flujo HTTP. Su API pública es **compatible** con la anterior: mismos campos, eventos, `CargarRecursosCaso()`, `getNroCaso()` y `GetCaseSetup()` |

Escenas y prefabs: **sin cambios**. El GUID del script se conserva (se sobrescribió el `.cs` y su `.meta` sigue igual), así que la referencia en `SampleScene` no se rompe.

### Qué hay de nuevo

**Inspector → sección "Red: tolerancia a fallos"**

| Campo | Valor por defecto | Para qué sirve |
|---|---|---|
| `timeoutSegundos` | 60 | Render en plan gratuito puede tardar unos 50 s en despertar |
| `maxIntentos` | 3 | Intentos totales ante errores transitorios |
| `esperaInicialSegundos` | 2 | *Backoff* exponencial: 2 s → 4 s → 8 s |
| `maxCaracteresCuerpoLog` | 1500 | Cuánto del cuerpo de la respuesta se imprime |

**API pública nueva**

```csharp
public event Action<string, string> DialogosErrorEvent; // (fase, mensaje para el estudiante)
public bool HuboErrorDeCarga { get; }
public void ReintentarCarga();                          // para un botón "Reintentar"
```

**Política de reintentos**

| Situación | ¿Reintenta? |
|---|---|
| Sin conexión, DNS, *timeout* (`ConnectionError`) | Sí |
| HTTP 500, 502, 503, 504, 408, 429 | Sí |
| HTTP 400, 401, 403, 404, 422, 501 | No (repetir no lo arregla) |
| JSON inválido o vacío | No |

**Ejemplo del nuevo log ante un 500:**
```
[ApiManager] Error al obtener get-dialogos (caso=4, fase=Desarrollo) — intento 3/3
  Origen probable : SERVIDOR (HTTP 500) — el endpoint lanzó una excepción interna (BD, consulta o código del backend)...
  Resultado Unity : ProtocolError  |  HTTP 500  |  HTTP/1.1 500 Internal Server Error
  URL             : GET https://api-labpsicologia.onrender.com/api/get-dialogos?caso=4&fase=Desarrollo
  Content-Type    : application/json; charset=utf-8  |  87 caracteres  |  1.3 s
  Mensaje servidor: <texto que envíe el backend>
  Cuerpo de la respuesta:
    {"message": "..."}
  → Sin más reintentos.
```
Los intentos intermedios salen como **Warning** (amarillo) y solo el último como **Error** (rojo).

**Clasificación automática del origen** (línea "Origen probable"):

- `RED / CONEXIÓN`: el servidor no respondió.
- `SERVIDOR / HOSTING (502/503/504)`: Render caído o despertando.
- `SERVIDOR (500)`: excepción en el backend; viene con el cuerpo JSON o HTML.
- `ENDPOINT / URL (404)`, `PETICIÓN / PARÁMETROS (400/422)`, `AUTORIZACIÓN (401/403)`.
- `ESTRUCTURA JSON → …`: HTTP 200 pero con cuerpo vacío, que no es JSON, objeto en lugar de arreglo, sintaxis rota o lista vacía.

Además, aparece un **Warning** si el JSON se leyó pero tiene `contenido` o `personaje` vacíos, o `tienePregunta=true` sin `preguntaId`. Es la señal típica de que un campo cambió de nombre en el backend, porque `JsonUtility` lo ignora sin avisar.

---

## 3. Integración en el Editor

1. Abrir Unity y esperar a que compile (0 errores).
2. Seleccionar el objeto con `ApiManager` en `SampleScene` → comprobar que aparece la sección **"Red: tolerancia a fallos"** con los valores por defecto.
3. **Recomendado (siguiente cambio, aún no aplicado):** suscribir `DialogosManager` al evento de error para quitar el bloqueo de "Cargando…". En `DialogosManager.cs`, junto a las suscripciones de la línea ~156:
   ```csharp
   apiManager.DialogosErrorEvent += OnDialogosError;
   ```
   y en la desuscripción de la línea ~331:
   ```csharp
   apiManager.DialogosErrorEvent -= OnDialogosError;
   ```
   con el método:
   ```csharp
   private void OnDialogosError(string fase, string mensaje)
   {
       Debug.LogWarning($"[DialogosManager] Fase {fase} no disponible: {mensaje}");
       // Mostrar 'mensaje' en panelLoading y un botón que llame a apiManager.ReintentarCarga()
   }
   ```
   Mientras no esté suscrito, `ApiManager` avisa en consola de que nadie escucha el error.
4. **Pruebas:**
   - Flujo normal: Login → simulador → llegar a `Entrada3` → los 3 logs de "Diálogos … cargados" aparecen.
   - Error de red: desactivar el Wi-Fi → aparecen 3 Warnings con "RED / CONEXIÓN" y luego el Error final.
   - Error de endpoint: cambiar temporalmente `apiUrl` en el Inspector a `.../apiX` → "ENDPOINT / URL (HTTP 404)" sin reintentos.
   - Reproducir el 500 y **copiar la línea "Cuerpo de la respuesta"** para revisarla en el backend.
5. Commit: `git add Assets/Scripts/legacy/ApiManager.cs CHANGELOG_ApiManagerErrores.md STUDY_NOTES_ManejoErroresHTTP.md`.

Verificación hecha: el script compila sin errores en una comprobación con Mono contra las firmas de la API de Unity. No se ejecutó dentro del Editor.
