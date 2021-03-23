using Mirror;
using UnityEngine;

public class Player : NetworkBehaviour
{
	public float Speed = 5;
    public Camera MainCamera;
    public Gun Gun;

    [Header("Particles")]
    public GameObject BloodHit;
    public GameObject GroundHit;

    private Rigidbody rigidBody;
    private bool isGrounded = true;
    private bool isPaused;
    private bool isAiming;

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        rigidBody.freezeRotation = true;
        Cursor.lockState = CursorLockMode.Locked;

        if (!isLocalPlayer)
        {
            MainCamera.enabled = false;
        }
    }

    void Update()
    {
        if (!isLocalPlayer)
		{
			return;
		}

        MoveControls();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                isPaused = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                isPaused = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        GunControls();
    }

    private void MoveControls()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.position += transform.forward * Speed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            transform.position -= transform.forward * Speed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            transform.position -= transform.right * Speed * Time.deltaTime;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            transform.position += transform.right * Speed * Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rigidBody.AddForce(Vector3.up * 5, ForceMode.Impulse);
            isGrounded = false;
            Invoke("Ground", 1f);
        }
    }

    void Ground()
    {
        isGrounded = true;
    }

    private void GunControls()
    {
        if (isAiming)
        {
            if (Input.GetMouseButtonDown(0))
            {
                ShootGunLocal();
            }
            else if (Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.LeftShift))
            {
                ReleaseGun();
                isAiming = false;
            }
        }
        else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.LeftShift))
        {
            AimGun();
            isAiming = true;
        }
    }

    private void ShootGunLocal()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Player player = hit.collider.GetComponent<Player>();
            if (player != null)
            {
                DamagePlayerOnServer(player, hit.point, hit.normal);
            }
            else
            {
                HitGroundOnServer(hit.point);
            }
        }

        SyncMuzzleFlashWithServer(this);
    }

    [Command]
    private void DamagePlayerOnServer(Player player, Vector3 point, Vector3 hitDirection)
    {
        HitPlayerToClients(player, hitDirection);
        GameObject hit = Instantiate(BloodHit, point, Quaternion.identity);
        NetworkServer.Spawn(hit);
        Destroy(hit, 2.5f);
    }

    [Command]
    private void HitGroundOnServer(Vector3 point)
    {
        GameObject hit = Instantiate(GroundHit, point, Quaternion.identity);
        NetworkServer.Spawn(hit);
        Destroy(hit, 2.5f);
    }

    [Command]
    private void SyncMuzzleFlashWithServer(Player player)
    {
        FireGunToClients(player);
    }

    [ClientRpc]
    private void FireGunToClients(Player player)
    {
        player.GetComponentInChildren<Gun>().ShootGun();
    }

    [ClientRpc]
    private void HitPlayerToClients(Player player, Vector3 hitDirection)
    {
        player.GetComponent<Rigidbody>().AddForce(-hitDirection * 5, ForceMode.Impulse);
    }

    private void ReleaseGun()
    {
        Gun.ReleaseGun();
    }

    private void AimGun()
    {
        Gun.AimGun();
    }
}
