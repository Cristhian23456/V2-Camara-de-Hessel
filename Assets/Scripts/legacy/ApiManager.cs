using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System;

public class ApiManager : MonoBehaviour
{
    [SerializeField]
    private string apiUrl = "https://api-labpsicologia.onrender.com/api";
    [SerializeField]
    private TextMeshProUGUI txtDatos;
    public List<Dialogos> dialogosList = new List<Dialogos>();
    public List<Dialogos> dialogosListDes = new List<Dialogos>();
    public List<Dialogos> dialogosListFin = new List<Dialogos>();
    public int nroCaso;
    [SerializeField]
    private GameObject [] personajesCasos;

    [Header("Configuraciones de Casos Unificadas")]
    [SerializeField]
    private List<CaseSetupSO> casoSetups = new List<CaseSetupSO>();
    private Dictionary<int, CaseSetupSO> casoSetupsDict = new Dictionary<int, CaseSetupSO>();

    [Header("Red: tolerancia a fallos")]
    [Tooltip("Segundos máximos por petición. Render (plan gratuito) puede tardar ~50 s en despertar.")]
    [SerializeField] private int timeoutSegundos = 60;
    [Tooltip("Intentos totales por petición ante errores transitorios (5xx, 408, 429, sin conexión).")]
    [SerializeField, Range(1, 5)] private int maxIntentos = 3;
    [Tooltip("Espera inicial entre reintentos; se duplica en cada intento (backoff exponencial).")]
    [SerializeField] private float esperaInicialSegundos = 2f;
    [Tooltip("Caracteres máximos del cuerpo de respuesta que se imprimen en consola.")]
    [SerializeField] private int maxCaracteresCuerpoLog = 1500;

    private const string LogTag = "[ApiManager]";

    private void Awake()
    {
        InitializeCaseSetups();
    }

    private void InitializeCaseSetups()
    {
        casoSetupsDict.Clear();
        foreach (var setup in casoSetups)
        {
            if (setup != null && !casoSetupsDict.ContainsKey(setup.nroCaso))
            {
                casoSetupsDict.Add(setup.nroCaso, setup);
            }
        }
    }

    public CaseSetupSO GetCaseSetup(int caso)
    {
        if (casoSetupsDict.Count == 0) InitializeCaseSetups();
        if (casoSetupsDict.TryGetValue(caso, out CaseSetupSO setup))
        {
            return setup;
        }
        return null;
    }


    // Evento para indicar que los diálogos se han cargado fase inicial
    public event Action<List<Dialogos>> DialogosCargadosEvent;
    // Evento para indicar que los diálogos se han cargado fase desarrollo
    public event Action<List<Dialogos>> DialogosCargadosDesarrolladoEvent;
    // Evento para indicar que los diálogos se han cargado
    public event Action<List<Dialogos>> DialogosCargadosFinalEvent;

    /// <summary>
    /// NUEVO: se dispara cuando una fase NO se pudo cargar tras todos los reintentos.
    /// Parámetros: (fase, mensaje legible para mostrar al estudiante).
    /// Sin este evento, DialogosManager se quedaba esperando para siempre con "Cargando…".
    /// </summary>
    public event Action<string, string> DialogosErrorEvent;

    /// <summary>True si alguna fase falló en el último intento de carga.</summary>
    public bool HuboErrorDeCarga { get; private set; }

    private bool recursosCargados = false;

    void Start()
    {
        // Consumimos la api de manera diferida para acelerar la carga del juego
        nroCaso = new System.Random().Next(2) == 0 ? 1 : 4;
        Debug.Log("Número de caso generado (Diferido): " + nroCaso);

        if (personajesCasos == null || personajesCasos.Length < 4)
        {
            Debug.LogError($"{LogTag} 'personajesCasos' necesita 4 elementos asignados en el Inspector (C1 terapeuta/paciente, C4 terapeuta/paciente).");
            return;
        }

        if (nroCaso == 1)
        {
            personajesCasos[0].SetActive(true);
            personajesCasos[1].SetActive(true);
            personajesCasos[2].SetActive(false);
            personajesCasos[3].SetActive(false);
        }
        else if (nroCaso == 4)
        {
            personajesCasos[0].SetActive(false);
            personajesCasos[1].SetActive(false);
            personajesCasos[2].SetActive(true);
            personajesCasos[3].SetActive(true);
        }
    }

