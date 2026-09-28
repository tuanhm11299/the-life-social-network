# ADR 0002: Clean uploaded images with ImageSharp 3

- **Status:** accepted
- **Date:** 2026-09-28

## Context

Uploads were saved byte for byte. Photos from phones carry metadata (EXIF, XMP, IPTC) with the GPS location, the
camera and the time, so posting a photo could reveal where someone lives. Next on the roadmap is resizing and
thumbnails, which also needs an image library, so one library should cover both jobs.

Options considered:

| Option | Pros | Cons |
|---|---|---|
| **ImageSharp** (Six Labors) | Pure .NET with no native binaries, so it behaves the same on Windows, macOS and Linux CI. Handles JPEG, PNG, WebP and animated GIF, and can resize. | Split license: free for open-source projects, paid for companies over $1M revenue. |
| SkiaSharp (MIT) | Fast, and can resize. | Native binaries per OS. Can't encode GIF, and the EXIF orientation has to be applied by hand. |
| Magick.NET (ImageMagick) | Supports every format, with a simple `Strip()`. | Large native dependency with a long CVE history. |
| NetVips (libvips) | Fastest, and uses the least memory. | Native binaries, and a less obvious API for newcomers. |
| Hand-written byte stripper | No dependency, lossless. | Loses the orientation tag, can't resize, and passes other hidden data through unchanged. |
| System.Drawing | Built in. | Windows-only since .NET 6. |

## Decision

- Use **ImageSharp 3.1.x**. Version 4 checks for a Six Labors license key at build time and fails Release builds
  without one; 3.1 has the same license terms without the key check.
- `Infrastructure/Images/ImageProcessor.cs` decodes every upload, turns it upright (`AutoOrient`), removes
  EXIF, XMP, IPTC, PNG text and GIF comments, and writes a fresh file in the same format. The ICC color profile is
  kept because it only describes colors.
- `LocalFileStorage` calls it before writing to disk, so no use case can forget to. A future cloud `IFileStorage`
  calls it the same way.
- Only the four allowed formats are registered with the decoder, and images over 60 megapixels (all frames
  together) are refused before decoding, to guard against decompression bombs.
- An image that passes `ImageRules` but can't be decoded throws `InvalidImageException`, which the API returns as 400.

## Consequences

- Uploads cost CPU and memory to re-encode. That's fine at this scale; measure in Phase 4.
- JPEGs are re-compressed (quality 85), so they lose a little quality.
- Files uploaded before this change still have their metadata. Delete `backend/src/TheLife.Api/uploads/` to start clean.
- Upgrading to ImageSharp 4 means getting a license key (a `sixlabors.lic` file locally, a secret in CI).
