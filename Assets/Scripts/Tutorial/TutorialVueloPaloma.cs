using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialVueloPaloma : MonoBehaviour
{
    // ─── Referencias ─────────────────────────────────────────────────────────────

    [Header("Referencias")]
    public VueloPalomaGaze controlVuelo;
    public Canvas canvasTutorial;
    public Transform camaraHead;

    [Header("UI - Panel Principal del tutorial")]
    public GameObject panelPrincipal;

    [Header("Input - Gatillo Derecho (Descarga)")]
    public InputActionProperty descargaAction;

    // ─── UI Textos ────────────────────────────────────────────────────────────────

    [Header("UI - Textos")]
    public TextMeshProUGUI textoTitulo;
    public TextMeshProUGUI textoSubtitulo;
    public TextMeshProUGUI textoContador;
    public TextMeshProUGUI textoPasoNumero;

    [Header("UI - Debug")]
    public TextMeshProUGUI textoDebug;

    // ─── UI Imagen instruccion ────────────────────────────────────────────────────

    [Header("UI - Imagen de instruccion (reemplaza texto)")]
    public Image imagenInstruccion;
    public Vector2 tamanioNormal = new Vector2(300f, 200f);

    // ─── UI Resto ─────────────────────────────────────────────────────────────────

    [Header("UI - Barra de progreso")]
    public Image barraProgreso;

    [Header("UI - Paneles")]
    public GameObject panelCompletado;

    // ─── Panel Tutorial Completo con botones ──────────────────────────────────────

    [Header("UI - Panel Tutorial Completo")]
    public GameObject panelTutorialCompleto;
    public Image imagenFondo;
    public GameObject botonReiniciar;
    public GameObject botonVolverMenu;
    public string nombreEscenaMenu = "Menu";

    // ─── Configuracion ────────────────────────────────────────────────────────────

    [Header("Configuracion del Tutorial")]
    public float tiempoRequerido = 3f;
    public float velocidadDecaimiento = 1.5f;
    public float distanciaCanvas = 2f;
    public float alturaCanvas = 0.1f;

    [Header("Umbrales de deteccion")]
    public float umbralGradosCabeza = 20f;
    public float umbralGradosAbajo = 25f;
    public float umbralJoystick = 0.2f;

    // ─── Calibracion ─────────────────────────────────────────────────────────────

    private float pitchInicial = 0f;
    private float yawInicial = 0f;
    private Vector3 forwardInicialProyectado;
    private bool calibrado = false;

    // ─── Tipo de movimiento ───────────────────────────────────────────────────────

    public enum TipoMovimiento
    {
        MirarIzquierda,
        MirarDerecha,
        MirarArriba,
        MirarAbajo,
        JoystickAdelante,
        FrenarGatilloIzquierdo,
        DescargaGatilloDerecho,
        Combinado_GiroYAvance
    }

    [System.Serializable]
    public class PasoTutorial
    {
        public string titulo;
        public string subtitulo;
        public Sprite imagenPaso;   // PNG de diseno para este paso
        public TipoMovimiento movimiento;
    }

    [Header("Pasos del Tutorial")]
    public List<PasoTutorial> pasos = new List<PasoTutorial>();

    // ─── Estado privado ───────────────────────────────────────────────────────────

    private int pasoActual = 0;
    private float tiempoAcumulado = 0f;
    private bool tutorialActivo = true;
    private bool esperandoReset = false;
    private RectTransform imagenRect;

    // ═════════════════════════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ═════════════════════════════════════════════════════════════════════════════

    void OnEnable()
    {
        descargaAction.action?.Enable();
    }

    void OnDisable()
    {
        // Solo deshabilitar si el objeto se destruye, no cuando el menu lo pausa
        if (!gameObject.activeInHierarchy)
            descargaAction.action?.Disable();
    }

    void Start()
    {
        if (controlVuelo == null) controlVuelo = GetComponent<VueloPalomaGaze>();
        if (camaraHead == null) camaraHead = Camera.main.transform;

        if (imagenInstruccion != null)
            imagenRect = imagenInstruccion.GetComponent<RectTransform>();

        StartCoroutine(CalibrarConDelay());

        if (pasos == null || pasos.Count == 0)
            InicializarPasosPorDefecto();

        MostrarPasoActual();

        if (panelTutorialCompleto != null) panelTutorialCompleto.SetActive(false);
        if (panelCompletado != null) panelCompletado.SetActive(false);
        if (botonReiniciar != null) botonReiniciar.SetActive(false);
        if (botonVolverMenu != null) botonVolverMenu.SetActive(false);
    }

    IEnumerator CalibrarConDelay()
    {
        yield return new WaitForSeconds(0.5f);
        CalibrarPosicionInicial();
        MostrarPasoActual();
    }

    void Update()
    {
        // PosicionarCanvas siempre, incluso al terminar el tutorial
        // para que los paneles finales sigan frente al jugador
        PosicionarCanvas();

        if (!tutorialActivo) return;

        MostrarDebug();

        if (esperandoReset) return;

        bool haciendo = VerificarMovimiento(pasos[pasoActual].movimiento);

        if (haciendo)
        {
            tiempoAcumulado += Time.deltaTime;
            ActualizarUI();
            if (tiempoAcumulado >= tiempoRequerido)
                StartCoroutine(CompletarPaso());
        }
        else
        {
            tiempoAcumulado = Mathf.Max(0f, tiempoAcumulado - Time.deltaTime * velocidadDecaimiento);
            ActualizarUI();
        }
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // CALIBRACION
    // ═════════════════════════════════════════════════════════════════════════════

    void CalibrarPosicionInicial()
    {
        if (camaraHead == null) return;

        Vector3 rot = camaraHead.localEulerAngles;
        pitchInicial = rot.x > 180 ? rot.x - 360 : rot.x;
        yawInicial = rot.y > 180 ? rot.y - 360 : rot.y;

        forwardInicialProyectado = Vector3.ProjectOnPlane(
            camaraHead.forward, Vector3.up
        ).normalized;

        calibrado = true;
        Debug.Log($"Calibrado — Pitch:{pitchInicial:F1} Yaw:{yawInicial:F1} Forward:{forwardInicialProyectado}");
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // MOSTRAR PASO
    // ═════════════════════════════════════════════════════════════════════════════

    void MostrarPasoActual()
    {
        if (pasoActual >= pasos.Count) return;
        var p = pasos[pasoActual];

        if (textoTitulo != null) textoTitulo.text = p.titulo;
        if (textoSubtitulo != null) textoSubtitulo.text = p.subtitulo;
        if (textoPasoNumero != null) textoPasoNumero.text = $"Paso {pasoActual + 1} / {pasos.Count}";

        if (imagenInstruccion != null && p.imagenPaso != null)
        {
            imagenInstruccion.sprite = p.imagenPaso;
            imagenInstruccion.enabled = true;
            if (imagenRect != null)
                imagenRect.sizeDelta = tamanioNormal;
        }

        ActualizarBarra(0f);
        ActualizarContador(0f);
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // COMPLETAR PASO
    // ═════════════════════════════════════════════════════════════════════════════

    IEnumerator CompletarPaso()
    {
        esperandoReset = true;
        if (panelCompletado != null) panelCompletado.SetActive(true);
        ActualizarBarra(1f);
        yield return new WaitForSeconds(1.2f);
        if (panelCompletado != null) panelCompletado.SetActive(false);
        pasoActual++;
        tiempoAcumulado = 0f;
        if (pasoActual >= pasos.Count) FinalizarTutorial();
        else { MostrarPasoActual(); esperandoReset = false; }
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // FINALIZAR
    // ═════════════════════════════════════════════════════════════════════════════

    void CentrarPanelFinal()
    {
        if (panelTutorialCompleto == null || camaraHead == null) return;

        Vector3 forward = camaraHead.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 posicion = camaraHead.position + forward * distanciaCanvas;
        posicion.y = camaraHead.position.y;

        panelTutorialCompleto.transform.position = posicion;
        panelTutorialCompleto.transform.rotation = Quaternion.LookRotation(forward);
    }

    void FinalizarTutorial()
    {
        tutorialActivo = false;

        if (panelPrincipal != null)
            panelPrincipal.SetActive(false);

        if (panelTutorialCompleto != null)
            panelTutorialCompleto.SetActive(true);

        CentrarPanelFinal();

        if (botonReiniciar != null) botonReiniciar.SetActive(true);
        if (botonVolverMenu != null) botonVolverMenu.SetActive(true);

        Time.timeScale = 0f;
    }

    // ─── Botones publicos ─────────────────────────────────────────────────────────

    public void OnReiniciarTutorial()
    {
        Time.timeScale = 1f;
        if (panelTutorialCompleto != null) panelTutorialCompleto.SetActive(false);
        ReiniciarTutorial();
    }

    public void OnVolverMenu()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetString("EscenaDestino", nombreEscenaMenu);
        PlayerPrefs.Save();
        SceneManager.LoadScene("SceneCarga");
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // PASOS POR DEFECTO
    // ═════════════════════════════════════════════════════════════════════════════

    void InicializarPasosPorDefecto()
    {
        pasos = new List<PasoTutorial>
        {
            new PasoTutorial { titulo="Bienvenido al vuelo",  subtitulo="La paloma gira siguiendo tu mirada",    movimiento=TipoMovimiento.MirarIzquierda },
            new PasoTutorial { titulo="Gira al otro lado",     subtitulo="Inclinate para doblar en vuelo",        movimiento=TipoMovimiento.MirarDerecha },
            new PasoTutorial { titulo="Sube de altitud",       subtitulo="Controla la altura con tu cabeza",      movimiento=TipoMovimiento.MirarArriba },
            new PasoTutorial { titulo="Baja en picada",        subtitulo="Cuidado con el suelo",                  movimiento=TipoMovimiento.MirarAbajo },
            new PasoTutorial { titulo="Acelera",               subtitulo="Cuanto mas empujes, mas rapido vas",    movimiento=TipoMovimiento.JoystickAdelante },
            new PasoTutorial { titulo="Frena el vuelo",        subtitulo="Reduce velocidad para maniobrar mejor", movimiento=TipoMovimiento.FrenarGatilloIzquierdo },
            new PasoTutorial { titulo="Descarga",              subtitulo="Usala en el momento justo",             movimiento=TipoMovimiento.DescargaGatilloDerecho },
            new PasoTutorial { titulo="Combina todo",          subtitulo="Esquiva edificios como un experto",     movimiento=TipoMovimiento.Combinado_GiroYAvance }
        };
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // DETECCION
    // ═════════════════════════════════════════════════════════════════════════════

    bool VerificarMovimiento(TipoMovimiento tipo)
    {
        if (camaraHead == null || !calibrado) return false;

        // Yaw relativo
        Vector3 rot = camaraHead.localEulerAngles;
        float yawAbs = rot.y > 180 ? rot.y - 360 : rot.y;
        float yaw = yawAbs - yawInicial;
        if (yaw > 180f) yaw -= 360f;
        if (yaw < -180f) yaw += 360f;

        // Pitch con Dot Product
        float pitchDot = Vector3.Dot(camaraHead.forward, Vector3.up);
        float pitchGrados = Mathf.Asin(Mathf.Clamp(pitchDot, -1f, 1f)) * Mathf.Rad2Deg;

        float pitchNeutral = Mathf.Asin(
            Mathf.Clamp(Vector3.Dot(forwardInicialProyectado.normalized, Vector3.up), -1f, 1f)
        ) * Mathf.Rad2Deg;

        float pitchRelativo = pitchGrados - pitchNeutral;

        float joystick = 0f, freno = 0f, descarga = 0f;

        if (controlVuelo != null)
        {
            var a = controlVuelo.acelerarAction.action;
            if (a != null) joystick = a.ReadValue<Vector2>().y;
            var f = controlVuelo.desacelerarAction.action;
            if (f != null) freno = f.ReadValue<float>();
        }
        if (descargaAction.action != null)
            descarga = descargaAction.action.ReadValue<float>();

        switch (tipo)
        {
            case TipoMovimiento.MirarIzquierda: return yaw < -umbralGradosCabeza;
            case TipoMovimiento.MirarDerecha: return yaw > umbralGradosCabeza;
            case TipoMovimiento.MirarArriba: return pitchRelativo > umbralGradosCabeza;
            case TipoMovimiento.MirarAbajo: return pitchRelativo < -umbralGradosAbajo;
            case TipoMovimiento.JoystickAdelante: return joystick > umbralJoystick;
            case TipoMovimiento.FrenarGatilloIzquierdo: return freno > umbralJoystick;
            case TipoMovimiento.DescargaGatilloDerecho: return descarga > umbralJoystick;
            case TipoMovimiento.Combinado_GiroYAvance: return yaw < -umbralGradosCabeza && joystick > umbralJoystick;
        }
        return false;
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // UI HELPERS
    // ═════════════════════════════════════════════════════════════════════════════

    void ActualizarUI()
    {
        ActualizarBarra(tiempoAcumulado / tiempoRequerido);
        ActualizarContador(tiempoAcumulado);
    }

    void ActualizarBarra(float t)
    {
        if (barraProgreso != null)
            barraProgreso.fillAmount = Mathf.Clamp01(t);
    }

    void ActualizarContador(float tiempo)
    {
        if (textoContador == null) return;
        float r = Mathf.Max(0f, tiempoRequerido - tiempo);
        if (tiempo <= 0f) textoContador.text = $"Manten {tiempoRequerido:0}s";
        else if (r <= 0f) textoContador.text = "Perfecto!";
        else textoContador.text = $"Manten... {r:0.0}s";
    }

    void MostrarDebug()
    {
        if (textoDebug == null || camaraHead == null) return;

        float pitchDot = Vector3.Dot(camaraHead.forward, Vector3.up);
        float pitchGrados = Mathf.Asin(Mathf.Clamp(pitchDot, -1f, 1f)) * Mathf.Rad2Deg;

        Vector3 rot = camaraHead.localEulerAngles;
        float yawAbs = rot.y > 180 ? rot.y - 360 : rot.y;
        float yaw = yawAbs - yawInicial;
        if (yaw > 180f) yaw -= 360f;
        if (yaw < -180f) yaw += 360f;

        bool detectado = VerificarMovimiento(pasos[pasoActual].movimiento);

        textoDebug.text =
            $"Pitch (dot): {pitchGrados:F1}\n" +
            $"Yaw rel: {yaw:F1}\n" +
            $"Umbral: +/-{umbralGradosCabeza}  Abajo:{umbralGradosAbajo}\n" +
            $"DETECTADO: {(detectado ? "SI" : "no")}  t:{tiempoAcumulado:F1}/{tiempoRequerido}";
    }

    void PosicionarCanvas()
    {
        if (canvasTutorial == null || camaraHead == null) return;
        Vector3 pos = camaraHead.position + camaraHead.forward * distanciaCanvas + Vector3.up * alturaCanvas;
        canvasTutorial.transform.position = pos;
        canvasTutorial.transform.LookAt(camaraHead.position);
        canvasTutorial.transform.Rotate(0, 180f, 0);
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // API PUBLICA
    // ═════════════════════════════════════════════════════════════════════════════

    /// <summary>Usado por MenuEnJuego para saber si debe reactivar el tutorial al cerrar el menu</summary>
    public bool TutorialEnCurso() { return tutorialActivo; }

    public void SaltarTutorial() { StopAllCoroutines(); FinalizarTutorial(); }

    public void ReiniciarTutorial()
    {
        StopAllCoroutines();
        pasoActual = 0;
        tiempoAcumulado = 0f;
        esperandoReset = false;
        tutorialActivo = true;

        if (panelPrincipal != null) panelPrincipal.SetActive(true);
        if (panelTutorialCompleto != null) panelTutorialCompleto.SetActive(false);
        if (panelCompletado != null) panelCompletado.SetActive(false);
        if (botonReiniciar != null) botonReiniciar.SetActive(false);
        if (botonVolverMenu != null) botonVolverMenu.SetActive(false);

        StartCoroutine(CalibrarConDelay());
        MostrarPasoActual();
    }

    public void IrAPaso(int i)
    {
        if (i < 0 || i >= pasos.Count) return;
        StopAllCoroutines();
        pasoActual = i;
        tiempoAcumulado = 0f;
        esperandoReset = false;
        tutorialActivo = true;
        MostrarPasoActual();
    }
}