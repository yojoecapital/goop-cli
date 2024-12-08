using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;


#pragma warning disable CA1050 // Declare types in namespaces
public class FileMetadata
#pragma warning restore CA1050 // Declare types in namespaces
{
    public DateTime timestamp;
    public string fileId;
}

#pragma warning disable CA1050 // Declare types in namespaces
public partial class Metadata 
{
    public Dictionary<string, FileMetadata> mappings;
    public string folderId;
}
#pragma warning restore CA1050 // Declare types in namespaces

[JsonSerializable(typeof(FileMetadata))]
[JsonSerializable(typeof(Dictionary<string, FileMetadata>))]
[JsonSerializable(typeof(Metadata))]
public partial class JsonContext : JsonSerializerContext { }