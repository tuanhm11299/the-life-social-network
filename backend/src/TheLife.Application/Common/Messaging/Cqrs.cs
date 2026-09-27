using TheLife.Application.Common.Results;

namespace TheLife.Application.Common.Messaging;

// CQRS in one file.
//
//  * A COMMAND changes data (create a post, like a post, follow a user...).
//  * A QUERY only reads data and never changes anything.
//
// Every use case ("feature slice") is one file under Features/ that contains:
//   1. the command or query record  (the input),
//   2. an optional FluentValidation validator,
//   3. the handler                    (the logic).
//
// Handlers are discovered automatically (see DependencyInjection.cs) and wrapped in
// a validation decorator, so a handler only runs when its input is valid.
// API endpoints ask for a handler by its interface, e.g. ICommandHandler<CreatePostCommand, PostDto>.

/// <summary>Marker for a request that changes state and returns <typeparamref name="TResponse"/>.</summary>
public interface ICommand<TResponse>;

/// <summary>Marker for a read-only request that returns <typeparamref name="TResponse"/>.</summary>
public interface IQuery<TResponse>;

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
