# CHANGELOG — Limpieza de Assets y Dependencias

**Proyecto:** Cámara de Gesell VR · Unity 6000.3.18f1 · UTPL XRlab
**Fecha:** 2026-11-12
**Rama de trabajo:** `limpieza-dependencias-assets`

---

## ⛔ 0. Bloqueante encontrado antes de tocar nada: el repositorio está vacío

Durante la auditoría de `.git` se confirmó lo siguiente:

| Ref | Commit | Estado |
|---|---|---|
| `main` (local) | `a65de49` "rama mal trabajada" | **árbol vacío** (`4b825dc…`) |
| `origin/main` (GitHub) | `a65de49` | **árbol vacío**, ya está subido |
| `limpieza-dependencias-assets` | `a65de49` | creada desde el commit vacío |
| commit padre | `6e1ca0e` | **último estado válido** del proyecto |

Ese commit **borra todos los archivos del repositorio**. Los archivos siguen en tu disco, pero Git los ve como *no rastreados* (el índice tiene 0 entradas). Consecuencia: ahora mismo no existe una red de seguridad en Git para la limpieza, y quien clone `main` recibe un proyecto vacío.

Se rescata **sin reescribir el historial público** (ejecutar en Git Bash, en la raíz del proyecto, con Unity cerrado):

```bash
git branch respaldo/commit-vacio a65de49          # deja una etiqueta al commit vacío
git reset --mixed 6e1ca0e                          # la rama vuelve al último estado bueno; NO toca el disco
git status                                         # ahora solo aparecen tus cambios reales
git add -A && git commit -m "Restaurar proyecto completo (estado previo a limpieza)"
git merge -s ours origin/main -m "Descartar commit vacio a65de49 de main"
git push -u origin limpieza-dependencias-assets
```

`merge -s ours` registra el commit vacío como "ya integrado", pero conserva tu árbol de archivos. Así, cuando fusiones esta rama en `main` (PR), `main` avanza **con todos los archivos** y sin `push --force`. Las STUDY_NOTES explican el porqué.

> Haz este paso **antes** del menú 3 (mover huérfanos). Además, copia o comprime la carpeta del proyecto como respaldo extra.

---

## 1. Rastreo de dependencias: resultados ya verificados

Escenas del flujo real, confirmadas leyendo el código y los eventos de UI:

- `Iniciar sesion.unity` → `LoginUsuario.cs` carga `SceneManager.LoadScene("SampleScene")` y los botones llaman a `SceneChange.changeScena("SampleScene")`.
- `SampleScene.unity` → contiene `DialogosManager` con `casoConfigs` para el **Caso 1** (`TerapeutaC1`/`PacienteC1`) y el **Caso 4** (`TerapeutaC4`/`PacienteC4`).
- `Pruebas.unity` → **ningún script ni evento la carga**. Sí está en Build Settings, por lo que se incluiría en el build sin motivo.
- `_Recovery/0.unity`, `0 (1).unity`, `0 (2).unity` → autoguardados de Unity tras un crash, sin uso.

### Los "laboratorios" duplicados

No hay 3 sino **4 copias** del modelo del laboratorio, y **no existe un archivo llamado "Lab Psicologia Import Assets"** en `Assets/`. El nombre real es `LabPsicologia*.fbx`:

| Asset | GUID | Tamaño | Usado por | Veredicto |
|---|---|---|---|---|
| `Assets/LabPsicologia.fbx` (raíz) | `be41261208d0ef741b72175357759cc0` | 3,95 MB | **Iniciar sesion.unity** (18 refs) + 3 escenas `_Recovery` | ✅ **Canónico para Login** |
| `Assets/Models/LabPsicologia1.fbx` | `a5a23a962f65cc54da2b75eb1620abfb` | 4,32 MB | **SampleScene.unity** (104 refs), `Prefabs/LabPsicologia1.prefab`, `Pruebas.unity` | ✅ **Canónico para el Simulador (casos 1 y 4)** |
| `Assets/Models/LabPsicologia.fbx` | `7b6f68ed74a94944a86b7511123ce121` | 3,95 MB | solo `Pruebas.unity` (huérfana) | 🗄️ Archivar |
| `Assets/Models/LabPsicologia2.fbx` | `e42c4d1d182dbd941a50207ee238d6be` | 4,30 MB | **nadie** (0 refs) | 🗄️ Archivar |

