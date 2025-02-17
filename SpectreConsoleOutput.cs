using System.Collections.Generic;
using System.Drawing;
using OllamaSharp.Models.Chat;
using Spectre.Console;

namespace FxConsole
{
    // Only Spectre Console code here!
    public static class SpectreConsoleOutput
    {
        // Main menu
        public static List<string> SelectScenarios()
        {
            // Ask for the user's favorite fruits
            var scenarios = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("Select the [green]scenario[/] to run.")
                    .PageSize(10)
                    .Required(true)
                    .MoreChoicesText("[grey](Move up and down to reveal more scenarios)[/]")
                    .InstructionsText(
                        "[grey](Press [blue]<space>[/] to toggle a scenario, " +
                        "[green]<enter>[/] to accept)[/]")
                    .AddChoices(new[] {
                    "Gather customer data for nutrition data",
                    "New Account",
                    "Open an Account",
                    "Write Email",
                    "Create Store"
                        })
                    //.AddChoiceGroup("Open an Account", new[]
                    //    { "CreateStore"
                    //    })
                    );
            return scenarios;
        }
        public static void DisplayTitle(string title = "")
        {
            AnsiConsole.Write(new FigletText(title).Centered().Color(Spectre.Console.Color.Purple));
        }

        public static void DisplayWait(string? modelName, string? title = "Loading the model...")
        {
            string boxtitle = title ?? "Loading the model " + modelName ?? "" + "...";

            var panel = new Panel(boxtitle)
            {
                Border = BoxBorder.Rounded,
                Padding = new Padding(10, 1, 1, 1),
                BorderStyle = new Style(Spectre.Console.Color.Red)

            };

            AnsiConsole.Write(panel);
        }

        public static void ClearDisplay()
        {
            AnsiConsole.Clear();
        }

        public static void DisplayTitleH2(string subtitle)
        {
            AnsiConsole.MarkupLine($"[bold][blue]=== {subtitle} ===[/][/]");
            AnsiConsole.MarkupLine($"");
        }

        public static void DisplayTitleH3(string subtitle)
        {
            AnsiConsole.MarkupLine($"[bold]>> {subtitle}[/]");
            AnsiConsole.MarkupLine($"");
        }

        public static void DisplayQuestion(string question)
        {
            AnsiConsole.MarkupLine($"[bold][blue]>>Q: {question}[/][/]");
            AnsiConsole.MarkupLine($"");
        }
        public static void DisplayAnswerStart(string answerPrefix)
        {
            AnsiConsole.Markup($"[bold][blue]>> {answerPrefix}:[/][/]");
        }

        public static void DisplayFilePath(string prefix, string filePath)
        {
            var path = new TextPath(filePath);

            AnsiConsole.Markup($"[bold][blue]>> {prefix}: [/][/]");
            AnsiConsole.Write(path);
            AnsiConsole.MarkupLine($"");
        }

        public static void DisplaySubtitle(string prefix, string content)
        {
            AnsiConsole.Markup($"[bold][blue]>> {prefix}: [/][/]");
            AnsiConsole.WriteLine(content);
            AnsiConsole.MarkupLine($"");
        }

        //public static int ShowProgress(string question)
        //{
        //    var number = AnsiConsole.Progress();
        //    number.Start(f => f.AddTaskBefore())
        //    return 1;
        //}

        public static int AskForNumber(string question)
        {
            var number = AnsiConsole.Ask<int>(@$"[green]{question}[/]");
            return number;
        }

        public static string AskForString(string question)
        {
            var response = AnsiConsole.Ask<string>(@$"[green]{question}[/]");
            return response;
        }
    }
}
