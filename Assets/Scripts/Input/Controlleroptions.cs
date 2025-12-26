using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class GameOptionsFocus : MonoBehaviour
{
    public Selectable firstSelectable;

    void OnEnable()
    {
        StartCoroutine(DelayedSelect());
    }

    IEnumerator DelayedSelect()
    {
        // wait one frame so StateMachine & EventSystem settle
        yield return null;

        if (EventSystem.current == null)
            yield break;

        EventSystem.current.SetSelectedGameObject(null);

        if (firstSelectable != null && firstSelectable.isActiveAndEnabled)
            firstSelectable.Select();
    }
}
