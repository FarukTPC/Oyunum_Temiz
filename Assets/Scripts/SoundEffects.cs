using UnityEngine;

public class SoundEffects : MonoBehaviour
{
    #region Variables
    [Header("Global Settings")]
    [Tooltip("Tüm seslerin genel ses seviyesi kontrolü")]
    [Range(0f, 1f)] public float masterVolume = 1f;

    [Header("Rain Audio Settings")]
    public AudioSource rainAudioSource;
    [Range(0f, 1f)] public float rainVolume = 0.5f;

    

    #endregion

    #region Unity Methods

    private void Start()
    {
        InitializeRain();
    }

    private void Update()
    {
        UpdateVolumes();
    }

    #endregion
    
    #region Custom Functions

    /// <summary>
    /// 
    /// </summary>
    
    private void InitializeRain()
    {
        if (rainAudioSource != null)
        {
            rainAudioSource.loop = true;
            rainAudioSource.Play();
        }
        else
        {
            Debug.LogError("SoundEffects: RainAudioSource atanmamış!");
        }
    }

    /// <summary>
    /// 
    /// </summary>   
    
    private void UpdateVolumes()
    {
        if (rainAudioSource != null)
        {
            rainAudioSource.volume = masterVolume * rainVolume;
        }
    }

    #endregion
}
