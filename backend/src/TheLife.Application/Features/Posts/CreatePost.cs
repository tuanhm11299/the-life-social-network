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
        var savedUrls = new List<string>();
        var images = new List<NewPostImage>();

        try
        {
            // Every photo is stored twice: full size for the feed and a small square for grids.
            // (The upload is decoded once per size: simpler than sharing one decoded image, and fast enough.)
            foreach (var upload in command.Images)
            {
                var url = await fileStorage.SaveImageAsync(upload, "posts", ImageSize.Photo, cancellationToken);
                savedUrls.Add(url);
                var thumbnailUrl = await fileStorage.SaveImageAsync(upload, "posts/thumbnails", ImageSize.Thumbnail, cancellationToken);
                savedUrls.Add(thumbnailUrl);

                images.Add(new NewPostImage(url, thumbnailUrl));
            }

            var post = Post.Create(me, command.Caption, images, clock.GetUtcNow().UtcDateTime);
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
            foreach (var url in savedUrls) await fileStorage.DeleteAsync(url, CancellationToken.None);
            throw;
        }
    }
}
