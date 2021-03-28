using Mirror;
using UnityEngine;

public class HealthKit : NetworkBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        Player player = collision.collider.GetComponent<Player>();
        if (player != null)
        {
            if (player.Health < 10)
            {
                player.ResetHealth();
                NetworkServer.Destroy(gameObject);
            }
        }
    }
}
