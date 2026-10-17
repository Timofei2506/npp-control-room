// Заглушки UnityEditor — только для офлайн-проверки Assets/Editor/*.cs.
using System;
using UnityEngine;

namespace UnityEngine.SceneManagement
{
    public struct Scene
    {
        public string name { get { return ""; } }
        public string path { get { return ""; } }
        public int rootCount { get { return 0; } }
        public bool isDirty { get { return false; } }
        public bool IsValid() { return true; }
    }
    public static class SceneManager
    {
        public static Scene GetActiveScene() { return new Scene(); }
    }
}

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItemAttribute : Attribute { public MenuItemAttribute(string item) { } }

    [AttributeUsage(AttributeTargets.Class)]
    public class InitializeOnLoadAttribute : Attribute { }

    public static class EditorApplication
    {
        public delegate void CallbackFunction();
        public static CallbackFunction delayCall;
        public static bool isPlayingOrWillChangePlaymode { get { return false; } }
        public static bool isPlaying { get { return false; } }
    }

    public static class AssetDatabase
    {
        public static void Refresh() { }
        public static void SaveAssets() { }
    }

    public enum BuildTarget { StandaloneWindows64, StandaloneWindows, StandaloneLinux64 }
    public enum BuildOptions { None }

    public class PlayerSettings
    {
        public static string companyName { get; set; }
        public static string productName { get; set; }
        public static string bundleVersion { get; set; }
        public static UnityEngine.FullScreenMode fullScreenMode { get; set; }
        public static int defaultScreenWidth { get; set; }
        public static int defaultScreenHeight { get; set; }
        public static bool resizableWindow { get; set; }
        public static bool runInBackground { get; set; }
        public static UnityEngine.ColorSpace colorSpace { get; set; }
    }

    public class EditorBuildSettingsScene
    {
        public string path { get; set; }
        public bool enabled { get; set; }
        public EditorBuildSettingsScene(string path, bool enabled) { this.path = path; this.enabled = enabled; }
    }

    public static class EditorBuildSettings
    {
        public static EditorBuildSettingsScene[] scenes { get; set; }
    }

    public static class EditorUtility
    {
        public static void DisplayProgressBar(string title, string info, float progress) { }
        public static void ClearProgressBar() { }
    }
}

namespace UnityEditor.Build.Reporting
{
    public class BuildReport
    {
        public BuildSummary summary { get { return new BuildSummary(); } }
    }
    public struct BuildSummary
    {
        public BuildResult result { get { return BuildResult.Succeeded; } }
        public ulong totalSize { get { return 0; } }
    }
    public enum BuildResult { Unknown, Succeeded, Failed, Cancelled }
}

namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
    public static class EditorSceneManager
    {
        public static UnityEngine.SceneManagement.Scene GetActiveScene() { return new UnityEngine.SceneManagement.Scene(); }
        public static bool SaveScene(UnityEngine.SceneManagement.Scene scene, string path) { return true; }
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() { return true; }
        public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup setup, NewSceneMode mode) { return new UnityEngine.SceneManagement.Scene(); }
    }

    public class SceneSetup { }
}

namespace UnityEditor.Build
{
    public class BuildPlayerOptions
    {
        public string[] scenes { get; set; }
        public string locationPathName { get; set; }
        public UnityEditor.BuildTarget target { get; set; }
        public UnityEditor.BuildOptions options { get; set; }
    }
}

namespace UnityEditor
{
    public static class BuildPipeline
    {
        public static UnityEditor.Build.Reporting.BuildReport BuildPlayer(UnityEditor.Build.BuildPlayerOptions opts) { return new UnityEditor.Build.Reporting.BuildReport(); }
    }
}
