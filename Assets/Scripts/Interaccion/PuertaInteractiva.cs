using System.Collections;
using UnityEngine;

/// <summary>
/// Puerta que el jugador abre y cierra con la tecla E. Cada hoja gira alrededor de su bisagra
/// (un punto en el mundo, así que no depende del pivote con que se exportó el modelo) y, si
/// <see cref="HojaPuerta.alejarDelJugador"/> está activo, siempre se abre hacia el lado contrario
/// al jugador para no golpearlo. La pose guardada en la escena es la pose CERRADA.
/// </summary>
public class PuertaInteractiva : MonoBehaviour, IInteractuable
{
    [System.Serializable]
    public class HojaPuerta
    {
        public Transform hoja;
        [Tooltip("Bisagra en coordenadas del mundo (pose cerrada).")]
        public Vector3 bisagra;
        [Tooltip("Extremo libre de la hoja en coordenadas del mundo (pose cerrada).")]
        public Vector3 extremoLibre;
        [Range(10f, 120f)] public float angulo = 95f;
        [Tooltip("Si está activo, se abre hacia el lado opuesto al jugador. Si no, usa 'sentido'.")]
        public bool alejarDelJugador = true;
        [Tooltip("+1 o −1: sentido de giro cuando 'alejarDelJugador' está desactivado.")]
        public float sentido = 1f;
        [Tooltip("Piezas sueltas que giran con la hoja (manijas que no son hijas, etc.).")]
        public Transform[] piezasExtra;

        [System.NonSerialized] public float anguloActual;
        [System.NonSerialized] public float signoAplicado = 1f;
    }

    [SerializeField] private string nombreEspacio = "Puerta";
    [SerializeField] private string textoAbrir = "Abrir puerta";
    [SerializeField] private string textoCerrar = "Cerrar puerta";
    [SerializeField] private HojaPuerta[] hojas = new HojaPuerta[0];
    [SerializeField, Min(0.1f)] private float duracion = 0.7f;
    [SerializeField, Min(0.3f)] private float radio = 1.1f;
    [SerializeField] private bool permitirCerrar = true;
    [Tooltip("Mostrar un aviso con el nombre del espacio la primera vez que se abre.")]
    [SerializeField] private bool avisarAlAbrir = true;
    [SerializeField] private AudioClip sonido;
    [SerializeField, Range(0f, 1f)] private float volumen = 0.6f;

    private bool abierta;
    private bool animando;
    private bool yaAvisado;
    private Vector3 centro;
    private AudioSource fuente;
    private static AudioClip sonidoPorDefecto;

    public bool Abierta => abierta;
    public string NombreEspacio => nombreEspacio;

    public Vector3 PuntoInteraccion => centro;
    public float RadioInteraccion => radio;
    public bool PuedeInteractuar => !animando && (!abierta || permitirCerrar);
    public string TextoInteraccion => abierta ? textoCerrar : textoAbrir;

    private void Awake()
    {
        Vector3 suma = Vector3.zero;
        int n = 0;
        foreach (var h in hojas)
        {
            if (h == null || h.hoja == null) continue;
            suma += (h.bisagra + h.extremoLibre) * 0.5f;
            n++;
        }
        centro = n > 0 ? suma / n : transform.position;

        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0.85f;
        fuente.minDistance = 1f;
        fuente.maxDistance = 12f;
    }

    private void OnEnable() => InteractorJugador.Registrar(this);

    private void OnDisable()
    {
        InteractorJugador.Quitar(this);
        // Si la desactivan a mitad de animación, deja la hoja en su pose final.
        if (animando)
        {
            StopAllCoroutines();
            foreach (var h in hojas) Girar(h, (abierta ? h.angulo : 0f) - h.anguloActual);
            animando = false;
        }
    }

    public void Interactuar(Transform jugador)
    {
        if (!PuedeInteractuar) return;
        if (abierta) Cerrar(); else Abrir(jugador);
    }

    public void Abrir(Transform jugador = null)
    {
        if (abierta || animando) return;
        foreach (var h in hojas)
        {
            if (h == null || h.hoja == null) continue;
            h.signoAplicado = h.alejarDelJugador && jugador != null ? SignoAlejado(h, jugador.position) : Mathf.Sign(h.sentido);
        }
        abierta = true;
        Reproducir(1.05f);
        StartCoroutine(Animar(true));

        if (avisarAlAbrir && !yaAvisado && !string.IsNullOrEmpty(nombreEspacio))
        {
            yaAvisado = true;
            AvisoUI.Mostrar(nombreEspacio, "Puedes recorrer este espacio libremente.", TipoAviso.Info, 2.5f);
        }
    }

