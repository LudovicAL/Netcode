using Unity.Netcode;
using UnityEngine;

public class CharacterController2D : NetworkBehaviour {

    [Tooltip("Player's movement speed")]
    [SerializeField]
    private float movementSpeed;

    // Start is called before the first frame update
    void Start() {
    }

    // Update is called once per frame
    void Update() {
        move();
    }

    private void move() {
        if (IsOwner) {
            Vector3 moveDirection = new Vector3(0f, 0f, 0f);

            if (Input.GetKey(KeyCode.W)) {
                moveDirection.y++;
            }
            if (Input.GetKey(KeyCode.A)) {
                moveDirection.x--;
            }
            if (Input.GetKey(KeyCode.S)) {
                moveDirection.y--;
            }
            if (Input.GetKey(KeyCode.D)) {
                moveDirection.x++;
            }

            transform.position += moveDirection * movementSpeed * Time.deltaTime;
        }
    }
}
