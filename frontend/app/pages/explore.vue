<script setup lang="ts">
import { postsApi } from '~/api/posts'

useHead({ title: 'Explore · The Life' })

// Popular posts from people you don't follow yet.
const { items: posts, loading, hasMore, error, loadMore } = useInfiniteList(cursor => postsApi.explore(cursor))
onMounted(loadMore)
</script>

<template>
  <div class="mx-auto max-w-5xl md:px-6 md:py-8">
    <h1 class="sr-only">Explore</h1>

    <PostGrid :posts="posts" />

    <UEmpty
      v-if="!loading && !hasMore && posts.length === 0"
      icon="i-ph-compass"
      title="Nothing to explore yet"
      description="You already follow everyone who posted. Check back later!"
      class="py-16"
    />

    <UAlert v-if="error" :title="getErrorMessage(error)" color="error" variant="subtle" class="m-4" />

    <InfiniteScrollTrigger :loading="loading" :has-more="hasMore && !error" @load="loadMore" />
  </div>
</template>
