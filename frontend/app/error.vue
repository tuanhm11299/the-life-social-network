<script setup lang="ts">
import type { NuxtError } from '#app'

// Shown for unknown URLs (404) and unexpected crashes.
const props = defineProps<{ error: NuxtError }>()

const isNotFound = computed(() => props.error.statusCode === 404)
</script>

<template>
  <UApp>
    <div class="flex min-h-dvh flex-col items-center justify-center gap-4 bg-default px-6 text-center">
      <span class="font-logo text-4xl font-bold tracking-tight text-highlighted">The Life</span>
      <h1 class="text-xl font-semibold text-highlighted">
        {{ isNotFound ? "Sorry, this page isn't available." : 'Something went wrong.' }}
      </h1>
      <p class="max-w-sm text-sm text-muted">
        {{ isNotFound ? 'The link you followed may be broken, or the page may have been removed.' : error.message }}
      </p>
      <UButton label="Go back to The Life" @click="clearError({ redirect: '/' })" />
    </div>
  </UApp>
</template>
