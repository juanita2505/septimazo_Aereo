using UnityEngine;

public class DetectorZona : MonoBehaviour
{
    public ZonaDescargaDiana zonaDescarga;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Pico")) return;
        zonaDescarga.EntrarCollider(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Pico")) return;
        zonaDescarga.SalirCollider(other);
    }
}