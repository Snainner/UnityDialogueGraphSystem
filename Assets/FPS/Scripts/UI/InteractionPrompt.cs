using System;
using UnityEngine;
using TMPro;
using Unity.FPS.Game;

public class InteractionPrompt : MonoBehaviour
{
    public  TextMeshProUGUI InteractonText;
    public RectTransform panel;

    private void Awake()
    {
        InteractonText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        
        EventManager.AddListener<InteractionPromptEvent>(OnDisplayInteractionPrompt);
    }

    void OnDisable()
    {
        EventManager.RemoveListener<InteractionPromptEvent>(OnDisplayInteractionPrompt);
        HidePrompt();
    }

    void OnDisplayInteractionPrompt(InteractionPromptEvent evt)
    {
        if (!string.IsNullOrEmpty(evt.PromptText))
        {
            ShowPrompt(evt.PromptText);
        }
        else
        {
            HidePrompt();
        }
       
    }

    void ShowPrompt(string prompt)
    {
        InteractonText.text = prompt;
        InteractonText.gameObject.SetActive(true);
    }

    void HidePrompt()
    {
        InteractonText.gameObject.SetActive(false);
    }
}
