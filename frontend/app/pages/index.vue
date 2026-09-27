<script setup lang="ts">
import { postsApi } from '~/api/posts'
import { useAuthStore } from '~/stores/auth'

useHead({ title: 'The Life' })

const auth = useAuthStore()
const createPost = useCreatePost()

const { items: posts, loading, hasMore, error, loadMore, removeWhere } = useInfiniteList(cursor => postsApi.feed(cursor))
onMounted(loadMore)
</script>

<template>
  <div class="mx-auto flex max-w-5xl justify-center gap-16 md:px-6 md:py-6">
    <!-- Feed column -->
    <div class="w-full max-w-[470px]">
      <StoryTray class="mb-2 md:mb-6" />

      <div class="space-y-4 sm:space-y-6">
        <PostCard
          v-for="(post, index) in posts"
          :key="post.id"
          v-model:post="posts[index]!"
          @deleted="removeWhere(p => p.id === post.id)"
        />
      </div>

      <UAlert
        v-if="error"
        title="Couldn't load your feed"
        :description="getErrorMessage(error)"
        color="error"
        variant="subtle"
        :actions="[{ label: 'Try again', onClick: loadMore }]"
        class="mx-3 my-4 sm:mx-0"
      />

      <!-- Nothing to show yet: invite the user to follow people or post something. -->
      <div v-if="!loading && !hasMore && posts.length === 0" class="px-4 py-10">
        <UEmpty
          icon="i-ph-camera"
          title="Welcome to The Life"
          description="Follow people to see their photos here, or share your first post."
          :actions="[{ label: 'Share a photo', icon: 'i-ph-plus-square', onClick: createPost.open }]"
        />
        <SuggestedUsers class="mt-8 lg:hidden" />
      </div>

      <InfiniteScrollTrigger :loading="loading" :has-more="hasMore && !error" @load="loadMore" />

      <p v-if="!hasMore && posts.length > 0" class="pb-10 text-center text-sm text-muted">
        <UIcon name="i-ph-check-circle" class="size-8" /><br>
        You're all caught up
      </p>
    </div>

    <!-- Right column on large screens -->
    <aside class="hidden w-80 shrink-0 pt-4 lg:block">
      <div v-if="auth.user" class="mb-6 flex items-center gap-3">
        <NuxtLink :to="`/u/${auth.user.username}`">
          <UserAvatar :user="auth.user" size="xl" />
        </NuxtLink>
        <div class="min-w-0">
          <NuxtLink :to="`/u/${auth.user.username}`" class="block truncate text-sm font-semibold text-highlighted">
            {{ auth.user.username }}
          </NuxtLink>
          <p class="truncate text-sm text-muted">{{ auth.user.displayName }}</p>
        </div>
      </div>
      <SuggestedUsers />
    </aside>
  </div>
</template>
