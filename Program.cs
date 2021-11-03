using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.Tracing;
using Azure;
// using Azure.AI.Language.Conversations;
using Azure.AI.Language.QuestionAnswering;
using Azure.Core.Diagnostics;

var command = new RootCommand("Cognitive Service - Language sample application for testing new releases.")
{
    new Option<Uri>(
        "--clu-endpoint",
        getDefaultValue: () =>
        {
            if (Uri.TryCreate(Environment.GetEnvironmentVariable("CONVERSATIONS_URI"), UriKind.Absolute, out var endpoint))
            {
                return endpoint;
            }

            return null;
        },
        description: "Conversations (formerly QnA Maker) endpoint. The default is the CONVERSATIONS_URI environment variable, if set.")
    {
        IsRequired = true,
    },

    new Option<string>(
        "--clu-key",
        getDefaultValue: () =>
        {
            return Environment.GetEnvironmentVariable("QUESTIONANSWERING_PROJECT");
        },
        description: "Conversations API key. The default is the CONVERSATIONS_KEY environment variable, if set.")
    {
        IsRequired = true,
    },

    new Option<string>(
        "--clu-project",
        getDefaultValue: () =>
        {
            return Environment.GetEnvironmentVariable("CONVERSATIONS_PROJECT");
        },
        description: "Conversations project to query. The default is the CONVERSATIONS_PROJECT environment variable, if set.")
    {
        IsRequired = true,
    },

    new Option<string>(
        "--clu-deployment",
        getDefaultValue: () =>
        {
            return "prod";
        },
        description: "Conversations deployment to query.")
    {
        IsRequired = true,
    },

    new Option<string>(
        "--clu-utterance",
        getDefaultValue: () => "We'll have 2 plates of seared salmon nigiri.",
        description: "The question to ask the Conversations service.")
    {
        IsRequired = true,
    },

    new Option<Uri>(
        "--qna-endpoint",
        getDefaultValue: () =>
        {
            if (Uri.TryCreate(Environment.GetEnvironmentVariable("QUESTIONANSWERING_ENDPOINT"), UriKind.Absolute, out var endpoint))
            {
                return endpoint;
            }

            return null;
        },
        description: "Question Answering (formerly QnA Maker) endpoint. The default is the QUESTIONANSWERING_ENDPOINT environment variable, if set.")
    {
        IsRequired = true,
    },

    new Option<string>(
        "--qna-key",
        getDefaultValue: () =>
        {
            return Environment.GetEnvironmentVariable("QUESTIONANSWERING_KEY");
        },
        description: "Question Answering API key. The default is the QUESTIONANSWERING_KEY environment variable, if set.")
    {
        IsRequired = true,
    },

    new Option<string>(
        "--qna-project",
        getDefaultValue: () =>
        {
            return Environment.GetEnvironmentVariable("QUESTIONANSWERING_PROJECT");
        },
        description: "Question Answering project to query. The default is the QUESTIONANSWERING_PROJECT environment variable, if set.")
    {
        IsRequired = true,
    },

    new Option<string>(
        "--qna-deployment",
        getDefaultValue: () =>
        {
            return "test";
        },
        description: "Question Answering deployment to query.")
    {
        IsRequired = true,
    },

    new Option<string>(
        "--qna-question",
        getDefaultValue: () => "How long should my Surface battery last?",
        description: "The question to ask the Question Answering service.")
    {
        IsRequired = true,
    },

    new Option<bool>(
        new[]{"-d", "--debug" },
        "Enable debug logging."),
};

command.Handler = CommandHandler.Create<Options>(async options =>
{
    using var _debug = options.Debug ? new AzureEventSourceListener(
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

    var qnaClient = new QuestionAnsweringClient(options.QnaEndpoint, options.QnaCredential);

    Console.WriteLine($"Asking \x1b[33mQuestion Answering\x1b[m: \x1b[32m{options.QnaQuestion}\x1b[m");
    AnswersResult result = await qnaClient.GetAnswersAsync(options.QnaQuestion, options.QnaProjectModel);

    foreach (KnowledgeBaseAnswer answer in result.Answers)
    {
        Console.WriteLine($"({answer.Confidence:P2}) {answer.Answer}");
        Console.WriteLine($"Source: {answer.Source}");
        Console.WriteLine();
    }
});

return await command.InvokeAsync(args);

class Options
{
    public Uri QnaEndpoint { get; set; }
    public string QnaKey { get; set; }
    public string QnaProject { get; set; }
    public string QnaDeployment { get; set; }
    public string QnaQuestion { get; set; }

    public AzureKeyCredential QnaCredential => new AzureKeyCredential(QnaKey);
    public QuestionAnsweringProject QnaProjectModel => new QuestionAnsweringProject(QnaProject, QnaDeployment);

    public Uri CluEndpoint { get; set; }
    public string CluKey { get; set; }
    public string CluProject { get; set; }
    public string CluDeployment { get; set; }
    public string CluUtterance { get; set; }

    public AzureKeyCredential CluCredential => new AzureKeyCredential(CluKey);

    public bool Debug { get; set; }
}
