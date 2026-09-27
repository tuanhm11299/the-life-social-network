// Client-side checks for images before uploading. The API checks again; these just give faster feedback.
// Keep in sync with backend/src/TheLife.Application/Common/Files/ImageRules.cs.

export const MAX_IMAGE_SIZE_BYTES = 10 * 1024 * 1024
export const MAX_IMAGES_PER_POST = 10
export const ACCEPTED_IMAGE_TYPES = 'image/jpeg,image/png,image/webp,image/gif'

/** Returns an error message, or null when the file is fine. */
export function validateImageFile(file: File): string | null {
  if (!ACCEPTED_IMAGE_TYPES.split(',').includes(file.type)) return `"${file.name}" is not a JPEG, PNG, WebP or GIF image.`
  if (file.size > MAX_IMAGE_SIZE_BYTES) return `"${file.name}" is larger than 10 MB.`
  return null
}
