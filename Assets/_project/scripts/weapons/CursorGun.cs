using UnityEngine;
using UnityEngine.InputSystem;

public class CursorGun : Gun
{
    [SerializeField] private Camera aimCamera;

    protected override void Awake()
    {
        base.Awake();

        if (aimCamera == null)
            aimCamera = Camera.main;
    }

    public override Vector2 GetAimDirection()
    {
        Camera cameraToUse = aimCamera != null ? aimCamera : Camera.main;
        if (cameraToUse == null)
            return Vector2.right;

        Vector3 mouseScreen = MousePosition();
        Vector3 mouseWorld = cameraToUse.ScreenToWorldPoint(mouseScreen);
        Vector2 origin = aimPivot != null ? aimPivot.position : transform.position;
        Vector2 direction = mouseWorld - (Vector3)origin;

        if (direction.sqrMagnitude < 0.0001f)
            return Vector2.right;

        return direction; // raw, not normalised
    }

    private Vector3 MousePosition()
    {
        Vector2 screenPosition = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
        Vector3 mousePosition = new Vector3(screenPosition.x, screenPosition.y, 0f);

        Camera cameraToUse = aimCamera != null ? aimCamera : Camera.main;
        mousePosition.z = cameraToUse != null ? Mathf.Abs(cameraToUse.transform.position.z) : 0f;
        return mousePosition;
    }
}