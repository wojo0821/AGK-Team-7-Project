using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StoryImage : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Sprite[] images;
    [SerializeField] private float NextImageDelay;

    private Coroutine coroutine;
    private void Start()
    {
        coroutine = StartCoroutine(NextImage());
    }
    private IEnumerator NextImage()
    {
        for (int i = 0; i < images.Length; i++)
        {
            image.sprite = images[i];
            yield return new WaitForSeconds(NextImageDelay);
        }

        this.gameObject.SetActive(false);
    }
}
