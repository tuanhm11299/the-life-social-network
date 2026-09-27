<script setup lang="ts">
import { usersApi } from '~/api/users'
import type { UserListItem } from '~/types/api'

useHead({ title: 'Search · The Life' })

const term = ref('')
const results = ref<UserListItem[]>([])
const searching = ref(false)
const showError = useErrorToast()

// Wait until the user stops typing for a moment before searching (a "debounce").
const DEBOUNCE_MS = 300
let debounceTimer: ReturnType<typeof setTimeout> | undefined
let latestSearch = 0

watch(term, (value) => {
  clearTimeout(debounceTimer)
  const trimmed = value.trim()

  if (!trimmed) {
    results.value = []
    searching.value = false
    return
  }

  searching.value = true
  debounceTimer = setTimeout(() => search(trimmed), DEBOUNCE_MS)
})

async function search(query: string) {
  const searchId = ++latestSearch
  try {
    const users = await usersApi.search(query)
    if (searchId === latestSearch) results.value = users // ignore answers to older searches
  }
  catch (error) {
    showError(error)
  }
  finally {
    if (searchId === latestSearch) searching.value = false
  }
}

onBeforeUnmount(() => clearTimeout(debounceTimer))
</script>

<template>
  <div class="mx-auto max-w-xl px-4 py-4 md:py-8">
    <h1 class="mb-4 hidden text-2xl font-semibold text-highlighted md:block">Search</h1>

    <UInput
      v-model="term"
      icon="i-lucide-search"
      placeholder="Search people"
      size="lg"
      autofocus
      :loading="searching"
      class="w-full"
      aria-label="Search people"
    />

    <div class="mt-4">
      <template v-if="term.trim()">
        <UserRow v-for="(user, index) in results" :key="user.id" v-model:user="results[index]!" />
        <p v-if="!searching && results.length === 0" class="py-10 text-center text-sm text-muted">
          No people found for "{{ term.trim() }}".
        </p>
      </template>

      <SuggestedUsers v-else :limit="15" />
    </div>
  </div>
</template>
