﻿using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SKProcess.Steps;


namespace ProcessVectors;

#pragma warning disable SKEXP0070 // Chat completion connector is currently experimental.
#pragma warning disable SKEXP0001 // AsChatCompletionService
#pragma warning disable SKEXP0050 // Microsoft.SemanticKernel.Plugins.Web.WebSearchEnginePlugin
#pragma warning disable SKEXP0080 // Microsoft.SemanticKernel.Process.LocalRuntime
#pragma warning disable SKEXP0010 // response type

/// <summary>
/// Demonstrate creation of <see cref="KernelProcess"/> and
/// eliciting its response to five explicit user messages.<br/>
/// For each test there is a different set of user messages that will cause different steps to be triggered using the same pipeline.<br/>
/// For visual reference of the process check the <see href="https://github.com/microsoft/semantic-kernel/tree/main/dotnet/samples/GettingStartedWithProcesses/README.md#step02a_accountOpening" >diagram</see>.
/// </summary>
public class Step02b_AccountOpening() //: BaseTest(output, redirectSystemConsoleOutput: true)
{
    // Target Open AI Services
    //protected override bool ForceOpenAI => true;
    //StartMeUps startMeUps
    public async Task<KernelProcess> SetupAccountOpeningProcessAsync<TUserInputStep>() where TUserInputStep : ScriptedUserInputStep
    {
        ProcessBuilder process = new("AccountOpeningProcessWithSubprocesses");
        var newCustomerFormStep = process.AddStepFromType<CompleteNewCustomerFormStep>();
        var userInputStep = process.AddStepFromType<TUserInputStep>();
        var displayAssistantMessageStep = process.AddStepFromType<DisplayAssistantMessageStep>();
        var accountVerificationStep = process.AddStepFromProcess(NewAccountVerificationProcess.CreateProcess());
        var accountCreationStep = process.AddStepFromProcess(NewAccountCreationProcess.CreateProcess());
        var mailServiceStep = process.AddStepFromType<MailServiceStep>();

                

        process
            .OnInputEvent(AccountOpeningEvents.StartProcess)
            .SendEventTo(new ProcessFunctionTargetBuilder(newCustomerFormStep, CompleteNewCustomerFormStep.Functions.NewAccountWelcome));

        // When the welcome message is generated, send message to displayAssistantMessageStep
        newCustomerFormStep
            .OnEvent(AccountOpeningEvents.NewCustomerFormWelcomeMessageComplete)
            .SendEventTo(new ProcessFunctionTargetBuilder(displayAssistantMessageStep, DisplayAssistantMessageStep.Functions.DisplayAssistantMessage));

        // When the userInput step emits a user input event, send it to the newCustomerForm step
        // Function names are necessary when the step has multiple public functions like CompleteNewCustomerFormStep: NewAccountWelcome and NewAccountProcessUserInfo
        userInputStep
            .OnEvent(CommonEvents.UserInputReceived)
            .SendEventTo(new ProcessFunctionTargetBuilder(newCustomerFormStep, CompleteNewCustomerFormStep.Functions.NewAccountProcessUserInfo, "userMessage"));

        //userInputStep
        //    .OnEvent(CommonEvents.Exit)
        //    .StopProcess();

        // When the newCustomerForm step emits needs more details, send message to displayAssistantMessage step
        newCustomerFormStep
            .OnEvent(AccountOpeningEvents.NewCustomerFormNeedsMoreDetails)
            .SendEventTo(new ProcessFunctionTargetBuilder(displayAssistantMessageStep, DisplayAssistantMessageStep.Functions.DisplayAssistantMessage));

        // After any assistant message is displayed, user input is expected to the next step is the userInputStep
        displayAssistantMessageStep
            .OnEvent(CommonEvents.AssistantResponseGenerated)
            .SendEventTo(new ProcessFunctionTargetBuilder(userInputStep, ScriptedUserInputStep.Functions.GetUserInput));

        // When the newCustomerForm is completed...
        newCustomerFormStep
            .OnEvent(AccountOpeningEvents.NewCustomerFormCompleted)
            // The information gets passed to the account verificatino step
            .SendEventTo(accountVerificationStep.WhereInputEventIs(AccountOpeningEvents.NewCustomerFormCompleted))
            // The information gets passed to the validation process step
            .SendEventTo(accountCreationStep.WhereInputEventIs(AccountOpeningEvents.NewCustomerFormCompleted));

        // When the newCustomerForm is completed, the user interaction transcript with the user is passed to the core system record creation step
        newCustomerFormStep
            .OnEvent(AccountOpeningEvents.CustomerInteractionTranscriptReady)
            .SendEventTo(accountCreationStep.WhereInputEventIs(AccountOpeningEvents.CustomerInteractionTranscriptReady));

        // When the creditScoreCheck step results in Rejection, the information gets to the mailService step to notify the user about the state of the application and the reasons
        accountVerificationStep
            .OnEvent(AccountOpeningEvents.CreditScoreCheckRejected)
            .SendEventTo(new ProcessFunctionTargetBuilder(mailServiceStep));
        
        // When the fraudDetectionCheck step fails, the information gets to the mailService step to notify the user about the state of the application and the reasons
        accountVerificationStep
            .OnEvent(AccountOpeningEvents.FraudDetectionCheckFailed)
            .SendEventTo(new ProcessFunctionTargetBuilder(mailServiceStep));

        // When the fraudDetectionCheck step passes, the information gets to core system record creation step to kickstart this step
        accountVerificationStep
            .OnEvent(AccountOpeningEvents.FraudDetectionCheckPassed)
            .SendEventTo(accountCreationStep.WhereInputEventIs(AccountOpeningEvents.NewAccountVerificationCheckPassed));

        // After crmRecord and marketing gets created, a welcome packet is created to then send information to the user with the mailService step
        accountCreationStep
            .OnEvent(AccountOpeningEvents.WelcomePacketCreated)
            .SendEventTo(new ProcessFunctionTargetBuilder(mailServiceStep));

        // All possible paths end up with the user being notified about the account creation decision throw the mailServiceStep completion
        mailServiceStep
            .OnEvent(AccountOpeningEvents.MailServiceSent)
            .StopProcess();
        /**/

        KernelProcess kernelProcess = process.Build();
        
        // generate Mermaid process graph image
        //var mernmaidcode = kernelProcess.ToMermaid();
        //string? generatedImagePath = await Renderers.MermaidRenderer.GenerateMermaidImageAsync(mernmaidcode, "AccountOpeningProcessStep2b.png");
        //Console.WriteLine("Generated Mermaid process image. Path: { generatedImagePath}");

        return kernelProcess;
    }

