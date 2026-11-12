using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;

public class MovimientoInteractivo : MonoBehaviour
{
    [SerializeField]
    private Transform punto;
    [SerializeField]
    private Transform personajeTransform;
    [SerializeField]
    private GameObject grupo;
    [SerializeField]
    private GameObject mensajes;
    [SerializeField]
    private PlayerController playerController;

    // Start is called before the first frame update
    void Start()
    {
        // Do not disable PlayerController here as it blocks keyboard movement when intro screens are bypassed.
        // The player is already kept inactive during the cinematic via GameObject.SetActive.
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void teletransprote()
    {
        grupo.SetActive(false);
        Time.timeScale = 1;
        mensajes.SetActive(false);
        personajeTransform.position = punto.position;
        playerController.enabled = true;
    }

    public void desactivarScript()
    {
        playerController.enabled = false;
    }
    public void activarScript()
    {
        playerController.enabled = true;
    }
}
