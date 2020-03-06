 #if UNITY_EDITOR
 
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;

    class EditorBuild : IPostprocessBuildWithReport {

        public int callbackOrder { get { return 0; } }

        public void OnPostprocessBuild(BuildReport repoort) {
            FileUtil.ReplaceFile("changelog.txt", "../Build/changelog.txt");
        }
    }
 
 #endif