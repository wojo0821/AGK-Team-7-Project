using Photon.Pun;
using UnityEngine;

// IPunObservable을 상속받으면 PhotonView가 이 컴포넌트를 실시간 동기화해 줍니다.
public class PlayerColorSync : MonoBehaviourPun, IPunObservable
{
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // 내가 생성한 내 캐릭터라면, 선택한 캐릭터 색상을 내 몸통에 적용
        if (photonView.IsMine && spriteRenderer != null)
        {
            spriteRenderer.color = ChooseCharacterStyle.CharacterColor;
        }
    }

    // 포톤 네트워크가 색상 데이터를 계속 실시간 송수신(동기화) 해주는 함수
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 내 캐릭터인 경우: 다른 사람들에게 내 색상(RGB) 데이터를 보냄
            stream.SendNext(spriteRenderer.color.r);
            stream.SendNext(spriteRenderer.color.g);
            stream.SendNext(spriteRenderer.color.b);
        }
        else
        {
            // 상대방 캐릭터인 경우: 상대방이 보낸 색상(RGB) 데이터를 수신받아 내 화면에 칠함
            float r = (float)stream.ReceiveNext();
            float g = (float)stream.ReceiveNext();
            float b = (float)stream.ReceiveNext();

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(r, g, b, 1f);
            }
        }
    }
}
