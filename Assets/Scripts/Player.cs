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
    public int Health = 10;

    private Rigidbody rigidBody;
    private Animator animator;
    private GameObject lowHealth;
    private bool isGrounded = true;
    private bool isPaused;
    private bool isAiming;
    private bool isWalking;
    private bool isDead;

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        lowHealth = GameObject.Find("Canvas").transform.Find("Low Health").gameObject;
        animator.SetInteger("AnimationState", 1);
        rigidBody.freezeRotation = true;
        Cursor.lockState = CursorLockMode.Locked;

        if (!isLocalPlayer)
        {
            MainCamera.enabled = false;
        }
    }

    void Update()
    {
        CheckHealth();

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

        UpdateAnimations();
    }

    private void MoveControls()
    {
        isWalking = true;
        if (Input.GetKey(KeyCode.W))
        {
            transform.position += transform.forward * Speed * Time.deltaTime;
        }
        else
        {
            isWalking = false;
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

    private void UpdateAnimations()
    {
        if (isAiming)
        {
            if (isWalking)
            {
                UpdateAnimationState(3);
            }
            else
            {
                UpdateAnimationState(2);
            }
        }
        else
        {
            if (isWalking)
            {
                UpdateAnimationState(0);
            }
            else
            {
                UpdateAnimationState(1);
            }
        }
    }

    private void UpdateAnimationState(int id)
    {
        if (animator.GetInteger("AnimationState") != id)
        {
            UpdateAnimationOnServer(this, id);
        }
    }

    private void CheckHealth()
    {
        if (Health <= 0 && !isDead)
        {
            animator.enabled = false;
            //die
            isDead = true;
        }
        else if (isLocalPlayer)
        {
            if (Health <= 5)
            {
                lowHealth.SetActive(true);
            }
            else
            {
                lowHealth.SetActive(false);
            }
        }
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

    [Command]
    private void UpdateAnimationOnServer(Player player, int id)
    {
        UpdateAnimationToClients(player, id);
    }

    [ClientRpc]
    private void FireGunToClients(Player player)
    {
        player.GetComponentInChildren<Gun>().ShootGun();
    }

    [ClientRpc]
    private void HitPlayerToClients(Player player, Vector3 hitDirection)
    {
        player.Health -= 5;
        player.GetComponent<Rigidbody>().AddForce(-hitDirection * 5, ForceMode.Impulse);
    }

    [ClientRpc]
    private void UpdateAnimationToClients(Player player, int id)
    {
        player.animator.SetInteger("AnimationState", id);
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
