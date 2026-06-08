using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class CasoConfig
{
    public int nroCaso;
    public string terapeutaObjectName = "TerapeutaC1";
    public string pacienteObjectName = "PacienteC1";
    public int counterAtencion = 16;
    public GameObject personajeLlorando;
    public GameObject personajeNormal;
}

public class DialogosManager : MonoBehaviour
{
    [SerializeField]
    private GameObject uiPreguntas;
  
    [SerializeField]
    private TextMeshProUGUI txtMensaje;
    [SerializeField]
    private TextMeshProUGUI txtPersonaje;
    [SerializeField]
    private TextMeshProUGUI txtPregunta;
    private List<Button> listButtons = new List<Button>();
    public int contador = 0;

    [SerializeField]
    private GameObject btn_Siguiente;
    [SerializeField]
    private Button btn_aceptar;
    [SerializeField]
    private Transform container_preguntas;
    [SerializeField]
    private Button btn_prefab;
    [SerializeField]
    private GameObject ui_retroalimentacion;
    [SerializeField]
    private TextMeshProUGUI txtRetroalimentacion;
    public List<Dialogos> dialogosList = new List<Dialogos>();
    private List<Dialogos> dialogosListDesarrollo = new List<Dialogos>();
    private List<Dialogos> dialogosListFin = new List<Dialogos>();
    [SerializeField]
    private ApiManager apiManager;
    [SerializeField]
    private FichaDiagnostico fichaDiagnostico;
    [SerializeField]
    private BeckInventory inventarioBeck;
    public string fase;
    [SerializeField]
    private GameObject dialagoPsicologo;
    [SerializeField]
    private GameObject dialagoPaciente;
    [SerializeField]
    private TextMeshProUGUI txtDialogoPsiscologo, txtDialogoPaciente;
    [SerializeField]
    private Button btnSigPaciente;
    [SerializeField]
    private Animator animDoctor, animPaciente;
    [SerializeField]
    private AudioSource audioSource;
    [SerializeField]
    private AudioClip audioAtencion;
    Preguntas pregunta;
    [SerializeField]
    private ManejadorCamara manejadorCamara;

    private double caliPorIncorrecto = 0;
    [SerializeField]
    private Calificacion calificacion;
    [SerializeField]
    private VistaFicha vistaFicha;
    private bool parado = true;
    [Header("Label nombres personajes")]
    [SerializeField]
    private TextMeshProUGUI txtNombrePaciente;
    [SerializeField]
    private TextMeshProUGUI txtNombrePsicologo;
    [Header("Ubicaciones personajes no se muevan")]
    [SerializeField]
    private Transform ubiSerntadoPsicologo;
    [SerializeField]
    private Transform ubiSerntadoPaciente;
    [SerializeField]
    private GameObject gameObjectPiscolog;
    [SerializeField]
    private GameObject gameObjectPaciente;
    [Header("Cambios escena")]
    [SerializeField]
    private FinalizarCaso finalizarCaso;
    [SerializeField]
    private AnimationClip animationEntregar;
    [Header("Panel Indicaciones")]
    [SerializeField]
    private GameObject panelIndiAniamciones;
    [SerializeField]
    private TextMeshProUGUI txtAnimaciones;
    [SerializeField]
    private GameObject[] listUbicacionesCamera;
    [SerializeField]
    private Camera mainCamera;
    [Header("Guardar informacion")]
    [SerializeField]
    private SaveData saveData;
    [SerializeField]
    private LoadData loadData;
    [SerializeField] private GameObject btnContinuarDesarrollo;
    [SerializeField] private GameObject personajeC4Llorando, personajeC4;
    [SerializeField] private GameObject pc1, pc4, tc1, tc4;
    bool estado = true;
    private bool d1, d2, d3;
    private bool startDeferred = false;
    private string deferredPhase = "";

    [Header("Configuración de Casos Unificada")]
    [SerializeField]
    private List<CasoConfig> casoConfigs = new List<CasoConfig>();
    private Dictionary<int, CasoConfig> casoConfigsDict = new Dictionary<int, CasoConfig>();

    void Start()
    {
        InitializeCasoConfigs();
        ResolveMissingReferences();

        // Deactivate loading panel on scene startup only if we are NOT loading history
        bool isHistoryGame = false;
        if (loadData != null)
        {
            isHistoryGame = loadData.tieneHistorial;
        }
        else
        {
            GameObject loginObj = GameObject.Find("LoginController");
            if (loginObj != null)
            {
                LoadData ld = loginObj.GetComponent<LoadData>();
                if (ld != null) isHistoryGame = ld.tieneHistorial;
            }
        }

        if (!isHistoryGame)
        {
            GameObject loadingPanel = GameObject.Find("panelLoading");
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }

        if (apiManager != null)
        {
            apiManager.DialogosCargadosEvent += OnDialogosInicialCargados;
            apiManager.DialogosCargadosDesarrolladoEvent += OnDialogosDesarrolloCargados;
            apiManager.DialogosCargadosFinalEvent += OnDialogosFinCargados;
        }
        if (dialagoPaciente != null) dialagoPaciente.SetActive(false);
        if (dialagoPsicologo != null) dialagoPsicologo.SetActive(false);
    }

