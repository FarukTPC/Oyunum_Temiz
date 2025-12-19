using UnityEngine;
using System.Collections;

public class ClimbSpot : MonoBehaviour, IInteractable
{
    [Header("Tırmanma Ayarları")]
    [Tooltip("Karakter tırmanmayı bitirince tam olarak nerede dursun?")]
    [SerializeField] private Transform targetPosition;

    [Tooltip("Animasyon kaç saniye sürüyor? (Bu süre bitince ışınlanır)")]
    [SerializeField] private float climbDuration = 1.5f;

    public void Interact(PlayerMovement player)
    {
        player.canMove = false;
        StartCoroutine(ClimbRoutine(player));
    }

    private IEnumerator ClimbRoutine(PlayerMovement player)
    {
        Animator anim = player.GetComponentInChildren<Animator>();

        if(anim != null)
        {
            anim.SetTrigger("Climb");
        }

        player.transform.rotation = transform.rotation;

        yield return new WaitForSeconds(climbDuration);

        CharacterController cc = player.GetComponent<CharacterController>();
        
        if(cc != null) cc.enabled = false;
        player.transform.position = targetPosition.position;
        if(cc != null) cc.enabled = true;

        player.canMove = true;
    }
}