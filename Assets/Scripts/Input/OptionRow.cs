using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class OptionRow : MonoBehaviour, ISubmitHandler, IMoveHandler
{
    public TextMeshProUGUI valueText;
    public string[] values;

    int index = 0;

    void Start()
    {
        if (values.Length > 0)
            valueText.text = values[index];
    }

    // X / A button
    public void OnSubmit(BaseEventData eventData)
    {
        Next();
    }

    // D-pad / stick left-right
    public void OnMove(AxisEventData eventData)
    {
        if (eventData.moveDir == MoveDirection.Right)
        {
            Next();
            eventData.Use();
        }
        else if (eventData.moveDir == MoveDirection.Left)
        {
            Prev();
            eventData.Use();
        }
        else
        {
            // allow up/down to continue navigation
            ExecuteEvents.Execute(gameObject, eventData, ExecuteEvents.moveHandler);
        }
    }

    void Next()
    {
        index = (index + 1) % values.Length;
        valueText.text = values[index];
        Apply();
    }

    void Prev()
    {
        index = (index - 1 + values.Length) % values.Length;
        valueText.text = values[index];
        Apply();
    }

    void Apply()
    {
        // hook into SpaceshipOptions here
        // example:
        // SpaceshipOptions.SetResolution(values[index]);
    }
}
