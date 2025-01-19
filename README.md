# Ollama Qdrant Vector Search CLI

A simple command-line application for semantic search using vector embeddings. The application uses [Ollama](https://ollama.ai/) for text embeddings and generative AI, and [Qdrant](https://qdrant.tech/) as the vector database.

## Prerequisites

- Docker and Docker Compose
- .NET 8.0 SDK
- NVIDIA GPU (recommended) with appropriate CUDA drivers
- Local ollama installation (models are used from `/usr/share/ollama/.ollama/models`)

### Required Ollama Models
The following models must be installed locally to be mapped into the Docker container:
```bash
ollama pull snowflake-arctic-embed2  # Multilingual embedding model with a dimension of 1024
ollama pull mistral                  # Efficient generative AI model that fits on consumer grade hardware
```
## Getting Started

1. Start the required services using Docker Compose:

```bash
cd docker
docker-compose up
```

This will start:
- Qdrant vector database on ports 6333 and 6334 (http://localhost:6333/dashboard)
- Ollama service on port 11434

2. Run the integration tests to verify everything is working:

```bash
cd tests/VectorSearch.Tests
dotnet test
```

## Using the CLI

The CLI supports three main commands: `insert`, `search`, and `prompt`.

### Insert Vectors

Insert vectors from a text file (processes in batches of 100):
```bash
dotnet run insert --collection tech_articles --file ../../tests/TestData/tech_articles.txt
```

Insert a single text entry:
```bash
dotnet run insert --collection tech_articles --text "Embeddings are fun!"
```

### Search

Search for similar texts:
```bash
dotnet run search --text "Databases" --collection tech_articles --limit 3
```

Example output:
```
Search Results:
---------------
Score: 0.590
Text: Database backups protect against data loss and enable recovery.
Score: 0.585
Text: Database optimization improves performance.
Score: 0.579
Text: Database maintenance ensures optimal performance.
```

### Prompt with Context

Get an AI response with context from similar texts:
```bash
dotnet run prompt --collection tech_articles --text "What do you think about Databases?" --limit 3
```

The prompt command will:
1. Find similar texts in the vector database
2. Use them as context for the AI response
3. Return a response in the style of an angry pirate tech expert

```
AI Response:
------------
Arr matey, databases be a crucial treasure in this digital age! A well-maintained database, with its decks swabbed clean o' bad code an' inefficiencies, can sail through performance storms. Regular maintenance an' optimization is necessary to keep the shipshape, as it ensures the smoothest journey for yer queries and data operations.
But, let me warn ya – neglectin' backups be a foolhardy move! The loss o' valuable data would be akin to losing yer precious treasure map or rum stash. Regular backups can guarantee that ye can recover from any scurvy devils who might take yer data hostage or if the Kraken attacks yer database unexpectedly.
So, keep an eye on databases, me hearties – they're more important than ye may initially think! Ye don't wanna end up in Davy Jones' Locker with a broken database and lost data!
```