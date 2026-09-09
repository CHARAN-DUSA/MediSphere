import { Component, inject, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';
import { AdminService, AdminDoctorDetail } from '../../../../core/services/admin.service';
import { ToastService } from '../../../../core/services/toast.service';
import { Doctor } from '../../../../core/models/doctor.model';
import { DoctorService } from '../../../../core/services/doctor.service';

@Component({
  selector: 'app-doctor-management',
  standalone: true,
  imports: [CommonModule, FormsModule, MsIconComponent],
  templateUrl: './doctor-management.html',
  styleUrls: ['./doctor-management.css']
})
export class DoctorManagementComponent implements OnInit, OnDestroy {
  private adminService = inject(AdminService);
  private doctorService = inject(DoctorService);
  private toast = inject(ToastService);

  doctors = signal<Doctor[]>([]);
  loading = signal(true);
  searchQuery = signal('');
  selectedProfileImageUrl: string | null = null;
  doctorImageUrls: Record<number, string> = {};

  selectedDoctorDetail = signal<AdminDoctorDetail | null>(null);
  doctorToDelete = signal<Doctor | null>(null);
  deleteReason = '';

  // The doctor's real share of gross revenue, computed from the actual
  // figures returned by the API rather than a hardcoded percentage —
  // so the label always matches what's genuinely being paid out.
  netPayoutPct = computed(() => {
    const d = this.selectedDoctorDetail();
    if (!d || !d.totalGrossEarnings) return 0;
    return +((d.totalNetEarnings ?? 0) / d.totalGrossEarnings * 100).toFixed(1);
  });

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
    const q = this.searchQuery().trim();

    if (q) {
      this.adminService.searchDoctors(q, 1, 50).subscribe({
        next: (r) => {
          this.doctors.set(r.data?.items ?? []);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.toast.error('Search failed.');
        }
      });
    } else {
      this.adminService.getDoctors().subscribe({
        next: (r) => {
          Object.values(this.doctorImageUrls).forEach(url => URL.revokeObjectURL(url));
          this.doctorImageUrls = {};
          this.doctors.set(r.data ?? []);
          this.loading.set(false);
          (r.data ?? []).forEach(doc => {
            if (doc.profileImageUrl) {
              this.doctorService.getProfileImageBlob(doc.id).subscribe({
                next: blob => { this.doctorImageUrls = { ...this.doctorImageUrls, [doc.id]: URL.createObjectURL(blob) }; },
                error: () => { /* fallback */ }
              });
            }
          });
        },
        error: () => this.loading.set(false)
      });
    }
  }

  onSearchChange(): void {
    this.load();
  }

  clearSearch(): void {
    this.searchQuery.set('');
    this.load();
  }

  viewDoctorDetails(doc: Doctor): void {
    if (!doc.id) return;
    this.adminService.getDoctorDetail(doc.id).subscribe({
      next: (res) => {
        this.selectedDoctorDetail.set(res.data ?? null);
      },
      error: () => {
        this.toast.error('Failed to fetch doctor details.');
      }
    });
  }

  closeDetailModal(): void {
    this.selectedDoctorDetail.set(null);
  }

  approveDoctor(doc: Doctor, approve: boolean): void {
    this.adminService.approveDoctor(doc.id, approve).subscribe({
      next: () => {
        this.toast.success(approve ? 'Doctor approved.' : 'Doctor rejected.');
        this.load();
      },
      error: () => this.toast.error('Failed to update doctor approval status.')
    });
  }

  suspendDoctor(doc: Doctor): void {
    this.adminService.suspendDoctor(doc.id).subscribe({
      next: () => {
        this.toast.success('Doctor suspended.');
        this.load();
      },
      error: () => this.toast.error('Failed to suspend doctor.')
    });
  }

  toggleBlockDoctor(doc: Doctor): void {
    const action = doc.isActive ? 'block' : 'unblock';
    const obs = doc.isActive
      ? this.adminService.blockDoctor(doc.id)
      : this.adminService.unblockDoctor(doc.id);

    obs.subscribe({
      next: () => {
        this.toast.success(`Doctor ${action}ed.`);
        this.load();
      },
      error: () => this.toast.error(`Failed to ${action} doctor.`)
    });
  }

  requestDeleteDoctor(doc: Doctor): void {
    this.doctorToDelete.set(doc);
    this.deleteReason = '';
  }

  closeDeleteModal(): void {
    this.doctorToDelete.set(null);
  }

  confirmDeleteDoctor(): void {
    const doc = this.doctorToDelete();
    if (!doc || !doc.id) return;

    this.adminService.deleteDoctor(doc.id, this.deleteReason || 'Admin requested deletion').subscribe({
      next: () => {
        this.toast.success('Doctor account soft-deleted and deactivated.');
        this.closeDeleteModal();
        this.load();
      },
      error: () => {
        this.toast.error('Failed to delete doctor account.');
      }
    });
  }

  getApprovalStatusLabel(status: any): string {
    if (status === 1 || status === 'Approved' || status === '1') return 'Approved';
    if (status === 2 || status === 'Rejected' || status === '2') return 'Rejected';
    return 'Pending Review';
  }

  getApprovalStatusClass(status: any): string {
    if (status === 1 || status === 'Approved' || status === '1') return 'badge-approved';
    if (status === 2 || status === 'Rejected' || status === '2') return 'badge-rejected';
    return 'badge-pending';
  }

  ngOnDestroy(): void {
    Object.values(this.doctorImageUrls).forEach(url => URL.revokeObjectURL(url));
    if (this.selectedProfileImageUrl && this.selectedProfileImageUrl.startsWith('blob:')) {
      URL.revokeObjectURL(this.selectedProfileImageUrl);
    }
  }
}