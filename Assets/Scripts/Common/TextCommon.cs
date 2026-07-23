using TMPro;
using UnityEngine;

public class TextCommon : MonoBehaviour
{
    [SerializeField] private TMP_Text front;
    [SerializeField] private TMP_Text back;

    public void SetText(string text) {

        front.text = text;
        back.text = text;
    }
}
