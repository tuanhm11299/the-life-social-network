<script setup lang="ts">
import { usersApi } from '~/api/users'
import type { FollowStatus, Profile } from '~/types/api'

// A user's profile: /u/<username>
const route = useRoute()
const username = computed(() => route.params.username as string)
const storyViewer = useStoryViewer()
const showError = useErrorToast()

const profile = ref<Profile | null>(null)
const profileLoading = ref(true)
const profileError = ref('')

const followersOpen = ref(false)
const followingOpen = ref(false)

const { items: posts, loading: postsLoading, hasMore, loadMore, reload: reloadPosts } =
  useInfiniteList(cursor => usersApi.posts(username.value, cursor))

async function loadProfile() {
  profileLoading.value = true
  profileError.value = ''
  try {
    profile.value = await usersApi.profile(username.value)
  }
  catch (error) {
    profile.value = null
    profileError.value = getErrorMessage(error, "This profile couldn't be loaded.")
  }
  finally {
    profileLoading.value = false
  }
}

// Load again when navigating from one profile to another (the page component is reused).
watch(username, () => {
  loadProfile()
  reloadPosts()
}, { immediate: true })

useHead(() => ({
  title: profile.value ? `${profile.value.displayName} (@${profile.value.username}) · The Life` : 'The Life',
}))

function onFollowChange(status: FollowStatus) {
  if (profile.value) profile.value.followerCount = status.followerCount
}

async function openStories() {
  if (!profile.value?.hasActiveStory) return
  try {
    const stories = await usersApi.stories(profile.value.username)
    if (stories.stories.length > 0) storyViewer.open([stories])
  }
  catch (error) {
    showError(error)
  }
}
</script>

<template>
  <div class="mx-auto max-w-4xl md:px-6 md:py-8">
    <!-- Loading -->
    <div v-if="profileLoading && !profile" class="flex items-center gap-6 p-4 md:gap-16 md:p-8">
      <USkeleton class="size-20 rounded-full md:size-36" />
      <div class="flex-1 space-y-3">
        <USkeleton class="h-6 w-40" />
        <USkeleton class="h-4 w-60" />
        <USkeleton class="h-4 w-32" />
      </div>
    </div>

    <!-- Not found -->
    <UEmpty
      v-else-if="!profile"
      icon="i-ph-user-circle-dashed"
      title="Sorry, this page isn't available"
      :description="profileError"
      :actions="[{ label: 'Go home', to: '/' }]"
      class="py-20"
    />

    <template v-else>
      <header class="px-4 pb-4 pt-4 md:flex md:gap-16 md:px-0 md:pb-10 md:pt-0">
        <div class="flex items-center gap-6 md:block md:ps-10">
          <button
            type="button"
            :disabled="!profile.hasActiveStory"
            :aria-label="profile.hasActiveStory ? `View ${profile.username}'s story` : undefined"
            class="shrink-0 disabled:cursor-default"
            @click="openStories"
          >
            <UserAvatar
              :user="profile"
              size="3xl"
              avatar-class="size-20 text-2xl md:size-36 md:text-5xl"
              :ring="profile.hasActiveStory ? 'unseen' : 'none'"
            />
          </button>

          <!-- Phone: username next to the avatar -->
          <div class="min-w-0 md:hidden">
            <h1 class="truncate text-xl text-highlighted">{{ profile.username }}</h1>
          </div>
        </div>

        <div class="mt-4 min-w-0 flex-1 md:mt-2">
          <div class="flex flex-wrap items-center gap-3">
            <h1 class="hidden truncate text-xl text-highlighted md:block">{{ profile.username }}</h1>
            <div class="flex w-full gap-2 md:w-auto">
              <template v-if="profile.isMe">
                <UButton to="/settings" label="Edit profile" color="neutral" variant="soft" class="flex-1 justify-center md:flex-none" />
                <UButton to="/saved" icon="i-ph-bookmark-simple" color="neutral" variant="soft" aria-label="Saved posts" />
              </template>
              <FollowButton
                v-else
                v-model:following="profile.isFollowedByMe"
                :username="profile.username"
                size="md"
                class="flex-1 md:flex-none md:px-6"
                @change="onFollowChange"
              />
            </div>
          </div>

          <!-- Counters (desktop). Phone shows them in a bar below. -->
          <div class="mt-5 hidden gap-10 md:flex">
            <span><b class="text-highlighted">{{ compactNumber(profile.postCount) }}</b> posts</span>
            <button type="button" @click="followersOpen = true">
              <b class="text-highlighted">{{ compactNumber(profile.followerCount) }}</b> followers
            </button>
            <button type="button" @click="followingOpen = true">
              <b class="text-highlighted">{{ compactNumber(profile.followingCount) }}</b> following
            </button>
          </div>

          <div class="mt-4 text-sm">
            <p class="font-semibold text-highlighted">{{ profile.displayName }}</p>
            <p v-if="profile.bio" class="whitespace-pre-line">{{ profile.bio }}</p>
          </div>
        </div>
      </header>

      <!-- Counters (phone) -->
      <div class="grid grid-cols-3 border-y border-default py-3 text-center text-sm md:hidden">
        <div><b class="block text-highlighted">{{ compactNumber(profile.postCount) }}</b><span class="text-muted">posts</span></div>
        <button type="button" @click="followersOpen = true">
          <b class="block text-highlighted">{{ compactNumber(profile.followerCount) }}</b><span class="text-muted">followers</span>
        </button>
        <button type="button" @click="followingOpen = true">
          <b class="block text-highlighted">{{ compactNumber(profile.followingCount) }}</b><span class="text-muted">following</span>
        </button>
      </div>

      <div class="flex justify-center border-default md:border-t">
        <span class="-mt-px flex items-center gap-1.5 border-highlighted py-3 text-xs font-semibold uppercase tracking-widest text-highlighted md:border-t">
          <UIcon name="i-ph-grid-four" class="size-4" /> Posts
        </span>
      </div>

      <PostGrid :posts="posts" />

      <UEmpty
        v-if="!postsLoading && !hasMore && posts.length === 0"
        icon="i-ph-camera"
        :title="profile.isMe ? 'Share your first photo' : 'No posts yet'"
        :description="profile.isMe ? 'When you share photos, they will appear on your profile.' : undefined"
        :actions="profile.isMe ? [{ label: 'Create a post', onClick: useCreatePost().open }] : undefined"
        class="py-16"
      />

      <InfiniteScrollTrigger :loading="postsLoading" :has-more="hasMore" @load="loadMore" />

      <UserListModal
        v-model:open="followersOpen"
        title="Followers"
        :fetch-page="cursor => usersApi.followers(profile!.username, cursor)"
      />
      <UserListModal
        v-model:open="followingOpen"
        title="Following"
        :fetch-page="cursor => usersApi.following(profile!.username, cursor)"
      />
    </template>
  </div>
</template>
