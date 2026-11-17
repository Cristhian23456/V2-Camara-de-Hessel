using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Documento de consentimiento informado que el estudiante lee y firma (dibujando su firma)
/// antes de continuar la entrevista. La interfaz se construye en tiempo de ejecución.
/// Uso: ConsentimientoFirma.Instancia.Mostrar(nombre, nroCaso, () => { ...continuar... });
/// </summary>
public class ConsentimientoFirma : MonoBehaviour
{
    private static ConsentimientoFirma instancia;

    [Header("Documento")]
    [SerializeField] private string titulo = "CONSENTIMIENTO INFORMADO";
    [SerializeField, TextArea(10, 30)] private string cuerpo =
        "En el marco de la práctica simulada en la Cámara de Gesell del Laboratorio de Psicología de la UTPL, se deja constancia de lo siguiente:\n\n" +
        "1. Objetivo. La entrevista tiene como fin realizar una evaluación psicológica inicial con propósitos exclusivamente formativos.\n\n" +
        "2. Observación y registro. La sesión puede ser observada desde la sala de observación, a través del espejo unidireccional, y registrada en audio y video desde la sala de control, únicamente con fines académicos y de supervisión.\n\n" +
        "3. Confidencialidad. La información obtenida es confidencial y se tratará conforme a los principios éticos de la profesión. No se divulgarán datos que permitan identificar al paciente.\n\n" +
        "4. Voluntariedad. El paciente puede retirar su consentimiento y suspender la entrevista en cualquier momento, sin consecuencia alguna.\n\n" +
        "5. Compromiso del profesional en formación. Me comprometo a respetar la dignidad, la autonomía y la confidencialidad del paciente, y a actuar bajo la supervisión del docente responsable.\n\n" +
        "Este documento forma parte de un ejercicio de simulación. Los casos y personajes son ficticios.";
    [SerializeField] private string textoAcepto = "He leído el documento y acepto actuar conforme a sus términos.";

    [Header("Firma")]
    [SerializeField] private Vector2Int resolucionFirma = new Vector2Int(900, 220);
    [SerializeField, Range(1, 8)] private int grosorTrazo = 3;
    [Tooltip("Cantidad mínima de píxeles dibujados para aceptar la firma (evita firmar con un solo punto).")]
    [SerializeField, Min(1)] private int pixelesMinimos = 400;
    [Tooltip("Guarda la firma como PNG en persistentDataPath/Firmas.")]
    [SerializeField] private bool guardarPng = true;

    public static ConsentimientoFirma Instancia
    {
        get
        {
            if (instancia == null)
            {
                instancia = FindAnyObjectByType<ConsentimientoFirma>();
                if (instancia == null) instancia = new GameObject("[ConsentimientoFirma]").AddComponent<ConsentimientoFirma>();
            }
            return instancia;
        }
    }

    public bool Firmado { get; private set; }
    public bool Visible => raiz != null && raiz.activeSelf;
    public string RutaFirmaGuardada { get; private set; }
    public Texture2D FirmaTextura => lienzo != null ? lienzo.Textura : null;
    public event Action OnFirmado;

    private GameObject raiz;
    private LienzoFirma lienzo;
    private Toggle chkAcepto;
    private Button btnFirmar;
    private TextMeshProUGUI txtDatos;
    private TextMeshProUGUI txtEstado;
    private ScrollRect scroll;
    private Action alFirmar;
    private string nombreEstudiante;
    private bool cursorVisiblePrevio;
    private CursorLockMode cursorLockPrevio;

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        instancia = this;
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    /// <summary>Abre el documento. <paramref name="alFirmar"/> se llama una sola vez al firmar.</summary>
    public void Mostrar(string estudiante, int nroCaso, Action alFirmar)
    {
        if (raiz == null) Construir();
        AsegurarEventSystem();

        this.alFirmar = alFirmar;
        nombreEstudiante = string.IsNullOrWhiteSpace(estudiante) ? "Estudiante" : estudiante.Trim();
        Firmado = false;
        RutaFirmaGuardada = null;

        txtDatos.text = $"<b>Profesional en formación:</b> {nombreEstudiante}     <b>Fecha:</b> {DateTime.Now:dd/MM/yyyy}" +
                        (nroCaso > 0 ? $"     <b>Caso:</b> {nroCaso}" : "") +
                        "\n<b>Paciente:</b> firmado durante la sesión";
        chkAcepto.isOn = false;
        lienzo.Limpiar();
        scroll.verticalNormalizedPosition = 1f;

        cursorVisiblePrevio = Cursor.visible;
        cursorLockPrevio = Cursor.lockState;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        raiz.SetActive(true);
        ActualizarEstado();
    }

