using UnityEngine;

public class FlechaFlotante : MonoBehaviour
{
    public float amplitud = 3f;
    public float velocidad = 2f;

    private Vector3 posicionInicial;
    public float velocidadRotacion = 30f;


    void Start()
    {
        posicionInicial = transform.position;
    }

    void Update()
    {
        transform.position =
            posicionInicial +
            Vector3.up * Mathf.Sin(Time.time * velocidad) * amplitud;

        transform.Rotate(
            Vector3.up,
            velocidadRotacion * Time.deltaTime,
            Space.World
        );
    }
}