using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bg.DirectoryDuplicator.YamlDotNet.RepresentationModel;
using UnityEditor;
using UnityEngine;

namespace Bg.DirectoryDuplicator.Editor {
    public static class DirectoryDuplicator {
        /// <summary>
        /// Copy directory and change guid dependencies in target directory
        /// </summary>
        /// <param name="originDirectory">original directory absolute path</param>
        /// <param name="targetDirectory">copy destination directory absolute path</param>
        /// <param name="copyExcludeDirectories">exclude sub directories that included in origin directory from copy</param>
        /// <param name="progressCallback">callback of progress. returns progress and total count of file count</param>
        public static async Task CopyDirectoryWithDependencies(string originDirectory, string targetDirectory, string[] copyExcludeDirectories = null, Action<(int progress, int total)> progressCallback = null) {
            if (!Directory.Exists(originDirectory)) {
                return;
            }
            
            CopyDirectory(originDirectory, targetDirectory, copyExcludeDirectories);
            AssetDatabase.Refresh();
            
            await ChangeGuidToNewFile(originDirectory, targetDirectory, progressCallback);
            AssetDatabase.Refresh();
        }
        
        /// <summary>
        /// Copy directory(sub directories included)
        /// </summary>
        /// <param name="originDirectory">original directory absolute path</param>
        /// <param name="targetDirectory">copy destination directory absolute path</param>
        /// <param name="copyExcludeDirectories">exclude sub directories that included in origin directory from copy</param>
        public static void CopyDirectory(string originDirectory, string targetDirectory, string[] copyExcludeDirectories = null) {
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
                if (copyExcludeDirectories != null && copyExcludeDirectories.Any(itr => fileInfo.FullName.Contains(itr))) {
                    continue;
                }
                string tempDir = fileInfo.DirectoryName.Replace(originDirectory, targetDirectory);
                string tempPath = Path.Combine(tempDir, fileInfo.Name);
                CreateDirectoryIfNotExist(tempPath);
                AssetDatabase.CopyAsset(GetRelativePath(fileInfo.FullName), GetRelativePath(tempPath));
            }
        }
        
        /// <summary>
        /// Change guid dependencies in target directory
        /// </summary>
        /// <param name="originDirectory">original directory absolute path</param>
        /// <param name="targetDirectory">copy destination directory absolute path</param>
        /// <param name="progressCallback">callback of progress. returns progress and total count of file count</param>
        public static async Task ChangeGuidToNewFile(string originDirectory, string targetDirectory, Action<(int progress, int total)> progressCallback = null) {
            var guidMap = CreateGuidMap(originDirectory, targetDirectory);
            var targetExt = new string[] {
                ".anim",".controller",".overrideController",".prefab",".mat",".material",".playable",".asset",".unity"
            };

            List<Task> taskList = new List<Task>();
            var newAssetPaths = Directory.GetFiles(targetDirectory, "*", SearchOption.AllDirectories).Where(itr => !itr.EndsWith(".meta")).ToList();
            
            var context = SynchronizationContext.Current;
            object lockObj = new object();
            int total = newAssetPaths.Count();
            int progress = 0;

            foreach (var path in newAssetPaths) {
                if (targetExt.All(itr => !path.Contains(itr))) {
                    continue;
                }

                var task = Task.Run(() => {
                    var yaml = LoadYaml(path);
                    foreach (var doc in yaml.Documents) {
                        var root = doc.RootNode;
                        ChangeGuidToNewFileRecursively(string.Empty, root, guidMap);
                    }
                    StreamWriter sw = new StreamWriter(path);
                    yaml.Save(sw, false);
                    sw.Close();
                    File.WriteAllText(path, ArrangeYaml(path.Replace(targetDirectory, originDirectory), path));
                    context.Post(_ => {
                        lock (lockObj) {
                            progress++;
                            progressCallback?.Invoke((progress, total));
                        }
                    }, null);
                });
                taskList.Add(task);
            }

            await Task.WhenAll(taskList);
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
                if (string.IsNullOrEmpty(newGuid)) {
                    continue;
                }
                
                ret[originGuid] = newGuid;
            }

            return ret;
        }

        private static string GetRelativePath(string path) {
            string dataPathRoot = Directory.GetParent(Application.dataPath).FullName;
            string ret = path.Replace(dataPathRoot, "");
            if (ret.StartsWith(Path.DirectorySeparatorChar.ToString())) {
                ret = ret.Remove(0, 1);
            }
            return ret;
        }

        private static YamlStream LoadYaml(string path) {
            var input = new StringReader(File.ReadAllText(path));
            var yaml = new YamlStream();
            yaml.Load(input);
            return yaml;
        }

        private static void ChangeGuidToNewFileRecursively(string nodeKey, YamlNode node, Dictionary<string, string> guidMap) {
            switch (node.NodeType) {
                case YamlNodeType.Mapping: {
                    var mappingNode = node as YamlMappingNode;
                    foreach (var entry in mappingNode.Children) {
                        ChangeGuidToNewFileRecursively(((YamlScalarNode)entry.Key).Value, entry.Value, guidMap);
                    }
                    break;
                }
                case YamlNodeType.Sequence: {
                    var seqNode = node as YamlSequenceNode;
                    foreach (var yamlNode in seqNode.Children) {
                        ChangeGuidToNewFileRecursively(string.Empty, yamlNode, guidMap);
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
            StringBuilder sb = new StringBuilder();
            string header = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
";
            sb.Append(header);

            StringReader originString = new StringReader(File.ReadAllText(originPath));
            StringReader newString = new StringReader(File.ReadAllText(newPath));

            Queue<string> originDivList = new Queue<string>();
            while (originString.Peek() > -1) {
                string line = originString.ReadLine();
                if (line.StartsWith("---")) {
                    originDivList.Enqueue(line);
                }
            }
            
            sb.AppendLine(originDivList.Dequeue());
            
            while (newString.Peek() > -1) {
                string line = newString.ReadLine();
                if (line.StartsWith("...")) continue;
                if (line.StartsWith("---")) {
                    sb.AppendLine(originDivList.Dequeue());
                } else {
                    sb.AppendLine(line);
                }
            }

            return sb.ToString();
        }
    }
}