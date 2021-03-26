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
    public bool IsDead;

    private Rigidbody rigidBody;
    private Animator animator;
    private GameObject lowHealth;
    private bool isGrounded = true;
    private bool isPaused;
    private bool isAiming;
    private bool isWalking;

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        animator.SetInteger("AnimationState", 1);
        rigidBody.freezeRotation = true;

        if (isLocalPlayer)
        {
            lowHealth = GameObject.Find("Canvas").transform.Find("Low Health").gameObject;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            MainCamera.enabled = false;
        }
    }

    void Update()
    {
        if (!isLocalPlayer || IsDead)
		{
			return;
		}

        if (Health == 10)
        {
            lowHealth.SetActive(false);
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

    private void TakeHit(Vector3 hitDirection)
    {
        if (IsDead)
        {
            return;
        }

        Health -= 5;
        if (Health <= 0)
        {
            Die(hitDirection);
        }
        else
        {
            //rigidBody.AddForce(-hitDirection * 7.5f, ForceMode.Impulse);
            if (Health <= 5 && isLocalPlayer)
            {
                lowHealth.SetActive(true);
            }
        }
    }

    private void Die(Vector3 hitDirection)
    {
        animator.enabled = false;
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = false;
            rb.AddForce(-hitDirection * 2.5f, ForceMode.Impulse);
        }
        rigidBody.isKinematic = true;
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
        }
        GetComponent<BoxCollider>().enabled = false;
        IsDead = true;
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
        player.TakeHit(hitDirection);
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
