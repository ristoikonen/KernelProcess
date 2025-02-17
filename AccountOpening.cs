using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SKProcess.Steps;

namespace ProcessVectors;

    /// <summary>
    /// Get customer data, do fraud dectetion before opening an account
    /// </summary>
    public class AccountOpening //:BaseTest
    {
        #pragma warning disable SKEXP0070 // Chat completion connector is currently experimental.
        #pragma warning disable SKEXP0001 // AsChatCompletionService
        #pragma warning disable SKEXP0050 // Microsoft.SemanticKernel.Plugins.Web.WebSearchEnginePlugin
        #pragma warning disable SKEXP0080 // Microsoft.SemanticKernel.Process.LocalRuntime
        #pragma warning disable SKEXP0010 // response type

        // Target Open AI Services
        //protected override bool ForceOpenAI => true;

        public async Task<KernelProcess> SetupAccountOpeningProcessAsync<TUserInputStep>() where TUserInputStep : ScriptedUserInputStep
        {
            ProcessBuilder process = new("AccountOpeningProcess");
            var newCustomerFormStep = process.AddStepFromType<CompleteNewCustomerFormStep>();
            var userInputStep = process.AddStepFromType<TUserInputStep>();
            var displayAssistantMessageStep = process.AddStepFromType<DisplayAssistantMessageStep>();
            var customerCreditCheckStep = process.AddStepFromType<CreditScoreCheckStep>();
            var fraudDetectionCheckStep = process.AddStepFromType<FraudDetectionStep>();
            var mailServiceStep = process.AddStepFromType<MailServiceStep>();
            var coreSystemRecordCreationStep = process.AddStepFromType<NewAccountStep>();
            var marketingRecordCreationStep = process.AddStepFromType<NewMarketingEntryStep>();
            var crmRecordStep = process.AddStepFromType<CRMRecordCreationStep>();
            var welcomePacketStep = process.AddStepFromType<WelcomePacketStep>();

            process.OnInputEvent(AccountOpeningEvents.StartProcess)
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
                // The information gets passed to the core system record creation step
                .SendEventTo(new ProcessFunctionTargetBuilder(customerCreditCheckStep, functionName: CreditScoreCheckStep.Functions.DetermineCreditScore, parameterName: "customerDetails"))
                // The information gets passed to the fraud detection step for validation
                .SendEventTo(new ProcessFunctionTargetBuilder(fraudDetectionCheckStep, functionName: FraudDetectionStep.Functions.FraudDetectionCheck, parameterName: "customerDetails"))
                // The information gets passed to the core system record creation step
                .SendEventTo(new ProcessFunctionTargetBuilder(coreSystemRecordCreationStep, functionName: NewAccountStep.Functions.CreateNewAccount, parameterName: "customerDetails"));

            // When the newCustomerForm is completed, the user interaction transcript with the user is passed to the core system record creation step
            newCustomerFormStep
                .OnEvent(AccountOpeningEvents.CustomerInteractionTranscriptReady)
                .SendEventTo(new ProcessFunctionTargetBuilder(coreSystemRecordCreationStep, functionName: NewAccountStep.Functions.CreateNewAccount, parameterName: "interactionTranscript"));

            // When the creditScoreCheck step results in Rejection, the information gets to the mailService step to notify the user about the state of the application and the reasons
            customerCreditCheckStep
                .OnEvent(AccountOpeningEvents.CreditScoreCheckRejected)
                .SendEventTo(new ProcessFunctionTargetBuilder(mailServiceStep, functionName: MailServiceStep.Functions.SendMailToUserWithDetails, parameterName: "message"));

            // When the creditScoreCheck step results in Approval, the information gets to the fraudDetection step to kickstart this step
            customerCreditCheckStep
                .OnEvent(AccountOpeningEvents.CreditScoreCheckApproved)
                .SendEventTo(new ProcessFunctionTargetBuilder(fraudDetectionCheckStep, functionName: FraudDetectionStep.Functions.FraudDetectionCheck, parameterName: "previousCheckSucceeded"));

            // When the fraudDetectionCheck step fails, the information gets to the mailService step to notify the user about the state of the application and the reasons
            fraudDetectionCheckStep
                .OnEvent(AccountOpeningEvents.FraudDetectionCheckFailed)
                .SendEventTo(new ProcessFunctionTargetBuilder(mailServiceStep, functionName: MailServiceStep.Functions.SendMailToUserWithDetails, parameterName: "message"));

            // When the fraudDetectionCheck step passes, the information gets to core system record creation step to kickstart this step
            fraudDetectionCheckStep
                .OnEvent(AccountOpeningEvents.FraudDetectionCheckPassed)
                .SendEventTo(new ProcessFunctionTargetBuilder(coreSystemRecordCreationStep, functionName: NewAccountStep.Functions.CreateNewAccount, parameterName: "previousCheckSucceeded"));

          // When the coreSystemRecordCreation step successfully creates a new accountId, it will trigger the creation of a new marketing entry through the marketingRecordCreation step
                    coreSystemRecordCreationStep
                        .OnEvent(AccountOpeningEvents.NewMarketingRecordInfoReady)
                        .SendEventTo(new ProcessFunctionTargetBuilder(marketingRecordCreationStep, functionName: NewMarketingEntryStep.Functions.CreateNewMarketingEntry, parameterName: "userDetails"));
        
                    // When the coreSystemRecordCreation step successfully creates a new accountId, it will trigger the creation of a new CRM entry through the crmRecord step
                    coreSystemRecordCreationStep
                        .OnEvent(AccountOpeningEvents.CRMRecordInfoReady)
                        .SendEventTo(new ProcessFunctionTargetBuilder(crmRecordStep, functionName: CRMRecordCreationStep.Functions.CreateCRMEntry, parameterName: "userInteractionDetails"));
        /*
                    // ParameterName is necessary when the step has multiple input arguments like welcomePacketStep.CreateWelcomePacketAsync
                    // When the coreSystemRecordCreation step successfully creates a new accountId, it will pass the account information details to the welcomePacket step
                    coreSystemRecordCreationStep
                        .OnEvent(AccountOpeningEvents.NewAccountDetailsReady)
                        .SendEventTo(new ProcessFunctionTargetBuilder(welcomePacketStep, parameterName: "accountDetails"));
        */
        // When the marketingRecordCreation step successfully creates a new marketing entry, it will notify the welcomePacket step it is ready
        marketingRecordCreationStep
            .OnEvent(AccountOpeningEvents.NewMarketingEntryCreated)
                        .SendEventTo(new ProcessFunctionTargetBuilder(welcomePacketStep, parameterName: "marketingEntryCreated"));
        /*
                    // When the crmRecord step successfully creates a new CRM entry, it will notify the welcomePacket step it is ready
                    crmRecordStep
                        .OnEvent(AccountOpeningEvents.CRMRecordInfoEntryCreated)
                        .SendEventTo(new ProcessFunctionTargetBuilder(welcomePacketStep, parameterName: "crmRecordCreated"));

                    // After crmRecord and marketing gets created, a welcome packet is created to then send information to the user with the mailService step
                    welcomePacketStep
                        .OnEvent(AccountOpeningEvents.WelcomePacketCreated)
                        .SendEventTo(new ProcessFunctionTargetBuilder(mailServiceStep, functionName: MailServiceStep.Functions.SendMailToUserWithDetails, parameterName: "message"));

                    // All possible paths end up with the user being notified about the account creation decision throw the mailServiceStep completion
                    mailServiceStep
                        .OnEvent(AccountOpeningEvents.MailServiceSent)
                        .StopProcess();
        */
            KernelProcess kernelProcess = process.Build();
            
            //string generatedImagePath = await Renderers.MermaidRenderer.GenerateMermaidImageAsync(kernelProcess.ToMermaid(), "AccountOpeningProcess.png");
            //Console.WriteLine($"Generated Mermaid process image. Path: { generatedImagePath}" );

            return kernelProcess;
        }
     

    }
