using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace ChatBubble
{
    public static class FileExtensions
    {
        public enum FileType { ClientMessages, ClientCookie, ServerUsers, ServerSessions, ServerMessages };
        public static Dictionary<FileType, string> FileExtensionDictionary = new Dictionary<FileType, string>()
        {
            [FileType.ClientMessages] = ".cbm",
        };
    }

    public interface IFileManager
    {
        Encoding Encoding { get; }

        void WriteToFile(string filePath, IWritableFileContent contents, bool writeToEnd);
        IWritableFileContent ReadFromFile(string filePath);
        void ClearFile(string filePath);
        void RemoveFile(string filePath);
        string[] GetDirectoryFiles(string directory, bool includeDirectoryPrefix, bool keepFileExtensions);
        bool FileExists(string directory);
    }

    public interface ISpecialSymbolConverter
    {
        Dictionary<string, string> SymbolToConvertedDictionary { get; }
        Dictionary<string, string> ConvertedToSymbolDictionary { get; }

        string Convert(string input);
        string ConvertBack(string input);
        bool IsConverted(string input);
    }

    public interface IWritableFileContent
    {
        bool IsConverted { get; }
        string WritableContents { get; }
        ISpecialSymbolConverter Converter { get; }
    }

    public class FileEntry
    {
        public bool IsConverted { get; private set; }
        public ISpecialSymbolConverter Converter { get; private set; }
        public string EntryName { get; protected set; }
        public string EntrySeparatorBeginning { get; protected set; }
        public string EntrySeparatorEnding { get; protected set; }
        public object Content { get; protected set; }

        protected FileEntry(string entryName, ISpecialSymbolConverter converter)
        {
            EntryName = entryName;
            Converter = converter;
        }
        protected FileEntry(string entryName, object entryContent, ISpecialSymbolConverter converter)
        {
            EntryName = entryName;
            Converter = converter;
            Content = entryContent;
            if (entryContent is string content)
                IsConverted = Converter.IsConverted(content);
        }

        private FileEntry(string entryName, string entrySeparatorBegin, string entrySeparatorEnd, ISpecialSymbolConverter converter)
        {
            Converter = converter;
            EntryName = entryName;
            EntrySeparatorBeginning = entrySeparatorBegin;
            EntrySeparatorEnding = entrySeparatorEnd;
        }

        public FileEntry(string entryName, string entrySeparatorBegin, string entrySeparatorEnd, object entryContent, ISpecialSymbolConverter converter)
            : this(entryName, entrySeparatorBegin, entrySeparatorEnd, converter)
        {
            IsConverted = false;
            Content = entryContent;
            if (entryContent is string content)
                IsConverted = Converter.IsConverted(content);
        }

        public string GetEntryContents(FileEntry fileEntry)
        {
            string result = String.Empty;

            if (fileEntry.Content is FileEntry innerEntry)
            {
                result = GetEntryContents(innerEntry);
            }
            else if(fileEntry.Content is Dictionary<string, FileEntry> innerEntryDictionary)
            {
                foreach (FileEntry entry in innerEntryDictionary.Values)
                    result += GetEntryContents(entry);
            }
            else
                result = GetConvertedContents();

            return EntrySeparatorBeginning + Environment.NewLine + result + Environment.NewLine + EntrySeparatorEnding + Environment.NewLine;
        }

        private string GetConvertedContents()
        {
            if (Content is string content)
            {
                if (IsConverted)
                {
                    return Converter.ConvertBack(content);
                }
                else
                {
                    return Converter.Convert(content);
                }
            }
            else return Content.ToString();
        }
    }

    public class GenericSpecialSymbolConverter : ISpecialSymbolConverter
    {
        public Dictionary<string, string> SymbolToConvertedDictionary { get; private set; }
        public Dictionary<string, string> ConvertedToSymbolDictionary { get; private set; }

        public GenericSpecialSymbolConverter()
        {
            SymbolToConvertedDictionary = new Dictionary<string, string>()
            {
                ["="] = "[eqlsgn]",
                ["<"] = "[lssthn]",
                [">"] = "[mrethn]",
                ["("] = "[lftbrt]",
                [")"] = "[rgtbrt]",
            };

            ConvertedToSymbolDictionary = new Dictionary<string, string>()
            {
                ["[eqlsgn]"] = "=",
                ["[lssthn]"] = "<",
                ["[mrethn]"] = ">",
                ["[lftbrt]"] = "(",
                ["[rgtbrt]"] = ")",
            };
        }

        public string Convert(string input)
        {
            string newString = input;

            foreach(string specialCharacter in SymbolToConvertedDictionary.Keys)
            {
                newString = newString.Replace(specialCharacter, SymbolToConvertedDictionary[specialCharacter]);
            }

            return newString;
        }

        public string ConvertBack(string input)
        {
            string newString = input;

            foreach (string convertedString in ConvertedToSymbolDictionary.Values)
            {
                newString = newString.Replace(convertedString, ConvertedToSymbolDictionary[convertedString]);
            }

            return newString;
        }

        public bool IsConverted(string input)
        {
            foreach (string specialCharacter in SymbolToConvertedDictionary.Values)
            {
                if (input.Contains(specialCharacter))
                    return false;
            }
            return true;
        }
    }

    public class GenericFileContents : IWritableFileContent
    {
        public bool IsConverted { get; protected set; }
        public string WritableContents { get; protected set; }
        public ISpecialSymbolConverter Converter { get; private set; }
        
        protected GenericFileContents(ISpecialSymbolConverter converter)
        {
            Converter = converter;
        }

        public GenericFileContents(string contentString, ISpecialSymbolConverter converter)
        {         
            Converter = converter;
            IsConverted = Converter.IsConverted(contentString);
            WritableContents = contentString;
        }       
    }

    public abstract class EntrySeparatedFileContents : GenericFileContents
    {
        public Dictionary<string, FileEntry> EntryDictionary { get; protected set; }

        protected EntrySeparatedFileContents(ISpecialSymbolConverter converter) : base(converter)
        {

        }

        protected EntrySeparatedFileContents(Dictionary<string, FileEntry> fileEntries, ISpecialSymbolConverter converter) : this(converter)
        {
            EntryDictionary = fileEntries;
            CollectWritableEntryContents(EntryDictionary);
        }

        void CollectWritableEntryContents(Dictionary<string, FileEntry> entryDictionary)
        {
            string contents = String.Empty;

            foreach(FileEntry entry in entryDictionary.Values)
            {
                contents += entry.GetEntryContents(entry);
            }

            WritableContents = contents;
            IsConverted = true;
        }

        protected abstract Dictionary<string, FileEntry> ParseFileContents(string content);
    }
}
