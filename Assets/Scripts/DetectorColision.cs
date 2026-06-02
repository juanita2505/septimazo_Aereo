using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Adjuntar al GameObject "Pico" (CapsuleCollider Is Trigger = true).
/// Al chocar muestra un menú con opciones Reiniciar y Volver al Inicio.
/// </summary>
public class DetectorColision : MonoBehaviour
{
    [Header("Capas que causan muerte")]
    [Tooltip("Selecciona las capas de edificios, suelo, obstaculos")]
    public LayerMask capasObstaculos = ~0;

    [Header("Canvas del menu de muerte (World Space)")]
    [Tooltip("El Canvas completo del menu que aparece al chocar")]
    public Canvas canvasMuerte;

    [Tooltip("Distancia frente al jugador donde aparece el menu")]
    public float distanciaMenu = 2f;
    public float alturaMenu = 0f;

    [Header("Escenas")]
    public string nombreEscenaJuego = "SampleScene";
    public string nombreEscenaMenu = "Menu";

    [Header("Efectos (opcional)")]
    public GameObject efectoMuerte;
    public AudioClip sonidoChoque;

    // ─── Privados ─────────────────────────────────────────────────────────────────

    private bool chocando = false;
    private AudioSource audioSource;
    private VueloPalomaGaze controlVuelo;
    private Transform camaraHead;

    // ═════════════════════════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ═════════════════════════════════════════════════════════════════════════════

    void Start()
    {
        controlVuelo = GetComponentInParent<VueloPalomaGaze>();
        camaraHead = Camera.main.transform;

        if (sonidoChoque != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.playOnAwake = false;
        }

        // Empieza oculto
        if (canvasMuerte != null)
            canvasMuerte.gameObject.SetActive(false);
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // DETECCION
    // ═════════════════════════════════════════════════════════════════════════════

    void OnTriggerEnter(Collider otro)
    {
        if (chocando) return;
        if ((capasObstaculos.value & (1 << otro.gameObject.layer)) == 0) return;
        if (otro.isTrigger) return;

        Debug.Log($"Choque con: {otro.gameObject.name}");
        StartCoroutine(MostrarMenuMuerte());
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // SECUENCIA DE MUERTE
    // ═════════════════════════════════════════════════════════════════════════════

    IEnumerator MostrarMenuMuerte()
    {
        chocando = true;

        // 1. Detener el vuelo
        if (controlVuelo != null)
            controlVuelo.enabled = false;

        // 2. Sonido
        if (audioSource != null && sonidoChoque != null)
            audioSource.PlayOneShot(sonidoChoque);

        // 3. Efecto visual
        if (efectoMuerte != null)
            efectoMuerte.SetActive(true);

        // 4. Pequeña pausa dramática antes de mostrar el menu
        yield return new WaitForSeconds(0.6f);

        // 5. Posicionar y mostrar el menu
        if (canvasMuerte != null)
        {
            PosicionarMenuFrenteAlJugador();
            canvasMuerte.gameObject.SetActive(true);
        }
    }

    void PosicionarMenuFrenteAlJugador()
    {
        if (canvasMuerte == null || camaraHead == null) return;

        Vector3 forward = camaraHead.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.01f)
            forward = camaraHead.forward;

        forward.Normalize();

        Vector3 pos =
            camaraHead.position +
            forward * distanciaMenu +
            Vector3.up * alturaMenu;

        canvasMuerte.transform.position = pos;

        // Hace que el menú mire hacia el jugador
        canvasMuerte.transform.LookAt(camaraHead);
        canvasMuerte.transform.Rotate(0f, 180f, 0f);
    }

    // ═════════════════════════════════════════════════════════════════════════════
    // BOTONES — conectar en el Inspector con OnClick
    // ═════════════════════════════════════════════════════════════════════════════

    /// <summary>Boton "Reiniciar" — recarga la escena de juego</summary>
    public void Reiniciar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    /// <summary>Boton "Volver al Inicio" — carga el menu principal</summary>
    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nombreEscenaMenu);
    }
}