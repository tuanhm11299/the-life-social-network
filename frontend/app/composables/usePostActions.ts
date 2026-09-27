import { postsApi } from '~/api/posts'
import type { Post } from '~/types/api'

/**
 * Like / unlike / save / unsave / share for one post, shared by <PostCard> and <PostDetail>.
 *
 * Updates are "optimistic": the heart turns red immediately, then the server's answer is applied.
 * If the request fails, the previous state is restored and an error toast is shown.
 *
 * `post` must be writable (a `defineModel` or a `ref`), because the new state is assigned to it.
 */
export function usePostActions(post: Ref<Post>) {
  const showError = useErrorToast()
  const toast = useToast()

  async function toggleLike() {
    const before = post.value
    const liking = !before.isLikedByMe
    post.value = { ...before, isLikedByMe: liking, likeCount: before.likeCount + (liking ? 1 : -1) }

    try {
      const status = liking ? await postsApi.like(before.id) : await postsApi.unlike(before.id)
      post.value = { ...post.value, isLikedByMe: status.isLikedByMe, likeCount: status.likeCount }
    }
    catch (error) {
      post.value = { ...post.value, isLikedByMe: before.isLikedByMe, likeCount: before.likeCount }
      showError(error)
    }
  }

  /** Double-tapping a photo only ever likes it (it never removes a like), so a second double-tap can't undo it by accident. */
  async function likeFromDoubleTap() {
    if (!post.value.isLikedByMe) await toggleLike()
  }

  async function toggleSave() {
    const saving = !post.value.isSavedByMe
    post.value = { ...post.value, isSavedByMe: saving }

    try {
      const status = saving ? await postsApi.save(post.value.id) : await postsApi.unsave(post.value.id)
      post.value = { ...post.value, isSavedByMe: status.isSavedByMe }
      if (saving) toast.add({ title: 'Saved to your collection', icon: 'i-ph-bookmark-simple-fill' })
    }
    catch (error) {
      post.value = { ...post.value, isSavedByMe: !saving }
      showError(error)
    }
  }

  /** Uses the phone's share sheet when available, otherwise copies the link. */
  async function share() {
    const url = `${window.location.origin}/p/${post.value.id}`

    if (navigator.share) {
      try {
        await navigator.share({ title: `Post by ${post.value.author.username}`, url })
      }
      catch {
        // The user closed the share sheet; nothing to do.
      }
      return
    }

    await navigator.clipboard.writeText(url)
    toast.add({ title: 'Link copied to clipboard', icon: 'i-lucide-link' })
  }

  return { toggleLike, likeFromDoubleTap, toggleSave, share }
}
