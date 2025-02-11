using FxConsole;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel;
using ProcessVectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SKProcess.Utilities;

namespace SKProcess
{
    #pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.        
    #pragma warning disable SKEXP0070 // Chat completion connector is currently experimental.
    #pragma warning disable SKEXP0050 // Microsoft.SemanticKernel.Plugins.Web.WebSearchEnginePlugin
    #pragma warning disable SKEXP0080 // Microsoft.SemanticKernel.Process.LocalRuntime

    // Use Semantic Kernel to create a vector store and perform text searches on a data model, here; Customer POCO
    public class CreateVectorStore()
    {
        //#pragma warning disable format // Format item can be simplified
        //#pragma warning disable CA1861 // Avoid constant arrays as arguments

        /// <summary>
        /// Delegate to create a record.
        /// </summary>
        /// <typeparam name="TKey">Type of the record key.</typeparam>
        /// <typeparam name="TRecord">Type of the record.</typeparam>
        internal delegate TRecord CreateRecord<TKey, TRecord>(string text, ReadOnlyMemory<float> vector) where TKey : notnull;


        internal static async Task<IVectorStoreRecordCollection<TKey, TRecord>> CreateCollectionFromListAsync<TKey, TRecord>(
            IVectorStore vectorStore,
            string collectionName,
            string[] entries,
            ITextEmbeddingGenerationService embeddingGenerationService,
            CreateRecord<TKey, TRecord> createRecord
            )
            where TKey : notnull
        {
            // Get and create collection if it doesn't exist.
            var collection = vectorStore.GetCollection<TKey, TRecord>(collectionName);
            await collection.CreateCollectionIfNotExistsAsync().ConfigureAwait(false);

            //https://devblogs.microsoft.com/dotnet/configureawait-faq/
            //var sx = await embeddingGenerationService.GenerateEmbeddingAsync(entries[0]).ConfigureAwait(false);

            // Create records and generate embeddings for them.
            var tasks = entries.Select(entry => Task.Run(async () =>
            {
                var record = createRecord(entry, await embeddingGenerationService.GenerateEmbeddingAsync(entry).ConfigureAwait(false));
                await collection.UpsertAsync(record).ConfigureAwait(false);
            }));
            await Task.WhenAll(tasks).ConfigureAwait(false);

            return collection;
        }

