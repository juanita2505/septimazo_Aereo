using UnityEngine;

public class ZonaDescarga : MonoBehaviour
{
    [Header("UI")]
    public GameObject indicadorUI;
    public GameObject overlayAmarillo;

    [Header("Debug")]
    public int nivelZona = 1;

    [Header("Input")]
    public KeyCode teclaDescarga = KeyCode.X;

    private bool enZona = false;
    private CanvasGroup canvasGroup;

    [Header("Zonas")]
    public GameObject zonaNivel1;
    public GameObject zonaNivel2;
    public GameObject zonaNivel3;

    public CamaraDescarga camaraDescarga;

    void Start()
    {
        //Detectar nivel segun el nombre del objeto
        if (gameObject.name.ToLower().Contains("zonauno"))
        {
            nivelZona = 1;
        }
        else if (gameObject.name.ToLower().Contains("zonados"))
        {
            nivelZona = 2;
        }
        else if (gameObject.name.ToLower().Contains("zonatres"))
        {
            nivelZona = 3;
        }

        Debug.Log("Esta zona es nivel: " + nivelZona);

        if (indicadorUI != null)
            indicadorUI.SetActive(false);

        if (overlayAmarillo != null)
        {
            canvasGroup = overlayAmarillo.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = overlayAmarillo.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0f;
        }
    }

    void Update()
    {
        // SOLO el fade depende del canvasGroup
        if (canvasGroup != null)
        {
            float objetivo = enZona ? 0.3f : 0f;

            canvasGroup.alpha = Mathf.Lerp(
                canvasGroup.alpha,
                objetivo,
                Time.deltaTime * 5f
            );
        }

        // INPUT SIEMPRE ACTIVO
        if (enZona && Input.GetKeyDown(teclaDescarga))
        {
            Debug.Log("DESCARGA ACTIVADA en nivel: " + nivelZona);
            EjecutarDescarga();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pico"))
        {
            Debug.Log("Entraste a zona nivel: " + nivelZona);

            enZona = true;

            if (indicadorUI != null)
                indicadorUI.SetActive(true);

            if (overlayAmarillo != null)
                overlayAmarillo.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Pico"))
        {
            Debug.Log("Saliste de zona nivel: " + nivelZona);

            enZona = false;

            if (indicadorUI != null)
                indicadorUI.SetActive(false);

        }
    }

    void EjecutarDescarga()
    {
        Transform pico = GameObject.FindGameObjectWithTag("Pico").transform;
        Debug.Log("Hiciste la descarga correctamente");

        if (nivelZona == 1)
        {
            Debug.Log("Desbloqueaste nivel 2");
            zonaNivel1.SetActive(false);
            zonaNivel2.SetActive(true);
            // Posición inicial (desde donde "sale")
            Vector3 inicio = pico.position;

            // Rotación mirando hacia abajo
            Quaternion rot = Quaternion.Euler(90f, pico.eulerAngles.y, 0f);

            // Activar cámara
            camaraDescarga.Activar(inicio, rot);
        }
        else if (nivelZona == 2)
        {
            Debug.Log("Desbloqueaste nivel 3");

            zonaNivel2.SetActive(false);
            zonaNivel3.SetActive(true);
        }
        else if (nivelZona == 3)
        {
            Debug.Log("GANASTE");

            zonaNivel3.SetActive(false);

            enZona = false;

            if (indicadorUI != null)
                indicadorUI.SetActive(false);

            if (overlayAmarillo != null)
                overlayAmarillo.SetActive(false);
        }
    }

}