using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
	public float Speed = 5;
    public Camera MainCamera;
    public Gun Gun;
    public FlameThrower FlameThrower;
    public TextMesh TeamText;
    public GameObject PointsText;
    public Camera MiniMap;
    [SyncVar(hook = "ChangeWeapon")]
    public Weapon ActiveWeapon;

    [Header("Particles")]
    public GameObject BloodHit;
    public GameObject GroundHit;
    public GameObject Explosion;

    [Header("Properties")]
    public int Health = 10;
    [SyncVar]
    public bool IsDead;
    [SyncVar(hook = "UpdateTeamColorText")]
    public TeamColor TeamColor;
    public float IsBurningTime;

    [Header("Sounds")]
    public AudioSource Walking;
    public AudioSource Hit;
    public AudioSource HealthRestore;
    public AudioSource Detonate;
    public AudioSource ChangeWeaponSound;

    private Rigidbody rigidBody;
    private Animator animator;
    private GameObject lowHealth;
    private GameObject detonateText;

    private bool isGrounded = true;
    private bool isPaused;
    private bool isAiming;
    private bool isWalking;
    private float timeSinceLastGunShot;
    private int killCount;
    private int killsSinceBombDrop;
    private int requiredPointsToWin = 50;
    private bool isUsingFlameThrower;

    private void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        animator.SetInteger("AnimationState", 1);
        rigidBody.freezeRotation = true;
        ChangeWeapon(Weapon.None, ActiveWeapon);

        if (isLocalPlayer)
        {
            SetTeamColorOnServer(this);
            Transform canvas = GameObject.Find("Canvas").transform;
            lowHealth = canvas.Find("Low Health").gameObject;
            detonateText = canvas.Find("Detonate Text").gameObject;
            detonateText.SetActive(false);
            UpdateScore();
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            MainCamera.enabled = false;
            MiniMap.enabled = false;
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

        if (ActiveWeapon == Weapon.FlameThrower && FlameThrower.Fuel <= 0)
        {
            isUsingFlameThrower = false;
            FlameThrower.HideFlame();
            ChangeWeaponOnServer(this, Weapon.Gun);
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

        if (Input.GetKeyDown(KeyCode.A) && killsSinceBombDrop >= 3)
        {
            DetonateBomb();
        }

        GunControls();

        if (isUsingFlameThrower)
        {
            CheckForFlameDamage();
        }

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

            if (Manager.Instance.BluePoints == requiredPointsToWin || Manager.Instance.RedPoints == requiredPointsToWin)
            {
                EndGame();
            }
        }
    }

    public void ResetHealth()
    {
        if (isLocalPlayer)
        {
            HealthRestore.Play();
            ResetHealthToServer(this);
        }
    }

    private void EndGame()
    {
        IsDead = true;
        Manager.Instance.ShowFinalScore(TeamColor);
        Invoke("ResetServer", 4.5f);
    }

    private void MoveControls()
    {
        isWalking = true;
        if (Input.GetKey(KeyCode.W))
        {
            if (!Walking.isPlaying)
            {
                UpdateWalkingSoundOnServer(this, true);
            }
            transform.position += transform.forward * Speed * Time.deltaTime;
        }
        else
        {
            if (Walking.isPlaying)
            {
                UpdateWalkingSoundOnServer(this, false);
            }
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
                if (ActiveWeapon == Weapon.Gun)
                {
                    if (Time.time - timeSinceLastGunShot > 0.25f)
                    {
                        ShootGunLocal();
                        timeSinceLastGunShot = Time.time;
                    }
                }
                else if (ActiveWeapon == Weapon.FlameThrower)
                {
                    ShootFlamethrowerLocal();
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (ActiveWeapon == Weapon.FlameThrower)
                {
                    StopFlamethrowerLocal();
                }
            }
            else if (Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.LeftShift))
            {
                if (ActiveWeapon == Weapon.FlameThrower)
                {
                    StopFlamethrowerLocal();
                }
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
                    if (player.Health <= 5)
                    {
                        IncreaseKillCount();
                    }
                }
            }
            else
            {
                HitGroundOnServer(hit.point);
            }
        }

        SyncMuzzleFlashWithServer(this);
    }

    private void ShootFlamethrowerLocal()
    {
        isUsingFlameThrower = true;
        SyncUseFlameThrowerWithServer(this, true);
    }

    private void StopFlamethrowerLocal()
    {
        isUsingFlameThrower = false;
        SyncUseFlameThrowerWithServer(this, false);
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

        Hit.Play();

        TakeDamage(5, hitDirection);
    }

    private void TakeFireDamage()
    {
        if (IsDead)
        {
            return;
        }

        if (Health <= 3)
        {
            Hit.Play();
        }

        TakeDamage(3, Vector3.one);
    }

    private void TakeDamage(int amount, Vector3 hitDirection)
    {
        Health -= amount;
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
                AddPointToServer(Manager.Instance, TeamColor.Blue, transform.position);
            }
            else
            {
                AddPointToServer(Manager.Instance, TeamColor.Red, transform.position);
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

    private void DetonateBomb()
    {
        Detonate.Play();
        DetonateBombFromServer(this);
        killsSinceBombDrop = 0;
        detonateText.SetActive(false);
    }

    private void ChangeWeapon(Weapon oldValue, Weapon newValue)
    {
        ChangeWeaponSound.Play();

        Gun.gameObject.SetActive(false);
        FlameThrower.gameObject.SetActive(false);

        if (newValue == Weapon.Gun)
        {
            Gun.gameObject.SetActive(true);
        }
        else if (newValue == Weapon.FlameThrower)
        {
            FlameThrower.gameObject.SetActive(true);
        }
    }

    private void CheckForFlameDamage()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            Player player = hit.collider.GetComponent<Player>();
            if (player != null)
            {
                if (player.TeamColor != TeamColor && (Time.time - IsBurningTime) >= 0.15f)
                {
                    IsBurningTime = Time.time;
                    FireDamagePlayerOnServer(player);
                    if (player.Health <= 3)
                    {
                        IncreaseKillCount();
                    }
                }
            }
        }
    }

    private void IncreaseKillCount()
    {
        killCount++;
        killsSinceBombDrop++;
        if (killsSinceBombDrop >= 3)
        {
            detonateText.SetActive(true);
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
    private void FireDamagePlayerOnServer(Player player)
    {
        FireDamagePlayerToClients(player);
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
    private void SyncUseFlameThrowerWithServer(Player player, bool useFlame)
    {
        UseFlameThrowerToClients(player, useFlame);
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
    private void AddPointToServer(Manager manager, TeamColor color, Vector3 position)
    {
        if (color == TeamColor.Red)
        {
            manager.RedPoints += 10;
        }
        else
        {
            manager.BluePoints += 10;
        }

        GameObject pointsText = Instantiate(PointsText, position, Quaternion.identity);
        NetworkServer.Spawn(pointsText);
        Destroy(pointsText, 3);
    }

    private void RespawnPlayer()
    {
        RespawnPlayer(this);
    }

    private void ResetServer()
    {
        Manager.Instance.HideFinalScore();
        ResetServerOnServer();
        RespawnPlayer();
    }

    [Command]
    private void RespawnPlayer(Player player)
    {
        NetworkConnection conn = player.connectionToClient;
        GameObject newPlayer = Instantiate(Manager.Instance.PlayerPrefab, NetworkManager.singleton.GetStartPosition().position, Quaternion.identity);
        newPlayer.GetComponent<Player>().TeamColor = TeamColor;
        NetworkServer.ReplacePlayerForConnection(conn, newPlayer, true);
        NetworkServer.Destroy(player.gameObject);
    }

    [Command]
    private void ResetHealthToServer(Player player)
    {
        UpdatePlayerHealthOnClients(player);
    }

    [Command]
    private void DetonateBombFromServer(Player player)
    {
        Vector3 pos = Vector3.one;
        foreach (Player p in FindObjectsOfType<Player>())
        {
            if (p.TeamColor != player.TeamColor)
            {
                pos = p.transform.position;
                break;
            }
        }

        GameObject explosion = Instantiate(Explosion, pos, Quaternion.identity);
        NetworkServer.Spawn(explosion);
        Destroy(explosion, 3.5f);

        DetonateBombToClients(player.TeamColor, pos);
    }

    [Command]
    private void UpdateWalkingSoundOnServer(Player player , bool playWalkingSound)
    {
        UpdateWalkingSoundToClients(player, playWalkingSound);
    }

    [Command]
    private void ResetServerOnServer()
    {
        Manager.Instance.RedPoints = 0;
        Manager.Instance.BluePoints = 0;
    }

    [Command]
    public void ChangeWeaponOnServer(Player player, Weapon weapon)
    {
        player.ActiveWeapon = weapon;
    }

    [ClientRpc]
    private void FireGunToClients(Player player)
    {
        player.GetComponentInChildren<Gun>().ShootGun();
    }

    [ClientRpc]
    private void UseFlameThrowerToClients(Player player, bool useFlame)
    {
        FlameThrower flameThrower = player.GetComponentInChildren<FlameThrower>();
        if (flameThrower != null)
        {
            if (useFlame)
            {
                flameThrower.UseFlame();
            }
            else
            {
                flameThrower.HideFlame();
            }
        }
    }

    [ClientRpc]
    private void HitPlayerToClients(Player player, Vector3 hitDirection)
    {
        player.TakeHit(hitDirection);
    }

    [ClientRpc]
    private void FireDamagePlayerToClients(Player player)
    {
        player.TakeFireDamage();
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

    [ClientRpc]
    private void UpdatePlayerHealthOnClients(Player player)
    {
        player.Health = 10;
    }

    [ClientRpc]
    private void DetonateBombToClients(TeamColor teamColor, Vector3 pos)
    {
        foreach (Player player in FindObjectsOfType<Player>())
        {
            if (player.TeamColor != teamColor && Vector3.Distance(player.transform.position, pos) < 10)
            {
                player.Die(Vector3.one);
            }
        }
    }

    [ClientRpc]
    private void UpdateWalkingSoundToClients(Player player, bool playWalkingSound)
    {
        if (playWalkingSound)
        {
            player.Walking.Play();
        }
        else
        {
            player.Walking.Stop();
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

public enum Weapon
{
    None = 0,
    Gun = 1,
    FlameThrower = 2
}
