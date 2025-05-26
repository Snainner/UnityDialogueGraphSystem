using NUnit.Framework;
using System.Collections.Generic;

[System.Serializable]
public class PortData 
{
    public string PortGUID; 
    public List<PortEventField> EventFields = new List<PortEventField>();
}

[System.Serializable]
public class PortEventField
{
    public string PortGUID; // The GUID of the port this event field is connected to
    public string EventName; 

    public string FieldName;
    public string FieldType;
    public string FieldValue; 

}