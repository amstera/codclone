using Mirror;
using UnityEngine;

public class Player : NetworkBehaviour
{
	public float Speed = 5;
    public Camera MainCamera;
    public Gun Gun;

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
        SyncMuzzleFlashWithServer(netId);
    }

    [Command]
    private void SyncMuzzleFlashWithServer(uint id)
    {
        NetworkBehaviour[] gameObjects = FindObjectsOfType<NetworkBehaviour>();
        foreach (NetworkBehaviour nb in gameObjects)
        {
            if (nb.netId == id)
            {
                Player player = nb.GetComponent<Player>();
                if (player != null)
                {
                    FireGun(player);
                    break;
                }
            }
        }
    }

    [ClientRpc]
    private void FireGun(Player player)
    {
        player.GetComponentInChildren<Gun>().ShootGun();
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
