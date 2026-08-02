using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonManager : MonoBehaviourPunCallbacks
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });
    }
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("Create Room Failed" + message + returnCode);
    }
    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room");
        SpawnPlayer();
    }
    void SpawnPlayer()
    {
        Vector2 playerPos = new Vector2(Random.Range(-5f, 5f), 0);
        PhotonNetwork.Instantiate("Player", playerPos, Quaternion.identity);
    }
}
