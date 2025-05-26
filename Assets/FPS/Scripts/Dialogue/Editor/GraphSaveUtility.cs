using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Unity.FPS.Game;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphSaveUtility
{
    private DialogueGraphView _targetGraphView;
    private DialogueContainer _containerCache;
    private List<Edge> Edges => _targetGraphView.edges.ToList();
    private List<DialogueNode> Nodes => _targetGraphView.nodes.ToList().Cast<DialogueNode>().ToList(); //We need to cast the nodes to DialogueNode type so we can easily acces it via the GUID of in the dialogue node class 
    public static GraphSaveUtility GetInstance(DialogueGraphView targetGraphView)
    {
        return new GraphSaveUtility
        {
            _targetGraphView = targetGraphView
        };
    }

    public void SaveGraph(string fileName)
    {
        var entryPoint = Edges.Find(x=>x.output.node.title == "Start");
        if (entryPoint == null)
        {
            EditorUtility.DisplayDialog("Error", "Start node needs to be connected before saving", "OK");
            return;
        }

        var dialogueContainer = ScriptableObject.CreateInstance<DialogueContainer>();
        if (!SaveNodes(dialogueContainer)) return;

        SaveExposedProperties(dialogueContainer);

        if (!AssetDatabase.IsValidFolder("Assets/Resources")) //Check if the folder exists
        {
            //if not create a new folder in the assets folder
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        //To prevent saved objects from dissepearing when it is saved without edits it is first copied then set dirty to the save. 
        //
        var existingAsset = AssetDatabase.LoadAssetAtPath<DialogueContainer>($"Assets/Resources/{fileName}.asset");
        if (existingAsset != null)
        {
            //EditorUtility.DisplayDialog("Warning", $"The file {fileName} already exists, it will be overwritten", "OK");
            EditorUtility.CopySerialized(existingAsset, dialogueContainer);
            EditorUtility.SetDirty(dialogueContainer);
            AssetDatabase.SaveAssets();
        }
        else
        {
            AssetDatabase.CreateAsset(dialogueContainer, $"Assets/Resources/{fileName}.asset");
            AssetDatabase.SaveAssets();
        }
    }

    private bool SaveNodes(DialogueContainer dialogueContainer)
    {
        if (!Edges.Any()) return false; //If there aren't any edges we return 


        //When an output port is connected to in an input port its considered valid 
        //To prevent wrong connections on load we ensure the edge with the entry node as its output is the first one in the array by using OrderByDescending 
        var connectedPorts = Edges.Where(x => x.input.node != null &&(x.output.node is DialogueNode || x.output.node is EventNode))
        .OrderByDescending(x =>
        {
            //if the output node is a dialogue node we check if it is an entry point
            if (x.output.node is DialogueNode dialogueNode)
            {
                //Check if dialogue node is an entry point
                return dialogueNode.EntryPoint ? 1: 0;
            }
            //if its an event node we return 0 as it can't be an entry point
            return 0;
        }).ToArray();

        //previous code for adding edges to an array
        //var connectedPorts = Edges.Where(x => x.input.node != null).ToArray();
        //var connectedPorts = Edges.Where(x => x.input.node != null).OrderByDescending(x=> ((DialogueNode)(x.output.node)).EntryPoint).ToArray();

        for (int i = 0; i < connectedPorts.Length; i++)
        {
            /*
            check whether the output node is a dialogue node or an event node 
            based on that we create the NodeLinkData with or without the portEvent
            We can use the as operator to check if the node is of a certain type and cast it at the same time 
            Because the event node is extended from the dialogue node the event node needs to be first 
            */

            var outputEventNode = connectedPorts[i].output.node as EventNode;
            var inputEventNode = connectedPorts[i].input.node as EventNode;
            var outputDialogueNode = connectedPorts[i].output.node as DialogueNode;
            var inputDialogueNode = connectedPorts[i].input.node as DialogueNode;
            if (outputEventNode != null)
            {                
                dialogueContainer.NodeLinks.Add(new NodeLinkData
                {
                    BaseNodeGuid = outputEventNode.GUID,
                    PortName = connectedPorts[i].output.portName,
                    //BasePortData = connectedPorts[i].output.portData, 
                    //EventSelectionIndex = outputEventNode.EventSelectionIndex,
                    TargetNodeGuid = inputEventNode != null ? inputEventNode.GUID : inputDialogueNode.GUID,
                    
                });
            }
            else 
            {
                dialogueContainer.NodeLinks.Add(new NodeLinkData
                {
                    BaseNodeGuid = outputDialogueNode.GUID,
                    //port name is set to user input value in graphview 
                    PortName = connectedPorts[i].output.portName,
                    TargetNodeGuid = inputEventNode != null ? inputEventNode.GUID : inputDialogueNode.GUID,
                    
                });
            }
        }

        foreach (var dialogueNode in Nodes.Where(node => !node.EntryPoint))
        {
            if(dialogueNode is EventNode eventNode)
            {
                dialogueContainer.EventNodeData.Add(new EventNodeData
                {
                    NodeGUID = eventNode.GUID,
                    Position = eventNode.GetPosition().position
                });
                    
            }
            else
            {
                dialogueContainer.DialogueNodeData.Add(new DialogueNodeData
                {
                    NodeGUID = dialogueNode.GUID,
                    DialogueText = dialogueNode.DialogueText,
                    Position = dialogueNode.GetPosition().position, //Get position returns a rect rather then a vector2
                   
                }); 
            }
        }

        return true;
    }

    private void SaveExposedProperties(DialogueContainer dialogueContainer)
    {
       dialogueContainer.ExposedProperties.AddRange(_targetGraphView.ExposedProperties);
       
    }
    public void LoadGraph(string fileName)
    {
        _containerCache = Resources.Load<DialogueContainer>(fileName);
        //Check if the file exists 
        if (_containerCache == null)
        {
            Debug.LogError($"Could not find the file {fileName}");
            return;
        }

        //Clear the graph view to make sure there are no renmants leftover
        ClearGraph();
        CreateNodes(true);
        CreateNodes(false);
        ConnectNodes();
        CreateExposedProperties();
    }

    private void CreateExposedProperties()
    {
        _targetGraphView.ClearBlackboardAndExposedProperties();
        foreach (var exposedProperty in _containerCache.ExposedProperties)
        {
            _targetGraphView.AddPropertyToBlackboard(exposedProperty);
        }
    }
    private void ConnectNodes()
    {
        for (int i =0; i < Nodes.Count ; i++)
        {
            //i is used in a lambda expression in the for loop and therefor modified we need to make sure i is not modified when the lambda is executed later on
            //Because lambda gets i as a reference and not as a value, it will always use the last value of i when the lambda is executed
            var k = i; //Prevent acces to modified closure 
            //We get the node links from the cached savefile and then check the connections based on the guids
            var connections = _containerCache.NodeLinks.Where(x => x.BaseNodeGuid == Nodes[k].GUID).ToList();
            for(int j =0; j< connections.Count; j++)
            {
                var targetNodeGuid = connections[j].TargetNodeGuid;
                var targetNode = Nodes.First(x => x.GUID == targetNodeGuid);
                LinkNodes(Nodes[i].outputContainer[j].Q<Port>(),(Port)targetNode.inputContainer[0]);
                
                var eventNode = targetNode as EventNode;
                if (eventNode != null)
                {
                    targetNode.SetPosition(new Rect(_containerCache.EventNodeData.First(x => x.NodeGUID == targetNodeGuid).Position, _targetGraphView.defaultNodeSize));
                }
                else
                {
                    targetNode.SetPosition(new Rect(_containerCache.DialogueNodeData.First(x => x.NodeGUID == targetNodeGuid).Position, _targetGraphView.defaultNodeSize));
                }
            }

        }
    }

    private void LinkNodes(Port output, Port input)
    {
        var tempEdge = new Edge
        {
            output = output,
            input = input
        };

        tempEdge?.input.Connect(tempEdge);
        tempEdge?.output.Connect(tempEdge);
        _targetGraphView.Add(tempEdge);
    }
    private void ClearGraph()
    {
        //Because we always spawn an entrypoint when creating the graph we need to change it to the saved one first 
        Nodes.Find(x => x.EntryPoint).GUID = _containerCache.NodeLinks[0].BaseNodeGuid;

        foreach (var node in Nodes)
        {
            if (node.EntryPoint) continue;
            //We only save output ports 
            //We validate the connections and then remove them
            Edges.Where(x => x.input.node == node).ToList().ForEach(edge => _targetGraphView.RemoveElement(edge));

            //We can now safely remove the nodes
            _targetGraphView.RemoveElement(node);
        }
    }


    private void CreateNodes(bool nodeDataType)
    {
        if(nodeDataType)
        {
            foreach (var nodeData in _containerCache.DialogueNodeData)
            {
                //The position is passed in later, so for now a dummy position is used
                var tempNode = _targetGraphView.GenerateDialogueNode(nodeData.DialogueText, Vector2.zero);
                tempNode.GUID = nodeData.NodeGUID;



                _targetGraphView.AddElement(tempNode);

                var nodePorts = _containerCache.NodeLinks.Where(x => x.BaseNodeGuid == nodeData.NodeGUID).ToList();
                nodePorts.ForEach(x => _targetGraphView.AddChoicePort(tempNode, x.PortName));
            }
        }
        else
        {
            foreach (var nodeData in _containerCache.EventNodeData)
            {
                //The position is passed in later, so for now a dummy position is used
                var tempNode = _targetGraphView.GenerateEventNode("Event Node", Vector2.zero);
                tempNode.GUID = nodeData.NodeGUID;



                _targetGraphView.AddElement(tempNode);

                var nodePorts = _containerCache.NodeLinks.Where(x => x.BaseNodeGuid == nodeData.NodeGUID).ToList();
                
                nodePorts.ForEach(x => {
                    _targetGraphView.AddEventPort(tempNode ,x.PortName);
                    
                    //From that port we get the port data 
                });
            }
        }
    
    }
}
