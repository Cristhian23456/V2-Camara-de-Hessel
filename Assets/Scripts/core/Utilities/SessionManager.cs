using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public enum SessionPhase { Intro, Desarrollo, Final }

    [SerializeField] private SessionPhase currentPhase;
    [SerializeField] private int currentSession = 1;
    [SerializeField] private GameObject[] phasePanels;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private DialogosManager dialogosManager;

    public void IniciarSesion(int nroSesion)
    {
        currentSession = nroSesion;
        currentPhase = SessionPhase.Intro;

        string tituloSesion = $"SESIÓN {nroSesion}";
        string contenidoSesion = GetContenidoParaSesion(nroSesion);

        if (uiManager != null)
        {
            uiManager.MostrarMensaje(
                titulo: tituloSesion,
                contenido: contenidoSesion,
                textoBtnPrimario: "Continuar",
                accionPrimaria: () => IniciarFase(SessionPhase.Desarrollo)
            );
        }
        else
        {
            // Fallback directo si no hay UIManager activo
            IniciarFase(SessionPhase.Desarrollo);
        }
    }

    private void IniciarFase(SessionPhase fase)
    {
        currentPhase = fase;

        if (dialogosManager != null)
        {
            switch (fase)
            {
                case SessionPhase.Desarrollo:
                    dialogosManager.iniciarFase("Desarrollo");
                    break;
                case SessionPhase.Final:
                    dialogosManager.iniciarFase("Final");
                    break;
            }
        }
    }

    private string GetContenidoParaSesion(int sesion)
    {
        switch (sesion)
        {
            case 1:
                return "Después de recabar información en la primera sesión, el paciente y el terapeuta se reúnen nuevamente luego de una semana para continuar con la entrevista. En esta fase se realizará la aplicación de reactivos para obtener un diagnóstico más preciso.";
            case 2:
                return "Después de la aplicación de los reactivos en la sesión anterior, el paciente y el terapeuta se reúnen nuevamente luego de una semana para continuar con el proceso de la entrevista. En esta fase se dará a conocer el diagnóstico presuntivo del paciente obtenido de la ficha con los criterios diagnósticos y el Inventario de Beck.";
            default:
                return $"Preparando sesión número {sesion} del caso clínico...";
        }
    }
}
