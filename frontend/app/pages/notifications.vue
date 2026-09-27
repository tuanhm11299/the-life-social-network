<script setup lang="ts">
import { notificationsApi } from '~/api/notifications'
import { useNotificationsStore } from '~/stores/notifications'
import type { AppNotification } from '~/types/api'

useHead({ title: 'Notifications · The Life' })

const notificationsStore = useNotificationsStore()
const { items: notifications, loading, hasMore, loadMore } = useInfiniteList(cursor => notificationsApi.list(cursor))

onMounted(async () => {
  await loadMore()
  // Opening this page counts as reading everything. The list keeps its "unread" dots until the next visit.
  await notificationsStore.markAllRead().catch(() => {})
})

function describe(notification: AppNotification) {
  switch (notification.type) {
    case 'Like': return 'liked your post.'
    case 'Comment': return `commented: ${notification.commentText ?? ''}`
    case 'Follow': return 'started following you.'
  }
}
</script>

<template>
  <div class="mx-auto max-w-xl px-4 py-4 md:py-8">
    <h1 class="mb-4 text-xl font-semibold text-highlighted md:text-2xl">Notifications</h1>

    <ul>
      <li v-for="(notification, index) in notifications" :key="notification.id" class="flex items-center gap-3 py-2.5">
        <span class="size-2 shrink-0 rounded-full" :class="notification.isRead ? 'bg-transparent' : 'bg-primary'" />

        <NuxtLink :to="`/u/${notification.actor.username}`" class="shrink-0">
          <UserAvatar :user="notification.actor" size="lg" />
        </NuxtLink>

        <p class="min-w-0 flex-1 text-sm">
          <NuxtLink :to="`/u/${notification.actor.username}`" class="font-semibold text-highlighted">
            {{ notification.actor.username }}
          </NuxtLink>
          {{ describe(notification) }}
          <span class="text-muted"> {{ timeAgo(notification.createdAt) }}</span>
        </p>

        <NuxtLink v-if="notification.postId && notification.postImageUrl" :to="`/p/${notification.postId}`" class="shrink-0">
          <img :src="notification.postImageUrl" alt="Post" class="size-11 rounded object-cover" loading="lazy">
        </NuxtLink>

        <FollowButton
          v-else-if="notification.type === 'Follow'"
          v-model:following="notifications[index]!.isFollowingActor"
          :username="notification.actor.username"
          size="xs"
        />
      </li>
    </ul>

    <UEmpty
      v-if="!loading && !hasMore && notifications.length === 0"
      icon="i-ph-heart"
      title="No notifications yet"
      description="When people like or comment on your posts or follow you, you'll see it here."
      class="py-16"
    />

    <InfiniteScrollTrigger :loading="loading" :has-more="hasMore" @load="loadMore" />
  </div>
</template>
