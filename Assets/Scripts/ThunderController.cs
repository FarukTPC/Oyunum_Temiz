using System.Collections;
using UnityEngine;

public class ThunderController : MonoBehaviour
{
    #region Variables
    [Header("Light Settings (HDRP)")]
    [SerializeField] private Light thunderLight;
    [SerializeField] private float minIntensity = 5000f;
    [SerializeField] private float maxIntensity = 20000f;
    [SerializeField] private Color thunderColor = new Color(0.8f, 0.9f, 1f);

    [Header("Timing Settings")]
    [SerializeField] private float minTimeBetweenThunders = 5f;
    [SerializeField] private float maxTimeBetweenThunders = 15f;
    [SerializeField] private float flashDuration = 0.2f;
    
    [Header("Audio Settings")]
    [SerializeField] private AudioSource thunderAudioSource;
    [SerializeField] private AudioClip[] thunderSounds;

    #endregion

    #region Unity Methods

    private void Start()
    {
        if (thunderLight != null)
        {
            thunderLight.intensity = 0;
            thunderLight.color = thunderColor;

            StartCoroutine(ThunderRoutine());
        }
        else
        {
            Debug.LogError("ThunderLight atanmadı! Lütfen Inspector'dan ışığı atayın.");
        }
    }
        #endregion

        #region Custom Functions

        /// <summary>
        /// 
        /// </summary>
        
        private IEnumerator ThunderRoutine()
        {
            while (true)
            {
                float waitTime = Random.Range(minTimeBetweenThunders, maxTimeBetweenThunders);
                yield return new WaitForSeconds(waitTime);

                yield return StartCoroutine(FlashEffect());
            }
    }

    /// <summary
    /// 
    /// </summary>
    
    private IEnumerator FlashEffect()
    {
        if (thunderAudioSource != null && thunderSounds.Length > 0)
        {
            AudioClip clip = thunderSounds[Random.Range(0, thunderSounds.Length)];
            thunderAudioSource.PlayOneShot(clip);
        }

        thunderLight.intensity = Random.Range(minIntensity, maxIntensity);

        yield return new WaitForSeconds(flashDuration);

        thunderLight.intensity = 0;

        if (Random.value > 0.3f)
        {
            yield return new WaitForSeconds(0.1f);
            thunderLight.intensity = Random.Range(minIntensity / 2, maxIntensity / 2);
            yield return new WaitForSeconds(0.1f);
            thunderLight.intensity = 0;
        }
}

#endregion
}