    public void CargarRecursosCaso()
    {
        if (recursosCargados) return;
        recursosCargados = true;
        HuboErrorDeCarga = false;

        Debug.Log("Cargando dinámicamente recursos de diálogos desde la API para el caso: " + nroCaso);

        StartCoroutine(GetDialogosFromApi(nroCaso, "Inicial", (dialogos) => {
            dialogosList = dialogos;
            Debug.Log("Diálogos Inicial cargados: " + dialogosList.Count);
            DialogosCargadosEvent?.Invoke(dialogosList);
        }));

        StartCoroutine(GetDialogosFromApi(nroCaso, "Desarrollo", (dialogos) => {
            dialogosListDes = dialogos;
            Debug.Log("Diálogos Desarrollo cargados: " + dialogosListDes.Count);
            DialogosCargadosDesarrolladoEvent?.Invoke(dialogosListDes);
        }));

        StartCoroutine(GetDialogosFromApi(nroCaso, "Final", (dialogos) => {
            dialogosListFin = dialogos;
            Debug.Log("Diálogos final cargados: " + dialogosListFin.Count);
            DialogosCargadosFinalEvent?.Invoke(dialogosListFin);
        }));
    }

    /// <summary>
    /// NUEVO: permite que la UI (botón "Reintentar") vuelva a pedir los diálogos
    /// después de un fallo, sin reiniciar la escena.
    /// </summary>
    public void ReintentarCarga()
    {
        StopAllCoroutines();
        recursosCargados = false;
        CargarRecursosCaso();
    }

    // =====================================================================
    //  DIÁLOGOS
    // =====================================================================
    IEnumerator GetDialogosFromApi(int caso, string fase, Action<List<Dialogos>> callback)
    {
        string url = apiUrl.TrimEnd('/') + "/get-dialogos?caso=" + caso + "&fase=" + Uri.EscapeDataString(fase);
        string contexto = $"get-dialogos (caso={caso}, fase={fase})";

        // 1) Petición HTTP con reintentos y diagnóstico
        var resultado = new ResultadoHttp();
        yield return EnviarConReintentos(url, contexto, resultado);

        if (!resultado.Exito)
        {
            NotificarFallo(fase, resultado.MensajeUsuario);
            yield break;
        }

        // 2) Validación y deserialización del JSON (fuera del iterador: try/catch no admite yield)
        if (!IntentarParsearDialogos(resultado.Cuerpo, contexto, url, out List<Dialogos> listDialogos, out string errorJson))
        {
            NotificarFallo(fase, errorJson);
            yield break;
        }

        // 3) Preguntas asociadas: si alguna falla, la fase NO se da por cargada
        //    (antes quedaba dialogo.pregunta = null → NullReferenceException más tarde en la sesión).
        foreach (Dialogos dialogo in listDialogos)
        {
            if (!dialogo.tienePregunta) continue;

            var preguntaOk = new ResultadoPregunta();
            yield return GetPreguntaById(dialogo.preguntaId, dialogo, preguntaOk);
            if (!preguntaOk.Exito)
            {
                NotificarFallo(fase, $"No se pudo cargar la pregunta '{dialogo.preguntaId}' (diálogo orden {dialogo.orden}).");
                yield break;
            }
        }

        callback?.Invoke(listDialogos);
    }