    /// <summary>Firma por código (pruebas automáticas). Respeta las mismas validaciones que el botón.</summary>
    public bool IntentarFirmar()
    {
        if (!PuedeFirmar()) return false;
        Firmar();
        return true;
    }

    private bool PuedeFirmar() => raiz != null && raiz.activeSelf && chkAcepto.isOn && lienzo.PixelesPintados >= pixelesMinimos;

    private void ActualizarEstado()
    {
        bool hayFirma = lienzo.PixelesPintados >= pixelesMinimos;
        btnFirmar.interactable = PuedeFirmar();
        if (!hayFirma && !chkAcepto.isOn) txtEstado.text = "Dibuja tu firma en el recuadro y marca la casilla para continuar.";
        else if (!hayFirma) txtEstado.text = "Dibuja tu firma en el recuadro.";
        else if (!chkAcepto.isOn) txtEstado.text = "Marca la casilla de aceptación.";
        else txtEstado.text = "Listo. Presiona \"Firmar y continuar\".";
    }

    private void Firmar()
    {
        if (Firmado) return;
        Firmado = true;

        if (guardarPng) GuardarFirma();

        raiz.SetActive(false);
        Cursor.visible = cursorVisiblePrevio;
        Cursor.lockState = cursorLockPrevio;

        var callback = alFirmar;
        alFirmar = null;
        callback?.Invoke();
        OnFirmado?.Invoke();
    }

    private void GuardarFirma()
    {
        try
        {
            string carpeta = Path.Combine(Application.persistentDataPath, "Firmas");
            Directory.CreateDirectory(carpeta);
            string usuario = DataUsers.Instance != null && !string.IsNullOrEmpty(DataUsers.Instance.username)
                ? DataUsers.Instance.username
                : "estudiante";
            foreach (char c in Path.GetInvalidFileNameChars()) usuario = usuario.Replace(c, '_');
            RutaFirmaGuardada = Path.Combine(carpeta, $"consentimiento_{usuario}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            File.WriteAllBytes(RutaFirmaGuardada, lienzo.Textura.EncodeToPNG());
            Debug.Log("[Consentimiento] Firma guardada en " + RutaFirmaGuardada);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Consentimiento] No se pudo guardar la firma: " + e.Message);
            RutaFirmaGuardada = null;
        }
    }