    protected Kernel CreateKernelWithChatCompletion()
    {
        // Use LOOONG timeout!
        HttpClient httpClient5minTimeout = new HttpClient()
        {
            Timeout = TimeSpan.FromMinutes(5),
            BaseAddress = new Uri("http://localhost:11434")
        };

        IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddOllamaChatCompletion("llama3.2", httpClient5minTimeout);

        return kernelBuilder.Build();
    }

    /// <summary>
    /// This test uses a specific userId and DOB that makes the creditScore and Fraud detection to pass
    /// </summary>
    public async Task UseAccountOpeningProcessSuccessfulInteractionAsync()
    {
        Kernel kernel = CreateKernelWithChatCompletion();
        KernelProcess kernelProcess = await SetupAccountOpeningProcessAsync<UserInputSuccessfulInteractionStep>();
        using var runningProcess = await kernelProcess.StartAsync(kernel, new KernelProcessEvent() { Id = AccountOpeningEvents.StartProcess, Data = null });
    }

    /// <summary>
    /// This test uses a specific DOB that makes the creditScore to fail
    /// </summary>
    public async Task UseAccountOpeningProcessFailureDueToCreditScoreFailureAsync()
    {
        Kernel kernel = CreateKernelWithChatCompletion();
        KernelProcess kernelProcess = await SetupAccountOpeningProcessAsync<UserInputCreditScoreFailureInteractionStep>();
        using var runningProcess = await kernelProcess.StartAsync(kernel, new KernelProcessEvent() { Id = AccountOpeningEvents.StartProcess, Data = null });
    }

    /// <summary>
    /// This test uses a specific userId that makes the fraudDetection to fail
    /// </summary>
    public async Task UseAccountOpeningProcessFailureDueToFraudFailureAsync()
    {
        Kernel kernel = CreateKernelWithChatCompletion();
        KernelProcess kernelProcess = await SetupAccountOpeningProcessAsync<UserInputFraudFailureInteractionStep>();
        using var runningProcess = await kernelProcess.StartAsync(kernel, new KernelProcessEvent() { Id = AccountOpeningEvents.StartProcess, Data = null });
    }

