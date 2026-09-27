<script setup lang="ts">
import { z } from 'zod'
import type { Form, FormSubmitEvent } from '@nuxt/ui'
import { accountApi } from '~/api/account'
import { useAuthStore } from '~/stores/auth'

useHead({ title: 'Settings · The Life' })

const auth = useAuthStore()
const toast = useToast()
const showError = useErrorToast()

// ---- Profile photo ---------------------------------------------------------------------------

const avatarInput = useTemplateRef('avatarInput')
const avatarBusy = ref(false)

async function onAvatarChosen(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return

  const problem = validateImageFile(file)
  if (problem) return toast.add({ title: problem, color: 'error' })

  await updateAvatar(() => accountApi.updateAvatar(file), 'Profile photo updated')
}

async function updateAvatar(request: () => ReturnType<typeof accountApi.updateAvatar>, message: string) {
  avatarBusy.value = true
  try {
    auth.updateUser(await request())
    toast.add({ title: message, icon: 'i-lucide-check', color: 'success' })
  }
  catch (error) {
    showError(error)
  }
  finally {
    avatarBusy.value = false
  }
}

// ---- Profile details -------------------------------------------------------------------------

const profileSchema = z.object({
  displayName: z.string().trim().min(1, 'Name is required').max(50, 'At most 50 characters'),
  bio: z.string().max(150, 'At most 150 characters'),
})
type ProfileForm = z.output<typeof profileSchema>

const profileForm = reactive<ProfileForm>({
  displayName: auth.user?.displayName ?? '',
  bio: auth.user?.bio ?? '',
})
const savingProfile = ref(false)

async function saveProfile(event: FormSubmitEvent<ProfileForm>) {
  savingProfile.value = true
  try {
    auth.updateUser(await accountApi.updateProfile(event.data.displayName, event.data.bio))
    toast.add({ title: 'Profile saved', icon: 'i-lucide-check', color: 'success' })
  }
  catch (error) {
    showError(error)
  }
  finally {
    savingProfile.value = false
  }
}

// ---- Password --------------------------------------------------------------------------------

const passwordSchema = z.object({
  currentPassword: z.string().min(1, 'Enter your current password'),
  newPassword: z.string()
    .min(8, 'At least 8 characters')
    .regex(/[A-Za-z]/, 'Include at least one letter')
    .regex(/[0-9]/, 'Include at least one number'),
  confirmPassword: z.string(),
}).refine(data => data.newPassword === data.confirmPassword, {
  message: "Passwords don't match",
  path: ['confirmPassword'],
})
type PasswordForm = z.output<typeof passwordSchema>

const passwordFormRef = useTemplateRef<Form<PasswordForm>>('passwordFormRef')
const passwordForm = reactive<PasswordForm>({ currentPassword: '', newPassword: '', confirmPassword: '' })
const savingPassword = ref(false)

async function changePassword(event: FormSubmitEvent<PasswordForm>) {
  savingPassword.value = true
  try {
    await accountApi.changePassword(event.data.currentPassword, event.data.newPassword)
    Object.assign(passwordForm, { currentPassword: '', newPassword: '', confirmPassword: '' })
    toast.add({
      title: 'Password changed',
      description: 'You were signed out on your other devices.',
      icon: 'i-lucide-check',
      color: 'success',
    })
  }
  catch (error) {
    const fieldErrors = getFormErrors(error)
    if (fieldErrors.length > 0) passwordFormRef.value?.setErrors(fieldErrors)
    else if (getProblemDetails(error)?.code === 'Auth.WrongCurrentPassword')
      passwordFormRef.value?.setErrors([{ name: 'currentPassword', message: getErrorMessage(error) }])
    else showError(error)
  }
  finally {
    savingPassword.value = false
  }
}

async function logout() {
  await auth.logout()
  await navigateTo('/login')
}
</script>

<template>
  <div class="mx-auto max-w-2xl space-y-6 px-4 py-4 md:py-8">
    <h1 class="text-xl font-semibold text-highlighted md:text-2xl">Settings</h1>

    <!-- Profile photo -->
    <UCard v-if="auth.user">
      <div class="flex items-center gap-4">
        <UserAvatar :user="auth.user" size="3xl" avatar-class="size-16 text-xl" />
        <div class="min-w-0 flex-1">
          <p class="font-semibold text-highlighted">{{ auth.user.username }}</p>
          <p class="truncate text-sm text-muted">{{ auth.user.email }}</p>
        </div>
        <input ref="avatarInput" type="file" :accept="ACCEPTED_IMAGE_TYPES" class="hidden" @change="onAvatarChosen">
        <div class="flex flex-col gap-2 sm:flex-row">
          <UButton label="Change photo" size="sm" :loading="avatarBusy" @click="avatarInput?.click()" />
          <UButton
            v-if="auth.user.avatarUrl"
            label="Remove"
            size="sm"
            color="neutral"
            variant="ghost"
            :disabled="avatarBusy"
            @click="updateAvatar(accountApi.removeAvatar, 'Profile photo removed')"
          />
        </div>
      </div>
    </UCard>

    <!-- Profile details -->
    <UCard>
      <template #header>
        <h2 class="font-semibold text-highlighted">Edit profile</h2>
      </template>
      <UForm :schema="profileSchema" :state="profileForm" class="space-y-4" @submit="saveProfile">
        <UFormField name="displayName" label="Name">
          <UInput v-model="profileForm.displayName" class="w-full" />
        </UFormField>
        <UFormField name="bio" label="Bio" :hint="`${profileForm.bio.length}/150`">
          <UTextarea v-model="profileForm.bio" :maxlength="150" :rows="3" autoresize class="w-full" />
        </UFormField>
        <UButton type="submit" label="Save" :loading="savingProfile" />
      </UForm>
    </UCard>

    <!-- Password -->
    <UCard>
      <template #header>
        <h2 class="font-semibold text-highlighted">Change password</h2>
      </template>
      <UForm ref="passwordFormRef" :schema="passwordSchema" :state="passwordForm" class="space-y-4" @submit="changePassword">
        <UFormField name="currentPassword" label="Current password">
          <UInput v-model="passwordForm.currentPassword" type="password" autocomplete="current-password" class="w-full" />
        </UFormField>
        <UFormField name="newPassword" label="New password" help="At least 8 characters, with a letter and a number.">
          <UInput v-model="passwordForm.newPassword" type="password" autocomplete="new-password" class="w-full" />
        </UFormField>
        <UFormField name="confirmPassword" label="Confirm new password">
          <UInput v-model="passwordForm.confirmPassword" type="password" autocomplete="new-password" class="w-full" />
        </UFormField>
        <UButton type="submit" label="Change password" :loading="savingPassword" />
      </UForm>
    </UCard>

    <!-- Appearance and session -->
    <UCard>
      <div class="flex flex-wrap items-center justify-between gap-4">
        <div>
          <p class="font-semibold text-highlighted">Appearance</p>
          <p class="text-sm text-muted">Light, dark, or follow your device.</p>
        </div>
        <UColorModeSelect class="w-40" />
      </div>
      <USeparator class="my-4" />
      <UButton label="Log out" icon="i-ph-sign-out" color="error" variant="soft" @click="logout" />
    </UCard>
  </div>
</template>
