using FluentValidation;
using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Files;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Domain.Stories;

namespace TheLife.Application.Features.Stories;

public sealed record CreateStoryCommand(FileUpload? Image) : ICommand<StoryDto>;

public sealed class CreateStoryValidator : AbstractValidator<CreateStoryCommand>
{
    public CreateStoryValidator()
    {
        RuleFor(x => x.Image).MustBeAnImage();
    }
}

/// <summary>Posts a story that is visible to followers for 24 hours.</summary>
public sealed class CreateStoryHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IFileStorage fileStorage,
    TimeProvider clock)
    : ICommandHandler<CreateStoryCommand, StoryDto>
{
    public async Task<Result<StoryDto>> Handle(CreateStoryCommand command, CancellationToken cancellationToken)
    {
        var imageUrl = await fileStorage.SaveImageAsync(command.Image!, "stories", cancellationToken);

        var story = Story.Create(currentUser.Id, imageUrl, clock.GetUtcNow().UtcDateTime);
        db.Stories.Add(story);
        await db.SaveChangesAsync(cancellationToken);

        return new StoryDto(story.Id, story.ImageUrl, story.CreatedAt, story.ExpiresAt, IsViewedByMe: true);
    }
}
