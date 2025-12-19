using UnityEngine;
using UnityEngine.UI;

public class SFXVolumeControl : MonoBehaviour
{
    public Slider volumeSlider;
    public Image volumeIcon;
    public Sprite noBarSprite;
    public Sprite oneBarSprite;
    public Sprite twoBarSprite;
    public Sprite threeBarSprite;

    void Start()
    {
        float startValue = AudioManager.Instance.sfxSource.volume;

        if (volumeSlider)
        {
            volumeSlider.value = startValue;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        UpdateIcon(startValue);
    }

    void OnVolumeChanged(float value)
    {
        AudioManager.Instance.SetSFXVolume(value);
        UpdateIcon(value);
    }

    void UpdateIcon(float value)
    {
        if (value <= 0.00f)
            volumeIcon.sprite = noBarSprite;
        else if (value <= 0.33f)
            volumeIcon.sprite = oneBarSprite;
        else if (value <= 0.66f)
            volumeIcon.sprite = twoBarSprite;
        else
            volumeIcon.sprite = threeBarSprite;
    }
}
