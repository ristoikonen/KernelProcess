// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Reflection;
using Microsoft.SemanticKernel.Process;

namespace ProcessVectors;

#pragma warning disable SKEXP0070 // Chat completion connector is currently experimental.
#pragma warning disable SKEXP0001 // AsChatCompletionService
#pragma warning disable SKEXP0050 // Microsoft.SemanticKernel.Plugins.Web.WebSearchEnginePlugin
#pragma warning disable SKEXP0080 // Microsoft.SemanticKernel.Process.LocalRuntime


public sealed class BasicFxVectors
{
    // mock key
    string GOOGLE_API_KEY = "AIzaSyAk3oN-1Enge_LXitnHL4XFZDWSNMrCKwM8";
    public Uri ModelEndpoint { get; set; }
    public string ModelName { get; set; }

    //private IChatCompletionService? chatCompletionService { get; set; }

    public BasicFxVectors(Uri modelEndpoint, string modelName)
    {
        this.ModelEndpoint = modelEndpoint;
        this.ModelName = modelName;
    }


    public async Task CreateKernelAsync()
    {
        // PROCESS = > WE USE LONG TIMEOUT FOR THE MODEL!
        HttpClient httpClient2minTimeout = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(5),
            BaseAddress = this.ModelEndpoint
        };

        IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddOllamaChatCompletion(this.ModelName, httpClient2minTimeout);
        //kernelBuilder.Services.AddLogging(c => c.AddDebug().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace));

        Kernel kernel = kernelBuilder.Build();

        /*
        // add Google..chuck key to secrets..
        Microsoft.SemanticKernel.Plugins.Web.WebSearchEnginePlugin webSearchEnginePlugin = new(
        new GoogleConnector(new BaseClientService.Initializer()
        {
            ApiKey = GOOGLE_API_KEY,
            ApplicationName = "FxVectors"
        }, "google"));

        kernel.ImportPluginFromObject(webSearchEnginePlugin);
        */
        //Microsoft.SemanticKernel.KernelProcess process = new("ChatBot");
        
        ProcessBuilder process = new("ChatBot");

        
        var emailStep = process.AddStepFromType<IntroStep>();
        var newCustomerFormStep = process.AddStepFromType<CompleteNewCustomerFormStep>();
        //var userInputStep = process.AddStepFromType<TUserInputStep>();
        var userInputStep = process.AddStepFromType<ChatUserInputStep>();
        var displayAssistantMessageStep = process.AddStepFromType<DisplayAssistantMessageStep>();
        var customerCreditCheckStep = process.AddStepFromType<CreditScoreCheckStep>();
        var lastStep = process.AddStepFromType<LastStep>();

        var accountVerificationStep = process.AddStepFromProcess(NewAccountVerificationProcess.CreateProcess());

        // Define the process flow
        process
            .OnInputEvent(ProcessEvents.StartProcess)
            .SendEventTo(new ProcessFunctionTargetBuilder(newCustomerFormStep, "NewAccountProcessUserInfo"));

        // When the welcome message is generated, send message to displayAssistantMessageStep
        newCustomerFormStep
           .OnEvent(AccountOpeningEvents.NewCustomerFormWelcomeMessageComplete)
           .SendEventTo(new ProcessFunctionTargetBuilder(displayAssistantMessageStep, DisplayAssistantMessageStep.Functions.DisplayAssistantMessage));
        //.SendEventTo(new ProcessFunctionTargetBuilder(emailStep));

        // When the userInput step emits a user input event, send it to the newCustomerForm step
        // Function names are necessary when the step has multiple public functions like CompleteNewCustomerFormStep: NewAccountWelcome and NewAccountProcessUserInfo
        userInputStep
            .OnEvent(CommonEvents.UserInputReceived)
            .SendEventTo(new ProcessFunctionTargetBuilder(newCustomerFormStep, CompleteNewCustomerFormStep.Functions.NewAccountProcessUserInfo, "userMessage"));

