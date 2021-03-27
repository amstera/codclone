using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
	public float Speed = 5;
    public Camera MainCamera;
    public Gun Gun;
    public TextMesh TeamText;

    [Header("Particles")]
    public GameObject BloodHit;
    public GameObject GroundHit;

    [Header("Properties")]
    public int Health = 10;
    [SyncVar]
    public bool IsDead;
    [SyncVar(hook = "UpdateTeamColorText")]
    public TeamColor TeamColor;

    private Rigidbody rigidBody;
    private Animator animator;
    private GameObject lowHealth;

    private bool isGrounded = true;
    private bool isPaused;
    private bool isAiming;
    private bool isWalking;
    private float timeSinceLastGunShot;

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        animator.SetInteger("AnimationState", 1);
        rigidBody.freezeRotation = true;

        if (isLocalPlayer)
        {
            SetTeamColorOnServer(this);
            Transform canvas = GameObject.Find("Canvas").transform;
            lowHealth = canvas.Find("Low Health").gameObject;
            UpdateScore();
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            MainCamera.enabled = false;
            if (IsDead)
            {
                Die(Vector3.one);
            }
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

    public void UpdateScore()
    {
        if (isLocalPlayer)
        {
            Transform canvas = GameObject.Find("Canvas").transform;
            Text blueScoreText = canvas.Find("Blue Score Text").GetComponent<Text>();
            Text redScoreText = canvas.Find("Red Score Text").GetComponent<Text>();

            redScoreText.text = Manager.Instance.RedPoints.ToString();
            blueScoreText.text = Manager.Instance.BluePoints.ToString();
        }
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
            if (Input.GetMouseButtonDown(0) && Time.time - timeSinceLastGunShot > 0.25f)
            {
                ShootGunLocal();
                timeSinceLastGunShot = Time.time;
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
                if (player.TeamColor != TeamColor)
                {
                    DamagePlayerOnServer(player, hit.point, hit.normal);
                }
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

        if (isLocalPlayer)
        {
            if (TeamColor == TeamColor.Red)
            {
                AddPointToServer(Manager.Instance, TeamColor.Blue);
            }
            else
            {
                AddPointToServer(Manager.Instance, TeamColor.Red);
            }

            Invoke("RespawnPlayer", 2.5f);
        }
    }

    private void UpdateTeamColorText(TeamColor oldValue, TeamColor newValue)
    {
        TeamText.text = newValue.ToString();
        TeamText.color = newValue == TeamColor.Red ? Color.red : Color.blue;

        if (isLocalPlayer)
        {
            Transform canvas = GameObject.Find("Canvas").transform;
            Text teamUpperText = canvas.Find("Team Text").GetComponent<Text>();
            teamUpperText.text = $"{newValue} Team";
            teamUpperText.color = TeamText.color;
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
    private void SetTeamColorOnServer(Player player)
    {
        if (player.TeamColor == TeamColor.None)
        {
            Player[] players = FindObjectsOfType<Player>();
            int bluePlayers = players.Count(p => p.TeamColor == TeamColor.Blue);
            int redPlayers = players.Count(p => p.TeamColor == TeamColor.Red);
            if (bluePlayers > redPlayers)
            {
                player.TeamColor = TeamColor.Red;
            }
            else if (redPlayers > bluePlayers)
            {
                player.TeamColor = TeamColor.Blue;
            }
            else
            {
                player.TeamColor = Random.Range(0, 2) == 1 ? TeamColor.Red : TeamColor.Blue;
            }
        }

        UpdateTeamColorOnClients();
    }

    [Command]
    private void UpdateAnimationOnServer(Player player, int id)
    {
        UpdateAnimationToClients(player, id);
    }

    [Command]
    private void AddPointToServer(Manager manager, TeamColor color)
    {
        if (color == TeamColor.Red)
        {
            manager.RedPoints++;
        }
        else
        {
            manager.BluePoints++;
        }
    }

    private void RespawnPlayer()
    {
        RespawnPlayer(this);
    }

    [Command]
    private void RespawnPlayer(Player player)
    {
        NetworkConnection conn = player.connectionToClient;
        GameObject newPlayer = Instantiate(NM.Instance.PlayerPrefab, NetworkManager.singleton.GetStartPosition().position, Quaternion.identity);
        newPlayer.GetComponent<Player>().TeamColor = TeamColor;
        NetworkServer.Destroy(player.gameObject);
        NetworkServer.ReplacePlayerForConnection(conn, newPlayer, true);
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

    [ClientRpc]
    private void UpdateTeamColorOnClients()
    {
        foreach (Player player in FindObjectsOfType<Player>())
        {
            player.TeamText.text = player.TeamColor.ToString();
            player.TeamText.color = player.TeamColor == TeamColor.Red ? Color.red : Color.blue;
        }
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

public enum TeamColor
{
    None = 0,
    Red = 1,
    Blue = 2
}
