using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;
using System.Reflection;
using System.Text.Json.Serialization;

namespace ProcessVectors;

#pragma warning disable SKEXP0070 // Chat completion connector is currently experimental.
#pragma warning disable SKEXP0001 // AsChatCompletionService
#pragma warning disable SKEXP0050 // Microsoft.SemanticKernel.Plugins.Web.WebSearchEnginePlugin
#pragma warning disable SKEXP0080 // Microsoft.SemanticKernel.Process.LocalRuntime
#pragma warning disable SKEXP0010 // response type


/// <summary>
/// Represents the data structure for a form capturing details of a new customer, including personal information and contact details.
/// </summary>
public class NewCustomerForm
{

    // [VectorStoreRecordData(IsFilterable = true)]

    //Key = Guid.NewGuid()
    [JsonPropertyName("Key")]
    [VectorStoreRecordKey]
    [TextSearchResultName]
    public Guid Key { get; set; }

    [JsonPropertyName("userEmail")]
    [VectorStoreRecordData]
    public string UserEmail { get; set; } = string.Empty;


    [JsonPropertyName("userFirstName")]
    [VectorStoreRecordData]
    public string UserFirstName { get; set; } = string.Empty;


    [VectorStoreRecordData(IsFullTextSearchable = true)]
    [TextSearchResultValue]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("userLastName")]
    [VectorStoreRecordData]
    public string UserLastName { get; set; } = string.Empty;

    [JsonPropertyName("userDateOfBirth")]
    public string UserDateOfBirth { get; set; } = string.Empty;

    [JsonPropertyName("userState")]
    public string UserState { get; set; } = string.Empty;

    [JsonPropertyName("userPhoneNumber")]
    public string UserPhoneNumber { get; set; } = string.Empty;

    //[JsonPropertyName("userId")]
    //public string UserId { get; set; } = string.Empty;

    //[VectorStoreRecordVector(1536)]
    //[VectorStoreRecordVector(1536)] //8192
    [VectorStoreRecordVector(Dimensions: 4, DistanceFunction.CosineDistance, IndexKind.Hnsw)]
    
    [JsonPropertyName("embedding")]
    public ReadOnlyMemory<float> Embedding { get; init; }

    public NewCustomerForm CopyWithDefaultValues(string defaultStringValue = "Unanswered")
    {
        NewCustomerForm copy = new();
        PropertyInfo[] properties = typeof(NewCustomerForm).GetProperties();

        foreach (PropertyInfo property in properties)
        {
            // Get the value of the property  
            string? value = property.GetValue(this) as string;
            if (property.GetType() == typeof(string))
            {
                // Check if the value is an empty string  
                if (string.IsNullOrEmpty(value))
                {
                    property.SetValue(copy, defaultStringValue);
                }
                else
                {
                    property.SetValue(copy, value);
                }
            }
        }

        return copy;
    }

    public bool IsFormCompleted()
    {
        return !string.IsNullOrEmpty(UserFirstName) &&
            !string.IsNullOrEmpty(UserLastName) &&
            //!string.IsNullOrEmpty(UserId) &&
            !string.IsNullOrEmpty(UserDateOfBirth) &&
            !string.IsNullOrEmpty(UserState) &&
            !string.IsNullOrEmpty(UserEmail) &&
            !string.IsNullOrEmpty(UserPhoneNumber);
    }
}


