using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Unity.FPS.Game;
using UnityEditor.UIElements;
using log4net.Util;
using Unity.Tutorials.Core.Editor;
using Codice.Utils;


public class DialogueGraphView : GraphView
{
    public readonly Vector2 defaultNodeSize = new Vector2(150, 200);
    
    public Blackboard Blackboard;
    public List<ExposedProperty> ExposedProperties = new List<ExposedProperty>();
    private NodeSearchWindow _searchWindow;
    public DialogueGraphView(EditorWindow editorWindow)
    {
        styleSheets.Add(Resources.Load<StyleSheet>("DialogueGraph"));
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        AddElement(GenerateEntryPointNode());
        AddSearchWindow(editorWindow);
    }

    private void AddSearchWindow(EditorWindow editorWindow)
    {
        _searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
        _searchWindow.Init(editorWindow, this);
        nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindow);

    }

    private Port AddPort(Node node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single)
    {
        return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float));
    }

    private Port GeneratePort(Node node, Direction direction, Port.Capacity capacity = Port.Capacity.Single)
    {
        var port = node.InstantiatePort(Orientation.Horizontal, direction, capacity, typeof(float));
        
        if (direction == Direction.Input)
        {
            node.inputContainer.Add(port);
        }
        else
        {
            node.outputContainer.Add(port);
        }
        return port;
    }
    private DialogueNode GenerateEntryPointNode()
    {
        var node = new DialogueNode
        {
            title = "Start",
            GUID = Guid.NewGuid().ToString(),
            DialogueText = "Entry Point",
            EntryPoint = true
        };

        var generatedPort = AddPort(node, Direction.Output);
        generatedPort.portName = "Start";
        node.outputContainer.Add(generatedPort);

        //Prevent the entry node from being moveable or deletable
        //The syntax for removing capabilities is: ~
        node.capabilities &= ~Capabilities.Movable;
        node.capabilities &= ~Capabilities.Deletable;

        //Always need to refresh the containers for proper visuals
        node.RefreshExpandedState();
        node.RefreshPorts();

        node.SetPosition(new Rect(100, 200, 100, 150));
        return node;
    }

    public void CreateDialogueNode(string nodeName, Vector2 position)
    {
        AddElement(GenerateDialogueNode(nodeName, position));
    }
    public void CreateEventNode(string nodeName, Vector2 position)
    {
        AddElement(GenerateEventNode(nodeName, position));
    }
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();

        ports.ForEach(port =>
        {
            if (startPort != port && startPort.node != port.node)
            {
                compatiblePorts.Add(port);
            }
        });
        return compatiblePorts;
    }
    public DialogueNode GenerateDialogueNode(string nodeName, Vector2 position)
    {
        var dialogueNode = new DialogueNode
        {
            title = nodeName,
            DialogueText = nodeName,
            GUID = Guid.NewGuid().ToString()
        };
        var inputPort = AddPort(dialogueNode, Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Input";
        dialogueNode.inputContainer.Add(inputPort);

        dialogueNode.styleSheets.Add(Resources.Load<StyleSheet>("NodeStyle"));

        var button = new Button(() => { AddChoicePort(dialogueNode); });
        button.text = "Add Choice";
        dialogueNode.titleContainer.Add(button);

        var textField = new TextField(string.Empty);
        textField.RegisterValueChangedCallback(evt =>
        {
            dialogueNode.DialogueText = evt.newValue;
            dialogueNode.title = evt.newValue;
            /*
            if(dialogueNode.DialogueText.Contains("@event: "))
            {
               dialogueNode.AddToClassList("EventNode");
               //check if event is valid
            }
            else
            {
                dialogueNode.RemoveFromClassList("EventNode");
            }
            */
        });

        //Vlaue is set in the dialogueNode object so it can be saved later 
        textField.SetValueWithoutNotify(dialogueNode.title);
        dialogueNode.mainContainer.Add(textField);


        dialogueNode.RefreshExpandedState();
        dialogueNode.RefreshPorts();
        dialogueNode.SetPosition(new Rect(position, defaultNodeSize));

        return dialogueNode;
    }

    public EventNode GenerateEventNode(string nodeName, Vector2 position)
    {
        var eventNode = new EventNode
        {
            title = nodeName,
            EventName = null,
            GUID = Guid.NewGuid().ToString(),
        };
        
        var inputPort = GeneratePort(eventNode, Direction.Input, Port.Capacity.Multi);
        inputPort.portName = "Input";

        eventNode.inputContainer.Add(inputPort);
        eventNode.styleSheets.Add(Resources.Load<StyleSheet>("EventNodeStyle"));

        var button = new Button(() => { AddEventPort(eventNode); });
        button.text = "Add Event";
        eventNode.titleContainer.Add(button);

        //var textField = new TextField(string.Empty);
        //textField.RegisterValueChangedCallback(evt =>
        //{
        //    eventNode.title = evt.newValue;
            
        //});
        //textField.SetValueWithoutNotify(eventNode.title);
        //eventNode.mainContainer.Add(textField);

        eventNode.RefreshExpandedState();
        eventNode.RefreshPorts();
        eventNode.SetPosition(new Rect(position, defaultNodeSize));

        return eventNode;
    }

    public void AddEventPort(Node node, string OverriddenIndex= "", PortData portData = null)
    {
        if (portData != null)
        {
            foreach (var field in portData.EventFields)
            {
                GenerateFields(field);
            }
        }
        var eventFields = new List<PortEventField>();
        var eventPort = new PortData
        {
            PortGUID = Guid.NewGuid().ToString(),
            EventFields = eventFields
        };
        var generatedPort = AddPort(node, Direction.Output);
        
        var oldLabel = generatedPort.contentContainer.Q<Label>("type");
        oldLabel.visible = false;
        oldLabel.style.flexBasis = 0;

        var outputPortCount = node.outputContainer.Query("connector").ToList().Count;
        generatedPort.portName = " " + outputPortCount + 1;
        node.outputContainer.Add(generatedPort);

        var options = new List<GameEvent>();
        options.Add(new GameEvent()); //Add an empty event to the list to allow for a default selection
        foreach (var (name, _event) in EventReflectionUtility.GetAllEvents())
        {
            options.Add(_event);
        }

        //int selectedIndex = OverriddenIndex.IsNullOrEmpty() ? 0 : int.Parse(OverriddenIndex);
        int selectedIndex = 0;
        if (OverriddenIndex.IsNotNullOrEmpty())
        {
            selectedIndex = int.Parse(OverriddenIndex);
            generatedPort.portName = OverriddenIndex; 
        }
        else
        {
            selectedIndex = 0;
        }
        #region Override Port Name Attempt 
        //int selectedIndex = overriddenSelection == null ? selectedIndex = 0: selectedIndex = (int)overriddenSelection; 


        //var eventPortName = string.IsNullOrEmpty(overridenPortName) ? $"Event {outputPortCount + 1}" : overridenPortName;
        //int selectionIndex = 0;

        //if (!string.IsNullOrEmpty(overridenPortName))
        //{
        //    //textField.SendEvent(new ChangeEvent<string> { previousValue = "", newValue = "New Value" });
        //    selectionIndex = options.FindIndex(x => x.GetType().Name == overridenPortName);
        //    generatedPort.portName = overridenPortName;
        //    GenerateEventVariableFields(options[selectionIndex], generatedPort);

        //    if(selectionIndex < 0)
        //    {
        //        Debug.LogWarning($"Event {overridenPortName} not found in the available events.");
        //        selectionIndex = 0; // Fallback to the first event if not found
        //    }
        //}
        //else
        //{
        //    generatedPort.portName = $"Event {outputPortCount + 1}";
        //}
        #endregion
        var dropdown = new PopupField<GameEvent>("Event", options, 0, evt => evt.GetType().Name, evt => evt.GetType().Name);
        dropdown.SetValueWithoutNotify(options[selectedIndex]); 

        dropdown.RegisterValueChangedCallback(evt => 
        { 
            var eventType = evt.newValue.GetType();
            selectedIndex = dropdown.index; 
            Debug.Log($"Selected event: {eventType.Name} at index {selectedIndex}");
            generatedPort.portName = selectedIndex.ToString();
            //GenerateEventVariableFields(evt.newValue, generatedPort, eventPort.PortGUID, eventFields);

        });

        //dropdown.RegisterValueChangedCallback(evt => selectedOption = node.title);
        var dropdownLabel = dropdown.Q<Label>();
        dropdownLabel.visible = false;
        //label width is normally 100px and the font size is 12px
        //When the font is zero the label becomes a lot wider 
        dropdownLabel.style.minWidth = 0;
        dropdownLabel.style.fontSize = 1;

        generatedPort.contentContainer.Add(new Label("  ")); //Dummy label to create a gap
        var flexibleSpace = new VisualElement();
        flexibleSpace.style.flexGrow = 1;
        flexibleSpace.Add(dropdown);
        generatedPort.contentContainer.Add(flexibleSpace); 
        
        var deleteButton = new Button(() => RemovePort(node, generatedPort))
        {
            text = "X"
        };
        deleteButton.name = "delete-button";
        deleteButton.style.marginLeft = 8;
        generatedPort.contentContainer.Add(deleteButton);

        node.RefreshExpandedState();
        node.RefreshPorts();
    }

    private void UpdatePortFieldValue()
    {
        //check what type of data it is 

    }
    private void ResizeUILabel(Label label, string text)
    {
        label.text = text;
        label.style.minWidth = new StyleLength(new Length(label.resolvedStyle.fontSize * text.Length, LengthUnit.Pixel));
    }
    private void GenerateFields(PortEventField field)
    {
        if (field == null)
        {
            Debug.LogWarning("No event field found for the port data.");
            return;
        }
        else
        {
            switch (field.FieldType)
            {
                case "String":
                    var textField = new TextField(field.FieldName)
                    {
                        value = field.FieldValue
                    };
                    textField.RegisterValueChangedCallback(valueChangeEvent =>
                    {
                        field.FieldValue = valueChangeEvent.newValue;

                        Debug.Log($"Updated field {field.FieldName} to {field.FieldValue}");
                    });
                    ResizeUILabel(textField.Q<Label>(), field.FieldName);
                    break;
                case "Int32":
                    var intField = new IntegerField(field.FieldName)
                    {
                        value = int.Parse(field.FieldValue)
                    };
                    intField.RegisterValueChangedCallback(valueChangeEvent =>
                    {
                        field.FieldValue = valueChangeEvent.newValue.ToString();
                        Debug.Log($"Updated field {field.FieldName} to {field.FieldValue}");
                    });
                    ResizeUILabel(intField.Q<Label>(), field.FieldName);
                    break;
                case "Single":
                    var floatField = new FloatField(field.FieldName)
                    {
                        value = float.Parse(field.FieldValue)
                    };
                    floatField.RegisterValueChangedCallback(valueChangeEvent =>
                    {
                        field.FieldValue = valueChangeEvent.newValue.ToString();
                        Debug.Log($"Updated field {field.FieldName} to {field.FieldValue}");
                    });
                    ResizeUILabel(floatField.Q<Label>(), field.FieldName);
                    break;
                case "Boolean":
                    var toggle = new Toggle(field.FieldName)
                    {
                        value = bool.Parse(field.FieldValue)
                    };
                    toggle.RegisterValueChangedCallback(valueChangeEvent =>
                    {
                        field.FieldValue = valueChangeEvent.newValue.ToString();
                        Debug.Log($"Updated field {field.FieldName} to {field.FieldValue}");
                    });
                    ResizeUILabel(toggle.Q<Label>(), field.FieldName);
                    break;
                case "GameObject":
                    var objectField = new ObjectField(field.FieldName)
                    {
                        objectType = typeof(GameObject),
                        value = GameObject.Find(field.FieldValue) //Assuming the field value is the name of the GameObject
                    };
                    objectField.RegisterValueChangedCallback(valueChangeEvent =>
                    {
                        field.FieldValue = valueChangeEvent.newValue != null ? ((GameObject)valueChangeEvent.newValue).name : string.Empty;
                        Debug.Log($"Updated field {field.FieldName} to {field.FieldValue}");
                    });
                    ResizeUILabel(objectField.Q<Label>(), field.FieldName);
                    break;
            }

        }

    }

    private PortEventField CreateFieldObject(string portGUID, string eventName, string fieldName, string fieldType, string fieldValue)
    {
        var portEventField = new PortEventField
        {
            PortGUID = portGUID,
            EventName = eventName,
            FieldName = fieldName,
            FieldType = fieldType,
            FieldValue = fieldValue
        };
       return portEventField;
    }
    public void GetEventVariables(GameEvent evt, Port generatedPort, string portGUID, List<PortEventField> fieldList)
    {
        if(generatedPort.contentContainer.Children() != null)
        {
            foreach (var child in generatedPort.contentContainer.Children().ToList())
            {
                if (child.name == "event-variable")
                {
                    generatedPort.contentContainer.Remove(child);
                }
            }
        }

        var eventVariables = EventReflectionUtility.GetGameEventFields(evt);
        var fieldObject = CreateFieldObject(portGUID, evt.GetType().Name, eventVariables.GetType().Name, eventVariables.GetType().Name, "");
        GenerateFields(fieldObject);
        /*if (eventVariables != null)
        {
            foreach (var field in eventVariables)
            {
                var fieldType = field.FieldType;
                var fieldName = field.Name;

                //Check if the field is a string, int or float and create a text field for it
                if (fieldType == typeof(string) || fieldType == typeof(int) || fieldType == typeof(float))
                {
                    var textField = new TextField(fieldName)
                    {
                        value = field.GetValue(evt)?.ToString() ?? string.Empty
                    };
                    textField.RegisterValueChangedCallback(valueChangeEvent =>
                    {
                        //Set the value of the field in the event instance
                        field.SetValue(evt, Convert.ChangeType(valueChangeEvent.newValue, fieldType));
                        portEventField.FieldValue = field.GetValue(valueChangeEvent.newValue)?.ToString() ?? string.Empty;
                    });
                    ResizeUILabel(textField.Q<Label>(), fieldName);
                    textField.name = "event-variable";
                    generatedPort.contentContainer.Add(textField);
                    
                }
                else if (fieldType == typeof(bool))
                {
                    var toggle = new Toggle(fieldName)
                    {
                        value = (bool)field.GetValue(evt)
                    };
                    toggle.RegisterValueChangedCallback(valueChangeEvent =>
                    {
                        //Set the value of the field in the event instance
                        field.SetValue(evt, valueChangeEvent.newValue);
                        portEventField.FieldValue = field.GetValue(valueChangeEvent.newValue)?.ToString() ?? string.Empty;
                    });
                    toggle.name = "event-variable";
                    ResizeUILabel(toggle.Q<Label>(), fieldName);

                    generatedPort.contentContainer.Add(toggle);
                }
                else if (fieldType == typeof(GameObject))
                {
                    var objectField = new ObjectField(fieldName)
                    {
                        objectType = typeof(GameObject),
                        value = field.GetValue(evt) as GameObject

                    };
                    objectField.RegisterValueChangedCallback(valueChangeEvent =>
                    {
                        field.SetValue(evt, valueChangeEvent.newValue);
                        portEventField.FieldValue = field.GetValue(valueChangeEvent.newValue)?.ToString() ?? string.Empty;
                    });
                    objectField.name = "event-variable";
                    ResizeUILabel(objectField.Q<Label>(), fieldName);

                    generatedPort.contentContainer.Add(objectField);
                }
                portEventField.FieldName = fieldName;
                portEventField.FieldType = fieldType.Name;
                fieldList.Add(portEventField);
                //Add the port event field to the port data
                Debug.Log($"Added field: {fieldName} of type {fieldType.Name} with value {portEventField.FieldValue} to port {generatedPort.portName}");
            }
            
        }*/


    }
    public void AddChoicePort(Node node, string overridenPortName = "") //Default value is null so that it isn't required
    {
        var generatedPort = AddPort(node, Direction.Output);

        //Query the type and name of the port and then we can remove it
        var oldLabel = generatedPort.contentContainer.Q<Label>("type");
        oldLabel.visible = false;
        oldLabel.style.flexBasis = 0;

        var outputPortCount = node.outputContainer.Query("connector").ToList().Count;
        generatedPort.portName = " " + outputPortCount + 1;
        node.outputContainer.Add(generatedPort);

        //Check if there is an overridden port name if not use the default one
        var choicePortName = string.IsNullOrEmpty(overridenPortName) ? $"Choice {outputPortCount + 1}" : overridenPortName;

        //Create a text field to change the name of the port
        var textField = new TextField
        {
            name = string.Empty,
            value = choicePortName
        };
        //When the text field is the port name is updated 
        textField.RegisterValueChangedCallback(evt => generatedPort.portName = evt.newValue);
        generatedPort.contentContainer.Add(new Label("  ")); //Dummy label to create a gap
        //Prevent the text field from covering the output port
        var flexibleSpace = new VisualElement();
        flexibleSpace.style.flexGrow = 1;
        flexibleSpace.Add(textField);
        generatedPort.contentContainer.Add(flexibleSpace);
        
        var deleteButton = new Button(() => RemovePort(node, generatedPort))
        {
            text = "X"
        };
        generatedPort.contentContainer.Add(deleteButton);

        generatedPort.portName = choicePortName;
        node.RefreshExpandedState();
        node.RefreshPorts();
    }

    
    private void RemovePort(Node node, Port generatedPort)
    {
        //We get the portname and make sure it belongs to its respective node so we don't accidently delete the wrong one
        var targetEdge = edges.ToList().Where(x => x.output.portName == generatedPort.portName && x.output.node == generatedPort.node);
        //Then we delete the edge before removing the port so it isn't left behind
        if (targetEdge.Any())
        {
            var edge = targetEdge.First();
            edge.input.Disconnect(edge);
            RemoveElement(targetEdge.First());
        }

        node.outputContainer.Remove(generatedPort);
        //After removing the port the node and its ports need to be refreshed for visual updates
        node.RefreshPorts();
        node.RefreshExpandedState();
        

    }

    public void ClearBlackboardAndExposedProperties()
    {
        ExposedProperties.Clear();
        Blackboard.Clear();
    }

    public void AddPropertyToBlackboard(ExposedProperty exposedProperty)
    {
        var localPropertyName = exposedProperty.PropertyName;
        var localPropertyValue = exposedProperty.PropertyValue;
        //Check if the property name already exists in the blackboard and adds a tag if it does to prevent duplicates
        while (ExposedProperties.Any(x=> x.PropertyName == localPropertyName))
        {
            
            localPropertyName = $"{localPropertyName} (1)";
            
        }

        var property = new ExposedProperty();
        property.PropertyName = localPropertyName;
        property.PropertyValue = localPropertyValue;
        ExposedProperties.Add(property);

        var container = new VisualElement();
        var blackboardField = new BlackboardField {
            text = property.PropertyName,
            typeText = property.PropertyValue
        };
        
        container.Add(blackboardField);

        var propertyValueTextField = new TextField("Value")
        {
            value = localPropertyValue
        };
        propertyValueTextField.RegisterValueChangedCallback(evt =>
        {
           var changingPropertyIndex = ExposedProperties.FindIndex(x => x.PropertyName == property.PropertyName);
            ExposedProperties[changingPropertyIndex].PropertyValue = evt.newValue;
        });
        var blackboardValueRow = new BlackboardRow(blackboardField, propertyValueTextField);
        container.Add(blackboardValueRow);

        

        Blackboard.Add(container);
        
    }
}
