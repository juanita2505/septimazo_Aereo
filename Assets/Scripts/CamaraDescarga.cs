using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CamaraDescarga : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidadCaida = 5f;
    public float distanciaFinal = 0.2f;
    public float alturaFinalFallback = 1f;

    private Camera cam;
    private bool activa = false;
    private bool finalizado = false;

    private ZonaDescarga zonaOrigen;
    private Transform objetivo;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.enabled = false;
    }

    public void Activar(Vector3 posicionInicial, Transform destino, ZonaDescarga zona)
    {
        zonaOrigen = zona;
        objetivo = destino;
        finalizado = false;

        transform.position = posicionInicial;

        if (objetivo != null)
        {
            transform.LookAt(objetivo);
        }
        else
        {
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        activa = true;
        cam.enabled = true;
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
            {
                Terminar();
            }
        }
        else
        {
            transform.position += Vector3.down * velocidadCaida * Time.deltaTime;

            if (transform.position.y <= alturaFinalFallback)
            {
                Terminar();
            }
        }
    }

    private void Terminar()
    {
        if (finalizado) return;
        finalizado = true;
        activa = false;

        cam.enabled = false;

        if (zonaOrigen != null)
            zonaOrigen.FinalizarAnimacionDescarga();
    }
}