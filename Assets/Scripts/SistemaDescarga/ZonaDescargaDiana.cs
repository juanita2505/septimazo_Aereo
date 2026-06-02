using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ZonaDescargaDiana : MonoBehaviour
{
    [Header("Overlays de color")]
    public GameObject overlayRojo;
    public GameObject overlayAmarillo;
    public GameObject overlayVerde;

    [Header("Radios de la diana")]
    public float radioVerde = 3f;
    public float radioAmarillo = 7f;
    public float radioRojo = 13f;

    [Header("Niveles")]
    public GameObject zonaNivel1;
    public GameObject zonaNivel2;
    public GameObject zonaNivel3;

    [Header("Camara")]
    public CamaraDescarga camaraDescarga;

    [Header("Ventana Camara")]
    public GameObject ventanaCamara;

    [Header("Destino")]
    public Transform destinoCaida;

    [Header("Input VR")]
    public InputActionProperty descargaAction;

    [Header("Debug")]
    public int nivelZona = 1;

    private bool enZona = false;
    private bool descargaEnProceso = false;
    private string zonaActual = "";

    private CanvasGroup cgRojo;
    private CanvasGroup cgAmarillo;
    private CanvasGroup cgVerde;

    private bool dentroDelCollider = false; //  nueva variable privada

    void OnEnable() { descargaAction.action?.Enable(); }
    void OnDisable() { descargaAction.action?.Disable(); }

    void Start()
    {
        descargaAction.action?.Enable();

        if (gameObject.name.ToLower().Contains("zonauno")) nivelZona = 1;
        else if (gameObject.name.ToLower().Contains("zonados")) nivelZona = 2;
        else if (gameObject.name.ToLower().Contains("zonatres")) nivelZona = 3;

        cgRojo = ConfigurarOverlay(overlayRojo);
        cgAmarillo = ConfigurarOverlay(overlayAmarillo);
        cgVerde = ConfigurarOverlay(overlayVerde);

        if (ventanaCamara != null) ventanaCamara.SetActive(false);
    }

    CanvasGroup ConfigurarOverlay(GameObject overlay)
    {
        if (overlay == null) return null;
        var cg = overlay.GetComponent<CanvasGroup>();
        if (cg == null) cg = overlay.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        overlay.SetActive(false);
        return cg;
    }

    void Update()
    {
        if (dentroDelCollider && !descargaEnProceso)
        {
            GameObject pico = GameObject.FindGameObjectWithTag("Pico");
            if (pico != null)
            {
                float alturaMaxima = transform.position.y + radioRojo;

                if (pico.transform.position.y > alturaMaxima)
                {
                    // Subio demasiado, desactiva
                    enZona = false;
                    zonaActual = "";
                }
                else
                {
                    // Altura valida, activa
                    enZona = true;
                    ActualizarZonaPorPosicion();
                }
            }
        }

        ActualizarOverlay(cgRojo, overlayRojo, zonaActual == "Roja");
        ActualizarOverlay(cgAmarillo, overlayAmarillo, zonaActual == "Amarilla");
        ActualizarOverlay(cgVerde, overlayVerde, zonaActual == "Verde");

        if (enZona && !descargaEnProceso)
        {
            if (descargaAction.action.WasPressedThisFrame())
                EjecutarDescarga();
        }
    }

    void ActualizarZonaPorPosicion()
    {
        GameObject pico = GameObject.FindGameObjectWithTag("Pico");
        if (pico == null) return;

        Vector3 centro = transform.position;
        Vector3 picoPosFlat = new Vector3(
            pico.transform.position.x,
            centro.y,
            pico.transform.position.z
        );
        float distancia = Vector3.Distance(picoPosFlat, centro);

        if (distancia <= radioVerde)
            zonaActual = "Verde";
        else if (distancia <= radioAmarillo)
            zonaActual = "Amarilla";
        else if (distancia <= radioRojo)
            zonaActual = "Roja";

        Debug.Log($"Distancia: {distancia:F1} Zona: {zonaActual}");
    }

    public void EntrarCollider(Collider other)
    {
        dentroDelCollider = true;
        float alturaMaxima = transform.position.y + radioRojo;
        if (other.transform.position.y > alturaMaxima) return;
        enZona = true;
        ActualizarZonaPorPosicion();
        Debug.Log($"Entro zona: {zonaActual}");
    }

    public void SalirCollider(Collider other)
    {
        dentroDelCollider = false;
        enZona = false;
        zonaActual = "";
        Debug.Log("Salio de la diana");
    }

    void ActualizarOverlay(CanvasGroup cg, GameObject overlay, bool visible)
    {
        if (cg == null || overlay == null) return;
        float objetivo = visible ? 0.5f : 0f;

        if (visible && !overlay.activeSelf) overlay.SetActive(true);

        cg.alpha = Mathf.Lerp(cg.alpha, objetivo, Time.deltaTime * 3f);

        if (!visible && cg.alpha < 0.01f && overlay.activeSelf)
            overlay.SetActive(false);
    }

    void EjecutarDescarga()
    {
        Debug.Log($"DESCARGA en zona: {zonaActual}");

        GameObject picoObj = GameObject.FindGameObjectWithTag("Pico");
        if (picoObj == null || camaraDescarga == null || destinoCaida == null) return;

        // Agrega puntos segun zona
        if (SistemaPuntaje.Instance != null)
            SistemaPuntaje.Instance.AgregarPuntos(zonaActual);

        descargaEnProceso = true;
        enZona = false;
        zonaActual = "";

        if (ventanaCamara != null) ventanaCamara.SetActive(true);
        camaraDescarga.Activar(picoObj.transform.position, destinoCaida, this);
    }

    public void FinalizarAnimacionDescarga()
    {
        descargaEnProceso = false;

        if (ventanaCamara != null) ventanaCamara.SetActive(false);

        if (nivelZona == 1)
        {
            if (zonaNivel1 != null) zonaNivel1.SetActive(false);
            if (zonaNivel2 != null) zonaNivel2.SetActive(true);
        }
        else if (nivelZona == 2)
        {
            if (zonaNivel2 != null) zonaNivel2.SetActive(false);
            if (zonaNivel3 != null) zonaNivel3.SetActive(true);
        }
        else if (nivelZona == 3)
        {
            if (zonaNivel3 != null) zonaNivel3.SetActive(false);

            // Muestra panel final
            if (PanelInicio.Instance != null)
                PanelInicio.Instance.MostrarPanelFinal();
        }
    }

    public void OnTriggerEnterExterno(Collider other)
    {
        if (!other.CompareTag("Pico")) return;
        enZona = true;
        ActualizarZonaPorPosicion();
    }

    public void OnTriggerExitExterno(Collider other)
    {
        if (!other.CompareTag("Pico")) return;
        enZona = false;
        zonaActual = "";
    }

    public string GetZonaActual() => zonaActual;
}