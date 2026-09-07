import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PatientProfile } from '../../../../core/models/patient-profile.model';
import { AdminService, AdminPatientDetail } from '../../../../core/services/admin.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-patient-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './patient-management.html',
  styleUrls: ['./patient-management.css']
})
export class PatientManagementComponent implements OnInit {
  private adminService = inject(AdminService);
  private toast = inject(ToastService);

  loading = signal(false);
  patients = signal<PatientProfile[]>([]);
  searchQuery = signal('');
  selectedPatientDetail = signal<AdminPatientDetail | null>(null);
  patientToDelete = signal<PatientProfile | null>(null);
  deleteReason = '';

  ngOnInit(): void {
    this.loadPatients();
  }

  loadPatients(): void {
    this.loading.set(true);
    const q = this.searchQuery().trim();

    if (q) {
      this.adminService.searchPatients(q, 1, 50).subscribe({
        next: (res) => {
          this.patients.set(res.data?.items ?? []);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.toast.error('Search failed.');
        }
      });
    } else {
      this.adminService.getPatients().subscribe({
        next: (response) => {
          this.patients.set(response.data ?? []);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.toast.error('Failed to load patients.');
        }
      });
    }
  }

  onSearchChange(): void {
    this.loadPatients();
  }

  clearSearch(): void {
    this.searchQuery.set('');
    this.loadPatients();
  }

  viewPatientDetails(patient: PatientProfile): void {
    if (!patient.id) return;
    this.adminService.getPatientDetail(patient.id).subscribe({
      next: (res) => {
        this.selectedPatientDetail.set(res.data ?? null);
      },
      error: () => {
        this.toast.error('Failed to fetch patient details.');
      }
    });
  }

  closeDetailModal(): void {
    this.selectedPatientDetail.set(null);
  }

  requestDeletePatient(patient: PatientProfile): void {
    this.patientToDelete.set(patient);
    this.deleteReason = '';
  }

  closeDeleteModal(): void {
    this.patientToDelete.set(null);
  }

  confirmDeletePatient(): void {
    const p = this.patientToDelete();
    if (!p || !p.id) return;

    this.adminService.deletePatient(p.id, this.deleteReason || 'Admin requested deletion').subscribe({
      next: () => {
        this.toast.success('Patient account deleted and personal data anonymized.');
        this.closeDeleteModal();
        this.loadPatients();
      },
      error: () => {
        this.toast.error('Failed to delete patient account.');
      }
    });
  }

  toggleBlockUser(email: string, isCurrentlyActive: boolean): void {
    const shouldBlock = isCurrentlyActive;

    this.adminService.blockUser(email, shouldBlock).subscribe({
      next: (response) => {
        this.toast.success(
          response.message ||
          (shouldBlock ? 'User blocked successfully.' : 'User unblocked successfully.')
        );
        this.loadPatients();
      },
      error: () => {
        this.toast.error('Failed to update user status.');
      }
    });
  }
}