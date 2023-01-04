using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;

public class CharacterController2D : NetworkBehaviour {

    [Tooltip("Player's movement speed")]
    [SerializeField]
    private float movementSpeed;

    void Start() {
        
    }

    void Update() {
        move();
    }

    //Moves the character in the desired direction
    private void move() {
        if (!IsOwner) {
            return;
        }
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
