using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Polly;
using Tarik.Application.Common;
using TiktokenSharp;

namespace Tarik.Infrastructure;

public class ShortTermMemoryService : IShortTermMemoryService
{
    private readonly IOpenAIService _openAIService;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly ILogger<ShortTermMemoryService> _logger;
    private readonly Dictionary<string, (string fileHash, string text)> _memory;

    public ShortTermMemoryService(IOpenAIService openAIService, ILogger<ShortTermMemoryService> logger)
    {
        _logger = logger;
        _openAIService = openAIService;
        _retryPolicy = RetryPolicies.CreateRetryPolicy(2, _logger);
        _memory = new Dictionary<string, (string fileHash, string text)>();
    }

    public string Dump()
    {
        return $"""
        {_memory.Count} files in short-term memory:
        {string.Join(Environment.NewLine, _memory.Select(x => $"{x.Key} => {x.Value.text}"))}
        """;
    }

    public string? Recall(PathTo path, string fileHash)
    {
        if (_memory.TryGetValue(path.RelativePath, out var value))
        {
            if (value.fileHash == fileHash)
            {
                return value.text;
            }
        }

        return null;
    }

    public async Task Memorize(PathTo path, CancellationToken cancellationToken)
    {
        // using md5 get hash of file in path
        using var md5 = MD5.Create();

        string content = await File.ReadAllTextAsync(path.AbsolutePath, cancellationToken);
        string fileHash = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(content)));

        if (Recall(path, fileHash) == null)
        {
            _logger.LogInformation($"File {path} has changed, summarizing");
            // if hash is not in memory, summarize file
            string summary = await Summarize(path, fileHash, content, cancellationToken);
            // store hash in memory
            _memory[path.RelativePath] = (fileHash, summary);
        }
        else
        {
            _logger.LogInformation($"File {path} has not changed, skipping");
        }
    }

    private async Task<string> Summarize(PathTo path, string fileHash, string content, CancellationToken cancellationToken)
    {
        TikToken tikToken = TikToken.EncodingForModel("gpt-3.5-turbo");
        string fileName = Path.GetFileName(path.AbsolutePath);
        string summarizePrompt = SummarizeFilePrompt.SummarizePrompt(fileName, content);
        var summarizePromptTokens = tikToken.Encode(summarizePrompt);

        if (summarizePromptTokens.Count > 4000)
        {
            content = content.Substring(0, content.Length - 1000); // TODO arbirarily shaving off 1000 characters, should be smarter
            return await Summarize(path, fileHash, content, cancellationToken);
        }

        ChatCompletionCreateRequest chatCompletionCreateRequest = new()
        {
            Model = Models.ChatGpt3_5Turbo,
            MaxTokens = 4000 - summarizePromptTokens.Count,
            Temperature = 0.2f,
            N = 1,
            Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem(summarizePrompt),
                }
        };

        _logger.LogDebug($"Sending request to OpenAI: {chatCompletionCreateRequest}");

        var chatResponse = await _retryPolicy
            .ExecuteAsync(async () => await _openAIService.ChatCompletion
                .CreateCompletion(chatCompletionCreateRequest, cancellationToken: cancellationToken)
            );

        if (!chatResponse.Successful)
        {
            throw new HttpRequestException($"Failed to plan work: {chatResponse.Error?.Message}");
        }

        return chatResponse.Choices[0].Message.Content;
    }
}
