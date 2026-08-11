using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using ExitGames.Client.Photon;

public class ChooseCharacterStyle : MonoBehaviourPunCallbacks
{
    Image childImage;
    public static Color CharacterColor;
    [SerializeField] private int styleId;

    private void Start()
    {
        childImage = transform.GetChild(0).GetComponent<Image>();
    }

    public void OnClick()
    {
        CharacterColor = childImage.color;
        SceneManager.LoadScene("SampleScene");
    }
}
