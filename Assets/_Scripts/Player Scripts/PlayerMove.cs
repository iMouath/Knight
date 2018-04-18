using UnityEngine;

public class PlayerMove : MonoBehaviour {
    private Animator anim;
    private bool canMove;
    private CharacterController charController;
    private CollisionFlags collisionFlags = CollisionFlags.None;
    private bool finished_Movement = true;

    private readonly float gravity = 9.8f;
    private float height;

    private readonly float moveSpeed = 5f;
    private Vector3 player_Move = Vector3.zero;

    private float player_ToPointDistance;

    private Vector3 target_Pos = Vector3.zero;

    public bool FinishedMovement {
        get { return finished_Movement; }
        set { finished_Movement = value; }
    }

    public Vector3 TargetPosition {
        get { return target_Pos; }
        set { target_Pos = value; }
    }

    private void Awake() {
        anim = GetComponent<Animator>();
        charController = GetComponent<CharacterController>();
    }

    private void Update() {
        CalculateHeight();
        CheckIfFinishedMovement();
    }

    private bool IsGrounded() {
        return collisionFlags == CollisionFlags.CollidedBelow ? true : false;
    }

    private void CalculateHeight() {
        if (IsGrounded())
            height = 0f;
        else
            height -= gravity * Time.deltaTime;
    }

    private void CheckIfFinishedMovement() {
        // if we DID NOT finished movement
        if (!finished_Movement) {
            if (!anim.IsInTransition(0) && !anim.GetCurrentAnimatorStateInfo(0).IsName("Stand")
                                        && anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.8f)
                finished_Movement = true;
        }
        else {
            MoveThePlayer();
            player_Move.y = height * Time.deltaTime;
            collisionFlags = charController.Move(player_Move);
        }
    }

    private void MoveThePlayer() {
        if (Input.GetMouseButtonDown(0)) {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
                if (hit.collider is TerrainCollider) {
                    player_ToPointDistance = Vector3.Distance(transform.position, hit.point);

                    if (player_ToPointDistance >= 1.0f) {
                        canMove = true;
                        target_Pos = hit.point;
                    }
                }
        } // if mouse button down

        if (canMove) {
            anim.SetFloat("Walk", 1.0f);

            var target_Temp = new Vector3(target_Pos.x, transform.position.y, target_Pos.z);

            transform.rotation = Quaternion.Slerp(transform.rotation,
                                                  Quaternion.LookRotation(target_Temp - transform.position),
                                                  15.0f * Time.deltaTime);

            player_Move = transform.forward * moveSpeed * Time.deltaTime;

            if (Vector3.Distance(transform.position, target_Pos) <= 0.1f) canMove = false;
        }
        else {
            player_Move.Set(0f, 0f, 0f);
            anim.SetFloat("Walk", 0f);
        }
    }
} // class
