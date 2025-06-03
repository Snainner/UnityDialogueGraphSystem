using System;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    PlayerInputHandler m_PlayerInputHandler;
    /*Extend the dialogue parser into this so it is called from here and hopefully being able to make the parser static
     On dialogue activation, enable UI elements and disable movement.
     Lastly, use movement keys to pick dialogue options*/
    private void Start()
    {
        //m_PlayerInputHandler = GetComponent<PlayerInputHandler>();
        
    }

    private void OnEnable()
    {
        //EventManager.AddListener<DialogueEvent>(ControlDialogue);
    }

    private void OnDisable()
    {
        //EventManager.RemoveListener<DialogueEvent>(ControlDialogue);
    }

    private void ControlDialogue(DialogueEvent evt)
    {
        //disable movement 
        //enable cursor and use that for now
        
      
        
    }
}
