# Tarik - TaskToPR

<img src="https://user-images.githubusercontent.com/2335582/226174735-298dfef7-2108-4f50-8509-c1a7fec807ec.png"  width="10%">

Tarik is a chain-of-thought LLM-based autocoder, intended to be used as a tool for developers to delegate small tasks to.

![image](https://user-images.githubusercontent.com/2335582/227788613-4e38cddb-5637-45ce-97b5-bb0bf3e5b40c.png)

## Project Goal

The goal of this project is to enable developers to delegate small tasks to Tarik, allowing them to focus on more complex aspects of their work. Tarik will, when assigned an issue:

- Suggest a plan
- Create a branch and make changes according to above plan when approved
- Create a PR with above changes

At this point a human can intervene and continue working on the issue - better yet if Tarik's contribution is good enough, simply merge the PR and close the issue.

## How it works

Tarik continuously monitors issues on Github that are assigned to him. When an issue is assigned to Tarik, he will:

- Read the issue description
- (TBD) Look at the code in the repository and build up a mental model of the codebase
  - (TBD) perhaps using vector embeddings to represent the codebase?
- (TBD) Use the codebase model to suggest a plan for the issue
- Create a branch and make changes according to above plan
- Create a PR with above changes

## Tech Stack

- .NET 7

### API's consumed

- [OpenAI's GPT-4 API](https://platform.openai.com/docs/models/gpt-4)
- [Github API](https://docs.github.com/en/rest)

## Getting started

- Install the [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
- Set the [.env](./docs/env.MD) variables
- Initialize the secrets:

```bash
./scripts/dotnet-user-secrets.sh
```

- Run the project:

```bash
dotnet run
```

## [Contribution](./CONTRIBUTION.md)
