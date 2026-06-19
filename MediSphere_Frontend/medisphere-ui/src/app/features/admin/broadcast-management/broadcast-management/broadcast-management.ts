import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { BroadcastDto } from '../../../../core/models/notification.model';
import { AdminService } from '../../../../core/services/admin.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-broadcast-management',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule
  ],
  templateUrl: './broadcast-management.html',
  styleUrls: ['./broadcast-management.css']
})
export class BroadcastManagementComponent implements OnInit {

  private adminService = inject(AdminService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  sendingBroadcast = signal(false);

  broadcastForm = this.fb.group({
    title: ['', Validators.required],
    message: ['', Validators.required],
    type: ['Broadcast', Validators.required]
  });

  ngOnInit(): void {}

  sendBroadcast(): void {

    if (this.broadcastForm.invalid) {
      this.broadcastForm.markAllAsTouched();
      return;
    }

    this.sendingBroadcast.set(true);

    this.adminService.broadcast(
      this.broadcastForm.value as BroadcastDto
    ).subscribe({
      next: () => {

        this.toast.success(
          'Global broadcast notification sent successfully.'
        );

        this.broadcastForm.reset({
          type: 'Broadcast'
        });

        this.sendingBroadcast.set(false);
      },

      error: () => {

        this.toast.error(
          'Failed to send broadcast alert.'
        );

        this.sendingBroadcast.set(false);
      }
    });
  }
}