using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class FlameThrowerPickUp : NetworkBehaviour
{
    public List<Vector3> SpawnPositions;

    void Start()
    {
        if (isServer)
        {
            transform.position = SpawnPositions[Random.Range(0, SpawnPositions.Count)];
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Player player = collision.collider.GetComponent<Player>();
        if (player != null)
        {
            if (player.ActiveWeapon != Weapon.FlameThrower)
            {
                player.ChangeWeaponOnServer(player, Weapon.FlameThrower);
                NetworkServer.Destroy(gameObject);
            }
        }
    }
}
