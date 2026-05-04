using UnityEngine;
using UnityEngine.InputSystem;

public class VueloPalomaGaze : MonoBehaviour
{
    [Header("Referencias")]
    public Transform camaraHead; // Arrastra la Main Camera aquí

    [Header("Configuración de Vuelo")]
    public float velocidadBase = 5f;
    public float velocidadMaxima = 30f;
    public float suavizadoGiro = 3f;
    public float zonaMuertaGrados = 5f; // Grados de inclinación ignorados

    [Header("Límites de la Media Esfera")]
    public float limiteGiroHorizontal = 60f; // Máximo ángulo de cuello para girar
    public float limiteGiroVertical = 45f;   // Máximo ángulo de cuello para subir/bajar

    [Header("Inputs")]
    public InputActionProperty acelerarAction;
    public InputActionProperty desacelerarAction;

    private float velocidadActual;
    private float yawAcumulado;

    [Header("Modo Testing PC")]
    public bool usarModoTesting = true;
    public float velocidadMovimientoTesting = 10f;
    public float sensibilidadMouse = 2f;

    private float rotacionX = 0f;
    private float rotacionY = 0f;

    void OnEnable()
    {
        if (acelerarAction.action != null) acelerarAction.action.Enable();
        if (desacelerarAction.action != null) desacelerarAction.action.Enable();
    }

    void Start()
    {
        velocidadActual = velocidadBase;
        yawAcumulado = transform.eulerAngles.y;
        if (GetComponent<Rigidbody>()) GetComponent<Rigidbody>().isKinematic = true;
        
        if (camaraHead == null) camaraHead = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
            if (usarModoTesting)
            {
                ModoTestingPC();
                return;
            }

            ManejarVelocidad();
            ManejarDireccionGaze();
        
    }

    void ManejarVelocidad()
    {
        float acc = acelerarAction.action.ReadValue<float>();
        float dec = desacelerarAction.action.ReadValue<float>();

        float objetivo = velocidadBase;
        if (dec > 0.05f) objetivo = Mathf.Lerp(velocidadBase, 0, dec);
        else if (acc > 0.05f) objetivo = Mathf.Lerp(velocidadBase, velocidadMaxima, acc);

        velocidadActual = Mathf.MoveTowards(velocidadActual, objetivo, 10f * Time.deltaTime);
    }

    void ManejarDireccionGaze()
    {
        // 1. Obtener rotación local de la cámara respecto al XR Origin
        // Normalizamos los ángulos para que vayan de -180 a 180
        Vector3 rotRotativa = camaraHead.localEulerAngles;
        float pitchHead = (rotRotativa.x > 180) ? rotRotativa.x - 360 : rotRotativa.x;
        float yawHead = (rotRotativa.y > 180) ? rotRotativa.y - 360 : rotRotativa.y;

        // 2. Procesar Input Vertical (Pitch)
        float pitchFinal = 0f;
        if (Mathf.Abs(pitchHead) > zonaMuertaGrados)
        {
            // Mapeo: si miro 45° arriba, la paloma sube
            float inputVertical = Mathf.Clamp(pitchHead / limiteGiroVertical, -1f, 1f);
            pitchFinal = inputVertical * 85f; 
        }

        // 3. Procesar Input Horizontal (Yaw/Roll)
        float factorGiro = 0f;
        float rollVisual = 0f;

        if (Mathf.Abs(yawHead) > zonaMuertaGrados)
        {
            // Mapeo de intensidad de giro según desviación del cuello
            float inputHorizontal = Mathf.Clamp(yawHead / limiteGiroHorizontal, -1f, 1f);
            
            // Agilidad inversamente proporcional a la velocidad
            float agilidad = Mathf.Lerp(80f, 25f, velocidadActual / velocidadMaxima);
            
            factorGiro = inputHorizontal * agilidad;
            rollVisual = inputHorizontal * 45f; // Inclinación estética
            
            yawAcumulado += factorGiro * Time.deltaTime;
        }

        // 4. Aplicar rotación al cuerpo (Rig)
        // La rotación en X y Z es temporal por el "input", el Yaw es acumulativo
        Quaternion rotTarget = Quaternion.Euler(pitchFinal, yawAcumulado, -rollVisual);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotTarget, suavizadoGiro * Time.deltaTime);

        // 5. Movimiento
        transform.position += transform.forward * velocidadActual * Time.deltaTime;
    }
    void ModoTestingPC()
    {
        // 🔶 MOVIMIENTO (WASD + subir/bajar)
        float h = Input.GetAxis("Horizontal"); // A D
        float v = Input.GetAxis("Vertical");   // W S

        float subir = 0f;
        if (Input.GetKey(KeyCode.Space)) subir = 1f;
        if (Input.GetKey(KeyCode.LeftControl)) subir = -1f;

        Vector3 direccion = new Vector3(h, subir, v);
        transform.Translate(direccion * velocidadMovimientoTesting * Time.deltaTime);

        // 🔶 ROTACIÓN CON MOUSE
        float mouseX = Input.GetAxis("Mouse X") * sensibilidadMouse * 100f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidadMouse * 100f * Time.deltaTime;

        rotacionY += mouseX;
        rotacionX -= mouseY;
        rotacionX = Mathf.Clamp(rotacionX, -80f, 80f);

        transform.rotation = Quaternion.Euler(rotacionX, rotacionY, 0f);
    }
}