import type { DropdownMenuItem } from '@nuxt/ui'
import { useAuthStore } from '~/stores/auth'
import { useNotificationsStore } from '~/stores/notifications'

export interface NavItem {
  label: string
  icon: string
  /** Icon used when the item is the current page (filled version). */
  activeIcon: string
  /** Where the item goes. Items without `to` run `action` instead (e.g. open the Create dialog). */
  to?: string
  action?: () => void
  badge?: number
}

/**
 * The app's main navigation, shared by the desktop sidebar and the phone bottom bar,
 * so both always offer the same destinations.
 */
export function useNavigation() {
  const route = useRoute()
  const auth = useAuthStore()
  const notifications = useNotificationsStore()
  const createPost = useCreatePost()
  const colorMode = useColorMode()

  const profilePath = computed(() => `/u/${auth.user?.username ?? ''}`)

  const home: NavItem = { label: 'Home', icon: 'i-ph-house', activeIcon: 'i-ph-house-fill', to: '/' }
  const search: NavItem = { label: 'Search', icon: 'i-ph-magnifying-glass', activeIcon: 'i-ph-magnifying-glass-bold', to: '/search' }
  const explore: NavItem = { label: 'Explore', icon: 'i-ph-compass', activeIcon: 'i-ph-compass-fill', to: '/explore' }
  const create: NavItem = { label: 'Create', icon: 'i-ph-plus-square', activeIcon: 'i-ph-plus-square-fill', action: createPost.open }

  const notificationsItem = computed<NavItem>(() => ({
    label: 'Notifications',
    icon: 'i-ph-heart',
    activeIcon: 'i-ph-heart-fill',
    to: '/notifications',
    badge: notifications.unreadCount,
  }))

  /** Desktop sidebar (profile is rendered separately because it shows the avatar). */
  const sidebarItems = computed<NavItem[]>(() => [home, search, explore, notificationsItem.value, create])

  /** Phone bottom bar. Notifications live in the top bar on phones, which keeps the bottom bar to five items. */
  const bottomBarItems: NavItem[] = [home, search, create, explore]

  /** The "More" menu: less frequent destinations. */
  const moreMenuItems = computed<DropdownMenuItem[][]>(() => [
    [
      { label: 'Saved', icon: 'i-ph-bookmark-simple', to: '/saved' },
      { label: 'Settings', icon: 'i-ph-gear', to: '/settings' },
      {
        label: colorMode.value === 'dark' ? 'Light mode' : 'Dark mode',
        icon: colorMode.value === 'dark' ? 'i-ph-sun' : 'i-ph-moon',
        onSelect: () => { colorMode.preference = colorMode.value === 'dark' ? 'light' : 'dark' },
      },
    ],
    [{ label: 'Log out', icon: 'i-ph-sign-out', onSelect: logout }],
  ])

  function isActive(path: string | undefined) {
    if (!path) return false
    return path === '/' ? route.path === '/' : route.path.startsWith(path)
  }

  async function logout() {
    await auth.logout()
    await navigateTo('/login')
  }

  return { sidebarItems, bottomBarItems, moreMenuItems, profilePath, isActive }
}