        // bye bye?
        userInputStep
            .OnEvent(CommonEvents.Exit)
            .StopProcess();

        // When the newCustomerForm step emits needs more details, send message to displayAssistantMessage step
        newCustomerFormStep
            .OnEvent(AccountOpeningEvents.NewCustomerFormNeedsMoreDetails)
            .SendEventTo(new ProcessFunctionTargetBuilder(displayAssistantMessageStep, DisplayAssistantMessageStep.Functions.DisplayAssistantMessage));

        // After any assistant message is displayed, user input is expected to the next step is the userInputStep
        displayAssistantMessageStep
            .OnEvent("AssistantResponseGenerated")
            .SendEventTo(new ProcessFunctionTargetBuilder(userInputStep, ScriptedUserInputStep.Functions.GetUserInput));

//TODO: Continue from here!

        // When the newCustomerForm is completed...
        newCustomerFormStep
            .OnEvent(AccountOpeningEvents.NewCustomerFormCompleted)
            // The information gets passed to the account verificatino step
            .SendEventTo(accountVerificationStep.WhereInputEventIs(AccountOpeningEvents.NewCustomerFormCompleted));

        // The information gets passed to the validation process step
        //.SendEventTo(accountCreationStep.WhereInputEventIs(AccountOpeningEvents.NewCustomerFormCompleted));

        //.OnEvent(AccountOpeningEvents.NewCustomerFormNeedsMoreDetails)
        //.SendEventTo(new ProcessFunctionTargetBuilder(displayAssistantMessageStep, DisplayAssistantMessageStep.Functions.DisplayAssistantMessage));

        emailStep
            .OnFunctionResult()
            .SendEventTo(new ProcessFunctionTargetBuilder(lastStep));

        lastStep
            .OnFunctionResult()
            .StopProcess();

        // Build the process to get a handle that can be started
        KernelProcess kernelProcess = process.Build();

        //using var runningProcess = new KernelProcess(kernelProcess);
        using var runningProcess = await kernelProcess.StartAsync(
            kernel,
            new KernelProcessEvent()
            {
                Id = ProcessEvents.StartProcess,
                Data = "Nutrition Assistant for Customers"
            });


        //await process.StartAsync(kernel, new KernelProcessEvent { Id = "Start", Data = "Contoso GlowBrew" });
        // var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        // var settings = new OllamaPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
    }
}


/// <summary>
/// A step that elicits user input.
/// </summary>
public sealed class ChatUserInputStep : ScriptedUserInputStep
{
    public override void PopulateUserInputs(UserInputState state)
    {
        state.UserInputs.Add("Fred");
        state.UserInputs.Add("Hayes");
        state.UserInputs.Add("12 11 1988");
        state.UserInputs.Add("NSW");
        state.UserInputs.Add("(02) 12345678");
        state.UserInputs.Add("freddie@gmail.com");
        state.UserInputs.Add("This text will be ignored because exit process condition was already met at this point.");
    }

    public override async ValueTask GetUserInputAsync(KernelProcessStepContext context)
    {
        var userMessage = this.GetNextUserMessage();

        if (string.Equals(userMessage, "exit", StringComparison.OrdinalIgnoreCase))
        {
            // exit condition met, emitting exit event
            await context.EmitEventAsync(new() { Id = UserInputEvents.Exit, Data = userMessage });
            return;
        }

        // emitting userInputReceived event
        await context.EmitEventAsync(new() { Id = "UserInputReceived", Data = userMessage });
    }
}

//testing events RI
public static class UserInputEvents
{
    public static readonly string Exit = nameof(Exit);

}

// RI
public static class RyeCatcher
{
    public static event EventHandler Exithandler;
    // public void Main()  {  EventInfo? eInfo = t.GetEvent("Elapsed"); }
    //protected virtual void OnExit(EventArgs e)
    //{
    //    Exithandler?.Invoke(this, e);
    //}

}


