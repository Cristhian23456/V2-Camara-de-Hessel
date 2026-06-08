using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleHandler : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    private Toggle myToggle;
    [SerializeField]
    private GameObject panel;

    void Start()
    {
        // Asegúrate de que el Toggle esté asignado
        if (myToggle != null)
        {
            // Agrega el método OnToggleChanged como listener para el evento onValueChanged
            myToggle.onValueChanged.AddListener(OnToggleChanged);
        }
    }

    // Este método se ejecutará cada vez que el valor del Toggle cambie
    void OnToggleChanged(bool isOn)
    {
        if (isOn)
        {
            panel.SetActive(false);
        }
        else
        {
            panel.SetActive(false);
        }
    }
}
