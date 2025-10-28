using UnityEngine;
using UnityEngine.InputSystem;

public class PersonMovement : MonoBehaviour
{
    public float movementSpeed;

    void Update()
    {
        float horizontalInput = 0f;
        float verticalInput = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) horizontalInput = -1f;
            if (Keyboard.current.dKey.isPressed) horizontalInput = 1f;
            if (Keyboard.current.wKey.isPressed) verticalInput = 1f;
            if (Keyboard.current.sKey.isPressed) verticalInput = -1f;
        }

        Vector3 movement = new Vector3(horizontalInput, 0f, verticalInput).normalized;
        transform.Translate(movement * movementSpeed * Time.deltaTime);
    }
}
