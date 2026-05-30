using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TutorialVueloPaloma : MonoBehaviour
{
    [Header("Referencias")]
    public VueloPalomaGaze controlVuelo;
    public Canvas canvasTutorial;
    public Transform camaraHead;

    [Header("Input - Gatillo Derecho (Descarga)")]
    public InputActionProperty descargaAction;

    [Header("UI – Textos (se mantienen)")]
    public TextMeshProUGUI textoTitulo;
    public TextMeshProUGUI textoSubtitulo;
    public TextMeshProUGUI textoContador;
    public TextMeshProUGUI textoPasoNumero;

    [Header("UI – Imagen de instrucción (reemplaza el texto)")]
    public Image imagenInstruccion;         // Image en el Canvas
    public Vector2 tamañoNormal = new Vector2(300f, 200f);  // ajusta al tamaño de tus PNGs

    [Header("UI – Debug")]
    public TextMeshProUGUI textoDebug;

    [Header("UI – Barra de progreso")]
    public Image barraProgreso;

    [Header("UI – Paneles")]
    public GameObject panelCompletado;

    [Header("UI – Panel Principal del tutorial")]
    public GameObject panelPrincipal;

    [Header("UI – Panel Tutorial Completo")]
    public GameObject panelTutorialCompleto;
    public Image imagenFondo;              // tu PNG TutorialFinalizado
    public GameObject botonReiniciar;      // tu PNG botón reiniciar
    public GameObject botonVolverMenu;     // tu PNG botón volver al menú
    public string nombreEscenaMenu = "Menu"; // nombre exacto de tu escena menú

    [Header("Configuración del Tutorial")]
    public float tiempoRequerido = 3f;
    public float velocidadDecaimiento = 1.5f;
    public float distanciaCanvas = 2f;
    public float alturaCanvas = 0.1f;

    [Header("Umbrales de detección")]
    public float umbralGradosCabeza = 20f;
    public float umbralGradosAbajo = 25f;   // más fácil bajar
    public float umbralJoystick = 0.2f;

    //Agregar estas variables privadas 
    private float pitchInicial = 0f;
    private float yawInicial = 0f;
    private Vector3 forwardInicialProyectado; // referencia horizontal al inicio
    private bool calibrado = false;

    // PASO TUTORIAL — ahora usa Sprite en vez de string instruccion

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
        public Sprite imagenPaso;    //  tu PNG de diseno para este paso
        public TipoMovimiento movimiento;
    }

    [Header("Pasos del Tutorial")]
    public List<PasoTutorial> pasos = new List<PasoTutorial>();

   
    // PRIVADOS

    private int pasoActual = 0;
    private float tiempoAcumulado = 0f;
    private bool tutorialActivo = true;
    private bool esperandoReset = false;
    private RectTransform imagenRect;


    // INICIALIZACIN
 
    void OnEnable() { descargaAction.action?.Enable(); }
    void OnDisable() { descargaAction.action?.Disable(); }

    void Start()
    {
        if (controlVuelo == null)
            controlVuelo = GetComponent<VueloPalomaGaze>();
        if (camaraHead == null)
            camaraHead = Camera.main.transform;
        if (imagenInstruccion != null)
            imagenRect = imagenInstruccion.GetComponent<RectTransform>();

        // Espera un frame para que la cámara esté lista antes de calibrar
        StartCoroutine(CalibrарConDelay());

        if (pasos == null || pasos.Count == 0)
            InicializarPasosPorDefecto();

        MostrarPasoActual();

        if (panelTutorialCompleto != null) panelTutorialCompleto.SetActive(false);
        if (panelCompletado != null) panelCompletado.SetActive(false);
    }

    IEnumerator CalibrарConDelay()
    {
        yield return new WaitForSeconds(0.5f); // espera medio segundo
        CalibrarPosicionInicial();
        MostrarPasoActual();
    }

    void Update()
    {
        if (!tutorialActivo) return;

        PosicionarCanvas();
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

    // Agregar este mtodo 
    void CalibrarPosicionInicial()
    {
        if (camaraHead == null) return;

        Vector3 rot = camaraHead.localEulerAngles;
        pitchInicial = rot.x > 180 ? rot.x - 360 : rot.x;
        yawInicial = rot.y > 180 ? rot.y - 360 : rot.y;

        // Guarda el forward proyectado en el plano horizontal
        // como referencia neutral para arriba/abajo
        forwardInicialProyectado = Vector3.ProjectOnPlane(
            camaraHead.forward,
            Vector3.up
        ).normalized;

        calibrado = true;
        Debug.Log($"Calibrado — Pitch:{pitchInicial:F1} Yaw:{yawInicial:F1} Forward:{forwardInicialProyectado}");
    }


    // MOSTRAR PASO — ahora swapea imagen

    void MostrarPasoActual()
    {
        if (pasoActual >= pasos.Count) return;
        var p = pasos[pasoActual];

        // Textos que se mantienen
        if (textoTitulo != null) textoTitulo.text = p.titulo;
        if (textoSubtitulo != null) textoSubtitulo.text = p.subtitulo;
        if (textoPasoNumero != null) textoPasoNumero.text = $"Paso {pasoActual + 1} / {pasos.Count}";

        // Swap de imagen de instruccin
        if (imagenInstruccion != null && p.imagenPaso != null)
        {
            imagenInstruccion.sprite = p.imagenPaso;
            imagenInstruccion.enabled = true;

            // Ajusta el tamaño del RectTransform al sprite automticamente
            if (imagenRect != null)
                imagenRect.sizeDelta = tamañoNormal;
        }

        ActualizarBarra(0f);
        ActualizarContador(0f);
    }

   
    // COMPLETAR PASO
    
    IEnumerator CompletarPaso()
    {
        esperandoReset = true;
        if (panelCompletado != null) panelCompletado.SetActive(true);
        ActualizarBarra(1f);
        yield return new WaitForSeconds(1.2f);
        if (panelCompletado != null) panelCompletado.SetActive(false);

        pasoActual++;
        tiempoAcumulado = 0f;

        if (pasoActual >= pasos.Count)
            FinalizarTutorial();
        else
        {
            MostrarPasoActual();
            esperandoReset = false;
        }
    }

    void CentrarPanelFinal()
    {
        if (panelTutorialCompleto == null || camaraHead == null) return;

        // Posición fija frente a la cámara en el momento de finalizar
        Vector3 forward = camaraHead.forward;

        // Proyecta el forward en el plano horizontal
        // para que el panel siempre quede perfectamente vertical (2D)
        forward.y = 0f;
        forward.Normalize();

        // Posiciona el panel frente al usuario
        Vector3 posicion = camaraHead.position + forward * distanciaCanvas;

        // Altura fija a nivel de los ojos
        posicion.y = camaraHead.position.y;

        panelTutorialCompleto.transform.position = posicion;

        // Rota para mirar al usuario — siempre perfectamente vertical
        panelTutorialCompleto.transform.rotation = Quaternion.LookRotation(forward);
    }

    void FinalizarTutorial()
    {
        tutorialActivo = false;

        // Oculta solo el contenido del tutorial
        if (panelPrincipal != null)
            panelPrincipal.SetActive(false);

        // Activa el panel primero para poder posicionarlo
        if (panelTutorialCompleto != null)
            panelTutorialCompleto.SetActive(true);

        // Centra el panel frente al usuario ← nuevo
        CentrarPanelFinal();

        // Activa los botones
        if (botonReiniciar != null) botonReiniciar.SetActive(true);
        if (botonVolverMenu != null) botonVolverMenu.SetActive(true);

        // Congela la escena
        Time.timeScale = 0f;
    }

    public void OnReiniciarTutorial()
    {
        // Descongela primero
        Time.timeScale = 1f;

        // Oculta el panel
        if (panelTutorialCompleto != null)
            panelTutorialCompleto.SetActive(false);

        // Reinicia
        ReiniciarTutorial();
    }

    // ── Boton Volver al Menu ─────────────────────────────────────
    public void OnVolverMenu()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetString("EscenaDestino", nombreEscenaMenu);
        PlayerPrefs.Save();
        SceneManager.LoadScene("SceneCarga");
    }



    // PASOS POR DEFECTO (fallback sin sprites)

    void InicializarPasosPorDefecto()
    {
        pasos = new List<PasoTutorial>
        {
            new PasoTutorial { titulo="¡Bienvenido al vuelo!",  subtitulo="La paloma gira siguiendo tu mirada",     movimiento=TipoMovimiento.MirarIzquierda },
            new PasoTutorial { titulo="Gira al otro lado",       subtitulo="Inclínate para doblar en vuelo",         movimiento=TipoMovimiento.MirarDerecha },
            new PasoTutorial { titulo="Sube de altitud",         subtitulo="Controla la altura con tu cabeza",       movimiento=TipoMovimiento.MirarArriba },
            new PasoTutorial { titulo="Baja en picada",          subtitulo="¡Cuidado con el suelo!",                 movimiento=TipoMovimiento.MirarAbajo },
            new PasoTutorial { titulo="¡Acelera!",               subtitulo="Cuanto más empujes, más rápido vas",     movimiento=TipoMovimiento.JoystickAdelante },
            new PasoTutorial { titulo="Frena el vuelo",          subtitulo="Reduce velocidad para maniobrar mejor",  movimiento=TipoMovimiento.FrenarGatilloIzquierdo },
            new PasoTutorial { titulo="¡Descarga!",              subtitulo="Úsala en el momento justo",              movimiento=TipoMovimiento.DescargaGatilloDerecho },
            new PasoTutorial { titulo="¡Combina todo!",          subtitulo="Esquiva edificios como un experto",      movimiento=TipoMovimiento.Combinado_GiroYAvance }
        };
    }


    // DETECCIN (igual que antes)

    bool VerificarMovimiento(TipoMovimiento tipo)
    {
        if (camaraHead == null || !calibrado) return false;

        // ── Yaw relativo (izquierda/derecha) ──────────────────────
        Vector3 rot = camaraHead.localEulerAngles;
        float yawAbs = rot.y > 180 ? rot.y - 360 : rot.y;
        float yaw = yawAbs - yawInicial;
        if (yaw > 180f) yaw -= 360f;
        if (yaw < -180f) yaw += 360f;

        // ── Pitch con Dot Product (arriba/abajo) ──────────────────
        // Mide cuánto apunta la cámara hacia arriba o abajo
        // respecto al plano horizontal — no le afecta el gimbal lock
        float pitchDot = Vector3.Dot(camaraHead.forward, Vector3.up);
        // pitchDot: -1 = mirando al suelo, 0 = horizontal, +1 = al cielo

        // Convertir a grados aproximados (-90 a +90)
        float pitchGrados = Mathf.Asin(Mathf.Clamp(pitchDot, -1f, 1f)) * Mathf.Rad2Deg;
        // pitchGrados: negativo = mirando abajo, positivo = mirando arriba

        // Pitch neutral al inicio (para hacer relativo)
        float pitchNeutral = Mathf.Asin(
            Mathf.Clamp(
                Vector3.Dot(forwardInicialProyectado.normalized, Vector3.up),
                -1f, 1f
            )
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
            case TipoMovimiento.MirarIzquierda:
                return yaw < -umbralGradosCabeza;

            case TipoMovimiento.MirarDerecha:
                return yaw > umbralGradosCabeza;

            // Subir: cámara apunta más arriba que la neutral
            case TipoMovimiento.MirarArriba:
                return pitchRelativo > umbralGradosCabeza;

            // Bajar: cámara apunta más abajo que la neutral
            case TipoMovimiento.MirarAbajo:
                return pitchRelativo < -umbralGradosAbajo;

            case TipoMovimiento.JoystickAdelante:
                return joystick > umbralJoystick;

            case TipoMovimiento.FrenarGatilloIzquierdo:
                return freno > umbralJoystick;

            case TipoMovimiento.DescargaGatilloDerecho:
                return descarga > umbralJoystick;

            case TipoMovimiento.Combinado_GiroYAvance:
                return yaw < -umbralGradosCabeza && joystick > umbralJoystick;
        }
        return false;
    }


    // UI HELPERS

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
        if (tiempo <= 0f) textoContador.text = $"Mantén {tiempoRequerido:0}s";
        else if (r <= 0f) textoContador.text = "¡Perfecto!";
        else textoContador.text = $"Mantén... {r:0.0}s";
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
            $"Pitch (dot): {pitchGrados:F1}°\n" +
            $"Yaw rel: {yaw:F1}°\n" +
            $"Umbral: ±{umbralGradosCabeza}°  Abajo:{umbralGradosAbajo}°\n" +
            $"DETECTADO: {(detectado ? "SI ✓" : "no")}  t:{tiempoAcumulado:F1}/{tiempoRequerido}";
    }

    void PosicionarCanvas()
    {
        if (canvasTutorial == null || camaraHead == null) return;
        Vector3 pos = camaraHead.position + camaraHead.forward * distanciaCanvas + Vector3.up * alturaCanvas;
        canvasTutorial.transform.position = pos;
        canvasTutorial.transform.LookAt(camaraHead.position);
        canvasTutorial.transform.Rotate(0, 180f, 0);
    }

    
    // API PuBLICA
  
    public void SaltarTutorial() { StopAllCoroutines(); FinalizarTutorial(); }

    public void ReiniciarTutorial()
    {
        StopAllCoroutines();
        pasoActual = 0; tiempoAcumulado = 0f;
        esperandoReset = false; tutorialActivo = true;

        // Reactiva el contenido del tutorial
        if (panelPrincipal != null) panelPrincipal.SetActive(true);
        if (panelTutorialCompleto != null) panelTutorialCompleto.SetActive(false);
        if (panelCompletado != null) panelCompletado.SetActive(false);
        if (botonReiniciar != null) botonReiniciar.SetActive(false);
        if (botonVolverMenu != null) botonVolverMenu.SetActive(false);

        StartCoroutine(CalibrарConDelay());
        MostrarPasoActual();
    }

    public void IrAPaso(int i)
    {
        if (i < 0 || i >= pasos.Count) return;
        StopAllCoroutines();
        pasoActual = i; tiempoAcumulado = 0f;
        esperandoReset = false; tutorialActivo = true;
        MostrarPasoActual();
    }
}