using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Unity.FPS.Game;

public class DialogueParser : MonoBehaviour
{
    [SerializeField] private DialogueContainer dialogue;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button choiceButton;
    [SerializeField] private Transform buttonContainer;

    private void Start()
    {
        var dialogueData = dialogue.NodeLinks.First();
        ProceedDialogue(dialogueData.TargetNodeGuid);
    }

    private void ProceedDialogue(string dialogueData)
    {
        var text = dialogue.DialogueNodeData.Find(x => x.NodeGUID == dialogueData).DialogueText;
        var choices = dialogue.NodeLinks.Where(x => x.BaseNodeGuid == dialogueData);
        if (CheckForEvent(text))
        {
            return;
        }

        dialogueText.text = ProcessProperties(text);

        var buttons = buttonContainer.GetComponentsInChildren<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            Destroy(buttons[i].gameObject);
        }
        foreach (var choice in choices)
        {
            var button = Instantiate(choiceButton, buttonContainer);
            button.GetComponentInChildren<TextMeshProUGUI>().text = choice.PortName;
            button.onClick.AddListener(() =>
            {
                ProceedDialogue(choice.TargetNodeGuid);
            });
        }
    }

    private bool CheckForEvent(string text)
    {
        if(text.StartsWith("@event: "))
        {
            var eventName = text.Substring(7).Trim();
            
            //TriggerEvent();
            return true;
        }
        return false;
    }
    private void TriggerEvent(Event eventName)
    {
        // Implement your event triggering logic here
        //Events.DamageEvent.Sender = gameObject;
       // Events.DamageEvent.DamageValue = 10f; 
       //EventManager.Broadcast(Events.eventName);
        Debug.Log($"Event triggered: {eventName}");
    }
    private string ProcessProperties(string text)
    {
       foreach (var exposedProperties in dialogue.ExposedProperties)
        {
            text = text.Replace($"[{exposedProperties.PropertyName}]", exposedProperties.PropertyValue);
        }
        return text;
    }
}