    private bool IntentarParsearDialogos(string json, string contexto, string url, out List<Dialogos> lista, out string error)
    {
        lista = null;
        error = null;
        string recortado = json?.Trim() ?? string.Empty;

        if (recortado.Length == 0)
        {
            error = "El servidor respondió 200 OK pero sin contenido.";
            Debug.LogError(FormatearDiagnosticoJson(contexto, url, "RESPUESTA VACÍA", error, recortado));
            return false;
        }

        // El endpoint devuelve un ARREGLO: [ {...}, {...} ]. JsonUtility no parsea arreglos raíz,
        // por eso se envuelve en {"dialogos": [...]}. Si llega un objeto, se analiza qué es.
        string envuelto;
        if (recortado.StartsWith("["))
        {
            envuelto = "{\"dialogos\":" + recortado + "}";
        }
        else if (recortado.StartsWith("{") && recortado.Contains("\"dialogos\""))
        {
            envuelto = recortado; // el backend ya envía { "dialogos": [...] }
        }
        else if (recortado.StartsWith("{"))
        {
            string msg = ExtraerMensajeServidor(recortado);
            error = "El servidor devolvió un objeto JSON en lugar de la lista de diálogos" +
                    (msg != null ? $": \"{msg}\"" : ".");
            Debug.LogError(FormatearDiagnosticoJson(contexto, url, "ESTRUCTURA JSON INESPERADA (se esperaba un arreglo [])", error, recortado));
            return false;
        }
        else
        {
            error = "La respuesta no es JSON (¿página HTML de error del hosting?).";
            Debug.LogError(FormatearDiagnosticoJson(contexto, url, "RESPUESTA NO-JSON", error, recortado));
            return false;
        }

        DialogosContainer contenedor;
        try
        {
            contenedor = JsonUtility.FromJson<DialogosContainer>(envuelto);
        }
        catch (ArgumentException ex)
        {
            error = "JSON mal formado: " + ex.Message;
            Debug.LogError(FormatearDiagnosticoJson(contexto, url, "JSON MAL FORMADO (falla de sintaxis)", error, recortado));
            return false;
        }

        if (contenedor?.dialogos == null)
        {
            error = "No se pudo mapear la lista 'dialogos' del JSON.";
            Debug.LogError(FormatearDiagnosticoJson(contexto, url, "MAPEO JSON → C# FALLIDO", error, recortado));
            return false;
        }

        if (contenedor.dialogos.Count == 0)
        {
            error = "El servidor no tiene diálogos para este caso y fase.";
            Debug.LogError(FormatearDiagnosticoJson(contexto, url, "LISTA VACÍA (revisar datos en la base de datos)", error, recortado));
            return false;
        }

        // JsonUtility ignora en silencio los campos que no coinciden: se validan los esenciales.
        var avisos = new StringBuilder();
        for (int i = 0; i < contenedor.dialogos.Count; i++)
        {
            Dialogos d = contenedor.dialogos[i];
            if (d == null) { avisos.AppendLine($"  • [{i}] elemento null"); continue; }
            if (string.IsNullOrWhiteSpace(d.contenido))
                avisos.AppendLine($"  • [{i}] orden={d.orden}: 'contenido' vacío (¿el campo cambió de nombre en el backend?)");
            if (string.IsNullOrWhiteSpace(d.personaje))
                avisos.AppendLine($"  • [{i}] orden={d.orden}: 'personaje' vacío");
            if (d.tienePregunta && string.IsNullOrWhiteSpace(d.preguntaId))
                avisos.AppendLine($"  • [{i}] orden={d.orden}: tienePregunta=true pero 'preguntaId' vacío");
        }
        if (avisos.Length > 0)
            Debug.LogWarning($"{LogTag} {contexto}: el JSON se leyó, pero hay campos sospechosos:\n{avisos}");

        // Si un dialogo tiene pregunta sin id, la petición de pregunta fallaría: se corta aquí con mensaje claro.
        foreach (var d in contenedor.dialogos)
        {
            if (d != null && d.tienePregunta && string.IsNullOrWhiteSpace(d.preguntaId))
            {
                error = $"El diálogo orden {d.orden} indica que tiene pregunta pero no trae 'preguntaId'.";
                return false;
            }
        }

        contenedor.dialogos.RemoveAll(d => d == null);
        lista = contenedor.dialogos;
        return true;
    }

    // =====================================================================
    //  PREGUNTAS
    // =====================================================================
    private class ResultadoPregunta { public bool Exito; }

    IEnumerator GetPreguntaById(string preguntaId, Dialogos dialogo, ResultadoPregunta salida)
    {
        string url = apiUrl.TrimEnd('/') + "/get-questionsId/" + Uri.EscapeDataString(preguntaId);
        string contexto = $"get-questionsId ({preguntaId})";

        var resultado = new ResultadoHttp();
        yield return EnviarConReintentos(url, contexto, resultado);
        if (!resultado.Exito) yield break;

        Preguntas pregunta = null;
        string cuerpo = resultado.Cuerpo?.Trim() ?? string.Empty;
        if (!cuerpo.StartsWith("{"))
        {
            Debug.LogError(FormatearDiagnosticoJson(contexto, url, "ESTRUCTURA JSON INESPERADA (se esperaba un objeto {})",
                "La pregunta no llegó como objeto JSON.", cuerpo));
            yield break;
        }
        try
        {
            pregunta = JsonUtility.FromJson<Preguntas>(cuerpo);
        }
        catch (ArgumentException ex)
        {
            Debug.LogError(FormatearDiagnosticoJson(contexto, url, "JSON MAL FORMADO (falla de sintaxis)", ex.Message, cuerpo));
            yield break;
        }

        if (pregunta == null || string.IsNullOrWhiteSpace(pregunta.pregunta) || pregunta.respuestas == null || pregunta.respuestas.Length == 0)
        {
            Debug.LogError(FormatearDiagnosticoJson(contexto, url, "MAPEO JSON → C# INCOMPLETO",
                "Faltan 'pregunta' o 'respuestas' en el objeto recibido.", cuerpo));
            yield break;
        }

        // Los audios ya NO se cargan aquí al inicio de golpe.
        // Esto optimiza la carga inicial cargando los recursos "por partes" (bajo demanda) en DialogosManager.cs.
        dialogo.pregunta = pregunta;
        salida.Exito = true;
    }

