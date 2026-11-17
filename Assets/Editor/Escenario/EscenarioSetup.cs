using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Herramientas de la rama feature/mejoras-escenario. Cada opción es idempotente:
/// se puede ejecutar varias veces y reemplaza lo creado antes (sin duplicar).
/// Menú: Tools ▸ Escenario.
/// </summary>
public static class EscenarioSetup
{
    private const string Carpeta = "Assets/Materials/Escenario";
    private const string RutaRT = Carpeta + "/RT_CamaraGesell.renderTexture";
    private const string RutaMatPantalla = Carpeta + "/MAT_PantallaEnVivo.mat";
    private const string RutaMatRotulo = Carpeta + "/MAT_RotuloRestringido.mat";

    private const string NombreRaiz = "[MejorasEscenario]";
    private const string NombreMonitor = "E7_PB.003";            // monitor del escritorio de la sala de control
    private const string NombrePantalla = "PantallaEnVivo_SalaControl";
    private const string NombreCamara = "CamaraVigilancia_Gesell";

    // Área → nombre del rótulo existente (hijo de "etiquetas_Letreros") que marca su puerta
    private static readonly (string area, string rotulo)[] AreasRestringidas =
    {
        ("Archivo", "Cube.009"),
        ("Consultorio 1", "Cube.011"),
        ("Consultorio 2", "Cube.012"),
    };

    // ---------------------------------------------------------------- Menú

    [MenuItem("Tools/Escenario/1. Cámara en vivo en la sala de control")]
    public static void MenuCamaraEnVivo()
    {
        if (CrearCamaraEnVivo(out string msg)) Guardar();
        Debug.Log("[Escenario] " + msg);
    }

    [MenuItem("Tools/Escenario/2. Avisos de área restringida")]
    public static void MenuAvisos()
    {
        if (CrearAvisosRestringidos(out string msg)) Guardar();
        Debug.Log("[Escenario] " + msg);
    }

    [MenuItem("Tools/Escenario/Aplicar todo")]
    public static void MenuTodo()
    {
        bool a = CrearCamaraEnVivo(out string m1);
        bool b = CrearAvisosRestringidos(out string m2);
        if (a || b) Guardar();
        Debug.Log("[Escenario] " + m1 + "\n" + m2);
    }

    // ---------------------------------------------------------------- 1. Cámara en vivo

