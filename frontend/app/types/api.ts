// TypeScript shapes of the JSON returned by the .NET API.
// They mirror the C# DTO records in backend/src/TheLife.Application/Features/**.
// Dates are ISO-8601 strings in UTC (e.g. "2026-09-23T04:58:34.66Z").

export interface UserSummary {
  id: string
  username: string
  displayName: string
  avatarUrl: string | null
}

/** A row in a list of people (followers, search results, likes...). */
export interface UserListItem extends UserSummary {
  isFollowedByMe: boolean
}

/** The signed-in user's own account. */
export interface CurrentUser extends UserSummary {
  email: string
  bio: string
}

export interface Profile {
  id: string
  username: string
  displayName: string
  bio: string
  avatarUrl: string | null
  postCount: number
  followerCount: number
  followingCount: number
  isMe: boolean
  isFollowedByMe: boolean
  hasActiveStory: boolean
  joinedAt: string
}

export interface AuthResponse {
  accessToken: string
  accessTokenExpiresAt: string
  user: CurrentUser
}

export interface Post {
  id: string
  author: UserSummary
  caption: string
  imageUrls: string[]
  createdAt: string
  likeCount: number
  commentCount: number
  isLikedByMe: boolean
  isSavedByMe: boolean
}

export interface PostComment {
  id: string
  postId: string
  author: UserSummary
  text: string
  createdAt: string
  /** True when I wrote the comment or own the post. */
  canDelete: boolean
}

/**
 * One page of an infinite list. Pass `nextCursor` back to get the next page;
 * `null` means there is nothing more to load.
 */
export interface CursorPage<T> {
  items: T[]
  nextCursor: string | null
}

export interface LikeStatus {
  isLikedByMe: boolean
  likeCount: number
}

export interface SaveStatus {
  isSavedByMe: boolean
}

export interface FollowStatus {
  isFollowedByMe: boolean
  followerCount: number
}

export interface Story {
  id: string
  imageUrl: string
  createdAt: string
  expiresAt: string
  isViewedByMe: boolean
}

/** One circle in the story bar: a person and their active stories (oldest first). */
export interface StoryTrayItem {
  user: UserSummary
  stories: Story[]
  hasUnseen: boolean
}

export type NotificationType = 'Like' | 'Comment' | 'Follow'

export interface AppNotification {
  id: string
  type: NotificationType
  actor: UserSummary
  postId: string | null
  postImageUrl: string | null
  commentText: string | null
  createdAt: string
  isRead: boolean
  isFollowingActor: boolean
}

/** Error body returned by the API (RFC 7807 "problem details"). */
export interface ProblemDetails {
  status?: number
  title?: string
  detail?: string
  code?: string
  /** Field name → error messages, for validation errors. */
  errors?: Record<string, string[]>
}