    public int getNroCaso()
    {
        return nroCaso;
    }

    // =====================================================================
    //  NÚCLEO HTTP: reintentos + clasificación del error + diagnóstico
    // =====================================================================
    private class ResultadoHttp
    {
        public bool Exito;
        public string Cuerpo;
        public string MensajeUsuario;
    }

    private IEnumerator EnviarConReintentos(string url, string contexto, ResultadoHttp salida)
    {
        float espera = esperaInicialSegundos;

        for (int intento = 1; intento <= maxIntentos; intento++)
        {
            float t0 = Time.realtimeSinceStartup;
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = timeoutSegundos;
                request.SetRequestHeader("Accept", "application/json");

                yield return request.SendWebRequest();
                float duracion = Time.realtimeSinceStartup - t0;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    if (intento > 1) Debug.Log($"{LogTag} {contexto}: éxito en el intento {intento}/{maxIntentos}.");
                    salida.Exito = true;
                    salida.Cuerpo = request.downloadHandler?.text;
                    yield break;
                }

                bool reintentable = EsReintentable(request);
                bool ultimo = intento == maxIntentos || !reintentable;
                string diagnostico = FormatearDiagnosticoHttp(request, contexto, intento, duracion, reintentable && !ultimo);

                if (ultimo)
                {
                    Debug.LogError(diagnostico);
                    salida.Exito = false;
                    salida.MensajeUsuario = MensajeParaUsuario(request);
                    yield break;
                }

                Debug.LogWarning(diagnostico);
            }

