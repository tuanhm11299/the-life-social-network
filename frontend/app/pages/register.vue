<script setup lang="ts">
import { z } from 'zod'
import type { Form, FormSubmitEvent } from '@nuxt/ui'
import { useAuthStore } from '~/stores/auth'

definePageMeta({ layout: 'auth', public: true })
useHead({ title: 'Sign up · The Life' })

const auth = useAuthStore()

// Same rules as the API (backend/.../Features/Auth/AuthRules.cs), checked here for instant feedback.
const schema = z.object({
  email: z.email('Enter a valid email address'),
  displayName: z.string().trim().max(50, 'At most 50 characters'),
  username: z.string()
    .min(3, 'At least 3 characters')
    .max(30, 'At most 30 characters')
    .regex(/^[a-zA-Z0-9._]+$/, 'Only letters, numbers, dots and underscores'),
  password: z.string()
    .min(8, 'At least 8 characters')
    .regex(/[A-Za-z]/, 'Include at least one letter')
    .regex(/[0-9]/, 'Include at least one number'),
})
type RegisterForm = z.output<typeof schema>

const formRef = useTemplateRef<Form<RegisterForm>>('formRef')
const form = reactive<RegisterForm>({ email: '', displayName: '', username: '', password: '' })
const submitting = ref(false)
const errorMessage = ref('')

async function onSubmit(event: FormSubmitEvent<RegisterForm>) {
  submitting.value = true
  errorMessage.value = ''
  try {
    await auth.register({ ...event.data, displayName: event.data.displayName || undefined })
    await navigateTo('/')
  }
  catch (error) {
    // Field errors (e.g. "username taken") appear under the field; anything else above the button.
    const fieldErrors = getFormErrors(error)
    if (fieldErrors.length > 0) formRef.value?.setErrors(fieldErrors)
    else errorMessage.value = getErrorMessage(error)
  }
  finally {
    submitting.value = false
  }
}
</script>

<template>
  <UCard :ui="{ body: 'p-8 sm:p-10' }">
    <div class="mb-8 text-center">
      <span class="font-logo text-4xl font-bold tracking-tight text-highlighted">The Life</span>
      <p class="mt-2 text-sm text-muted">Sign up to share photos and stories with your friends.</p>
    </div>

    <UForm ref="formRef" :schema="schema" :state="form" class="space-y-4" @submit="onSubmit">
      <UFormField name="email" label="Email">
        <UInput v-model="form.email" type="email" autocomplete="email" autofocus size="lg" class="w-full" />
      </UFormField>

      <UFormField name="displayName" label="Full name" hint="Optional">
        <UInput v-model="form.displayName" autocomplete="name" size="lg" class="w-full" />
      </UFormField>

      <UFormField name="username" label="Username" help="Letters, numbers, dots and underscores.">
        <UInput v-model="form.username" autocomplete="username" size="lg" class="w-full" />
      </UFormField>

      <UFormField name="password" label="Password" help="At least 8 characters, with a letter and a number.">
        <UInput v-model="form.password" type="password" autocomplete="new-password" size="lg" class="w-full" />
      </UFormField>

      <UAlert v-if="errorMessage" :title="errorMessage" color="error" variant="subtle" icon="i-lucide-circle-alert" />

      <UButton type="submit" label="Sign up" block size="lg" :loading="submitting" />
    </UForm>
  </UCard>

  <UCard class="mt-4" :ui="{ body: 'py-5 text-center text-sm' }">
    Have an account?
    <NuxtLink to="/login" class="font-semibold text-primary">Log in</NuxtLink>
  </UCard>
</template>
