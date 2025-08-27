using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private BoxCollider groundCollider;
    [SerializeField] private int coinAmount;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Spawn(coinAmount);
    }


    public void Spawn(int amount = 1)
    {
        if (coinPrefab == null || groundCollider == null) {
            Debug.LogWarning("[CoinSpawner] Missing coinPrefab or groundCollider.");
            return;
        }

        for (int i = 0; i < amount; i++) {
            Vector3 pos = RandomPointInBounds(groundCollider.bounds);
            Instantiate(coinPrefab, pos, Quaternion.identity);
        }
    }

    public static  Vector3 RandomPointInBounds(Bounds bounds) {
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            1f,
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }

    // Update is called once per frame
    // void Update()
    // {
        
    // }
}
