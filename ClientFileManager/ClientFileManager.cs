using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using ChatBubble;

namespace ChatBubble
{
    public class ClientFileManager : IFileManager
    {
        public Encoding Encoding { get; set; }
        public ISpecialSymbolConverter Converter { get; set; }

        static Encoding us_US = Encoding.GetEncoding(20127);

        public static string defaultDirectoryRoot { get; private set; }

        public static string defaultLogDirectory { get; private set; }

        object streamLock = new object();

        public static string defaultLocalCookiesDirectory { get; private set; }
        public static string defaultLocalUserDataDirectory { get; private set; }
        public static string defaultLocalUserDialoguesDirectory { get; private set; }

        public void ClearFile(string filePath)
        {
            lock (streamLock)
            {
                FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate);

                fileStream.SetLength(0);
                fileStream.Close();
            }
        }

        public bool FileExists(string directory)
        {
            if(File.Exists(directory))
            {
                return true;
            }
            return false;
        }

        public string[] GetDirectoryFiles(string directory, bool includeDirectoryPrefix, bool keepFileExtensions)
        {
            if (Directory.Exists(directory) == false)
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

            if (!keepFileExtensions)
            {
                for (int i = 0; i < output.Length; i++)
                {
                    output[i] = output[i].Remove(output[i].Length - 4);
                }
            }

            return (output);
        }

        public IWritableFileContent ReadFromFile(string filePath)
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
                fileStream.Close();

