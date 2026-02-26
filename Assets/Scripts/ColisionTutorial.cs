using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColisionTutorial : MonoBehaviour
{
    [SerializeField]
    private GameObject controleClick;
    [SerializeField]
    private GameObject panelAviso;
    [SerializeField]
    private PlayerController playerController;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            Time.timeScale = 0;
            playerController.enabled = false;
            controleClick.SetActive(true);
            panelAviso.SetActive(false);
        }
    }
}
