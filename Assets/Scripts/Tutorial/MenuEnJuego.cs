using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menú en-juego para Septimazo Aéreo.
/// Se abre con el botón Menu del controlador izquierdo (XRI Left / Menu).
/// Adjuntar al mismo GameObject que VueloPalomaGaze.
/// </summary>
public class MenuEnJuego : MonoBehaviour
{
    // ─── Referencias ────────────────────────────────────────────────────────────

    [Header("Referencias")]
    public Transform camaraHead;

    [Header("Canvas del menú (World Space)")]
    public Canvas canvasMenu;

    [Header("Panel de volumen")]
    [Tooltip("El panel con el slider de volumen")]
    public GameObject panelVolumen;

    [Header("Input – Botón para abrir/cerrar el menú")]
    [Tooltip("XRI Left / Menu  o  cualquier botón que prefieras")]
    public InputActionProperty botonMenuAction;

    // ─── UI ─────────────────────────────────────────────────────────────────────

    [Header("UI – Slider de volumen")]
    public Slider sliderVolumen;
    public TextMeshProUGUI textoVolumen;

    [Header("Audio")]
    [Tooltip("Si usas AudioMixer, asígna el Mixer aquí. Si no, se ajusta AudioListener.volume")]
    public AudioMixer audioMixer;
    [Tooltip("Nombre del parámetro expuesto en el AudioMixer (ej: 'MasterVolume')")]
    public string parametroMixer = "MasterVolume";

    [Header("Escenas")]
    public string nombreEscenaJuego = "SampleScene";
    public string nombreEscenaTutorial = "TutorialInteractivo";

    [Header("Posición del menú")]
    public float distanciaMenu = 2f;
    public float alturaMenu = 0f;

    // ─── Estado ──────────────────────────────────────────────────────────────────

    private bool menuAbierto = false;
    private bool botonPresionadoAntes = false;

    // ─── VueloPalomaGaze ref ──────────────────────────────────────────────────────

    private VueloPalomaGaze controlVuelo;
    private TutorialVueloPaloma controlTutorial;

    // ════════════════════════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ════════════════════════════════════════════════════════════════════════════

    void OnEnable() { botonMenuAction.action?.Enable(); }
    void OnDisable() { botonMenuAction.action?.Disable(); }

    void Start()
    {
        controlVuelo = GetComponent<VueloPalomaGaze>();
        controlTutorial = GetComponent<TutorialVueloPaloma>();

        if (camaraHead == null)
            camaraHead = Camera.main.transform;

        // Empieza cerrado
        if (canvasMenu != null)
            canvasMenu.gameObject.SetActive(false);
        if (panelVolumen != null)
            panelVolumen.SetActive(false);

        // Inicializar slider
        if (sliderVolumen != null)
        {
            sliderVolumen.minValue = 0f;
            sliderVolumen.maxValue = 1f;
            sliderVolumen.value = 1f;
            sliderVolumen.onValueChanged.AddListener(CambiarVolumen);
            ActualizarTextoVolumen(1f);
        }
    }

    void Update()
    {
        float valorBoton = botonMenuAction.action != null
            ? botonMenuAction.action.ReadValue<float>()
            : 0f;

        bool presionado = valorBoton > 0.5f;

        // Toggle al presionar (flanco de subida)
        if (presionado && !botonPresionadoAntes)
        {
            if (menuAbierto) CerrarMenu();
            else AbrirMenu();
        }

        botonPresionadoAntes = presionado;

        // Mantener el menú frente al jugador mientras está abierto
        if (menuAbierto)
            PosicionarMenu();
    }

    // ════════════════════════════════════════════════════════════════════════════
    // ABRIR / CERRAR
    // ════════════════════════════════════════════════════════════════════════════

    public void AbrirMenu()
    {
        menuAbierto = true;

        if (canvasMenu != null)
            canvasMenu.gameObject.SetActive(true);

        PosicionarMenu();

        // Pausar el vuelo y el tutorial
        if (controlVuelo != null) controlVuelo.enabled = false;
        if (controlTutorial != null) controlTutorial.enabled = false;
    }

    public void CerrarMenu()
    {
        menuAbierto = false;

        if (canvasMenu != null)
            canvasMenu.gameObject.SetActive(false);

        // Reanudar el vuelo siempre
        if (controlVuelo != null)
            controlVuelo.enabled = true;

        // Reanudar el tutorial SOLO si sigue activo (no ha terminado)
        if (controlTutorial != null && controlTutorial.TutorialEnCurso())
            controlTutorial.enabled = true;
    }

    // ════════════════════════════════════════════════════════════════════════════
    // BOTONES DEL MENÚ (conectar en el Inspector con OnClick)
    // ════════════════════════════════════════════════════════════════════════════

    public void Reanudar()
    {
        CerrarMenu();
    }

    public void IrAlJuegoSinTutorial()
    {
        CerrarMenu();
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void IrAlTutorial()
    {
        CerrarMenu();
        SceneManager.LoadScene(nombreEscenaTutorial);
    }

    public void AbrirCerrarVolumen()
    {
        if (panelVolumen == null) return;
        panelVolumen.SetActive(!panelVolumen.activeSelf);
    }

    public void SalirDelJuego()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ════════════════════════════════════════════════════════════════════════════
    // VOLUMEN
    // ════════════════════════════════════════════════════════════════════════════

    public void CambiarVolumen(float valor)
    {
        if (audioMixer != null)
        {
            // Convertir 0-1 a dB (-80 a 0)
            float db = valor > 0.001f
                ? Mathf.Log10(valor) * 20f
                : -80f;
            audioMixer.SetFloat(parametroMixer, db);
        }
        else
        {
            AudioListener.volume = valor;
        }

        ActualizarTextoVolumen(valor);
    }

    void ActualizarTextoVolumen(float valor)
    {
        if (textoVolumen != null)
            textoVolumen.text = $"Volumen: {Mathf.RoundToInt(valor * 100)}%";
    }

    // ════════════════════════════════════════════════════════════════════════════
    // POSICIÓN
    // ════════════════════════════════════════════════════════════════════════════

    void PosicionarMenu()
    {
        if (canvasMenu == null || camaraHead == null) return;

        // Usar solo el yaw de la cámara (no inclinar el panel si miras arriba)
        Vector3 forward = camaraHead.forward;
        forward.y = 0f;

        if (forward == Vector3.zero)
            forward = Vector3.forward;

        forward.Normalize();

        Vector3 pos = camaraHead.position
            + forward * distanciaMenu
            + Vector3.up * alturaMenu;

        canvasMenu.transform.position = pos;
        canvasMenu.transform.rotation = Quaternion.LookRotation(forward);
    }
}