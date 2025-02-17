﻿// Licensed to the .NET Foundation under one or more agreements.
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

namespace ProcessVectors;
//TODO: move to namespace SKProcess.Steps

#pragma warning disable SKEXP0070 // Chat completion connector is currently experimental.
#pragma warning disable SKEXP0001 // AsChatCompletionService
#pragma warning disable SKEXP0050 // Microsoft.SemanticKernel.Plugins.Web.WebSearchEnginePlugin
#pragma warning disable SKEXP0080 // Microsoft.SemanticKernel.Process.LocalRuntime
#pragma warning disable SKEXP0010 // response type


/// <summary>
/// Step that is helps the user fill up a new account form.<br/>
/// Also provides a welcome message for the user.
/// </summary>
public class CompleteNewCustomerFormStep : KernelProcessStep<NewCustomerFormState>
{
    // mock
    const string PATH_JSON_FILES = @"C:\tmp\";

    public static class Functions
    {
        public const string NewAccountProcessUserInfo = nameof(NewAccountProcessUserInfo);
        public const string NewAccountWelcome = nameof(NewAccountWelcome);
    }

    internal NewCustomerFormState? _state;

    internal string _formCompletionSystemPrompt = """
    The goal is to fill up all the fields needed for a form.
    The user may provide information to fill up multiple fields of the form in one message.
    The user needs to fill up a form, all the fields of the form are necessary
    Customers and addresses are in Australia

    <CURRENT_FORM_STATE>
    {{current_form_state}}
    <CURRENT_FORM_STATE>

    GUIDANCE:
    - If there are missing details, give the user a useful message that will help fill up the remaining fields.
    - Your goal is to help guide the user to provide the missing details on the current form.
    - Encourage the user to provide the remainingdetails with examples if necessary.
    - Fields with value 'Unanswered' need to be answered by the user.
    - Format phone numbers and user ids correctly if the user does not provide the expected format.
    - If the user does not make use of parenthesis in the phone number, add them.
    - For date fields, confirm with the user first if the date format is not clear. Example 02/03 03/02 could be March 2nd or February 3rd.
    """;

    internal string _welcomeMessage = """
    Hello there, I can help you out fill out the information needed to open a new account with us.
    Please provide some personal information like first name and last name to get started.
    """;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public override ValueTask ActivateAsync(KernelProcessStepState<NewCustomerFormState> state)
    {
        _state = state.State;
        return ValueTask.CompletedTask;
    }

    [KernelFunction(Functions.NewAccountWelcome)]
    public async Task NewAccountWelcomeMessageAsync(KernelProcessStepContext context, Kernel _kernel)
    {
        _state?.conversation.Add(new ChatMessageContent { Role = AuthorRole.Assistant, Content = _welcomeMessage });
        await context.EmitEventAsync(new() { Id = AccountOpeningEvents.NewCustomerFormWelcomeMessageComplete, Data = _welcomeMessage });
    }

    private Kernel CreateNewCustomerFormKernel(Kernel _baseKernel)
    {
        // Creating another kernel that only makes use private functions to fill up the new customer form
        Kernel kernel = new(_baseKernel.Services);

        kernel.ImportPluginFromFunctions("FillForm", [
            KernelFunctionFactory.CreateFromMethod(OnUserProvidedFirstName, functionName: nameof(OnUserProvidedFirstName)),
        KernelFunctionFactory.CreateFromMethod(OnUserProvidedLastName, functionName: nameof(OnUserProvidedLastName)),
        KernelFunctionFactory.CreateFromMethod(OnUserProvidedDOBDetails, functionName: nameof(OnUserProvidedDOBDetails)),
        KernelFunctionFactory.CreateFromMethod(OnUserProvidedStateOfResidence, functionName: nameof(OnUserProvidedStateOfResidence)),
        KernelFunctionFactory.CreateFromMethod(OnUserProvidedPhoneNumber, functionName: nameof(OnUserProvidedPhoneNumber)),
        //KernelFunctionFactory.CreateFromMethod(OnUserProvidedUserId, functionName: nameof(OnUserProvidedUserId)),
        KernelFunctionFactory.CreateFromMethod(OnUserProvidedEmailAddress, functionName: nameof(OnUserProvidedEmailAddress)),
    ]);

        // kernel.ImportPluginFromFunctions("FillForm");        
        return kernel;
    }

