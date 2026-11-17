# CHANGELOG — Unificación del modelo Lab Psicología

**Proyecto:** Cámara de Gesell · Unity 6000.3.18f1
**Fecha:** 2026-11-13
**Objetivo:** que todas las escenas (las actuales y las que se modelen después) usen **un único** laboratorio: `PF_LabPsicologia`.

---

## 1. Análisis: las 4 versiones NO son copias

Datos extraídos de los `.meta` y del YAML de las escenas:

| Versión | Tamaño | Materiales remapeados | Luces/cámaras del FBX | Colliders generados | Uso |
|---|---|---|---|---|---|
| `Assets/LabPsicologia.fbx` | 3,95 MB | **24** (fachada ED7, murales, zinc, vereda, cafetería…) | **Sí / Sí** | No | **Iniciar sesion**: 13 overrides, **4 piezas quitadas**, 4 refs directas |
| `Assets/Models/LabPsicologia.fbx` | 3,95 MB | 0 (materiales embebidos) | Sí / No | Sí | Solo `Pruebas.unity` (huérfana) |
| `Assets/Models/LabPsicologia1.fbx` | 4,32 MB | 6 | No / No | **Sí** | **SampleScene**: 23 overrides, **22 piezas quitadas**, **80 refs directas a sus mallas** + `Prefabs/LabPsicologia1.prefab` (98 overrides) |
| `Assets/Models/LabPsicologia2.fbx` | 4,30 MB | 13 | No / No | Sí | Nadie |

### Decisión: maestro basado en `LabPsicologia1.fbx`

1. **Es la que más depende del resto del proyecto**: el simulador (casos 1 y 4) tiene 80 referencias directas a sus mallas y 22 piezas quitadas. Cambiarla es lo más riesgoso; mantenerla como base deja `SampleScene` **intacta en geometría**.
2. **Genera colliders**, que el recorrido del estudiante necesita.
3. `Iniciar sesion` es el decorado de fondo del login: solo tiene 13 overrides y 4 referencias directas, así que es la escena más barata de migrar.

### ⚠️ Riesgo a verificar visualmente: la escena de Login

La versión de Login es la única que remapea 24 materiales e importa luces y cámaras del FBX. Al pasar al maestro:
- Las piezas que también existan en `LabPsicologia1` se verán con **sus** materiales (solo 6 remapeados, el resto embebidos), así que el exterior puede verse distinto.
- Las **luces** que venían dentro del FBX de Login desaparecen. Si el login queda oscuro, se añaden luces de escena.
- Si el reporte del paso 1 muestra **nodos de Login que no existen** en `LabPsicologia1` (p. ej. la fachada exterior), hay que aplicar la **opción B**.

**Opción B (la definitiva para seguir modelando):** exportar desde Blender **un único FBX maestro** con todo (interior y exterior), sustituir el contenido de `LabPsicologia1.fbx` **conservando su `.meta`** (mismo GUID, así no se rompe ninguna escena) y volver a ejecutar los pasos 1 y 3. Cada escena muestra u oculta lo que necesita.

---

## 2. Archivos creados

| Archivo | Tipo | Qué hace |
|---|---|---|
| `Assets/Editor/Limpieza/LabPsicologiaUnifier.cs` | **Nuevo** · script de Editor | Menú **Tools ▸ Lab Psicología** |
| `Assets/Models/LabPsicologia/PF_LabPsicologia.prefab` | Lo crea la herramienta (paso 2) | Prefab Variant de `LabPsicologia1.fbx`, el laboratorio único |
| `Iniciar sesion.unity`, `SampleScene.unity` | Los modifica la herramienta (paso 3) | Su instancia pasa a ser de `PF_LabPsicologia` |

### Menú `Tools ▸ Lab Psicología`

| Opción | Acción | ¿Modifica? |
|---|---|---|
| **1. Comparar versiones** | Para cada versión: nodos, mallas, vértices, triángulos, materiales, luces, cámaras y colliders. Diferencias frente a la canónica, piezas quitadas y referencias directas en cada escena, y una recomendación. Reporte en `Reportes_Limpieza/Comparacion_LabPsicologia_<fecha>.md` | No |
| **2. Crear prefab maestro** | Crea `PF_LabPsicologia.prefab` como **Prefab Variant** de `LabPsicologia1.fbx` | Crea 1 asset |
| **3. Unificar escenas** | Backup de las escenas → reemplaza cada instancia de cualquier versión por el maestro con `PrefabUtility.ReplacePrefabAssetOfPrefabInstance` (emparejando por ruta de jerarquía y conservando overrides) → **vuelve a ocultar** las piezas que la escena había quitado → re-vincula mallas y materiales de versiones viejas al sub-asset del mismo nombre en la canónica → guarda. Bitácora `unificacion_log.csv` | Sí (con backup) |

**Salvaguardas:**
- Solo procesa instancias de nivel superior.
- Si una escena usa un prefab derivado (como el viejo `Prefabs/LabPsicologia1.prefab`), **avisa y no lo toca**.
- Las piezas antes quitadas se **desactivan** en vez de borrarse (reversible).
- Las referencias sin equivalente quedan como `SIN-EQUIVALENTE` en la bitácora y en un aviso.

Verificado: compila sin errores contra las firmas de la API de Unity usadas (comprobación con Mono). **No se ha ejecutado en el Editor.**

---

## 3. Integración en el Editor

1. **Antes:** commit o `.zip` del proyecto (recuerda el estado de `main`, bug A8 del README).
2. Copiar `LabPsicologiaUnifier.cs` a `Assets/Editor/Limpieza/` y esperar a que compile.
3. **Tools ▸ Lab Psicología ▸ 1. Comparar versiones** → abrir el reporte y mirar:
   - el % de nodos de `Assets/LabPsicologia.fbx` (Login) que existen en la canónica,
   - la lista "Nodos que no están en la canónica".
   Con 100 % (o con piezas faltantes que Login no necesita), seguir. Si faltan piezas visibles en Login, aplicar la **opción B** antes.
4. **2. Crear prefab maestro**.
5. **3. Unificar escenas** → aceptar. Leer el resumen final.
6. **QA visual:**
   - `Iniciar sesion`: comparar con una captura de antes (materiales del exterior, iluminación, encuadre de cámara).
   - `SampleScene`: recorrido completo (entrada, salas, Cámara de Gesell). Las colisiones y las piezas ocultas deben comportarse igual.
   - Si algo falla: restaurar las escenas desde `Reportes_Limpieza/backup_unificar_lab_<fecha>/`.
7. Archivar las versiones viejas: **Tools ▸ Limpieza Proyecto ▸ 1** (comprobar que las 3 FBX antiguas y `Prefabs/LabPsicologia1.prefab` aparecen como huérfanos) **→ 3**.
8. Commit: `git add -A && git commit -m "Unificar Lab Psicología en PF_LabPsicologia"`.

### Para las escenas nuevas que vas a modelar
- Arrastra **siempre `PF_LabPsicologia`** a la escena, nunca un `.fbx`.
- Lo que cambie solo en esa escena (piezas ocultas, posiciones) son **overrides de instancia**. Si la misma configuración se repite en varias escenas, crea un **Prefab Variant** (`PF_LabPsicologia_Exterior`, por ejemplo).
- Si cambias la geometría en Blender, **re-exporta sobre el mismo archivo** `LabPsicologia1.fbx` (no crees `LabPsicologia3.fbx`): el GUID se conserva y todas las escenas se actualizan solas.
