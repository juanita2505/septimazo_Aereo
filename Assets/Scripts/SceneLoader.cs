using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void CargarEscena(string nombreEscena)
    {
        Time.timeScale = 1f;  //  resetea antes de cargar
        PlayerPrefs.SetString("EscenaDestino", nombreEscena);
        PlayerPrefs.Save();
        SceneManager.LoadScene("SceneCarga");
    }

    public void ReiniciarEscena()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SalirJuego()
    {
        Application.Quit();
        Debug.Log("Saliendo del juego");
    }
}