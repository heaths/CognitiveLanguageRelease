using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.Tracing;
using Azure.AI.Language.QuestionAnswering;
using Azure;
using Azure.Core.Diagnostics;

var command = new RootCommand
{
    new Argument<Uri>(
        "endpoint",
        getDefaultValue: () =>
        {
            if (Uri.TryCreate(Environment.GetEnvironmentVariable("QUESTIONANSWERING_ENDPOINT"), UriKind.Absolute, out var endpoint))
            {
                return endpoint;
            }

            return null;
        },
        description: "The Cognitive Services endpoint URI."),

    new Option<string>(
        new[]{"-k", "--key"},
        getDefaultValue: () =>
        {
            return Environment.GetEnvironmentVariable("QUESTIONANSWERING_KEY");
        },
        description: "Shared API key. The default is the QUESTIONANSWERING_KEY environment variable, if set.")
    {
        IsRequired = true,
    },

    new Option<string>(
        new[]{"-p", "--project"},
        getDefaultValue: () =>
        {
            return Environment.GetEnvironmentVariable("QUESTIONANSWERING_PROJECT");
        },
        description: "The project to query. The default is the QUESTIONANSWERING_PROJECT environment variable, if set.")
    {
        IsRequired = true,
    },

    new Option<string>(
        "--deployment",
        getDefaultValue: () =>
        {
            return "test";
        },
        description: "The deployment to query. The default is \"test\".")
    {
        IsRequired = true,
    },

    new Option<string>(
        "--question",
        getDefaultValue: () => "How long should my Surface battery last?",
        description: "The question to ask. The default is \"How long should my Surface battery last?\".")
    {
        IsRequired = true,
    },

    new Option<bool>(
        new[]{"-d", "--debug" },
        "Enable debug logging."),
};

command.Description = "Cognitive Service - Language sample application for testing new releases.";
command.Handler = CommandHandler.Create<Uri, string, string, string, string, bool>(async (endpoint, key, project, deployment, question, debug) =>
{
    using var _debug = debug ? new AzureEventSourceListener(
        (args, message) =>
        {
            var fg = Console.ForegroundColor;
            Console.ForegroundColor = args.Level switch
            {
                EventLevel.Error => ConsoleColor.Red,
                EventLevel.Warning => ConsoleColor.Yellow,
                _ => ConsoleColor.DarkGray,
            };

            try
            {
                Console.Write($"[{args.Level}] ");

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = fg;
            }
        },
        EventLevel.Verbose) : null;

    var credential = new AzureKeyCredential(key);
    var qnaClient = new QuestionAnsweringClient(endpoint, credential);
    var qnaProject = new QuestionAnsweringProject(project, deployment);

    Console.WriteLine($"Asking: \x1b[32m{question}\x1b[m");
    AnswersResult result = await qnaClient.GetAnswersAsync(question, qnaProject);

    foreach (KnowledgeBaseAnswer answer in result.Answers)
    {
        Console.WriteLine($"({answer.Confidence:P2}) {answer.Answer}");
        Console.WriteLine($"Source: {answer.Source}");
        Console.WriteLine();
    }
});

return await command.InvokeAsync(args);
