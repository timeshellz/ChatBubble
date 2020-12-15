using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;


namespace ChatBubble
{
    public static class FileIOStreamer
    {
        static Encoding us_US = Encoding.GetEncoding(20127);

        public static string defaultDirectoryRoot { get; private set; }

        public static string defaultLogDirectory { get; private set; }

        public static string defaultRegisteredUsersDirectory { get; private set; }
        public static string defaultActiveUsersDirectory { get; private set; }
        public static string defaultPendingMessagesDirectory { get; private set; }

        public static string defaultLocalCookiesDirectory { get; private set; }
        public static string defaultLocalUserDataDirectory { get; private set; }
        public static string defaultLocalUserDialoguesDirectory { get; private set; }

        static Object streamLock = new Object();

        public static bool LoggingEnabled { get; set; }

        /// <summary>
        /// Writes a defined string to a defined file at filepath with specified conditions.
        /// </summary>
        /// <param name="filePath">Output file path.</param>
        /// <param name="input">Output string.</param>
        /// <param name="writeFromStart">If true, will write from the beginning of the file. Otherwise, will append data to the end of file.</param>
        /// <param name="beginFromString">String from which to start inserting data.</param>
        public static void WriteToFile(string filePath, string input, bool writeFromStart = true, string beginFromString = "")
        {
            lock (streamLock)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    if (filePath.Contains(@"/") && !Directory.Exists(filePath.Substring(0, filePath.LastIndexOf("/"))))
                    {
                        Directory.CreateDirectory(filePath.Substring(0, filePath.LastIndexOf("/")));
                    }
                }
                else
                {
                    if (filePath.Contains(@"\") && !Directory.Exists(filePath.Substring(0, filePath.LastIndexOf(@"\"))))
                    {
                        Directory.CreateDirectory(filePath.Substring(0, filePath.LastIndexOf(@"\")));
                    }
                }

                FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate);

                byte[] byteStream = new byte[fileStream.Length];

                input = input.Replace("\r", "");
                string inputStream = input.Replace("\n", Environment.NewLine);

                if (beginFromString != "")       //Reads all data from file and rewrites it in a new form
                {
                    fileStream.Read(byteStream, 0, byteStream.Length);

                    string fileContents = us_US.GetString(byteStream);

                    int beginInsertFromIndex = fileContents.IndexOf(beginFromString) + beginFromString.Length;  //Makes sure to remove null entry flag
                    int endInsertOnIndex = fileContents.Substring(beginInsertFromIndex).IndexOf("==");

                    if (fileContents.Substring(beginInsertFromIndex, endInsertOnIndex).Contains("null") == true)
                    {
                        fileContents = fileContents.Remove(beginInsertFromIndex, endInsertOnIndex);
                    }

                    fileContents = fileContents.Insert(beginInsertFromIndex, input);

                    try
                    {
                        inputStream = fileContents.Substring(0, fileContents.IndexOf('\0'));
                    }
                    catch
                    {
                        inputStream = fileContents;
                    }

                    writeFromStart = true;
                }

                if (writeFromStart == false)
                {
                    fileStream.Seek(fileStream.Length, 0);
                }
                else
                {
                    fileStream.Seek(0, 0);
                }

                byteStream = us_US.GetBytes(inputStream);

                fileStream.Write(byteStream, 0, inputStream.Length);

                fileStream.Close();
            }
        }

        /// <summary>
        /// Reads a string of characters from file at filepath according to specified conditions.
        /// </summary>eginFromString">If not empty, will try to read beginning from the first occurence of this string.</param>
        /// <param name="endOnString">If not empty, will try to end reading until the first occurence of this string.</param>
        /// <returns></returns>
        public static string ReadFromFile(string filePath, string beginFromString = "", string endOnString = "")
        {
            lock (streamLock)
            {
                FileStream fileStream;

                if (FileExists(filePath))
                {
                    fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                }
                else
                {
                    fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
                }

                byte[] byteStream = new byte[fileStream.Length];

                fileStream.Read(byteStream, 0, byteStream.Length);

                string output = us_US.GetString(byteStream);
                output = output.Replace("\n", "");
                output = output.Replace("\r", "");

                if (endOnString != "")
                {
                    output = output.Substring(0, output.IndexOf(endOnString));
                }
                if (beginFromString != "")
                {
                    output = output.Substring(output.IndexOf(beginFromString));
                }

                fileStream.Close();

                return output;
            }
        }

        /// <summary>
        /// Empties the file at specified filepath.
        /// </summary>
        /// <param name="filePath">File path.</param>
        public static void ClearFile(string filePath)
        {
            FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate);

            fileStream.SetLength(0);
            fileStream.Close();
        }

        public static void RemoveFile(string filePath)
        {
            File.Delete(filePath);
        }

        public static void SwapFileEntry(string filePath, string beginFromString, string oldEntry, string newEntry, bool isLongEntry = true, bool swapFullEntry = false)
        {
            lock (streamLock)
            {
                ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim();

                FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate);
                byte[] byteStream = new byte[fileStream.Length];

                fileStream.Read(byteStream, 0, byteStream.Length);

                string rawContents = us_US.GetString(byteStream);

                try
                {
                    rawContents = rawContents.Substring(0, rawContents.IndexOf('\0'));
                }
                catch { }

                int beginIndex = rawContents.IndexOf(beginFromString) + beginFromString.Length;
                int endIndex;

                if (isLongEntry == true)
                {
                    endIndex = rawContents.Substring(beginIndex).IndexOf("==" + beginFromString.Substring(0, beginFromString.Length - 2));
                }
                else
                {
                    endIndex = rawContents.Substring(beginIndex).IndexOf(Environment.NewLine);
                }

                string midContents = rawContents.Substring(beginIndex, endIndex);

                if (swapFullEntry == false)
                {
                    midContents = midContents.Replace(oldEntry, "");
                }
                else
                {
                    midContents = "";
                }

                midContents = newEntry;

                rawContents = rawContents.Remove(beginIndex, endIndex);
                rawContents = rawContents.Insert(beginIndex, midContents);

                Array.Clear(byteStream, 0, byteStream.Length);
                byteStream = us_US.GetBytes(rawContents);

                fileStream.Seek(0, 0);
                fileStream.Write(byteStream, 0, rawContents.Length);

                fileStream.SetLength(rawContents.Length);
                fileStream.Close();
            }
        }

