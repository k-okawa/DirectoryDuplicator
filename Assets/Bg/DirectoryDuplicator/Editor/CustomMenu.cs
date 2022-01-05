using System.IO;
using System.Linq;
using Bg.DirectoryDuplicator.YamlDotNet.Core.Tokens;
using Bg.DirectoryDuplicator.YamlDotNet.RepresentationModel;
using UnityEditor;
using UnityEngine;

namespace Bg.DirectoryDuplicator.Editor {
    public static class CustomMenu {
        private const string DIALOG_TITLE = "BgDirectoryDuplicator";
        
        [MenuItem("Assets/Bg/DuplicateDirectoryWithDependencies")]
        static async void DuplicateDirectoryWithDependencies() {
            Object[] selectedAsset = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
            if (selectedAsset.Length != 1) {
                EditorUtility.DisplayDialog("DirectoryDuplicator", "Please select a directory", "OK", "");
                return;
            }
            string directoryPath = AssetDatabase.GetAssetPath(selectedAsset.First());
            directoryPath = Path.GetFullPath(directoryPath);
            if (!Directory.Exists(directoryPath)) {
                EditorUtility.DisplayDialog("DirectoryDuplicator", "Please select a directory", "OK", "");
                return;
            }
            if (directoryPath == Application.dataPath) {
                EditorUtility.DisplayDialog("DirectoryDuplicator", "Please select a directory under the assets path", "OK", "");
                return;
            }

            string newDirectoryPath = directoryPath + "(copy)";
            int count = 1;
            while (Directory.Exists(newDirectoryPath)) {
                newDirectoryPath = directoryPath + $"(copy{count})";
            }
        
            EditorUtility.DisplayProgressBar("DirectoryDuplicator", "Executing directory copy and reference migration", 0);
            try {
                await DirectoryDuplicator.CopyDirectoryWithDependencies(directoryPath, newDirectoryPath, null,
                    ret => {
                        EditorUtility.DisplayProgressBar("DirectoryDuplicator",
                            $"Executing directory copy and reference migration {ret.progress}/{ret.total}",
                            ret.progress / (float)ret.total);
                    });
            }
            finally {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}