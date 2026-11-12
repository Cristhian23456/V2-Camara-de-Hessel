// =============================================================================
//  ProjectCleanupTool.cs  —  Cámara de Gesell (UTPL XRlab) · Unity 6
//  Herramienta de Editor: rastreo de GUIDs, consolidación de duplicados y
//  archivado seguro de assets huérfanos mediante la API de AssetDatabase.
//
//  Menú:  Tools ▸ Limpieza Proyecto ▸ ...
//    1. Analizar dependencias (solo lectura)
//    2. Re-vincular duplicados exactos
//    3. Mover huérfanos a _Archivo_Obsoleto
//    4. Revertir último movimiento
//    Rastrear referencias del asset seleccionado
//
//  Todo lo que escribe (reportes, backups, bitácoras de movimiento) va a
//  <raíz del proyecto>/Reportes_Limpieza/ (fuera de Assets: Unity no lo importa).
// =============================================================================
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace CamaraGesell.EditorTools
{
    public static class ProjectCleanupTool
    {
        // ---------------------------------------------------------------------
        // CONFIGURACIÓN — ajustar aquí si cambia el flujo del simulador
        // ---------------------------------------------------------------------

        /// Escenas del flujo principal: login → simulador (casos clínicos 1 y 4).
        static readonly string[] MainScenes =
        {
            "Assets/Scenes/Iniciar sesion.unity",
            "Assets/Scenes/SampleScene.unity",
        };

        /// Escenas que el rastreo confirmó fuera del flujo (no las carga ningún script
        /// ni ningún evento de UI). Se archivan y se quitan de Build Settings.
        static readonly string[] ObsoleteScenes =
        {
            "Assets/Scenes/Pruebas.unity",
            "Assets/_Recovery/0.unity",
            "Assets/_Recovery/0 (1).unity",
            "Assets/_Recovery/0 (2).unity",
        };

        /// Carpetas que NUNCA se archivan (código, login protegido, carga dinámica,
        /// paquetes de terceros). Su contenido además se trata como raíz de dependencias.
        static readonly string[] ProtectedFolders =
        {
            "Assets/Scripts",
            "Assets/LoginLab",
            "Assets/Editor",
            "Assets/TextMesh Pro",
            "Assets/Standard Assets",
            "Assets/Plugins",
            "Assets/StreamingAssets",
            "Assets/Settings",
        };

        /// Cualquier carpeta llamada "Resources" se protege: Resources.Load<> usa
        /// rutas en texto que el grafo de dependencias NO puede ver.
        const string ResourcesSegment = "/Resources/";

        /// Extensiones que nunca se archivan (código y shaders: Shader.Find es dinámico).
        static readonly HashSet<string> ProtectedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".asmdef", ".asmref", ".dll", ".so", ".aar", ".jar", ".rsp",
            ".shader", ".shadergraph", ".shadersubgraph", ".compute", ".cginc", ".hlsl",
        };

        /// Archivos serializados como texto YAML donde pueden vivir referencias por GUID.
        static readonly HashSet<string> YamlExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".unity", ".prefab", ".mat", ".asset", ".controller", ".overrideController",
            ".anim", ".mask", ".playable", ".physicMaterial", ".physicsMaterial2D",
            ".lighting", ".guiskin", ".fontsettings", ".flare", ".renderTexture",
            ".cubemap", ".spriteatlas", ".spriteatlasv2", ".signal", ".terrainlayer",
            ".brush", ".mixer", ".meta",
        };

        const string ArchiveRoot = "Assets/_Archivo_Obsoleto";
        const string MenuRoot = "Tools/Limpieza Proyecto/";

        static readonly Regex GuidRegex = new Regex(@"guid:\s*([0-9a-f]{32})", RegexOptions.Compiled);
        static readonly Regex TrailingVariant = new Regex(@"(\s*\(\d+\)|\d+|[_\- ]copy|[_\- ]old)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        static string ReportDir
        {
            get
            {
                string dir = Path.Combine(ProjectRoot, "Reportes_Limpieza");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }
        static string Stamp => DateTime.Now.ToString("yyyyMMdd_HHmmss");

        // ---------------------------------------------------------------------
        // MODELO DE ANÁLISIS
        // ---------------------------------------------------------------------
        class DuplicateGroup
        {
            public string Hash;
            public List<string> Members = new List<string>();
            public string Canonical;              // el que se conserva
            public List<string> Relinkable = new List<string>(); // se pueden fusionar con seguridad
            public List<string> ManualReview = new List<string>(); // idénticos pero importación distinta
        }

        class Analysis
        {
            public HashSet<string> Keep = new HashSet<string>();
            public List<string> Orphans = new List<string>();
            public Dictionary<string, List<string>> ReferrersByGuid = new Dictionary<string, List<string>>();
            public List<DuplicateGroup> ExactDuplicates = new List<DuplicateGroup>();
            public Dictionary<string, List<string>> NameVariants = new Dictionary<string, List<string>>();
            public List<string> MissingMainScenes = new List<string>();
        }

        // ---------------------------------------------------------------------
        // 1. ANALIZAR (solo lectura)
        // ---------------------------------------------------------------------
        [MenuItem(MenuRoot + "1. Analizar dependencias (solo lectura)", priority = 1)]
        public static void MenuAnalyze()
        {
            var a = RunAnalysis(showProgress: true);
            string path = WriteReport(a);
            Debug.Log($"[Limpieza] Análisis completo. Huérfanos: {a.Orphans.Count}, grupos duplicados exactos: {a.ExactDuplicates.Count}. Reporte: {path}");
            EditorUtility.RevealInFinder(path);
        }

        static Analysis RunAnalysis(bool showProgress)
        {
            var a = new Analysis();
            try
            {
                string[] allAssets = AssetDatabase.GetAllAssetPaths()
                    .Where(p => p.StartsWith("Assets/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(p))
                    .ToArray();

                // --- Raíces del grafo ------------------------------------------------
                var roots = new HashSet<string>();
                foreach (var s in MainScenes)
                {
                    if (File.Exists(Path.Combine(ProjectRoot, s))) roots.Add(s);
                    else a.MissingMainScenes.Add(s);
                }
                foreach (var p in allAssets)
                    if (IsProtected(p)) roots.Add(p);

                foreach (var o in PlayerSettings.GetPreloadedAssets())
                    AddObjectRoot(roots, o);
                AddObjectRoot(roots, GraphicsSettings.defaultRenderPipeline);
                for (int i = 0; i < QualitySettings.count; i++)
                    AddObjectRoot(roots, QualitySettings.GetRenderPipelineAssetAt(i));

                // --- Cierre transitivo de dependencias -------------------------------
                if (showProgress) EditorUtility.DisplayProgressBar("Limpieza", "Calculando dependencias…", 0.1f);
                foreach (var d in AssetDatabase.GetDependencies(roots.ToArray(), true))
                    a.Keep.Add(d);
                foreach (var r in roots) a.Keep.Add(r);

                // --- Huérfanos -------------------------------------------------------
                var obsoleteSet = new HashSet<string>(ObsoleteScenes);
                foreach (var p in allAssets)
                {
                    if (p.StartsWith(ArchiveRoot + "/", StringComparison.Ordinal)) continue;
                    if (IsProtected(p)) continue;
                    if (!a.Keep.Contains(p) || obsoleteSet.Contains(p))
                        a.Orphans.Add(p);
                }
                a.Orphans.Sort(StringComparer.Ordinal);

                // --- Índice inverso de GUIDs (rastreo) -------------------------------
                if (showProgress) EditorUtility.DisplayProgressBar("Limpieza", "Indexando referencias por GUID…", 0.4f);
                BuildReverseIndex(a);

                // --- Duplicados exactos (mismo contenido binario) ---------------------
                if (showProgress) EditorUtility.DisplayProgressBar("Limpieza", "Buscando duplicados exactos…", 0.7f);
                FindExactDuplicates(a, allAssets);

                // --- Variantes por nombre (LabPsicologia, LabPsicologia1, …) -----------
                FindNameVariants(a, allAssets);
            }
            finally
            {
                if (showProgress) EditorUtility.ClearProgressBar();
            }
            return a;
        }

        static void AddObjectRoot(HashSet<string> roots, UnityEngine.Object o)
        {
            if (o == null) return;
            string p = AssetDatabase.GetAssetPath(o);
            if (!string.IsNullOrEmpty(p) && p.StartsWith("Assets/", StringComparison.Ordinal)) roots.Add(p);
        }

        static bool IsProtected(string path)
        {
            if (ProtectedExtensions.Contains(Path.GetExtension(path))) return true;
            if (path.Contains(ResourcesSegment)) return true;
            foreach (var f in ProtectedFolders)
                if (path.StartsWith(f + "/", StringComparison.Ordinal)) return true;
            return false;
        }

        static void BuildReverseIndex(Analysis a)
        {
            string assetsDir = Application.dataPath;
            foreach (var file in Directory.EnumerateFiles(assetsDir, "*", SearchOption.AllDirectories))
            {
                if (!YamlExtensions.Contains(Path.GetExtension(file))) continue;
                if (!IsTextYaml(file)) continue;
                string rel = ToAssetPath(file);
                string self = file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)
                    ? AssetDatabase.AssetPathToGUID(rel.Substring(0, rel.Length - 5)) : AssetDatabase.AssetPathToGUID(rel);
                string text;
                try { text = File.ReadAllText(file); } catch { continue; }
                foreach (Match m in GuidRegex.Matches(text))
                {
                    string g = m.Groups[1].Value;
                    if (g == self) continue; // la línea "guid:" propia del .meta
                    if (!a.ReferrersByGuid.TryGetValue(g, out var list))
                        a.ReferrersByGuid[g] = list = new List<string>();
                    if (!list.Contains(rel)) list.Add(rel);
                }
            }
        }

        static void FindExactDuplicates(Analysis a, string[] allAssets)
        {
            // Pre-filtro barato por (extensión, tamaño) antes de calcular MD5.
            // Los assets protegidos (Resources, TMP, Standard Assets…) no participan:
            // no se re-vinculan ni se reescriben paquetes de terceros.
            var bySize = allAssets
                .Where(p => !IsProtected(p))
                .Where(p => !p.StartsWith(ArchiveRoot + "/", StringComparison.Ordinal))
                .GroupBy(p => (Path.GetExtension(p).ToLowerInvariant(), new FileInfo(Path.Combine(ProjectRoot, p)).Length))
                .Where(g => g.Count() > 1 && g.Key.Item2 > 0);

            using (var md5 = MD5.Create())
            {
                foreach (var sizeGroup in bySize)
                {
                    var byHash = sizeGroup.GroupBy(p => HashFile(md5, p)).Where(g => g.Count() > 1);
                    foreach (var hg in byHash)
                    {
                        var grp = new DuplicateGroup { Hash = hg.Key, Members = hg.ToList() };
                        // Canónico: el que está en el grafo del flujo principal y tiene más referencias.
                        grp.Canonical = grp.Members
                            .OrderByDescending(p => a.Keep.Contains(p) && !IsUnderArchiveOrObsolete(p))
                            .ThenByDescending(p => ReferrerCount(a, p))
                            .ThenBy(p => p.Length) // nombre más corto (sin " (1)")
                            .First();
                        foreach (var m in grp.Members.Where(m => m != grp.Canonical))
                        {
                            // Mismo contenido NO basta: si el .meta difiere (escala, sRGB, materiales
                            // remapeados…) el asset importado es distinto. Se exige .meta idéntico.
                            if (!SameImportSettings(grp.Canonical, m))
                                grp.ManualReview.Add(m);
                            else
                                grp.Relinkable.Add(m);
                        }
                        a.ExactDuplicates.Add(grp);
                    }
                }
            }
        }

        static void FindNameVariants(Analysis a, string[] allAssets)
        {
            foreach (var g in allAssets
                .Where(p => !IsProtected(p) && !p.StartsWith(ArchiveRoot + "/", StringComparison.Ordinal))
                .GroupBy(p => Path.GetExtension(p).ToLowerInvariant() + "|" +
                              TrailingVariant.Replace(Path.GetFileNameWithoutExtension(p), "").Trim().ToLowerInvariant())
                .Where(g => g.Count() > 1))
            {
                a.NameVariants[g.Key] = g.OrderBy(p => p).ToList();
            }
        }

        static bool IsUnderArchiveOrObsolete(string p) =>
            p.StartsWith(ArchiveRoot + "/", StringComparison.Ordinal) || ObsoleteScenes.Contains(p);

        static int ReferrerCount(Analysis a, string assetPath)
        {
            string g = AssetDatabase.AssetPathToGUID(assetPath);
            return a.ReferrersByGuid.TryGetValue(g, out var l) ? l.Count : 0;
        }

        static string HashFile(MD5 md5, string assetPath)
        {
            using (var fs = File.OpenRead(Path.Combine(ProjectRoot, assetPath)))
                return BitConverter.ToString(md5.ComputeHash(fs)).Replace("-", "");
        }

        /// Compara los .meta ignorando la línea guid y la sección externalObjects
        /// NO se ignora: si los remapeos de materiales difieren, no es seguro fusionar.
        static bool SameImportSettings(string a, string b)
        {
            try { return NormalizedMeta(a) == NormalizedMeta(b); } catch { return false; }
        }

        static string NormalizedMeta(string assetPath) => string.Join("\n",
            File.ReadAllLines(Path.Combine(ProjectRoot, assetPath + ".meta"))
                .Where(l => !l.StartsWith("guid:", StringComparison.Ordinal)));

        static bool IsTextYaml(string fullPath)
        {
            if (fullPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) return true;
            try
            {
                using (var fs = File.OpenRead(fullPath))
                {
                    var buf = new byte[5];
                    return fs.Read(buf, 0, 5) == 5 && Encoding.ASCII.GetString(buf) == "%YAML";
                }
            }
            catch { return false; }
        }

        static string ToAssetPath(string fullPath) =>
            "Assets" + fullPath.Substring(Application.dataPath.Length).Replace('\\', '/');

        // ---------------------------------------------------------------------
        // REPORTE
        // ---------------------------------------------------------------------
        static string WriteReport(Analysis a)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Reporte de dependencias — {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine();
            sb.AppendLine("Escenas raíz: " + string.Join(", ", MainScenes.Select(s => $"`{s}`")));
            if (a.MissingMainScenes.Count > 0)
                sb.AppendLine("\n> ⚠️ Escenas principales NO encontradas: " + string.Join(", ", a.MissingMainScenes));
            sb.AppendLine($"\n- Assets en el grafo del flujo principal: **{a.Keep.Count}**");
            sb.AppendLine($"- Huérfanos a archivar: **{a.Orphans.Count}** ({FormatSize(a.Orphans)})");
            sb.AppendLine($"- Grupos de duplicados exactos: **{a.ExactDuplicates.Count}**");

            sb.AppendLine("\n## Variantes por nombre (rastreo de GUID y referencias)\n");
            foreach (var kv in a.NameVariants.OrderBy(k => k.Key))
            {
                sb.AppendLine($"### `{kv.Key.Split('|')[1]}` ({kv.Key.Split('|')[0]})");
                sb.AppendLine("| Asset | GUID | ¿En flujo principal? | Referenciado por |");
                sb.AppendLine("|---|---|---|---|");
                foreach (var p in kv.Value)
                {
                    string g = AssetDatabase.AssetPathToGUID(p);
                    a.ReferrersByGuid.TryGetValue(g, out var refs);
                    string r = refs == null || refs.Count == 0 ? "—" : string.Join("<br>", refs.Select(x => $"`{x}`"));
                    sb.AppendLine($"| `{p}` | `{g}` | {(a.Keep.Contains(p) && !ObsoleteScenes.Contains(p) ? "Sí" : "No")} | {r} |");
                }
                sb.AppendLine();
            }

            sb.AppendLine("## Duplicados exactos (mismo MD5)\n");
            foreach (var d in a.ExactDuplicates)
            {
                sb.AppendLine($"- Canónico: `{d.Canonical}`");
                foreach (var m in d.Relinkable) sb.AppendLine($"  - fusionable → `{m}` ({ReferrerCount(a, m)} referencias)");
                foreach (var m in d.ManualReview) sb.AppendLine($"  - ⚠️ revisión manual (import settings distintos) → `{m}`");
            }

            sb.AppendLine("\n## Huérfanos (se moverán a `_Archivo_Obsoleto`)\n");
            foreach (var o in a.Orphans) sb.AppendLine($"- `{o}`");

            string path = Path.Combine(ReportDir, $"Reporte_Dependencias_{Stamp}.md");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            return path;
        }

        static string FormatSize(IEnumerable<string> paths)
        {
            long total = 0;
            foreach (var p in paths)
            {
                var fi = new FileInfo(Path.Combine(ProjectRoot, p));
                if (fi.Exists) total += fi.Length;
            }
            return $"{total / (1024f * 1024f):0.0} MB";
        }

        // ---------------------------------------------------------------------
        // 2. RE-VINCULAR DUPLICADOS EXACTOS
        // ---------------------------------------------------------------------
        [MenuItem(MenuRoot + "2. Re-vincular duplicados exactos", priority = 2)]
        public static void MenuRelink()
        {
            if (EditorSettings.serializationMode != SerializationMode.ForceText)
            {
                EditorUtility.DisplayDialog("Limpieza", "El proyecto debe usar Asset Serialization = Force Text.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var a = RunAnalysis(showProgress: true);
            var plan = new List<(string file, string from, string to)>();
            foreach (var d in a.ExactDuplicates)
            {
                string toGuid = AssetDatabase.AssetPathToGUID(d.Canonical);
                foreach (var dup in d.Relinkable)
                {
                    string fromGuid = AssetDatabase.AssetPathToGUID(dup);
                    if (!a.ReferrersByGuid.TryGetValue(fromGuid, out var refs)) continue;
                    foreach (var f in refs)
                    {
                        if (f == dup + ".meta") continue;
                        plan.Add((f, fromGuid, toGuid));
                    }
                }
            }

            if (plan.Count == 0)
            {
                EditorUtility.DisplayDialog("Limpieza", "No hay referencias a duplicados exactos fusionables. Nada que re-vincular.", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Re-vincular",
                    $"Se reescribirán {plan.Select(p => p.file).Distinct().Count()} archivos para apuntar al asset canónico.\n" +
                    "Se hará backup en Reportes_Limpieza/. ¿Continuar?", "Re-vincular", "Cancelar"))
                return;

            // Cerrar escenas para que Unity no sobrescriba desde memoria lo que editamos en disco.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            string backup = Path.Combine(ReportDir, $"backup_relink_{Stamp}");
            var log = new StringBuilder("archivo;guid_anterior;guid_nuevo\n");
            foreach (var fileGroup in plan.GroupBy(p => p.file))
            {
                string full = Path.Combine(ProjectRoot, fileGroup.Key);
                string bkp = Path.Combine(backup, fileGroup.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(bkp));
                File.Copy(full, bkp, true);

                string text = File.ReadAllText(full);
                foreach (var item in fileGroup)
                {
                    text = Regex.Replace(text, @"guid:\s*" + item.from, "guid: " + item.to);
                    log.AppendLine($"{fileGroup.Key};{item.from};{item.to}");
                }
                File.WriteAllText(full, text, new UTF8Encoding(false));
            }
            File.WriteAllText(Path.Combine(backup, "relink_log.csv"), log.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log($"[Limpieza] Re-vinculación completa. Backup y bitácora en: {backup}");
        }

        // ---------------------------------------------------------------------
        // 3. MOVER HUÉRFANOS
        // ---------------------------------------------------------------------
        [MenuItem(MenuRoot + "3. Mover huérfanos a _Archivo_Obsoleto", priority = 3)]
        public static void MenuArchive()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var a = RunAnalysis(showProgress: true); // siempre recalcular: nunca confiar en un análisis viejo
            if (a.MissingMainScenes.Count > 0)
            {
                EditorUtility.DisplayDialog("Limpieza", "Faltan escenas principales; se cancela para no archivar de más.", "OK");
                return;
            }
            if (a.Orphans.Count == 0)
            {
                EditorUtility.DisplayDialog("Limpieza", "No hay huérfanos que mover.", "OK");
                return;
            }
            string report = WriteReport(a);
            if (!EditorUtility.DisplayDialog("Mover a _Archivo_Obsoleto",
                    $"Se moverán {a.Orphans.Count} assets ({FormatSize(a.Orphans)}).\nRevisa el reporte:\n{report}\n\n¿Continuar?",
                    "Mover", "Cancelar"))
                return;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureFolder(ArchiveRoot);

            var moved = new StringBuilder("origen;destino\n");
            var errors = new List<string>();
            var touchedFolders = new HashSet<string>();

            // Carpetas destino y rutas únicas se resuelven ANTES de StartAssetEditing:
            // dentro de ese bloque AssetDatabase no registra carpetas nuevas hasta el final
            // y IsValidFolder devolvería false, creando "Models 1", "Models 2", …
            var moves = new List<(string src, string dst)>();
            foreach (var src in a.Orphans)
            {
                string dst = ArchiveRoot + "/" + src.Substring("Assets/".Length);
                EnsureFolder(Path.GetDirectoryName(dst).Replace('\\', '/'));
                if (File.Exists(Path.Combine(ProjectRoot, dst)))
                    dst = AssetDatabase.GenerateUniqueAssetPath(dst);
                moves.Add((src, dst));
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var mv in moves)
                {
                    string src = mv.src, dst = mv.dst;
                    string err = AssetDatabase.MoveAsset(src, dst); // conserva .meta → conserva GUID
                    if (string.IsNullOrEmpty(err))
                    {
                        moved.AppendLine($"{src};{dst}");
                        touchedFolders.Add(Path.GetDirectoryName(src).Replace('\\', '/'));
                    }
                    else errors.Add($"{src}: {err}");
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // Quitar escenas archivadas de Build Settings.
            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Where(s => !ObsoleteScenes.Contains(s.path) && !s.path.StartsWith(ArchiveRoot + "/", StringComparison.Ordinal))
                .ToArray();

            DeleteEmptyFolders(touchedFolders);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string logPath = Path.Combine(ReportDir, $"movimientos_{Stamp}.csv");
            File.WriteAllText(logPath, moved.ToString(), new UTF8Encoding(false));
            foreach (var e in errors) Debug.LogError("[Limpieza] " + e);
            Debug.Log($"[Limpieza] Movidos {a.Orphans.Count - errors.Count}/{a.Orphans.Count}. Bitácora: {logPath}");
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        static void DeleteEmptyFolders(IEnumerable<string> folders)
        {
            foreach (var start in folders.OrderByDescending(f => f.Length))
            {
                string f = start;
                while (!string.IsNullOrEmpty(f) && f != "Assets" && AssetDatabase.IsValidFolder(f) && !IsProtected(f + "/x"))
                {
                    bool empty = AssetDatabase.GetSubFolders(f).Length == 0 &&
                                 AssetDatabase.FindAssets("", new[] { f }).Length == 0;
                    if (!empty) break;
                    AssetDatabase.DeleteAsset(f);
                    f = Path.GetDirectoryName(f)?.Replace('\\', '/');
                }
            }
        }

        // ---------------------------------------------------------------------
        // 4. REVERTIR
        // ---------------------------------------------------------------------
        [MenuItem(MenuRoot + "4. Revertir último movimiento", priority = 4)]
        public static void MenuRevert()
        {
            var last = Directory.GetFiles(ReportDir, "movimientos_*.csv").OrderBy(f => f).LastOrDefault();
            if (last == null) { EditorUtility.DisplayDialog("Limpieza", "No hay bitácora de movimientos.", "OK"); return; }
            if (!EditorUtility.DisplayDialog("Revertir", $"Revertir los movimientos de:\n{Path.GetFileName(last)}?", "Revertir", "Cancelar")) return;

            var rows = File.ReadAllLines(last).Skip(1).Select(l => l.Split(';')).Where(x => x.Length == 2).ToList();
            foreach (var parts in rows) EnsureFolder(Path.GetDirectoryName(parts[0]).Replace('\\', '/'));

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var parts in rows)
                {
                    string err = AssetDatabase.MoveAsset(parts[1], parts[0]);
                    if (!string.IsNullOrEmpty(err)) Debug.LogError($"[Limpieza] {parts[1]}: {err}");
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            File.Move(last, last + ".revertido");
            AssetDatabase.Refresh();
            Debug.Log("[Limpieza] Reversión completa. Si quitaste escenas de Build Settings, vuelve a agregarlas a mano.");
        }

        // ---------------------------------------------------------------------
        // UTILIDAD: rastrear referencias del asset seleccionado
        // ---------------------------------------------------------------------
        [MenuItem(MenuRoot + "Rastrear referencias del asset seleccionado", priority = 20)]
        public static void MenuTraceSelected()
        {
            var a = new Analysis();
            BuildReverseIndex(a);
            foreach (var obj in Selection.objects)
            {
                string p = AssetDatabase.GetAssetPath(obj);
                string g = AssetDatabase.AssetPathToGUID(p);
                a.ReferrersByGuid.TryGetValue(g, out var refs);
                Debug.Log($"[Limpieza] {p}  (GUID {g})  → {(refs == null ? 0 : refs.Count)} referencias:\n" +
                          (refs == null ? "  (ninguna)" : string.Join("\n", refs.Select(r => "  " + r))));
            }
        }

        [MenuItem(MenuRoot + "Rastrear referencias del asset seleccionado", true)]
        static bool ValidateTraceSelected() => Selection.objects.Length > 0;
    }
}
#endif
