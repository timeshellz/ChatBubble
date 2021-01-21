using System;


namespace ChatBubble
{
    public class ServerFileManager : GenericFileManager
    {
        public static string DefaultRootDirectory { get; private set; }
        public static string DefaultRegisteredUsersDirectory { get; private set; }
        public static string DefaultActiveUsersDirectory { get; private set; }
        public static string DefaultPendingMessagesDirectory { get; private set; }
        public static string DefaultLogDirectory { get; private set; }

        public static bool IsLoggingEnabled { get; set; }

        public void LogData(string input)
        {
            if (IsLoggingEnabled)
            {
                string timedInput = "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToString("HH:mm:ss") + "] " + input + "\n";

                WriteToFile(defaultLogDirectory, timedInput, false);
            }
        }

        public static void FileTimeOutComparator(string directory, string[] entryArray, TimeSpan fileTimeOutSpan)
        {
            string[] files = GetDirectoryFiles(directory, true, true);

            foreach (string file in files)
            {
                DateTime lastWriteDate = File.GetLastWriteTime(file);
                DateTime currentDate = DateTime.Now;

                TimeSpan fileLifeSpan = currentDate.Subtract(lastWriteDate);

                string fileName = file.Replace(directory, "");
                fileName = fileName.Replace(".txt", "");

                if (Array.Exists(entryArray, name => name.Substring(0, name.IndexOf("ip=")) == fileName) != true)
                {
                    //This is a bit of a wacky way to make sure that user sessions update roughly before after users log out, 
                    //rather than only when the file times out (because if the user logs out right before that, keep me logged in
                    //functionality won't work properly.

                    /*if (fileLifeSpan <= TimeSpan.FromSeconds(5.5))                   
                    {
                        File.SetLastWriteTime(file, currentDate - fileLifeSpan);
                    }*/
                    if (fileLifeSpan >= fileTimeOutSpan)
                    {
                        File.Delete(file);
                    }
                }
                else
                {
                    File.SetLastWriteTime(file, currentDate);
                }
            }
        }

        public static void SetServerDirectories(string[] directoryData)
        {
            DefaultRootDirectory = directoryData[0];
            DefaultRegisteredUsersDirectory = directoryData[0] + directoryData[1];
            DefaultActiveUsersDirectory = directoryData[0] + directoryData[2];
            DefaultPendingMessagesDirectory = directoryData[0] + directoryData[3];
            DefaultLogDirectory = directoryData[0] + directoryData[4];
        }
    }
}
