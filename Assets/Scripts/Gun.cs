using UnityEngine;

public class Gun : MonoBehaviour
{
    public GameObject MuzzleFlash;
    public Camera MainCamera;
    public Camera AimCamera;

    [Header("Sounds")]
    public AudioSource GunShot;

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
        GunShot.Play();
        MuzzleFlash.SetActive(true);
        Invoke("StopMuzzleFlash", 0.25f);
    }

    private void StopMuzzleFlash()
    {
        MuzzleFlash.SetActive(false);
    }
}
