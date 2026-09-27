<script setup lang="ts">
import { storiesApi } from '~/api/stories'
import { useAuthStore } from '~/stores/auth'
import type { StoryTrayItem } from '~/types/api'

/**
 * Full-screen story player (opened with useStoryViewer().open(...); lives once in layouts/default.vue).
 *
 *  - Each story shows for 5 seconds, with progress bars at the top.
 *  - Tap the left third to go back, anywhere else to go forward. Press and hold to pause.
 *  - After a person's last story it continues with the next person, then closes.
 */
const STORY_DURATION_MS = 5000
const HOLD_THRESHOLD_MS = 250

const auth = useAuthStore()
const { state, close } = useStoryViewer()
const showError = useErrorToast()

const groupIndex = ref(0)
const storyIndex = ref(0)
const progress = ref(0) // 0 → 1 for the story on screen
const paused = ref(false)
const imageLoaded = ref(false)
const viewersOpen = ref(false)

const group = computed<StoryTrayItem | undefined>(() => state.value.groups[groupIndex.value])
const story = computed(() => group.value?.stories[storyIndex.value])
const isMine = computed(() => group.value?.user.id === auth.user?.id)

const isOpen = computed({
  get: () => state.value.open,
  set: (value) => {
    if (!value) close()
  },
})

// ---- Navigation ------------------------------------------------------------------------------

/** Start with the first story not seen yet, so people don't rewatch stories they already saw. */
function firstUnseenIndex(item: StoryTrayItem | undefined) {
  const index = item?.stories.findIndex(s => !s.isViewedByMe) ?? -1
  return Math.max(0, index)
}

function showCurrentStory() {
  progress.value = 0
  imageLoaded.value = false
  markCurrentAsViewed()
}

function next() {
  if (!group.value) return close()

  if (storyIndex.value < group.value.stories.length - 1) {
    storyIndex.value++
  }
  else if (groupIndex.value < state.value.groups.length - 1) {
    groupIndex.value++
    storyIndex.value = firstUnseenIndex(group.value)
  }
  else {
    return close()
  }
  showCurrentStory()
}

function previous() {
  if (storyIndex.value > 0) {
    storyIndex.value--
  }
  else if (groupIndex.value > 0) {
    groupIndex.value--
    storyIndex.value = (group.value?.stories.length ?? 1) - 1
  }
  showCurrentStory()
}

function markCurrentAsViewed() {
  const current = story.value
  if (!current || isMine.value || current.isViewedByMe) return

  current.isViewedByMe = true
  storiesApi.markViewed(current.id).catch(() => {
    // Not worth interrupting the story for; it will simply show as unseen next time.
  })
}

// ---- Timer -----------------------------------------------------------------------------------

let animationFrame = 0
let lastFrameTime = 0

function tick(time: number) {
  const isRunning = !paused.value && imageLoaded.value && !viewersOpen.value
  if (lastFrameTime && isRunning) {
    progress.value += (time - lastFrameTime) / STORY_DURATION_MS
    if (progress.value >= 1) next()
  }
  lastFrameTime = time
  animationFrame = requestAnimationFrame(tick)
}

function startTimer() {
  cancelAnimationFrame(animationFrame)
  lastFrameTime = 0
  animationFrame = requestAnimationFrame(tick)
}

watch(() => state.value.open, (open) => {
  if (open) {
    groupIndex.value = state.value.startGroupIndex
    storyIndex.value = firstUnseenIndex(group.value)
    paused.value = false
    showCurrentStory()
    startTimer()
  }
  else {
    cancelAnimationFrame(animationFrame)
  }
})

onBeforeUnmount(() => cancelAnimationFrame(animationFrame))

// ---- Touch / mouse / keyboard ----------------------------------------------------------------

let pressStartedAt = 0

function onPressStart() {
  pressStartedAt = Date.now()
  paused.value = true
}

function onPressEnd(event: PointerEvent) {
  paused.value = false
  const wasTap = Date.now() - pressStartedAt < HOLD_THRESHOLD_MS
  if (!wasTap) return

  const bounds = (event.currentTarget as HTMLElement).getBoundingClientRect()
  const tappedLeftThird = event.clientX - bounds.left < bounds.width / 3
  if (tappedLeftThird) previous()
  else next()
}

function onKeydown(event: KeyboardEvent) {
  if (!state.value.open) return
  if (event.key === 'ArrowRight') next()
  if (event.key === 'ArrowLeft') previous()
}