        public static void RemoveFileEntry(string filePath, string beginFromString, string entry, bool isLongEntry = true, bool removeFullEntry = false)
        {
            lock (streamLock)
            {
                ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim();

                FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate);
                byte[] byteStream = new byte[fileStream.Length];

                fileStream.Read(byteStream, 0, byteStream.Length);

                string rawContents = us_US.GetString(byteStream);

                try
                {
                    rawContents = rawContents.Substring(0, rawContents.IndexOf('\0'));
                }
                catch { }

                int beginIndex = rawContents.IndexOf(beginFromString) + beginFromString.Length;
                int endIndex;

                if (isLongEntry == true)
                {
                    endIndex = rawContents.Substring(beginIndex).IndexOf("==" + beginFromString.Substring(0, beginFromString.Length - 2));
                }
                else
                {
                    endIndex = rawContents.Substring(beginIndex).IndexOf(Environment.NewLine);
                }

                string midContents = rawContents.Substring(beginIndex, endIndex);

                if (removeFullEntry != true)
                {
                    midContents = midContents.Replace(entry, "");
                }
                else
                {
                    midContents = "";
                }

                if (midContents == "")
                {
                    midContents = "null\n";
                }

                rawContents = rawContents.Remove(beginIndex, endIndex);
                rawContents = rawContents.Insert(beginIndex, midContents);

                Array.Clear(byteStream, 0, byteStream.Length);
                byteStream = us_US.GetBytes(rawContents);

                fileStream.Seek(0, 0);
                fileStream.Write(byteStream, 0, rawContents.Length);

                fileStream.SetLength(rawContents.Length);
                fileStream.Close();
            }
        }

        /// <summary>
        /// Gets a string array of files at specified directory.
        /// </summary>
        /// <param name="directory">Required directory.</param>
        /// <param name="includeDirectoryPrefix">If true, will include the path to the files in the output.</param>
        /// <param name="keepFileExtensions">If true, will keep the file extension for each file.</param>
        /// <returns></returns>
        public static string[] GetDirectoryFiles(string directory, bool includeDirectoryPrefix, bool keepFileExtensions)
        {
            if(Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            string[] output = Directory.GetFiles(directory);

            if (includeDirectoryPrefix == false)
            {
                for (int i = 0; i < output.Length; i++)
                {
                     output[i] = output[i].Replace(directory, "");                  
                }
            }

            if(!keepFileExtensions)
            {
                for(int i = 0; i < output.Length; i++)
                {
                    output[i] = output[i].Remove(output[i].Length - 4);
                }
            }

            return (output);
        }

        //The following method updates the local database at specified directory with temporary database stored in an array
        //If file's "writedate" is older than fileTimeOutSpan, and that file is not in the array, it gets deleted
        //Otherwise, if that file is still in the array, but has timed out, it's writedate is updated

        /// <summary>
        /// Updates the local database at specified directory by comparing it to temporary database stored in an array.<para />
        /// fileTimeOutSpan defines the time-out span for files in the database.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="entryArray"></param>
        /// <param name="fileTimeOutSpan"></param>
        public static void FileTimeOutComparator(string directory, string[] entryArray, TimeSpan fileTimeOutSpan)
        {
            string[] files = GetDirectoryFiles(directory, true, true);

            foreach(string file in files)
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
                    if(fileLifeSpan >= fileTimeOutSpan)
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

        public static bool FileExists(string directory)
        {
            if(File.Exists(directory))
            {
                return true;
            }
            return false;
        }

        public static void SetServerDirectories(string[] directoryData)
        {
            defaultDirectoryRoot = directoryData[0];
            defaultRegisteredUsersDirectory = directoryData[0] + directoryData[1];
            defaultActiveUsersDirectory = directoryData[0] + directoryData[2];
            defaultPendingMessagesDirectory = directoryData[0] + directoryData[3];
            defaultLogDirectory = directoryData[0] + directoryData[4];
        }

        public static void SetClientRootDirectory(string mainDirectory)
        {
            defaultDirectoryRoot = mainDirectory;

            defaultLocalCookiesDirectory = defaultDirectoryRoot + @"\Cookies\";
        }

        public static void SetLocalUserDirectory(string userID)
        {
            defaultLocalUserDataDirectory = defaultDirectoryRoot + @"\UserData" + @"\user" + userID;

            defaultLocalUserDialoguesDirectory = defaultLocalUserDataDirectory + @"\Dialogues\";
        }

        public static void LogWriter(string input)
        {
            if (LoggingEnabled)
            {
                string timedInput = "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToString("HH:mm:ss") + "] " + input + "\n";

                WriteToFile(defaultLogDirectory, timedInput, false);
            }
        }
    }
}