        // Create and use InMemory vector store
        public async Task CreateStoreAsync(StartMeUps startMeUps)
        {
            Kernel kernel = Kernel.CreateBuilder()
                .AddOllamaTextEmbeddingGeneration(
                    endpoint: startMeUps.ModelEndpoint,
                    modelId: startMeUps.ModelName)
                .Build();

            var embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

            // Generate embeddings for each chunk.
            //var embeddings = await embeddingGenerator.GenerateEmbeddingsAsync(
            //   [
            //    "My Name is Gary Nett\nI live in Canberra! Gary Nett lives in ACT, Canberra.\n Customer since 2020.\n I am good milk driker but I do not like apples."
            //    ]);

            var embeddings =
                await embeddingGenerator.GenerateEmbeddingsAsync(
                [
                    //"I like apples.",
                    //"I like oranges."

                    "My Name is Gary Nett\nI live in Canberra! Gary Nett lives in ACT, Canberra.\n Customer since 2020.\n I am good milk driker but I do not like apples.",
                    "My Name is Mary Hamilton, I am from Sydney.\n I do not like to eat apples but do like olives",
                    "My Name is Susan Franck, I am from Sydney.\n I do not like to eat apples but do like eggs",
                    "My Name is Deliah Aaron, I am from ACT.\n I like olives"
                ]);

            // https://github.com/microsoft/semantic-kernel/discussions/8622


            Console.WriteLine($"Generated {embeddings.Count} embeddings for the provided text");

            // Construct an InMemory vector store.
            var vectorStore = new InMemoryVectorStore();
            var collectionName = "records";

            // Get and create collection if it doesn't exist.
            //var recordCollection = vectorStore.GetCollection<TKey, TRecord>(collectionName);
            var recordCollection = vectorStore.GetCollection<string, NewCustomerForm>(collectionName);
            await recordCollection.CreateCollectionIfNotExistsAsync().ConfigureAwait(false);


            // TODO populate the record collection with your test data
            // Example https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Search/VectorStore_TextSearch.cs

            // Delegate which will create a record.
            static NewCustomerForm CreateRecord(string text, ReadOnlyMemory<float> embedding)
            {
                return new()
                {
                    //Key = "", //Guid.NewGuid(),
                    Text = text,
                    //UserEmail = email,
                    Embedding = embedding
                }; 
            }

            List<NewCustomerForm> l = new List<NewCustomerForm>() {  
                
                new NewCustomerForm() { UserLastName = "Nett",  Text = "I am good milk driker but I do not like apples" },
                new NewCustomerForm() { UserLastName = "Hamilton",  Text = "I like to eat apples but do not like olive oil" },
                new NewCustomerForm() { UserLastName = "Franck",  Text = "I eat onions" },
                new NewCustomerForm() { UserLastName = "Aaron",  Text = "i like milk" }

            };
            
            // Create records and generate embeddings for them.
            //var tasks = entries.Select(entry => Task.Run(async () =>
            //{
            //    var record = createRecord(entry, await embeddingGenerationService.GenerateEmbeddingAsync(entry).ConfigureAwait(false));
            //    await collection.UpsertAsync(record).ConfigureAwait(false);
            //}));
            //await Task.WhenAll(tasks).ConfigureAwait(false);

            // Create a record collection from a list of strings using the provided delegate.
            string[] lines =
            [
                "My Name is Gary Nett.\", //\nI live in Canberra! Gary Nett lives in ACT, Canberra.\n Customer since 2020.\n I am good milk driker but I do not like apples.",
                "My Name is Mary Hamilton." //, I am from Sydney.\n I like to eat apples but do not like olive oil"
            ];

            var vectorizedSearch = await CreateCollectionFromListAsync<string, NewCustomerForm>(
                vectorStore, collectionName, lines, embeddingGenerator, CreateRecord);


            // Create a text search instance using the InMemory vector store.
            var textSearch = new VectorStoreTextSearch<NewCustomerForm>(vectorizedSearch, embeddingGenerator);
            await ExecuteSearchesAsync(textSearch);

            
            // tester ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

/*
            var embeddings2 =
                await embeddingGenerator.GenerateEmbeddingsAsync(
                [
                    "Looking for apple buyers"
                ]);

            VectorStoreRecordDefinition vectorStoreRecordDefinition = new()
            {
                Properties = new List<VectorStoreRecordProperty>
                {
                    new VectorStoreRecordKeyProperty("Key", typeof(string)),
                    new VectorStoreRecordDataProperty("Term", typeof(string)),
                    new VectorStoreRecordDataProperty("Definition", typeof(string)),
                    new VectorStoreRecordVectorProperty("DefinitionEmbedding", typeof(ReadOnlyMemory<float>)) { Dimensions = 1536 }
                }
            };

            var collection = new InMemoryVectorStoreRecordCollection<string, MyDataModel>(
                             "mydata",
                             new()
                             {
                                 VectorStoreRecordDefinition = vectorStoreRecordDefinition,
                                 KeyResolver = (record) => record.Key,
                                 VectorResolver = (vectorName, record) => record.Vectors[vectorName]
                             });

            var searchString1 = "What is an Application Programming Interface";
            var searchVector1 = await embeddingGenerator.GenerateEmbeddingAsync(searchString1);
            var searchResult1 = collection.VectorizedSearchAsync(searchVector1).Result; //.ToListAsync(); 

            var genericDataModelCollection = vectorStore.GetCollection<string, VectorStoreGenericDataModel<string>>(
            "glossary",
            vectorStoreRecordDefinition);

            // Since we have schema information available from the record definition
            // it's possible to create a collection with the right vectors, dimensions,
            // indexes and distance functions.
            await genericDataModelCollection.CreateCollectionIfNotExistsAsync();

            // When retrieving a record from the collection, data and vectors can
            // now be accessed via the Data and Vector dictionaries respectively.
            var record = await genericDataModelCollection.GetAsync("SK");

            var vectorStore2 = new InMemoryVectorStore();
            var collection2 = vectorStore2.GetCollection<string, MyDataModel>("skproducts");

            // Create the vector search options and indicate that we want to search the FeatureListEmbedding property.
            //var vectorSearchOptions = new VectorSearchOptions
            //{
            //    VectorPropertyName = nameof(MyDataModel..FeatureListEmbedding)
            //};
            var searchString = "What is an Application Programming Interface";
            var searchVector = await embeddingGenerator.GenerateEmbeddingAsync(searchString);

            // This snippet assumes searchVector is already provided, having been created using the embedding model of your choice.
            var searchResult =  collection2.VectorizedSearchAsync(searchVector).Result; //.ToListAsync(); 
            Console.WriteLine("Found record Data: " + record?.Data);
            var searchResult2 = await collection2.VectorizedSearchAsync(searchVector);
            await foreach (var result in searchResult2.Results)
            {
                Console.WriteLine("Found record Data: " + result.Record);
            }

            // var collection = new InMemoryVectorStoreRecordCollection<Guid, NewCustomerForm>("customers");
            // await collection.UpsertAsync(CreateRecord("Nett", embeddings[0]));

            var searchResult22 = await recordCollection.VectorizedSearchAsync(embeddings2, new() { Top = 4 });


            // Inspect the returned hotel.
            //await foreach (var record in searchResult.Results)
            //{
            //    Console.WriteLine("Found record Text: " + record.Record.Text);
            //    Console.WriteLine("Found record score: " + record.Score);
            //}
          

*/

            // eo tester ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++


            // Create a text search instance using the InMemory vector store.
            //var textSearch = new VectorStoreTextSearch<DataModel>(recordCollection, textEmbeddingGeneration);
            //var textSearch = new VectorStoreTextSearch<NewCustomerForm>(recordCollection, textEmbeddingGeneration);

            // Search and return results as TextSearchResult items
            var query = "List customers who like apples";
            

            KernelSearchResults<TextSearchResult> textResults = await textSearch.GetTextSearchResultsAsync(query, new() { Top = 2, Skip = 0 });
            Console.WriteLine("\n--- Text Search Results ---\n");
            await foreach (TextSearchResult result in textResults.Results)
            {
                Console.WriteLine($"Name:  {result.Name}");
                Console.WriteLine($"Value: {result.Value}");
                Console.WriteLine($"Link:  {result.Link}");
            }

        }

