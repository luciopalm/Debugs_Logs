using UnityEngine;
using System.Collections;

public class NPCController : MonoBehaviour, Interactable
{
    [SerializeField] Dialog dialog;
    
    public void Interact()
    {   
        if (DialogManager.Instance.dialogIsOpen)
            return;
        StartCoroutine(DialogManager.Instance.ShowDialog(dialog));
    }
}