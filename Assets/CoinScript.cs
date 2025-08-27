using UnityEngine;

public class CoinScript : MonoBehaviour
{
    
    private void OnTriggerEnter(Collider other) {
        PlayerHandler player = other.GetComponent<PlayerHandler>();
        if (player != null) {
            player.IncreasePoints(1);
            Destroy(gameObject);
        }
    }

}
