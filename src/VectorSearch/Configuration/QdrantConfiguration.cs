namespace VectorSearch.Configuration;

public record QdrantConfiguration(string Host = "localhost", int Port = 6334, ulong VectorSize = 1024);