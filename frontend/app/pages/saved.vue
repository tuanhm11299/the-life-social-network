<script setup lang="ts">
import { postsApi } from '~/api/posts'

useHead({ title: 'Saved · The Life' })

const { items: posts, loading, hasMore, loadMore } = useInfiniteList(cursor => postsApi.saved(cursor))
onMounted(loadMore)
</script>

<template>
  <div class="mx-auto max-w-5xl md:px-6 md:py-8">
    <div class="flex items-center gap-2 px-4 py-4 md:px-0">
      <UIcon name="i-ph-bookmark-simple" class="size-6" />
      <h1 class="text-xl font-semibold text-highlighted">Saved</h1>
      <span class="ms-2 text-sm text-muted">Only you can see what you've saved</span>
    </div>

    <PostGrid :posts="posts" />

    <UEmpty
      v-if="!loading && !hasMore && posts.length === 0"
      icon="i-ph-bookmark-simple"
      title="Save photos you want to see again"
      description="Tap the bookmark under any post to save it here."
      class="py-16"
    />

    <InfiniteScrollTrigger :loading="loading" :has-more="hasMore" @load="loadMore" />
  </div>
</template>
