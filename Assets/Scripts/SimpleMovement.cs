using UnityEngine;

public class SimpleMovement : MonoBehaviour
{
    public float Speed = 1;

    private void Update()
    {
        var horizontalAxis = Input.GetAxisRaw("Horizontal");
        var verticalAxis = Input.GetAxisRaw("Vertical");

        var movementVector = new Vector3(horizontalAxis, 0, verticalAxis).normalized * Time.deltaTime * Speed;
        transform.Translate(movementVector, Space.Self);
    }
}
