using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

[System.Serializable]
public class NodeLinkData 
{
    public string PortName;
    public string BaseNodeGuid;
    public string TargetNodeGuid;
    public List<PortEventField> PortUserData;
}
