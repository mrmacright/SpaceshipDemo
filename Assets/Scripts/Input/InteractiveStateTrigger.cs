using UnityEngine;
using GameplayIngredients.StateMachines;

public class InteractiveStateTrigger : MonoBehaviour
{
    public StateMachine stateMachine;

    public void Activate()
    {
        if (stateMachine != null)
        {
            stateMachine.SetState("Active");
        }
        else
        {
            Debug.LogWarning("InteractiveStateTrigger: No stateMachine assigned!");
        }
    }
}