onMounted(() => window.addEventListener('keydown', onKeydown))
onBeforeUnmount(() => window.removeEventListener('keydown', onKeydown))

// ---- Own stories -----------------------------------------------------------------------------

async function deleteCurrentStory() {
  const current = story.value
  const currentGroup = group.value
  if (!current || !currentGroup) return

  try {
    await storiesApi.remove(current.id)
    currentGroup.stories.splice(storyIndex.value, 1)

    if (currentGroup.stories.length === 0) return close()
    storyIndex.value = Math.min(storyIndex.value, currentGroup.stories.length - 1)
    showCurrentStory()
  }
  catch (error) {
    showError(error)
  }
}

async function fetchViewers() {
  return { items: await storiesApi.viewers(story.value!.id), nextCursor: null }
}

/** Width of the filled part of each progress bar. */
function barFill(index: number) {
  if (index < storyIndex.value) return '100%'
  if (index > storyIndex.value) return '0%'
  return `${Math.min(progress.value, 1) * 100}%`
}
</script>

<template>
  <UModal v-model:open="isOpen" fullscreen :close="false" :ui="{ content: 'bg-black' }" title="Stories">
    <template #content>
      <div v-if="group && story" class="relative flex h-full items-center justify-center">
        <!-- Desktop: arrows beside the story -->
        <UButton
          icon="i-lucide-chevron-left"
          color="neutral"
          variant="soft"
          class="absolute start-4 hidden rounded-full md:flex"
          aria-label="Previous story"
          @click="previous"
        />
        <UButton
          icon="i-lucide-chevron-right"
          color="neutral"
          variant="soft"
          class="absolute end-4 hidden rounded-full md:flex"
          aria-label="Next story"
          @click="next"
        />

        <!-- The story itself, in a 9:16 frame -->
        <div class="relative aspect-[9/16] h-full max-h-dvh max-w-full overflow-hidden bg-neutral-900 md:h-[92vh] md:rounded-xl">
          <img
            :key="story.id"
            :src="story.imageUrl"
            :alt="`Story by ${group.user.username}`"
            class="size-full object-contain select-none"
            draggable="false"
            @load="imageLoaded = true"
            @error="imageLoaded = true"
          >

          <!-- Tap / hold area -->
          <div
            class="absolute inset-0 touch-none"
            @pointerdown="onPressStart"
            @pointerup="onPressEnd"
            @pointerleave="paused = false"
            @contextmenu.prevent
          />

          <!-- Progress bars + header -->
          <div class="pointer-events-none absolute inset-x-0 top-0 bg-gradient-to-b from-black/60 to-transparent p-3 pt-[max(0.75rem,env(safe-area-inset-top))]">
            <div class="flex gap-1">
              <div v-for="(s, index) in group.stories" :key="s.id" class="h-0.5 flex-1 overflow-hidden rounded bg-white/35">
                <div class="h-full bg-white" :style="{ width: barFill(index) }" />
              </div>
            </div>

            <div class="pointer-events-auto mt-3 flex items-center gap-2 text-white">
              <NuxtLink :to="`/u/${group.user.username}`" class="flex items-center gap-2" @click="close">
                <UserAvatar :user="group.user" size="sm" />
                <span class="text-sm font-semibold">{{ group.user.username }}</span>
              </NuxtLink>
              <span class="text-sm text-white/70">{{ timeAgo(story.createdAt) }}</span>

              <UButton
                icon="i-lucide-x"
                color="neutral"
                variant="link"
                class="ms-auto text-white"
                aria-label="Close stories"
                @click="close"
              />
            </div>
          </div>

          <!-- Own story: who saw it, delete -->
          <div
            v-if="isMine"
            class="absolute inset-x-0 bottom-0 flex items-center justify-between bg-gradient-to-t from-black/60 to-transparent p-3 pb-[max(0.75rem,env(safe-area-inset-bottom))]"
          >
            <UButton icon="i-lucide-eye" label="Seen by" color="neutral" variant="link" class="text-white" @click="viewersOpen = true" />
            <UButton icon="i-lucide-trash-2" color="neutral" variant="link" class="text-white" aria-label="Delete story" @click="deleteCurrentStory" />
          </div>
        </div>
      </div>

      <UserListModal
        v-if="isMine && story"
        v-model:open="viewersOpen"
        title="Seen by"
        :fetch-page="fetchViewers"
        empty-text="No one has seen this story yet."
      />
    </template>
  </UModal>
</template>
