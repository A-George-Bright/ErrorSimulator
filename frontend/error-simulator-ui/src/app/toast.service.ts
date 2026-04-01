import { inject, Injectable, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private snackBar = inject(MatSnackBar);
  private platformId = inject(PLATFORM_ID);

  success(message: string) {
    this.open(message, 'toast-success', 4000);
  }

  error(message: string) {
    this.open(message, 'toast-error', 5000);
  }

  warning(message: string) {
    this.open(message, 'toast-warning', 4000);
  }

  info(message: string) {
    this.open(message, 'toast-info', 4000);
  }

  private open(message: string, panelClass: string, duration: number) {
    if (!isPlatformBrowser(this.platformId)) return;
    this.snackBar.open(message, '✕', {
      duration,
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: [panelClass]
    });
  }
}
