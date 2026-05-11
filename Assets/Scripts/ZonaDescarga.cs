using UnityEngine;

public class ZonaDescarga : MonoBehaviour
{
    [Header("UI")]
    public GameObject indicadorUI;
    public GameObject overlayAmarillo;
    public GameObject ventanaCamara;

    [Header("Niveles")]
    public GameObject zonaNivel1;
    public GameObject zonaNivel2;
    public GameObject zonaNivel3;

    [Header("Camara")]
    public CamaraDescarga camaraDescarga;

    [Header("Destino")]
    public Transform destinoCaida;

    [Header("Input")]
    public KeyCode teclaDescarga = KeyCode.X;

    [Header("Debug")]
    public int nivelZona = 1;

    private bool enZona = false;
    private CanvasGroup canvasGroup;

    void Start()
    {
        if (gameObject.name.ToLower().Contains("zonauno")) nivelZona = 1;
        else if (gameObject.name.ToLower().Contains("zonados")) nivelZona = 2;
        else if (gameObject.name.ToLower().Contains("zonatres")) nivelZona = 3;

        if (indicadorUI != null)
            indicadorUI.SetActive(false);

        if (overlayAmarillo != null)
        {
            canvasGroup = overlayAmarillo.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = overlayAmarillo.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
            overlayAmarillo.SetActive(false);
        }

        if (ventanaCamara != null)
            ventanaCamara.SetActive(false);
    }

    void Update()
    {
        if (canvasGroup != null)
        {
            float objetivo = enZona ? 0.3f : 0f;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, objetivo, Time.deltaTime * 5f);
        }

        if (enZona && Input.GetKeyDown(teclaDescarga))
        {
            EjecutarDescarga();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Pico")) return;

        enZona = true;

        if (indicadorUI != null)
            indicadorUI.SetActive(true);

        if (overlayAmarillo != null)
            overlayAmarillo.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Pico")) return;

        enZona = false;

        if (indicadorUI != null)
            indicadorUI.SetActive(false);

        if (overlayAmarillo != null)
            overlayAmarillo.SetActive(false);
    }

    void EjecutarDescarga()
    {
        Debug.Log("DESCARGA ACTIVADA");

        enZona = false;

        if (indicadorUI != null)
            indicadorUI.SetActive(false);

        if (overlayAmarillo != null)
            overlayAmarillo.SetActive(false);

        if (ventanaCamara != null)
            ventanaCamara.SetActive(true);

        GameObject picoObj = GameObject.FindGameObjectWithTag("Pico");
        if (picoObj == null)
        {
            Debug.LogError("No se encontró el Pico");
            return;
        }

        if (camaraDescarga == null)
        {
            Debug.LogError("CamaraDescarga no asignada");
            return;
        }

        if (destinoCaida == null)
        {
            Debug.LogError("No asignaste destinoCaida en el Inspector para esta zona.");
            return;
        }

        camaraDescarga.Activar(
            picoObj.transform.position,
            destinoCaida,
            this
        );
    }

    public void FinalizarAnimacionDescarga()
    {
        if (ventanaCamara != null)
            ventanaCamara.SetActive(false);

        if (nivelZona == 1)
        {
            Debug.Log("Desbloqueaste nivel 2");
            if (zonaNivel1 != null) zonaNivel1.SetActive(false);
            if (zonaNivel2 != null) zonaNivel2.SetActive(true);
        }
        else if (nivelZona == 2)
        {
            Debug.Log("Desbloqueaste nivel 3");
            if (zonaNivel2 != null) zonaNivel2.SetActive(false);
            if (zonaNivel3 != null) zonaNivel3.SetActive(true);
        }
        else if (nivelZona == 3)
        {
            Debug.Log("GANASTE");
            if (zonaNivel3 != null) zonaNivel3.SetActive(false);
        }
    }
}