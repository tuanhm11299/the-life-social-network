using FluentValidation;
using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Files;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Application.Features.Auth;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Users;

/// <param name="Image">The new profile picture, or null to remove the current one.</param>
public sealed record UpdateAvatarCommand(FileUpload? Image) : ICommand<CurrentUserDto>;

public sealed class UpdateAvatarValidator : AbstractValidator<UpdateAvatarCommand>
{
    public UpdateAvatarValidator()
    {
        When(x => x.Image is not null, () => RuleFor(x => x.Image).MustBeAnImage());
    }
}

public sealed class UpdateAvatarHandler(IAppDbContext db, ICurrentUser currentUser, IFileStorage fileStorage)
    : ICommandHandler<UpdateAvatarCommand, CurrentUserDto>
{
    public async Task<Result<CurrentUserDto>> Handle(UpdateAvatarCommand command, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == currentUser.Id, cancellationToken);
        if (user is null) return AuthErrors.UserNotFound;

        var oldAvatarUrl = user.AvatarUrl;
        var newAvatarUrl = command.Image is null
            ? null
            : await fileStorage.SaveImageAsync(command.Image, "avatars", cancellationToken);

        user.ChangeAvatar(newAvatarUrl);
        await db.SaveChangesAsync(cancellationToken);

        // Delete the old file only after the database points to the new one.
        if (oldAvatarUrl is not null) await fileStorage.DeleteAsync(oldAvatarUrl, cancellationToken);

        return CurrentUserDto.From(user);
    }
}
