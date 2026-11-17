// =============================================================================
//  LabPsicologiaUnifier.cs  —  Cámara de Gesell (UTPL XRlab) · Unity 6
//  Deja Models/LabPsicologia1.fbx como ÚNICO modelo del laboratorio.
//
//  Menú:  Tools ▸ Lab Psicología ▸ ...
//    1. Comparar versiones (solo lectura)
//    2. Unificar escenas en LabPsicologia1
//    3. Descartar versiones antiguas (a la Papelera)
//    4. Eliminar duplicados restaurados (a la Papelera)
//
//  Todo va a la Papelera de Windows (AssetDatabase.MoveAssetsToTrash), no se borra
//  permanentemente. Reportes y backups en <proyecto>/Reportes_Limpieza/.
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
        /// Modelo principal y único del laboratorio.
        const string CanonicalSource = "Assets/Models/LabPsicologia1.fbx";

        /// Versiones a descartar (se buscan también sus copias en _Archivo_Obsoleto).
        static readonly string[] DiscardedVersions =
        {
            "Assets/LabPsicologia.fbx",
            "Assets/Models/LabPsicologia.fbx",
            "Assets/Models/LabPsicologia2.fbx",
        };

        /// Otros assets que solo existen por las versiones descartadas.
        static readonly string[] DiscardedExtras =
        {
            "Assets/Models/LabPsicologia.fbm",      // texturas extraídas de Models/LabPsicologia.fbx
            "Assets/Models/LabPsicologia2.fbm",     // texturas extraídas de LabPsicologia2.fbx
            "Assets/Prefabs/LabPsicologia1.prefab", // variante antigua con 98 overrides, sin uso
            "Assets/Scenes/Pruebas.unity",          // escena de comparación de versiones, fuera del build
        };

        /// Todas las escenas del proyecto (fuera de _Archivo_Obsoleto y de las descartadas):
        /// incluye escenas nuevas en las que se esté trabajando, aunque no estén en el build.
        static List<string> ScenesToUnify()
        {
            var discarded = new HashSet<string>(DiscardedExtras);
            return AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.StartsWith(ArchiveRoot + "/") && !discarded.Contains(p))
                .OrderBy(p => p).ToList();
        }

        const string ArchiveRoot = "Assets/_Archivo_Obsoleto";
        const string MenuRoot = "Tools/Lab Psicología/";

        static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        static string ReportDir { get { var d = Path.Combine(ProjectRoot, "Reportes_Limpieza"); Directory.CreateDirectory(d); return d; } }
        static string Stamp => DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // ---------------------------------------------------------------------
        // Utilidades
        // ---------------------------------------------------------------------
        static bool Exists(string assetPath) =>
            File.Exists(Path.Combine(ProjectRoot, assetPath)) || AssetDatabase.IsValidFolder(assetPath);

        static string Archived(string assetPath) => ArchiveRoot + "/" + assetPath.Substring("Assets/".Length);

        /// Rutas existentes de las versiones descartadas (original + copia archivada).
        static List<string> DiscardedModelPaths()
        {
            var l = new List<string>();
            foreach (var p in DiscardedVersions)
            {
                if (Exists(p)) l.Add(p);
                if (Exists(Archived(p))) l.Add(Archived(p));
            }
            return l;
        }

        static HashSet<string> AllVersionPaths()
        {
            var s = new HashSet<string>(DiscardedModelPaths());
            s.Add(CanonicalSource);
            return s;
        }

        static string RelPath(Transform t, Transform root)
        {
            if (t == root) return "";
            var parts = new List<string>();
            for (var c = t; c != null && c != root; c = c.parent) parts.Add(c.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        static Transform FindByRelPath(Transform root, string rel) => string.IsNullOrEmpty(rel) ? root : root.Find(rel);

        static long SizeOf(IEnumerable<string> assetPaths)
        {
            long total = 0;
            foreach (var p in assetPaths)
            {
                string full = Path.Combine(ProjectRoot, p);
                if (File.Exists(full)) total += new FileInfo(full).Length;
                else if (Directory.Exists(full))
                    total += new DirectoryInfo(full).GetFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
            }
            return total;
        }

        static string MB(long bytes) => $"{bytes / (1024f * 1024f):0.0} MB";

        /// Cierre de dependencias: escenas del build + TODAS las escenas de trabajo + carpetas Resources.
        static HashSet<string> GameDependencyClosure()
        {
            var roots = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
            roots.AddRange(ScenesToUnify());
            roots.AddRange(AssetDatabase.GetAllAssetPaths().Where(p => p.Contains("/Resources/") && !p.StartsWith(ArchiveRoot + "/")));
            return new HashSet<string>(AssetDatabase.GetDependencies(roots.ToArray(), true));
        }

        // ---------------------------------------------------------------------
        // 1. COMPARAR VERSIONES (solo lectura)
        // ---------------------------------------------------------------------
        class ModelInfo
        {
            public string Path;
            public HashSet<string> Nodes = new HashSet<string>();
            public HashSet<string> Materials = new HashSet<string>();
            public int Renderers, Vertices, Lights, Cameras, Colliders;
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
                if (mf.sharedMesh != null) info.Vertices += mf.sharedMesh.vertexCount;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                info.Renderers++;
                foreach (var m in r.sharedMaterials) if (m != null) info.Materials.Add(m.name);
            }
            info.Lights = go.GetComponentsInChildren<Light>(true).Length;
            info.Cameras = go.GetComponentsInChildren<Camera>(true).Length;
            info.Colliders = go.GetComponentsInChildren<Collider>(true).Length;
            return info;
        }

        static string Lista(IEnumerable<string> items)
        {
            var l = items.ToList();
            if (l.Count == 0) return "ninguno";
            return string.Join(", ", l.Take(40).Select(x => $"`{x}`")) + (l.Count > 40 ? $" … (+{l.Count - 40})" : "");
        }

        [MenuItem(MenuRoot + "1. Comparar versiones (solo lectura)", priority = 1)]
        public static void MenuCompare()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var infos = new List<ModelInfo> { Inspect(CanonicalSource) };
            infos.AddRange(DiscardedModelPaths().Select(Inspect));
            var canon = infos[0];

            var sb = new StringBuilder();
            sb.AppendLine($"# Comparación de versiones de LabPsicologia — {DateTime.Now:yyyy-MM-dd HH:mm}\n");
            sb.AppendLine($"Modelo principal: `{CanonicalSource}`\n");
            sb.AppendLine("| Versión | GUID | Nodos | Renderers | Vértices | Materiales | Luces | Cámaras | Colliders | % nodos en principal |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            foreach (var i in infos)
            {
                float pct = i.Nodes.Count == 0 ? 0 : 100f * i.Nodes.Intersect(canon.Nodes).Count() / i.Nodes.Count;
                sb.AppendLine($"| `{i.Path}` | `{AssetDatabase.AssetPathToGUID(i.Path)}` | {i.Nodes.Count} | {i.Renderers} | {i.Vertices:N0} | {i.Materials.Count} | {i.Lights} | {i.Cameras} | {i.Colliders} | {pct:0}% |");
            }
            foreach (var i in infos.Skip(1))
            {
                sb.AppendLine($"\n### `{i.Path}` frente al principal");
                sb.AppendLine("- Nodos que el principal no tiene: " + Lista(i.Nodes.Except(canon.Nodes).OrderBy(x => x)));
                sb.AppendLine("- Materiales que el principal no usa: " + Lista(i.Materials.Except(canon.Materials).OrderBy(x => x)));
            }

            sb.AppendLine("\n## Uso en escenas\n");
            var versions = AllVersionPaths();
            foreach (var sp in ScenesToUnify())
            {
                var scene = EditorSceneManager.OpenScene(sp, OpenSceneMode.Additive);
                try
                {
                    sb.AppendLine($"### `{sp}`");
                    foreach (var root in FindVersionInstances(scene, versions))
                    {
                        string src = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
                        var assetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(src);
                        var removed = PrefabUtility.GetRemovedGameObjects(root).Where(r => r.assetGameObject != null && assetRoot != null)
                            .Select(r => RelPath(r.assetGameObject.transform, assetRoot.transform)).ToList();
                        sb.AppendLine($"- **{root.name}** usa `{src}` · {PrefabUtility.GetPropertyModifications(root)?.Length ?? 0} overrides · {removed.Count} piezas quitadas" +
                                      (src == CanonicalSource ? " ✅ ya es el principal" : " → se migrará"));
                        if (removed.Count > 0) sb.AppendLine("  - Quitadas: " + Lista(removed));
                    }
                    var direct = FindDirectReferences(scene, versions, CanonicalSource);
                    sb.AppendLine($"- Referencias directas a versiones descartadas: **{direct.Count}** " +
                                  (direct.Count > 0 ? "(" + string.Join(", ", direct.Select(d => d.value.name).Distinct().Take(15)) + ")" : ""));
                    sb.AppendLine();
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }

            string path = Path.Combine(ReportDir, $"Comparacion_LabPsicologia_{Stamp}.md");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            Debug.Log($"[LabUnifier] Reporte: {path}");
            EditorUtility.RevealInFinder(path);
        }

        // ---------------------------------------------------------------------
        // 2. UNIFICAR ESCENAS EN LabPsicologia1
        // ---------------------------------------------------------------------
        [MenuItem(MenuRoot + "2. Unificar escenas en LabPsicologia1", priority = 2)]
        public static void MenuUnify()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var target = AssetDatabase.LoadAssetAtPath<GameObject>(CanonicalSource);
            if (target == null) { EditorUtility.DisplayDialog("Lab Psicología", $"No existe {CanonicalSource}", "OK"); return; }

            var scenes = ScenesToUnify();
            if (!EditorUtility.DisplayDialog("Unificar Lab Psicología",
                    $"Todas las instancias del laboratorio pasarán a usar:\n{CanonicalSource}\n\nEscenas:\n" + string.Join("\n", scenes) +
                    "\n\nSe hace backup de cada escena. ¿Continuar?", "Unificar", "Cancelar"))
                return;

            var versions = AllVersionPaths();
            var targetSubAssets = AssetDatabase.LoadAllAssetsAtPath(CanonicalSource);
            string backup = Path.Combine(ReportDir, $"backup_unificar_lab_{Stamp}");
            Directory.CreateDirectory(backup);
            var log = new StringBuilder("escena;accion;objeto;detalle\n");
            int migradas = 0, sinEquivalente = 0;

            foreach (var scenePath in scenes)
            {
                File.Copy(Path.Combine(ProjectRoot, scenePath), Path.Combine(backup, Path.GetFileName(scenePath)), true);
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                bool changed = false;

                foreach (var root in FindVersionInstances(scene, versions))
                {
                    string oldSrc = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
                    if (oldSrc == CanonicalSource)
                    {
                        if (RemoveUnused(root, scenePath, log)) changed = true;
                        else log.AppendLine($"{scenePath};sin-cambios;{root.name};ya usa el principal");
                        continue;
                    }

                    var oldRoot = AssetDatabase.LoadAssetAtPath<GameObject>(oldSrc);
                    var removedPaths = PrefabUtility.GetRemovedGameObjects(root)
                        .Where(r => r.assetGameObject != null && oldRoot != null)
                        .Select(r => RelPath(r.assetGameObject.transform, oldRoot.transform)).ToList();

                    // Conserva overrides emparejando por ruta de jerarquía (no destructivo).
                    var settings = new PrefabReplacingSettings
                    {
                        objectMatchMode = ObjectMatchMode.ByHierarchy,
                        changeRootNameToAssetName = false,
                        logInfo = true,
                    };
                    PrefabUtility.ReplacePrefabAssetOfPrefabInstance(root, target, settings, InteractionMode.AutomatedAction);
                    log.AppendLine($"{scenePath};migrada;{root.name};{oldSrc} -> {CanonicalSource}");
                    migradas++; changed = true;

                    // Piezas que la escena había quitado: se ocultan si existen en el principal.
                    var stillRemoved = new HashSet<string>(PrefabUtility.GetRemovedGameObjects(root)
                        .Where(r => r.assetGameObject != null)
                        .Select(r => RelPath(r.assetGameObject.transform, target.transform)));
                    foreach (var rel in removedPaths.Where(r => !stillRemoved.Contains(r)))
                    {
                        var t = FindByRelPath(root.transform, rel);
                        if (t == null) { log.AppendLine($"{scenePath};quitada-no-existe-en-principal;{rel};"); continue; }
                        if (!t.gameObject.activeSelf) continue;
                        Undo.RecordObject(t.gameObject, "Ocultar pieza");
                        t.gameObject.SetActive(false);
                        log.AppendLine($"{scenePath};pieza-ocultada;{rel};estaba quitada en la versión anterior");
                    }

                    // Overrides que apuntaban a objetos del modelo viejo sin equivalente: se eliminan
                    // (si no, la escena seguiría dependiendo del FBX descartado).
                    RemoveUnused(root, scenePath, log);
                }

                // Mallas / materiales sueltos que apuntaban a versiones descartadas.
                foreach (var d in FindDirectReferences(scene, versions, CanonicalSource))
                {
                    var match = targetSubAssets.FirstOrDefault(o => o != null && o.GetType() == d.value.GetType() && o.name == d.value.name);
                    if (match != null)
                    {
                        d.so.FindProperty(d.propertyPath).objectReferenceValue = match;
                        d.so.ApplyModifiedPropertiesWithoutUndo();
                        log.AppendLine($"{scenePath};revinculado;{d.ownerPath};{d.propertyPath}: {d.value.name}");
                        changed = true;
                    }
                    else
                    {
                        sinEquivalente++;
                        log.AppendLine($"{scenePath};SIN-EQUIVALENTE;{d.ownerPath};{d.propertyPath}: '{d.value.name}' ({d.value.GetType().Name}) de {AssetDatabase.GetAssetPath(d.value)}");
                    }
                }

                if (changed) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
            }

            string logPath = Path.Combine(backup, "unificacion_log.csv");
            File.WriteAllText(logPath, log.ToString(), new UTF8Encoding(false));
            Debug.Log($"[LabUnifier] Instancias migradas: {migradas}. Sin equivalente: {sinEquivalente}. Bitácora: {logPath}");
            EditorUtility.DisplayDialog("Lab Psicología",
                $"Instancias migradas a LabPsicologia1: {migradas}\n" +
                (sinEquivalente > 0 ? $"⚠️ Referencias sin equivalente: {sinEquivalente} (ver bitácora)\n" : "") +
                "\nRevisa visualmente 'Iniciar sesion' antes del paso 3.", "OK");
        }

        static bool RemoveUnused(GameObject root, string scenePath, StringBuilder log)
        {
            int before = PrefabUtility.GetPropertyModifications(root)?.Length ?? 0;
            PrefabUtility.RemoveUnusedOverrides(new[] { root }, InteractionMode.AutomatedAction);
            int after = PrefabUtility.GetPropertyModifications(root)?.Length ?? 0;
            if (after == before) return false;
            log.AppendLine($"{scenePath};overrides-huerfanos-eliminados;{root.name};{before - after}");
            return true;
        }

        // ---------------------------------------------------------------------
        // 3. DESCARTAR VERSIONES ANTIGUAS (Papelera)
        // ---------------------------------------------------------------------
        [MenuItem(MenuRoot + "3. Descartar versiones antiguas (a la Papelera)", priority = 3)]
        public static void MenuDiscard()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var candidates = new List<string>(DiscardedModelPaths());
            foreach (var e in DiscardedExtras)
            {
                if (Exists(e)) candidates.Add(e);
                if (Exists(Archived(e))) candidates.Add(Archived(e));
            }
            if (candidates.Count == 0) { EditorUtility.DisplayDialog("Lab Psicología", "No queda ninguna versión antigua.", "OK"); return; }

            // Antes de comprobar: el modelo principal NO debe depender de las versiones descartadas.
            var prep = new StringBuilder();
            Independizar(candidates, prep);

            // Seguridad: nada que el juego use puede ir a la Papelera.
            var closure = GameDependencyClosure();
            var blocked = new List<string>();
            foreach (var c in candidates)
            {
                bool inUse = AssetDatabase.IsValidFolder(c)
                    ? AssetDatabase.FindAssets("", new[] { c }).Select(AssetDatabase.GUIDToAssetPath).Any(closure.Contains)
                    : closure.Contains(c);
                if (inUse) blocked.Add(c);
            }
            if (blocked.Count > 0)
            {
                var who = new StringBuilder();
                foreach (var b in blocked)
                {
                    var targets = AssetDatabase.IsValidFolder(b)
                        ? new HashSet<string>(AssetDatabase.FindAssets("", new[] { b }).Select(AssetDatabase.GUIDToAssetPath))
                        : new HashSet<string> { b };
                    var referrers = closure.Where(c => !targets.Contains(c) && AssetDatabase.GetDependencies(c, false).Any(targets.Contains)).Take(5);
                    who.AppendLine($"{b}\n   ← {string.Join("\n   ← ", referrers)}");
                }
                File.WriteAllText(Path.Combine(ReportDir, $"descarte_bloqueado_{Stamp}.txt"), prep + "\n" + who, new UTF8Encoding(false));
                EditorUtility.DisplayDialog("Lab Psicología",
                    "Estas rutas todavía las usa el juego y NO se descartarán:\n\n" + who + "\n(Detalle en Reportes_Limpieza/descarte_bloqueado_*.txt)", "OK");
                return;
            }

            long size = SizeOf(candidates);
            if (!EditorUtility.DisplayDialog("Descartar versiones antiguas",
                    $"Se enviarán a la Papelera de Windows ({MB(size)}):\n\n" + string.Join("\n", candidates) + "\n\n¿Continuar?",
                    "Enviar a la Papelera", "Cancelar"))
                return;

            var failed = new List<string>();
            bool ok = AssetDatabase.MoveAssetsToTrash(candidates.ToArray(), failed);
            AssetDatabase.Refresh();

            var sb = new StringBuilder(prep.ToString() + "\nruta;resultado\n");
            foreach (var c in candidates) sb.AppendLine($"{c};{(failed.Contains(c) ? "FALLÓ" : "papelera")}");
            string logPath = Path.Combine(ReportDir, $"descarte_versiones_{Stamp}.csv");
            File.WriteAllText(logPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log($"[LabUnifier] Descartadas {candidates.Count - failed.Count}/{candidates.Count} rutas ({MB(size)}). Bitácora: {logPath}");
            if (!ok) foreach (var f in failed) Debug.LogError("[LabUnifier] No se pudo enviar a la Papelera: " + f);
        }

        const string SharedMaterials = "Assets/Models/Materials";
        const string SharedTextures = "Assets/Models/Textures";

        static bool IsUnder(string path, IEnumerable<string> candidates) =>
            candidates.Any(c => path == c || path.StartsWith(c + "/", StringComparison.Ordinal));

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// Quita al modelo principal toda dependencia de las versiones que se van a descartar:
        ///  a) remapeos de material que apuntan a materiales EMBEBIDOS en otro FBX → copia .mat propia;
        ///  b) texturas que el juego usa y viven en carpetas descartadas (.fbm) → se mueven (mismo GUID).
        static void Independizar(List<string> candidates, StringBuilder log)
        {
            var importer = AssetImporter.GetAtPath(CanonicalSource) as ModelImporter;
            if (importer != null)
            {
                bool reimport = false;
                foreach (var kv in importer.GetExternalObjectMap())
                {
                    if (kv.Value == null || !IsUnder(AssetDatabase.GetAssetPath(kv.Value), candidates)) continue;
                    if (kv.Value is Material mat)
                    {
                        EnsureFolder(SharedMaterials);
                        string safe = string.Join("_", kv.Key.name.Split(Path.GetInvalidFileNameChars()));
                        string matPath = AssetDatabase.GenerateUniqueAssetPath($"{SharedMaterials}/{safe}.mat");
                        var copy = new Material(mat);
                        AssetDatabase.CreateAsset(copy, matPath);
                        importer.AddRemap(kv.Key, copy);
                        log.AppendLine($"material-independizado;{kv.Key.name};{AssetDatabase.GetAssetPath(kv.Value)} -> {matPath}");
                    }
                    else
                    {
                        importer.RemoveRemap(kv.Key);
                        log.AppendLine($"remapeo-quitado;{kv.Key.name};{AssetDatabase.GetAssetPath(kv.Value)}");
                    }
                    reimport = true;
                }
                if (reimport) importer.SaveAndReimport();
            }

            var closure = GameDependencyClosure();
            var toMove = closure.Where(p => IsUnder(p, candidates) && !p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)
                                            && !p.EndsWith(".unity") && !p.EndsWith(".prefab") && !AssetDatabase.IsValidFolder(p)).ToList();
            if (toMove.Count > 0) EnsureFolder(SharedTextures);
            foreach (var src in toMove)
            {
                string dst = AssetDatabase.GenerateUniqueAssetPath($"{SharedTextures}/{Path.GetFileName(src)}");
                string err = AssetDatabase.MoveAsset(src, dst);
                log.AppendLine(string.IsNullOrEmpty(err) ? $"textura-conservada;{src} -> {dst}" : $"ERROR-mover;{src};{err}");
            }
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------------
        // 4. ELIMINAR DUPLICADOS RESTAURADOS (Papelera)
        //    Assets que la limpieza movió a _Archivo_Obsoleto y luego reaparecieron en su
        //    ruta original con un GUID NUEVO: nada los referencia, solo ocupan espacio.
        // ---------------------------------------------------------------------
        [MenuItem(MenuRoot + "4. Eliminar duplicados restaurados (a la Papelera)", priority = 4)]
        public static void MenuRemoveRestoredDuplicates()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var logs = Directory.GetFiles(ReportDir, "movimientos_*.csv");
            if (logs.Length == 0) { EditorUtility.DisplayDialog("Lab Psicología", "No hay bitácoras de movimientos.", "OK"); return; }

            var closure = GameDependencyClosure();
            var duplicates = new List<string>();
            var keptInUse = new List<string>();
            foreach (var log in logs)
            foreach (var line in File.ReadAllLines(log).Skip(1))
            {
                var parts = line.Split(';');
                if (parts.Length != 2) continue;
                string original = parts[0], archived = parts[1];
                if (!File.Exists(Path.Combine(ProjectRoot, original)) || !File.Exists(Path.Combine(ProjectRoot, archived))) continue;
                if (AssetDatabase.AssetPathToGUID(original) == AssetDatabase.AssetPathToGUID(archived)) continue; // no es un duplicado
                if (closure.Contains(original)) { keptInUse.Add(original); continue; } // el juego lo usa: no tocar
                duplicates.Add(original);
            }
            duplicates = duplicates.Distinct().ToList();
            if (duplicates.Count == 0) { EditorUtility.DisplayDialog("Lab Psicología", "No se encontraron duplicados restaurados.", "OK"); return; }

            long size = SizeOf(duplicates);
            string report = Path.Combine(ReportDir, $"duplicados_restaurados_{Stamp}.csv");
            File.WriteAllText(report, "ruta;guid\n" + string.Join("\n", duplicates.Select(d => $"{d};{AssetDatabase.AssetPathToGUID(d)}")), new UTF8Encoding(false));

            if (!EditorUtility.DisplayDialog("Eliminar duplicados restaurados",
                    $"{duplicates.Count} archivos ({MB(size)}) son copias restauradas con GUID nuevo que nada usa.\n" +
                    (keptInUse.Count > 0 ? $"{keptInUse.Count} se conservan porque el juego sí los usa.\n" : "") +
                    $"\nLista completa: {report}\n\n¿Enviarlos a la Papelera?", "Enviar a la Papelera", "Cancelar"))
                return;

            var failed = new List<string>();
            AssetDatabase.MoveAssetsToTrash(duplicates.ToArray(), failed);
            DeleteEmptyFolders(duplicates.Select(d => Path.GetDirectoryName(d).Replace('\\', '/')).Distinct());
            AssetDatabase.Refresh();
            Debug.Log($"[LabUnifier] Duplicados enviados a la Papelera: {duplicates.Count - failed.Count}/{duplicates.Count} ({MB(size)}).");
            foreach (var f in failed) Debug.LogError("[LabUnifier] No se pudo enviar a la Papelera: " + f);
        }

        static void DeleteEmptyFolders(IEnumerable<string> folders)
        {
            foreach (var start in folders.OrderByDescending(f => f.Length))
            {
                string f = start;
                while (!string.IsNullOrEmpty(f) && f != "Assets" && AssetDatabase.IsValidFolder(f))
                {
                    if (AssetDatabase.GetSubFolders(f).Length > 0 || AssetDatabase.FindAssets("", new[] { f }).Length > 0) break;
                    AssetDatabase.DeleteAsset(f);
                    f = Path.GetDirectoryName(f)?.Replace('\\', '/');
                }
            }
        }

        // ---------------------------------------------------------------------
        // Búsquedas en escena
        // ---------------------------------------------------------------------
        static List<GameObject> FindVersionInstances(Scene scene, HashSet<string> versions)
        {
            var result = new List<GameObject>();
            foreach (var go in scene.GetRootGameObjects())
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                if (!PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)) continue;
                string src = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
                if (src != null && versions.Contains(src)) result.Add(t.gameObject);
            }
            return result;
        }

        class DirectRef
        {
            public SerializedObject so;
            public string propertyPath, ownerPath;
            public Object value;
        }

        static List<DirectRef> FindDirectReferences(Scene scene, HashSet<string> versions, string exceptPath)
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
                    if (v == null || v is GameObject || v is Component) continue;
                    string p = AssetDatabase.GetAssetPath(v);
                    if (string.IsNullOrEmpty(p) || !versions.Contains(p) || p == exceptPath) continue;
                    if (PrefabUtility.IsPartOfPrefabInstance(comp) && !it.prefabOverride) continue;
                    refs.Add(new DirectRef { so = so, propertyPath = it.propertyPath, value = v, ownerPath = RelPath(comp.transform, null) + " (" + comp.GetType().Name + ")" });
                }
            }
            return refs;
        }
    }
}
#endif