    private static void AsegurarEventSystem()
    {
        if (EventSystem.current != null) return;
        if (FindAnyObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    // ---------------------------------------------------------------- Construcción de la UI

    private static readonly Color Papel = new Color32(0xFB, 0xFA, 0xF5, 0xFF);
    private static readonly Color Tinta = new Color32(0x1F, 0x29, 0x37, 0xFF);
    private static readonly Color Azul = new Color32(0x0B, 0x3D, 0x91, 0xFF);
    private static readonly Color Gris = new Color32(0x9C, 0xA3, 0xAF, 0xFF);

    private void Construir()
    {
        raiz = new GameObject("CanvasConsentimiento", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        raiz.transform.SetParent(transform, false);
        var canvas = raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;
        var escalador = raiz.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920, 1080);
        escalador.matchWidthOrHeight = 0.5f;

        // Fondo oscuro que bloquea los clics al resto de la interfaz
        var fondo = Crear("Fondo", raiz.transform, typeof(Image));
        Estirar(fondo);
        fondo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        // Hoja del documento
        var hoja = Crear("Documento", raiz.transform, typeof(Image));
        Ubicar(hoja, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120f, 1000f));
        hoja.GetComponent<Image>().color = Papel;

        var titulo = Texto(hoja, "Titulo", this.titulo, 40, FontStyles.Bold, Azul, TextAlignmentOptions.Center);
        UbicarArriba(titulo.gameObject, 30f, 56f, 50f);

        var linea = Crear("Linea", hoja, typeof(Image));
        UbicarArriba(linea, 92f, 3f, 50f);
        linea.GetComponent<Image>().color = Azul;

        // Cuerpo con scroll
        var vista = Crear("Cuerpo", hoja, typeof(Image), typeof(ScrollRect));
        UbicarArriba(vista, 110f, 420f, 50f);
        vista.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.03f);
        var viewport = Crear("Viewport", vista, typeof(RectMask2D));
        Estirar(viewport, 16f);
        var contenido = Texto(viewport, "Texto", cuerpo, 25, FontStyles.Normal, Tinta, TextAlignmentOptions.TopJustified);
        var rc = (RectTransform)contenido.transform;
        rc.anchorMin = new Vector2(0f, 1f);
        rc.anchorMax = new Vector2(1f, 1f);
        rc.pivot = new Vector2(0.5f, 1f);
        rc.sizeDelta = Vector2.zero;
        contenido.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll = vista.GetComponent<ScrollRect>();
        scroll.viewport = (RectTransform)viewport.transform;
        scroll.content = rc;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        txtDatos = Texto(hoja, "Datos", "", 22, FontStyles.Normal, Tinta, TextAlignmentOptions.TopLeft);
        UbicarArriba(txtDatos.gameObject, 545f, 64f, 50f);

        var etiqueta = Texto(hoja, "EtiquetaFirma", "Firma del profesional en formación:", 22, FontStyles.Bold, Tinta, TextAlignmentOptions.TopLeft);
        UbicarArriba(etiqueta.gameObject, 618f, 30f, 50f);

        // Recuadro de firma
        var marco = Crear("MarcoFirma", hoja, typeof(Image));
        UbicarArriba(marco, 652f, 200f, 50f);
        marco.GetComponent<Image>().color = Gris;
        var areaFirma = Crear("AreaFirma", marco, typeof(RawImage), typeof(LienzoFirma));
        Estirar(areaFirma, 2f);
        lienzo = areaFirma.GetComponent<LienzoFirma>();
        lienzo.grosor = grosorTrazo;
        lienzo.Inicializar(resolucionFirma.x, resolucionFirma.y);
        lienzo.Cambio += ActualizarEstado;
        var guia = Texto(marco, "Guia", "Firme aquí con el mouse", 18, FontStyles.Italic, Gris, TextAlignmentOptions.BottomRight);
        Estirar(guia.gameObject, 10f);
        guia.raycastTarget = false;

        // Casilla de aceptación
        chkAcepto = CrearCasilla(hoja, textoAcepto);
        UbicarArriba(chkAcepto.gameObject, 868f, 40f, 50f);
        chkAcepto.onValueChanged.AddListener(_ => ActualizarEstado());

        txtEstado = Texto(hoja, "Estado", "", 20, FontStyles.Italic, new Color32(0xB4, 0x53, 0x09, 0xFF), TextAlignmentOptions.MidlineLeft);
        var re = (RectTransform)txtEstado.transform;
        re.anchorMin = re.anchorMax = re.pivot = new Vector2(0f, 0f);
        re.anchoredPosition = new Vector2(50f, 36f);
        re.sizeDelta = new Vector2(560f, 50f);

        var btnLimpiar = CrearBoton(hoja, "Limpiar", Color.white, Azul);
        var rl = (RectTransform)btnLimpiar.transform;
        rl.anchorMin = rl.anchorMax = rl.pivot = new Vector2(1f, 0f);
        rl.anchoredPosition = new Vector2(-330f, 30f);
        rl.sizeDelta = new Vector2(180f, 60f);
        btnLimpiar.onClick.AddListener(() => lienzo.Limpiar());

        btnFirmar = CrearBoton(hoja, "Firmar y continuar", Azul, Color.white);
        var rb = (RectTransform)btnFirmar.transform;
        rb.anchorMin = rb.anchorMax = rb.pivot = new Vector2(1f, 0f);
        rb.anchoredPosition = new Vector2(-50f, 30f);
        rb.sizeDelta = new Vector2(260f, 60f);
        btnFirmar.onClick.AddListener(() => { if (PuedeFirmar()) Firmar(); });

        raiz.SetActive(false);
    }

