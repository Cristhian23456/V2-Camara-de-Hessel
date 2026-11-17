// =============================================================================
//  LabPsicologiaUnifier.cs  —  Cámara de Gesell (UTPL XRlab) · Unity 6
//  Unifica las 4 versiones del modelo del laboratorio en UN prefab maestro
//  (PF_LabPsicologia) usado por todas las escenas.
//
//  Menú:  Tools ▸ Lab Psicología ▸ ...
//    1. Comparar versiones (solo lectura)      → reporte .md
//    2. Crear prefab maestro                   → Assets/Models/LabPsicologia/PF_LabPsicologia.prefab
//    3. Unificar escenas al prefab maestro     → reemplaza instancias + re-vincula mallas/materiales
//
//  Requiere Unity 2022.2+ (PrefabUtility.ReplacePrefabAssetOfPrefabInstance).
//  Todo lo que modifica queda respaldado en <proyecto>/Reportes_Limpieza/.
// =============================================================================
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CamaraGesell.EditorTools
{
    public static class LabPsicologiaUnifier
    {
        // ---------------------------------------------------------------------
        // CONFIGURACIÓN
        // ---------------------------------------------------------------------

        /// Fuente del prefab maestro. LabPsicologia1 es la que usa el simulador
        /// (SampleScene: 80 referencias directas a sus mallas + 22 piezas quitadas).
        /// Cambiar SOLO si el reporte del paso 1 recomienda otra.
        const string CanonicalSource = "Assets/Models/LabPsicologia1.fbx";

        /// Todas las versiones conocidas (se buscan también dentro de _Archivo_Obsoleto).
        static readonly string[] KnownVersions =
        {
            "Assets/LabPsicologia.fbx",
            "Assets/Models/LabPsicologia.fbx",
            "Assets/Models/LabPsicologia1.fbx",
            "Assets/Models/LabPsicologia2.fbx",
        };

        const string MasterFolder = "Assets/Models/LabPsicologia";
        const string MasterPath = MasterFolder + "/PF_LabPsicologia.prefab";

        static readonly string[] ScenesToUnify =
        {
            "Assets/Scenes/Iniciar sesion.unity",
            "Assets/Scenes/SampleScene.unity",
        };

        const string MenuRoot = "Tools/Lab Psicología/";

        static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        static string ReportDir { get { var d = Path.Combine(ProjectRoot, "Reportes_Limpieza"); Directory.CreateDirectory(d); return d; } }
        static string Stamp => DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // ---------------------------------------------------------------------
        // Utilidades
        // ---------------------------------------------------------------------
        static List<string> ExistingVersions()
        {
            var all = new List<string>();
            foreach (var p in KnownVersions)
            {
                if (File.Exists(Path.Combine(ProjectRoot, p))) all.Add(p);
                string archived = "Assets/_Archivo_Obsoleto/" + p.Substring("Assets/".Length);
                if (File.Exists(Path.Combine(ProjectRoot, archived))) all.Add(archived);
            }
            return all;
        }

        static string RelPath(Transform t, Transform root)
        {
            if (t == root) return "";
            var parts = new List<string>();
            for (var c = t; c != null && c != root; c = c.parent) parts.Add(c.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        static Transform FindByRelPath(Transform root, string rel)
        {
            if (string.IsNullOrEmpty(rel)) return root;
            return root.Find(rel);
        }

        static bool IsVersionAsset(Object o, HashSet<string> versionPaths)
        {
            if (o == null) return false;
            string p = AssetDatabase.GetAssetPath(o);
            return !string.IsNullOrEmpty(p) && versionPaths.Contains(p);
        }

        static string SourceAssetPathOf(GameObject instanceRoot)
        {
            return PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(instanceRoot);
        }

        // ---------------------------------------------------------------------
        // 1. COMPARAR VERSIONES (solo lectura)
        // ---------------------------------------------------------------------
        class ModelInfo
        {
            public string Path;
            public HashSet<string> Nodes = new HashSet<string>();
            public int Renderers, Vertices, Triangles, Lights, Cameras, Colliders;
            public HashSet<string> Meshes = new HashSet<string>();
            public HashSet<string> Materials = new HashSet<string>();
        }

        static ModelInfo Inspect(string path)
        {
            var info = new ModelInfo { Path = path };
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) return info;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                string rel = RelPath(t, go.transform);
                if (rel.Length > 0) info.Nodes.Add(rel);
            }
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                var m = mf.sharedMesh;
                if (m == null) continue;
                info.Meshes.Add(m.name);
                info.Vertices += m.vertexCount;
                for (int s = 0; s < m.subMeshCount; s++) info.Triangles += (int)(m.GetIndexCount(s) / 3);
            }
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                info.Renderers++;
                foreach (var mat in r.sharedMaterials) if (mat != null) info.Materials.Add(mat.name);
            }
            info.Lights = go.GetComponentsInChildren<Light>(true).Length;
            info.Cameras = go.GetComponentsInChildren<Camera>(true).Length;
            info.Colliders = go.GetComponentsInChildren<Collider>(true).Length;
            return info;
        }

        [MenuItem(MenuRoot + "1. Comparar versiones (solo lectura)", priority = 1)]
        public static void MenuCompare()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var versions = ExistingVersions();
            var infos = versions.Select(Inspect).ToList();
            var canon = infos.FirstOrDefault(i => i.Path == CanonicalSource);

            var sb = new StringBuilder();
            sb.AppendLine($"# Comparación de versiones de LabPsicologia — {DateTime.Now:yyyy-MM-dd HH:mm}\n");
            sb.AppendLine($"Fuente canónica configurada: `{CanonicalSource}`\n");
            sb.AppendLine("| Versión | Nodos | Renderers | Vértices | Triángulos | Mallas | Materiales | Luces | Cámaras | Colliders |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            foreach (var i in infos)
                sb.AppendLine($"| `{i.Path}` | {i.Nodes.Count} | {i.Renderers} | {i.Vertices:N0} | {i.Triangles:N0} | {i.Meshes.Count} | {i.Materials.Count} | {i.Lights} | {i.Cameras} | {i.Colliders} |");

            if (canon != null)
            {
                sb.AppendLine("\n## Diferencias frente a la fuente canónica\n");
                foreach (var i in infos.Where(x => x != canon))
                {
                    var faltan = i.Nodes.Except(canon.Nodes).OrderBy(x => x).ToList();
                    var mallasFaltan = i.Meshes.Except(canon.Meshes).OrderBy(x => x).ToList();
                    var matsFaltan = i.Materials.Except(canon.Materials).OrderBy(x => x).ToList();
                    int comunes = i.Nodes.Intersect(canon.Nodes).Count();
                    float pct = i.Nodes.Count == 0 ? 0 : 100f * comunes / i.Nodes.Count;
                    sb.AppendLine($"### `{i.Path}` — {pct:0}% de sus nodos existen en la canónica");
                    sb.AppendLine($"- Nodos que **no** están en la canónica ({faltan.Count}): " + Lista(faltan));
                    sb.AppendLine($"- Mallas que **no** están en la canónica ({mallasFaltan.Count}): " + Lista(mallasFaltan));
                    sb.AppendLine($"- Materiales que **no** usa la canónica ({matsFaltan.Count}): " + Lista(matsFaltan));
                    sb.AppendLine();
                }
            }

            sb.AppendLine("## Uso en escenas\n");
            var versionSet = new HashSet<string>(versions);
            foreach (var scenePath in ScenesToUnify)
            {
                if (!File.Exists(Path.Combine(ProjectRoot, scenePath))) { sb.AppendLine($"- `{scenePath}`: no existe\n"); continue; }
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                try { DescribeScene(scene, versionSet, canon, sb); }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }

            sb.AppendLine("## Recomendación automática\n");
            if (canon == null) sb.AppendLine("- ⚠️ La fuente canónica no existe. Revisa `CanonicalSource`.");
            else
            {
                bool cubreTodo = infos.Where(x => x != canon).All(x => x.Nodes.Except(canon.Nodes).Count() == 0);
                sb.AppendLine(cubreTodo
                    ? "- ✅ La canónica contiene todos los nodos de las demás versiones: unificar con el paso 3 es seguro (revisar materiales/luces)."
                    : "- ⚠️ Otras versiones tienen nodos que la canónica no tiene (ver listas). Si alguna escena los necesita, " +
                      "exporta un FBX maestro único desde Blender que incluya todo, colócalo en `CanonicalSource` y repite el paso 1.");
            }

            string path = Path.Combine(ReportDir, $"Comparacion_LabPsicologia_{Stamp}.md");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            Debug.Log($"[LabUnifier] Reporte generado: {path}");
            EditorUtility.RevealInFinder(path);
        }

        static string Lista(List<string> l) =>
            l.Count == 0 ? "ninguno" : (l.Count > 40 ? string.Join(", ", l.Take(40).Select(x => $"`{x}`")) + $" … (+{l.Count - 40})" : string.Join(", ", l.Select(x => $"`{x}`")));

        static void DescribeScene(Scene scene, HashSet<string> versionSet, ModelInfo canon, StringBuilder sb)
        {
            sb.AppendLine($"### `{scene.path}`");
            foreach (var root in FindVersionInstances(scene, versionSet))
            {
                string src = SourceAssetPathOf(root);
                var removed = PrefabUtility.GetRemovedGameObjects(root);
                var assetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(src);
                var removedPaths = removed.Where(r => r.assetGameObject != null && assetRoot != null)
                                          .Select(r => RelPath(r.assetGameObject.transform, assetRoot.transform)).ToList();
                int mods = PrefabUtility.GetPropertyModifications(root)?.Length ?? 0;
                int enCanon = canon == null ? 0 : removedPaths.Count(p => canon.Nodes.Contains(p));
                sb.AppendLine($"- Instancia **{root.name}** de `{src}` · {mods} overrides · {removedPaths.Count} piezas quitadas ({enCanon} existen en la canónica)");
                if (removedPaths.Count > 0) sb.AppendLine("  - Quitadas: " + Lista(removedPaths));
            }
            foreach (var go in scene.GetRootGameObjects())
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                var g = t.gameObject;
                if (!PrefabUtility.IsOutermostPrefabInstanceRoot(g)) continue;
                string nearest = SourceAssetPathOf(g);
                var original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(g);
                string originalPath = original != null ? AssetDatabase.GetAssetPath(original) : null;
                if (nearest != null && !versionSet.Contains(nearest) && nearest != MasterPath && originalPath != null && versionSet.Contains(originalPath))
                    sb.AppendLine($"- ⚠️ **{g.name}** usa el prefab derivado `{nearest}` (basado en `{originalPath}`). No se reemplaza automáticamente: sus overrides viven en ese prefab.");
            }
            var direct = FindDirectReferences(scene, versionSet, null);
            if (direct.Count > 0)
            {
                sb.AppendLine($"- Referencias directas a sub-assets (mallas/materiales) fuera de la instancia: **{direct.Count}**");
                foreach (var g in direct.GroupBy(d => AssetDatabase.GetAssetPath(d.value)))
                    sb.AppendLine($"  - `{g.Key}`: {g.Count()} ({string.Join(", ", g.Select(d => d.value.name).Distinct().Take(15))})");
            }
            sb.AppendLine();
        }

        // ---------------------------------------------------------------------
        // 2. CREAR PREFAB MAESTRO
        // ---------------------------------------------------------------------
        [MenuItem(MenuRoot + "2. Crear prefab maestro", priority = 2)]
        public static void MenuCreateMaster()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(MasterPath) != null)
            {
                EditorUtility.DisplayDialog("Lab Psicología", $"El prefab maestro ya existe:\n{MasterPath}", "OK");
                return;
            }
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(CanonicalSource);
            if (src == null)
            {
                EditorUtility.DisplayDialog("Lab Psicología", $"No se encontró la fuente canónica:\n{CanonicalSource}", "OK");
                return;
            }
            CreateMaster(src);
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(MasterPath));
        }

        static GameObject CreateMaster(GameObject src)
        {
            if (!AssetDatabase.IsValidFolder(MasterFolder))
                AssetDatabase.CreateFolder("Assets/Models", "LabPsicologia");

            // Guardar una instancia de un modelo como prefab crea un *Prefab Variant* del FBX:
            // hereda la geometría del FBX y permite añadir componentes (colliders, scripts) sin tocarlo.
            var temp = (GameObject)PrefabUtility.InstantiatePrefab(src);
            try
            {
                temp.name = "PF_LabPsicologia";
                var master = PrefabUtility.SaveAsPrefabAsset(temp, MasterPath, out bool ok);
                if (!ok) throw new Exception("SaveAsPrefabAsset falló");
                Debug.Log($"[LabUnifier] Prefab maestro creado: {MasterPath} (variante de {CanonicalSource})");
                return master;
            }
            finally { Object.DestroyImmediate(temp); }
        }

        // ---------------------------------------------------------------------
        // 3. UNIFICAR ESCENAS
        // ---------------------------------------------------------------------
        [MenuItem(MenuRoot + "3. Unificar escenas al prefab maestro", priority = 3)]
        public static void MenuUnify()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var master = AssetDatabase.LoadAssetAtPath<GameObject>(MasterPath);
            if (master == null)
            {
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(CanonicalSource);
                if (src == null) { EditorUtility.DisplayDialog("Lab Psicología", "No existe la fuente canónica.", "OK"); return; }
                master = CreateMaster(src);
            }

            var scenes = ScenesToUnify.Where(s => File.Exists(Path.Combine(ProjectRoot, s))).ToList();
            if (!EditorUtility.DisplayDialog("Unificar Lab Psicología",
                    "Se reemplazarán TODAS las instancias de las 4 versiones del laboratorio por PF_LabPsicologia en:\n\n" +
                    string.Join("\n", scenes) +
                    "\n\nSe hace backup de cada escena en Reportes_Limpieza/. ¿Continuar?", "Unificar", "Cancelar"))
                return;

            var versionSet = new HashSet<string>(ExistingVersions());
            var canonicalSubAssets = AssetDatabase.LoadAllAssetsAtPath(CanonicalSource);
            string backup = Path.Combine(ReportDir, $"backup_unificar_lab_{Stamp}");
            Directory.CreateDirectory(backup);
            var log = new StringBuilder("escena;accion;objeto;detalle\n");

            foreach (var scenePath in scenes)
            {
                File.Copy(Path.Combine(ProjectRoot, scenePath), Path.Combine(backup, Path.GetFileName(scenePath)), true);
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                // 3a. Reemplazar instancias del modelo por el prefab maestro
                foreach (var root in FindVersionInstances(scene, versionSet))
                {
                    string oldSrc = SourceAssetPathOf(root);
                    var oldAssetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(oldSrc);
                    var removedPaths = PrefabUtility.GetRemovedGameObjects(root)
                        .Where(r => r.assetGameObject != null && oldAssetRoot != null)
                        .Select(r => RelPath(r.assetGameObject.transform, oldAssetRoot.transform)).ToList();

                    var settings = new PrefabReplacingSettings
                    {
                        objectMatchMode = ObjectMatchMode.ByHierarchy,
                        // prefabOverridesOptions se deja por defecto: conserva todos los overrides posibles.
                        changeRootNameToAssetName = false,
                        logInfo = true,
                    };
                    PrefabUtility.ReplacePrefabAssetOfPrefabInstance(root, master, settings, InteractionMode.AutomatedAction);
                    log.AppendLine($"{scenePath};reemplazo;{root.name};{oldSrc} -> {MasterPath}");

                    // Las piezas que la escena había QUITADO se vuelven a ocultar si el maestro las tiene.
                    // Se desactivan (no se borran): es reversible y no depende de cómo Unity mapee los "removed".
                    var stillRemoved = new HashSet<string>(PrefabUtility.GetRemovedGameObjects(root)
                        .Where(r => r.assetGameObject != null)
                        .Select(r => RelPath(r.assetGameObject.transform, master.transform)));
                    foreach (var rel in removedPaths)
                    {
                        if (stillRemoved.Contains(rel)) continue;
                        var t = FindByRelPath(root.transform, rel);
                        if (t == null) { log.AppendLine($"{scenePath};pieza-quitada-no-existe-en-maestro;{rel};"); continue; }
                        if (t.gameObject.activeSelf)
                        {
                            Undo.RecordObject(t.gameObject, "Ocultar pieza");
                            t.gameObject.SetActive(false);
                            log.AppendLine($"{scenePath};pieza-ocultada;{rel};estaba quitada en la versión anterior");
                        }
                    }
                }

                // 3b. Re-vincular referencias directas a mallas/materiales de versiones viejas
                foreach (var d in FindDirectReferences(scene, versionSet, CanonicalSource))
                {
                    var match = canonicalSubAssets.FirstOrDefault(o => o != null && o.GetType() == d.value.GetType() && o.name == d.value.name);
                    if (match != null)
                    {
                        d.so.FindProperty(d.propertyPath).objectReferenceValue = match;
                        d.so.ApplyModifiedPropertiesWithoutUndo();
                        log.AppendLine($"{scenePath};revinculado;{d.ownerPath};{d.propertyPath}: {d.value.name} -> {CanonicalSource}");
                    }
                    else
                        log.AppendLine($"{scenePath};SIN-EQUIVALENTE;{d.ownerPath};{d.propertyPath}: '{d.value.name}' ({d.value.GetType().Name}) de {AssetDatabase.GetAssetPath(d.value)}");
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            string logPath = Path.Combine(backup, "unificacion_log.csv");
            File.WriteAllText(logPath, log.ToString(), new UTF8Encoding(false));
            int sinEq = log.ToString().Split('\n').Count(l => l.Contains(";SIN-EQUIVALENTE;"));
            Debug.Log($"[LabUnifier] Unificación completa. Bitácora: {logPath}" + (sinEq > 0 ? $"  ⚠️ {sinEq} referencias sin equivalente en la canónica." : ""));
            EditorUtility.DisplayDialog("Lab Psicología",
                "Unificación completa.\n\n" + (sinEq > 0 ? $"⚠️ {sinEq} referencias no tienen equivalente en la versión canónica (ver bitácora).\n\n" : "") +
                "Siguiente paso: revisa visualmente ambas escenas y luego ejecuta\nTools ▸ Limpieza Proyecto ▸ 1 y 3 para archivar las versiones viejas.", "OK");
        }

        // ---------------------------------------------------------------------
        // Búsquedas en escena
        // ---------------------------------------------------------------------
        static List<GameObject> FindVersionInstances(Scene scene, HashSet<string> versionSet)
        {
            var result = new List<GameObject>();
            foreach (var go in scene.GetRootGameObjects())
            {
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                {
                    var g = t.gameObject;
                    // Solo instancias de nivel superior: una instancia anidada dentro de otro prefab
                    // debe cambiarse en ese prefab, no en la escena.
                    if (!PrefabUtility.IsOutermostPrefabInstanceRoot(g)) continue;
                    string src = SourceAssetPathOf(g);
                    if (src != null && versionSet.Contains(src)) result.Add(g);
                }
            }
            return result;
        }

        class DirectRef
        {
            public SerializedObject so;
            public string propertyPath;
            public Object value;
            public string ownerPath;
        }

        /// Referencias serializadas (MeshFilter.m_Mesh, MeshCollider.m_Mesh, Renderer.m_Materials…) que apuntan a
        /// sub-assets de alguna versión del laboratorio distinta de 'exceptPath'.
        static List<DirectRef> FindDirectReferences(Scene scene, HashSet<string> versionSet, string exceptPath)
        {
            var refs = new List<DirectRef>();
            foreach (var root in scene.GetRootGameObjects())
            foreach (var comp in root.GetComponentsInChildren<Component>(true))
            {
                if (comp == null || comp is Transform) continue;
                var so = new SerializedObject(comp);
                var it = so.GetIterator();
                while (it.Next(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var v = it.objectReferenceValue;
                    if (v == null || v is GameObject || v is Component) continue; // solo mallas, materiales, avatares…
                    if (!IsVersionAsset(v, versionSet)) continue;
                    if (exceptPath != null && AssetDatabase.GetAssetPath(v) == exceptPath) continue;
                    // Si la propiedad viene del propio prefab (sin override) el reemplazo ya la resolvió.
                    if (PrefabUtility.IsPartOfPrefabInstance(comp) && !it.prefabOverride) continue;
                    refs.Add(new DirectRef
                    {
                        so = so,
                        propertyPath = it.propertyPath,
                        value = v,
                        ownerPath = RelPath(comp.transform, null) + " (" + comp.GetType().Name + ")",
                    });
                }
            }
            return refs;
        }
    }
}
#endif
