using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Unity.FPS.Game;
using UnityEngine.ProBuilder.MeshOperations;

public class DialogueParser : MonoBehaviour
{
    [SerializeField] private DialogueContainer dialogueContainer;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button choiceButton;
    [SerializeField] private Transform buttonContainer;

    private void Start()
    {
        var dialogueData = dialogueContainer.NodeLinks.First();
        ProceedDialogue(dialogueData.TargetNodeGuid);
        
        //When the first node after entry isn't a dialogue node it can't find it. 
    }

    private void ProceedDialogue(string nodeGUID)
    {
        if (IsDialogueNode(nodeGUID, dialogueContainer))
        {
            var text = dialogueContainer.DialogueNodeData.Find(x => x.NodeGUID == nodeGUID).DialogueText;

            var choices = dialogueContainer.NodeLinks.Where(x => x.BaseNodeGuid == nodeGUID);
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
        else if (IsEventNode(nodeGUID, dialogueContainer))
        {
            //var eventNode = dialogueContainer.EventNodeData.Find(x => x.NodeGUID == nodeGUID);
            var eventPorts = dialogueContainer.NodeLinks.Where(x => x.BaseNodeGuid == nodeGUID);
            foreach (var port in eventPorts)
            {
                var portFields = port.PortUserData;
                BroadCastEventPort(portFields);
            }
        }

    }

    private void BroadCastEventPort(List<PortEventField> portFields)
    {
        if(portFields == null ||  portFields.Count == 0)
            return;
        
        string eventName =  portFields[0].EventName;
      

        var eventType = typeof(Events);
        
        FieldInfo eventField = eventType.GetField(eventName, BindingFlags.Public | BindingFlags.Static); 
        if (eventField == null)
        {
            Debug.LogError($"Event {eventName} not found in Events class");
            return;
        }
        Type gameEventType = eventField.FieldType;
        GameEvent gameEvent = Activator.CreateInstance(gameEventType) as GameEvent;
        
        
        foreach (var field in portFields)
        {
            //To set the variable fields in the game event we first need to get them 
            FieldInfo fieldInfo = gameEventType.GetField(field.FieldName, BindingFlags.Public | BindingFlags.Static);
            if (fieldInfo == null)
                continue;
            object value = null;
            Type fieldType = Type.GetType(field.FieldType);
            try
            {
                if (fieldType.IsEnum)
                {
                    value = Enum.Parse(fieldType, field.FieldValue);
                }
                else
                {
                    value = Convert.ChangeType(field.FieldValue, fieldType);
                }
                
                fieldInfo.SetValue(gameEvent, value);
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not convert field {field.FieldName} to enum {field.FieldType} {e}");
                continue;
            }
        }
        EventManager.Broadcast(gameEvent);
    }
    private GameEvent FindGameEvent(string eventName)
    {
        return EventReflectionUtility.GetAllEvents().FirstOrDefault(x => x.Name == eventName).EventInstance;
    }

    bool IsEventNode(string nodeGuid, DialogueContainer dialogue)
    {
        Debug.Log("Its a event node");
        return dialogue.EventNodeData.Any(x => x.NodeGUID == nodeGuid);
    }

    bool IsDialogueNode(string nodeGUID, DialogueContainer dialogue)
    {
        Debug.Log("Its a dialogue node");
        return dialogue.DialogueNodeData.Any(x => x.NodeGUID == nodeGUID);
    }
    private string ProcessProperties(string text)
    {
       foreach (var exposedProperties in dialogueContainer.ExposedProperties)
        {
            text = text.Replace($"[{exposedProperties.PropertyName}]", exposedProperties.PropertyValue);
        }
        return text;
    }
}
