using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace ChatBubble.FileManager
{
    public class FileManager
    {
        BinaryFormatter formatter = new BinaryFormatter();
        object threadLock = new object();

        void TryCreateDirectory(string filePath)
        {
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
        }

        public string[] GetDirectoryFiles(string directoryPath)
        {
            TryCreateDirectory(directoryPath);

            string[] output = Directory.GetFiles(directoryPath);

            return (output);
        }

        public void AppendToFile(string filePath, object newObject, int objectKey)
        {
            Dictionary<int, object> inputDictionary = new Dictionary<int, object>();
            inputDictionary.Add(objectKey, newObject);

            AppendToFile(filePath, inputDictionary);
        }

        public void AppendToFile(string filePath, Dictionary<int, object> input)
        {
            FileExtensionSpecifications specifications = GetFileSpecifications(filePath);

            if (!File.Exists(filePath))
                CreateFile(filePath);

            lock (threadLock)
            {
                FileStream fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write);

                Dictionary<int, object> collidingObjects = new Dictionary<int, object>();
 
                if (specifications.RequiresMetadata)
                {                   
                    Dictionary<int, long> metadataDictionary = GetFileMetadata(filePath);

                    foreach (KeyValuePair<int, object> pair in input)
                    {
                        try
                        {
                            metadataDictionary.Add(pair.Key, fileStream.Position);
                            formatter.Serialize(fileStream, pair.Value);
                        }
                        catch (ArgumentException)
                        {
                            collidingObjects.Add(pair.Key, pair.Value);
                        }
                        catch { }
                    }

                    SetFileMetadata(filePath, metadataDictionary);
                }    
                else
                {
                    foreach (KeyValuePair<int, object> pair in input)
                    {
                        try
                        {
                            formatter.Serialize(fileStream, pair.Value);
                        }
                        catch { }
                    }
                }

                fileStream.Close();

                if (collidingObjects.Count > 0)
                {
                    foreach (KeyValuePair<int, object> collidingPair in collidingObjects)
                    {
                        ReplaceObjectInFile(filePath, collidingPair.Value, collidingPair.Key);
                    }
                }
            }
        }

        public Dictionary<int, object> ReadFromFile(string filePath, int firstObjectIndex)
        {
            FileExtensionSpecifications specifications = GetFileSpecifications(filePath);
            Dictionary<int, object> outputDictionary = new Dictionary<int, object>();

            lock (threadLock)
            {
                if (!File.Exists(filePath))
                {
                    throw new FileManagerException(FileManagerException.ExceptionType.FileNotFound, "File not found.");
                }

                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                if (specifications.RequiresMetadata)
                {
                    Dictionary<int, long> metadataDictionary = GetFileMetadata(filePath);

                    foreach(KeyValuePair<int, long> pair in metadataDictionary)
                    {
                        if (pair.Key >= firstObjectIndex)
                        {
                            fileStream.Seek(pair.Value, SeekOrigin.Begin);
                            outputDictionary.Add(pair.Key, formatter.Deserialize(fileStream));
                        }
                    }
                }
                else
                {
                    while(fileStream.Position < fileStream.Length)
                    {
                        outputDictionary.Add(outputDictionary.Count, formatter.Deserialize(fileStream));
                    }
                }

                fileStream.Close();

                return outputDictionary;
            }
        }

        public Dictionary<int, object> ReadFromFile(string filePath)
        {
            return ReadFromFile(filePath, 0);
        }

        public object ReadObjectFromFile(string filePath, int objectKey)
        {
            long o;
            return ReadObjectFromFile(filePath, objectKey, out o);
        }

        object ReadObjectFromFile(string filePath, int objectKey, out long objectByteLength)
        {
            FileExtensionSpecifications specifications = GetFileSpecifications(filePath);
            object output;

            lock (threadLock)
            {
                if (!File.Exists(filePath))
                {
                    throw new FileManagerException(FileManagerException.ExceptionType.FileNotFound, "File not found.");
                }

                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                long objectStartOffset = 0;
                long objectEndOffset = 0;

                if (specifications.RequiresMetadata)
                {
                    Dictionary<int, long> metadataDictionary = GetFileMetadata(filePath);

                    fileStream.Seek(metadataDictionary[objectKey], SeekOrigin.Begin);
                    objectStartOffset = fileStream.Position;
                }

                try
                {
                    output = formatter.Deserialize(fileStream);
                    objectEndOffset = fileStream.Position;
                }
                catch
                {
                    throw new FileManagerException(FileManagerException.ExceptionType.SerializationFailure, "Unable to deserialize object.");
                }
                finally { fileStream.Close(); }

                objectByteLength = objectEndOffset - objectStartOffset;

                return output;
            }
        }

        public void RemoveObjectFromFile(string filePath, int objectKey)
        {
            FileExtensionSpecifications specifications = GetFileSpecifications(filePath);

            if (!specifications.RequiresMetadata)
                throw new FileManagerException(FileManagerException.ExceptionType.MetadataRequired, "Metadata support required to remove specific object.");

            lock(threadLock)
            {
                if (!File.Exists(filePath))
                {
                    throw new FileManagerException(FileManagerException.ExceptionType.FileNotFound, "File not found.");
                }

                long oldObjectLength;
                object oldObject = ReadObjectFromFile(filePath, objectKey, out oldObjectLength);

                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
                Dictionary<int, long> metadataDictionary = GetFileMetadata(filePath);

                fileStream.Seek(metadataDictionary[objectKey] + oldObjectLength, SeekOrigin.Begin);

                byte[] secondFileHalf = new byte[fileStream.Length - metadataDictionary[objectKey] + oldObjectLength];
                fileStream.Read(secondFileHalf, 0, secondFileHalf.Length);

                fileStream.Seek(metadataDictionary[objectKey], SeekOrigin.Begin);
                fileStream.Write(secondFileHalf, 0, secondFileHalf.Length);

                fileStream.SetLength(fileStream.Length - oldObjectLength);

                fileStream.Close();

                metadataDictionary.Remove(objectKey);

                Dictionary<int, long> metaDataCopy = new Dictionary<int, long>(metadataDictionary);

                foreach (KeyValuePair<int, long> pair in metaDataCopy)
                {
                    if (pair.Key > objectKey)
                    {
                        long oldLocation = pair.Value;
                        metadataDictionary[pair.Key] = pair.Value - oldObjectLength;
                    }
                }

                SetFileMetadata(filePath, metadataDictionary);
            }
        }

        public void ReplaceObjectInFile(string filePath, object newObject, int objectKey)
        {
            FileExtensionSpecifications specifications = GetFileSpecifications(filePath);

            if (!specifications.RequiresMetadata)
                throw new FileManagerException(FileManagerException.ExceptionType.MetadataRequired, "Metadata support required to replace specific object.");

            lock(threadLock)
            {
                if (!File.Exists(filePath))
                {
                    throw new FileManagerException(FileManagerException.ExceptionType.FileNotFound, "File not found.");
                }

                long oldObjectLength;
                object oldObject = ReadObjectFromFile(filePath, objectKey, out oldObjectLength);

                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);
                Dictionary<int, long> metadataDictionary = GetFileMetadata(filePath);

                MemoryStream memoryStream = new MemoryStream();

                try
                {                   
                    formatter.Serialize(memoryStream, newObject);
                }
                catch
                {
                    throw new FileManagerException(FileManagerException.ExceptionType.SerializationFailure, "Unable to serialize new object.");
                }

                byte[] serializedNewObjectBytes = memoryStream.ToArray();

                fileStream.Seek(metadataDictionary[objectKey] + oldObjectLength, SeekOrigin.Begin);

                long objectReplacementOffset = serializedNewObjectBytes.Length - oldObjectLength;
                byte[] secondFileHalf = new byte[fileStream.Length - metadataDictionary[objectKey] + oldObjectLength];
                fileStream.Read(secondFileHalf, 0, secondFileHalf.Length);

                fileStream.Seek(metadataDictionary[objectKey], SeekOrigin.Begin);
                fileStream.Write(serializedNewObjectBytes, 0, serializedNewObjectBytes.Length);

                fileStream.SetLength(fileStream.Length + objectReplacementOffset);

                fileStream.Write(secondFileHalf, 0, secondFileHalf.Length);

                fileStream.Close();

                Dictionary<int, long> metaDataCopy = new Dictionary<int, long>(metadataDictionary);

                foreach (KeyValuePair<int, long> pair in metaDataCopy)
                {
                    if (pair.Key > objectKey)
                    {
                        long oldLocation = pair.Value;
                        metadataDictionary[pair.Key] = pair.Value + objectReplacementOffset;
                    }
                }

                SetFileMetadata(filePath, metadataDictionary);
            }
        }

        public bool FileContainsObjectKey(string filePath, int key)
        {
            FileExtensionSpecifications specifications = GetFileSpecifications(filePath);

            if (!specifications.RequiresMetadata)
                throw new FileManagerException(FileManagerException.ExceptionType.MetadataRequired, "Metadata support required.");

            lock(threadLock)
            {
                if (GetFileMetadata(filePath).ContainsKey(key))
                    return true;
                else
                    return false;
            }
        }

        public void DeleteFile(string filePath)
        {
            FileExtensionSpecifications specifications = GetFileSpecifications(filePath);

            if (!File.Exists(filePath))
            {
                throw new FileManagerException(FileManagerException.ExceptionType.FileNotFound, "File not found.");
            }

            File.Delete(filePath);

            if (specifications.RequiresMetadata)
            {
                string metadataPath = Path.ChangeExtension(filePath, FileExtensions.GetExtensionForFileType(FileExtensions.FileType.FileMetadata));

                if (!File.Exists(metadataPath))
                {
                    throw new FileManagerException(FileManagerException.ExceptionType.MetadataNotFound, "Metadata not found.");
                }

                File.Delete(metadataPath);
            }
        }

        public void TryDeleteFile(string filePath)
        {
            try
            {
                DeleteFile(filePath);
            }
            catch { return; }
        }

        public void CreateFile(string filePath)
        {
            FileExtensionSpecifications specifications = GetFileSpecifications(filePath);

            TryCreateDirectory(filePath);

            if (File.Exists(filePath))
            {
                throw new FileManagerException(FileManagerException.ExceptionType.FileAlreadyExists, "File already exists.");
            }

            FileStream fs = File.Create(filePath);

            if (specifications.RequiresMetadata)
            {
                string metadataPath = Path.ChangeExtension(filePath, FileExtensions.GetExtensionForFileType(FileExtensions.FileType.FileMetadata));

                if (File.Exists(metadataPath))
                {
                    throw new FileManagerException(FileManagerException.ExceptionType.MetadataAlreadyExists, "Metadata file already exists.");
                }

                FileStream mfs = File.Create(metadataPath);

                mfs.Close();
            }

            fs.Close();
        }

        public void TryCreateFile(string filePath)
        {
            try
            {
                CreateFile(filePath);
            }
            catch
            { return; }
        }

        private Dictionary<int, long> GetFileMetadata(string filePath)
        {
            string metadataPath = Path.ChangeExtension(filePath, FileExtensions.GetExtensionForFileType(FileExtensions.FileType.FileMetadata));

            if (!File.Exists(metadataPath))
            {
                throw new FileManagerException(FileManagerException.ExceptionType.MetadataNotFound, "Metadata file not found.");
            }

            FileStream metadataFileStream = new FileStream(metadataPath, FileMode.Open, FileAccess.Read);
            Dictionary<int, long> metadataDictionary;

            if (metadataFileStream.Length > 0)
            {
                try
                {
                    metadataDictionary = (Dictionary<int, long>)formatter.Deserialize(metadataFileStream);
                }
                catch
                {
                    throw new FileManagerException(FileManagerException.ExceptionType.SerializationFailure, "Unable to deserialize metadata.");
                }
            }
            else
                metadataDictionary = new Dictionary<int, long>();

            metadataFileStream.Close();

            return metadataDictionary;
        }

        private void SetFileMetadata(string filePath, Dictionary<int, long> metadataDictionary)
        {
            string metadataPath = Path.ChangeExtension(filePath, FileExtensions.GetExtensionForFileType(FileExtensions.FileType.FileMetadata));

            FileStream metadataFileStream = new FileStream(metadataPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            try
            {
                formatter.Serialize(metadataFileStream, metadataDictionary);
            }
            catch
            {
                throw new FileManagerException(FileManagerException.ExceptionType.SerializationFailure, "Unable to serialize metadata.");
            }
            finally { metadataFileStream.Close(); }

            metadataFileStream.Close();
        }

        public int GetLastMetadataKey(string filePath)
        {
            FileExtensionSpecifications specifications = GetFileSpecifications(filePath);

            if (!specifications.RequiresMetadata)
                throw new FileManagerException(FileManagerException.ExceptionType.MetadataRequired, "Metadata support required.");

            lock (threadLock)
            {
                Dictionary<int, long> metadata = GetFileMetadata(filePath);
                if (metadata.Count == 0) return 0;
                return metadata.Keys.Max();
            }
        }

        private FileExtensionSpecifications GetFileSpecifications(string filePath)
        {
            //if (!Uri.IsWellFormedUriString(filePath, UriKind.Absolute))
                //throw new FileManagerException("Incorrect file path string specified.");

            try
            {
                FileExtensions.FileType fileType = FileExtensions.FileExtensionsDictionary[Path.GetExtension(filePath)];
                bool requiresMetadata = FileExtensions.MetadataRequirementDictionary[fileType];

                return new FileExtensionSpecifications(fileType, requiresMetadata);
            }
            catch
            {
                throw new FileManagerException(FileManagerException.ExceptionType.UnknownFileExtension, "Unknown file extension specified.");
            }        
        }

        public bool FileExists(string filePath)
        {
            FileExtensionSpecifications specifications = GetFileSpecifications(filePath);

            if (!File.Exists(filePath))
            {
                return false;
            }

            if (specifications.RequiresMetadata && !File.Exists(Path.ChangeExtension(filePath, FileExtensions.GetExtensionForFileType(FileExtensions.FileType.FileMetadata))))
                return false;

            return true;
        }
    }

    class FileExtensionSpecifications
    {
        public FileExtensions.FileType Type { get; private set; }
        public bool RequiresMetadata { get; private set; }

        public FileExtensionSpecifications(FileExtensions.FileType type, bool requiresMetadata)
        {
            Type = type;
            RequiresMetadata = requiresMetadata;
        }
    }

    public class FileManagerException : Exception
    {
        public enum ExceptionType 
        { UnknownFileExtension, SerializationFailure, MetadataRequired,
            MetadataAlreadyExists, FileAlreadyExists, FileNotFound, MetadataNotFound
        }

        public ExceptionType Type { get; private set; }

        public FileManagerException(ExceptionType type, string message) : base(message) 
        {
            Type = type;
        }
    }
}
