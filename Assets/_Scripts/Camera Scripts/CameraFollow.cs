using UnityEngine;

public class CameraFollow : MonoBehaviour {
    private float current_Height;
    private float current_Rotation;
    public float follow_Distance = 6f;

    public float follow_Height = 8f;

    private Transform player;

    private float target_Height;

    // Use this for initialization
    private void Awake() {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }


    // Update is called once per frame
    private void Update() {
        target_Height = player.position.y + follow_Height;
        current_Rotation = transform.eulerAngles.y;
        current_Height = Mathf.Lerp(transform.position.y, target_Height, 0.9f * Time.deltaTime);

        var euler = Quaternion.Euler(0f, current_Rotation, 0f);

        var target_Position = player.position - euler * Vector3.forward * follow_Distance;

        target_Position.y = current_Height;
        transform.position = target_Position;
        transform.LookAt(player);
    }
}
