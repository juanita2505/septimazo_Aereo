using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialVueloPaloma : MonoBehaviour
{
    [Header("Referencias")]
    public VueloPalomaGaze controlVuelo;
    public Canvas canvasTutorial;
    public Transform camaraHead;

    [Tooltip("El panel principal con toda la UI del tutorial (se oculta al terminar)")]
    public GameObject panelPrincipal;

    [Header("Input - Gatillo Derecho (Descarga)")]
    public InputActionProperty descargaAction;

    [Header("UI - Textos")]
    public TextMeshProUGUI textoTitulo;
    public TextMeshProUGUI textoInstruccion;
    public TextMeshProUGUI textoSubtitulo;
    public TextMeshProUGUI textoContador;
    public TextMeshProUGUI textoPasoNumero;

    [Header("UI - Debug")]
    public TextMeshProUGUI textoDebug;

    [Header("UI - Barra de progreso")]
    public Image barraProgreso;

    [Header("UI - Icono de movimiento")]
    public Image iconoMovimiento;

    [Header("UI - Paneles")]
    public GameObject panelCompletado;
    public GameObject panelTutorialCompleto;

    [Header("UI - Panel Final")]
    public GameObject panelFinal;
    public TextMeshProUGUI textoFinal;
    public float tiempoAntesDeCambiarEscena = 3f;
    public string nombreEscenaPrincipal = "SampleScene";

    [Header("Iconos (opcional)")]
    public Sprite iconoCabezaIzquierda;
    public Sprite iconoCabezaDerecha;
    public Sprite iconoCabezaArriba;
    public Sprite iconoCabezaAbajo;
    public Sprite iconoJoystickAdelante;
    public Sprite iconoFreno;
    public Sprite iconoDescarga;
    public Sprite iconoCombinado;

    [Header("Configuracion del Tutorial")]
    public float tiempoRequerido = 3f;
    public float velocidadDecaimiento = 1.5f;
    public float distanciaCanvas = 2f;
    public float alturaCanvas = 0.1f;

    [Header("Umbrales de deteccion")]
    public float umbralGradosCabeza = 10f;
    public float umbralJoystick = 0.2f;

    public enum TipoMovimiento
    {
        MirarIzquierda, MirarDerecha, MirarArriba, MirarAbajo,
        JoystickAdelante, FrenarGatilloIzquierdo, DescargaGatilloDerecho, Combinado_GiroYAvance
    }

    [System.Serializable]
    public class PasoTutorial
    {
        public string titulo;
        public string instruccion;
        public string subtitulo;
        public TipoMovimiento movimiento;
        public Sprite icono;
    }

    private List<PasoTutorial> pasos = new List<PasoTutorial>();
    private int pasoActual = 0;
    private float tiempoAcumulado = 0f;
    private bool tutorialActivo = true;
    private bool esperandoReset = false;

    void OnEnable()
    {
        descargaAction.action?.Enable();
    }

    void OnDisable()
    {
        if (!gameObject.activeInHierarchy)
            descargaAction.action?.Disable();
    }

    void Start()
    {
        if (controlVuelo == null) controlVuelo = GetComponent<VueloPalomaGaze>();
        if (camaraHead == null) camaraHead = Camera.main.transform;

        InicializarPasos();
        MostrarPasoActual();

        if (panelTutorialCompleto != null) panelTutorialCompleto.SetActive(false);
        if (panelCompletado != null) panelCompletado.SetActive(false);
        if (panelFinal != null) panelFinal.SetActive(false);
    }

    void Update()
    {
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

    void MostrarDebug()
    {
        if (textoDebug == null || camaraHead == null) return;

        Vector3 localRot = camaraHead.localEulerAngles;
        Vector3 worldRot = camaraHead.eulerAngles;

        float pitchL = localRot.x > 180 ? localRot.x - 360 : localRot.x;
        float yawL = localRot.y > 180 ? localRot.y - 360 : localRot.y;
        float pitchW = worldRot.x > 180 ? worldRot.x - 360 : worldRot.x;
        float yawW = worldRot.y > 180 ? worldRot.y - 360 : worldRot.y;

        float joystick = 0f, freno = 0f, descarga = 0f;

        if (controlVuelo != null)
        {
            var a = controlVuelo.acelerarAction.action;
            if (a != null) joystick = a.ReadValue<Vector2>().y;
            var f = controlVuelo.desacelerarAction.action;
            if (f != null) freno = f.ReadValue<float>();
        }
        if (descargaAction.action != null) descarga = descargaAction.action.ReadValue<float>();

        bool detectado = VerificarMovimiento(pasos[pasoActual].movimiento);

        textoDebug.text =
            $"LOCAL  pitch:{pitchL:F1}  yaw:{yawL:F1}\n" +
            $"WORLD  pitch:{pitchW:F1}  yaw:{yawW:F1}\n" +
            $"Joy:{joystick:F2}  Freno:{freno:F2}  Desc:{descarga:F2}\n" +
            $"Umbral: {umbralGradosCabeza}  DETECTADO: {(detectado ? "SI" : "no")}" +
            $"  t:{tiempoAcumulado:F1}/{tiempoRequerido}";
    }

    bool VerificarMovimiento(TipoMovimiento tipo)
    {
        if (camaraHead == null) return false;

        Vector3 rot = camaraHead.localEulerAngles;
        float pitch = rot.x > 180 ? rot.x - 360 : rot.x;
        float yaw = rot.y > 180 ? rot.y - 360 : rot.y;

        float joystick = 0f, freno = 0f, descarga = 0f;

        if (controlVuelo != null)
        {
            var a = controlVuelo.acelerarAction.action;
            if (a != null) joystick = a.ReadValue<Vector2>().y;
            var f = controlVuelo.desacelerarAction.action;
            if (f != null) freno = f.ReadValue<float>();
        }
        if (descargaAction.action != null) descarga = descargaAction.action.ReadValue<float>();

        switch (tipo)
        {
            case TipoMovimiento.MirarIzquierda: return yaw < -umbralGradosCabeza;
            case TipoMovimiento.MirarDerecha: return yaw > umbralGradosCabeza;
            case TipoMovimiento.MirarArriba: return pitch < -umbralGradosCabeza;
            case TipoMovimiento.MirarAbajo: return pitch > umbralGradosCabeza;
            case TipoMovimiento.JoystickAdelante: return joystick > umbralJoystick;
            case TipoMovimiento.FrenarGatilloIzquierdo: return freno > umbralJoystick;
            case TipoMovimiento.DescargaGatilloDerecho: return descarga > umbralJoystick;
            case TipoMovimiento.Combinado_GiroYAvance: return yaw < -umbralGradosCabeza && joystick > umbralJoystick;
        }
        return false;
    }

    void InicializarPasos()
    {
        pasos = new List<PasoTutorial>
        {
            new PasoTutorial { titulo="Bienvenido al vuelo",      instruccion="Gira la cabeza hacia la IZQUIERDA",           subtitulo="La paloma gira siguiendo tu mirada",       movimiento=TipoMovimiento.MirarIzquierda,         icono=iconoCabezaIzquierda },
            new PasoTutorial { titulo="Gira al otro lado",         instruccion="Gira la cabeza hacia la DERECHA",             subtitulo="Inclinate para doblar en vuelo",            movimiento=TipoMovimiento.MirarDerecha,           icono=iconoCabezaDerecha },
            new PasoTutorial { titulo="Sube de altitud",           instruccion="Mira hacia ARRIBA para subir",                subtitulo="Controla la altura con tu cabeza",          movimiento=TipoMovimiento.MirarArriba,            icono=iconoCabezaArriba },
            new PasoTutorial { titulo="Baja en picada",            instruccion="Mira hacia ABAJO para descender",             subtitulo="Cuidado con el suelo",                      movimiento=TipoMovimiento.MirarAbajo,             icono=iconoCabezaAbajo },
            new PasoTutorial { titulo="Acelera",                   instruccion="Empuja el JOYSTICK IZQUIERDO hacia adelante",  subtitulo="Cuanto mas empujes, mas rapido vas",        movimiento=TipoMovimiento.JoystickAdelante,       icono=iconoJoystickAdelante },
            new PasoTutorial { titulo="Frena el vuelo",            instruccion="Presiona el GATILLO IZQUIERDO para frenar",   subtitulo="Reduce velocidad para maniobrar mejor",     movimiento=TipoMovimiento.FrenarGatilloIzquierdo, icono=iconoFreno },
            new PasoTutorial { titulo="Descarga",                  instruccion="Presiona el GATILLO DERECHO para la descarga", subtitulo="Usala en el momento justo",                 movimiento=TipoMovimiento.DescargaGatilloDerecho, icono=iconoDescarga },
            new PasoTutorial { titulo="Combina todo",              instruccion="Gira a la izquierda MIENTRAS aceleras",       subtitulo="Asi esquivaras edificios como un experto",  movimiento=TipoMovimiento.Combinado_GiroYAvance,  icono=iconoCombinado }
        };
    }

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

    void MostrarPasoActual()
    {
        if (pasoActual >= pasos.Count) return;
        var p = pasos[pasoActual];
        if (textoTitulo != null) textoTitulo.text = p.titulo;
        if (textoInstruccion != null) textoInstruccion.text = p.instruccion;
        if (textoSubtitulo != null) textoSubtitulo.text = p.subtitulo;
        if (textoPasoNumero != null) textoPasoNumero.text = $"Paso {pasoActual + 1} / {pasos.Count}";
        if (iconoMovimiento != null && p.icono != null) iconoMovimiento.sprite = p.icono;
        ActualizarBarra(0f);
        ActualizarContador(0f);
    }

    void FinalizarTutorial()
    {
        tutorialActivo = false;

        if (panelPrincipal != null) panelPrincipal.SetActive(false);

        if (panelFinal != null)
        {
            panelFinal.SetActive(true);
            if (textoFinal != null)
                textoFinal.text = "Ya puedes empezar a jugar!\nCargando...";
        }

        StartCoroutine(CargarEscenaPrincipal());
    }

    IEnumerator CargarEscenaPrincipal()
    {
        float tiempo = tiempoAntesDeCambiarEscena;
        while (tiempo > 0f)
        {
            if (textoFinal != null)
                textoFinal.text = $"Ya puedes empezar a jugar!\n\nCargando en {Mathf.CeilToInt(tiempo)}...";
            yield return null;
            tiempo -= Time.deltaTime;
        }
        SceneManager.LoadScene(nombreEscenaPrincipal);
    }

    void ActualizarUI()
    {
        ActualizarBarra(tiempoAcumulado / tiempoRequerido);
        ActualizarContador(tiempoAcumulado);
    }

    void ActualizarBarra(float t)
    {
        if (barraProgreso != null) barraProgreso.fillAmount = Mathf.Clamp01(t);
    }

    void ActualizarContador(float tiempo)
    {
        if (textoContador == null) return;
        float r = Mathf.Max(0f, tiempoRequerido - tiempo);
        if (tiempo <= 0f) textoContador.text = $"Manten {tiempoRequerido:0}s";
        else if (r <= 0f) textoContador.text = "Perfecto!";
        else textoContador.text = $"Manten... {r:0.0}s";
    }

    void PosicionarCanvas()
    {
        if (canvasTutorial == null || camaraHead == null) return;
        Vector3 pos = camaraHead.position + camaraHead.forward * distanciaCanvas + Vector3.up * alturaCanvas;
        canvasTutorial.transform.position = pos;
        canvasTutorial.transform.LookAt(camaraHead.position);
        canvasTutorial.transform.Rotate(0, 180f, 0);
    }

    public bool TutorialEnCurso() { return tutorialActivo; }

    public void SaltarTutorial() { StopAllCoroutines(); FinalizarTutorial(); }

    public void ReiniciarTutorial()
    {
        StopAllCoroutines();
        pasoActual = 0; tiempoAcumulado = 0f; esperandoReset = false; tutorialActivo = true;
        if (panelPrincipal != null) panelPrincipal.SetActive(true);
        if (panelTutorialCompleto != null) panelTutorialCompleto.SetActive(false);
        if (panelCompletado != null) panelCompletado.SetActive(false);
        if (panelFinal != null) panelFinal.SetActive(false);
        MostrarPasoActual();
    }

    public void IrAPaso(int i)
    {
        if (i < 0 || i >= pasos.Count) return;
        StopAllCoroutines();
        pasoActual = i; tiempoAcumulado = 0f; esperandoReset = false; tutorialActivo = true;
        MostrarPasoActual();
    }
}