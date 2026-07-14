using UnityEngine;

public class GeneralManager : MonoBehaviour
{
    [Range(0f, 2f)]
    [SerializeField] private float timeScale = 1f;

    // Update is called once per frame
    void Update()
    {
        Time.timeScale = timeScale;
    }
}
