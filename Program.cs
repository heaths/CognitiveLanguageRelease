using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.Tracing;
using Azure;
using Azure.AI.Language.Conversations;
using Azure.AI.Language.QuestionAnswering;
using Azure.Core.Diagnostics;

var command = new RootCommand("Cognitive Service - Language sample application for testing new releases.")
{
    new Option<Uri>(
        "--clu-endpoint",
        getDefaultValue: () =>
        {
            if (Uri.TryCreate(Environment.GetEnvironmentVariable("AZURE_CONVERSATIONS_ENDPOINT"), UriKind.Absolute, out var endpoint))
            {
                return endpoint;
            }

            return null;
        },
        description: "Conversations (formerly QnA Maker) endpoint. The default is the AZURE_CONVERSATIONS_ENDPOINT environment variable, if set.")
    {
        IsRequired = true,
    },

    new Option<string>(
        "--clu-key",
        getDefaultValue: () =>
        {
            return Environment.GetEnvironmentVariable("AZURE_CONVERSATIONS_KEY");
        },
        description: "Conversations API key. The default is the AZURE_CONVERSATIONS_KEY environment variable, if set.")
    {
        IsRequired = true,
    },

    new Option<string>(
        "--clu-project",
        getDefaultValue: () =>
        {
            return Environment.GetEnvironmentVariable("AZURE_CONVERSATIONS_PROJECT_NAME");
        },
        description: "Conversations project to query. The default is the AZURE_CONVERSATIONS_PROJECT_NAME environment variable, if set.")
    {
        IsRequired = true,
    },

    new Option<string>(
        "--clu-deployment",
        getDefaultValue: () =>
        {
            return "production";
        },
        description: "Conversations deployment to query.")
    {
        IsRequired = true,
    },

    new Option<string>(
        "--clu-utterance",
        getDefaultValue: () => "Send an email to Carol about the tomorrow's demo",
        description: "The question to ask the Conversations service.")
    {
        IsRequired = true,
    },

    new Option<Uri>(
        "--qna-endpoint",
        getDefaultValue: () =>
        {
            if (Uri.TryCreate(Environment.GetEnvironmentVariable("AZURE_QUESTIONANSWERING_ENDPOINT"), UriKind.Absolute, out var endpoint))
            {
                return endpoint;
            }

            return null;
        },
        description: "Question Answering (formerly QnA Maker) endpoint. The default is the AZURE_QUESTIONANSWERING_ENDPOINT environment variable, if set.")
    {
        IsRequired = true,
    },

    new Option<string>(
        "--qna-key",
        getDefaultValue: () =>
        {
            return Environment.GetEnvironmentVariable("AZURE_QUESTIONANSWERING_KEY");
        },
        description: "Question Answering API key. The default is the AZURE_QUESTIONANSWERING_KEY environment variable, if set.")
    {
        IsRequired = true,
    },

    new Option<string>(
        "--qna-project",
        getDefaultValue: () =>
        {
            return Environment.GetEnvironmentVariable("AZURE_QUESTIONANSWERING_PROJECT");
        },
        description: "Question Answering project to query. The default is the AZURE_QUESTIONANSWERING_PROJECT environment variable, if set.")
    {
        IsRequired = true,
    },

    new Option<string>(
        "--qna-deployment",
        getDefaultValue: () =>
        {
            return "production";
        },
        description: "Question Answering deployment to query.")
    {
        IsRequired = true,
    },

    new Option<string>(
        "--qna-question",
        getDefaultValue: () => "How are you?",
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

    var cluClient = new ConversationAnalysisClient(options.CluEndpoint, options.CluCredential);
    var cluProject = new ConversationsProject(options.CluProject, options.CluDeployment);

    Console.WriteLine($"Asking \x1b[33mConversations\x1b[m: \x1b[32m{options.CluUtterance}\x1b[m");
    var response = await cluClient.AnalyzeConversationAsync(options.CluUtterance, cluProject);
    
    var cluResult = response.Value as CustomConversationalTaskResult;
    Console.WriteLine($"Top intent \x1b[90m({cluResult.Results.Prediction.ProjectKind})\x1b[m: {cluResult.Results.Prediction.TopIntent}");

    Console.WriteLine();

    var qnaClient = new QuestionAnsweringClient(options.QnaEndpoint, options.QnaCredential);

    Console.WriteLine($"Asking \x1b[33mQuestion Answering\x1b[m: \x1b[32m{options.QnaQuestion}\x1b[m");
    AnswersResult qnaResult = await qnaClient.GetAnswersAsync(options.QnaQuestion, options.QnaProjectModel);

    foreach (Azure.AI.Language.QuestionAnswering.KnowledgeBaseAnswer answer in qnaResult.Answers)
    {
        Console.WriteLine($"\x1b[90m({answer.Confidence:P2})\x1b[m {answer.Answer}");
        Console.WriteLine($"\x1b[90mSource: {answer.Source}\x1b[m");
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