    private void InitializeCasoConfigs()
    {
        if (casoConfigs.Count == 0)
        {
            casoConfigs.Add(new CasoConfig { nroCaso = 1, terapeutaObjectName = "TerapeutaC1", pacienteObjectName = "PacienteC1", counterAtencion = 16 });
            casoConfigs.Add(new CasoConfig { nroCaso = 4, terapeutaObjectName = "TerapeutaC4", pacienteObjectName = "PacienteC4", counterAtencion = 27 });
        }

        casoConfigsDict.Clear();
        foreach (var config in casoConfigs)
        {
            if (config != null && !casoConfigsDict.ContainsKey(config.nroCaso))
            {
                casoConfigsDict.Add(config.nroCaso, config);
            }
        }
    }

    private void ResolveMissingReferences()
    {
        if (saveData == null)
        {
            GameObject loginObj = GameObject.Find("LoginController");
            if (loginObj != null) saveData = loginObj.GetComponent<SaveData>();
            if (saveData == null)
            {
                #if UNITY_6000_0_OR_NEWER || UNITY_2023_1_OR_NEWER
                saveData = FindAnyObjectByType<SaveData>();
                #else
                saveData = FindObjectOfType<SaveData>();
                #endif
            }
        }
        if (loadData == null)
        {
            GameObject loginObj = GameObject.Find("LoginController");
            if (loginObj != null) loadData = loginObj.GetComponent<LoadData>();
            if (loadData == null)
            {
                #if UNITY_6000_0_OR_NEWER || UNITY_2023_1_OR_NEWER
                loadData = FindAnyObjectByType<LoadData>();
                #else
                loadData = FindObjectOfType<LoadData>();
                #endif
            }
        }
        if (apiManager == null)
        {
            #if UNITY_6000_0_OR_NEWER || UNITY_2023_1_OR_NEWER
            apiManager = FindAnyObjectByType<ApiManager>();
            #else
            apiManager = FindObjectOfType<ApiManager>();
            #endif
        }
        if (fichaDiagnostico == null)
        {
            #if UNITY_6000_0_OR_NEWER || UNITY_2023_1_OR_NEWER
            fichaDiagnostico = FindAnyObjectByType<FichaDiagnostico>();
            #else
            fichaDiagnostico = FindObjectOfType<FichaDiagnostico>();
            #endif
        }
        if (inventarioBeck == null)
        {
            #if UNITY_6000_0_OR_NEWER || UNITY_2023_1_OR_NEWER
            inventarioBeck = FindAnyObjectByType<BeckInventory>();
            #else
            inventarioBeck = FindObjectOfType<BeckInventory>();
            #endif
        }
        if (calificacion == null)
        {
            #if UNITY_6000_0_OR_NEWER || UNITY_2023_1_OR_NEWER
            calificacion = FindAnyObjectByType<Calificacion>();
            #else
            calificacion = FindObjectOfType<Calificacion>();
            #endif
        }
        if (vistaFicha == null)
        {
            #if UNITY_6000_0_OR_NEWER || UNITY_2023_1_OR_NEWER
            vistaFicha = FindAnyObjectByType<VistaFicha>();
            #else
            vistaFicha = FindObjectOfType<VistaFicha>();
            #endif
        }
    }

