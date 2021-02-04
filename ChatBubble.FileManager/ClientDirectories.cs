using System;
using System.Collections.Generic;
using System.Text;

namespace ChatBubble.FileManager
{
    public static class ClientDirectories
    {
        public enum DirectoryType { Cookies, UserData }
        public enum UserDataDirectoryType { Dialogues }

        public static Dictionary<DirectoryType, string> DirectoryDictionary = new Dictionary<DirectoryType, string>()
        {
            [DirectoryType.Cookies] = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\ChatBubble\Cookies\",
            [DirectoryType.UserData] = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\ChatBubble\UserData\",
        };

        public static string GetUserDataFolder(UserDataDirectoryType directoryType, int userID)
        {
            switch(directoryType)
            {
                case UserDataDirectoryType.Dialogues:
                    return DirectoryDictionary[DirectoryType.UserData] + @"user" + userID.ToString() + @"\Dialogues\";
            }

            return DirectoryDictionary[DirectoryType.UserData] + @"user" + userID.ToString() + @"\";
        }
    }
}
