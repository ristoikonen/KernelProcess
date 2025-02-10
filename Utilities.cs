using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.TextToImage;
using Microsoft.SemanticKernel;
using ProcessVectors;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static FxConsole.FxConsole;
using Microsoft.SemanticKernel.Data;
using PuppeteerSharp.Input;
using FxConsole;
using System.Net;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.Extensions.VectorData;
using static SKProcess.Utilities;

namespace SKProcess
{
    public static class Utilities
    {
        const string jsonDirectory = @"C:\tmp\";
        public static List<string> Hashses { get; set; }

        //TODO: finish this
        public static async Task<bool> ExsistAsync(string fileNameB4hashing, string jsonDirectory)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                string hash = GetHash(sha256Hash, fileNameB4hashing);
                Console.WriteLine($"The SHA256 hash of {fileNameB4hashing} is: {hash}.");

                if (new DirectoryInfo(jsonDirectory).GetFiles().Select(f => f.Name).ToArray().Contains(hash))
                {
                    return true;
                }
            }
            return false;
        }

        //TODO: finish this
        public static async Task StoreJSONAsync(string jsonString, string emailAddress)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                string hash = GetHash(sha256Hash, emailAddress);
                Console.WriteLine($"The SHA256 hash of {emailAddress} is: {hash}.");

                string fileName = hash ?? "Account.json";
                await using FileStream createStream = File.Create(fileName);
                await JsonSerializer.SerializeAsync(createStream, jsonString);

                Console.WriteLine(File.ReadAllText(fileName));
            }
        }

        public static void AddTohashCollection(string addMe)
        {
            Hashses.Add(addMe);
        }


        public static bool Matches(string hashMadeOf2parts, string part1, string part2)
        {
            string source = part1 + part2;
            using (SHA256 sha256Hash = SHA256.Create())
            {
                string hash = GetHash(sha256Hash, source);
                Console.WriteLine($"The SHA256 hash of {source} is: {hash}.");
                Console.WriteLine("Verifying the hash...");
                if (VerifyHash(sha256Hash, source, hashMadeOf2parts))
                {
                    Console.WriteLine("The hashes are the same.");
                }
                else
                {
                    Console.WriteLine("The hashes are not same.");
                }
                return hashMadeOf2parts == hash;
            }
        }


        public static string GenerateHash(string part1, string part2)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                string hash = GetHash(sha256Hash, part1 + part2);
                Console.WriteLine($"The SHA256 hash of {part1 + part2} is: {hash}.");
                Console.WriteLine("Verifying the hash...");
                if (VerifyHash(sha256Hash, part1 + part2, hash))
                {
                    Console.WriteLine("The hashes are the same.");
                }
                else
                {
                    Console.WriteLine("The hashes are not same.");
                }
                return hash;
            }
        }

        // hash returned in hex format 
        private static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        // does input's hash match hash?
        private static bool VerifyHash(HashAlgorithm hashAlgorithm, string input, string hash)
        {
            var hashOfInput = GetHash(hashAlgorithm, input);
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(hashOfInput, hash) == 0;
        }



        // to exec batch files, remove this from Release -version!
        public static bool RunBat(string workingDirectory, string fileName)
        {
            using System.Diagnostics.Process p = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    WorkingDirectory = workingDirectory,
                    FileName = fileName,
                    UseShellExecute = true
                }
            };
            // true if Process starts
            return p.Start();
        }

        /// <summary>
        /// Delegate to create a record.
        /// </summary>
        /// <typeparam name="TKey">Type of the record key.</typeparam>
        /// <typeparam name="TRecord">Type of the record.</typeparam>
        //internal delegate TRecord CreateRecord<TKey, TRecord>(string text, ReadOnlyMemory<float> vector) where TKey : notnull;

        //internal static async Task<IVectorStoreRecordCollection<TKey, TRecord>> CreateCollectionFromListAsync<TKey, TRecord>(
        //IVectorStore vectorStore,
        //string collectionName,
        //string[] entries,
        //ITextEmbeddingGenerationService embeddingGenerationService,
        //CreateRecord<TKey, TRecord> createRecord)
        //where TKey : notnull
        //{
        //    // Get and create collection if it doesn't exist.
        //    var collection = vectorStore.GetCollection<TKey, TRecord>(collectionName);
        //    await collection.CreateCollectionIfNotExistsAsync().ConfigureAwait(false);

        //    // Create records and generate embeddings for them.
        //    var tasks = entries.Select(entry => Task.Run(async () =>
        //    {
        //        var record = createRecord(entry, await embeddingGenerationService.GenerateEmbeddingAsync(entry).ConfigureAwait(false));
        //        await collection.UpsertAsync(record).ConfigureAwait(false);
        //    }));
        //    await Task.WhenAll(tasks).ConfigureAwait(false);

        //    return collection;
        //}

    }



    //// The following example shows how to use Semantic Kernel to create a vector store and perform text searches.
    //public class CreateVectorStore()
    //{


    //    //#pragma warning disable format // Format item can be simplified
    //   // #pragma warning disable CA1861 // Avoid constant arrays as arguments


    //    internal static async Task<IVectorStoreRecordCollection<TKey, TRecord>> CreateCollectionFromListAsync<TKey, TRecord>(
    //    IVectorStore vectorStore,
    //    string collectionName,
    //    string[] entries,
    //    ITextEmbeddingGenerationService embeddingGenerationService,
    //    CreateRecord<TKey, TRecord> createRecord)
    //    where TKey : notnull
    //    {
    //        // Get and create collection if it doesn't exist.
    //        var collection = vectorStore.GetCollection<TKey, TRecord>(collectionName);
    //        await collection.CreateCollectionIfNotExistsAsync().ConfigureAwait(false);

    //        // Create records and generate embeddings for them.
    //        var tasks = entries.Select(entry => Task.Run(async () =>
    //        {
    //            var record = createRecord(entry, await embeddingGenerationService.GenerateEmbeddingAsync(entry).ConfigureAwait(false));
    //            await collection.UpsertAsync(record).ConfigureAwait(false);
    //        }));
    //        await Task.WhenAll(tasks).ConfigureAwait(false);

    //        return collection;
    //    }



    //    public async Task CreateStoreAsync(StartMeUps startMeUps)
    //    {

    //        //ITextToImageService dallE = kernel.GetRequiredService<ITextToImageService>();

    //        //// Create an embedding generation service.
    //        //var textEmbeddingGeneration = new OpenAITextEmbeddingGenerationService(
    //        //        modelId: TestConfiguration.OpenAI.EmbeddingModelId,
    //        //        apiKey: TestConfiguration.OpenAI.ApiKey);

    //        //// Construct an InMemory vector store.
    //        //var vectorStore = new InMemoryVectorStore();
    //        //var collectionName = "records";


    //        Kernel kernel = Kernel.CreateBuilder()
    //            .AddOllamaTextEmbeddingGeneration(
    //                endpoint: startMeUps.ModelEndpoint,
    //                modelId: startMeUps.ModelName)
    //            .Build();

    //        var embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

    //        // Generate embeddings for each chunk.
    //        var embeddings = await embeddingGenerator.GenerateEmbeddingsAsync(["My Name is Gary Nett\nI live in Canberra!"]);

    //        Console.WriteLine($"Generated {embeddings.Count} embeddings for the provided text");

    //        // Construct an InMemory vector store.
    //        var vectorStore = new InMemoryVectorStore();
    //        var collectionName = "records";

    //        // Get and create collection if it doesn't exist.
    //        //var recordCollection = vectorStore.GetCollection<TKey, TRecord>(collectionName);
    //        var recordCollection = vectorStore.GetCollection<Guid, NewCustomerForm>(collectionName);
    //        await recordCollection.CreateCollectionIfNotExistsAsync().ConfigureAwait(false);


    //        // TODO populate the record collection with your test data
    //        // Example https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Search/VectorStore_TextSearch.cs
    //        // Delegate which will create a record.
    //        static NewCustomerForm CreateRecord(string text, ReadOnlyMemory<float> embedding)
    //        {

    //            return new()
    //            {
    //                Key = Guid.NewGuid(),
    //                //Text = text,
    //                UserLastName = "Nett",
    //                UserEmail = "asko@gmx.com",
    //                Embedding = embedding
    //            };
    //        }

    //        // Create records and generate embeddings for them.
    //        //var tasks = entries.Select(entry => Task.Run(async () =>
    //        //{
    //        //    var record = createRecord(entry, await embeddingGenerationService.GenerateEmbeddingAsync(entry).ConfigureAwait(false));
    //        //    await collection.UpsertAsync(record).ConfigureAwait(false);
    //        //}));
    //        //await Task.WhenAll(tasks).ConfigureAwait(false);

    //        // Create a record collection from a list of strings using the provided delegate.
    //        string[] lines =
    //        [
    //            "Semantic Kernel is a lightweight, open-source development kit that lets you easily build AI agents and integrate the latest AI models into your C#, Python, or Java codebase. It serves as an efficient middleware that enables rapid delivery of enterprise-grade solutions.",
    //            "Semantic Kernel is a new AI SDK, and a simple and yet powerful programming model that lets you add large language capabilities to your app in just a matter of minutes. It uses natural language prompting to create and execute semantic kernel AI tasks across multiple languages and platforms.",
    //            "In this guide, you learned how to quickly get started with Semantic Kernel by building a simple AI agent that can interact with an AI service and run your code. To see more examples and learn how to build more complex AI agents, check out our in-depth samples."
    //        ];

    //        var vectorizedSearch = await CreateCollectionFromListAsync<Guid, NewCustomerForm>(
    //            vectorStore, collectionName, lines, embeddingGenerator, CreateRecord);


    //        //var vectorizedSearch = await CreateCollectionFromListAsync<Guid, NewCustomerForm>(
    //        //    vectorStore, collectionName, lines, textEmbeddingGeneration, CreateRecord);


    //        // Create a text search instance using the InMemory vector store.
    //        var textSearch = new VectorStoreTextSearch<NewCustomerForm>(vectorizedSearch, embeddingGenerator);
    //        await ExecuteSearchesAsync(textSearch);


    //        // Create a text search instance using the InMemory vector store.
    //        //var textSearch = new VectorStoreTextSearch<DataModel>(recordCollection, textEmbeddingGeneration);
    //        //var textSearch = new VectorStoreTextSearch<NewCustomerForm>(recordCollection, textEmbeddingGeneration);

    //        // Search and return results as TextSearchResult items
    //        var query = "What is the Semantic Kernel?";
    //        KernelSearchResults<TextSearchResult> textResults = await textSearch.GetTextSearchResultsAsync(query, new() { Top = 2, Skip = 0 });
    //        Console.WriteLine("\n--- Text Search Results ---\n");
    //        await foreach (TextSearchResult result in textResults.Results)
    //        {
    //            Console.WriteLine($"Name:  {result.Name}");
    //            Console.WriteLine($"Value: {result.Value}");
    //            Console.WriteLine($"Link:  {result.Link}");
    //        }

    //    }


    //    private async Task ExecuteSearchesAsync(VectorStoreTextSearch<NewCustomerForm> textSearch)
    //    {
            
    //        var query = "List customers and their chats";

    //        // Search and return results as a string items
    //        KernelSearchResults<string> stringResults = await textSearch.SearchAsync(query, new() { Top = 2, Skip = 0 });
    //        Console.WriteLine("--- String Results ---\n");
    //        await foreach (string result in stringResults.Results)
    //        {
    //            Console.WriteLine(result);
    //            //WriteHorizontalRule();
    //        }

    //        // Search and return results as TextSearchResult items
    //        KernelSearchResults<TextSearchResult> textResults = await textSearch.GetTextSearchResultsAsync(query, new() { Top = 2, Skip = 0 });
    //        Console.WriteLine("\n--- Text Search Results ---\n");
    //        await foreach (TextSearchResult result in textResults.Results)
    //        {
    //            Console.WriteLine($"Name:  {result.Name}");
    //            Console.WriteLine($"Value: {result.Value}");
    //            Console.WriteLine($"Link:  {result.Link}");
    //            //WriteHorizontalRule();
    //        }

    //        // Search and returns results as DataModel items
    //        KernelSearchResults<object> fullResults = await textSearch.GetSearchResultsAsync(query, new() { Top = 2, Skip = 0 });
    //        Console.WriteLine("\n--- DataModel Results ---\n");
    //        await foreach (NewCustomerForm result in fullResults.Results)
    //        {
    //            Console.WriteLine($"Key:         {result.Key}");
    //            Console.WriteLine($"Text:        {result.UserEmail}");
    //            Console.WriteLine($"Embedding:   {result.Embedding.Length}");
    //            //WriteHorizontalRule();
    //        }
    //    }

    //}


}
