using UnityEngine;

public class RacingKillTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        GameObject gameObject = other.gameObject;
        if (gameObject.layer != 8)
        {
            return;
        }
        gameObject = gameObject.transform.root.gameObject;
        if (IN_GAME_MAIN_CAMERA.Gametype == GameType.Multiplayer && gameObject.GetPhotonView() != null && gameObject.GetPhotonView().isMine)
        {
            HERO component = gameObject.GetComponent<HERO>();
            if (component != null)
            {
                component.MarkDead();
                component.photonView.RPC("netDie2", PhotonTargets.All, -1, "Server");
            }
        }
    }
}