using GameplayIngredients;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;
using System.Text;
using UnityEngine.InputSystem;

[ManagerDefaultPrefab("VFXDebugManager")]
public class VFXDebugManager : Manager
{
    [Header("UI")]
    public GameObject uiRoot;
    public Text debugText;

    const Key Toggle = Key.F7;
    const Key PrevFX = Key.PageUp;
    const Key NextFX = Key.PageDown;
    const Key PlayKey = Key.I;
    const Key StopKey = Key.U;
    const Key PauseKey = Key.P;
    const Key ReinitKey = Key.J;
    const Key StepKey = Key.K;
    const Key SortKey = Key.M;
    const Key ToggleVisibilityKey = Key.L;

    bool visible = false;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current[Toggle].wasPressedThisFrame)
        {
            visible = !visible;
            if (uiRoot != null) uiRoot.SetActive(visible);
        }

        if (visible && debugText != null)
        {
            debugText.text = UpdateVFXDebug();
        }
    }

    int selectedVFX = -1;
    Sorting sorting = Sorting.None;

    enum Sorting
    {
        None = 0,
        DistanceToCamera = 1,
        ParticleCount = 2
    }

    string UpdateVFXDebug()
    {
        var keyboard = Keyboard.current;
        VisualEffect[] allEffects = VFXManager.GetComponents();

        if (keyboard[SortKey].wasPressedThisFrame)
            sorting = (Sorting)(((int)sorting + 1) % 3);

        if (sorting == Sorting.DistanceToCamera)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                sorting = Sorting.ParticleCount;
            }
            else
            {
                allEffects = allEffects.OrderBy(o =>
                    Vector3.SqrMagnitude(o.gameObject.transform.position - camera.transform.position)
                ).ToArray();
            }
        }

        if (sorting == Sorting.ParticleCount)
        {
            allEffects = allEffects.OrderBy(o => -o.aliveParticleCount).ToArray();
        }

        if (allEffects.Length == 0)
            return "No Active VFX Components in scene";

        selectedVFX -= keyboard[PrevFX].wasPressedThisFrame ? 1 : 0;
        selectedVFX += keyboard[NextFX].wasPressedThisFrame ? 1 : 0;

        bool shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

        selectedVFX = Mathf.Clamp(selectedVFX, 0, allEffects.Length - 1);

        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"{allEffects.Length} Visual Effect Component(s) active. Sorting : {sorting}");
        sb.AppendLine();
        sb.AppendLine($"{"Game Object Name",-24}| {"Visual Effect Asset",-24}| {"PlayState",-12}| {"Visibility",-12}| {"Particle Count",12}");
        sb.AppendLine($"===================================================================================================================================");

        int idx = 0;

        foreach (var vfx in allEffects)
        {
            if (idx == selectedVFX)
                sb.Append("<color=orange>");

            string gameObjectname = vfx.gameObject.name;
            string vfxName = vfx.visualEffectAsset == null ? "(No VFX Asset)" : vfx.visualEffectAsset.name;
            string playState = vfx.pause ? "Paused" : "Playing";
            var renderer = vfx.GetComponent<Renderer>();
            string visibility = renderer.enabled ? (vfx.culled ? "Culled" : "Visible") : "Disabled";
            string particleCount = vfx.aliveParticleCount.ToString();

            sb.Append($"{gameObjectname,-24}| {vfxName,-24}| {playState,-12}| {visibility,-12}| {particleCount,12}");

            if (idx == selectedVFX)
                sb.Append("</color>");

            sb.Append("\n");
            idx++;
        }

        var selected = allEffects[selectedVFX];

        if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
        {
            var selectedRenderer = selected.GetComponent<Renderer>();
            selectedRenderer.enabled = Time.unscaledTime % 0.5f < 0.25f;
        }

        if (shift)
        {
            foreach (var vfx in allEffects)
            {
                if (keyboard[PlayKey].wasPressedThisFrame) vfx.Play();
                if (keyboard[StopKey].wasPressedThisFrame) vfx.Stop();
                if (keyboard[PauseKey].wasPressedThisFrame) vfx.pause = !vfx.pause;
                if (keyboard[ReinitKey].wasPressedThisFrame) vfx.Reinit();
                if (keyboard[StepKey].wasPressedThisFrame) vfx.AdvanceOneFrame();
                if (keyboard[ToggleVisibilityKey].wasPressedThisFrame)
                    vfx.GetComponent<Renderer>().enabled = !vfx.GetComponent<Renderer>().enabled;
            }
        }
        else
        {
            if (keyboard[PlayKey].wasPressedThisFrame) selected.Play();
            if (keyboard[StopKey].wasPressedThisFrame) selected.Stop();
            if (keyboard[PauseKey].wasPressedThisFrame) selected.pause = !selected.pause;
            if (keyboard[ReinitKey].wasPressedThisFrame) selected.Reinit();
            if (keyboard[StepKey].wasPressedThisFrame) selected.Simulate(Time.deltaTime);
            if (keyboard[ToggleVisibilityKey].wasPressedThisFrame)
                selected.GetComponent<Renderer>().enabled = !selected.GetComponent<Renderer>().enabled;
        }

        return sb.ToString();
    }
}
