using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ChooseCharacterStyle : MonoBehaviour
{
    Image childImage;
    public static Color playerColor;
    private void Awake()
    {
        childImage = transform.GetChild(0).GetComponent<Image>();
    }
    public void OnClick()
    {
        playerColor = childImage.color;
        Debug.Log("Player Color: " + playerColor);
        SceneManager.LoadScene("SampleScene");
    }
}
