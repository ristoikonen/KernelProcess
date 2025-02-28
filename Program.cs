

using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using ProcessVectors;
using SKProcess;
using System;
using System.Runtime.CompilerServices;
using static FxConsole.FxConsole;

namespace FxConsole;
class FxConsole
{
    static async Task Main(string[] args)
    {
        var starts = await FillStartMeUpsAsync();

        // write title
        SpectreConsoleOutput.DisplayTitleH3($"Semantic Kernel Process Framework application using " + starts.ModelName + " code by MS Sample and Risto.");

        // user choice scenarios
        var scenarios = SpectreConsoleOutput.SelectScenarios();
        var scenario = scenarios[0];

        // present
        switch (scenario)
        {
            case "Gather customer data for nutrition data":
                SpectreConsoleOutput.DisplayTitleH3($"Gather customer data for nutrition data, using model {starts.ModelName} with endpoint {starts.ModelEndpoint}");
                ProcessVectors.ProcessVectors basicFxVectors = new(starts.ModelEndpoint, starts.ModelName);
                await basicFxVectors.CreateKernelAsync();
                break;

            case "New Account":
                SpectreConsoleOutput.DisplayTitleH3($"Simulate account opening process by gathering cust data, same process steps as above, additionally; when done, create new account, using model {starts.ModelName} with endpoint {starts.ModelEndpoint}");
                Step02b_AccountOpening step02b = new();
                var kernelProcess_AccOpen = await step02b.SetupAccountOpeningProcessAsync<ChatUserAccountInputStep>();
                
                await Utilities.StartOllamaChatKernelProcessAsync(
                    starts.ModelName, starts.ModelEndpoint, kernelProcess_AccOpen, "Nutrition Assistant for Customers");

                break;
            case "Open an Account":
                SpectreConsoleOutput.DisplayTitleH3($"NOT READY YET.., using model {starts.ModelName} with endpoint {starts.ModelEndpoint}");
                // tester();
                AccountOpening accountOpening = new();
                await accountOpening.SetupAccountOpeningProcessAsync<ScriptedUserInputStep>();
                break;

            case "Write Email":
                SpectreConsoleOutput.DisplayTitleH3($"Uses functions to write emails per List of Customer, using model {starts.ModelName} with endpoint {starts.ModelEndpoint}");
                await EmailPerCustomer.LocalModel_ExampleAsync(starts.ModelEndpoint, starts.ModelName);

                break;
            // TODO: simplify process
            /*
            case "Account Info":
                SpectreConsoleOutput.DisplayTitleH3($"Simulate account opening process by gathering cust data, same process steps as above, additionally; when done, create new account, using model {starts.ModelName} with endpoint {starts.ModelEndpoint}");
                Step02b_AccountOpening step02b = new();
                var kernelProcess_AccOpen = await step02b.SetupAccountOpeningProcessAsync<ChatUserAccountInputStep>();
                await Utilities.StartOllamaChatKernelProcessAsync(
                    starts.ModelName, starts.ModelEndpoint, kernelProcess_AccOpen, "Nutrition Assistant for Customers");

                break;
            */
            case "Create Store":
                
                SpectreConsoleOutput.DisplayTitleH3($"Create Vector Store, using model {starts.ModelName} with endpoint {starts.ModelEndpoint}");
                CreateVectorStore createVectorStore = new();
                await createVectorStore.CreateVectorStoreAsync(starts);

                break;


                
        }
    }

    static async Task<StartMeUps> FillStartMeUpsAsync()
    {
        return await Task.FromResult<StartMeUps>(new StartMeUps
        {
            ModelEndpoint = new Uri("http://localhost:11434"),
            ModelName = "llama3.2" // "mistral"  "deepseek-r1:1.5b"
        });
    }


