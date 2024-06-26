/***********************************************************************************//*
*  作者: 邹杭特                                                                       *
*  时间: 2021-04-24                                                                   *
*  版本: master_5122a2                                                                *
*  功能:                                                                              *
*   - 文件工具                                                                        *
*//************************************************************************************/

using UnityEngine;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace UFrame.NodeGraph
{
    public class FileUtility
    {
        public static void RemakeDirectory(string localFolderPath)
        {
            if (Directory.Exists(localFolderPath))
            {
                FileUtility.DeleteDirectory(localFolderPath, true);
            }
            Directory.CreateDirectory(localFolderPath);
        }

        public static void CopyFile(string sourceFilePath, string targetFilePath)
        {
            var parentDirectoryPath = Path.GetDirectoryName(targetFilePath);
            Directory.CreateDirectory(parentDirectoryPath);
            File.Copy(sourceFilePath, targetFilePath, true);
        }

        public static void CopyTemplateFile(string sourceFilePath, string targetFilePath, string srcName, string dstName)
        {
            var parentDirectoryPath = Path.GetDirectoryName(targetFilePath);
            Directory.CreateDirectory(parentDirectoryPath);

            StreamReader r = File.OpenText(sourceFilePath);

            string contents = r.ReadToEnd();
            contents = contents.Replace(srcName, dstName);

            File.WriteAllText(targetFilePath, contents);
        }

        public static void DeleteFileThenDeleteFolderIfEmpty(string localTargetFilePath)
        {
            File.Delete(localTargetFilePath);
            File.Delete(localTargetFilePath + NGSettings.UNITY_METAFILE_EXTENSION);
            var directoryPath = Directory.GetParent(localTargetFilePath).FullName;
            var restFiles = GetFilePathsInFolder(directoryPath);
            if (!restFiles.Any())
            {
                FileUtility.DeleteDirectory(directoryPath, true);
                File.Delete(directoryPath + NGSettings.UNITY_METAFILE_EXTENSION);
            }
        }

        // Get all files under given path, including files in child folders
        public static List<string> GetAllFilePathsInFolder(string localFolderPath, bool includeHidden = false, bool includeMeta = !NGSettings.IGNORE_META)
        {
            var filePaths = new List<string>();

            if (string.IsNullOrEmpty(localFolderPath))
            {
                return filePaths;
            }
            if (!Directory.Exists(localFolderPath))
            {
                return filePaths;
            }

            GetFilePathsRecursively(localFolderPath, filePaths, includeHidden, includeMeta);

            return filePaths;
        }

        // Get files under given path
        public static List<string> GetFilePathsInFolder(string folderPath, bool includeHidden = false, bool includeMeta = !NGSettings.IGNORE_META)
        {
            var filePaths = Directory.GetFiles(folderPath).Select(p => p);

            if (!includeHidden)
            {
                filePaths = filePaths.Where(path => !(Path.GetFileName(path).StartsWith(NGSettings.DOTSTART_HIDDEN_FILE_HEADSTRING)));
            }
            if (!includeMeta)
            {
                filePaths = filePaths.Where(path => !FileUtility.IsMetaFile(path));
            }

            // Directory.GetFiles() returns platform dependent delimiter, so make sure replace with "/"
            if (Path.DirectorySeparatorChar != NGSettings.UNITY_FOLDER_SEPARATOR)
            {
                filePaths = filePaths.Select(filePath => filePath.Replace(Path.DirectorySeparatorChar.ToString(), NGSettings.UNITY_FOLDER_SEPARATOR.ToString()));
            }

            return filePaths.ToList();
        }

        private static void GetFilePathsRecursively(string localFolderPath, List<string> filePaths, bool includeHidden = false, bool includeMeta = !NGSettings.IGNORE_META)
        {
            var folders = Directory.GetDirectories(localFolderPath);

            foreach (var folder in folders)
            {
                GetFilePathsRecursively(folder, filePaths, includeHidden, includeMeta);
            }

            var files = GetFilePathsInFolder(localFolderPath, includeHidden, includeMeta);
            filePaths.AddRange(files);
        }

        /**
			create combination of path.

			delimiter is always '/'.
		*/
        public static string PathCombine(params string[] paths)
        {
            if (paths.Length < 2)
            {
                throw new ArgumentException("Argument must contain at least 2 strings to combine.");
            }

            var combinedPath = _PathCombine(paths[0], paths[1]);
            var restPaths = new string[paths.Length - 2];

            Array.Copy(paths, 2, restPaths, 0, restPaths.Length);
            foreach (var path in restPaths) combinedPath = _PathCombine(combinedPath, path);

            return combinedPath;
        }

        private static string _PathCombine(string head, string tail)
        {
            if (!head.EndsWith(NGSettings.UNITY_FOLDER_SEPARATOR.ToString()))
            {
                head = head + NGSettings.UNITY_FOLDER_SEPARATOR;
            }

            if (string.IsNullOrEmpty(tail))
            {
                return head;
            }

            if (tail.StartsWith(NGSettings.UNITY_FOLDER_SEPARATOR.ToString()))
            {
                tail = tail.Substring(1);
            }

            return Path.Combine(head, tail);
        }

        public static string GetPathWithProjectPath(string pathUnderProjectFolder)
        {
            var assetPath = Application.dataPath;
            var projectPath = Directory.GetParent(assetPath).ToString();
            return FileUtility.PathCombine(projectPath, pathUnderProjectFolder);
        }

        public static string GetPathWithAssetsPath(string pathUnderAssetsFolder)
        {
            var assetPath = Application.dataPath;
            return FileUtility.PathCombine(assetPath, pathUnderAssetsFolder);
        }

        public static string ProjectPathWithSlash()
        {
            var assetPath = Application.dataPath;
            return Directory.GetParent(assetPath).ToString() + NGSettings.UNITY_FOLDER_SEPARATOR;
        }

        public static bool IsMetaFile(string filePath)
        {
            if (filePath.EndsWith(NGSettings.UNITY_METAFILE_EXTENSION)) return true;
            return false;
        }

        public static bool ContainsHiddenFiles(string filePath)
        {
            var pathComponents = filePath.Split(NGSettings.UNITY_FOLDER_SEPARATOR);
            foreach (var path in pathComponents)
            {
                if (path.StartsWith(NGSettings.DOTSTART_HIDDEN_FILE_HEADSTRING)) return true;
            }
            return false;
        }

        //public static string EnsurePrefabBuilderCacheDirExists(BuildTarget t, Model.NodeData node) {
        //          var cacheDir = FileUtility.PathCombine(NGSettings.Path.PrefabBuilderCachePath, node.Id, SystemDataUtility.GetPathSafeTargetName(t));

        //	if (!Directory.Exists(cacheDir)) {
        //		Directory.CreateDirectory(cacheDir);
        //	}
        //	if (!cacheDir.EndsWith(NGSettings.UNITY_FOLDER_SEPARATOR.ToString())) {
        //		cacheDir = cacheDir + NGSettings.UNITY_FOLDER_SEPARATOR.ToString();
        //	}
        //	return cacheDir;
        //}

        //      public static string EnsureAssetGeneratorCacheDirExists(BuildTarget t, Model.NodeData node) {
        //          var cacheDir = FileUtility.PathCombine(NGSettings.Path.AssetGeneratorCachePath, node.Id, SystemDataUtility.GetPathSafeTargetName(t));

        //          if (!Directory.Exists(cacheDir)) {
        //              Directory.CreateDirectory(cacheDir);
        //          }
        //          if (!cacheDir.EndsWith(NGSettings.UNITY_FOLDER_SEPARATOR.ToString())) {
        //              cacheDir = cacheDir + NGSettings.UNITY_FOLDER_SEPARATOR.ToString();
        //          }
        //          return cacheDir;
        //      }


        //public static string EnsureAssetBundleCacheDirExists(BuildTarget t, Model.NodeData node, bool remake = false) {
        //          var cacheDir = FileUtility.PathCombine(NGSettings.Path.BundleBuilderCachePath, node.Id, BuildTargetUtility.TargetToAssetBundlePlatformName(t));

        //	if (!Directory.Exists(cacheDir)) {
        //		Directory.CreateDirectory(cacheDir);
        //	} else {
        //		if(remake) {
        //			RemakeDirectory(cacheDir);
        //		}
        //	}
        //	return cacheDir;
        //}

        //public static string GetImportSettingTemplateFilePath(string name) {
        //	if(name == NGSettings.GUI_TEXT_SETTINGTEMPLATE_MODEL) {
        //              return NGSettings.Path.SettingTemplateModel;
        //	}
        //	if(name == NGSettings.GUI_TEXT_SETTINGTEMPLATE_AUDIO) {
        //              return NGSettings.Path.SettingTemplateAudio;
        //	}
        //	if(name == NGSettings.GUI_TEXT_SETTINGTEMPLATE_TEXTURE) {
        //              return NGSettings.Path.SettingTemplateTexture;
        //	}
        //	#if UNITY_5_6 || UNITY_5_6_OR_NEWER
        //	if(name == NGSettings.GUI_TEXT_SETTINGTEMPLATE_VIDEO) {
        //              return NGSettings.Path.SettingTemplateVideo;
        //	}
        //	#endif
        //	return null;
        //}

        //public static string GetImportSettingTemplateFilePath(AssetReference a) {
        //	if(a.filterType == typeof(ModelImporter)) {
        //              return NGSettings.Path.SettingTemplateModel;
        //	}
        //	if(a.filterType == typeof(AudioImporter)) {
        //              return NGSettings.Path.SettingTemplateAudio;
        //	}
        //	if(a.filterType == typeof(TextureImporter)) {
        //              return NGSettings.Path.SettingTemplateTexture;
        //	}
        //	#if UNITY_5_6 || UNITY_5_6_OR_NEWER
        //	if(a.filterType == typeof(VideoClipImporter)) {
        //              return NGSettings.Path.SettingTemplateVideo;
        //	}
        //	#endif
        //	return null;
        //}
        public static void DeleteDirectory(string dirPath, bool isRecursive, bool forceDelete = true)
        {
            if (forceDelete)
            {
                RemoveFileAttributes(dirPath, isRecursive);
            }
            Directory.Delete(dirPath, isRecursive);
        }

        public static void RemoveFileAttributes(string dirPath, bool isRecursive)
        {
            foreach (var file in Directory.GetFiles(dirPath))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            if (isRecursive)
            {
                foreach (var dir in Directory.GetDirectories(dirPath))
                {
                    RemoveFileAttributes(dir, isRecursive);
                }
            }
        }
    }
}