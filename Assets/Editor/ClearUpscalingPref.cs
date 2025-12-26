using UnityEditor;
  using UnityEngine;

  public static class ClearUpscalingPref
  {
      [MenuItem("Tools/Clear Upscaling Pref")]
      public static void Clear()
      {
          PlayerPrefs.DeleteKey("GameOptions.Spaceship.UpsamplingMethod");
          PlayerPrefs.Save();
          Debug.Log("Cleared GameOptions.Spaceship.UpsamplingMethod");
      }
  }