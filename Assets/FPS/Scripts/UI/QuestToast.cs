using TMPro;
using UnityEngine;

public class QuestToast : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI counterText;
    public TextMeshProUGUI isOptionalText;
    private bool isCompleted;

    public void CreateQuestPrefab(string title, string description, bool completed, bool isOptional, string counterText = "")
    {
        titleText.text = title;
        descriptionText.text = description;
        isOptionalText.text = isOptional ? "Optional" : "";
        isCompleted = completed;
        if(isCompleted){}
            //change colour or something  
    }

    public void DestroyQuestPrefab()
    {
        Destroy(gameObject); 
    }
}
