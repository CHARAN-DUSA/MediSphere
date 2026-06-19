/** Resolves a department iconUrl to a Material Icons ligature name. */
export function resolveDeptIcon(iconUrl?: string | null): string {
  if (iconUrl && /^[a-z0-9_]+$/.test(iconUrl)) {
    return iconUrl;
  }
  return 'local_hospital';
}
