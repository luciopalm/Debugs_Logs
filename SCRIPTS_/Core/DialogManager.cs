using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DialogManager : MonoBehaviour
{
    [SerializeField] GameObject dialogBox;
    [SerializeField] Text dialogText;
    [SerializeField] int lettersPerSecond;
    
    public event Action OnShowDialog;
    public event Action OnHideDialog;

    public static DialogManager Instance {get; private set; }

    private void Awake()
    {
        Instance = this;
    }
    Dialog dialog;
    int currentLine = 0;
    bool isTyping;
    public bool dialogIsOpen = false;

    public IEnumerator ShowDialog(Dialog dialog)
    {   
        if (dialog == null || dialog.Lines == null || dialog.Lines.Count == 0)
        {   
            Debug.LogError("Dialog vazio ou nulo!");
            dialogIsOpen = false;
            yield break;
        }

        if (dialogIsOpen) yield break;
        
        dialogIsOpen = true;
        OnShowDialog?.Invoke();
        this.dialog = dialog;
        dialogBox.SetActive(true);
        currentLine = 0;
        StartCoroutine(TypeDialog(dialog.Lines[currentLine]));
        
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            pc.canInteract = false;
        }
        yield return null;
    }

    public void HandleUpdate()
    {   
        if (Input.GetKeyDown(KeyCode.Z) && !isTyping)
        {
            ++currentLine;
            if (currentLine < dialog.Lines.Count)
            {
                StartCoroutine(TypeDialog(dialog.Lines[currentLine]));
            }
            else
            {
                dialogBox.SetActive(false);
                currentLine = 0;
                dialogIsOpen = false;

                PlayerController pc = FindFirstObjectByType<PlayerController>();
                if (pc != null)
                {
                    StartCoroutine(EnableInteractionNextFrame(pc));
                }
                
                OnHideDialog?.Invoke();
            }
        }
    }

    private IEnumerator EnableInteractionNextFrame(PlayerController pc)
    {
         yield return null; // espera 1 frame
         pc.canInteract = true;
    }

    public IEnumerator TypeDialog(string line)
    {   
        if (string.IsNullOrEmpty(line))
        {
            isTyping = false;
            yield break;
        }

        dialogText.text = "";
        isTyping = true;
        
        foreach (var letter in line.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(1f / lettersPerSecond);
        }
        isTyping = false;
    }
}