using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PantallaCarga : MonoBehaviour
{
    [Header("UI")]
    public Image barraProgreso;
    public TextMeshProUGUI textoPorcentaje;
    public TextMeshProUGUI textoCargando;

    [Header("Configuración")]
    public float tiempoTotal = 5f;
    public float velocidadPuntos = 0.4f;
    public float tiempoMostrarListo = 1.5f; //  cunto se ve "¡A volar!"

    private Coroutine coroutinaTexto; //  referencia para detenerla

    void Start()
    {
        string escenaDestino = PlayerPrefs.GetString("EscenaDestino", "Menu");
        coroutinaTexto = StartCoroutine(AnimarCargando()); //  guarda referencia
        StartCoroutine(Cargar(escenaDestino));
    }

    IEnumerator AnimarCargando()
    {
        string[] estados = {
            "Preparando el vuelo",
            "Preparando el vuelo.",
            "Preparando el vuelo..",
            "Preparando el vuelo..."
        };
        int i = 0;

        while (true)
        {
            if (textoCargando != null)
                textoCargando.text = estados[i % estados.Length];
            i++;
            yield return new WaitForSeconds(velocidadPuntos);
        }
    }

    IEnumerator Cargar(string nombreEscena)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(nombreEscena);
        op.allowSceneActivation = false;

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < tiempoTotal)
        {
            tiempoTranscurrido += Time.unscaledDeltaTime; // <- cambiado

            float progresoVisual = Mathf.Clamp01(tiempoTranscurrido / tiempoTotal);

            if (barraProgreso != null)
                barraProgreso.fillAmount = progresoVisual;
            if (textoPorcentaje != null)
                textoPorcentaje.text = Mathf.RoundToInt(progresoVisual * 100f) + "%";

            yield return null;
        }

        if (barraProgreso != null) barraProgreso.fillAmount = 1f;
        if (textoPorcentaje != null) textoPorcentaje.text = "100%";

        if (coroutinaTexto != null) StopCoroutine(coroutinaTexto);
        if (textoCargando != null) textoCargando.text = "A volar!";

        yield return new WaitForSecondsRealtime(tiempoMostrarListo); // <- cambiado
        op.allowSceneActivation = true;
    }
}