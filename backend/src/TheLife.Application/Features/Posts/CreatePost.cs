using FluentValidation;
using TheLife.Application.Common.Abstractions;
using TheLife.Application.Common.Files;
using TheLife.Application.Common.Messaging;
using TheLife.Application.Common.Results;
using TheLife.Domain.Posts;
using Microsoft.EntityFrameworkCore;

namespace TheLife.Application.Features.Posts;

public sealed record CreatePostCommand(string? Caption, IReadOnlyList<FileUpload> Images) : ICommand<PostDto>;

public sealed class CreatePostValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostValidator()
    {
        RuleFor(x => x.Caption).MaximumLength(Post.CaptionMaxLength);
        RuleFor(x => x.Images)
            .NotEmpty().WithMessage("Add at least one photo.")
            .Must(images => images.Count <= Post.MaxImages).WithMessage($"You can add up to {Post.MaxImages} photos.");
        RuleForEach(x => x.Images).MustBeAnImage();
    }
}

public sealed class CreatePostHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IFileStorage fileStorage,
    TimeProvider clock)
    : ICommandHandler<CreatePostCommand, PostDto>
{
    public async Task<Result<PostDto>> Handle(CreatePostCommand command, CancellationToken cancellationToken)
    {
        var me = currentUser.Id;
        var imageUrls = new List<string>();

        try
        {
            foreach (var image in command.Images)
            {
                imageUrls.Add(await fileStorage.SaveImageAsync(image, "posts", cancellationToken));
            }

            var post = Post.Create(me, command.Caption, imageUrls, clock.GetUtcNow().UtcDateTime);
            db.Posts.Add(post);
            await db.SaveChangesAsync(cancellationToken);

            return await db.Posts
                .AsNoTracking()
                .Where(p => p.Id == post.Id)
                .Select(PostProjections.ToPostDto(me))
                .SingleAsync(cancellationToken);
        }
        catch
        {
            // Do not leave orphaned files behind when something fails halfway.
            foreach (var url in imageUrls) await fileStorage.DeleteAsync(url, CancellationToken.None);
            throw;
        }
    }
}
