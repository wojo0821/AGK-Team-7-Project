using UnityEngine;

public class GameManager : MonoBehaviour
{

    bool isGameStart = false;
    [Header("Lava Reference")]
    [SerializeField] private Lava lava; // 씬의 Lava 오브젝트 연결

    [Header("Lava Speed Balancing")]
    [SerializeField] private float baseSpeed = 0.5f;          // 기본 용암 속도 (차이가 없을 때)
    [SerializeField] private float speedMultiplier = 0.1f;    // 높이 차이 1칸당 빨라질 속도
    [SerializeField] private float maxLavaSpeed = 10f;        // 용암의 최대 속도 제한

    [Header("Debug Info")]
    [SerializeField] private float currentHeightDiff;         // 현재 1등과 꼴등의 높이 차이
    [SerializeField] private float currentLavaSpeed;          // 현재 적용된 용암 속도

    private void Update()
    {
        AdjustLavaSpeedByPlayerHeights();
    }

    private void AdjustLavaSpeedByPlayerHeights()
    {
        if (lava == null) return;

        // 씬에 생성된 모든 PlayerController 검색
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        // 플레이어가 1명만 있으면 기본 속도로 적용
        if (players.Length == 1)
        {
            currentHeightDiff = 0f;
            currentLavaSpeed = baseSpeed;
            lava.SetRiseSpeed(baseSpeed);

            return;
        }

        // 가장 높은 Y값과 가장 낮은 Y값 찾기
        float maxY = float.MinValue;
        float minY = float.MaxValue;

        foreach (PlayerController p in players)
        {
            float playerY = p.transform.position.y;
            if (playerY > maxY) maxY = playerY;
            if (playerY < minY) minY = playerY;
        }

        // 1등과 꼴등의 높이 차이 계산
        currentHeightDiff = Mathf.Max(0f, maxY - minY);

        // 속도 계산: 기본속도 + (높이 차이 * 가속 배율)
        currentLavaSpeed = baseSpeed + (currentHeightDiff * speedMultiplier);

        // 속도가 너무 빨라지지 않도록 최대값 제한(Clamp)
        currentLavaSpeed = Mathf.Clamp(currentLavaSpeed, baseSpeed, maxLavaSpeed);

        // Lava에 새로운 속도 적용
        lava.SetRiseSpeed(currentLavaSpeed);
    }
}
