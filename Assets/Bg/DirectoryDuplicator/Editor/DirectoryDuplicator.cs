using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bg.DirectoryDuplicator.YamlDotNet.RepresentationModel;
using UnityEditor;

namespace Bg.DirectoryDuplicator.Editor {
    public static class DirectoryDuplicator {

        public static void CopyDirectoryWithDependencies(string originDirectory, string targetDirectory) {
            if (!Directory.Exists(originDirectory)) {
                return;
            }
            
            CopyDirectory(originDirectory, targetDirectory);
            AssetDatabase.Refresh();

            var guidMap = CreateGuidMap(originDirectory, targetDirectory);
            
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
                fileInfo.CopyTo(tempPath);
            }
        }

        private static void CreateDirectoryIfNotExist(string filePath) {
            string parentDirectory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(parentDirectory)) {
                Directory.CreateDirectory(parentDirectory);
            }
        }

        private static Dictionary<string, (string originGuid, string newGuid)> CreateGuidMap(string originDirectory, string targetDirectory) {
            var ret = new Dictionary<string, (string originGuid, string newGuid)>();

            var originMetaPaths = Directory.GetFiles(originDirectory, "*.meta", SearchOption.AllDirectories);
            var newMetaPaths = Directory.GetFiles(targetDirectory, "*.meta", SearchOption.AllDirectories);

            foreach (var newMetaPath in newMetaPaths) {
                string pathName = newMetaPath.Replace(targetDirectory, "");
                string originPath = originMetaPaths.FirstOrDefault(itr => itr.Contains(pathName));
                if (string.IsNullOrEmpty(originPath)) {
                    continue;
                }
                string originGuid = GetGuidFromMetaFile(originPath);
                string newGuid = GetGuidFromMetaFile(newMetaPath);

                ret[Path.ChangeExtension(newMetaPath, "")] = (originGuid, newGuid);
            }

            return ret;
        }

        private static YamlStream LoadYaml(string path) {
            var input = new StringReader(File.ReadAllText(path));
            var yaml = new YamlStream();
            yaml.Load(input);
            return yaml;
        }

        private static string GetGuidFromMetaFile(string path) {
            var yaml = LoadYaml(path);
            var doc = yaml.Documents.First();
            var mapping = (YamlMappingNode)doc.RootNode;
            return ((YamlScalarNode)mapping["guid"]).Value;
        }
    }
}