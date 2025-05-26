using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Overlays;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


public class DialogueGraph : EditorWindow
{
    private DialogueGraphView _graphView;
    private string _fileName = "New Dialogue";

    [MenuItem("Graph/Dialogue Graph")]
    public static void OpenDialogueGraph()
    {
        var window = GetWindow<DialogueGraph>();
        window.titleContent = new GUIContent("Dialogue Graph");

    }

    //Used to open the graph from double clicking the asset
    [OnOpenAsset]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        //Get the instance ID of the object to find it in the project
        string assetPath = AssetDatabase.GetAssetPath(instanceID);
        DialogueContainer dialogueContainer = AssetDatabase.LoadAssetAtPath<DialogueContainer>(assetPath);
        if (dialogueContainer !=null)
        {
            //Open the dialogue graph window
            DialogueGraph window = GetWindow<DialogueGraph>();
            window.titleContent = new GUIContent($"{dialogueContainer.name}(Dialogue Graph)");
            //Load the contents of the dialogue container
            window._fileName = dialogueContainer.name;
            window.RequestDataOperation(false); //true is save, false is load
            return true;
        }
        return false;
    }
    private void OnEnable()
    {
        ContructGraph();
        GenerateToolBar();
        GenerateMiniMap();
        GenerateBlackboard();
    }

    private void GenerateBlackboard()
    {
        var blackboard = new Blackboard();
        blackboard.Add(new BlackboardSection { title = "Variables" });
        //To add items they need to be subscribed to the blackboard
        blackboard.addItemRequested = _blackboard => { _graphView.AddPropertyToBlackboard(new ExposedProperty()); };
        blackboard.editTextRequested = (blackboard, element, newValue) =>
        {
            var oldPropertyName = ((BlackboardField)element).text;
            if (_graphView.ExposedProperties.Any(x => x.PropertyName == newValue))
            {
                EditorUtility.DisplayDialog("Error", "This property name already exists, pick a new one", "OK");
                return;
            }

            var propertyIndex = _graphView.ExposedProperties.FindIndex(x => x.PropertyName == oldPropertyName);
            _graphView.ExposedProperties[propertyIndex].PropertyName = newValue;
            ((BlackboardField)element).text = newValue;
        };

        blackboard.SetPosition(new Rect(10, 30, 200, 300)); 
        _graphView.Add(blackboard);
        _graphView.Blackboard = blackboard;
    }
    private void OnDisable()
    {
        rootVisualElement.Remove(_graphView);

    }

    private void ContructGraph()
    {
        _graphView = new DialogueGraphView(this)
        {
            name = "Dialogue Graph"
        };
        _graphView.StretchToParentSize();
        rootVisualElement.Add(_graphView);
    }

    private void GenerateToolBar()
    {
        var toolbar = new Toolbar();

        var fileNameTextField = new TextField("File Name");
        fileNameTextField.value = _fileName;
        fileNameTextField.MarkDirtyRepaint(); //Mark as dirty otherwise it won't show up
        fileNameTextField.RegisterValueChangedCallback(evt => _fileName = evt.newValue); //Register a callback to save the file name in the directory
        toolbar.Add(fileNameTextField);

        toolbar.Add(new Button(() => RequestDataOperation(true)) { text = "Save" });
        toolbar.Add(new Button(() => RequestDataOperation(false)) { text = "Load" });

        //var nodeCreateButton = new Button(() =>
        //{
        //    _graphView.CreateNode("Dialogue Node");
        //});

        //nodeCreateButton.text = "Create Node";
        //toolbar.Add(nodeCreateButton);
        rootVisualElement.Add(toolbar);
    }

    private void GenerateMiniMap()
    {
        var miniMap = new MiniMap{ anchored = true};
        var miniMapWidth = 200;
        var cords = _graphView.contentViewContainer.WorldToLocal(new Vector2(this.position.width - (miniMapWidth + 10), 30)); 
        miniMap.SetPosition(new Rect(cords.x, cords.y, miniMapWidth, 140));
        _graphView.Add(miniMap);
    }

    private void RequestDataOperation(bool save)
    {
        if (string.IsNullOrEmpty(_fileName))
        {
            Debug.LogError("File name is empty. Please enter a valid file name.");
            return;
        }

        var saveUtility = GraphSaveUtility.GetInstance(_graphView);
        if (save)
        {
            saveUtility.SaveGraph(_fileName);
        }
        else
        {
            saveUtility.LoadGraph(_fileName);
        }
    }
}