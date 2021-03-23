using Mirror;
using UnityEngine;

public class MouseLook : NetworkBehaviour
{
    public Camera MainCamera;
	public RotationAxes axes = RotationAxes.MouseXAndY;
	public float sensitivityX = 15F;
	public float sensitivityY = 15F;
	public float minimumX = -360F;
	public float maximumX = 360F;
	public float minimumY = -60F;
	public float maximumY = 60F;
	float rotationX;
	float rotationY;
	Quaternion originalCameraRotation;
	Quaternion originalPlayerRotation;

	void Start()
	{
		originalCameraRotation = MainCamera.transform.localRotation;
		originalPlayerRotation = transform.localRotation;
	}

	void Update()
	{
        if (!isLocalPlayer)
        {
            return;
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
			MainCamera.transform.localRotation = originalCameraRotation * yQuaternion;
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
			MainCamera.transform.localRotation = originalCameraRotation * yQuaternion;
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