using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PanelInicio : MonoBehaviour
{
    public static PanelInicio Instance;

    [Header("Paneles")]
    public GameObject panelInicio;
    public GameObject panelFinal;

    [Header("Score final")]
    public TextMeshProUGUI textoScoreFinal;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Time.timeScale = 0f;
        panelInicio.SetActive(true);
        if (panelFinal != null) panelFinal.SetActive(false);
    }

    public void EmpezarJuego()
    {
        Time.timeScale = 1f;
        panelInicio.SetActive(false);
    }

    public void MostrarPanelFinal()
    {
        Time.timeScale = 0f;
        panelFinal.SetActive(true);

        // Muestra el score final
        if (textoScoreFinal != null && SistemaPuntaje.Instance != null)
            textoScoreFinal.text = $"{SistemaPuntaje.Instance.GetPuntaje()}/30";
    }
}