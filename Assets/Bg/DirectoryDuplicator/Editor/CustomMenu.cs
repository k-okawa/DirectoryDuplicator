using System.IO;
using System.Linq;
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
            string path = AssetDatabase.GetAssetPath(selectedAsset.First());
            if (!Directory.Exists(path)) {
                EditorUtility.DisplayDialog(DIALOG_TITLE, "Please select a directory", "OK", "");
            }

            var allFilePaths = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                .Where(itr => Path.GetExtension(itr) != ".meta").ToList();
            
            
        }
    }
}