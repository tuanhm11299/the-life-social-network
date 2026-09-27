<script setup lang="ts">
import { postsApi } from '~/api/posts'
import type { Post } from '~/types/api'

// A single post with its comments: /p/<post id>
const route = useRoute()
const router = useRouter()
const postId = computed(() => route.params.id as string)

const post = ref<Post | null>(null)
const loading = ref(true)
const errorMessage = ref('')

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    post.value = await postsApi.get(postId.value)
  }
  catch (error) {
    post.value = null
    errorMessage.value = getErrorMessage(error, "This post couldn't be loaded.")
  }
  finally {
    loading.value = false
  }
}

watch(postId, load, { immediate: true })

useHead(() => ({
  title: post.value ? `${post.value.author.displayName} on The Life` : 'Post · The Life',
}))

function onDeleted() {
  if (window.history.length > 1) router.back()
  else navigateTo('/')
}
</script>

<template>
  <div class="mx-auto max-w-5xl md:px-6 md:py-8">
    <div class="flex items-center gap-2 border-b border-default px-2 py-1 md:hidden">
      <UButton icon="i-lucide-chevron-left" color="neutral" variant="ghost" aria-label="Back" @click="router.back()" />
      <span class="font-semibold text-highlighted">Post</span>
    </div>

    <div v-if="loading" class="grid gap-4 lg:grid-cols-[minmax(0,1fr)_380px]">
      <USkeleton class="aspect-square w-full" />
      <div class="hidden space-y-3 p-4 lg:block">
        <USkeleton class="h-8 w-40" />
        <USkeleton class="h-4 w-full" />
        <USkeleton class="h-4 w-2/3" />
      </div>
    </div>

    <UEmpty
      v-else-if="!post"
      icon="i-ph-image-broken"
      title="Sorry, this page isn't available"
      :description="errorMessage"
      :actions="[{ label: 'Go home', to: '/' }]"
      class="py-20"
    />

    <PostDetail v-else v-model:post="post" @deleted="onDeleted" />
  </div>
</template>
