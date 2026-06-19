import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PatientProfile } from '../../../../core/models/patient-profile.model';
import { AdminService } from '../../../../core/services/admin.service';
import { ToastService } from '../../../../core/services/toast.service';



@Component({
  selector: 'app-patient-management',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './patient-management.html',
  styleUrls: ['./patient-management.css']
})
export class PatientManagementComponent implements OnInit {

  private adminService = inject(AdminService);
  private toast = inject(ToastService);

  loading = signal(false);

  patients = signal<PatientProfile[]>([]);

  ngOnInit(): void {
    this.loadPatients();
  }

  loadPatients(): void {

    this.loading.set(true);

    this.adminService.getPatients().subscribe({
      next: (response) => {

        this.patients.set(response.data ?? []);

        this.loading.set(false);
      },
      error: () => {

        this.loading.set(false);

        this.toast.error(
          'Failed to load patients.'
        );
      }
    });
  }

  toggleBlockUser(
    email: string,
    currentBlockStatus: boolean
  ): void {

    const nextBlockStatus =
      !currentBlockStatus;

    this.adminService
      .blockUser(email, nextBlockStatus)
      .subscribe({
        next: (response) => {

          this.toast.success(
            response.message ||
            (nextBlockStatus
              ? 'User blocked successfully.'
              : 'User restored successfully.')
          );

          this.loadPatients();
        },

        error: () => {

          this.toast.error(
            'Failed to update user status.'
          );
        }
      });
  }
}