public static class ProcessEvents
{
    public const string StartProcess = nameof(StartProcess);
}

public sealed class IntroStep : KernelProcessStep
{
    public static class Functions
    {
        public const string SendMailToUserWithDetails = nameof(SendMailToUserWithDetails);
    }

    //[KernelFunction]
    //public async ValueTask ExecuteAsync(KernelProcessStepContext context)
    //{
    //    Console.WriteLine("Step 1 - Start\n");
    //    //Kernel kernel = CreateKernelWithChatCompletion();
    //    //KernelProcess kernelProcess = SetupAccountOpeningProcess<UserInputSuccessfulInteractionStep>();
    //    //using var runningProcess = await kernelProcess.StartAsync(kernel, new KernelProcessEvent() { Id = AccountOpeningEvents.StartProcess, Data = null });
    //}

    /// <summary>
    /// Mock step that emulates Mail Service with a message for the user.
    /// </summary>
    [KernelFunction(nameof(Functions.SendMailToUserWithDetails))]
    public async Task SendMailServiceAsync(KernelProcessStepContext context, string message)
    {
        Console.WriteLine("======== MAIL SERVICE ======== ");
        Console.WriteLine(message);
        Console.WriteLine("============================== ");

        await context.EmitEventAsync(new() { Id = "MailServiceSent", Data = message });
    }

    //private async Task<Kernel> CreateKernelWithChatCompletionAsync()
    //{
    //    IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
    //    kernelBuilder.AddOllamaChatCompletion("modelName", new Uri("https://model.endpoint"));
    //    return kernelBuilder.Build();
    //}
}

public sealed class LastStep : KernelProcessStep
{
    /// <summary>
    /// Prints an introduction message to the console.
    /// </summary>
    //[KernelFunction]
    //public void PrintIntroMessage()
    //{
    //    System.Console.WriteLine("Welcome to Processes in Semantic Kernel.\n");
    //}
    [KernelFunction]
    public async ValueTask ExecuteAsync(KernelProcessStepContext context)
    {
        Console.WriteLine("The End\n");
    }
}

public sealed class ChatBotState
{
    internal ChatHistory ChatMessages { get; } = new();
}

public static class Events
{

    public const string StartProcess = "startProcess";



    public static readonly string SendMailToUserWithDetails = nameof(SendMailToUserWithDetails);

    public const string IntroComplete = "introComplete";
    public const string AssistantResponseGenerated = "assistantResponseGenerated";
    public const string Exit = "exit";
}

// A process step to gather information about a product
public class GatherProductInfoStep : KernelProcessStep
{
    [KernelFunction]
    public string GatherProductInformation(string productName)
    {
        Console.WriteLine($"{nameof(GatherProductInfoStep)}:\n\tGathering product information for product named {productName}");

        // For example purposes we just return some fictional information.
        return
            """
            Product Description:
            GlowBrew is a revolutionary AI driven coffee machine with industry leading number of LEDs and programmable light shows. The machine is also capable of brewing coffee and has a built in grinder.

            Product Features:
            1. **Luminous Brew Technology**: Customize your morning ambiance with programmable LED lights that sync with your brewing process.
            2. **AI Taste Assistant**: Learns your taste preferences over time and suggests new brew combinations to explore.
            3. **Gourmet Aroma Diffusion**: Built-in aroma diffusers enhance your coffee's scent profile, energizing your senses before the first sip.

            Troubleshooting:
            - **Issue**: LED Lights Malfunctioning
                - **Solution**: Reset the lighting settings via the app. Ensure the LED connections inside the GlowBrew are secure. Perform a factory reset if necessary.
            """;
    }
}

public class GeneratedDocumentationState
{
    public ChatHistory? ChatHistory { get; set; }
}

// A process step to generate documentation for a product
public class GenerateDocumentationStep : KernelProcessStep<GeneratedDocumentationState>
{
    private GeneratedDocumentationState _state = new();

