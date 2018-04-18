using UnityEngine;

public class IdleRunJump : MonoBehaviour {
    protected Animator animator;
    public bool ApplyGravity = true;
    public float DirectionDampTime = .25f;

    // Use this for initialization
    private void Start() {
        animator = GetComponent<Animator>();

        if (animator.layerCount >= 2)
            animator.SetLayerWeight(1, 1);
    }

    // Update is called once per frame
    private void Update() {
        if (animator) {
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.IsName("Base Layer.Motion")) {
                if (Input.GetButton("Fire1") || Input.GetKey(KeyCode.Space)) animator.SetBool("Jump", true);
            }
            else {
                animator.SetBool("Jump", false);
            }

            if (Input.GetButtonDown("Fire2") && animator.layerCount >= 2)
                animator.SetBool("Hi", !animator.GetBool("Hi"));


            var h = Input.GetAxis("Horizontal");
            var v = Input.GetAxis("Vertical");

            animator.SetFloat("Speed", h * h + v * v);
            animator.SetFloat("Direction", h, DirectionDampTime, Time.deltaTime);
        }
    }
}