using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Events;

public class StaticDialogueManager : MonoBehaviour
{
    [System.Serializable]
    public class DialogueStep {
        [TextArea(2, 5)] public string npcTalk;   

        [Header("End Conversation")]    
        public bool isEndNode;

        [Header("Event to Trigger")]
        public UnityEvent onNodeReached; 

        [Header("--- Only if NOT an End Node ---")]
        public string optLeft;
        public int nextIndexLeft;

        public string optRight;
        public int nextIndexRight;
    }

    [Header("UI References")]
    public TMP_Text dialogueText;
    public Button leftButton;
    public Button rightButton;

    [Header("Dialogue Content")]
    public List<DialogueStep> dialogueList; 
    private int currentIndex = 0;

    void Start()
    {
        // Add listeners once at the start
        leftButton.onClick.AddListener(() => OnChoiceSelected(true));
        rightButton.onClick.AddListener(() => OnChoiceSelected(false));

        UpdateUI();
    }

    void UpdateUI()
    {
        // Check if we are within the list bounds
        if (currentIndex >= 0 && currentIndex < dialogueList.Count)
        {
            DialogueStep current = dialogueList[currentIndex];
            current.onNodeReached?.Invoke();
            
            // If the current step is marked as the end, wrap it up
            if (current.isEndNode)
            {
                EndConversation(current.npcTalk);
                return;
            }

            dialogueText.text = current.npcTalk;
            leftButton.GetComponentInChildren<TMP_Text>().text = current.optLeft;
            rightButton.GetComponentInChildren<TMP_Text>().text = current.optRight;
        }
        else
        {
            EndConversation("...");
        }
    }

    void OnChoiceSelected(bool isLeft)
    {
        DialogueStep current = dialogueList[currentIndex];
        currentIndex = isLeft ? current.nextIndexLeft : current.nextIndexRight;
        
        UpdateUI();
    }

    void EndConversation(string finalMessage)
    {
        dialogueText.text = finalMessage;
        leftButton.gameObject.SetActive(false);
        rightButton.gameObject.SetActive(false);
        Debug.Log("Dialogue Finished.");
    }
}