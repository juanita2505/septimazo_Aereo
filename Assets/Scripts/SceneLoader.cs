using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void CargarEscena(string nombreEscena)
    {
        PlayerPrefs.SetString("EscenaDestino", nombreEscena);
        PlayerPrefs.Save();
        SceneManager.LoadScene("SceneCarga");
    }

    public void SalirJuego()
    {
        Application.Quit();
        Debug.Log("Saliendo del juego");
    }
}