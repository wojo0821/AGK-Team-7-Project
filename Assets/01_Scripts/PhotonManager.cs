using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PhotonView))]
public class PhotonManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI readyStatusText;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;
    [SerializeField] private GameObject readyUIPanel;

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
        SetReadyStatus(false);
        UpdateReadyUI();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateReadyUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateReadyUI();
        if (PhotonNetwork.IsMasterClient && !hasSpawned)
        {
            CheckAllPlayersReady();
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("IsReady"))
        {
            UpdateReadyUI();

            if (PhotonNetwork.IsMasterClient && !hasSpawned)
            {
                CheckAllPlayersReady();
            }
        }
    }

    public void OnClickReady()
    {
        if (!PhotonNetwork.InRoom || hasSpawned) return;

        isReady = !isReady;
        SetReadyStatus(isReady);

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

    private void CheckAllPlayersReady()
    {
        if (PhotonNetwork.PlayerList.Length == 0) return;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.TryGetValue("IsReady", out object ready) || !(bool)ready)
            {
                return;
            }
        }

        if (pv != null)
        {
            pv.RPC(nameof(RPC_SpawnAllPlayers), RpcTarget.All);
        }
    }

    [PunRPC]
    private void RPC_SpawnAllPlayers()
    {
        if (hasSpawned) return;
        hasSpawned = true;

        if (readyUIPanel != null)
        {
            readyUIPanel.SetActive(false);
        }

        SpawnPlayer();

        // ★ 전원 준비 완료로 게임 시작될 때 용암 시작!
        FindFirstObjectByType<Lava>()?.StartLava();
    }

    public void SpawnPlayer()
    {
        Vector2 playerPos = new Vector2(Random.Range(-5f, 5f), 0);
        GameObject player = PhotonNetwork.Instantiate("Player", playerPos, Quaternion.identity);
        player.GetComponent<SpriteRenderer>().color = ChooseCharacterStyle.CharacterColor;
    }
}
