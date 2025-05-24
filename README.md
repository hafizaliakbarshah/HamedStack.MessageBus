# HamedStack.MessageBus 🚀

![HamedStack.MessageBus](https://img.shields.io/badge/HamedStack.MessageBus-v1.0.0-blue.svg)
![GitHub Releases](https://img.shields.io/badge/releases-latest-yellowgreen.svg)

Welcome to **HamedStack.MessageBus**! This repository provides a lightweight and flexible mediator implementation for .NET applications. It offers a clean approach to the mediator pattern, enabling decoupled communication between components through commands, queries, and events.

## Table of Contents

- [Features](#features)
- [Getting Started](#getting-started)
- [Installation](#installation)
- [Usage](#usage)
- [Examples](#examples)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)
- [Releases](#releases)

## Features

- **Lightweight Design**: Minimal overhead for high performance.
- **Decoupled Communication**: Components communicate without direct references.
- **Support for Commands, Queries, and Events**: Flexible handling of different message types.
- **Easy Integration**: Works seamlessly with existing .NET applications.
- **Event Handlers**: Support for publish-subscribe patterns.

## Getting Started

To get started with **HamedStack.MessageBus**, you will need to have .NET installed on your machine. You can download the latest version of .NET from the official [Microsoft website](https://dotnet.microsoft.com/download).

### Installation

You can install **HamedStack.MessageBus** via NuGet. Run the following command in your terminal:

```bash
dotnet add package HamedStack.MessageBus
```

Alternatively, you can download the latest release from our [Releases section](https://github.com/hafizaliakbarshah/HamedStack.MessageBus/releases). This will provide you with the necessary files to integrate into your project.

## Usage

After installation, you can start using **HamedStack.MessageBus** in your application. Below is a basic example of how to set up the mediator and use it to send commands and queries.

### Basic Setup

1. **Create a Mediator Instance**:

   ```csharp
   var mediator = new Mediator();
   ```

2. **Define Commands and Queries**:

   ```csharp
   public class MyCommand : ICommand
   {
       public string Data { get; set; }
   }

   public class MyQuery : IQuery<string>
   {
       public int Id { get; set; }
   }
   ```

3. **Create Handlers**:

   ```csharp
   public class MyCommandHandler : ICommandHandler<MyCommand>
   {
       public Task Handle(MyCommand command)
       {
           // Handle command
           return Task.CompletedTask;
       }
   }

   public class MyQueryHandler : IQueryHandler<MyQuery, string>
   {
       public Task<string> Handle(MyQuery query)
       {
           // Handle query and return result
           return Task.FromResult("Result");
       }
   }
   ```

4. **Send Commands and Queries**:

   ```csharp
   await mediator.Send(new MyCommand { Data = "Test" });
   var result = await mediator.Send(new MyQuery { Id = 1 });
   ```

## Examples

To see more examples of how to use **HamedStack.MessageBus**, please refer to the `Examples` folder in the repository. You will find detailed implementations for various use cases, including:

- Sending commands
- Handling events
- Querying data

## Contributing

We welcome contributions to **HamedStack.MessageBus**! If you would like to contribute, please follow these steps:

1. Fork the repository.
2. Create a new branch (`git checkout -b feature/YourFeature`).
3. Make your changes and commit them (`git commit -m 'Add new feature'`).
4. Push to the branch (`git push origin feature/YourFeature`).
5. Create a pull request.

Please ensure your code adheres to the existing coding standards and includes appropriate tests.

## License

**HamedStack.MessageBus** is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.

## Contact

For any questions or suggestions, feel free to reach out via GitHub issues or contact me directly.

## Releases

You can find the latest releases of **HamedStack.MessageBus** [here](https://github.com/hafizaliakbarshah/HamedStack.MessageBus/releases). Make sure to download the necessary files and execute them as needed.

Thank you for your interest in **HamedStack.MessageBus**! We hope you find it useful for your .NET applications. Happy coding!