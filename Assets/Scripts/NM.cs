using Mirror;
using UnityEngine;

public class NM : MonoBehaviour
{
    public static NM Instance;
    public GameObject PlayerPrefab;

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
}
