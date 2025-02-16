using Microsoft.SemanticKernel;
using ProcessVectors;


namespace SKProcess.Steps
{
    #pragma warning disable SKEXP0080 // Microsoft.SemanticKernel.Process.LocalRuntime
    #pragma warning disable SKEXP0010 // response type

    /// <summary>
    /// <see cref="ScriptedUserInputStep"/> Step with interactions that makes the Process fail due fraud detection failure
    /// </summary>
    public sealed class UserInputFraudFailureInteractionStep : ScriptedUserInputStep
    {
        public override void PopulateUserInputs(UserInputState state)
        {
            state.UserInputs.Add("I would like to open an account");
            state.UserInputs.Add("I am Fred Hayes born 12/11/1988");
            state.UserInputs.Add("I live in 2913 Franklin ACT my phone is 041423522");
            state.UserInputs.Add("TFN: 183064214");
            state.UserInputs.Add("freddie@gmail.com");
            state.UserInputs.Add("what else do you need?");
        }
    }

    /// <summary>
    /// <see cref="ScriptedUserInputStep"/> Step with interactions that makes the Process fail due credit score failure
    /// </summary>
    public sealed class UserInputCreditScoreFailureInteractionStep : ScriptedUserInputStep
    {
        public override void PopulateUserInputs(UserInputState state)
        {
            state.UserInputs.Add("I would like to open an account");
            state.UserInputs.Add("I am Fred Hayes born 12/11/1988");
            state.UserInputs.Add("I live in 2913 Franklin ACT my phone is 041423522");
            state.UserInputs.Add("TFN: 183064214");
            state.UserInputs.Add("freddie@gmail.com");
            state.UserInputs.Add("what else do you need?");
        }
    }

    /// <summary>
    /// <see cref="ScriptedUserInputStep"/> Step with interactions that makes the Process pass all steps and successfully open a new account
    /// </summary>
    public sealed class UserInputSuccessfulInteractionStep : ScriptedUserInputStep
    {
        public override void PopulateUserInputs(UserInputState state)
        {
            state.UserInputs.Add("I would like to open an account");
            state.UserInputs.Add("I am Fred Hayes born 12/11/1988");
            state.UserInputs.Add("I live in 2913 Franklin ACT my phone is 041423522");
            state.UserInputs.Add("TFN: 183064214");
            state.UserInputs.Add("freddie@gmail.com");
            state.UserInputs.Add("what else do you need?");
        }
    }

    /// <summary>
    /// Mock step that emulates the creation a new marketing user entry.
    /// </summary>
    public class NewMarketingEntryStep : KernelProcessStep
    {
        public static class Functions
        {
            public const string CreateNewMarketingEntry = nameof(CreateNewMarketingEntry);
        }

        [KernelFunction(Functions.CreateNewMarketingEntry)]
        public async Task CreateNewMarketingEntryAsync(KernelProcessStepContext context, MarketingNewEntryDetails userDetails, Kernel _kernel)
        {
            Console.WriteLine($"[MARKETING ENTRY CREATION] New Account {userDetails.AccountId} created");

            // Placeholder for a call to API to create new entry of user for marketing purposes
            await context.EmitEventAsync(new() { Id = AccountOpeningEvents.NewMarketingEntryCreated, Data = true });
        }
    }

    /// <summary>
    /// Mock step that emulates the creation of a Welcome Packet for a new user after account creation
    /// </summary>
    public class WelcomePacketStep : KernelProcessStep
    {
        public static class Functions
        {
            public const string CreateWelcomePacket = nameof(CreateWelcomePacket);
        }

        [KernelFunction(Functions.CreateWelcomePacket)]
        public async Task CreateWelcomePacketAsync(KernelProcessStepContext context, bool marketingEntryCreated, bool crmRecordCreated, AccountDetails accountDetails, Kernel _kernel)
        {
            Console.WriteLine($"[WELCOME PACKET] New Account {accountDetails.AccountId} created");

            var mailMessage = $"""
            Dear {accountDetails.UserFirstName} {accountDetails.UserLastName}
            We are thrilled to inform you that you have successfully created a new PRIME ABC Account with us!
            
            Account Details:
            Account Number: {accountDetails.AccountId}
            Account Type: {accountDetails.AccountType}
            
            Please keep this confidential for security purposes.
            
            Here is the contact information we have in file:
            
            Email: {accountDetails.UserEmail}
            Phone: {accountDetails.UserPhoneNumber}
            
            Thank you for opening an account with us!
            """;

            await context.EmitEventAsync(new()
            {
                Id = AccountOpeningEvents.WelcomePacketCreated,
                Data = mailMessage,
                Visibility = KernelProcessEventVisibility.Public,
            });
        }
    }

    /// <summary>
    /// Mock step that emulates the creation of a new CRM entry
    /// </summary>
    public class CRMRecordCreationStep : KernelProcessStep
    {
        public static class Functions
        {
            public const string CreateCRMEntry = nameof(CreateCRMEntry);
        }

        [KernelFunction(Functions.CreateCRMEntry)]
        public async Task CreateCRMEntryAsync(KernelProcessStepContext context, AccountUserInteractionDetails userInteractionDetails, Kernel _kernel)
        {
            Console.WriteLine($"[CRM ENTRY CREATION] New Account {userInteractionDetails.AccountId} created");

            // Placeholder for a call to API to create new CRM entry
            await context.EmitEventAsync(new() { Id = AccountOpeningEvents.CRMRecordInfoEntryCreated, Data = true });
        }
    }

}
