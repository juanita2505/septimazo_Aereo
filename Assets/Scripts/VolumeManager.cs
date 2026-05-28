using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class VolumeManager : MonoBehaviour
{
   
    // REFERENCIAS UI

    [Header("Panel Volumen")]
    public GameObject panelVolumen;

    [Header("Slider y Texto")]
    public Slider sliderVolumen;
    public TextMeshProUGUI textoPorcentaje;

    [Header("Botones del menú exterior (para bloquear)")]
    public List<CanvasGroup> botonesMenu = new List<CanvasGroup>();


    // BOToN CERRAR (Button_Salir dentro del panel)

    [Header("Botón Cerrar Panel")]
    public RectTransform botonCerrar;
    public Image imagenBotonCerrar;
    public Color colorNormalCerrar = Color.white;
    public Color colorHoverCerrar = new Color(0.7f, 0.3f, 0.3f);
    public float hoverScaleCerrar = 1.2f;


    // ANIMACIoN

    [Header("Animación")]
    public float fadeSpeed = 8f;
    public float scaleSpeed = 8f;


    // AUDIO

    [Header("Sonido Hover (aleatorio entre 3)")]
    public AudioClip[] hoverSounds = new AudioClip[3];
    public float hoverSoundDelay = 0.1f;

    [Header("Sonido Click")]
    public AudioClip clickSound;
    public float clickSoundDelay = 0.3f;

    [Header("Sonido Abrir y Cerrar Panel")]
    public AudioClip abrirPanelSound;
    public AudioClip cerrarPanelSound;


    // PRIVADOS

    private bool panelVisible = false;
    private CanvasGroup panelCanvasGroup;
    private AudioSource audioSource;
    private Vector3 escalaOriginalCerrar;
    private Vector3 escalaTargetCerrar;
    private float lastHoverTime = -99f;
    private float lastClickTime = -99f;
    private int lastHoverIndex = -1;
    private const string VOLUME_KEY = "MasterVolume";


    // INICIALIZACIÓN

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;

        // CanvasGroup del panel
        panelCanvasGroup = panelVolumen.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
            panelCanvasGroup = panelVolumen.AddComponent<CanvasGroup>();

        // Panel empieza oculto
        panelVolumen.SetActive(false);
        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;

        // Escala original boton cerrar
        if (botonCerrar != null)
        {
            escalaOriginalCerrar = botonCerrar.localScale;
            escalaTargetCerrar = escalaOriginalCerrar;
        }

        // Cargar volumen guardado (default 75)
        float volumenGuardado = PlayerPrefs.GetFloat(VOLUME_KEY, 1f);
        sliderVolumen.value = volumenGuardado;
        AudioListener.volume = volumenGuardado;
        ActualizarTexto(volumenGuardado);

        sliderVolumen.onValueChanged.AddListener(OnSliderChanged);
    }

    void Update()
    {
        // Animacion hover boton cerrar
        if (botonCerrar != null)
        {
            botonCerrar.localScale = Vector3.Lerp(
                botonCerrar.localScale,
                escalaTargetCerrar,
                Time.deltaTime * scaleSpeed
            );
        }
    }


    // ABRIR  CERRAR PANEL

    public void TogglePanel()
    {
        if (panelVisible) CerrarPanel();
        else AbrirPanel();
    }

    public void AbrirPanel()
    {
        panelVisible = true;
        panelVolumen.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadePanel(1f));
        SetMenuExteriorInteractable(false);
        PlaySoundDirecto(abrirPanelSound);
    }

    public void CerrarPanel()
    {
        panelVisible = false;
        StopAllCoroutines();
        StartCoroutine(FadePanel(0f));
        SetMenuExteriorInteractable(true);
        PlaySoundDirecto(cerrarPanelSound);
    }


    // BLOQUEAR MENu EXTERIOR

    private void SetMenuExteriorInteractable(bool estado)
    {
        foreach (var cg in botonesMenu)
        {
            if (cg == null) continue;
            cg.interactable = estado;
            cg.blocksRaycasts = estado;
            cg.alpha = estado ? 1f : 0.4f;
        }
    }


    // HOVER Y CLICK BOTÓN CERRAR

    public void OnCerrarHoverEnter()
    {
        escalaTargetCerrar = escalaOriginalCerrar * hoverScaleCerrar;
        if (imagenBotonCerrar != null)
            imagenBotonCerrar.color = colorHoverCerrar;
        if (CanPlaySound(lastHoverTime, hoverSoundDelay))
        {
            PlayRandomHover();
            lastHoverTime = Time.time;
        }
    }

    public void OnCerrarHoverExit()
    {
        escalaTargetCerrar = escalaOriginalCerrar;
        if (imagenBotonCerrar != null)
            imagenBotonCerrar.color = colorNormalCerrar;
    }

    public void OnCerrarClick()
    {
        if (CanPlaySound(lastClickTime, clickSoundDelay))
        {
            PlaySoundDirecto(clickSound);
            lastClickTime = Time.time;
        }
        StartCoroutine(ClickFlashCerrar());
        CerrarPanel();
    }

    private IEnumerator ClickFlashCerrar()
    {
        escalaTargetCerrar = escalaOriginalCerrar * 0.85f;
        yield return new WaitForSeconds(0.1f);
        escalaTargetCerrar = escalaOriginalCerrar * hoverScaleCerrar;
    }


    // SLIDER

    private void OnSliderChanged(float valor)
    {
        AudioListener.volume = valor;
        ActualizarTexto(valor);
        PlayerPrefs.SetFloat(VOLUME_KEY, valor);
        PlayerPrefs.Save();
    }

    private void ActualizarTexto(float valor)
    {
        if (textoPorcentaje != null)
            textoPorcentaje.text = Mathf.RoundToInt(valor * 100f) + "%";
    }


    // AUDIO HELPERS

    private bool CanPlaySound(float lastTime, float delay)
    {
        return !audioSource.isPlaying &&
               Time.time - lastTime >= delay;
    }

    private void PlayRandomHover()
    {
        if (hoverSounds.Length == 0) return;
        if (hoverSounds.Length == 1)
        {
            audioSource.PlayOneShot(hoverSounds[0]);
            return;
        }
        int index;
        do { index = Random.Range(0, hoverSounds.Length); }
        while (index == lastHoverIndex);
        lastHoverIndex = index;
        audioSource.PlayOneShot(hoverSounds[index]);
    }

    private void PlaySoundDirecto(AudioClip clip)
    {
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }


    // FADE PANEL

    private IEnumerator FadePanel(float targetAlpha)
    {
        panelCanvasGroup.interactable = targetAlpha == 1f;
        panelCanvasGroup.blocksRaycasts = targetAlpha == 1f;

        while (Mathf.Abs(panelCanvasGroup.alpha - targetAlpha) > 0.01f)
        {
            panelCanvasGroup.alpha = Mathf.Lerp(
                panelCanvasGroup.alpha,
                targetAlpha,
                Time.deltaTime * fadeSpeed
            );
            yield return null;
        }

        panelCanvasGroup.alpha = targetAlpha;
        if (targetAlpha == 0f)
            panelVolumen.SetActive(false);
    }
}