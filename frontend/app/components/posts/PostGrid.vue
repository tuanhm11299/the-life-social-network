<script setup lang="ts">
import type { Post } from '~/types/api'

/** A 3-column grid of square thumbnails (profile, explore, saved). Hovering shows likes and comments. */
defineProps<{ posts: Post[] }>()
</script>

<template>
  <div class="grid grid-cols-3 gap-0.5 sm:gap-1">
    <NuxtLink
      v-for="post in posts"
      :key="post.id"
      :to="`/p/${post.id}`"
      class="group relative block aspect-square overflow-hidden bg-muted"
      :aria-label="`Post by ${post.author.username}`"
    >
      <img :src="post.thumbnailUrl" alt="" class="size-full object-cover" loading="lazy">

      <UIcon
        v-if="post.imageUrls.length > 1"
        name="i-ph-copy-simple-fill"
        class="absolute end-2 top-2 size-5 text-white drop-shadow"
      />

      <div
        class="absolute inset-0 hidden items-center justify-center gap-6 bg-black/35 font-semibold text-white group-hover:flex"
      >
        <span class="flex items-center gap-1.5"><UIcon name="i-ph-heart-fill" class="size-5" /> {{ compactNumber(post.likeCount) }}</span>
        <span class="flex items-center gap-1.5"><UIcon name="i-ph-chat-circle-fill" class="size-5" /> {{ compactNumber(post.commentCount) }}</span>
      </div>
    </NuxtLink>
  </div>
</template>