**Decisión de consolidación: NO se fuerza el re-vínculo entre versiones distintas.** `LabPsicologia.fbx` y `LabPsicologia1.fbx` son archivos diferentes (3,95 MB frente a 4,32 MB) con remapeos de materiales distintos en sus `.meta` (25 frente a 7 entradas de `externalObjects`). Si Login pasara a apuntar a `LabPsicologia1`, se romperían los 18 *overrides* de la instancia del prefab y cambiarían la geometría y los materiales: es exactamente el "colapso" que viste al tocar uno. Lo seguro es:

1. Archivar las copias que no usa ningún flujo (`Models/LabPsicologia.fbx`, `Models/LabPsicologia2.fbx`, sus texturas `.fbm` si quedan huérfanas, y `Pruebas.unity`).
2. Re-vincular automáticamente **solo** duplicados exactos (mismo MD5 **y** `.meta` idéntico).
3. Unificar Login y Simulador en un único modelo queda como tarea propia de QA visual (ver "Pendientes").

---

## 2. Archivos creados o modificados

| Archivo | Tipo | Cambio |
|---|---|---|
| `Assets/Editor/Limpieza/ProjectCleanupTool.cs` | **Nuevo** · script de Editor | Rastreo de GUIDs, re-vínculo, archivado y reversión |
| `.gitattributes` | **Nuevo** | Git LFS para modelos, texturas, audio, video y fuentes; fin de línea LF para YAML/C# |
| `.gitignore` | Modificado | Añade `.idea/` (incluida `Assets/Scripts/.idea`), `*.slnx`, `Assets/_Recovery/`, `Reportes_Limpieza/`, `scratch/` y basura del SO |
| `CHANGELOG_LimpiezaAssets.md` | Nuevo | Este documento |
| `STUDY_NOTES_ManejoDependencias.md` | Nuevo | Fundamentos técnicos |
| `ProjectSettings/EditorBuildSettings.asset` | Lo modifica la herramienta (menú 3) | Quita `Pruebas.unity` del build |
| `Assets/_Archivo_Obsoleto/` | Lo crea la herramienta (menú 3) | Recibe los huérfanos conservando su ruta relativa |

No se tocan `Assets/LoginLab`, `Assets/Scripts`, ninguna carpeta `Resources` (audios de preguntas y retroalimentación, video del tutor), `TextMesh Pro` ni `Standard Assets`. Están protegidos por código.

---

## 3. Qué hace `ProjectCleanupTool.cs`

Menú **Tools ▸ Limpieza Proyecto**:

| Opción | Acción | ¿Modifica? |
|---|---|---|
| **1. Analizar dependencias** | Calcula el grafo desde las escenas principales, las carpetas protegidas y los assets precargados; indexa cada `guid:` de los archivos YAML y `.meta`; detecta duplicados por MD5 y variantes por nombre. Genera `Reportes_Limpieza/Reporte_Dependencias_<fecha>.md` | No |
| **2. Re-vincular duplicados exactos** | Reescribe `guid: duplicado → guid: canónico` en escenas, prefabs, materiales y `.meta`. Antes hace backup de cada archivo y guarda una bitácora CSV | Sí (con backup) |
| **3. Mover huérfanos a `_Archivo_Obsoleto`** | Recalcula el análisis, pide confirmación, mueve con `AssetDatabase.MoveAsset` (el GUID se conserva), quita las escenas archivadas del build, borra las carpetas que quedan vacías y guarda `movimientos_<fecha>.csv` | Sí (reversible) |
| **4. Revertir último movimiento** | Lee el último CSV y devuelve cada asset a su ruta original | Sí |
| **Rastrear referencias del asset seleccionado** | Selecciona cualquier asset en Project y lista en la consola quién lo referencia | No |

