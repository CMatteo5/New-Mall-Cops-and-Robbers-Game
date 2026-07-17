using UnityEngine;

public class JailManager : MonoBehaviour
{
    public static JailManager Instance;

    [SerializeField] private Transform jailSpawnPoint;

    private void Awake()
    {
        Instance = this;
    }

    public Vector3 JailPosition => jailSpawnPoint != null ?
        jailSpawnPoint.position : Vector3.zero;
}