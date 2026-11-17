# CHANGELOG — Revisión y merge de `fix/issues-johanw23m`

**Fecha:** 17/11/2026 · **Rama destino:** `limpieza-dependencias-assets` (commit de merge `a27bdcf`)

## 1. Qué entró con el merge (autor: JohanW23M)

| Commit | Issue | Archivos |
|---|---|---|
| `103dd18` | #13 Lip-sync solo en el avatar que habla | `Assets/Scripts/core/Utilities/AvatarLipSync.cs`, `Assets/Scripts/legacy/DialogosManager.cs` |
| `9b012c0` | #12 Retroalimentación de la ficha DSM según el caso | `Assets/Scripts/legacy/VistaFicha.cs`, `Assets/Scripts/legacy/FichaDiagnostico.cs` |
| `ae98b02` | #3 Validación y errores del inicio de sesión | `Assets/LoginLab/LoginUsuario.cs`, `Assets/LoginLab/WebService.cs` |

Merge sin conflictos. Proyecto compila con **0 errores** (solo advertencias antiguas de `Standard Assets` y `UriUtpl` sin uso).

## 2. Corrección añadida durante la revisión

**Problema:** el login nuevo genera mensajes distintos (campos vacíos, credenciales incorrectas, sin conexión, error del servidor), pero los escribe en `txtMensajeError` (TextMeshPro). El panel real de la escena `Iniciar sesion` (`Panel_Datos_Incorrectos/Text_Bienvenida`) es un `UnityEngine.UI.Text` legacy, así que el campo quedaba sin asignar y el jugador siempre veía "Usuario y/o contraseña incorrectos", aunque el motivo fuera otro.

**Cambios:**
- `Assets/LoginLab/LoginUsuario.cs`
  - Nuevo campo `public Text txtMensajeErrorLegacy` (con Tooltip).
  - `MostrarMensaje()` escribe también en ese texto cuando hay mensaje (si el mensaje está vacío no borra el texto original del panel).
- `Assets/Scenes/Iniciar sesion.unity`
  - `LoginController > LoginUsuario.txtMensajeErrorLegacy` → `Panel_Datos_Incorrectos/.../Text_Bienvenida`.
- `Packages/manifest.json`
  - Se vuelve a añadir `com.unity.pipeline` (paquete de conexión Editor ↔ Claude; solo Editor, no va en el build).

## 3. Pruebas en Play (escena `Iniciar sesion`)

| Caso | Resultado |
|---|---|
| Usuario y contraseña vacíos | Panel muestra "Ingresa tu usuario y tu contraseña." ✔ |
| `prueba.claude@utpl.edu.ec` + clave falsa | POST real a `campus3d.utpl.edu.ec`; panel muestra "Usuario o contraseña incorrectos.", botón se reactiva, panel de espera se cierra ✔ |
| Login con cuenta real | **Pendiente** (validar que el servidor acepta el usuario sin `@utpl.edu.ec`) |

## 4. Integración en el Editor
No requiere pasos manuales: la referencia ya quedó guardada en la escena. Si se crea otro panel de error con TextMeshPro, asignarlo en `txtMensajeError`; si es UI legacy, en `txtMensajeErrorLegacy`.

## 5. Pendientes detectados
- **Medio:** texto `FichaC4` es borrador ("la paciente"); validar con el guion clínico del Caso 4.
- **Medio (previo):** token `Authorization` escrito en `WebService.cs`; contraseña guardada en texto plano en `PlayerPrefs`.
- **Bajo:** campo `UriUtpl` sin uso (warning CS0414).