Salvaguardas incluidas: exige *Force Text*, guarda y cierra las escenas abiertas antes de editar YAML en disco, crea las carpetas fuera de `StartAssetEditing` (evita carpetas duplicadas "Models 1"), cancela si falta una escena principal y nunca reutiliza un análisis viejo.

Verificado: el script compila sin errores contra las firmas de la API de Unity 6 usadas (compilación de comprobación con Mono). **No se ha ejecutado dentro de Unity**: este entorno no tiene acceso a tu Editor.

---

## 4. Integración en el Editor (en orden)

1. **Unity cerrado** → ejecutar el rescate de Git de la sección 0 y hacer un respaldo `.zip` de la carpeta.
2. Instalar LFS una vez por PC: `git lfs install`.
3. Abrir el proyecto en Unity 6000.3.18f1 y esperar a que compile (aparecerá el menú **Tools ▸ Limpieza Proyecto**).
4. **1. Analizar dependencias** → abrir el reporte. Comprobar que la tabla `labpsicologia` coincide con la de la sección 1 y revisar la lista de huérfanos. Si algo de la lista se carga por nombre desde código fuera de `Resources`, añadir su carpeta a `ProtectedFolders` y repetir.
5. **2. Re-vincular duplicados exactos** → aceptar solo si el reporte lista grupos "fusionables".
6. **3. Mover huérfanos** → confirmar. Revisar la consola: 0 errores.
7. **Prueba de humo en Play:** Login → entrar al simulador → completar Caso 1 → Caso 4 (diálogos, audio y lip-sync, ficha diagnóstica, Beck). Ante cualquier *Missing*, usar **4. Revertir**.
8. **File ▸ Build Profiles**: comprobar que solo quedan `Iniciar sesion` (índice 0) y `SampleScene`.
9. Commit:
   ```bash
   git add .gitattributes && git add --renormalize .
   git add -A
   git commit -m "Limpieza: archivar huérfanos, Git LFS y gitignore"
   git lfs ls-files | head        # deben aparecer .fbx, .wav, .mp4, .png…
   git push
   ```
10. **Prueba de clon limpio** en otra carpeta o PC: `git clone <url>` → `git lfs pull` → abrir en Unity → 0 errores en consola → repetir la prueba de humo.

---

## 5. Hallazgos adicionales registrados (no corregidos en este cambio)

- **Historial de 1,02 GB sin LFS:** `.git/objects/pack` pesa 1.019.975.256 bytes. Los binarios se subieron como blobs normales. `.gitattributes` solo afecta a commits nuevos. Para el **repositorio institucional** se recomienda empezar historial limpio (primer commit ya con `.gitattributes`) en vez de migrar este.
- **Sin paquete XR:** `Packages/manifest.json` no incluye OpenXR, Meta XR SDK ni XR Interaction Toolkit (solo el módulo `com.unity.modules.xr`). Hay que confirmar con qué se ejecuta en VR antes de la fase de optimización.
- `Assets/LabPsicologia.fbx` vive en la raíz de `Assets/`. Se puede mover a `Models/` con `MoveAsset` cuando ya no exista `Models/LabPsicologia.fbx` (tras el menú 3).
- `Assets/Scripts/.idea/` es basura de Rider dentro de `Assets`. Unity la ignora por empezar con punto y ahora Git también.
- `core/Data/CaseDataValidator.cs` pesa 0 bytes: es un script vacío.

## 6. Pendientes

- [ ] Unificar el laboratorio de Login y del Simulador en un único FBX, con QA visual.
- [ ] Decidir si `_Archivo_Obsoleto` se elimina definitivamente tras 1–2 ciclos de QA sin incidencias.
- [ ] Configurar UnityYAMLMerge en las PCs del equipo.
