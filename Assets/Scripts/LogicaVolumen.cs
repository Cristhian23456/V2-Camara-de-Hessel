using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LogicaVolumen : MonoBehaviour
{
    [SerializeField]
    private Slider slider;
    [SerializeField]
    private float sliderValue;
    [SerializeField]
    private GameObject imageMute;
    // Start is called before the first frame update
    void Start()
    {
        slider.value = PlayerPrefs.GetFloat("volumenAudio", 0.5f);
        AudioListener.volume = slider.value;
        validateMute();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ChangeSlider(float valor)
    {
        sliderValue = valor;
        PlayerPrefs.SetFloat("volumenAudio", sliderValue);
        AudioListener.volume = slider.value;
        validateMute();
    }
    public void validateMute()
    {
        if (sliderValue == 0)
        {
            imageMute.SetActive(true);
        }
        else
        {
            imageMute.SetActive(false);
        }
    }
}