                if (filePath.Substring(filePath.LastIndexOf('.')) == FileExtensions.FileExtensionDictionary[FileExtensions.FileType.ClientMessages])
                {
                    MessagesFileContent fileContent = new MessagesFileContent(output, Converter);
                    return fileContent;
                }
                else
                {
                    return new GenericFileContents(output, Converter);
                }
            }
        }

        public void RemoveFile(string filePath)
        {
            File.Delete(filePath);
        }

        public void WriteToFile(string filePath, IWritableFileContent content, bool writeToEnd)
        {
            lock (streamLock)
            {
                if (filePath.Contains(@"\") && !Directory.Exists(filePath.Substring(0, filePath.LastIndexOf(@"\"))))
                {
                    Directory.CreateDirectory(filePath.Substring(0, filePath.LastIndexOf(@"\")));
                }

                FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate);

                byte[] byteStream = new byte[fileStream.Length];

                if (writeToEnd)
                {
                    fileStream.Seek(fileStream.Length, 0);
                }
                else
                {
                    fileStream.Seek(0, 0);
                }

                byteStream = us_US.GetBytes(content.WritableContents);

                fileStream.Write(byteStream, 0, content.WritableContents.Length);
                fileStream.Close();
            }
        }

        public static void SetLocalUserDirectory(string userID)
        {
            defaultLocalUserDataDirectory = defaultDirectoryRoot + @"\UserData" + @"\user" + userID;

            defaultLocalUserDialoguesDirectory = defaultLocalUserDataDirectory + @"\Dialogues\";
        }

        public static void SetClientRootDirectory(string mainDirectory)
        {
            defaultDirectoryRoot = mainDirectory;

            defaultLocalCookiesDirectory = defaultDirectoryRoot + @"\Cookies\";
        }
    }

    public class MessagesFileContent : EntrySeparatedFileContents
    {
        public readonly static Dictionary<MessageFileEntry.MessageEntryType, string[]> MessageEntrySeparators = new Dictionary<MessageFileEntry.MessageEntryType, string[]>()
        {
            [MessageFileEntry.MessageEntryType.Message] = new string[2] { "<message>", "<message/>" },
            [MessageFileEntry.MessageEntryType.Status] = new string[2] { "<status>", "<status/>" },
            [MessageFileEntry.MessageEntryType.Time] = new string[2] { "<time>", "<time/>" },
            [MessageFileEntry.MessageEntryType.Content] = new string[2] { "<content>", "<content/>" },
            [MessageFileEntry.MessageEntryType.ID] = new string[2] { "<msgid>", "<msgid/>" },
        };

        public readonly static Dictionary<MessageFileEntry.MessageEntryType, string> MessageEntryKeys = new Dictionary<MessageFileEntry.MessageEntryType, string>()
        {
            [MessageFileEntry.MessageEntryType.Message] = "message_id",
            [MessageFileEntry.MessageEntryType.Status] = "status",
            [MessageFileEntry.MessageEntryType.Time] = "time",
            [MessageFileEntry.MessageEntryType.Content] = "content",
            [MessageFileEntry.MessageEntryType.ID] = "id",
        };

        static readonly string[] messageSplitSeparators = MessageEntrySeparators[MessageFileEntry.MessageEntryType.Message];

        public MessagesFileContent(string fileContent, ISpecialSymbolConverter converter) : base(converter)
        {
            EntryDictionary = ParseFileContents(fileContent);
            WritableContents = fileContent;
        }

        public MessagesFileContent(Dictionary<string, FileEntry> fileEntries, ISpecialSymbolConverter converter) : base(fileEntries, converter)
        {

        }

        protected override Dictionary<string, FileEntry> ParseFileContents(string fileContent)
        {
            Dictionary<string, FileEntry> fileEntries = new Dictionary<string, FileEntry>();
            string[] messages = fileContent.Split(messageSplitSeparators, StringSplitOptions.RemoveEmptyEntries);
            
            foreach(string message in messages)
            {
                string status =
                    message.Substring(message.IndexOf(MessageEntrySeparators[MessageFileEntry.MessageEntryType.Status][0] +
                    MessageEntrySeparators[MessageFileEntry.MessageEntryType.Status][0].Length),
                    message.IndexOf(MessageEntrySeparators[MessageFileEntry.MessageEntryType.Status][1]));

                string time =
                    message.Substring(message.IndexOf(MessageEntrySeparators[MessageFileEntry.MessageEntryType.Time][0] +
                    MessageEntrySeparators[MessageFileEntry.MessageEntryType.Time][0].Length),
                    message.IndexOf(MessageEntrySeparators[MessageFileEntry.MessageEntryType.Time][1]));

                string content =
                    message.Substring(message.IndexOf(MessageEntrySeparators[MessageFileEntry.MessageEntryType.Content][0] +
                    MessageEntrySeparators[MessageFileEntry.MessageEntryType.Content][0].Length),
                    message.IndexOf(MessageEntrySeparators[MessageFileEntry.MessageEntryType.Content][1]));

                int id =
                    Convert.ToInt32(message.Substring(message.IndexOf(MessageEntrySeparators[MessageFileEntry.MessageEntryType.ID][0] +
                    MessageEntrySeparators[MessageFileEntry.MessageEntryType.ID][0].Length),
                    message.IndexOf(MessageEntrySeparators[MessageFileEntry.MessageEntryType.ID][1])));

                MessageFileEntry messageEntry =
                new MessageFileEntry(MessagesFileContent.MessageEntryKeys[MessageFileEntry.MessageEntryType.Message] + id.ToString(),
                id, status, time, content, Converter);
                fileEntries.Add(messageEntry.EntryName, messageEntry);
            }

            return fileEntries;
        }
    }

    public class MessageFileEntry : FileEntry
    {       
        public enum MessageEntryType { Message, Time, Status, Content, ID}

        public MessageFileEntry(string entryName, object entryContent, MessageEntryType entryType, ISpecialSymbolConverter converter)
            : base(entryName, entryContent, converter)
        {
            EntrySeparatorBeginning = MessagesFileContent.MessageEntrySeparators[entryType][0];
            EntrySeparatorEnding = MessagesFileContent.MessageEntrySeparators[entryType][1];
        }

        public MessageFileEntry(string entryName, int messageID, string messageStatus, string messageTime, string messageContent, ISpecialSymbolConverter converter)
            : base(entryName, converter)
        {
            EntrySeparatorBeginning = MessagesFileContent.MessageEntrySeparators[MessageEntryType.Message][0];
            EntrySeparatorBeginning = MessagesFileContent.MessageEntrySeparators[MessageEntryType.Message][1];

            Dictionary<string, MessageFileEntry> innerMessageEntries = new Dictionary<string, MessageFileEntry>()
            {
                [MessagesFileContent.MessageEntryKeys[MessageFileEntry.MessageEntryType.ID]]
               = new MessageFileEntry(MessagesFileContent.MessageEntryKeys[MessageFileEntry.MessageEntryType.ID],
               messageID, MessageFileEntry.MessageEntryType.ID, converter),

                [MessagesFileContent.MessageEntryKeys[MessageFileEntry.MessageEntryType.Status]]
               = new MessageFileEntry(MessagesFileContent.MessageEntryKeys[MessageFileEntry.MessageEntryType.Status],
               messageStatus, MessageFileEntry.MessageEntryType.Status, converter),

                [MessagesFileContent.MessageEntryKeys[MessageFileEntry.MessageEntryType.Time]]
               = new MessageFileEntry(MessagesFileContent.MessageEntryKeys[MessageFileEntry.MessageEntryType.Time],
               messageTime, MessageFileEntry.MessageEntryType.Time, converter),

                [MessagesFileContent.MessageEntryKeys[MessageFileEntry.MessageEntryType.Content]]
               = new MessageFileEntry(MessagesFileContent.MessageEntryKeys[MessageFileEntry.MessageEntryType.Content],
               messageContent, MessageFileEntry.MessageEntryType.Content, converter),
            };

            Content = innerMessageEntries;
        }
    }
}
