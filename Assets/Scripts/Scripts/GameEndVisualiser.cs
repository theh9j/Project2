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

    [Header("Game Lost")]
    [SerializeField] private GameObject gameLose;
    [SerializeField] private Transform noMovesText;
    [SerializeField] private Transform loseButtons;
    [SerializeField] private Transform loseVisual;

    [SerializeField] private Button revive;
    [SerializeField] private TMP_Text reviveOptionText;

    [SerializeField] private Button restart;

    private int price; //STC
    [SerializeField] private string reviveOp1 = "Revive\n<sprite name=\"coin\"> ";
    [SerializeField] private string reviveOp2 = "Revive\n<sprite name=\"clip\"> Watch Ad";


    //Common

    public event Action LoadLevel;

    //Background
    [Header("Background")]
    [SerializeField] private GameObject background;
    public bool GamePause { get; private set; } = false;
    private Image backgroundImg;

    private LevelSaveData thisLevel = new();

    void Awake() {
        backgroundImg = background.GetComponent<Image>();
        continueButton.onClick.AddListener(() => StartCoroutine(Continue()));

        revive.onClick.AddListener(() => {
            if (SaveManager.Instance != null && SaveManager.Instance.coins > price) {
                SaveManager.Instance.coins -= price;
            } else PlayAd();

            //Open up slot logic here -> gameManager

        });

        restart.onClick.AddListener(() => {
            LoadLevel?.Invoke();
            UndoLoseScreen()
            .OnComplete(() => AnimateBackground(false));
        });
    }

    void Update() {
        if (gameWin.activeSelf)
            shineSpot.rotation = Quaternion.Euler(0, 0, Time.time * spinSpeed);

    }

    public void Win(LevelSaveData data) {
        if (gameWin.activeSelf) return;
        GamePause = true;
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
    }

    public void Lose() {
        if (gameLose.activeSelf) return;
        GamePause = true;

        if (SaveManager.Instance != null && SaveManager.Instance.coins > price) reviveOptionText.text = reviveOp1 + price;
        else reviveOptionText.text = reviveOp2;

        SetLoseButtons(true);

        AnimateBackground(true).OnComplete(() => {
            noMovesText.DOKill();
            gameLose.SetActive(true);

            DG.Tweening.Sequence seq = DOTween.Sequence();

            seq.Join(noMovesText.DOLocalMoveY(0f, .5f)
                .From(Screen.height * 1.5f)
                .SetEase(Ease.OutBack, 1f));

            seq.Join(noMovesText.GetComponent<CanvasGroup>().
                DOFade(1f, .5f).From(0f));

            seq.Join(loseVisual.DOScale(1f, .5f).From(0f)
                .SetEase(Ease.OutBack, 2f));

            seq.Append(loseButtons.DOLocalMoveY(0f, .5f)
                .From(-Screen.height).SetEase(Ease.OutBack, 1f));

        });

    }

    private Tween UndoLoseScreen() {
        SetLoseButtons(false);

        noMovesText.DOLocalMoveY(Screen.height * 1.5f, .75f)
            .SetEase(Ease.InBack, 1f);

        noMovesText.GetComponent<CanvasGroup>().
            DOFade(0f, .5f);

        loseVisual.DOScale(0f, .5f).SetEase(Ease.InBack, 1f);

        return loseButtons.DOLocalMoveY(-Screen.height * 1.5f, .75f).SetEase(Ease.InBack, 1f)
            .OnComplete(() => gameLose.SetActive(false));
    }

    public Tween AnimateBackground(bool open) {
        backgroundImg.DOKill();
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

        LoadLevel?.Invoke();

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
        
        yield return coinDisplay.CoinAdd(thisLevel.rewards.coins);
    }

    private void SetLoseButtons(bool set) {
        revive.interactable = set;
        restart.interactable = set;
    }

    private void PlayAd() {
        Debug.Log("Ad played");
        //this func is a placeholder
    }

}
