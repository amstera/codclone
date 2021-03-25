using Mirror;
using UnityEngine;

public class MouseLook : NetworkBehaviour
{
    public Camera MainCamera;
    public Camera AimCamera;
	public RotationAxes axes = RotationAxes.MouseXAndY;
	public float sensitivityX = 15F;
	public float sensitivityY = 15F;
	public float minimumX = -360F;
	public float maximumX = 360F;
	public float minimumY = -60F;
	public float maximumY = 60F;

	private float rotationX;
	private float rotationY;
	private Quaternion originalCameraRotation;
    private Quaternion originalAimCameraRotation;
    private Quaternion originalPlayerRotation;

    void Start()
	{
		originalCameraRotation = MainCamera.transform.localRotation;
        originalAimCameraRotation = AimCamera.transform.localRotation;
        originalPlayerRotation = transform.localRotation;
	}

	void Update()
	{
        if (!isLocalPlayer || GetComponent<Player>().IsDead)
        {
            return;
        }

        Camera viewedCamera = MainCamera.enabled ? MainCamera : AimCamera;
        Quaternion camRotation = MainCamera.enabled ? originalCameraRotation : originalAimCameraRotation;

        if (viewedCamera == MainCamera)
        {
            minimumY = -60f;
            maximumY = 60f;
        }
        else
        {
            minimumY = -15;
            maximumY = 5;
        }

        if (axes == RotationAxes.MouseXAndY)
		{
			rotationX += Input.GetAxis("Mouse X") * sensitivityX;
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
			rotationX = ClampAngle(rotationX, minimumX, maximumX);
			rotationY = ClampAngle(rotationY, minimumY, maximumY);
			Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
			Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, -Vector3.right);
			transform.localRotation = originalPlayerRotation * xQuaternion;
            viewedCamera.transform.localRotation = camRotation * yQuaternion;
		}
		else if (axes == RotationAxes.MouseX)
		{
			rotationX += Input.GetAxis("Mouse X") * sensitivityX;
			rotationX = ClampAngle(rotationX, minimumX, maximumX);
			Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
			transform.localRotation = originalPlayerRotation * xQuaternion;
		}
		else
		{
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
			rotationY = ClampAngle(rotationY, minimumY, maximumY);
			Quaternion yQuaternion = Quaternion.AngleAxis(-rotationY, Vector3.right);
            viewedCamera.transform.localRotation = camRotation * yQuaternion;
		}
	}

	private float ClampAngle(float angle, float min, float max)
	{
		if (angle < -360F)
		{
			angle += 360F;
		}
		if (angle > 360F)
		{
			angle -= 360F;
		}

		return Mathf.Clamp(angle, min, max);
	}
}

public enum RotationAxes
{
	MouseXAndY = 0,
	MouseX = 1,
	MouseY = 2
}