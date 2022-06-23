using UnityEngine;

public class FreeCam : MonoBehaviour
{
    public float Sensitivity = 2f;
    public float Speed = 25f;

    private Camera cam;
    private float yaw, pitch;
    private Vector3 dir;

    void Start()
    {
        cam = GetComponent<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || (Cursor.lockState == CursorLockMode.None && Input.GetMouseButtonDown(0)))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.None ? CursorLockMode.Locked : CursorLockMode.None;
        }
        if (Cursor.lockState != CursorLockMode.None)
        {
            dir = cam.transform.forward * Input.GetAxisRaw("Vertical") + cam.transform.right * Input.GetAxisRaw("Horizontal");
            dir.y += Input.GetKey(KeyCode.E) ? 1 : Input.GetKey(KeyCode.Q) ? -1 : 0;
            cam.transform.position += dir * Speed * Time.deltaTime;
            yaw += Input.GetAxis("Mouse X") * Sensitivity;
            pitch -= Input.GetAxis("Mouse Y") * Sensitivity;
            pitch = Mathf.Clamp(pitch, -90, 90);
            cam.transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        }
    }
}
