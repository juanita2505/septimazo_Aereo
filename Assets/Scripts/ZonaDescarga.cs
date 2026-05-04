using UnityEngine;

public class ZonaDescarga : MonoBehaviour
{
    [Header("UI")]
    public GameObject indicadorUI;
    public GameObject overlayAmarillo;

    [Header("Debug")]
    public int nivelZona = 1;

    private bool enZona = false;
    private CanvasGroup canvasGroup;

    void Start()
    {
        //Detectar nivel según el nombre del objeto
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
        if (canvasGroup == null) return;

        float objetivo = enZona ? 0.3f : 0f;

        canvasGroup.alpha = Mathf.Lerp(
            canvasGroup.alpha,
            objetivo,
            Time.deltaTime * 5f
        );
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
}