    private string systemPrompt =
            """
            Your job is to write high quality and engaging customer facing documentation for a new product from Contoso. You will be provide with information
            about the product in the form of internal documentation, specs, and troubleshooting guides and you must use this information and
            nothing else to generate the documentation. If suggestions are provided on the documentation you create, take the suggestions into account and
            rewrite the documentation. Make sure the product sounds amazing.
            """;

    // Called by the process runtime when the step instance is activated. Use this to load state that may be persisted from previous activations.
    override public ValueTask ActivateAsync(KernelProcessStepState<GeneratedDocumentationState> state)
    {
        this._state = state.State!;
        this._state.ChatHistory ??= new ChatHistory(systemPrompt);

        return base.ActivateAsync(state);
    }

    [KernelFunction]
    public async Task GenerateDocumentationAsync(Kernel kernel, KernelProcessStepContext context, string productInfo)
    {
        Console.WriteLine($"{nameof(GenerateDocumentationStep)}:\n\tGenerating documentation for provided productInfo...");

        // Add the new product info to the chat history
        this._state.ChatHistory!.AddUserMessage($"Product Info:\n\n{productInfo}");

        // Get a response from the LLM
        IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var generatedDocumentationResponse = await chatCompletionService.GetChatMessageContentAsync(this._state.ChatHistory!);

        await context.EmitEventAsync("DocumentationGenerated", generatedDocumentationResponse.Content!.ToString());
    }


}

// A process step to publish documentation
public class PublishDocumentationStep : KernelProcessStep
{
    [KernelFunction]
    public void PublishDocumentation(string docs)
    {
        // For example purposes we just write the generated docs to the console
        Console.WriteLine($"{nameof(PublishDocumentationStep)}:\n\tPublishing product documentation:\n\n{docs}");
    }
}
/// <summary>
/// A plugin that returns favorite information for a user.
/// </summary>
public class CustomerDataPlugin
{
    // Mock data for the lights
    private readonly List<AustralianState> states = new()
    {
        new AustralianState { Id = "ACT", Name = "Australian Capital Territory" },
        new AustralianState { Id = "NSW", Name = "New South Wales" }
    };

    [KernelFunction("list_states")]
    [Description("Returns list of Australian states.")]
    public async Task<List<AustralianState>> ListAustralianStatesAsync()
    {
        return states;
    }
}

public class AustralianState
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

}


/// <summary>
/// Class that represents a controllable alarm.
/// </summary>
public class MyAlarmPlugin
{
    private string _hour;
    private DateTime _houroff;

    public MyAlarmPlugin(string providedHour)
    {
        this._hour = providedHour;
        //this._houroff = DateTime.Now;
    }

    [KernelFunction, Description("Sets an alarm at the provided time")]
    public string SetAlarm(string time)
    {
        this._hour = time;
        this._houroff = DateTime.Parse(time);
        return GetCurrentAlarm();
    }

    [KernelFunction, Description("Get current alarm set")]
    public string GetCurrentAlarm()
    {
        return $"Alarm set for {_hour}, {_houroff}";
    }

    [KernelFunction, Description("Activate alarm")]
    public string ActicateAlarm()
    {
        if (this._houroff.Minute == DateTime.Now.Minute)
        {
            return "Alarm activated at {_hour}";
        }
        return $"Alarm set for {_hour}";
    }
}
/// <summary>
/// Simple plugin that just returns the time.
/// </summary>
public class MyTimePlugin
{
    [KernelFunction, Description("Get the current time")]
    public DateTimeOffset Time() => DateTimeOffset.Now;
}




/// <summary>
/// Step used in the Processes Samples:
/// - Step_02_AccountOpening.cs
/// </summary>
public class DisplayAssistantMessageStep : KernelProcessStep
{
    public static class Functions
    {
        public const string DisplayAssistantMessage = nameof(DisplayAssistantMessage);
    }

