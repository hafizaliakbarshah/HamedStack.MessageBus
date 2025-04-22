# HamedStack.MessageBus

## Introduction

The Message Bus Library is a lightweight, flexible in-process messaging library for .NET applications. It provides a clean approach to implementing the mediator pattern, enabling decoupled communication between components through commands, queries, and events.

Key features include:
- Command and query handling with typed responses
- Event publishing and subscription
- Middleware pipeline for cross-cutting concerns
- Automatic handler and subscriber discovery
- Full integration with Microsoft's Dependency Injection

This library helps you build applications following CQRS (Command Query Responsibility Segregation) principles and enables clean, maintainable code with clear separation of concerns.

## Getting Started

### Basic Setup

To set up the message bus in your application, register it in your service collection during application startup:

```csharp
using Microsoft.Extensions.DependencyInjection;

// In your Startup.cs or Program.cs
services.AddMessageBus();
```

This minimal setup will:
1. Scan all loaded assemblies for handlers and subscribers
2. Register them in the dependency injection container
3. Set up the message bus ready for use

### Using the Message Bus

Inject the `IMessageBus` interface into your components to start using it:

```csharp
public class UserController
{
    private readonly IMessageBus _messageBus;
    
    public UserController(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }
    
    public async Task<UserDto> GetUser(int userId)
    {
        // Send a query through the message bus
        var result = await _messageBus.SendAsync<UserDto>(new GetUserQuery(userId));
        return result;
    }
}
```

## Core Concepts

### Commands, Queries, and Events

The library distinguishes between three types of messages:

1. **Commands**: Requests that change state but don't return data (e.g., `CreateUserCommand`)
2. **Queries**: Requests that return data but don't change state (e.g., `GetUserQuery`)
3. **Events**: Notifications that something has happened (e.g., `UserCreatedEvent`)

### Message Bus

The message bus is the central component that routes messages to their appropriate handlers:

- `SendAsync<T>(object message)`: Sends a command or query and expects a result of type `T`
- `SendAsync(object message)`: Sends a command or query with an untyped result
- `PublishAsync(object eventMessage)`: Publishes an event to all subscribers

### Handlers and Subscribers

- **Handlers**: Process commands and queries (one handler per message type)
- **Subscribers**: Respond to events (multiple subscribers per event type)

## Sending Commands and Queries

### Defining Command and Query Classes

Commands and queries are simple DTOs (Data Transfer Objects):

```csharp
// Command example (modifies state, no return value)
public class CreateUserCommand
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

// Query example (returns data, doesn't modify state)
public class GetUserQuery
{
    public int UserId { get; }
    
    public GetUserQuery(int userId)
    {
        UserId = userId;
    }
}

// DTO for query result
public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
}
```

### Sending Commands

Commands typically don't return data, so you can use the non-generic `SendAsync` method:

```csharp
// Sending a command that doesn't return a value
await _messageBus.SendAsync(new CreateUserCommand 
{
    Username = "johndoe",
    Email = "john@example.com",
    Password = "securePassword123"
});
```

Or if your command returns a simple confirmation:

```csharp
// Sending a command that returns a confirmation
bool success = await _messageBus.SendAsync<bool>(new DeactivateUserCommand(userId));
if (success) {
    // User was deactivated
}
```

### Sending Queries

Queries always return data, so use the generic `SendAsync<T>` method:

```csharp
// Sending a query that returns a UserDto
var user = await _messageBus.SendAsync<UserDto>(new GetUserQuery(42));

// Sending a query that returns a collection
var activeUsers = await _messageBus.SendAsync<List<UserDto>>(new GetActiveUsersQuery());
```

### Handling Cancellation

All methods accept an optional `CancellationToken` parameter:

```csharp
// With cancellation token
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // 5-second timeout
try {
    var result = await _messageBus.SendAsync<UserDto>(
        new GetUserQuery(userId), 
        cts.Token
    );
}
catch (OperationCanceledException) {
    // Handle timeout or cancellation
}
```

## Publishing Events

### Defining Event Classes

Events are simple DTOs that represent something that has happened:

```csharp
public class UserRegisteredEvent
{
    public int UserId { get; }
    public string Username { get; }
    public DateTime RegisteredAt { get; }
    
    public UserRegisteredEvent(int userId, string username)
    {
        UserId = userId;
        Username = username;
        RegisteredAt = DateTime.UtcNow;
    }
}
```

### Publishing Events

Use the `PublishAsync` method to send events to all registered subscribers:

```csharp
// Publishing an event
await _messageBus.PublishAsync(new UserRegisteredEvent(
    userId: 42,
    username: "johndoe"
));
```

The publish operation completes when all subscribers have processed the event.

### Fire-and-Forget Events

If you don't want to wait for subscribers to complete processing:

```csharp
// Fire and forget
_ = _messageBus.PublishAsync(new LogEvent("User action performed"));
```

Note: This approach should be used carefully as exceptions won't be caught by the caller.

## Creating Handlers