    [KernelFunction(Functions.NewAccountProcessUserInfo)]
    public async Task CompleteNewCustomerFormAsync(KernelProcessStepContext context, string userMessage, Kernel _kernel)
    {
        // Keeping track of all user interactions
        _state?.conversation.Add(new ChatMessageContent { Role = AuthorRole.User, Content = userMessage });

        Kernel kernel = CreateNewCustomerFormKernel(_kernel);

        OllamaPromptExecutionSettings settings = new()
        {
            // RI
            FunctionChoiceBehavior = FunctionChoiceBehavior.Required(),
            Temperature = 0.5f,
            //Temperature = 0.1f,  //randomness of the output
            // ERROR BUT TEST NumPredict WITH FLOAT: NumPredict = 1.0f,
            //NumPredict = 1, // number of predictions to generate
            TopP = 0.5f,  // top probability to sample from, 0 = just use most likely words
                            //ResponseFormat = typeof(Joke)
                            //ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                            //Temperature = 0.7,

        };

        var testJSON = JsonDocument.Parse("""
    {
        "type": "object",
        "properties": {
            "Customer": {
                    "type": "object",
                    "properties": {
                        "FirstName": { "type": "string" },
                        "lastName": { "type": "string" },
                        "DOBDetails": { "type": "string" },
                        "StateOfResidence": { "type": "string" },
                        "PhoneNumber": { "type": "string" },
                        "UserId": { "type": "string" },
                        "PhoneNumber": { "type": "string" },
                        "EmailAddress": { "type": "string" }
                    }
            }
        },
        "required": ["Customer"],
        "additionalProperties": false
    }
    """).RootElement;

        // https://github.com/microsoft/semantic-kernel/issues/9919
        OpenAIPromptExecutionSettings settingsOPENAI = new()
        {
            //ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions,
            Temperature = 0.7,
            MaxTokens = 2048
            //ResponseFormat = ChatResponseFormat.Json, //"json_object"
            //ResponseFormat = "json_object"  // currently experimental
        };

        ChatHistory chatHistory = new();
        chatHistory.AddSystemMessage(_formCompletionSystemPrompt
            .Replace("{{current_form_state}}", JsonSerializer.Serialize(_state!.newCustomerForm.CopyWithDefaultValues(), _jsonOptions)));
        chatHistory.AddRange(_state.conversation);
        IChatCompletionService chatService = kernel.Services.GetRequiredService<IChatCompletionService>();
        //RI
        ChatMessageContent response = await chatService.GetChatMessageContentAsync(chatHistory, settings, kernel).ConfigureAwait(false);
        var assistantResponse = "";

        //*Console.WriteLine(response);

        if (response != null)
        {
            assistantResponse = response.Items[0].ToString();
            // Keeping track of all assistant interactions
            _state?.conversation.Add(new ChatMessageContent { Role = AuthorRole.Assistant, Content = assistantResponse });
        }

        if ((_state?.newCustomerForm != null) && (_state?.newCustomerForm.IsFormCompleted() == true))
        {
            var form = _state?.newCustomerForm;

            // Serialise json in a file
            var jsoncust = JsonSerializer.Serialize(form);
            var pathfilename = PATH_JSON_FILES + (form?.UserFirstName ?? "") + @"_" + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss") + @".json";
            File.WriteAllText(pathfilename, jsoncust);

            Console.WriteLine($"[NEW_USER_FORM_COMPLETED]: {jsoncust}");

            // All user information is gathered to proceed to the next step
            await context.EmitEventAsync(new() { Id = AccountOpeningEvents.NewCustomerFormCompleted, Data = form, Visibility = KernelProcessEventVisibility.Public });
            await context.EmitEventAsync(new() { Id = AccountOpeningEvents.CustomerInteractionTranscriptReady, Data = form, Visibility = KernelProcessEventVisibility.Public });
            return;
        }

        // emit event: NewCustomerFormNeedsMoreDetails
        await context.EmitEventAsync(new() { Id = AccountOpeningEvents.NewCustomerFormNeedsMoreDetails, Data = assistantResponse });
    }

    [Description("User provided details of first name")]
    private Task OnUserProvidedFirstName(string firstName)
    {
        if (!string.IsNullOrEmpty(firstName) && _state != null)
        {
            _state.newCustomerForm.UserFirstName = firstName;
        }

        return Task.CompletedTask;
    }

    [Description("User provided details of last name")]
    private Task OnUserProvidedLastName(string lastName)
    {
        if (!string.IsNullOrEmpty(lastName) && _state != null)
        {
            _state.newCustomerForm.UserLastName = lastName;
        }

        return Task.CompletedTask;
    }

    [Description("User provided details of Australian State the user lives in, must be in 3-letter Uppercase State Abbreviation format like VIC for Victoria")]
    private Task OnUserProvidedStateOfResidence(string stateAbbreviation)
    {
        if (!string.IsNullOrEmpty(stateAbbreviation) && _state != null)
        {
            _state.newCustomerForm.UserState = stateAbbreviation;
        }

        return Task.CompletedTask;
    }

    [Description("User provided details of date of birth, must be in the format DD/MM/YYYY")]
    private Task OnUserProvidedDOBDetails(string date)
    {
        if (!string.IsNullOrEmpty(date) && _state != null)
        {
            _state.newCustomerForm.UserDateOfBirth = date;
        }

        return Task.CompletedTask;
    }

    [Description("User provided details of phone number, must be in the format (\\d{3})-\\d{3}-\\d{4}")]
    private Task OnUserProvidedPhoneNumber(string phoneNumber)
    {
        if (!string.IsNullOrEmpty(phoneNumber) && _state != null)
        {
            _state.newCustomerForm.UserPhoneNumber = phoneNumber;
        }

        return Task.CompletedTask;
    }
    /*
    [Description("User provided details of userId, must be in the format \\d{6}")]
    private Task OnUserProvidedUserId(string userId)
    {
        if (!string.IsNullOrEmpty(userId) && _state != null)
        {
            _state.newCustomerForm.UserId = userId;
        }

        return Task.CompletedTask;
    }
    */
    [Description("User provided email address, must be in the an email valid format")]
    private Task OnUserProvidedEmailAddress(string emailAddress)
    {
        if (!string.IsNullOrEmpty(emailAddress) && _state != null)
        {
            _state.newCustomerForm.UserEmail = emailAddress;
        }

        return Task.CompletedTask;
    }
}


