using System;
using System.Collections.Generic;

namespace ChatBubble.FileManager
{
    public static class FileExtensions
    {
        public enum FileType{ Cookie, Dialogue, FileMetadata }

        public static Dictionary<string, FileType> FileExtensionsDictionary = new Dictionary<string, FileType>()
        {
            [".cbc"] = FileType.Cookie,
            [".dlg"] = FileType.Dialogue,
            [".mtd"] = FileType.FileMetadata,
        };

        public static Dictionary<FileType, bool> MetadataRequirementDictionary = new Dictionary<FileType, bool>()
        {
            [FileType.Cookie] = false,
            [FileType.Dialogue] = true,
            [FileType.FileMetadata] = false,
        };

        public static string GetExtensionForFileType(FileType fileType)
        {
            foreach(KeyValuePair<string, FileType> pair in FileExtensionsDictionary)
            {
                if (pair.Value == fileType)
                    return pair.Key;
            }

            return "";
        }
    }
}