You can create handlers in two ways: by implementing interfaces or by convention.

### Interface-Based Handlers

Implement the `IHandler<TMessage, TResult>` interface for queries and commands that return results:

```csharp
public class GetUserQueryHandler : IHandler<GetUserQuery, UserDto>
{
    private readonly IUserRepository _userRepository;
    
    public GetUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    public async Task<UserDto> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetUserByIdAsync(query.UserId, cancellationToken);
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email
        };
    }
}
```

For commands that don't return a value, implement `IHandler<TMessage>`:

```csharp
public class CreateUserCommandHandler : IHandler<CreateUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IMessageBus _messageBus;
    
    public CreateUserCommandHandler(IUserRepository userRepository, IMessageBus messageBus)
    {
        _userRepository = userRepository;
        _messageBus = messageBus;
    }
    
    public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Username = command.Username,
            Email = command.Email,
            PasswordHash = HashPassword(command.Password)
        };
        
        var userId = await _userRepository.CreateUserAsync(user, cancellationToken);
        
        // Publish an event after handling the command
        await _messageBus.PublishAsync(new UserRegisteredEvent(userId, command.Username), cancellationToken);
    }
    
    private string HashPassword(string password) => /* password hashing logic */;
}
```

### Convention-Based Handlers

You can also create handlers without implementing interfaces, using method naming conventions:

```csharp
public class UserHandlers
{
    private readonly IUserRepository _userRepository;
    
    public UserHandlers(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }
    
    // This method will handle GetUserQuery by convention
    public async Task<UserDto> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetUserByIdAsync(query.UserId, cancellationToken);
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email
        };
    }
    
    // This method will handle CreateUserCommand by convention
    public async Task Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Implementation...
    }
}
```

The library will discover these methods during registration.

## Creating Event Subscribers

Just like handlers, you can create event subscribers using interfaces or convention.

### Interface-Based Subscribers

Implement the `IEventSubscriber<TEvent>` interface:

```csharp
public class EmailNotificationSubscriber : IEventSubscriber<UserRegisteredEvent>
{
    private readonly IEmailService _emailService;
    
    public EmailNotificationSubscriber(IEmailService emailService)
    {
        _emailService = emailService;
    }
    
    public async Task Consume(UserRegisteredEvent eventMessage, CancellationToken cancellationToken)
    {
        await _emailService.SendWelcomeEmailAsync(
            eventMessage.UserId,
            eventMessage.Username,
            cancellationToken
        );
    }
}
```

### Convention-Based Subscribers

Use the `Consume` method naming convention:

```csharp
public class NotificationHandlers
{
    private readonly IEmailService _emailService;
    
    public NotificationHandlers(IEmailService emailService)
    {
        _emailService = emailService;
    }
    
    // This method will subscribe to UserRegisteredEvent by convention
    public async Task Consume(UserRegisteredEvent eventMessage, CancellationToken cancellationToken)
    {
        await _emailService.SendWelcomeEmailAsync(
            eventMessage.UserId,
            eventMessage.Username,
            cancellationToken
        );
    }
}
```

### Multiple Subscribers

Multiple subscribers can consume the same event:

```csharp
// First subscriber
public class EmailNotificationSubscriber : IEventSubscriber<UserRegisteredEvent>
{
    public async Task Consume(UserRegisteredEvent eventMessage, CancellationToken cancellationToken)
    {
        // Send welcome email
    }
}

// Second subscriber
public class UserAnalyticsSubscriber : IEventSubscriber<UserRegisteredEvent>
{
    public async Task Consume(UserRegisteredEvent eventMessage, CancellationToken cancellationToken)
    {
        // Track user registration in analytics system
    }
}

// Third subscriber
public class AuditingSubscriber : IEventSubscriber<UserRegisteredEvent>
{
    public async Task Consume(UserRegisteredEvent eventMessage, CancellationToken cancellationToken)
    {
        // Log audit trail of user registration
    }
}
```

All subscribers will be called when the event is published.

## Middleware Pipeline

The middleware pipeline allows you to intercept and process messages before they reach their handlers, enabling cross-cutting concerns like logging, validation, and error handling.

### Creating Middleware

Create a middleware by implementing the `IMessageMiddleware` interface:

```csharp
public class LoggingMiddleware : IMessageMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;
    
    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }
    
    public async Task<object?> InvokeAsync(
        object message, 
        CancellationToken cancellationToken, 
        MessageBus.MessageDelegate next)
    {
        var messageType = message.GetType().Name;
        _logger.LogInformation("Processing message of type {MessageType}", messageType);
        
        try
        {
            // Call the next middleware in the pipeline
            var result = await next(message, cancellationToken);
            
            _logger.LogInformation("Successfully processed message of type {MessageType}", messageType);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message of type {MessageType}", messageType);
            throw; // Re-throw to propagate the exception
        }
    }
}
```

### Registering Middleware

Register middleware during message bus setup:

