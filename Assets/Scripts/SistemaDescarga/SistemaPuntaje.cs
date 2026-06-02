using UnityEngine;
using TMPro;

public class SistemaPuntaje : MonoBehaviour
{
    public static SistemaPuntaje Instance;

    [Header("UI")]
    public TextMeshProUGUI textoPuntaje;

    [Header("Puntos por zona")]
    public int puntosRojo = 5;
    public int puntosAmarillo = 7;
    public int puntosVerde = 10;

    private int puntajeTotal = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ActualizarTexto();
    }

    public void AgregarPuntos(string zona)
    {
        switch (zona)
        {
            case "Roja": puntajeTotal += puntosRojo; break;
            case "Amarilla": puntajeTotal += puntosAmarillo; break;
            case "Verde": puntajeTotal += puntosVerde; break;
        }
        ActualizarTexto();
        Debug.Log($"Puntos agregados zona {zona} — Total: {puntajeTotal}");
    }

    void ActualizarTexto()
    {
        if (textoPuntaje != null)
            textoPuntaje.text = $"Score: {puntajeTotal}/30";
    }

    public int GetPuntaje() => puntajeTotal;
}