            yield return new WaitForSecondsRealtime(espera);
            espera *= 2f;
        }
    }

    private static bool EsReintentable(UnityWebRequest r)
    {
        if (r.result == UnityWebRequest.Result.ConnectionError) return true; // sin red, DNS, timeout
        if (r.result != UnityWebRequest.Result.ProtocolError) return false; // DataProcessingError: no mejora reintentando
        long c = r.responseCode;
        return c == 408 || c == 429 || (c >= 500 && c != 501);
    }

    private string FormatearDiagnosticoHttp(UnityWebRequest r, string contexto, int intento, float duracion, bool seReintentara)
    {
        string cuerpo = r.downloadHandler?.text ?? string.Empty;
        string contentType = r.GetResponseHeader("Content-Type") ?? "(sin Content-Type)";
        string mensajeServidor = ExtraerMensajeServidor(cuerpo);

        var sb = new StringBuilder();
        sb.AppendLine($"{LogTag} Error al obtener {contexto} — intento {intento}/{maxIntentos}");
        sb.AppendLine($"  Origen probable : {ClasificarOrigen(r, cuerpo, contentType)}");
        sb.AppendLine($"  Resultado Unity : {r.result}  |  HTTP {r.responseCode}  |  {r.error}");
        sb.AppendLine($"  URL             : {r.method} {r.url}");
        sb.AppendLine($"  Content-Type    : {contentType}  |  {cuerpo.Length} caracteres  |  {duracion:0.0} s");
        if (mensajeServidor != null)
            sb.AppendLine($"  Mensaje servidor: {mensajeServidor}");
        sb.AppendLine("  Cuerpo de la respuesta:");
        sb.AppendLine(cuerpo.Length == 0 ? "    (vacío)" : "    " + Recortar(cuerpo));
        sb.Append(seReintentara ? "  → Se reintentará automáticamente." : "  → Sin más reintentos.");
        return sb.ToString();
    }

    /// Distingue entre fallo de red, del endpoint/servidor o de la petición enviada.
    private static string ClasificarOrigen(UnityWebRequest r, string cuerpo, string contentType)
    {
        switch (r.result)
        {
            case UnityWebRequest.Result.ConnectionError:
                return "RED / CONEXIÓN — no hubo respuesta (sin internet, DNS, firewall o timeout). El servidor ni siquiera respondió.";
            case UnityWebRequest.Result.DataProcessingError:
                return "PROCESAMIENTO LOCAL — Unity no pudo procesar la respuesta descargada.";
        }

        long c = r.responseCode;
        bool esHtml = cuerpo.TrimStart().StartsWith("<") || contentType.Contains("text/html");

        if (c >= 500)
        {
            if (c == 502 || c == 503 || c == 504)
                return $"SERVIDOR / HOSTING (HTTP {c}) — el servicio está caído, reiniciando o despertando (Render plan gratuito). No es un problema del JSON.";
            return esHtml
                ? $"SERVIDOR (HTTP {c}) — el endpoint lanzó una excepción y devolvió una página HTML de error. Revisar los logs del backend en Render."
                : $"SERVIDOR (HTTP {c}) — el endpoint lanzó una excepción interna (BD, consulta o código del backend). Los parámetros llegaron, pero el servidor falló al procesarlos. Revisar 'Mensaje servidor' y los logs del backend.";
        }
        if (c == 404) return "ENDPOINT / URL (HTTP 404) — la ruta no existe. Revisar 'apiUrl' en el Inspector y el nombre del endpoint.";
        if (c == 400 || c == 422) return $"PETICIÓN / PARÁMETROS (HTTP {c}) — el servidor rechazó los datos enviados (caso/fase/id). Revisar formato de la query.";
        if (c == 401 || c == 403) return $"AUTORIZACIÓN (HTTP {c}) — el endpoint requiere credenciales o bloqueó la petición.";
        if (c == 429) return "LÍMITE DE PETICIONES (HTTP 429) — demasiadas peticiones simultáneas.";
        return $"HTTP {c} — respuesta no exitosa del servidor.";
    }

    private static string MensajeParaUsuario(UnityWebRequest r)
    {
        if (r.result == UnityWebRequest.Result.ConnectionError)
            return "No hay conexión con el servidor. Verifica tu internet e inténtalo de nuevo.";
        if (r.responseCode >= 500)
            return "El servidor de la UTPL tuvo un problema al entregar los diálogos. Inténtalo de nuevo en unos minutos.";
        if (r.responseCode == 404)
            return "No se encontró el servicio de diálogos. Contacta al soporte del laboratorio.";
        return $"No se pudieron cargar los diálogos (código {r.responseCode}).";
    }

    /// Busca "message" / "error" / "msg" / "detail" en una respuesta JSON de error del backend.
    private static string ExtraerMensajeServidor(string cuerpo)
    {
        if (string.IsNullOrEmpty(cuerpo)) return null;
        var m = Regex.Match(cuerpo, "\"(message|error|msg|detail)\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[2].Value : null;
    }

    private string FormatearDiagnosticoJson(string contexto, string url, string tipo, string detalle, string cuerpo)
    {
        return $"{LogTag} {contexto}: respuesta HTTP 200 pero el JSON no es válido.\n" +
               $"  Origen probable : ESTRUCTURA JSON → {tipo}\n" +
               $"  Detalle         : {detalle}\n" +
               $"  URL             : GET {url}\n" +
               $"  Cuerpo recibido :\n    {(string.IsNullOrEmpty(cuerpo) ? "(vacío)" : Recortar(cuerpo))}";
    }

    private string Recortar(string s) =>
        s.Length <= maxCaracteresCuerpoLog ? s : s.Substring(0, maxCaracteresCuerpoLog) + $"… [+{s.Length - maxCaracteresCuerpoLog} caracteres]";

    private void NotificarFallo(string fase, string mensajeUsuario)
    {
        HuboErrorDeCarga = true;
        if (DialogosErrorEvent == null)
            Debug.LogWarning($"{LogTag} La fase '{fase}' falló y nadie escucha DialogosErrorEvent: la UI de carga puede quedarse esperando. " +
                             "Suscribir DialogosManager a apiManager.DialogosErrorEvent (ver CHANGELOG_ApiManagerErrores.md).");
        DialogosErrorEvent?.Invoke(fase, mensajeUsuario);
    }
}
