using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpcionesGame : MonoBehaviour
{
    [SerializeField]
    private Animator animatorPsicologo;
    private Animator animatorPaciente;
    private bool isPaused = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void PauseGame()
    {
       
        Time.timeScale = 0f; // Pausar el tiempo
        isPaused = true;
       
        animatorPsicologo.speed = 0f; // Pausa la animación
        animatorPaciente.speed = 0f; // Pausa la animación
    }
}