    /// <summary>
    /// Demonstrate creation of <see cref="KernelProcess"/> and
    /// eliciting its response to five explicit user messages.<br/>
    /// For each test there is a different set of user messages that will cause different steps to be triggered using the same pipeline.<br/>
    /// For visual reference of the process check the <see href="https://github.com/microsoft/semantic-kernel/tree/main/dotnet/samples/GettingStartedWithProcesses/README.md#step02b_accountOpening" >diagram</see>.
    /// </summary>
    public static class NewAccountCreationProcess
    {
        public static ProcessBuilder CreateProcess()
        {
            ProcessBuilder process = new("AccountCreationProcess");

            var coreSystemRecordCreationStep = process.AddStepFromType<NewAccountStep>();
            var marketingRecordCreationStep = process.AddStepFromType<NewMarketingEntryStep>();
            var crmRecordStep = process.AddStepFromType<CRMRecordCreationStep>();
            var welcomePacketStep = process.AddStepFromType<WelcomePacketStep>();

            // When the newCustomerForm is completed...
            process
                .OnInputEvent(AccountOpeningEvents.NewCustomerFormCompleted)
                // The information gets passed to the core system record creation step
                .SendEventTo(new ProcessFunctionTargetBuilder(coreSystemRecordCreationStep, functionName: NewAccountStep.Functions.CreateNewAccount, parameterName: "customerDetails"));

            // When the newCustomerForm is completed, the user interaction transcript with the user is passed to the core system record creation step
            process
                .OnInputEvent(AccountOpeningEvents.CustomerInteractionTranscriptReady)
                .SendEventTo(new ProcessFunctionTargetBuilder(coreSystemRecordCreationStep, functionName: NewAccountStep.Functions.CreateNewAccount, parameterName: "interactionTranscript"));

            // When the fraudDetectionCheck step passes, the information gets to core system record creation step to kickstart this step
            process
                .OnInputEvent(AccountOpeningEvents.NewAccountVerificationCheckPassed)
                .SendEventTo(new ProcessFunctionTargetBuilder(coreSystemRecordCreationStep, functionName: NewAccountStep.Functions.CreateNewAccount, parameterName: "previousCheckSucceeded"));

            // When the coreSystemRecordCreation step successfully creates a new accountId, it will trigger the creation of a new marketing entry through the marketingRecordCreation step
            coreSystemRecordCreationStep
                .OnEvent(AccountOpeningEvents.NewMarketingRecordInfoReady)
                .SendEventTo(new ProcessFunctionTargetBuilder(marketingRecordCreationStep, functionName: NewMarketingEntryStep.Functions.CreateNewMarketingEntry, parameterName: "userDetails"));

            // When the coreSystemRecordCreation step successfully creates a new accountId, it will trigger the creation of a new CRM entry through the crmRecord step
            coreSystemRecordCreationStep
                .OnEvent(AccountOpeningEvents.CRMRecordInfoReady)
                .SendEventTo(new ProcessFunctionTargetBuilder(crmRecordStep, functionName: CRMRecordCreationStep.Functions.CreateCRMEntry, parameterName: "userInteractionDetails"));

            // ParameterName is necessary when the step has multiple input arguments like welcomePacketStep.CreateWelcomePacketAsync
            // When the coreSystemRecordCreation step successfully creates a new accountId, it will pass the account information details to the welcomePacket step
            coreSystemRecordCreationStep
                .OnEvent(AccountOpeningEvents.NewAccountDetailsReady)
                .SendEventTo(new ProcessFunctionTargetBuilder(welcomePacketStep, parameterName: "accountDetails"));

            // When the marketingRecordCreation step successfully creates a new marketing entry, it will notify the welcomePacket step it is ready
            marketingRecordCreationStep
                .OnEvent(AccountOpeningEvents.NewMarketingEntryCreated)
                .SendEventTo(new ProcessFunctionTargetBuilder(welcomePacketStep, parameterName: "marketingEntryCreated"));

            // When the crmRecord step successfully creates a new CRM entry, it will notify the welcomePacket step it is ready
            crmRecordStep
                .OnEvent(AccountOpeningEvents.CRMRecordInfoEntryCreated)
                .SendEventTo(new ProcessFunctionTargetBuilder(welcomePacketStep, parameterName: "crmRecordCreated"));

            return process;
        }
    }


}