        public class MyDataModel
        {
            [VectorStoreRecordKey]
            public string Key { get; set; }
            
            [VectorStoreRecordData]
            public Dictionary<string, ReadOnlyMemory<float>> Vectors { get; set; }
        }

        private async Task ExecuteSearchesAsync(VectorStoreTextSearch<NewCustomerForm> textSearch)
        {

            var query = "List people who like olives";

            // Search and return results as a string items
            KernelSearchResults<string> stringResults = await textSearch.SearchAsync(query, new() { Top = 4, Skip = 0 });
            Console.WriteLine("--- String Results ---\n");
            await foreach (string result in stringResults.Results)
            {
                Console.WriteLine(result);
                Console.WriteLine($"{new string('-', 30)}");
            }

            // Search and return results as TextSearchResult items
            KernelSearchResults<TextSearchResult> textResults = await textSearch.GetTextSearchResultsAsync(query, new() { Top = 2, Skip = 0 });
            Console.WriteLine("\n--- Text Search Results ---\n");
            await foreach (TextSearchResult result in textResults.Results)
            {
                Console.WriteLine($"Name:  {result.Name}");
                Console.WriteLine($"Value: {result.Value}");
                Console.WriteLine($"Link:  {result.Link}");
                Console.WriteLine($"{new string('_', 30)}");
            }

            // Search and returns results as DataModel items
            KernelSearchResults<object> fullResults = await textSearch.GetSearchResultsAsync(query, new() { Top = 2, Skip = 0 });
            Console.WriteLine("\n--- DataModel Results ---\n");
            await foreach (NewCustomerForm result in fullResults.Results)
            {
                //Console.WriteLine($"Key:         {result.Key}");
                Console.WriteLine($"Text:        {result.UserEmail}");
                Console.WriteLine($"Embedding:   {result.Embedding.Length}");
                Console.WriteLine($"{new string('_', 30)}");
            }
        }
    }
}
