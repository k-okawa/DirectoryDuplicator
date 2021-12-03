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
            string path = AssetDatabase.GetAssetPath(selectedAsset.First());
            if (!Directory.Exists(path)) {
                EditorUtility.DisplayDialog(DIALOG_TITLE, "Please select a directory", "OK", "");
            }

            var allFilePaths = Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                .Where(itr => Path.GetExtension(itr) != ".meta").ToList();
            
            
        }

        [MenuItem("Assets/Bg/TestYaml")]
        static void TestYaml() {
            Object[] selectedAsset = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
            string path = AssetDatabase.GetAssetPath(selectedAsset.First());
            var input = new StringReader(File.ReadAllText(path));
            var yaml = new YamlStream();
            yaml.Load(input);

            foreach (var doc in yaml.Documents) {
                var mapping = (YamlMappingNode)doc.RootNode;
                foreach (var entry in mapping.Children)
                {
                    Debug.Log($"{((YamlScalarNode)entry.Key).Value}");
                    var childMap = (YamlMappingNode)entry.Value;
                    foreach (var childEntry in childMap.Children) {
                        if (childEntry.Value.NodeType == YamlNodeType.Scalar) {
                            Debug.Log($"{((YamlScalarNode)childEntry.Key).Value}:{((YamlScalarNode)childEntry.Value).Value}");
                        } else if(childEntry.Value.NodeType == YamlNodeType.Mapping) {
                            Debug.Log(childEntry.Value.ToString());
                        }
                    }
                }
                
            }
        }
    }
}