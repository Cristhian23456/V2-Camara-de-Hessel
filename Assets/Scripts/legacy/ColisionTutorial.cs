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
            // Programmatically hide the Aceptar button inside the popup to avoid manual clicks
            UnityEngine.UI.Button[] buttons = controleClick.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var btn in buttons)
            {
                btn.gameObject.SetActive(false);
            }

            controleClick.SetActive(true);
            panelAviso.SetActive(false);

            // Teleport the player immediately to enter the game
            MovimientoInteractivo mov = null;
            #if UNITY_6000_0_OR_NEWER || UNITY_2023_1_OR_NEWER
            mov = FindAnyObjectByType<MovimientoInteractivo>();
            #else
            mov = FindObjectOfType<MovimientoInteractivo>();
            #endif

            if (mov != null)
            {
                mov.teletransprote();
            }

            StartCoroutine(AutoDismissTutorialMessage());
        }
    }

    private IEnumerator AutoDismissTutorialMessage()
    {
        yield return new WaitForSeconds(1.8f);
        if (controleClick != null)
        {
            controleClick.SetActive(false);
        }
    }
}
