using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Bg.DirectoryDuplicator.YamlDotNet.RepresentationModel;
using UnityEditor;
using UnityEngine;

namespace Bg.DirectoryDuplicator.Editor {
    public static class DirectoryDuplicator {
        public static void CopyDirectoryWithDependencies(string originDirectory, string targetDirectory) {
            if (!Directory.Exists(originDirectory)) {
                return;
            }
            
            CopyDirectory(originDirectory, targetDirectory);
            AssetDatabase.Refresh();

            ChangeGuidToNewFile(originDirectory, targetDirectory);
        }
        
        public static void CopyDirectory(string originDirectory, string targetDirectory) {
            DirectoryInfo directoryInfo = new DirectoryInfo(originDirectory);
            if (!directoryInfo.Exists) {
                return;
            }

            if (!Directory.Exists(targetDirectory)) {
                Directory.CreateDirectory(targetDirectory);
            }

            FileInfo[] files = directoryInfo.GetFiles("*", SearchOption.AllDirectories);
            foreach (var fileInfo in files) {
                if (fileInfo.Extension == ".meta") {
                    continue;
                }
                string tempDir = fileInfo.DirectoryName.Replace(originDirectory, targetDirectory);
                string tempPath = Path.Combine(tempDir, fileInfo.Name);
                CreateDirectoryIfNotExist(tempPath);
                AssetDatabase.CopyAsset(GetRelativePath(fileInfo.FullName), GetRelativePath(tempPath));
            }
        }

        private static void CreateDirectoryIfNotExist(string filePath) {
            string parentDirectory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(parentDirectory)) {
                Directory.CreateDirectory(parentDirectory);
            }
        }

        private static Dictionary<string, string> CreateGuidMap(string originDirectory, string targetDirectory) {
            var ret = new Dictionary<string, string>();

            var originAssetPaths = Directory.GetFiles(originDirectory, "*", SearchOption.AllDirectories).Where(itr => !itr.EndsWith(".meta")).ToList();
            var newAssetPaths = Directory.GetFiles(targetDirectory, "*", SearchOption.AllDirectories).Where(itr => !itr.EndsWith(".meta")).ToList();

            foreach (var newMetaPath in newAssetPaths) {
                string pathName = newMetaPath.Replace(targetDirectory, "");
                string originPath = originAssetPaths.FirstOrDefault(itr => itr.Contains(pathName));
                if (string.IsNullOrEmpty(originPath)) {
                    continue;
                }
                string originGuid = AssetDatabase.GUIDFromAssetPath(GetRelativePath(originPath)).ToString();
                string newGuid = AssetDatabase.GUIDFromAssetPath(GetRelativePath(newMetaPath)).ToString();

                ret[originGuid] = newGuid;
            }

            return ret;
        }

        private static string GetRelativePath(string path) {
            string ret = path.Replace(Application.dataPath, "");
            if (ret.StartsWith(Path.DirectorySeparatorChar.ToString())) {
                ret = ret.Remove(0, 1);
            }
            ret = "Assets/" + ret;
            return ret;
        }

        private static YamlStream LoadYaml(string path) {
            var input = new StringReader(File.ReadAllText(path));
            var yaml = new YamlStream();
            yaml.Load(input);
            return yaml;
        }

        private static void ChangeGuidToNewFile(string originDirectory, string targetDirectory) {
            var guidMap = CreateGuidMap(originDirectory, targetDirectory);
            var targetExt = new string[] {
                ".anim",".controller",".overrideController",".prefab",".mat",".material",".playable",".asset",".unity"
            };
            
            var newAssetPaths = Directory.GetFiles(targetDirectory, "*", SearchOption.AllDirectories).Where(itr => !itr.EndsWith(".meta")).ToList();
            foreach (var path in newAssetPaths) {
                if (targetExt.All(itr => !path.Contains(itr))) {
                    continue;
                }

                var yaml = LoadYaml(path);
                foreach (var doc in yaml.Documents) {
                    var root = doc.RootNode;
                    ChangeGuidToNewFileRecursively(path, string.Empty, root, guidMap);
                }
                StreamWriter sw = new StreamWriter(path);
                yaml.Save(sw, false);
                sw.Close();
                File.WriteAllText(path, ArrangeYaml(path.Replace(targetDirectory, originDirectory), path));
            }
        }

        private static void ChangeGuidToNewFileRecursively(string path, string nodeKey, YamlNode node, Dictionary<string, string> guidMap) {
            switch (node.NodeType) {
                case YamlNodeType.Mapping: {
                    var mappingNode = node as YamlMappingNode;
                    foreach (var entry in mappingNode.Children) {
                        ChangeGuidToNewFileRecursively(path, ((YamlScalarNode)entry.Key).Value, entry.Value, guidMap);
                    }
                    break;
                }
                case YamlNodeType.Sequence: {
                    var seqNode = node as YamlSequenceNode;
                    foreach (var yamlNode in seqNode.Children) {
                        ChangeGuidToNewFileRecursively(path, string.Empty, yamlNode, guidMap);
                    }
                    break;
                }
                case YamlNodeType.Scalar: {
                    if (nodeKey == "guid") {
                        var scalarNode = node as YamlScalarNode;
                        if (guidMap.ContainsKey(scalarNode.Value)) {
                            scalarNode.Value = guidMap[scalarNode.Value];
                        }
                    } 
                    break;
                }
            }
        }

        private static string ArrangeYaml(string originPath, string newPath) {
            StringBuilder sb = new StringBuilder(File.ReadAllText(newPath));

            sb.Replace("...\n", "");

            var originReg = new Regex("&[-,0-9]+");
            var targetReg = new Regex(".*&[-,0-9]+");
            var matches = targetReg.Matches(sb.ToString());
            var matchLines = new List<string>();
            for (int i = 0; i < matches.Count; i++) {
                matchLines.Add(matches[i].Value);
            }

            StringReader sr = new StringReader(File.ReadAllText(originPath));
            while (sr.Peek() > -1) {
                string line = sr.ReadLine();
                if (line != null && line.StartsWith("---")) {
                    var idMatch = originReg.Match(line);
                    var targetMatch = matchLines.FirstOrDefault(itr => itr.Contains(idMatch.Value));
                    if (targetMatch != null) sb.Replace(targetMatch, line);
                }
            }

            string header = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
";
            sb.Insert(0, header);

            return sb.ToString();
        }
    }
}