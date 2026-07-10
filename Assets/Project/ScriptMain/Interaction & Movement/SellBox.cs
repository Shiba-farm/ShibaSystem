using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SellBox : MonoBehaviour, IInteractable
{
    public PromptType InteractPromptType => PromptType.Shop;

    public void Interact()
    {
        InGameUIManager.Instance.OpenExclusivePanel(InGamePanel.Selling);
    }

    // [Header("SFX")]
    // public AudioSource sfxSource;
    // public AudioClip openSfx;
    // public AudioClip sellSfx;
    // [Range(0f, 1f)] public float sfxVolume = 1f;

    // void PlaySfx(AudioClip clip)
    // {
    //     if (sfxSource && clip) sfxSource.PlayOneShot(clip, sfxVolume);
    // }
}