    public static void tester() 
    { 
        Customer c = new FxConsole.Customer();
        c.LastName = "123";
        c.Email = "me@gmx.com";
        //c.Hash = "123";
        Chatter cha = new Chatter();
        cha.Email = "me@gmx.com";
        cha.LastName = "123";
        var isit = c.Equals(cha);
        cha.CreateBaseHash();
        string hs = cha.BaseHash;

        var matt = Utilities.Matches(hs, c.Email, c.LastName);

    }


    public static string GenerateBaseHash<T>(string Email, string lastName) where T : IBaseIndexer, new()
    {
        //T obj;// = new T
        //obj.BaseHash = SKProcess.Utilities.GenerateHash(Email, lastName);
        string hash = Utilities.GenerateHash(Email, lastName);
        return hash; 
        //IBaseIndexer
        //BaseHash = hash;
        //SKProcess.Utilities.GenerateHash(Email, lastName);
        //obj.BaseHash = Email + lastName;
        //return obj.BaseHash;
    }



    public interface IBaseIndexer
    {
        public string BaseHash { get; set; }
        public void CreateBaseHash();
    }

    public class Customer : IEquatable<IBaseCustomer>, IBaseIndexer
    {
        public Customer()
        {
            //ID = Guid.NewGuid();
        }

        //public Guid ID { get; set; }

        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string Hash { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string BaseHash { get; set; } = default!;

        public void CreateBaseHash()
        {
            this.BaseHash = GenerateBaseHash<Customer>(Email, LastName);
            return; // this.BaseHash;
        }

        //    void IBaseIndexer.CreateBaseHash()
        //    {
        //        throw new NotImplementedException();where T : new ()
        //}

        bool IEquatable<IBaseCustomer>.Equals(IBaseCustomer? other)
        {
            if(other is null) 
                return false;

            string strA = (this.BaseHash ?? GenerateBaseHash<Customer>(this.Email, this.LastName));
            string strB = (other.BaseHash ?? GenerateBaseHash<Customer>(other.Email, other.LastName));
            return string.Compare(strA, strB, StringComparison.OrdinalIgnoreCase) == 0;
        }

        //TODO: override GetHashCode
        //public override int GetHashCode()
        //{
        //    CreateBaseHash();
        //    return this.BaseHash
        //        //base.GetHashCode();
        //}
        public override bool Equals(Object? obj) 
        {
            if (obj is not IBaseCustomer || obj is null)
                return false;

            string strA = (this.BaseHash ?? GenerateBaseHash<Customer>(this.Email, this.LastName));
            string strB = (((IBaseCustomer)obj).BaseHash ?? GenerateBaseHash<Customer>(((IBaseCustomer)obj).Email, ((IBaseCustomer)obj).LastName));
            return string.Compare(strA, strB, StringComparison.OrdinalIgnoreCase) == 0;
        }


        //int IMatcher<Customer>.Matches(Customer other)
        //{
        //    //return (other.BaseHash ?? GenerateBaseHash<Customer>(other.Email, other.ID)).ToUpper() ==
        //    //    this.BaseHash ?? GenerateBaseHash<Customer>(this.Email, this.ID);
        //    string strA = (other.BaseHash ?? GenerateBaseHash<Customer>(other.Email, other.ID));
        //    string strB = (other.BaseHash ?? GenerateBaseHash<Customer>(other.Email, other.ID));
        //    return string.Compare(strA, strB, StringComparison.OrdinalIgnoreCase);
        //}
    }

    //public interface IMatcher<T>
    //{
    //    int Matches(T other);
    //}


    public interface IBaseCustomer : IBaseIndexer
    {
        public string LastName { get; set; }
        public string Email { get; set; }
        
    }

    public class Chatter : IBaseCustomer
    {
        public List<string> ChatterLine { get; set; } = default!;
        //public string ID { get; set; }

        public string LastName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string BaseHash { get; set; } = default!;

        public void CreateBaseHash()
        {
            this.BaseHash = GenerateBaseHash<Chatter>(Email, LastName);
            return; // this.BaseHash;
        }
    }

}
