using UnityEngine;

public class FlameThrower : MonoBehaviour
{
    public float Fuel = 400;
    public GameObject Flames;
    private bool isActive;

    [Header("Sounds")]
    public AudioSource FireSound;

    void Update()
    {
        if (isActive)
        {
            Fuel -= 1f;
            if (Fuel <= 0)
            {
                HideFlame();
            }
        }
    }

    public void UseFlame()
    {
        FireSound.Play();
        Flames.SetActive(true);
        isActive = true;
    }

    public void HideFlame()
    {
        FireSound.Stop();
        Flames.SetActive(false);
        isActive = false;
    }
}
