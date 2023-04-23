using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using Polly;
using Tarik.Application.Common;
using TiktokenSharp;

namespace Tarik.Infrastructure;

public class ShortTermMemoryService : IShortTermMemoryService, IAsyncDisposable
{
    private readonly IOpenAIService _openAIService;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly ILogger<ShortTermMemoryService> _logger;
    private readonly Dictionary<string, Dictionary<string, FileData>> _memory;

    public ShortTermMemoryService(IOpenAIService openAIService, ILogger<ShortTermMemoryService> logger)
    {
        _logger = logger;
        _openAIService = openAIService;
        _retryPolicy = RetryPolicies.CreateRetryPolicy(2, _logger);
        _memory = LoadMemoryFromFileAsync();
    }

    public string Dump(string repoOwner, string repoName)
    {
        var repoMemory = getMemoryForRepo(repoOwner, repoName);

        if (repoMemory.Count == 0)
        {
            return "";
        }

        return $"""
        {repoMemory.Count} files in short-term memory:
        {string.Join(Environment.NewLine, repoMemory.Select(x => $"{x.Key} => {x.Value.Text}"))}
        """;
    }

    public string? Recall(string repoOwner, string repoName, PathTo path, string fileHash)
    {
        var repoMemory = getMemoryForRepo(repoOwner, repoName);

        if (repoMemory.TryGetValue(path.RelativePath, out var value))
        {
            if (value.FileHash == fileHash)
            {
                return value.Text;
            }
        }

        return null;
    }

    public async Task Memorize(string repoOwner, string repoName, PathTo path, CancellationToken cancellationToken)
    {
        // using md5 get hash of file in path
        using var md5 = MD5.Create();

        string content = await File.ReadAllTextAsync(path.AbsolutePath, cancellationToken);
        string fileHash = Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(content)));

        if (Recall(repoOwner, repoName, path, fileHash) == null)
        {
            _logger.LogInformation($"File {path.RelativePath} has changed, summarizing");
            // if hash is not in memory, summarize file
            string summary = await Summarize(path, fileHash, content, cancellationToken);
            // store hash in memory
            var repoMemory = getMemoryForRepo(repoOwner, repoName);
            repoMemory[path.RelativePath] = new FileData(fileHash, summary);
        }
        else
        {
            _logger.LogInformation($"File {path.RelativePath} has not changed, skipping");
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

    private Dictionary<string, FileData> getMemoryForRepo(string repoOwner, string repoName)
    {
        string repoId = repoIdentifier(repoOwner, repoName);
        if (!_memory.ContainsKey(repoId))
        {
            _memory[repoId] = new Dictionary<string, FileData>();
        }

        return _memory[repoId];
    }

    private Dictionary<string, Dictionary<string, FileData>> LoadMemoryFromFileAsync()
    {
        string filePath = "short_term_memory_data.json";

        if (File.Exists(filePath))
        {
            string serializedMemory = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, FileData>>>(serializedMemory) ?? new Dictionary<string, Dictionary<string, FileData>>();
        }

        return new Dictionary<string, Dictionary<string, FileData>>();
    }

    private static string repoIdentifier(string repoOwner, string repoName) => $"{repoOwner}/{repoName}";

    public async ValueTask DisposeAsync()
    {
        string filePath = "short_term_memory_data.json";
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        string serializedMemory = JsonSerializer.Serialize(_memory, jsonOptions);
        await File.WriteAllTextAsync(filePath, serializedMemory);
    }
}
