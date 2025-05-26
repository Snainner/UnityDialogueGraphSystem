using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.FPS.Game;


public static class EventReflectionUtility
{

    public static IEnumerable<(string Name, GameEvent EventInstance)> GetAllEvents()
    {
        var eventType = typeof(Events);
        //Get all the public static fields of the Event class
        var fields = eventType.GetFields(BindingFlags.Public | BindingFlags.Static);
        foreach (var field in fields)
        {
            //Check if the field is of type GameEvent and assignable from GameEvent
            if (typeof(GameEvent).IsAssignableFrom(field.FieldType))
            {
                var value = field.GetValue(null) as GameEvent;
                yield return (field.Name, value);
            }
        }
    }

    public static FieldInfo[] GetGameEventFields(GameEvent gameEvent)
    {
        var type = gameEvent.GetType();
        //var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        return type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

    }

}