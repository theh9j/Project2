using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameEndVisualiser : MonoBehaviour
{
    [SerializeField] private CoinVisualiser coinDisplay;
    //Win section
    [Header("Game Win")]
    [SerializeField] private GameObject gameWin;
    [SerializeField] private Transform zoom;

    [SerializeField] private Transform shineSpot;
    [SerializeField] private TMP_Text levelDisplay;
    [SerializeField] private TMP_Text coinBonus;

    [SerializeField] private Button continueButton;
    [SerializeField] private Transform wholeContinueButton;

    [SerializeField] private ParticleSystem leftConfetti;
    [SerializeField] private ParticleSystem rightConfetti;

    [SerializeField] private float spinSpeed = .15f;
    [SerializeField] private float coinAddDuration = 1.25f;
    [SerializeField] private int maxCoinAddSteps = 60;

    public event Action Advance;

    //Background
    [Header("Background")]
    [SerializeField] private GameObject background;
    public bool GamePause { get; private set; } = false;
    private Image backgroundImg;

    private LevelSaveData thisLevel = new();

    void Awake() {
        backgroundImg = background.GetComponent<Image>();
        continueButton.onClick.AddListener(() => StartCoroutine(Continue()));


    }

    void Update() {
        if (gameWin.activeSelf)
            shineSpot.rotation = Quaternion.Euler(0, 0, Time.time * spinSpeed);


    }

    public void Win(LevelSaveData data) {
        if (gameWin.activeSelf) return;
        thisLevel = data;
        zoom.gameObject.SetActive(false);
        wholeContinueButton.gameObject.SetActive(false);
        continueButton.interactable = true;

        AnimateBackground(true)
            .OnComplete(() => {
                zoom.DOKill();
                wholeContinueButton.DOKill();
                DG.Tweening.Sequence seq = DOTween.Sequence();

                seq.AppendCallback(() => leftConfetti.Play());

                seq.Append(zoom.DOScale(
                    1f,
                    .75f
                    ).From(0f)
                    .SetEase(Ease.OutBack, 2f)
                    .OnStart(() => zoom.gameObject.SetActive(true)));

                seq.AppendCallback(() => rightConfetti.Play());
                seq.AppendInterval(1f);

                seq.Append(wholeContinueButton.DOLocalMoveY(0f, .75f).From(-Screen.height * 1.5f).SetEase(Ease.OutBack, .75f)
                    .OnStart(() => wholeContinueButton.gameObject.SetActive(true)));
        });

        gameWin.SetActive(true);
        shineSpot.rotation = Quaternion.identity;

        levelDisplay.text = $"Level {data.levelId}";
        coinBonus.text = $"+{data.rewards.coins}";
        GamePause = true;
    }

    public Tween AnimateBackground(bool open) {
        if (open) {
            return backgroundImg.DOFade(.996f, .5f).From(0f)
                .OnStart(() => background.SetActive(true));
        } else {
            return backgroundImg.DOFade(0f, .5f)
                .OnComplete(() => background.SetActive(false));
        }
    }

    private IEnumerator Continue() {
        if (coinDisplay == null) yield break;
        continueButton.interactable = false;

        backgroundImg.DOFade(1f, 0f);

        Advance?.Invoke();

        zoom.DOKill();
        wholeContinueButton.DOKill();

        DG.Tweening.Sequence seq = DOTween.Sequence();

        seq.Join(
            zoom.DOScale(0f, .5f).SetEase(Ease.InBack, 2f).OnComplete(() => zoom.gameObject.SetActive(false))
            );
        
        seq.Join(
            wholeContinueButton.DOLocalMoveY(-Screen.height * 1.5f, .5f).SetEase(Ease.InBack, .75f)
            );

        seq.AppendCallback(() => gameWin.SetActive(false));
        seq.Append(AnimateBackground(false));
        
        yield return CoinAdd(thisLevel.rewards.coins);
    }

    private IEnumerator CoinAdd(int amount) {
        if (coinDisplay == null || amount <= 0) {
            yield break;
        }


        int startCoins = coinDisplay.CurrentCoins;
        int targetCoins = startCoins + amount;
        int steps = Mathf.Clamp(amount, 1, maxCoinAddSteps);
        float stepDuration = coinAddDuration / steps;

        for (int step = 1; step <= steps; step++) {
            float progress = (float)step / steps;
            float easedProgress = EaseOutCubic(progress);
            int displayedCoins = Mathf.RoundToInt(Mathf.Lerp(startCoins, targetCoins, easedProgress));

            coinDisplay.SetCoin(displayedCoins);
            yield return new WaitForSeconds(stepDuration);
        }

        coinDisplay.SetCoin(targetCoins);
    }

    private float EaseOutCubic(float value) {
        value = Mathf.Clamp01(value);
        return 1f - Mathf.Pow(1f - value, 3f);
    }

}
