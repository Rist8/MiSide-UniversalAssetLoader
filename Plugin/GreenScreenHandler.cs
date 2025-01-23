using UnityEngine;

public class GreenScreenHandler
{
    public static GameObject greenScreenCameraObject = null;
    public static Camera greenScreenCamera = null;
    public static float dx = 0.0f, dy = 0.0f, dz = 0.0f, rdx = 0.0f, rdy = 0.0f;
    private static float baseMovementSpeed = 0.03f;
    private static float maxMovementSpeed = 0.1f;
    private static float mouseSensitivity = 0.7f;

    public static void HandleGreenScreenCommand(string[] parts)
    {
        if (greenScreenCameraObject == null)
        {
            greenScreenCameraObject = new GameObject("GreenScreenCamera");
        }

        if (greenScreenCamera == null)
        {
            greenScreenCamera = greenScreenCameraObject.AddComponent<Camera>();
            greenScreenCamera.clearFlags = CameraClearFlags.SolidColor;
            greenScreenCamera.backgroundColor = Color.green;
        }

        // Set the camera's position and rotation defaults
        greenScreenCamera.transform.position = new Vector3(0.65f + dx, 1.6f + dy, 0.85f + dz);
        greenScreenCamera.transform.rotation = Quaternion.Euler(10 + rdx, -135 + rdy, 0);

        if (parts.Length == 2)
        {
            bool isActive = parts[1] != "off";
            greenScreenCameraObject.SetActive(isActive);
            SetGreenScreenObjectsActive(isActive);
        }
        else
        {
            greenScreenCameraObject.SetActive(true);
            SetGreenScreenObjectsActive(false);
        }

        if (parts.Length == 5 && parts[1] == "pos")
        {
            greenScreenCamera.transform.position = new Vector3(
                float.Parse(parts[2]) + 0.65f,
                float.Parse(parts[3]) + 1.6f,
                float.Parse(parts[4]) + 0.85f);
        }
        else if (parts.Length == 5 && parts[1] == "rot")
        {
            greenScreenCamera.transform.rotation = Quaternion.Euler(
                float.Parse(parts[2]) + 10,
                float.Parse(parts[3]) - 135,
                float.Parse(parts[4]));
        }
        else if (parts.Length == 9 && parts[1] == "pos" && parts[5] == "rot")
        {
            greenScreenCamera.transform.position = new Vector3(
                float.Parse(parts[2]) + 0.65f,
                float.Parse(parts[3]) + 1.6f,
                float.Parse(parts[4]) + 0.85f);

            greenScreenCamera.transform.rotation = Quaternion.Euler(
                float.Parse(parts[6]) + 10,
                float.Parse(parts[7]) - 135,
                float.Parse(parts[8]));
        }
    }

    public static void SetGreenScreenObjectsActive(bool isActive)
    {
        var partic = GameObject.Find("ParticlesBack");
        if (partic != null)
        {
            partic.SetActive(isActive);
        }

        var cyl = GameObject.Find("Cylinder");
        if (cyl != null)
        {
            cyl.SetActive(isActive);
        }
    }

    public static void ToggleGreenScreen()
    {
        if (greenScreenCameraObject == null || !greenScreenCameraObject.active)
            ConsoleCommandHandler.ConsoleEnter("greenscreen");
        else
            ConsoleCommandHandler.ConsoleEnter("greenscreen off");
    }

    public static void HandleGreenScreenCameraMovement()
    {
        if (greenScreenCameraObject != null && greenScreenCameraObject.active)
        {
            // Mouse Input for Rotation
            float mouseX = UnityEngine.Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = UnityEngine.Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Update rotation deltas
            rdy += mouseX; // Horizontal rotation (Y-axis)
            rdx -= mouseY; // Vertical rotation (X-axis)

            // Clamp the vertical rotation to prevent flipping
            rdx = Mathf.Clamp(rdx, -90f, 90f);

            // Movement Input
            Vector3 forward = greenScreenCamera.transform.forward;
            Vector3 right = greenScreenCamera.transform.right;
            Vector3 up = greenScreenCamera.transform.up;

            forward.y = 0; // Ignore vertical component for planar movement
            right.y = 0;

            forward.Normalize();
            right.Normalize();

            // Adjust speed if Shift is held
            if (UnityEngine.Input.GetKey(KeyCode.LeftShift))
            {
                baseMovementSpeed = Mathf.Min(baseMovementSpeed * 1.02f, maxMovementSpeed);
            }
            else
            {
                baseMovementSpeed = 0.03f;
            }

            Vector3 movement = Vector3.zero;
            if (UnityEngine.Input.GetKey(KeyCode.W)) movement += forward * baseMovementSpeed;
            if (UnityEngine.Input.GetKey(KeyCode.S)) movement -= forward * baseMovementSpeed;
            if (UnityEngine.Input.GetKey(KeyCode.D)) movement += right * baseMovementSpeed;
            if (UnityEngine.Input.GetKey(KeyCode.A)) movement -= right * baseMovementSpeed;
            if (UnityEngine.Input.GetKey(KeyCode.Space)) movement += up * baseMovementSpeed;
            if (UnityEngine.Input.GetKey(KeyCode.LeftControl)) movement -= up * baseMovementSpeed;

            // Update Camera Transform
            Vector3 newPosition = greenScreenCamera.transform.position + movement;
            Quaternion newRotation = Quaternion.Euler(rdx, -135 + rdy, 0);

            greenScreenCamera.transform.SetPositionAndRotation(newPosition, newRotation);
        }
    }
}
