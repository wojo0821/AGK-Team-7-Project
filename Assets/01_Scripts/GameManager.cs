using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Lava lava;
    [SerializeField] private TextMeshProUGUI leaderboardText; // 리더보드 텍스트 UI

    [Header("Lava Speed Settings")]
    [SerializeField] private float baseSpeed = 0.5f;       // 기본 용암 속도
    [SerializeField] private float speedMultiplier = 0.5f; // 거리당 가속 배율
    private float scoreTimer;

    private void Awake()
    {
        if (lava == null) lava = FindFirstObjectByType<Lava>();
    }

    private void Update()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (players.Length == 0) return;

        // 1. 1등 플레이어 & 최고/최저 높이 찾기
        PlayerController topPlayer = players[0];
        float maxY = float.MinValue, minY = float.MaxValue;

        foreach (var p in players)
        {
            float y = p.transform.position.y;
            if (y > maxY) { maxY = y; topPlayer = p; }
            if (y < minY) { minY = y; }
        }

        // 2. 높이 차이만큼 용암 속도 조절
        if (lava != null)
        {
            float speed = baseSpeed + (Mathf.Max(0, maxY - minY) * speedMultiplier);
            lava.SetRiseSpeed(Mathf.Clamp(speed, baseSpeed, 15f));
        }

        // 3. 1초마다 1등에게 점수 +1 지급 (방장만 실행)
        scoreTimer += Time.deltaTime;
        if (PhotonNetwork.IsMasterClient && scoreTimer >= 1f)
        {
            scoreTimer = 0f;
            PhotonView pv = topPlayer.GetComponent<PhotonView>();
            if (pv != null && pv.Owner != null)
            {
                int score = (int)(pv.Owner.CustomProperties["Score"] ?? 0);
                pv.Owner.SetCustomProperties(new Hashtable { { "Score", score + 1 } });
            }
        }
    }

    // 4. 점수가 바뀌면 리더보드 텍스트 갱신
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (leaderboardText == null || !changedProps.ContainsKey("Score")) return;

        string result = "<b>실시간 점수</b>\n";
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            int score = (int)(p.CustomProperties["Score"] ?? 0);
            string isMe = (p == PhotonNetwork.LocalPlayer) ? " (나)" : "";
            result += $"{p.NickName}{isMe}: {score}점\n";
        }
        leaderboardText.text = result;
    }
}
