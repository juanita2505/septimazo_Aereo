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

    // Referencias privadas
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
        Debug.Log($"Hover! originalScale:{originalScale} targetScale:{targetScale}");

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

        if (buttonImage != null)
            StartCoroutine(ClickFlash());
    }

    //Condicion global
    
    private bool CanPlaySound(float lastTime, float delay)
    {
        return !audioSource.isPlaying &&
               Time.time - lastTime >= delay;
    }

    private void PlayRandomHover()
    {
        if (hoverSounds.Length == 0) return;

        if (hoverSounds.Length == 1)
        {
            audioSource.PlayOneShot(hoverSounds[0]);
            return;
        }

        int index;
        do
        {
            index = Random.Range(0, hoverSounds.Length);
        } while (index == lastHoverIndex);

        lastHoverIndex = index;
        audioSource.PlayOneShot(hoverSounds[index]);
    }

    private IEnumerator ClickFlash()
    {
        targetScale = originalScale * 0.9f;
        if (buttonImage != null) buttonImage.color = clickColor;

        yield return new WaitForSeconds(0.1f);

        targetScale = originalScale * hoverScale;
        if (buttonImage != null) buttonImage.color = hoverColor;
    }
}