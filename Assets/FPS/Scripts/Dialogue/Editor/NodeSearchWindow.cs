using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
{
    private DialogueGraphView _graphView;
    private EditorWindow _window; //To capture the mouse position a reference to the editor window is needed
    private Texture2D _indentationIcon;

    //Inject the graph view into the search window
    public void Init(EditorWindow window, DialogueGraphView graphView)
    {
        _graphView = graphView;
        _window = window;

        _indentationIcon = new Texture2D(1, 1);
        _indentationIcon.SetPixel(0, 0, Color.clear); //Fixes weird indentation bug 
        _indentationIcon.Apply();
    }
    //Responsible for listing the elements in the window
    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        var tree = new List<SearchTreeEntry>()
        {
            new SearchTreeGroupEntry(new GUIContent("Create Elements"), 0),
            new SearchTreeGroupEntry(new GUIContent("Dialogue"), 1),
            new SearchTreeEntry(new GUIContent("Dialogue Node", _indentationIcon))
            {
                level = 2,
                userData = new DialogueNode()
            },
            new SearchTreeEntry(new GUIContent("Event Node", _indentationIcon))
            {
                level = 3,
                
                userData = new EventNode()
            }
        };
        return tree;
    }

    public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
    {
        var worldMousePosition = _window.rootVisualElement.ChangeCoordinatesTo(_window.rootVisualElement.parent, context.screenMousePosition - _window.position.position); //_Window.position.position is needed to go from a rect to a vector2  
        var localMousePosition = _graphView.contentViewContainer.WorldToLocal(worldMousePosition); //Convert the world position to a local position  
        switch (SearchTreeEntry.userData)
        {
            //Event node case needs to be first as it is derived from the dialoguenode 
            //If it is not first the switch statement will not be able to distinguish between the two types of nodes
            case EventNode eventNode:  
                _graphView.CreateEventNode("Event Node", localMousePosition);
                return true;
            case DialogueNode dialogueNode:
                _graphView.CreateDialogueNode("Dialogue Node", localMousePosition);
                return true;

            default:
                return false;
        }
    }
}