    public void Cerrar()
    {
        if (!abierta || animando) return;
        abierta = false;
        Reproducir(0.85f);
        StartCoroutine(Animar(false));
    }

    private IEnumerator Animar(bool abrir)
    {
        animando = true;
        float[] inicio = new float[hojas.Length];
        for (int i = 0; i < hojas.Length; i++) inicio[i] = hojas[i].anguloActual;

        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            float k = Suavizar(t / duracion);
            for (int i = 0; i < hojas.Length; i++)
            {
                var h = hojas[i];
                if (h == null || h.hoja == null) continue;
                float destino = abrir ? h.angulo : 0f;
                Girar(h, Mathf.Lerp(inicio[i], destino, k) - h.anguloActual);
            }
            yield return null;
        }
        foreach (var h in hojas)
            if (h != null && h.hoja != null) Girar(h, (abrir ? h.angulo : 0f) - h.anguloActual);
        animando = false;
    }

    private static void Girar(HojaPuerta h, float delta)
    {
        if (h == null || h.hoja == null || Mathf.Approximately(delta, 0f)) return;
        float grados = delta * h.signoAplicado;
        h.hoja.RotateAround(h.bisagra, Vector3.up, grados);
        if (h.piezasExtra != null)
            foreach (var p in h.piezasExtra) if (p != null) p.RotateAround(h.bisagra, Vector3.up, grados);
        h.anguloActual += delta;
    }

    /// <summary>Elige el sentido de giro que deja el extremo libre más lejos del jugador.</summary>
    private static float SignoAlejado(HojaPuerta h, Vector3 jugador)
    {
        Vector3 brazo = h.extremoLibre - h.bisagra;
        Vector3 a = h.bisagra + Quaternion.AngleAxis(h.angulo * 0.5f, Vector3.up) * brazo;
        Vector3 b = h.bisagra + Quaternion.AngleAxis(-h.angulo * 0.5f, Vector3.up) * brazo;
        a.y = b.y = jugador.y;
        return (a - jugador).sqrMagnitude >= (b - jugador).sqrMagnitude ? 1f : -1f;
    }

    private static float Suavizar(float x)
    {
        x = Mathf.Clamp01(x);
        return 1f - Mathf.Pow(1f - x, 3f); // ease-out cúbico
    }

    private void Reproducir(float tono)
    {
        var clip = sonido != null ? sonido : SonidoPorDefecto();
        if (clip == null) return;
        fuente.pitch = tono;
        fuente.PlayOneShot(clip, volumen);
    }

    /// <summary>"Clac" corto sintetizado, para no depender de un asset de audio.</summary>
    private static AudioClip SonidoPorDefecto()
    {
        if (sonidoPorDefecto != null) return sonidoPorDefecto;
        const int frecuencia = 44100;
        int muestras = (int)(frecuencia * 0.35f);
        var datos = new float[muestras];
        var rnd = new System.Random(7);
        for (int i = 0; i < muestras; i++)
        {
            float t = i / (float)frecuencia;
            float clic = Mathf.Exp(-t * 90f) * Mathf.Sin(2f * Mathf.PI * 180f * t);
            float roce = Mathf.Exp(-t * 9f) * ((float)rnd.NextDouble() * 2f - 1f) * 0.12f * Mathf.Clamp01(t * 30f);
            datos[i] = (clic * 0.7f + roce) * 0.8f;
        }
        sonidoPorDefecto = AudioClip.Create("PuertaClac", muestras, 1, frecuencia, false);
        sonidoPorDefecto.SetData(datos, 0);
        return sonidoPorDefecto;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        foreach (var h in hojas)
        {
            if (h == null) continue;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(h.bisagra, 0.05f);
            Gizmos.DrawLine(h.bisagra, h.extremoLibre);
        }
        Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.4f);
        Vector3 c = Application.isPlaying ? centro : transform.position;
        Gizmos.DrawWireSphere(c, radio);
    }
#endif
}
