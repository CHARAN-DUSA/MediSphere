import { Component, inject, OnInit, signal } from '@angular/core';
import { NgFor, NgIf, NgClass } from '@angular/common';
import { AdminService } from '../../../../core/services/admin.service';
import { ToastService } from '../../../../core/services/toast.service';
import { Doctor } from '../../../../core/models/doctor.model';

@Component({
  selector: 'app-doctor-management',
  standalone: true,
  imports: [NgFor, NgIf, NgClass],
  templateUrl: './doctor-management.html',
  styleUrls: ['./doctor-management.css']
})
export class DoctorManagementComponent implements OnInit {
  private adminService = inject(AdminService);
  private toast = inject(ToastService);

  doctors = signal<Doctor[]>([]);
  loading = signal(true);

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.adminService.getDoctors().subscribe({
      next: (r) => { this.doctors.set(r.data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
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