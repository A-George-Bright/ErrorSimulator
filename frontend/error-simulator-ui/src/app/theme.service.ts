import { Injectable, signal, effect, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  isDark = signal(true);

  private isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  constructor() {
    if (this.isBrowser) {
      const saved = localStorage.getItem('theme');
      if (saved === 'light') {
        this.isDark.set(false);
      }
    }

    effect(() => {
      const dark = this.isDark();
      if (this.isBrowser) {
        document.body.classList.toggle('light-theme', !dark);
        localStorage.setItem('theme', dark ? 'dark' : 'light');
      }
    });
  }

  toggle() {
    this.isDark.update(v => !v);
  }
}
