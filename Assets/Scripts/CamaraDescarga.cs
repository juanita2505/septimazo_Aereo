using UnityEngine;

public class CamaraDescarga : MonoBehaviour
{
    public float velocidadCaida = 5f;
    public float alturaFinal = 1f;

    private Camera cam;
    private bool activa = false;

    void Start()
    {
        cam = GetComponent<Camera>();

        if (cam != null)
            cam.enabled = false;
    }

    public void Activar(Vector3 posicion, Quaternion rotacion)
    {
        transform.position = posicion;
        transform.rotation = rotacion;

        activa = true;

        if (cam != null)
            cam.enabled = true;
    }

    void Update()
    {
        if (!activa) return;

        transform.position += Vector3.down * velocidadCaida * Time.deltaTime;

        if (transform.position.y <= alturaFinal)
        {
            Desactivar();
        }
    }

    void Desactivar()
    {
        activa = false;

        if (cam != null)
            cam.enabled = false;
    }
}