```csharp
services.AddMessageBus(builder =>
{
    builder.UseMiddleware<LoggingMiddleware>();
    builder.UseMiddleware<ValidationMiddleware>();
    builder.UseMiddleware<TransactionMiddleware>();
    // Middleware is executed in registration order
});
```

## Registration and Dependency Injection

### Basic Registration

Basic registration scans all assemblies and registers handlers and subscribers:

```csharp
services.AddMessageBus();
```

### Custom Registration

You can customize registration with more control:

```csharp
services.AddMessageBus(builder =>
{
    // Register handlers from specific assemblies only
    var businessLogicAssembly = typeof(CreateUserCommandHandler).Assembly;
    builder.RegisterHandlers(businessLogicAssembly);
    
    // Add middleware
    builder.UseMiddleware<LoggingMiddleware>();
    builder.UseMiddleware<ValidationMiddleware>();
});
```

### Handler Lifetimes

By default, handlers and subscribers are registered as transient services (created each time they're needed). You can change this by customizing the registration:

```csharp
// Register a specific handler as scoped
services.AddScoped<GetUserQueryHandler>();

// Register all handlers from certain namespace as scoped
var handlersToRegisterAsScoped = typeof(Program).Assembly.GetTypes()
    .Where(t => t.Namespace == "MyApp.ReadModel.Handlers" && 
                !t.IsAbstract && !t.IsInterface);

foreach (var handlerType in handlersToRegisterAsScoped)
{
    services.AddScoped(handlerType);
}

// Then add the mediator
services.AddMessageBus();
```

## Best Practices

### Command and Query Design

1. **Command and Query Separation**: Keep commands and queries separate - commands change state, queries return data.
2. **Immutability**: Make commands and queries immutable when possible by using readonly properties.
3. **Naming Conventions**: Use verb-noun naming for commands (`CreateUserCommand`) and noun-verb for queries (`UserGetQuery`).

```csharp
// Good command design - immutable with clear intent
public class DeactivateUserCommand
{
    public int UserId { get; }
    public string DeactivationReason { get; }
    
    public DeactivateUserCommand(int userId, string deactivationReason)
    {
        UserId = userId;
        DeactivationReason = deactivationReason ?? "User requested deactivation";
    }
}
```

### Handler Design

1. **Single Responsibility**: Each handler should do one thing well.
2. **Domain Logic**: Keep domain logic in domain objects, not in handlers.
3. **Exception Handling**: Use custom exceptions to provide meaningful error messages.

```csharp
public class DeactivateUserCommandHandler : IHandler<DeactivateUserCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IMessageBus _messageBus;
    
    public DeactivateUserCommandHandler(IUserRepository userRepository, IMessageBus messageBus)
    {
        _userRepository = userRepository;
        _messageBus = messageBus;
    }
    
    public async Task<bool> Handle(DeactivateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new EntityNotFoundException($"User with ID {command.UserId} not found");
        
        // Domain logic in the entity
        bool deactivated = user.Deactivate(command.DeactivationReason);
        
        if (deactivated)
        {
            await _userRepository.SaveChangesAsync(cancellationToken);
            await _messageBus.PublishAsync(new UserDeactivatedEvent(user.Id, command.DeactivationReason), cancellationToken);
        }
        
        return deactivated;
    }
}
```

### Event Design

1. **Past Tense**: Use past tense for event names as they represent something that has happened.
2. **Include Relevant Data**: Include all data subscribers might need to process the event.
3. **Immutability**: Make events immutable.

```csharp
public class PaymentProcessedEvent
{
    public Guid PaymentId { get; }
    public int OrderId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public DateTime ProcessedAt { get; }
    public PaymentMethod PaymentMethod { get; }
    
    public PaymentProcessedEvent(
        Guid paymentId, 
        int orderId, 
        decimal amount, 
        string currency,
        PaymentMethod paymentMethod)
    {
        PaymentId = paymentId;
        OrderId = orderId;
        Amount = amount;
        Currency = currency;
        ProcessedAt = DateTime.UtcNow;
        PaymentMethod = paymentMethod;
    }
}
```

### Performance Considerations

1. **Avoid Heavy Operations in Events**: Event subscribers should be lightweight and fast.
2. **Use Asynchronous Processing**: Consider using background processing for long-running tasks.
3. **Batch Processing**: When dealing with large collections, process in batches.

```csharp
// Example of offloading heavy work to a background service
public class EmailNotificationSubscriber : IEventSubscriber<UserRegisteredEvent>
{
    private readonly IBackgroundJobService _jobService;
    
    public EmailNotificationSubscriber(IBackgroundJobService jobService)
    {
        _jobService = jobService;
    }
    
    public Task Consume(UserRegisteredEvent eventMessage, CancellationToken cancellationToken)
    {
        // Queue the email sending to background processing
        _jobService.Enqueue<IEmailService>(
            service => service.SendWelcomeEmailAsync(
                eventMessage.UserId, 
                eventMessage.Username,
                CancellationToken.None
            )
        );
        
        return Task.CompletedTask;
    }
}
```
