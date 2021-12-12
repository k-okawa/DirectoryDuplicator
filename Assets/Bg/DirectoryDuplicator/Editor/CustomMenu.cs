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
        static void DuplicateDirectoryWithDependencies() {
            Object[] selectedAsset = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
            if (selectedAsset.Length != 1) {
                EditorUtility.DisplayDialog(DIALOG_TITLE, "Please select a directory", "OK", "");
            }
            string directoryPath = AssetDatabase.GetAssetPath(selectedAsset.First());
            directoryPath = Path.GetFullPath(directoryPath);
            if (!Directory.Exists(directoryPath)) {
                EditorUtility.DisplayDialog(DIALOG_TITLE, "Please select a directory", "OK", "");
            }

            string newDirectoryPath = directoryPath + "(copy)";
            int count = 1;
            while (Directory.Exists(newDirectoryPath)) {
                newDirectoryPath = directoryPath + $"(copy{count})";
            }
            
            DirectoryDuplicator.CopyDirectoryWithDependencies(directoryPath, newDirectoryPath);
        }
    }
}