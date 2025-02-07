# SemanticKernel Process Framework Cmd-line application 
## Local model is llama3.2 or similar. 


Process Framework has graph approach with nodes and edges.
I belive it or similar, will be useful as a bridge between AI and business processes.

### Notes
- Command line use, type: skprocess
- Targeting AI-automation of a business process
- Command line application that uses the Semantic Kernel's Process Framework to interact with a model.
- .NET 9.0,  C#  
- Install models using **Ollama run libraryname**, running in localhost 11434, using CPU .
- Uses latest **prerelease libraries** of Microsoft.Extensions.AI.Ollama and Microsoft.SemanticKernel.Connectors.Ollama



        // spot to serialise AI gathered data
        if ((_state?.newCustomerForm != null) && (_state?.newCustomerForm.IsFormCompleted() == true ))
        {
            Console.WriteLine($"[NEW_USER_FORM_COMPLETED]: {JsonSerializer.Serialize(_state?.newCustomerForm)}");
            var form = _state?.newCustomerForm;
            // All user information is gathered to proceed to the next step
            await context.EmitEventAsync(new() { Id = AccountOpeningEvents.NewCustomerFormCompleted, Data = _state?.newCustomerForm, Visibility = KernelProcessEventVisibility.Public });
            await context.EmitEventAsync(new() { Id = AccountOpeningEvents.CustomerInteractionTranscriptReady, Data = _state?.conversation, Visibility = KernelProcessEventVisibility.Public });
            return;
        }



Step Events: OnEvent, OnFunctionResult, SendOutputTo
OnEvent: Triggered when the class completes its execution.
OnFunctionResult: Activated when the defined Kernel Function emits results, allowing output to be sent to one or many Steps.
SendOutputTo: Defines the Step and Input for sending results to a subsequent Step.

