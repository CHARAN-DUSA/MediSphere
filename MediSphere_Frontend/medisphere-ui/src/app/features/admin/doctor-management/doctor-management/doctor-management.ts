import { Component, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { NgFor, NgIf, NgClass } from '@angular/common';
import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';
import { AdminService } from '../../../../core/services/admin.service';
import { ToastService } from '../../../../core/services/toast.service';
import { Doctor } from '../../../../core/models/doctor.model';
import { DoctorService } from '../../../../core/services/doctor.service';

@Component({
  selector: 'app-doctor-management',
  standalone: true,
  imports: [NgFor, NgIf, NgClass, MsIconComponent],
  templateUrl: './doctor-management.html',
  styleUrls: ['./doctor-management.css']
})
export class DoctorManagementComponent implements OnInit, OnDestroy {
  private adminService = inject(AdminService);
  private doctorService = inject(DoctorService);
  private toast = inject(ToastService);

  doctors = signal<Doctor[]>([]);
  loading = signal(true);
  selectedProfileImageUrl: string | null = null;
  doctorImageUrls: Record<number, string> = {};

  openProfileImage(doctorId: number): void {
    if (!doctorId) return;
    this.doctorService.getProfileImageBlob(doctorId).subscribe({
      next: (blob) => {
        if (this.selectedProfileImageUrl && this.selectedProfileImageUrl.startsWith('blob:')) {
          URL.revokeObjectURL(this.selectedProfileImageUrl);
        }
        this.selectedProfileImageUrl = URL.createObjectURL(blob);
      },
      error: (err) => {
        console.error('Failed to load doctor profile image', err);
        this.selectedProfileImageUrl = null;
      }
    });
  }

  closeProfileImage(): void {
    if (this.selectedProfileImageUrl && this.selectedProfileImageUrl.startsWith('blob:')) {
      URL.revokeObjectURL(this.selectedProfileImageUrl);
    }
    this.selectedProfileImageUrl = null;
  }

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.adminService.getDoctors().subscribe({
      next: (r) => {
        Object.values(this.doctorImageUrls).forEach(url => URL.revokeObjectURL(url));
        this.doctorImageUrls = {};
        this.doctors.set(r.data);
        this.loading.set(false);
        r.data.forEach(doc => {
          if (doc.profileImageUrl) {
            this.doctorService.getProfileImageBlob(doc.id).subscribe({
              next: blob => { this.doctorImageUrls = { ...this.doctorImageUrls, [doc.id]: URL.createObjectURL(blob) }; },
              error: () => { /* fallback: no image */ }
            });
          }
        });
      },
      error: () => this.loading.set(false)
    });
  }

  ngOnDestroy(): void {
    Object.values(this.doctorImageUrls).forEach(url => URL.revokeObjectURL(url));
    if (this.selectedProfileImageUrl && this.selectedProfileImageUrl.startsWith('blob:')) {
      URL.revokeObjectURL(this.selectedProfileImageUrl);
    }
  }

  verifyDoctor(id: number, approve: boolean) {
    this.adminService.approveDoctor(id, approve).subscribe({
      next: (r) => { this.toast.success(r.message || (approve ? 'Doctor approved.' : 'Registration rejected.')); this.load(); },
      error: () => this.toast.error('Failed to moderate doctor status.')
    });
  }

  suspendDoctor(id: number) {
    this.adminService.suspendDoctor(id).subscribe({
      next: (r) => { this.toast.success(r.message || 'Doctor suspended.'); this.load(); },
      error: () => this.toast.error('Failed to suspend doctor.')
    });
  }

  blockDoctor(id: number) {
    this.adminService.blockDoctor(id).subscribe({
      next: (r) => { this.toast.success(r.message || 'Doctor blocked.'); this.load(); },
      error: () => this.toast.error('Failed to block doctor.')
    });
  }

  unblockDoctor(id: number) {
    this.adminService.unblockDoctor(id).subscribe({
      next: (r) => { this.toast.success(r.message || 'Doctor restored.'); this.load(); },
      error: () => this.toast.error('Failed to restore doctor.')
    });
  }
}