    [KernelFunction(Functions.DisplayAssistantMessage)]
    public async ValueTask DisplayAssistantMessageAsync(KernelProcessStepContext context, string assistantMessage)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"ASSISTANT: {assistantMessage}\n");
        Console.ResetColor();

        // Emit the assistantMessageGenerated
        await context.EmitEventAsync(new() { Id = "AssistantResponseGenerated", Data = assistantMessage });
    }
}

/// <summary>
/// Step that is helps the user fill up a new account form.<br/>
/// Also provides a welcome message for the user.
/// </summary>
public class CompleteNewCustomerFormStep : KernelProcessStep<NewCustomerFormState>
{
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
            KernelFunctionFactory.CreateFromMethod(OnUserProvidedUserId, functionName: nameof(OnUserProvidedUserId)),
            KernelFunctionFactory.CreateFromMethod(OnUserProvidedEmailAddress, functionName: nameof(OnUserProvidedEmailAddress)),
        ]);

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
            Temperature = 0.1f,  //randomness of the output
            NumPredict = 1, // number of predictions to generate
            TopP = 0.2f,  // top probability to sample from, 0 = just use most likely words
            //ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            //Temperature = 0.7,
            //MaxTokens = 2048
        };

        //OpenAIPromptExecutionSettings settings = new()
        //{
        //    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        //    Temperature = 0.7,
        //    MaxTokens = 2048
        //};

        ChatHistory chatHistory = new();
        chatHistory.AddSystemMessage(_formCompletionSystemPrompt
            .Replace("{{current_form_state}}", JsonSerializer.Serialize(_state!.newCustomerForm.CopyWithDefaultValues(), _jsonOptions)));
        chatHistory.AddRange(_state.conversation);
        IChatCompletionService chatService = kernel.Services.GetRequiredService<IChatCompletionService>();
        ChatMessageContent response = await chatService.GetChatMessageContentAsync(chatHistory, null, kernel).ConfigureAwait(false);
        var assistantResponse = "";

        //*Console.WriteLine(response);

        if (response != null)
        {
            assistantResponse = response.ToString(); //.Items[0].ToString();
            // Keeping track of all assistant interactions
            _state?.conversation.Add(new ChatMessageContent { Role = AuthorRole.Assistant, Content = assistantResponse });
        }

        if (_state?.newCustomerForm != null && _state.newCustomerForm.IsFormCompleted())
        {
            Console.WriteLine($"[NEW_USER_FORM_COMPLETED]: {JsonSerializer.Serialize(_state?.newCustomerForm)}");
            // All user information is gathered to proceed to the next step
            await context.EmitEventAsync(new() { Id = AccountOpeningEvents.NewCustomerFormCompleted, Data = _state?.newCustomerForm, Visibility = KernelProcessEventVisibility.Public });
            await context.EmitEventAsync(new() { Id = AccountOpeningEvents.CustomerInteractionTranscriptReady, Data = _state?.conversation, Visibility = KernelProcessEventVisibility.Public });
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

    [Description("User provided details of userId, must be in the format \\d{6}")]
    private Task OnUserProvidedUserId(string userId)
    {
        if (!string.IsNullOrEmpty(userId) && _state != null)
        {
            _state.newCustomerForm.UserId = userId;
        }

        return Task.CompletedTask;
    }

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

/// <summary>
/// Mock step that emulates the creation of a new account that triggers other services after a new account id creation
/// </summary>
public class NewAccountStep : KernelProcessStep
{
    public static class Functions
    {
        public const string CreateNewAccount = nameof(CreateNewAccount);
    }

    [KernelFunction(Functions.CreateNewAccount)]
    public async Task CreateNewAccountAsync(KernelProcessStepContext context, bool previousCheckSucceeded, NewCustomerForm customerDetails, List<ChatMessageContent> interactionTranscript, Kernel _kernel)
    {
        // Placeholder for a call to API to create new account for user
        var accountId = new Guid();
        //AccountDetails accountDetails = new()
        //{
        //    UserDateOfBirth = customerDetails.UserDateOfBirth,
        //    UserFirstName = customerDetails.UserFirstName,
        //    UserLastName = customerDetails.UserLastName,
        //    UserId = customerDetails.UserId,
        //    UserPhoneNumber = customerDetails.UserPhoneNumber,
        //    UserState = customerDetails.UserState,
        //    UserEmail = customerDetails.UserEmail,
        //    AccountId = accountId,
        //    AccountType = AccountType.PrimeABC,
        //};

        Console.WriteLine($"[ACCOUNT CREATION] New Account {accountId} created");

        await context.EmitEventAsync(new()
        {
            Id = AccountOpeningEvents.NewMarketingRecordInfoReady,
            //Data = new MarketingNewEntryDetails
            //{
            //    AccountId = accountId,
            //    Name = $"{customerDetails.UserFirstName} {customerDetails.UserLastName}",
            //    PhoneNumber = customerDetails.UserPhoneNumber,
            //    Email = customerDetails.UserEmail,
            //}
        });

        await context.EmitEventAsync(new()
        {
            Id = AccountOpeningEvents.CRMRecordInfoReady,
            //Data = new AccountUserInteractionDetails
            //{
            //    AccountId = accountId,
            //    UserInteractionType = UserInteractionType.OpeningNewAccount,
            //    InteractionTranscript = interactionTranscript
            //}
        });

        await context.EmitEventAsync(new()
        {
            Id = AccountOpeningEvents.NewAccountDetailsReady,
            //Data = accountDetails,
        });
    }
}


/// <summary>
/// Mock step that emulates User Credit Score check, based on the date of birth the score will be enough or insufficient
/// </summary>
public class CreditScoreCheckStep : KernelProcessStep
{
    public static class Functions
    {
        public const string DetermineCreditScore = nameof(DetermineCreditScore);
    }

    private const int MinCreditScore = 600;

    [KernelFunction(Functions.DetermineCreditScore)]
    public async Task DetermineCreditScoreAsync(KernelProcessStepContext context, NewCustomerForm customerDetails, Kernel _kernel)
    {
        // Placeholder for a call to API to validate credit score with customerDetails
        var creditScore = customerDetails.UserDateOfBirth == "02/03/1990" ? 700 : 500;

        if (creditScore >= MinCreditScore)
        {
            Console.WriteLine("[CREDIT CHECK] Credit Score Check Passed");
            await context.EmitEventAsync(new() { Id = AccountOpeningEvents.CreditScoreCheckApproved, Data = true });
            return;
        }
        Console.WriteLine("[CREDIT CHECK] Credit Score Check Failed");
        await context.EmitEventAsync(new()
        {
            Id = AccountOpeningEvents.CreditScoreCheckRejected,
            Data = $"We regret to inform you that your credit score of {creditScore} is insufficient to apply for an account of the type PRIME ABC",
            Visibility = KernelProcessEventVisibility.Public,
        });
    }
}

/// <summary>
/// The state object for the <see cref="CompleteNewCustomerFormStep"/>
/// </summary>
public class NewCustomerFormState
{
    internal NewCustomerForm newCustomerForm { get; set; } = new();
    internal List<ChatMessageContent> conversation { get; set; } = [];
}

/// <summary>
/// Processes Events related to Account Opening scenarios.<br/>
/// Class used in <see cref="Step02a_AccountOpening"/>, <see cref="Step02b_AccountOpening"/> samples
/// </summary>
public static class AccountOpeningEvents
{
    public static readonly string StartProcess = nameof(StartProcess);

    public static readonly string NewCustomerFormWelcomeMessageComplete = nameof(NewCustomerFormWelcomeMessageComplete);
    public static readonly string NewCustomerFormCompleted = nameof(NewCustomerFormCompleted);
    public static readonly string NewCustomerFormNeedsMoreDetails = nameof(NewCustomerFormNeedsMoreDetails);
    public static readonly string CustomerInteractionTranscriptReady = nameof(CustomerInteractionTranscriptReady);

    public static readonly string NewAccountVerificationCheckPassed = nameof(NewAccountVerificationCheckPassed);

    public static readonly string CreditScoreCheckApproved = nameof(CreditScoreCheckApproved);
    public static readonly string CreditScoreCheckRejected = nameof(CreditScoreCheckRejected);

    public static readonly string FraudDetectionCheckPassed = nameof(FraudDetectionCheckPassed);
    public static readonly string FraudDetectionCheckFailed = nameof(FraudDetectionCheckFailed);

    public static readonly string NewAccountDetailsReady = nameof(NewAccountDetailsReady);

    public static readonly string NewMarketingRecordInfoReady = nameof(NewMarketingRecordInfoReady);
    public static readonly string NewMarketingEntryCreated = nameof(NewMarketingEntryCreated);
    public static readonly string CRMRecordInfoReady = nameof(CRMRecordInfoReady);
    public static readonly string CRMRecordInfoEntryCreated = nameof(CRMRecordInfoEntryCreated);

    public static readonly string WelcomePacketCreated = nameof(WelcomePacketCreated);

    public static readonly string MailServiceSent = nameof(MailServiceSent);
}

public class NewCustomerForm
{
    [JsonPropertyName("userFirstName")]
    public string UserFirstName { get; set; } = string.Empty;

    [JsonPropertyName("userLastName")]
    public string UserLastName { get; set; } = string.Empty;

    [JsonPropertyName("userDateOfBirth")]
    public string UserDateOfBirth { get; set; } = string.Empty;

    [JsonPropertyName("userState")]
    public string UserState { get; set; } = string.Empty;

    [JsonPropertyName("userPhoneNumber")]
    public string UserPhoneNumber { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("userEmail")]
    public string UserEmail { get; set; } = string.Empty;

    public NewCustomerForm CopyWithDefaultValues(string defaultStringValue = "Unanswered")
    {
        NewCustomerForm copy = new();
        PropertyInfo[] properties = typeof(NewCustomerForm).GetProperties();

        foreach (PropertyInfo property in properties)
        {
            // Get the value of the property  
            string? value = property.GetValue(this) as string;

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

        return copy;
    }

    public bool IsFormCompleted()
    {
        return !string.IsNullOrEmpty(UserFirstName) &&
            !string.IsNullOrEmpty(UserLastName) &&
            !string.IsNullOrEmpty(UserId) &&
            !string.IsNullOrEmpty(UserDateOfBirth) &&
            !string.IsNullOrEmpty(UserState) &&
            !string.IsNullOrEmpty(UserEmail) &&
            !string.IsNullOrEmpty(UserPhoneNumber);
    }
}


public static class CommonEvents
{
    public static readonly string UserInputReceived = nameof(UserInputReceived);
    public static readonly string UserInputComplete = nameof(UserInputComplete);
    public static readonly string AssistantResponseGenerated = nameof(AssistantResponseGenerated);
    public static readonly string Exit = nameof(Exit);
}

public static class Functions
{
    public const string NewAccountProcessUserInfo = nameof(NewAccountProcessUserInfo);
    public const string NewAccountWelcome = nameof(NewAccountWelcome);
}

public class ScriptedUserInputStep : KernelProcessStep<UserInputState>
{
    public static class Functions
    {
        public const string GetUserInput = nameof(GetUserInput);
    }

    protected bool SuppressOutput { get; init; }

    /// <summary>
    /// The state object for the user input step. This object holds the user inputs and the current input index.
    /// </summary>
    private UserInputState? _state;

    /// <summary>
    /// Method to be overridden by the user to populate with custom user messages
    /// </summary>
    /// <param name="state">The initialized state object for the step.</param>
    public virtual void PopulateUserInputs(UserInputState state)
    {
        return;
    }

    /// <summary>
    /// Activates the user input step by initializing the state object. This method is called when the process is started
    /// and before any of the KernelFunctions are invoked.
    /// </summary>
    /// <param name="state">The state object for the step.</param>
    /// <returns>A <see cref="ValueTask"/></returns>
    public override ValueTask ActivateAsync(KernelProcessStepState<UserInputState> state)
    {
        _state = state.State;

        PopulateUserInputs(_state!);

        return ValueTask.CompletedTask;
    }

    internal string GetNextUserMessage()
    {
        if (_state != null && _state.CurrentInputIndex >= 0 && _state.CurrentInputIndex < this._state.UserInputs.Count)
        {
            var userMessage = this._state!.UserInputs[_state.CurrentInputIndex];
            _state.CurrentInputIndex++;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"USER: {userMessage}");
            Console.ResetColor();

            return userMessage;
        }

        Console.WriteLine("SCRIPTED_USER_INPUT: No more scripted user messages defined, returning empty string as user message");
        return string.Empty;
    }

    /// <summary>
    /// Gets the user input.
    /// Could be overridden to customize the output events to be emitted
    /// </summary>
    /// <param name="context">An instance of <see cref="KernelProcessStepContext"/> which can be
    /// used to emit events from within a KernelFunction.</param>
    /// <returns>A <see cref="ValueTask"/></returns>
    [KernelFunction(Functions.GetUserInput)]
    public virtual async ValueTask GetUserInputAsync(KernelProcessStepContext context)
    {
        var userMessage = this.GetNextUserMessage();
        // Emit the user input
        if (string.IsNullOrEmpty(userMessage))
        {
            await context.EmitEventAsync(new() { Id = "Exit" });
            return;
        }

        await context.EmitEventAsync(new() { Id = "UserInputReceived", Data = userMessage });
    }
}

/// <summary>
/// The state object for the <see cref="ScriptedUserInputStep"/>
/// </summary>
public record UserInputState
{
    public List<string> UserInputs { get; init; } = [];
    public int CurrentInputIndex { get; set; } = 0;
 }




/// <summary>
/// Demonstrate creation of <see cref="KernelProcess"/> and
/// eliciting its response to five explicit user messages.<br/>
/// For each test there is a different set of user messages that will cause different steps to be triggered using the same pipeline.<br/>
/// For visual reference of the process check the <see href="https://github.com/microsoft/semantic-kernel/tree/main/dotnet/samples/GettingStartedWithProcesses/README.md#step02b_accountOpening" >diagram</see>.
/// </summary>
public static class NewAccountVerificationProcess
{
    public static ProcessBuilder CreateProcess()
    {
        ProcessBuilder process = new("AccountVerificationProcess");

        var customerCreditCheckStep = process.AddStepFromType<CreditScoreCheckStep>();
        //ri
        //var fraudDetectionCheckStep = process.AddStepFromType<FraudDetectionStep>();

        // When the newCustomerForm is completed...
        process
            .OnInputEvent(AccountOpeningEvents.NewCustomerFormCompleted)
            // The information gets passed to the core system record creation step
            .SendEventTo(new ProcessFunctionTargetBuilder(customerCreditCheckStep, functionName: CreditScoreCheckStep.Functions.DetermineCreditScore, parameterName: "customerDetails"));
        // The information gets passed to the fraud detection step for validation
        //ri
        //.SendEventTo(new ProcessFunctionTargetBuilder(fraudDetectionCheckStep, functionName: FraudDetectionStep.Functions.FraudDetectionCheck, parameterName: "customerDetails"));

        // When the creditScoreCheck step results in Approval, the information gets to the fraudDetection step to kickstart this step
        customerCreditCheckStep
            .OnEvent(AccountOpeningEvents.CreditScoreCheckApproved);
        //ri
        //.SendEventTo(new ProcessFunctionTargetBuilder(fraudDetectionCheckStep, functionName: FraudDetectionStep.Functions.FraudDetectionCheck, parameterName: "previousCheckSucceeded"));

        return process;
    }
}
