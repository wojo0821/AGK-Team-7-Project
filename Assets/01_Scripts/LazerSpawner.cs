using System.Collections;
using UnityEngine;

public class LazerSpawner : MonoBehaviour
{
    [Header("Lazer")]
    [SerializeField] private GameObject lazer;
    [Header("Lazer Settings")]
    [SerializeField] private bool isAlwaysOn = true;
    [SerializeField] private bool isAutoRotation = false;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float onTime = 2f;
    [SerializeField] private float offTime = 2f;
    [SerializeField] private Vector2 lazerSize;
    [SerializeField] private float zDirection;

    private GameObject newLazer;

    private void Start()
    {
        if (isAlwaysOn)
        {
            LazerOn();
        }
        else
        {
            StartCoroutine(LazerRoutine());
        }
    }
    private void FixedUpdate()
    {
        if (isAutoRotation)
        {
            newLazer.transform.Rotate(0, 0, rotationSpeed * Time.fixedDeltaTime);
        }
    }
    IEnumerator LazerRoutine()
    {
        while (true)
        {
            LazerOn();
            yield return new WaitForSeconds(onTime);
            LazerOff();
            yield return new WaitForSeconds(offTime);
        }
    }
    private void LazerOn()
    {
        newLazer = Instantiate(lazer, new Vector3(transform.position.x, transform.position.y, 0), Quaternion.Euler(0, 0, zDirection));
        newLazer.transform.localScale = new Vector3(lazerSize.x, lazerSize.y, 1);
    }
    private void LazerOff()
    {
        Destroy(newLazer);
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        // 1. 기존 기즈모 좌표계 백업
        Matrix4x4 oldMatrix = Gizmos.matrix;
        // 2. 현재 오브젝트 위치 + zDirection 회전 적용된 좌표계로 설정
        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.Euler(0, 0, zDirection), Vector3.one);
        // 3. 로컬 좌표 기준 (가로길이 / 2) 만큼 오른쪽으로 중심 이동
        Vector3 center = new Vector3(lazerSize.x / 2f, 0f, 0f);
        Gizmos.DrawWireCube(center, lazerSize - new Vector2(0, 0.75f)); //레이저 크기 일치를 위해 -0.75
        // 4. 좌표계 원상복구
        Gizmos.matrix = oldMatrix;
    }
}