    public void activaPersonajes()
    {
        int caso = apiManager.getNroCaso();
        CaseSetupSO setup = apiManager.GetCaseSetup(caso);

        if (setup != null)
        {
            GameObject tcObj = GameObject.Find(setup.terapeutaObjectName);
            GameObject pcObj = GameObject.Find(setup.pacienteObjectName);

            if (tcObj != null)
            {
                animDoctor = tcObj.GetComponent<Animator>();
                gameObjectPiscolog = tcObj;
            }
            if (pcObj != null)
            {
                animPaciente = pcObj.GetComponent<Animator>();
                gameObjectPaciente = pcObj;
            }
        }
        else if (casoConfigsDict.TryGetValue(caso, out CasoConfig config))
        {
            GameObject tcObj = GameObject.Find(config.terapeutaObjectName);
            GameObject pcObj = GameObject.Find(config.pacienteObjectName);

            if (tcObj != null)
            {
                animDoctor = tcObj.GetComponent<Animator>();
                gameObjectPiscolog = tcObj;
            }
            if (pcObj != null)
            {
                animPaciente = pcObj.GetComponent<Animator>();
                gameObjectPaciente = pcObj;
            }
        }
        else
        {
            // Fallback legacy
            if (caso == 1)
            {
                tc1 = GameObject.Find("TerapeutaC1");
                pc1 = GameObject.Find("PacienteC1");
                animDoctor = tc1 != null ? tc1.GetComponent<Animator>() : null;
                animPaciente = pc1 != null ? pc1.GetComponent<Animator>() : null;
                gameObjectPaciente = pc1;
                gameObjectPiscolog = tc1;
            }
            else if (caso == 4)
            {
                tc4 = GameObject.Find("TerapeutaC4");
                pc4 = GameObject.Find("PacienteC4");
                animDoctor = tc4 != null ? tc4.GetComponent<Animator>() : null;
                animPaciente = pc4 != null ? pc4.GetComponent<Animator>() : null;
                gameObjectPaciente = pc4;
                gameObjectPiscolog = tc4;
            }
        }

        ConfigureLipSync(gameObjectPiscolog);
        ConfigureLipSync(gameObjectPaciente);
    }

    private void ConfigureLipSync(GameObject target)
    {
        if (target != null)
        {
            var lip = target.GetComponent<AvatarLipSync>();
            if (lip == null) lip = target.AddComponent<AvatarLipSync>();
            lip.SetAudioSource(audioSource);
            lip.SetSensitivity(2.5f);
        }
    }

    private void OnDestroy()
    {
        if (apiManager != null)
        {
            apiManager.DialogosCargadosEvent -= OnDialogosInicialCargados;
            apiManager.DialogosCargadosDesarrolladoEvent -= OnDialogosDesarrolloCargados;
            apiManager.DialogosCargadosFinalEvent -= OnDialogosFinCargados;
        }
    }

    private void OnDialogosInicialCargados(List<Dialogos> dialogos)
    {
        dialogosList = dialogos;
        calificacion.preguntasCant = calcularCantidadP();
        d1 = true;
        CheckAllDialogosLoaded();
    }

    private void OnDialogosDesarrolloCargados(List<Dialogos> dialogos)
    {
        dialogosListDesarrollo = dialogos;
        d2 = true;
        CheckAllDialogosLoaded();
    }

    private void OnDialogosFinCargados(List<Dialogos> dialogos)
    {
        dialogosListFin = dialogos;
        d3 = true;
        CheckAllDialogosLoaded();
    }

