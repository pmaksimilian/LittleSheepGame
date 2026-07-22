using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
	private const string IS_WALKING = "IsWalking";
	
	// referenca na Animator komponento
	private Animator animator;
	
	// referenca na playera, ki jo nastavimo v editorju
	[SerializeField] private PlayerMovement player;
	
	private void Awake() {
		// preko reference dobimo komponento
		animator = GetComponent<Animator>();
	}
	private void Update() {
		// funkcija za set-anje bool-a v animatorju, uporabimo konstanto, ker se nastavlja preko stringa ("IsWalking" je ime fielda v editorju), IsWalking je getter za trenutno stanje playerja
		animator.SetFloat("LastInputX", player.LastInputX());
		animator.SetFloat("LastInputY", player.LastInputY());
		animator.SetFloat("InputX", player.InputX());
		animator.SetFloat("InputY", player.InputY());
		animator.SetBool(IS_WALKING, player.IsWalking());
	}
}