    public static bool CrearCamaraEnVivo(out string mensaje)
    {
        var monitor = BuscarEnEscena(NombreMonitor);
        if (monitor == null || monitor.GetComponent<MeshFilter>() == null)
        {
            mensaje = $"No se encontró el monitor '{NombreMonitor}' en la escena activa (¿está abierta SampleScene?).";
            return false;
        }

        AsegurarCarpeta();
        var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RutaRT);
        if (rt == null)
        {
            rt = new RenderTexture(640, 360, 16) { name = "RT_CamaraGesell", antiAliasing = 2 };
            AssetDatabase.CreateAsset(rt, RutaRT);
        }
        var mat = AssetDatabase.LoadAssetAtPath<Material>(RutaMatPantalla);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Unlit/Texture"));
            AssetDatabase.CreateAsset(mat, RutaMatPantalla);
        }
        mat.mainTexture = rt;
        EditorUtility.SetDirty(mat);

        var raiz = ObtenerRaiz();

        // Pantalla: un quad pegado a la cara frontal del monitor
        var anterior = raiz.transform.Find(NombrePantalla);
        if (anterior != null) Undo.DestroyObjectImmediate(anterior.gameObject);

        var t = monitor.transform;
        var bounds = monitor.GetComponent<MeshFilter>().sharedMesh.bounds;
        Vector3 centro = t.TransformPoint(bounds.center);
        // El modelo viene de Blender: el eje "up" local es la profundidad del monitor.
        Vector3 frente = -t.up;
        frente.y = 0f;
        frente.Normalize();
        float grosor = bounds.size.y * t.lossyScale.y;
        float ancho = bounds.size.x * t.lossyScale.x * 0.92f;
        float alto = ancho * 9f / 16f;

        var pantalla = new GameObject(NombrePantalla);
        Undo.RegisterCreatedObjectUndo(pantalla, "Pantalla en vivo");
        pantalla.transform.SetParent(raiz.transform, false);
        pantalla.transform.SetPositionAndRotation(centro + frente * (grosor * 0.5f + 0.004f), Quaternion.LookRotation(-frente, Vector3.up));

        var imagen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        imagen.name = "Imagen";
        Object.DestroyImmediate(imagen.GetComponent<Collider>());
        imagen.transform.SetParent(pantalla.transform, false);
        imagen.transform.localScale = new Vector3(ancho, alto, 1f);
        var rend = imagen.GetComponent<MeshRenderer>();
        rend.sharedMaterial = mat;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        rend.receiveShadows = false;

        var indicador = CrearTexto3D(pantalla.transform, "IndicadorEnVivo",
            "<color=#FF3B30><b>REC</b></color>  EN VIVO · Sala de entrevista",
            new Vector2(ancho * 0.62f, alto * 0.09f), TextAlignmentOptions.Left, Color.white);
        indicador.transform.localPosition = new Vector3(-ancho * 0.5f + ancho * 0.33f, alto * 0.5f - alto * 0.08f, -0.002f);

        // Cámara virtual: si ya existe se respeta su posición (pudo ajustarse a mano)
        var camGo = BuscarEnEscena(NombreCamara);
        if (camGo == null)
        {
            camGo = new GameObject(NombreCamara);
            Undo.RegisterCreatedObjectUndo(camGo, "Cámara vigilancia");
            camGo.transform.SetParent(raiz.transform, false);
            UbicarCamaraPorDefecto(camGo.transform);
        }
        var cam = camGo.GetComponent<Camera>();
        if (cam == null) cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 65f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 30f;
        cam.targetTexture = rt;
        cam.enabled = false;
        var vivo = camGo.GetComponent<CamaraEnVivo>();
        if (vivo == null) vivo = camGo.AddComponent<CamaraEnVivo>();
        var so = new SerializedObject(vivo);
        so.FindProperty("renderTexture").objectReferenceValue = rt;
        so.FindProperty("pantalla").objectReferenceValue = rend;
        so.FindProperty("indicador").objectReferenceValue = indicador;
        so.ApplyModifiedPropertiesWithoutUndo();

        mensaje = $"Cámara en vivo lista: '{NombreCamara}' → '{NombrePantalla}' ({ancho:F2} x {alto:F2} m).";
        return true;
    }

    private static void UbicarCamaraPorDefecto(Transform cam)
    {
        // Entre el terapeuta y el paciente, vista lateral desde la pared del espejo y a la altura del techo
        var terapeuta = BuscarEnEscena("TerapeutaC1");
        var paciente = BuscarEnEscena("PacienteC1");
        Vector3 medio = terapeuta != null && paciente != null
            ? (terapeuta.transform.position + paciente.transform.position) * 0.5f
            : new Vector3(-16.97f, 0f, -3.56f);
        cam.position = new Vector3(medio.x + 1.12f, 1.95f, medio.z);
        cam.LookAt(new Vector3(medio.x, 0.75f, medio.z));
    }

    // ---------------------------------------------------------------- 2. Avisos de área restringida

    public static bool CrearAvisosRestringidos(out string mensaje)
    {
        var etiquetas = BuscarEnEscena("etiquetas_Letreros");
        if (etiquetas == null)
        {
            mensaje = "No se encontró 'etiquetas_Letreros' en la escena activa (¿está abierta SampleScene?).";
            return false;
        }

        AsegurarCarpeta();
        var matRotulo = AssetDatabase.LoadAssetAtPath<Material>(RutaMatRotulo);
        if (matRotulo == null)
        {
            matRotulo = new Material(Shader.Find("Unlit/Color"));
            AssetDatabase.CreateAsset(matRotulo, RutaMatRotulo);
        }
        matRotulo.color = new Color32(0xC6, 0x28, 0x28, 0xFF);
        EditorUtility.SetDirty(matRotulo);

        var raiz = ObtenerRaiz();
        var log = new List<string>();
        foreach (var (area, nombreRotulo) in AreasRestringidas)
        {
            var rotulo = etiquetas.transform.Find(nombreRotulo);
            if (rotulo == null) { log.Add($"✗ {area}: no existe el rótulo '{nombreRotulo}'."); continue; }

            string nombreZona = "AvisoRestringido_" + area.Replace(" ", "");
            var previo = raiz.transform.Find(nombreZona);
            if (previo != null) Undo.DestroyObjectImmediate(previo.gameObject);

            // La puerta está bajo el rótulo; el pasillo queda hacia -Z. Se busca la superficie real con un raycast.
            Vector3 xz = rotulo.GetComponent<Renderer>().bounds.center;
            Vector3 origen = new Vector3(xz.x, 1.45f, xz.z - 1.5f);
            Vector3 superficie;
            if (Physics.Raycast(origen, Vector3.forward, out var hit, 2.5f, ~0, QueryTriggerInteraction.Ignore))
                superficie = hit.point;
            else
                superficie = new Vector3(xz.x, 1.45f, xz.z - 0.02f);

            var zona = new GameObject(nombreZona);
            Undo.RegisterCreatedObjectUndo(zona, "Aviso restringido");
            zona.transform.SetParent(raiz.transform, false);
            zona.transform.position = new Vector3(superficie.x, 0f, superficie.z);
            var caja = zona.AddComponent<BoxCollider>();
            caja.isTrigger = true;
            caja.center = new Vector3(0f, 1f, -0.5f);
            caja.size = new Vector3(0.9f, 2f, 0.8f);
            var comp = zona.AddComponent<AreaRestringida>();
            var so = new SerializedObject(comp);
            so.FindProperty("nombreArea").stringValue = area;
            so.FindProperty("mensaje").stringValue = area == "Archivo"
                ? "Acceso solo para personal autorizado. Aquí se guardan expedientes confidenciales."
                : "Acceso solo para personal autorizado. Este consultorio no forma parte de la práctica.";
            so.ApplyModifiedPropertiesWithoutUndo();

            // Rótulo físico en la puerta, mirando al pasillo
            var placa = new GameObject("Rotulo");
            placa.transform.SetParent(zona.transform, false);
            placa.transform.SetPositionAndRotation(superficie - Vector3.forward * 0.006f, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            var fondo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fondo.name = "Fondo";
            Object.DestroyImmediate(fondo.GetComponent<Collider>());
            fondo.transform.SetParent(placa.transform, false);
            fondo.transform.localScale = new Vector3(0.40f, 0.20f, 1f);
            var r = fondo.GetComponent<MeshRenderer>();
            r.sharedMaterial = matRotulo;
            r.shadowCastingMode = ShadowCastingMode.Off;
            var texto = CrearTexto3D(placa.transform, "Texto",
                "<b>ÁREA RESTRINGIDA</b>\n<size=78%>Solo personal autorizado</size>",
                new Vector2(0.36f, 0.16f), TextAlignmentOptions.Center, Color.white);
            texto.transform.localPosition = new Vector3(0f, 0f, -0.002f);

            log.Add($"✓ {area}: rótulo en {superficie:F2}{(hit.collider == null ? " (sin raycast, posición estimada)" : "")}");
        }

        mensaje = "Avisos de área restringida:\n" + string.Join("\n", log);
        return true;
    }

    // ---------------------------------------------------------------- Utilidades

    private static TextMeshPro CrearTexto3D(Transform padre, string nombre, string texto, Vector2 tam, TextAlignmentOptions alineacion, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(padre, false);
        var tmp = go.GetComponent<TextMeshPro>();
        tmp.text = texto;
        tmp.rectTransform.sizeDelta = tam;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 0.05f;
        tmp.fontSizeMax = 12f;
        tmp.alignment = alineacion;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.richText = true;
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) { mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false; }
        return tmp;
    }

    private static GameObject ObtenerRaiz()
    {
        var raiz = BuscarEnEscena(NombreRaiz);
        if (raiz == null)
        {
            raiz = new GameObject(NombreRaiz);
            Undo.RegisterCreatedObjectUndo(raiz, "Raíz mejoras escenario");
        }
        return raiz;
    }

    /// <summary>Busca por nombre en la escena activa, incluyendo objetos inactivos.</summary>
    private static GameObject BuscarEnEscena(string nombre)
    {
        var escena = EditorSceneManager.GetActiveScene();
        foreach (var root in escena.GetRootGameObjects())
        {
            if (root.name == nombre) return root;
            var t = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == nombre);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    private static void AsegurarCarpeta()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(Carpeta)) AssetDatabase.CreateFolder("Assets/Materials", "Escenario");
    }

    private static void Guardar()
    {
        AssetDatabase.SaveAssets();
        var escena = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
    }
}
