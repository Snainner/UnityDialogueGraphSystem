using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;

public class QuestLogManager : MonoBehaviour
{
    [SerializeField] GameObject questToast;
    [SerializeField] private RectTransform questList;
    [SerializeField] private RectTransform completedQuestList;
    private List<Objective> m_Objectives = new List<Objective>();
    private Dictionary<Objective, QuestToast> m_ObjectiveToastsDictionnary = new Dictionary<Objective, QuestToast>();
    void Awake()
    {
        EventManager.AddListener<ObjectiveUpdateEvent>(OnObjectiveUpdate);
        Objective.OnObjectiveCreated += RegisterObjective;
        Objective.OnObjectiveCompleted += UnregisterObjective;

    }
    public void RegisterObjective(Objective objective)
    {
        GameObject toastInstance = Instantiate(questToast, questList);
        QuestToast toast = toastInstance.GetComponent<QuestToast>();
        if (toast)
        {
            toast.CreateQuestPrefab(objective.Title, objective.Description, objective.IsCompleted, objective.IsOptional);
            m_ObjectiveToastsDictionnary.Add(objective,  toast);
        }
    }

    public void OnObjectiveUpdate(ObjectiveUpdateEvent evt)
    {
        if (m_ObjectiveToastsDictionnary.TryGetValue(evt.Objective, out QuestToast toast) &&
            toast != null)
        {
            if(!string.IsNullOrEmpty(evt.DescriptionText))
                toast.descriptionText.text = evt.DescriptionText;
            if(!string.IsNullOrEmpty(evt.CounterText))
                toast.counterText.text = evt.CounterText;
                
           
        }
    }

    void UnregisterObjective(Objective objective)
    {
        if  (m_ObjectiveToastsDictionnary.TryGetValue(objective, out QuestToast toast) && toast != null)
        {
            GameObject toastInstance = Instantiate(questToast, completedQuestList);
            QuestToast toaster = toastInstance.GetComponent<QuestToast>();
            if (toaster)
            {
                toaster.CreateQuestPrefab(objective.Title, objective.Description, objective.IsCompleted, objective.IsOptional);
            }
            //Could add it to a completed quest list for easy serialisation 
            m_ObjectiveToastsDictionnary.Remove(objective);
            toast.DestroyQuestPrefab();
        }
        EventManager.RemoveListener<ObjectiveUpdateEvent>(OnObjectiveUpdate);
        Objective.OnObjectiveCreated -= UnregisterObjective;
        Objective.OnObjectiveCompleted -= RegisterObjective;
    }
}
