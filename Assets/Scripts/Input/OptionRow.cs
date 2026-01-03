using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using GameOptionsUtility;

public class OptionRow : MonoBehaviour, ISubmitHandler, IMoveHandler
{
    public TextMeshProUGUI valueText;

    // Assign ONE of these per row in the Inspector
    public DropDownRenderResolution resolutionCycler;
    public DropDownQuality_Cycler qualityCycler;
    public HDRPAntiAliasing_Cycler aaCycler;
    public FPS_Cycler fpsCycler;
    public MetalFXDropdown metalFXCycler;

    // X / A button
    public void OnSubmit(BaseEventData eventData)
    {
        Debug.Log($"[OptionRow] OnSubmit fired on {gameObject.name}");
        ApplyNext();
    }

    // D-pad / stick left-right
    public void OnMove(AxisEventData eventData)
    {
        Debug.Log($"[OptionRow] OnMove {eventData.moveDir} on {gameObject.name}");

        if (eventData.moveDir == MoveDirection.Right)
        {
            ApplyNext();
            eventData.Use();
        }
        else if (eventData.moveDir == MoveDirection.Left)
        {
            ApplyPrev();
            eventData.Use();
        }
        // IMPORTANT:
        // Do nothing for Up / Down.
        // Unity handles vertical navigation automatically.
    }

    void ApplyNext()
    {
        if (resolutionCycler) resolutionCycler.Next();
        else if (qualityCycler) qualityCycler.Next();
        else if (aaCycler) aaCycler.Next();
        else if (fpsCycler) fpsCycler.Next();
        else if (metalFXCycler) metalFXCycler.OnRight();
    }

    void ApplyPrev()
    {
        if (resolutionCycler) resolutionCycler.Prev();
        else if (qualityCycler) qualityCycler.Prev();
        else if (aaCycler) aaCycler.Prev();
        else if (fpsCycler) fpsCycler.Prev();
        else if (metalFXCycler) metalFXCycler.OnLeft();
    }
}
