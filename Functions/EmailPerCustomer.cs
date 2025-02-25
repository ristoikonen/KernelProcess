// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace ProcessVectors;
//TODO: move to namespace SKProcess.Functions

#pragma warning disable SKEXP0070 // Chat completion connector is currently experimental.
#pragma warning disable SKEXP0001 // AsChatCompletionService
#pragma warning disable SKEXP0050 // Microsoft.SemanticKernel.Plugins.Web.WebSearchEnginePlugin
#pragma warning disable SKEXP0080 // Microsoft.SemanticKernel.Process.LocalRuntime
#pragma warning disable SKEXP0010 // response type


/// <summary>
/// Simple Customer List Plugin
/// </summary>
public class CustomersPlugin
{
    [KernelFunction]
    [Description("List customers first and last names.")]
    public List<NewCustomerForm> GetCustomers() => new()
            {
                new () { UserFirstName = "John", UserLastName = "Steward" , UserEmail = "axelf@gmx.com"},
                new () { UserFirstName = "Alice", UserLastName = "Namer" , UserEmail = "namer@gmx.com"},
                new () { UserFirstName = "Emily", UserLastName = "Hayes", UserEmail = "em@gmx.com" }
            };
}

/// <summary>
/// Writes Email Per List of Customer(s) 
/// </summary>
public static class EmailPerCustomer //(ITestOutputHelper output) : BaseTest(output)
{
    public static async Task LocalModel_ExampleAsync(Uri url, string modelId)
    {

        var kernelbuilder = Kernel.CreateBuilder()

            .AddOllamaChatCompletion(
                modelId: modelId,
                endpoint: url);

        kernelbuilder.Plugins.AddFromType<CustomersPlugin>();

        var kernel = kernelbuilder.Build();

        // Write multiple emails, one per customer. Write multiple emails, one per customer.
        var prompt = @"Rewrite the text between triple backticks into a welcoming new customer email. Write multiple emails, one per customer. Embed customer data from list like 'Congratulations UserFirstName UserLastName', as data is available. Use a professional tone, be clear and concise.
                   Sign the mail as RIAI Assistant.

                   Text: ```{{$input}}```";

        KernelFunction getCustomers = kernel.Plugins.GetFunction("CustomersPlugin", "GetCustomers");

        var mailFunction = kernel.CreateFunctionFromPrompt(prompt, new OllamaPromptExecutionSettings
        {
            TopP = 0.3f,
            Temperature = 0.2f,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Required(functions: [getCustomers]),
        });
        //, null,templateFormat:"handlebars"


        //var response = await kernel.InvokeAsync(mailFunction, new() { ["input"] = "Congratulations {{#with (CustomersPlugin-GetCustomers query)}} {{#each this}} {{customer.UserFirstName}} {{customer.UserLastName}} {{/each}} {{/with}} {{/query}} on your new personally assisted credit account. I'm going to send you furher information by the end of the week." });
        //var response = await kernel.InvokeAsync(mailFunction, new() { ["input"] = "Congratulations {{#each (getCustomers)}} {{UserFirstName}} {{UserLastName}} on your new personally assisted credit account. I'm going to send you furher information to {{UserEmail}}by the end of the week.{{/each}}" });
        
        //var response = await kernel.InvokeAsync(mailFunction, new() { ["input"] = "Congratulations {{#each (getCustomers)}} {{UserFirstName}} {{UserLastName}} {{/each}} on your new personally assisted credit account. I'm going to send you furher information to {{UserEmail}} by the end of the week." });
        //Console.WriteLine(response);

        var response = await kernel.InvokeAsync(mailFunction, new() { ["input"] = "Congratulations {{#each (getCustomers)}} {{UserFirstName}} {{UserLastName}} {{/each}} on your new personally assisted credit account. I'm going to send you furher information by the end of the week." });
        Console.WriteLine(response);

        /*
        await foreach (var emailbody in kernel.InvokeStreamingAsync(mailFunction, new() { ["input"] = "Congratulations {{#each (getCustomers}} {{UserFirstName}} {{UserLastName}} {{/each}} on your new personally assisted credit account. I'm going to send you furher information by the end of the week." }))
        {
            Console.WriteLine(emailbody);
        }
        */

    }

}
