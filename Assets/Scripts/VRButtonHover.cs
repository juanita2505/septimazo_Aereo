using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class VRButtonHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Escala")]
    public float hoverScale = 1.15f;
    public float animSpeed = 8f;

    [Header("Color")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 0.7f, 0f);
    public Color clickColor = Color.white;

    [Header("Sonido Hover (elige aleatorio entre los 3)")]
    public AudioClip[] hoverSounds = new AudioClip[3];
    public float hoverSoundDelay = 0.1f;

    [Header("Sonido Click (siempre el mismo)")]
    public AudioClip clickSound;
    public float clickSoundDelay = 0.3f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private Image buttonImage;
    private AudioSource audioSource;
    private float lastHoverSoundTime = -99f;
    private float lastClickSoundTime = -99f;
    private int lastHoverIndex = -1;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        buttonImage = GetComponent<Image>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * animSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
        if (buttonImage != null)
            buttonImage.color = hoverColor;

        if (CanPlaySound(lastHoverSoundTime, hoverSoundDelay))
        {
            PlayRandomHover();
            lastHoverSoundTime = Time.time;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
        if (buttonImage != null)
            buttonImage.color = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (CanPlaySound(lastClickSoundTime, clickSoundDelay))
        {
            if (clickSound != null)
                audioSource.PlayOneShot(clickSound);
            lastClickSoundTime = Time.time;
        }

        // Solo iniciar la coroutine si el objeto sigue activo
        if (buttonImage != null && gameObject.activeInHierarchy)
            StartCoroutine(ClickFlash());
    }

    private bool CanPlaySound(float lastTime, float delay)
    {
        return audioSource != null &&
               !audioSource.isPlaying &&
               Time.time - lastTime >= delay;
    }

    private void PlayRandomHover()
    {
        if (hoverSounds == null || hoverSounds.Length == 0) return;

        if (hoverSounds.Length == 1)
        {
            if (hoverSounds[0] != null)
                audioSource.PlayOneShot(hoverSounds[0]);
            return;
        }

        int index;
        int intentos = 0;
        do
        {
            index = Random.Range(0, hoverSounds.Length);
            intentos++;
        }
        while (index == lastHoverIndex && intentos < 10);

        lastHoverIndex = index;

        if (hoverSounds[index] != null)
            audioSource.PlayOneShot(hoverSounds[index]);
    }

    private IEnumerator ClickFlash()
    {
        // Guardar referencia local por si el objeto se desactiva
        Image imgRef = buttonImage;

        targetScale = originalScale * 0.9f;
        if (imgRef != null) imgRef.color = clickColor;

        yield return new WaitForSeconds(0.1f);

        // Verificar que el objeto siga activo antes de continuar
        if (!gameObject.activeInHierarchy) yield break;

        targetScale = originalScale * hoverScale;
        if (imgRef != null) imgRef.color = hoverColor;
    }
}