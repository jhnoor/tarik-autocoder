```
# Tarik - TaskToPR

<img src="https://user-images.githubusercontent.com/2335582/226174735-298dfef7-2108-4f50-8509-c1a7fec807ec.png"  width="10%">

Tarik is an AI-powered bot designed to streamline the software development process by taking on assigned tasks and creating pull requests in relevant code repositories. When necessary, Tarik engages in discussion within work items to gather more information, ensuring a thorough understanding before proceeding with the task.

![image](https://user-images.githubusercontent.com/2335582/227788613-4e38cddb-5637-45ce-97b5-bb0bf3e5b40c.png)

## Project Goal

The goal of this project is to enable developers to delegate small tasks to Tarik, allowing them to focus on more complex aspects of their work. Tarik can bootstrap an issue, create a branch and PR with suggested changes, and then hand over the work to a human developer to finish it off. This not only saves time but also helps maintain a high level of code quality throughout the development process.

## Naming

Tarik means "history" or "story" in Somali, aptly representing the user stories that Tarik will be ingesting to produce pull requests.

## Tech Stack

This project utilizes the following technologies:

- .NET 7: A high-performance, cross-platform framework for building modern, cloud-based, and internet-connected applications.
- Clean Architecture principles: A set of practices for organizing code in a way that promotes maintainability, testability, and separation of concerns.
- Github API: A RESTful API that allows interaction with GitHub's platform, enabling the automation of various tasks related to repositories, issues, and pull requests.
- OpenAI's GPT-4 API: An advanced language model that powers Tarik's AI capabilities, enabling it to understand and process user stories, as well as generate relevant code changes.

## Documentation

For more information on setting up the environment, please refer to the [env.MD](./docs/env.MD) file in the docs folder.

```