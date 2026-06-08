using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManejadorCamara : MonoBehaviour
{
   
    [Header("Ubicaciones cámara parados")]
    [SerializeField]
    private Transform camPsicologoParado = null;
    [SerializeField]
    private Transform camPacienteParado;
    [Header("Camaras")]
    [SerializeField]
    private GameObject camPaciente;
    [SerializeField]
    private GameObject camPsicolog;
    [SerializeField]
    private GameObject camaraGeneral;
    [Header("Ubicaciones cámara sentados")]
    [SerializeField]
    private Transform ubicacionSentadoPaciente;
    [SerializeField]
    private Transform ubicacionSentadoPsicologo;
   


    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(camPaciente.transform.rotation);
        Debug.Log(camPsicolog.transform.rotation);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void activarCamaraPaciente()
    {
        activarCam(camPsicolog, camPaciente);
        
    }
    public void activarCamaraPsicologo()
    {
        activarCam(camPaciente, camPsicolog);
    }
    public void activarCamaraGeneral()
    {
        camaraGeneral.SetActive(true);
        camPaciente.SetActive(false);
        camPsicolog.SetActive(false);
    }
    public void activarCam(GameObject cam1, GameObject cam2)
    {
        camaraGeneral.SetActive(false);
        cam1.SetActive(true);
        cam2.SetActive(false);
    }

    public void cambiarPosiciones(bool cambiar)
    {
        if (cambiar)
        {
            camPaciente.transform.position = camPacienteParado.position;
            camPaciente.transform.rotation = camPacienteParado.rotation;
            camPsicolog.transform.position = camPsicologoParado.position;
            camPsicolog.transform.rotation = camPsicologoParado.rotation;
            Debug.Log("Ejecute este cambio en x q esta parado");
            Debug.Log("----------------Paciente-----------------------");
            Debug.Log(camPacienteParado.position);
            Debug.Log(camPacienteParado.rotation);
            Debug.Log("----------------Psicologo-----------------------");
            Debug.Log(camPacienteParado.position);
            Debug.Log(camPacienteParado.rotation);
        }
        else
        {
            camPaciente.transform.position = ubicacionSentadoPaciente.position;
            camPaciente.transform.rotation = ubicacionSentadoPaciente.rotation;
            camPsicolog.transform.position = ubicacionSentadoPsicologo.position;
            camPsicolog.transform.rotation = ubicacionSentadoPsicologo.rotation;

            Debug.Log("Ejecute este cambio en x q esta sentado");
            Debug.Log("----------------Paciente-----------------------");
            Debug.Log(ubicacionSentadoPaciente.position);
            Debug.Log(ubicacionSentadoPaciente.rotation);
            Debug.Log("----------------Psicologo-----------------------");
            Debug.Log(ubicacionSentadoPsicologo.position);
            Debug.Log(ubicacionSentadoPsicologo.rotation);
        }
    }
}
