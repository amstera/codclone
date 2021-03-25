using UnityEngine;

public class Gun : MonoBehaviour
{
    public GameObject MuzzleFlash;
    public Camera MainCamera;
    public Camera AimCamera;

    public void AimGun()
    {
        MainCamera.enabled = false;
        AimCamera.enabled = true;
    }

    public void ReleaseGun()
    {
        MainCamera.enabled = true;
        AimCamera.enabled = false;
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
}
