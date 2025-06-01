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
    
    public string EventName; 

    public string FieldName;
    public string FieldType;
    public string FieldValue; 

}