    private void CheckAllDialogosLoaded()
    {
        if (d1 && d2 && d3)
        {
            if (btnContinuarDesarrollo != null) btnContinuarDesarrollo.SetActive(false); // Hide the continue button to satisfy user request 2
            activaPersonajes();

            // Hide loading panel if it was showing
            GameObject loadingPanel = GameObject.Find("panelLoading");
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }

            // If a start was deferred because resources weren't ready, execute it now!
            if (startDeferred)
            {
                startDeferred = false;
                if (deferredPhase != "")
                {
                    iniciarFase(deferredPhase);
                    deferredPhase = "";
                }
                darFuncionBtnAceptar();
            }
        }
    }

    public void iniciarFase(string fase)
    {
        if (!(d1 && d2 && d3))
        {
            // Resources are not loaded yet! Defer the phase initialization.
            deferredPhase = fase;
            startDeferred = true;
            GameObject loadingPanel = GameObject.Find("panelLoading");
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
                // Hide any button inside loading panel to prevent manual interference
                Transform btnTrans = loadingPanel.transform.Find("Button");
                if (btnTrans != null) btnTrans.gameObject.SetActive(false);
            }
            return;
        }

        switch (fase)
        {
            case "Desarrollo":
                ResetAnimationBools();
                dialogosList = dialogosListDesarrollo;
                this.fase = fase;
                contador = 0;
                parado = true;
                break;
            case "Final":
                ResetAnimationBools();
                this.fase = fase;
                contador = 0;
                dialogosList = dialogosListFin;
                parado = true;
                break;
        }
    }

    private void ResetAnimationBools()
    {
        if (animDoctor != null)
        {
            animDoctor.SetBool("pararse", false);
            animDoctor.SetBool("sentarse", false);
        }
        if (animPaciente != null)
        {
            animPaciente.SetBool("pararse", false);
            animPaciente.SetBool("sentarse", false);
        }
    }

    private void ToggleLlorando(bool llorando)
    {
        int caso = apiManager.getNroCaso();
        GameObject llorandoObj = null;
        GameObject normalObj = null;

        CaseSetupSO setup = apiManager.GetCaseSetup(caso);
        if (setup != null)
        {
            llorandoObj = setup.personajeLlorando;
            normalObj = setup.personajeNormal;
        }
        else if (casoConfigsDict.TryGetValue(caso, out CasoConfig config))
        {
            llorandoObj = config.personajeLlorando;
            normalObj = config.personajeNormal;
        }

        if (llorandoObj == null) llorandoObj = personajeC4Llorando;
        if (normalObj == null) normalObj = personajeC4;

        if (llorandoObj != null) llorandoObj.SetActive(llorando);
        if (normalObj != null) normalObj.SetActive(!llorando);
    }

    IEnumerator escribirTexto(string texto, TextMeshProUGUI txt, GameObject btn)
    {
        txt.maxVisibleCharacters = 0;
        txt.text = texto;
        txt.richText = true;
        for (int i = 0; i < texto.ToCharArray().Length; i++)
        {
            txt.maxVisibleCharacters++;
            yield return new WaitForSeconds(35f / 500); // Increased typewriter speed (x1.43 of slow speed) to align with character timings
        }

        if (texto == "Gracias, doctor. A veces siento que no puedo controlar estos pensamientos y emociones, los cuales me hacen sentir sumamente desanimada.")
        {
            ToggleLlorando(true);
        }

        if (contador < dialogosList.Count)
        {
            if (txt.gameObject.name != "txt_retroalimentacion")
            {
                yield return new WaitForSeconds(3f);
                if (fase == "Inicial")
                {
                    funcionalidadBtnSiguiente();
                }
                else
                {
                    if (dialogosList[contador].esImportante)
                    {
                        if (btn != null) btn.SetActive(true);
                    }
                    else
                    {
                        funcionalidadBtnSiguiente();
                    }
                }
            }
            else
            {
                if (fase == "Inicial")
                {
                    // Auto-advance after 4 seconds for retroalimentacion (like Attention Point) in Session 1
                    yield return new WaitForSeconds(4f);
                    if (ui_retroalimentacion != null) ui_retroalimentacion.SetActive(false);
                    if (btn != null) btn.SetActive(false);
                    contador++;
                    if (contador < dialogosList.Count)
                    {
                        buscarPersonaje(dialogosList[contador].personaje);
                        if (txtPersonaje != null) txtPersonaje.text = dialogosList[contador].personaje;
                        llamarUiDialogos();
                    }
                    else
                    {
                        ResolveMissingReferences();
                        if (panelIndiAniamciones != null) panelIndiAniamciones.SetActive(true);
                        if (txtAnimaciones != null) txtAnimaciones.text = "(Paciente se despide del terapeuta y sale de la sala)" +
                        "\n(El terapeuta empieza a llenar el documento con los criterios diagnósticos descritos)";
                        StopAllCoroutines();
                        if (animPaciente != null) animPaciente.SetBool("despedirse", true);

                        StartCoroutine(esperarAnimacion(panelIndiAniamciones, true, "Inicial", listUbicacionesCamera[2]));
                    }
                }
                else
                {
                    if (btn != null) btn.SetActive(true);
                }
            }
        }
    }

    public void funcionalidadBtnSiguiente()
    {
        if (btnSigPaciente != null) btnSigPaciente.gameObject.SetActive(false);

        if (contador < dialogosList.Count)
        {
            if (dialogosList[contador].tienePregunta)
            {
                dialagoPaciente.SetActive(false);
                dialagoPsicologo.SetActive(false);
                pregunta = dialogosList[contador].pregunta;
                cargarPreguntas(pregunta);
            }
            else
            {
                if (HandleSpecialDialogueEvents())
                {
                    return;
                }

                AdvanceToNextDialogue();
            }
        }
    }

    private bool HandleSpecialDialogueEvents()
    {
        int caso = apiManager.getNroCaso();
        int counterAtencion = 16;
        CaseSetupSO setup = apiManager.GetCaseSetup(caso);
        if (setup != null)
        {
            counterAtencion = setup.counterAtencion;
        }
        else if (casoConfigsDict.TryGetValue(caso, out CasoConfig config))
        {
            counterAtencion = config.counterAtencion;
        }
        else
        {
            counterAtencion = (caso == 1) ? 16 : 27;
        }

        if (fase == "Inicial" && contador == counterAtencion)
        {
            TriggerAttentionPoint();
            return true;
        }
        else if (fase == "Inicial" && contador == 6)
        {
            TriggerInformedConsent();
            return true;
        }
        else if (fase == "Desarrollo" && contador == 1)
        {
            TriggerSitDownIntro();
            return true;
        }
        else if (fase == "Desarrollo" && contador == 8)
        {
            TriggerTestExecution();
            return true;
        }

        return false;
    }

    private void TriggerAttentionPoint()
    {
        audioSource.clip = audioAtencion;
        audioSource.Play();
        dialagoPaciente.SetActive(false);
        dialagoPsicologo.SetActive(false);
        txtNombrePsicologo.gameObject.SetActive(false);
        txtNombrePaciente.gameObject.SetActive(false);
        manejadorCamara.activarCamaraGeneral();
        ui_retroalimentacion.SetActive(true);
        btn_aceptar.gameObject.SetActive(false);
        uiPreguntas.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(escribirTexto("¡Atención! Lo que estás a punto de observar podría ser clave para el desarrollo de este caso", txtRetroalimentacion, btn_aceptar.gameObject));

        btn_aceptar.onClick.RemoveAllListeners();
        btn_aceptar.onClick.AddListener(() =>
        {
            ui_retroalimentacion.SetActive(false);
            btn_aceptar.gameObject.SetActive(false);
            contador++;
            buscarPersonaje(dialogosList[contador].personaje);
            txtPersonaje.text = dialogosList[contador].personaje;
            llamarUiDialogos();
        });
    }

    private void TriggerInformedConsent()
    {
        dialagoPaciente.SetActive(false);
        dialagoPsicologo.SetActive(false);
        txtNombrePsicologo.gameObject.SetActive(false);
        txtNombrePaciente.gameObject.SetActive(false);
        manejadorCamara.activarCamaraGeneral();
        panelIndiAniamciones.SetActive(true);
        txtAnimaciones.text = "(El terapeuta entrega el consentimiento informado al paciente) \n" +
            "(El paciente simula leerlo y luego procede a firmar el consentimiento informado dado por el terapeuta. Posterior a ello se continua con la entrevista).";
        if (animDoctor != null) animDoctor.SetBool("entregar", true);
        StopAllCoroutines();
        StartCoroutine(ejecutarAnimacionFirmar());
        StartCoroutine(esperarAnimacion(panelIndiAniamciones, false, "Inicial", listUbicacionesCamera[1]));
    }

    private void TriggerSitDownIntro()
    {
        if (animDoctor != null) animDoctor.SetBool("sentarse", true);
        if (animPaciente != null) animPaciente.SetBool("sentarse", true);
        parado = false;
        manejadorCamara.cambiarPosiciones(parado);
        panelIndiAniamciones.SetActive(true);
        txtAnimaciones.text = "(Paciente pasa y se sienta frente al terapeuta)";
        StopAllCoroutines();
        StartCoroutine(esperarAnimacion(panelIndiAniamciones, false, fase, null));
    }

    private void TriggerTestExecution()
    {
        dialagoPaciente.SetActive(false);
        dialagoPsicologo.SetActive(false);
        txtNombrePsicologo.gameObject.SetActive(false);
        txtNombrePaciente.gameObject.SetActive(false);
        manejadorCamara.activarCamaraGeneral();
        panelIndiAniamciones.SetActive(true);
        txtAnimaciones.text = "(El terapeuta le presenta al paciente el test y empieza a simular que lo completa)";
        if (animDoctor != null) animDoctor.SetBool("entregar", true);
        StopAllCoroutines();
        StartCoroutine(ejecutarAnimacionFirmar());
        StartCoroutine(esperarAnimacion(panelIndiAniamciones, false, fase, listUbicacionesCamera[3]));
    }

    private void AdvanceToNextDialogue()
    {
        if (fase == "Final" && contador == 1)
        {
            if (animDoctor != null) animDoctor.SetBool("sentarse", true);
            if (animPaciente != null) animPaciente.SetBool("sentarse", true);
            parado = false;
            manejadorCamara.cambiarPosiciones(parado);
        }
        contador++;

        if (contador < dialogosList.Count)
        {
            buscarPersonaje(dialogosList[contador].personaje);
            txtPersonaje.text = dialogosList[contador].personaje;
            llamarUiDialogos();
        }
        else
        {
            HandlePhaseEnding();
        }
    }

    private void HandlePhaseEnding()
    {
        dialagoPsicologo.SetActive(false);
        dialagoPaciente.SetActive(false);

        if (fase == "Desarrollo")
        {
            inventarioBeck.notaInventarioBecker();
            txtNombrePaciente.gameObject.SetActive(false);
            txtNombrePsicologo.gameObject.SetActive(false);
        }
        else if (fase == "Final")
        {
            if (contador == dialogosList.Count)
            {
                panelIndiAniamciones.SetActive(true);
                txtAnimaciones.text = "(El terapeuta acompaña al paciente hasta la puerta y el paciente sale de la sala)";
                if (animPaciente != null) animPaciente.SetBool("despedirse", true);

                StopAllCoroutines();
                StartCoroutine(esperarAnimacion(panelIndiAniamciones, true, "Final", null));
            }
        }
    }

    public void cargarPreguntas(Preguntas pregunta)
    {
        txtNombrePsicologo.gameObject.SetActive(false);
        txtNombrePaciente.gameObject.SetActive(false);
        manejadorCamara.activarCamaraGeneral();

        btn_aceptar.gameObject.SetActive(false);
        ui_retroalimentacion.SetActive(true);
        if (pregunta.audio == null && !string.IsNullOrEmpty(pregunta.srcAudio))
        {
            string audioResourcePath = pregunta.srcAudio.Replace(".wav", "").Replace(".mp3", "");
            pregunta.audio = Resources.Load<AudioClip>(audioResourcePath);
        }
        audioSource.clip = pregunta.audio;
        audioSource.Play();
        StopAllCoroutines();
        StartCoroutine(escribirPregunta(pregunta.pregunta, txtRetroalimentacion, pregunta));
    }

    public void ActivarBotones(int cantidad, Preguntas pregunta)
    {
        if (listButtons.Count >= cantidad)
        {
            for (int i = 0; i < listButtons.Count; i++)
            {
                if (i < cantidad)
                {
                    listButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = pregunta.respuestas[i].respuesta;
                    listButtons[i].onClick.RemoveAllListeners();
                    Respuestas respuestaObj = pregunta.respuestas[i];
                    string retroalimentacion = pregunta.respuestas[i].retroalimentacion;
                    bool esCorrecta = pregunta.respuestas[i].esCorrecta;
                    string respuesta = pregunta.respuestas[i].respuesta;
                    int cali = pregunta.calificacion;

                    double result = (double)cali / pregunta.respuestas.Length;
                    caliPorIncorrecto = result;
                    calificacion.valorIncorrecto = result;
                    calificacion.valorPregunta = cali;
                    if (estado == true)
                    {
                        calificacion.incrementarFinal(cali);
                        calificacion.incrementarContador();
                        estado = false;
                    }

                    listButtons[i].onClick.AddListener(() => darFuncionBtn(pregunta.pregunta, retroalimentacion, esCorrecta, respuesta, respuestaObj, cali, pregunta.id));
                    listButtons[i].gameObject.SetActive(true);
                }
                else
                {
                    listButtons[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            int cantidadRestante = (cantidad - listButtons.Count);
            for (int i = 0; i < cantidadRestante; i++)
            {
                var newButton = Instantiate(btn_prefab, container_preguntas).GetComponent<Button>();
                newButton.gameObject.SetActive(true);
                listButtons.Add(newButton);
            }
            ActivarBotones(cantidad, pregunta);
        }
    }

    public void darFuncionBtn(string preguntaText, string retroalimentacion, bool esCorrecta, string respuesta, Respuestas respuestasobj, int valor, string id)
    {
        uiPreguntas.SetActive(false);
        ui_retroalimentacion.SetActive(false);
        if (saveData.modo != "Evaluacion")
        {
            ui_retroalimentacion.SetActive(true);
            if (respuestasobj.audio == null && !string.IsNullOrEmpty(respuestasobj.srcAudio))
            {
                string respuestasAudioPath = respuestasobj.srcAudio.Replace(".wav", "").Replace(".mp3", "");
                respuestasobj.audio = Resources.Load<AudioClip>(respuestasAudioPath);
            }
            audioSource.clip = respuestasobj.audio;
            audioSource.Play();

            StopAllCoroutines();
            StartCoroutine(escribirTexto(retroalimentacion, txtRetroalimentacion, btn_aceptar.gameObject));
        }
        else
        {
            funcionAceptarEvaluacion();
            if (id == "8YLueiNVSGXZ9LBFzS6r")
            {
                if (animDoctor != null) animDoctor.SetBool("sentarse", true);
                if (animPaciente != null) animPaciente.SetBool("sentarse", true);
                if (animDoctor != null) animDoctor.SetBool("hablar", true);
                parado = false;
                manejadorCamara.cambiarPosiciones(parado);
            }
            else if (id == "OWCRdkqf2t37Y4hrXtXl")
            {
                ToggleLlorando(false);
                if (animPaciente != null)
                {
                    animPaciente.SetBool("sentarse", true);
                    animPaciente.SetBool("hablar", true);
                }
            }
        }

        if (respuesta == "Frente al terapeuta")
        {
            if (animDoctor != null) animDoctor.SetBool("sentarse", true);
            if (animPaciente != null) animPaciente.SetBool("sentarse", true);
            parado = false;
            manejadorCamara.cambiarPosiciones(parado);
        }
        else if (respuesta == "Dar contención emocional a la paciente ")
        {
            ToggleLlorando(false);
            if (animPaciente != null)
            {
                animPaciente.SetBool("sentarse", true);
                animPaciente.SetBool("hablar", true);
            }
        }
        darFuncionAceptar(esCorrecta, valor, preguntaText, respuesta, retroalimentacion);
    }

    public void darFuncionBtnAceptar()
    {
        if (saveData.modo != "Evaluacion")
        {
            saveData.updatePartidaUser(fase, System.DateTime.Now.ToString("HH:mm:ss; dd MMMM yyyy"), "Caso " + apiManager.getNroCaso());
            saveData.fechaIncio = System.DateTime.Now.ToString("HH:mm:ss; dd MMMM yyyy");
        }

        if (!(d1 && d2 && d3))
        {
            // Resources are not loaded yet! Show loading screen and defer start.
            startDeferred = true;
            GameObject loadingPanel = GameObject.Find("panelLoading");
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
                // Hide any button inside loading panel to prevent manual interference
                Transform btnTrans = loadingPanel.transform.Find("Button");
                if (btnTrans != null) btnTrans.gameObject.SetActive(false);
            }
            return;
        }

        if (dialogosList.Count != 0)
        {
            buscarPersonaje(dialogosList[contador].personaje);
        }

        txtPersonaje.text = dialogosList[contador].personaje;
        llamarUiDialogos();
        manejadorCamara.cambiarPosiciones(parado);

        btnSigPaciente.GetComponent<Button>().onClick.RemoveAllListeners();
        btnSigPaciente.GetComponent<Button>().onClick.AddListener(() =>
        {
            funcionalidadBtnSiguiente();
        });
    }

    void funcionAceptarEvaluacion()
    {
        ResolveMissingReferences();
        if (ui_retroalimentacion != null) ui_retroalimentacion.SetActive(false);
        contador++;
        if (contador < dialogosList.Count)
        {
            if (txtPersonaje != null) txtPersonaje.text = dialogosList[contador].personaje;
            buscarPersonaje(dialogosList[contador].personaje);
            llamarUiDialogos();
            if (container_preguntas != null) container_preguntas.gameObject.SetActive(false);
        }
        else
        {
            if (fase == "Desarrollo")
            {
                HandlePhaseEnding();
            }
            else if (fase == "Inicial")
            {
                if (panelIndiAniamciones != null) panelIndiAniamciones.SetActive(true);
                if (txtAnimaciones != null) txtAnimaciones.text = "(Paciente se despide del terapeuta y sale de la sala)" +
                "\n(El terapeuta empieza a llenar el documento con los criterios diagnósticos descritos)";
                StopAllCoroutines();
                if (animPaciente != null) animPaciente.SetBool("despedirse", true);

                StartCoroutine(esperarAnimacion(panelIndiAniamciones, true, "Inicial", listUbicacionesCamera[2]));
            }
            else if (fase == "Final")
            {
                HandlePhaseEnding();
            }
        }
    }

    public void darFuncionAceptar(bool correcto, int valorSumar, string preguntaRe, string respuesta, string retroalimentacion)
    {
        ResolveMissingReferences();
        if (btn_aceptar != null) btn_aceptar.gameObject.SetActive(false);
        if (correcto)
        {
            if (vistaFicha != null) vistaFicha.addPregunta(preguntaRe, respuesta, retroalimentacion);
            if (calificacion != null) calificacion.incrementar(valorSumar);
            estado = true;
            if (btn_aceptar != null)
            {
                btn_aceptar.gameObject.SetActive(false);
                btn_aceptar.onClick.RemoveAllListeners();
                btn_aceptar.onClick.AddListener(() =>
                {
                    funcionAceptarEvaluacion();
                });
            }
        }
        else
        {
            if (calificacion != null) calificacion.decrementar(caliPorIncorrecto);
            if (btn_aceptar != null)
            {
                btn_aceptar.onClick.RemoveAllListeners();
                btn_aceptar.onClick.AddListener(() =>
                {
                    if (ui_retroalimentacion != null) ui_retroalimentacion.SetActive(true);
                    cargarPreguntas(pregunta);
                    btn_aceptar.gameObject.SetActive(false);
                });
            }
        }
    }

    IEnumerator escribirPregunta(string texto, TextMeshProUGUI txt, Preguntas pregunta)
    {
        txt.maxVisibleCharacters = 0;
        txt.text = texto;
        txt.richText = true;
        for (int i = 0; i < texto.ToCharArray().Length; i++)
        {
            txt.maxVisibleCharacters++;
            yield return new WaitForSeconds(25f / 500);
        }
        uiPreguntas.SetActive(true);
        ActivarBotones(pregunta.respuestas.Length, pregunta);
        container_preguntas.gameObject.SetActive(true);
    }

    IEnumerator esperarAnimacion(GameObject panel, bool faseInicial, string fase, GameObject ubicacionCamera)
    {
        if (ubicacionCamera != null)
        {
            mainCamera.transform.position = ubicacionCamera.transform.position;
            mainCamera.transform.rotation = ubicacionCamera.transform.rotation;
        }

        dialagoPaciente.SetActive(false);
        dialagoPsicologo.SetActive(false);
        txtNombrePsicologo.gameObject.SetActive(false);
        txtNombrePaciente.gameObject.SetActive(false);
        manejadorCamara.activarCamaraGeneral();

        yield return new WaitForSeconds(0.2f);
        if (animPaciente != null) animPaciente.SetBool("despedirse", false);

        yield return new WaitForSeconds(6.0f);

        panel.SetActive(false);
        if (!faseInicial)
        {
            contador++;
            buscarPersonaje(dialogosList[contador].personaje);
            llamarUiDialogos();
        }
        else
        {
            if (fase == "Inicial")
            {
                fichaDiagnostico.notaFichaDiagnostico();
            }
            else if (fase == "Final")
            {
                finalizarCaso.activarRetroFinal();
            }
        }
        mainCamera.transform.position = listUbicacionesCamera[0].transform.position;
        mainCamera.transform.rotation = listUbicacionesCamera[0].transform.rotation;
    }

    public void buscarPersonaje(string personajeHabalndo)
    {
        if (personajeHabalndo.Contains("Psicólogo") || personajeHabalndo.Contains("Terapeuta"))
        {
            txtNombrePsicologo.text = personajeHabalndo;
            if (!parado)
            {
                txtNombrePaciente.gameObject.SetActive(false);
                txtNombrePsicologo.gameObject.SetActive(true);
            }

            dialagoPsicologo.SetActive(true);
            dialagoPaciente.SetActive(false);
        }
        else if (personajeHabalndo.Contains("Paciente"))
        {
            txtNombrePaciente.text = personajeHabalndo;
            if (!parado)
            {
                txtNombrePaciente.gameObject.SetActive(true);
                txtNombrePsicologo.gameObject.SetActive(false);
            }

            dialagoPaciente.SetActive(true);
            dialagoPsicologo.SetActive(false);
        }
    }

    IEnumerator ejecutarAnimacionFirmar()
    {
        yield return new WaitForSeconds(0.3f);
        if (animDoctor != null) animDoctor.SetBool("entregar", false);
        yield return new WaitForSeconds(animationEntregar.length);
        if (animPaciente != null) animPaciente.SetBool("escribir", true);
        yield return new WaitForSeconds(0.3f);
        if (animPaciente != null) animPaciente.SetBool("escribir", false);
    }

    public void llamarUiDialogos()
    {
        string texto = dialogosList[contador].contenido;
        if (dialogosList[contador].personaje.Contains("Psicólogo") || dialogosList[contador].personaje.Contains("Terapeuta"))
        {
            if (!parado)
            {
                if (animDoctor != null) animDoctor.SetBool("hablar", true);
                ubicarPersonajeCentro();
            }
            manejadorCamara.activarCamaraPsicologo();
            StartCoroutine(escribirTexto(dialogosList[contador].contenido, txtDialogoPsiscologo, btnSigPaciente.gameObject));
        }
        else
        {
            if (!parado)
            {
                if (animPaciente != null) animPaciente.SetBool("hablar", true);
                ubicarPersonajeCentro();
            }
            manejadorCamara.activarCamaraPaciente();
            StartCoroutine(escribirTexto(dialogosList[contador].contenido, txtDialogoPaciente, btnSigPaciente.gameObject));
        }
    }

    public void ubicarPersonajeCentro()
    {
        if (gameObjectPiscolog != null && ubiSerntadoPsicologo != null)
            gameObjectPiscolog.transform.position = ubiSerntadoPsicologo.transform.position;
        if (gameObjectPaciente != null && ubiSerntadoPaciente != null)
            gameObjectPaciente.transform.position = ubiSerntadoPaciente.transform.position;
    }

    public int calcularCantidadP()
    {
        int cont = 0;
        for (int i = 0; i < dialogosList.Count; i++)
        {
            if (dialogosList[i].tienePregunta)
            {
                cont++;
            }
        }
        for (int i = 0; i < dialogosListDesarrollo.Count; i++)
        {
            if (dialogosListDesarrollo[i].tienePregunta)
            {
                cont++;
            }
        }
        for (int i = 0; i < dialogosListFin.Count; i++)
        {
            if (dialogosListFin[i].tienePregunta)
            {
                cont++;
            }
        }
        cont += 3;
        return cont;
    }
}
