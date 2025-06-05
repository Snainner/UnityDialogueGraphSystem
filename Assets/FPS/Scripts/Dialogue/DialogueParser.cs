using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Unity.FPS.Game;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.Rendering;



public class DialogueParser : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueContainer dialogueContainer;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI speakerName;
    [SerializeField] private Button choiceButton;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private CanvasGroup canvasGroup;
    private List<Button> buttonList = new List<Button>();
    bool isDialogueActive = false;
    bool lastNodeActive = false;

    private void Awake()
    {
        FadeOut();
    }

    public void Interact()
    {
        var dialogueData = dialogueContainer.NodeLinks.First();
        var dialogueEvent = new DialogueEvent();
        dialogueEvent.IsActive = true;
        EventManager.Broadcast(dialogueEvent);
        isDialogueActive = true;
        FadeIn();
        ProceedDialogue(dialogueData.TargetNodeGuid);
    }

    public string GetInteractPrompt()
    {
        return "Press E to talk to " + gameObject.name;
    }

    private void Update()
    {
        if (isDialogueActive)
        {
            for (int i = 0; i < buttonList.Count && buttonList.Count <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    buttonList[i].onClick.Invoke();
                }
            }

            if (lastNodeActive && Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                var dialogueEvent = new DialogueEvent();
                dialogueEvent.IsActive = false;
                EventManager.Broadcast(dialogueEvent);
                Debug.Log("NodeGUID is the last base node");
                isDialogueActive = false;
                FadeOut();
            }
        }
    }
    // private void Start()
    // {
    //     var dialogueData = dialogueContainer.NodeLinks.First();
    //     ProceedDialogue(dialogueData.TargetNodeGuid);
    //     
    //     //When the first node after entry isn't a dialogue node it can't find it. 
    // }

    private void ProceedDialogue(string nodeGUID)
    {
        
        var lastNode = dialogueContainer.NodeLinks.Last();
        if (nodeGUID == lastNode.BaseNodeGuid)
        {
            lastNodeActive = true;
        }
        
        if (IsDialogueNode(nodeGUID, dialogueContainer))
        {
            var text = dialogueContainer.DialogueNodeData.Find(x => x.NodeGUID == nodeGUID).DialogueText;
            var speakerString = dialogueContainer.DialogueNodeData.Find(x => x.NodeGUID == nodeGUID).SpeakerName;
            var choices = dialogueContainer.NodeLinks.Where(x => x.BaseNodeGuid == nodeGUID);
            dialogueText.text = ProcessProperties(text);
            speakerName.text = ProcessProperties(speakerString);

            buttonList.Clear();
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
                    Debug.Log($"Button Pressed {choice.TargetNodeGuid}");
                });
                buttonList.Add(button);
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
            
            buttonList.Clear();
            var buttons = buttonContainer.GetComponentsInChildren<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Destroy(buttons[i].gameObject);
            }

            var choices = dialogueContainer.NodeLinks.Where(x => x.BaseNodeGuid == nodeGUID);
            foreach (var choice in choices)
            {
                var button = Instantiate(choiceButton, buttonContainer);
                button.GetComponentInChildren<TextMeshProUGUI>().text = choice.PortName;
                button.onClick.AddListener(() =>
                {
                    ProceedDialogue(choice.TargetNodeGuid);
                });
                buttonList.Add(button);
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
            //To set the variable fields in the game event we first need to retrieve them
            Debug.Log("Entering field foreach loop");
            try
            {
                FieldInfo fieldInfo = gameEventType.GetField(field.FieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (fieldInfo == null)
                {
                    Debug.LogError($"Field {field.FieldName} not found");
                    break;
                }
                object value = null;
                Type fieldType = Type.GetType(field.FieldType);
                
                Debug.Log("Trying value conversion");
                if (fieldType.IsEnum)
                {
                    value = Enum.Parse(fieldType, field.FieldValue);
                    Debug.Log($"Value is enum: {value}");
                }
                else
                {
                    value = Convert.ChangeType(field.FieldValue, fieldType);
                    Debug.Log($"Value is not an enum: {value}");
                }
                Debug.Log($"Value: {field.FieldValue} and {value}");
                fieldInfo.SetValue(gameEvent, value);
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not convert field {field.FieldName} to enum {field.FieldType} {e}");
                break; 
            }
           
        }
        EventManager.Broadcast(gameEvent);
        Debug.Log($"Event {eventName} successfully broadcasted");
    }
    private GameEvent FindGameEvent(string eventName)
    {
        return EventReflectionUtility.GetAllEvents().FirstOrDefault(x => x.Name == eventName).EventInstance;
    }

    bool IsEventNode(string nodeGuid, DialogueContainer dialogue)
    {
        return dialogue.EventNodeData.Any(x => x.NodeGUID == nodeGuid);
    }

    bool IsDialogueNode(string nodeGUID, DialogueContainer dialogue)
    {
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

    void FadeIn()
    {
        canvasGroup.alpha = 1;
    }
    void FadeOut()
    {
        canvasGroup.alpha = 0;
    }
}
