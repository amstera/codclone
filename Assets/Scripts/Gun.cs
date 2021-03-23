using UnityEngine;

public class Gun : MonoBehaviour
{
    private Quaternion startRotation;
    private Vector3 startNormalPosition;
    private Vector3 startAimPosition;
    private Vector3 startPosition;

    public GameObject MuzzleFlash;

    [Header("Sway Movement")]
    public float Amount;
    public float MaxAmount;
    public float SmoothAmount;

    void Start()
    {
        startRotation = transform.localRotation;
        SetStartPositions();
    }

    void Update()
    {
        SwayMovement();
    }

    public void AimGun()
    {
        transform.Rotate(new Vector3(0, 80, 0), Space.Self);
        Camera.main.fieldOfView = 24;
        transform.position += Vector3.up * 0.15f;
        startPosition = startAimPosition;
    }

    public void ReleaseGun()
    {
        transform.localRotation = startRotation;
        Camera.main.fieldOfView = 60;
        transform.position -= Vector3.up * 0.15f;
        startPosition = startNormalPosition;
    }

    public void ShootGun()
    {
        MuzzleFlash.SetActive(true);
        Invoke("StopMuzzleFlash", 0.25f);
    }

    private void StopMuzzleFlash()
    {
        MuzzleFlash.SetActive(false);
    }

    private void SetStartPositions()
    {
        startNormalPosition = transform.localPosition;
        AimGun();
        startAimPosition = transform.localPosition;
        ReleaseGun();
    }

    private void SwayMovement()
    {
        float movementX = Input.GetAxis("Mouse X") * Amount + Input.GetAxis("Horizontal") * Amount / 3;
        float movementY = Input.GetAxis("Mouse Y") * Amount + Input.GetAxis("Vertical") * Amount / 3;
        movementX = Mathf.Clamp(movementX, -MaxAmount, MaxAmount);
        movementY = Mathf.Clamp(movementY, -MaxAmount, MaxAmount);

        Vector3 finalPos = new Vector3(movementX, movementY, 0);
        transform.localPosition = Vector3.Lerp(transform.localPosition, finalPos + startPosition, Time.deltaTime * SmoothAmount);
    }
}
