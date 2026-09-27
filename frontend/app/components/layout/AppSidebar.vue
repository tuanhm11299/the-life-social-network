<script setup lang="ts">
import { useAuthStore } from '~/stores/auth'
import type { NavItem } from '~/composables/useNavigation'

/**
 * Left navigation on tablets and desktops (hidden on phones).
 * Shows icons only on medium screens and icons + labels on large screens (xl).
 */
const auth = useAuthStore()
const { sidebarItems, moreMenuItems, profilePath, isActive } = useNavigation()

// Items are links or buttons; <component :is> needs the resolved NuxtLink component, not its name.
const NuxtLink = resolveComponent('NuxtLink')

const itemClass = 'relative flex items-center gap-4 rounded-lg p-3 text-highlighted transition-colors hover:bg-elevated'

function iconFor(item: NavItem) {
  return isActive(item.to) ? item.activeIcon : item.icon
}
</script>

<template>
  <aside
    class="fixed inset-y-0 start-0 z-30 hidden w-[72px] flex-col border-e border-default bg-default px-3 pb-5 pt-7 md:flex xl:w-60"
  >
    <div class="mb-6 flex h-10 items-center px-3">
      <AppLogo class="hidden xl:block" />
      <NuxtLink to="/" class="xl:hidden" aria-label="The Life home">
        <AppMark />
      </NuxtLink>
    </div>

    <nav class="flex flex-1 flex-col gap-1" aria-label="Main">
      <template v-for="item in sidebarItems" :key="item.label">
        <component
          :is="item.to ? NuxtLink : 'button'"
          :to="item.to"
          :type="item.to ? undefined : 'button'"
          :class="[itemClass, { 'font-bold': isActive(item.to) }]"
          :aria-label="item.label"
          @click="item.action?.()"
        >
          <UChip :show="!!item.badge" :text="item.badge" size="3xl" color="error">
            <UIcon :name="iconFor(item)" class="size-7" />
          </UChip>
          <span class="hidden xl:inline">{{ item.label }}</span>
        </component>
      </template>

      <NuxtLink
        v-if="auth.user"
        :to="profilePath"
        :class="[itemClass, { 'font-bold': isActive(profilePath) }]"
        aria-label="Profile"
      >
        <UserAvatar :user="auth.user" size="xs" :class="{ 'ring-2 ring-(--ui-text-highlighted) rounded-full': isActive(profilePath) }" />
        <span class="hidden xl:inline">Profile</span>
      </NuxtLink>
    </nav>

    <UDropdownMenu :items="moreMenuItems" :content="{ side: 'top', align: 'start' }">
      <button type="button" :class="itemClass" aria-label="More">
        <UIcon name="i-ph-list" class="size-7" />
        <span class="hidden xl:inline">More</span>
      </button>
    </UDropdownMenu>
  </aside>
</template>
