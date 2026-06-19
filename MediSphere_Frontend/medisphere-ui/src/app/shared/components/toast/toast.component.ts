import { Component, inject } from '@angular/core';
import { NgClass, NgIf } from '@angular/common';
import { ToastService } from '../../../core/services/toast.service';
import { MsIconComponent } from '../ms-icon/ms-icon.component';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [NgIf, NgClass, MsIconComponent],
  templateUrl: './toast.html',
  styleUrls: ['./toast.css']
})
export class ToastComponent {
  toast = inject(ToastService);
  icons: Record<string, string> = {
    success: 'check_circle',
    error: 'cancel',
    info: 'info',
    warning: 'warning'
  };
}
