using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class DialogueNodeData 
{
    public string DialogueText;
    public string SpeakerName;
    public string NodeGUID;
    public Vector2 Position;
    public IEnumerable<string> StyleClass;
}
