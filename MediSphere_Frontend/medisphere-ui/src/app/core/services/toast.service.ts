import { Injectable, signal } from '@angular/core';

export interface Toast { message: string; type: 'success' | 'error' | 'info' | 'warning'; }

@Injectable({ providedIn: 'root' })
export class ToastService {
  toast = signal<Toast | null>(null);

  show(message: string, type: Toast['type'] = 'info') {
    this.toast.set({ message, type });
    setTimeout(() => this.toast.set(null), 3500);
  }

  success(msg: string) { this.show(msg, 'success'); }
  error(msg: string) { this.show(msg, 'error'); }
  info(msg: string) { this.show(msg, 'info'); }
  warning(msg: string) { this.show(msg, 'warning'); }
}
