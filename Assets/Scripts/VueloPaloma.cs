using UnityEngine;
using UnityEngine.InputSystem;

public class VueloPalomaGaze : MonoBehaviour
{
    [Header("Referencias")]
    public Transform camaraHead;

    [Header("Configuración de Vuelo")]
    public float velocidadBase = 5f;
    public float velocidadMaxima = 30f;
    public float suavizadoGiro = 3f;
    public float zonaMuertaGrados = 5f;

    [Header("Límites")]
    public float limiteGiroHorizontal = 60f;
    public float limiteGiroVertical = 45f;

    [Header("Input")]
    public InputActionProperty acelerarAction;
    public InputActionProperty desacelerarAction;

    [Header("Testing")]
    public bool usarModoTesting = false;
    public float velocidadMovimientoTesting = 10f;
    public float sensibilidadMouse = 2f;

    private float velocidadActual;
    private float yawAcumulado;

    private Rigidbody rb;

    float rotacionX;
    float rotacionY;

    void OnEnable()
    {
        acelerarAction.action?.Enable();
        desacelerarAction.action?.Enable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        velocidadActual = velocidadBase;
        yawAcumulado = transform.eulerAngles.y;

        if (camaraHead == null)
            camaraHead = Camera.main.transform;
    }

    void FixedUpdate()
    {
        if (usarModoTesting)
        {
            ModoTestingPC();
            return;
        }

        ManejarVelocidad();
        ManejarDireccionGaze();
    }

    void LateUpdate()
    {
        if (!usarModoTesting && camaraHead != null)
        {
            // Mantiene centrada la posición física del visor
            // pero conserva la rotación (mirar arriba/abajo/izquierda/derecha)

            Vector3 posLocal = camaraHead.localPosition;

            posLocal.x = 0f;
            posLocal.y = 0f;
            posLocal.z = 0f;

            camaraHead.localPosition = posLocal;
        }
    }

    void ManejarVelocidad()
    {
        // Leer joystick izquierdo (Vector2)
        Vector2 move = acelerarAction.action.ReadValue<Vector2>();

        // Solo usar adelante/atrás
        float avance = Mathf.Clamp(move.y, 0f, 1f);

        // Velocidad según cuánto empujes el joystick
        float velocidadJoystick =
            Mathf.Lerp(
                velocidadBase,
                velocidadMaxima,
                avance
            );

        // Leer gatillo izquierdo
        float freno =
            desacelerarAction.action.ReadValue<float>();

        // Aplicar frenado
        float objetivo = velocidadJoystick;

        if (freno > 0.05f)
        {
            objetivo =
                Mathf.Lerp(
                    velocidadJoystick,
                    0f,
                    freno
                );
        }

        // Suavizar cambios
        velocidadActual =
            Mathf.Lerp(
                velocidadActual,
                objetivo,
                3f * Time.deltaTime
            );
    }

    void ManejarDireccionGaze()
    {
        Vector3 rot = camaraHead.localEulerAngles;

        float pitch =
            rot.x > 180 ? rot.x - 360 : rot.x;

        float yaw =
            rot.y > 180 ? rot.y - 360 : rot.y;

        float pitchFinal = 0;

        if (Mathf.Abs(pitch) > zonaMuertaGrados)
        {
            pitchFinal =
                Mathf.Clamp(
                    pitch / limiteGiroVertical,
                    -1f,
                    1f
                ) * 60f;
        }

        float roll = 0;

        if (Mathf.Abs(yaw) > zonaMuertaGrados)
        {
            float horizontal =
                Mathf.Clamp(
                    yaw / limiteGiroHorizontal,
                    -1f,
                    1f
                );

            yawAcumulado +=
                horizontal *
                60 *
                Time.deltaTime;

            roll = horizontal * 25f;
        }

        Quaternion objetivo =
            Quaternion.Euler(
                pitchFinal,
                yawAcumulado,
                -roll
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                objetivo,
                suavizadoGiro * Time.deltaTime
            );

        rb.linearVelocity =
            transform.forward *
            velocidadActual;
    }

    void ModoTestingPC()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        float mouseX =
            Input.GetAxis("Mouse X") *
            sensibilidadMouse;

        float mouseY =
            Input.GetAxis("Mouse Y") *
            sensibilidadMouse;

        rotacionY += mouseX;
        rotacionX -= mouseY;

        rotacionX = Mathf.Clamp(
            rotacionX,
            -80,
            80
        );

        transform.rotation =
            Quaternion.Euler(
                rotacionX,
                rotacionY,
                0
            );

        transform.Translate(
            new Vector3(h, 0, v) *
            velocidadMovimientoTesting *
            Time.deltaTime
        );
    }
}