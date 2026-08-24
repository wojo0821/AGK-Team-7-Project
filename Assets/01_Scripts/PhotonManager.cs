using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro; // TextMeshPro를 사용할 경우 (일반 Text라면 UnityEngine.UI 사용)
using UnityEngine;
using UnityEngine.UI;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI readyStatusText; // "준비 완료: 0 / 0" 텍스트
    [SerializeField] private Button readyButton;              // 준비 버튼
    [SerializeField] private TextMeshProUGUI readyButtonText; // 버튼 텍스트 ("준비" / "준비 취소")
    [SerializeField] private GameObject readyUIPanel;         // 준비 UI 전체 패널 (게임 시작 시 숨김용)

    private PhotonView pv;
    private bool isReady = false;
    private bool hasSpawned = false;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
    }

    void Start()
    {
        PhotonNetwork.SendRate = 30;
        PhotonNetwork.SerializationRate = 30;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("Join Random Room Failed, Creating new Room");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 10 });
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("Create Room Failed" + message + returnCode);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room");
        // 방에 들어왔을 때 내 초기 상태(IsReady = false) 설정 및 UI 갱신
        SetReadyStatus(false);
        UpdateReadyUI();
    }

    // 플레이어가 새로 들어왔을 때 UI 갱신
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateReadyUI();
    }

    // 플레이어가 나갔을 때 UI 갱신 및 방장이 체크
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateReadyUI();
        if (PhotonNetwork.IsMasterClient && !hasSpawned)
        {
            CheckAllPlayersReady();
        }
    }

    // 누군가 준비 상태(CustomProperties)를 바꿨을 때 자동 호출
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("IsReady"))
        {
            UpdateReadyUI();

            // 방장(MasterClient)이 전원 준비 완료 여부 검사
            if (PhotonNetwork.IsMasterClient && !hasSpawned)
            {
                CheckAllPlayersReady();
            }
        }
    }

    // UI '준비' 버튼 OnClick에 연결할 함수
    public void OnClickReady()
    {
        if (!PhotonNetwork.InRoom || hasSpawned) return;

        isReady = !isReady; // 준비 상태 토글
        SetReadyStatus(isReady);

        // 버튼 텍스트 변경
        if (readyButtonText != null)
        {
            readyButtonText.text = isReady ? "준비 취소" : "준비";
        }
    }

    private void SetReadyStatus(bool ready)
    {
        Hashtable props = new Hashtable { { "IsReady", ready } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    // 준비된 플레이어 수 계산 및 UI 텍스트 갱신
    private void UpdateReadyUI()
    {
        if (!PhotonNetwork.InRoom) return;

        int readyCount = 0;
        int totalPlayers = PhotonNetwork.PlayerList.Length;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.TryGetValue("IsReady", out object ready) && (bool)ready)
            {
                readyCount++;
            }
        }

        if (readyStatusText != null)
        {
            readyStatusText.text = $"준비 현황: {readyCount} / {totalPlayers}";
        }
    }

    // 방장만 검사: 전원 준비 완료 시 시작
    private void CheckAllPlayersReady()
    {
        // 최소 1명 이상 있고 전원 준비 상태인지 확인
        if (PhotonNetwork.PlayerList.Length == 0) return;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.TryGetValue("IsReady", out object ready) || !(bool)ready)
            {
                return; // 아직 준비 안 된 플레이어가 있음
            }
        }

        // 전원 준비 완료 -> 모든 클라이언트에서 스폰 RPC 실행
        pv.RPC(nameof(RPC_SpawnAllPlayers), RpcTarget.All);
    }

    [PunRPC]
    private void RPC_SpawnAllPlayers()
    {
        if (hasSpawned) return;
        hasSpawned = true;

        // 준비 UI 창 숨기기
        if (readyUIPanel != null)
        {
            readyUIPanel.SetActive(false);
        }

        SpawnPlayer();
    }

    public void SpawnPlayer()
    {
        Vector2 playerPos = new Vector2(Random.Range(-5f, 5f), 0);
        GameObject player = PhotonNetwork.Instantiate("Player", playerPos, Quaternion.identity);
        player.GetComponent<SpriteRenderer>().color = ChooseCharacterStyle.CharacterColor;
    }
}
