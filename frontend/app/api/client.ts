import { useAuthStore } from '~/stores/auth'
import { getErrorStatus } from '~/utils/errors'

export interface ApiRequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  /** A plain object is sent as JSON; FormData is sent as multipart (file uploads). */
  body?: Record<string, unknown> | FormData
  query?: Record<string, string | number | null | undefined>
}

/**
 * Calls The Life API as the signed-in user.
 *
 *  1. Adds the "Authorization: Bearer <access token>" header.
 *  2. If the API answers 401 (the access token expired), asks for a new one
 *     using the refresh-token cookie and retries the request once.
 *  3. If that fails too, the user is signed out and sent to the login page.
 *
 * Errors are thrown as ofetch's FetchError; use getErrorMessage() from utils/errors.ts to show them.
 */
export async function apiRequest<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
  const auth = useAuthStore()

  const send = () =>
    $fetch<T>(path, {
      method: options.method ?? 'GET',
      body: options.body,
      query: options.query,
      headers: auth.accessToken ? { Authorization: `Bearer ${auth.accessToken}` } : {},
    })

  try {
    return await send()
  }
  catch (error) {
    if (getErrorStatus(error) !== 401) throw error

    const refreshed = await auth.refreshSession()
    if (!refreshed) {
      await navigateTo('/login')
      throw error
    }

    return await send()
  }
}
