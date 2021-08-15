using UnityEditor;
using UnityEngine;

public class CamMove : MonoBehaviour {
    private float XSensitivity = 2f;
    private float YSensitivity = 2f;

    float xRotation = 0f;

    public float maxHorizontalSpeed = 10;
    public float horizontalAcceleration = 2;
    public float verticalSpeed = 10;
    Vector2 horizontalVelocity = new Vector2();
    bool cursorIsLocked;

    private void Awake() {
        Cursor.lockState = CursorLockMode.Locked;
        cursorIsLocked = true;
    }

    void Update() {
        if (cursorIsLocked) {
            RotateView();

            //accelerate
            horizontalVelocity += new Vector2(transform.forward.x, transform.forward.z).normalized * horizontalAcceleration * Input.GetAxisRaw("Vertical");
            horizontalVelocity += new Vector2(transform.right.x, transform.right.z).normalized * horizontalAcceleration * Input.GetAxisRaw("Horizontal");

            //clamp
            if (horizontalVelocity.magnitude > maxHorizontalSpeed) {
                horizontalVelocity = horizontalVelocity.normalized * maxHorizontalSpeed;
            }

            float verticalMove = 0;

            if (Input.GetKey(KeyCode.Space)) {
                verticalMove = verticalSpeed;
            }
            if (Input.GetKey(KeyCode.LeftShift)) {
                verticalMove -= verticalSpeed;
            }

            transform.Translate(new Vector3(horizontalVelocity.x, verticalMove, horizontalVelocity.y) * Time.deltaTime, Space.World);

            if (Input.GetAxisRaw("Vertical") == 0 && Input.GetAxisRaw("Horizontal") == 0) {
                horizontalVelocity *= 0.96f;
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape)) {
            cursorIsLocked = false;
        }
        if (Input.GetMouseButtonDown(0) && Input.mousePosition.x >= 0 && Input.mousePosition.y >= 0 && Input.mousePosition.x <= Handles.GetMainGameViewSize().x && Input.mousePosition.y <= Handles.GetMainGameViewSize().y) {
            cursorIsLocked = true;
        }
    }

    private void RotateView() {
        float mouseX = Input.GetAxis("Mouse X") * XSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * YSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, transform.localRotation.eulerAngles.y, 0f);
        transform.Rotate(Vector3.up * mouseX, Space.World);
    }
}
