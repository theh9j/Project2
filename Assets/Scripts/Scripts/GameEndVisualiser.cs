using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameEndVisualiser : MonoBehaviour
{

    //Win section
    [Header("Game Win")]
    [SerializeField] private GameObject gameWin;
    [SerializeField] private Transform zoom;

    [SerializeField] private Transform shineSpot;
    [SerializeField] private TMP_Text levelDisplay;
    [SerializeField] private TMP_Text coinBonus;

    [SerializeField] private Button continueButton;

    [SerializeField] private float spinSpeed = .15f;

    //Background
    [SerializeField] private GameObject background;
    private Image backgroundImg;

    void Awake() {
        backgroundImg = background.GetComponent<Image>();

        continueButton.onClick.AddListener(() => {

        });
    }

    void Update() {
        if (gameWin.activeSelf) {
            float angle = Time.deltaTime * spinSpeed;

            shineSpot.rotation = Quaternion.Euler(0, 0, angle);
        }


    }

    public void Win(LevelSaveData data) {
        if (!gameWin.activeSelf) return;
        AnimateBackground(true);

        gameWin.SetActive(true);
        shineSpot.rotation = Quaternion.identity;

        levelDisplay.text = $"Level {data.levelId}";
        coinBonus.text = $"+{data.rewards.coins}";

        zoom.DOKill();
        zoom.DOScale(
            1f,
            .5f
            ).From(0f)
            .SetEase(Ease.InOutBack, 1f);
    }

    public Tween AnimateBackground(bool open) {
        if (open) {
            return backgroundImg.DOFade(.996f, .25f)
                .OnStart(() => background.SetActive(true));
        } else {
            return backgroundImg.DOFade(0f, .25f)
                .OnComplete(() => background.SetActive(false));
        }
    }

}
