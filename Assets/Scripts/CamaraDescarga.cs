using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class CamaraDescarga : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidadCaida = 5f;
    public float distanciaFinal = 0.2f;
    public float alturaFinalFallback = 1f;

    [Header("UI")]
    public Image imagenBorde;
    public RawImage imagenCamara;

    private Camera cam;
    private bool activa = false;
    private bool finalizado = false;
    private ZonaDescargaDiana zonaOrigen;
    private Transform objetivo;
    private AudioSource audioSource;

    [Header("Sonido")]
    public AudioClip[] sonidosDescarga = new AudioClip[3];
    public float volumenSonido = 6f;

    private int ultimoSonidoIndex = -1;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.enabled = false;

        if (imagenBorde != null) imagenBorde.enabled = false;
        if (imagenCamara != null) imagenCamara.enabled = false;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
    }

    public void Activar(Vector3 posicionInicial, Transform destino, ZonaDescargaDiana zona)
    {
        zonaOrigen = zona;
        objetivo = destino;
        finalizado = false;

        transform.position = posicionInicial;

        if (objetivo != null)
            transform.LookAt(objetivo);
        else
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        activa = true;
        cam.enabled = true;

        if (imagenBorde != null) imagenBorde.enabled = true;
        if (imagenCamara != null) imagenCamara.enabled = true;
    }

    void Update()
    {
        if (!activa) return;

        if (objetivo != null)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                objetivo.position,
                velocidadCaida * Time.deltaTime
            );

            transform.LookAt(objetivo);

            if (Vector3.Distance(transform.position, objetivo.position) <= distanciaFinal)
                Terminar();
        }
        else
        {
            transform.position += Vector3.down * velocidadCaida * Time.deltaTime;

            if (transform.position.y <= alturaFinalFallback)
                Terminar();
        }
    }

    private void Terminar()
    {
        if (finalizado) return;
        finalizado = true;
        activa = false;
        cam.enabled = false;

        if (imagenBorde != null) imagenBorde.enabled = false;
        if (imagenCamara != null) imagenCamara.enabled = false;

        // Reproduce sonido aleatorio sin repetir el mismo dos veces seguidas
        if (audioSource != null && sonidosDescarga.Length > 0)
        {
            int index;
            do { index = Random.Range(0, sonidosDescarga.Length); }
            while (index == ultimoSonidoIndex && sonidosDescarga.Length > 1);

            ultimoSonidoIndex = index;

            if (sonidosDescarga[index] != null)
                audioSource.PlayOneShot(sonidosDescarga[index], volumenSonido);
        }

        if (zonaOrigen != null)
            zonaOrigen.FinalizarAnimacionDescarga();
    }
}