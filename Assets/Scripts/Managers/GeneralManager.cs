using DG.Tweening;
using System;
using UnityEngine;

public class GeneralManager : MonoBehaviour
{
    [Range(0f, 2f)]
    [SerializeField] private float timeScale = 1f;
    [SerializeField] private int fps = 120;
    [SerializeField] private int dotweenLimit = 1500;

    void Awake() {
        Application.targetFrameRate = fps;
        DOTween.SetTweensCapacity(dotweenLimit, dotweenLimit);
    }

    void Update()
    {
        Time.timeScale = timeScale;
    }
}