    private static GameObject Crear(string nombre, Transform padre, params Type[] componentes)
    {
        var tipos = new Type[componentes.Length + 1];
        tipos[0] = typeof(RectTransform);
        componentes.CopyTo(tipos, 1);
        var go = new GameObject(nombre, tipos);
        go.transform.SetParent(padre, false);
        return go;
    }

    private static GameObject Crear(string nombre, GameObject padre, params Type[] componentes) => Crear(nombre, padre.transform, componentes);

    private static void Estirar(GameObject go, float margen = 0f)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
    }

    private static void Ubicar(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 tam)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }

    /// <summary>Ancla a todo el ancho del padre, a <paramref name="y"/> píxeles del borde superior.</summary>
    private static void UbicarArriba(GameObject go, float y, float alto, float margenX)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -y);
        rt.sizeDelta = new Vector2(-2f * margenX, alto);
    }

    private static TextMeshProUGUI Texto(GameObject padre, string nombre, string texto, float tam, FontStyles estilo, Color color, TextAlignmentOptions alineacion)
    {
        var go = Crear(nombre, padre, typeof(TextMeshProUGUI));
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = texto;
        t.fontSize = tam;
        t.fontStyle = estilo;
        t.color = color;
        t.alignment = alineacion;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.richText = true;
        return t;
    }

    private static Button CrearBoton(GameObject padre, string texto, Color fondo, Color colorTexto)
    {
        var go = Crear("Btn_" + texto, padre, typeof(Image), typeof(Button));
        var img = go.GetComponent<Image>();
        img.color = fondo;
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        var colores = btn.colors;
        colores.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.6f);
        colores.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        btn.colors = colores;
        var t = Texto(go, "Texto", texto, 24, FontStyles.Bold, colorTexto, TextAlignmentOptions.Center);
        Estirar(t.gameObject);
        t.raycastTarget = false;
        // Borde del botón claro
        if (fondo == Color.white) go.AddComponent<Outline>().effectColor = Azul;
        return btn;
    }

    private static Toggle CrearCasilla(GameObject padre, string texto)
    {
        var go = Crear("Acepto", padre, typeof(Toggle));
        var caja = Crear("Caja", go, typeof(Image));
        Ubicar(caja, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
        var imgCaja = caja.GetComponent<Image>();
        imgCaja.color = Color.white;
        caja.AddComponent<Outline>().effectColor = Azul;
        var marca = Crear("Marca", caja, typeof(Image));
        Estirar(marca, 7f);
        var imgMarca = marca.GetComponent<Image>();
        imgMarca.color = Azul;
        var etiqueta = Texto(go, "Etiqueta", texto, 22, FontStyles.Normal, Tinta, TextAlignmentOptions.MidlineLeft);
        var re = (RectTransform)etiqueta.transform;
        re.anchorMin = Vector2.zero;
        re.anchorMax = Vector2.one;
        re.offsetMin = new Vector2(48f, 0f);
        re.offsetMax = Vector2.zero;
        var toggle = go.GetComponent<Toggle>();
        toggle.targetGraphic = imgCaja;
        toggle.graphic = imgMarca;
        toggle.isOn = false;
        return toggle;
    }
}
