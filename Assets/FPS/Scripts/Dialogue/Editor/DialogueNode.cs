using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class DialogueNode : Node
{
    public string GUID; //An unique identifier for the node
    public string DialogueText;
    public string SpeakerName;
    public bool EntryPoint = false;
}
