using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PhotonView))]
public class LavaKill : MonoBehaviourPunCallbacks
{
    [SerializeField] private string menuSceneName = "MainMenu"; // 이동할 씬 이름
    private bool isGameOver = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isGameOver) return;

        // 1. 닿은 대상이 플레이어인지 확인
        PlayerController player = collision.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            // 2. 내 캐릭터가 닿았을 때만 전원에게 게임 종료 RPC 전송
            PhotonView playerPv = player.GetComponent<PhotonView>();
            if (playerPv != null && playerPv.IsMine)
            {
                isGameOver = true;
                photonView.RPC(nameof(RPC_GameOverAndLeave), RpcTarget.All);
            }
        }
    }

    [PunRPC]
    private void RPC_GameOverAndLeave()
    {
        isGameOver = true;
        Debug.Log("게임 오버! 메인 메뉴로 즉시 복귀합니다.");

        // 1. 포톤 룸 퇴장
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        // 2. 기다리지 않고 즉시 메인 메뉴 씬 로드 (멈춤 버그 방지)
        SceneManager.LoadScene(menuSceneName);
    }
}
