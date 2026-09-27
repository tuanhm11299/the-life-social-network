using FluentValidation;
using TheLife.Application.Common.Results;

namespace TheLife.Application.Common.Messaging;

/// <summary>
/// Runs every FluentValidation validator registered for the command before calling the real handler.
/// If anything is invalid the handler is skipped and a validation <see cref="Error"/> is returned.
/// </summary>
internal sealed class ValidatingCommandHandler<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner,
    IEnumerable<IValidator<TCommand>> validators)
    : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
    {
        var error = await RequestValidation.ValidateAsync(command, validators, cancellationToken);
        return error is null ? await inner.Handle(command, cancellationToken) : error;
    }
}

/// <summary>Same as <see cref="ValidatingCommandHandler{TCommand,TResponse}"/> but for queries.</summary>
internal sealed class ValidatingQueryHandler<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> inner,
    IEnumerable<IValidator<TQuery>> validators)
    : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
    {
        var error = await RequestValidation.ValidateAsync(query, validators, cancellationToken);
        return error is null ? await inner.Handle(query, cancellationToken) : error;
    }
}

internal static class RequestValidation
{
    /// <summary>Returns null when the request is valid, otherwise an error listing every invalid field.</summary>
    public static async Task<Error?> ValidateAsync<TRequest>(
        TRequest request, IEnumerable<IValidator<TRequest>> validators, CancellationToken cancellationToken)
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count == 0) return null;

        var fieldErrors = failures
            .GroupBy(failure => ToCamelCase(failure.PropertyName))
            .ToDictionary(group => group.Key, group => group.Select(f => f.ErrorMessage).Distinct().ToArray());

        return Error.FromFieldErrors(fieldErrors);
    }

    // "Caption" → "caption", so field names match the JSON the frontend sends.
    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
