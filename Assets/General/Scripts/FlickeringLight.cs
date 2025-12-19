using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class FlickeringLight : MonoBehaviour
{
    [Header("Bileşenler")]
    public Light spotLight;
    public MeshRenderer bulbRenderer;
    private AudioSource audioSource;
    [Header ("Ses Dosyaları")]
    public AudioClip electricHum;
    public AudioClip[] glitchSounds;
    [Header("Zamanlama Ayarları")]
    public float minWaitTime = 0.05f;
    public float maxWaitTime = 0.2f;

    [Header("Voltaj Ayarları")]
    [Range(0f, 1f)]
    public float failureChance = 0.1f;

    [Tooltip("En karanlık anında ne kadar yansın?")]
    [Range(0f, 1f)]
    public float minBrightness = 0.0f;

    [Tooltip("En parlak titremesinde ne kadar yansın?")]
    [Range(0f, 1f)]
    public float maxBrightness = 0.5f;

    private float defaultLightIntensity;
    private Color defaultEmissiveColor;
    private Material targetMaterial;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (electricHum != null)
        {
            audioSource.clip = electricHum;
            audioSource.loop = true;
            audioSource.volume = 0.3f;
            audioSource.Play();
        }
        if (spotLight != null) defaultLightIntensity = spotLight.intensity;
        if (bulbRenderer != null)
        {
            targetMaterial = bulbRenderer.material;
            if (targetMaterial.HasProperty("_EmissiveColor"))
            defaultEmissiveColor = targetMaterial.GetColor("_EmissiveColor");
            else
            defaultEmissiveColor = Color.white * 10f;

        }
        StartCoroutine(FlickerLoop());

        IEnumerator FlickerLoop()
        {
            while (true)
            {
                if (Random.value < failureChance)
                {
                    float dimFactor = Random.Range (0.1f, 0.7f);
                    if (spotLight != null) spotLight.intensity = defaultLightIntensity * dimFactor;
                    if (targetMaterial != null) targetMaterial.SetColor("_EmissiveColor", defaultEmissiveColor * dimFactor);
                    if (glitchSounds.Length > 0)
                    {
                        audioSource.pitch = Random.Range(0.8f,1.2f);
                        AudioClip randomClip = glitchSounds[Random.Range(0, glitchSounds.Length)] ;
                        audioSource.PlayOneShot(randomClip, 1f);
                    }
                    yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
            }
            else
                {
                    if (spotLight != null) spotLight.intensity = defaultLightIntensity;
                    if (targetMaterial != null) targetMaterial.SetColor("_EmissiveColor", defaultEmissiveColor);

                    audioSource.pitch = 1f;
                    yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
                }
            }
        }
    }
}

