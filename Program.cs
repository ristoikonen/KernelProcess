

namespace FxConsole;
class FxConsole
{
    static async Task Main(string[] args)
    {
        var starts = await FillStartMeUpsAsync();

        // write title
        SpectreConsoleOutput.DisplayTitleH3($"Semantic Kernel Process Framework application using " + starts.ModelName + " coded by Risto.");

        // user choice scenarios
        var scenarios = SpectreConsoleOutput.SelectScenarios();
        var scenario = scenarios[0];

        // present
        switch (scenario)
        {
            case "Gather customer data for nutrition data":
                ProcessVectors.ProcessVectors basicFxVectors = new(starts.ModelEndpoint, starts.ModelName);
                await basicFxVectors.CreateKernelAsync();
                break;
            case "About":
                // TODO: Add printing of readme here
                // code README.md
                //System.Diagnostics.Process p = new System.Diagnostics.Process();
                //p.StartInfo.WorkingDirectory = @"C:\Users\risto\source\repos\Kernel";
                //p.StartInfo.FileName = "runr README.MD";
                //p.StartInfo.UseShellExecute = true;
                //p.Start();

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
}
