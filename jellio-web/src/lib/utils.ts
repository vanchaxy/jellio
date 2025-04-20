import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function getBaseUrl() {
  var match = window.location.href.match(/.*?\/jellio/);
  if (!match) {
    throw new Error('URL must include /jellio');
  }
  return match[0];
}
