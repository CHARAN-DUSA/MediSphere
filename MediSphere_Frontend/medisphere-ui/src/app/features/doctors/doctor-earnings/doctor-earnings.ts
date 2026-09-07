import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Appointment } from '../../../core/models/appointment.model';
import { DoctorEarningsDto } from '../../../core/models/doctor.model';
import { AppointmentService } from '../../../core/services/appointment.service';
import { AuthService } from '../../../core/services/auth.service';
import { DoctorService } from '../../../core/services/doctor.service';
import { ToastService } from '../../../core/services/toast.service';
import { MsIconComponent } from '../../../shared/components/ms-icon/ms-icon.component';

@Component({
  selector: 'app-doctor-earnings',
  standalone: true,
  imports: [MsIconComponent, CommonModule, RouterLink],
  templateUrl: './doctor-earnings.html',
  styleUrls: ['./doctor-earnings.css']
})
export class DoctorEarningsComponent implements OnInit {
  private doctorService = inject(DoctorService);
  private apptService = inject(AppointmentService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);

  earnings = signal<DoctorEarningsDto | null>(null);
  appointments = signal<Appointment[]>([]);
  loading = signal(true);

  // Derived stats
  completedAppts = computed(() => this.appointments().filter(a => a.status === 'Completed'));
  pendingAppts = computed(() => this.appointments().filter(a => a.status === 'Pending'));
  cancelledAppts = computed(() => this.appointments().filter(a => a.status === 'Cancelled'));

  deductionRate = computed(() => {
    const e = this.earnings();
    if (!e || e.totalGrossEarnings === 0) return 0;
    const totalDed = e.totalPlatformFeesPaid + e.totalTaxesPaid + e.totalAdminCommissionPaid;
    return ((totalDed / e.totalGrossEarnings) * 100).toFixed(1);
  });

  netPercent = computed(() => {
    const e = this.earnings();
    if (!e || e.totalGrossEarnings === 0) return 0;
    return ((e.totalNetEarnings / e.totalGrossEarnings) * 100).toFixed(1);
  });

  // Bar widths for breakdown visualization
  platformFeeWidth = computed(() => {
    const e = this.earnings();
    if (!e || e.totalGrossEarnings === 0) return 0;
    return (e.totalPlatformFeesPaid / e.totalGrossEarnings) * 100;
  });

  taxWidth = computed(() => {
    const e = this.earnings();
    if (!e || e.totalGrossEarnings === 0) return 0;
    return (e.totalTaxesPaid / e.totalGrossEarnings) * 100;
  });

  commissionWidth = computed(() => {
    const e = this.earnings();
    if (!e || e.totalGrossEarnings === 0) return 0;
    return (e.totalAdminCommissionPaid / e.totalGrossEarnings) * 100;
  });

  netWidth = computed(() => {
    const e = this.earnings();
    if (!e || e.totalGrossEarnings === 0) return 0;
    return (e.totalNetEarnings / e.totalGrossEarnings) * 100;
  });

  ngOnInit() {
    const docId = this.auth.referenceId();
    if (!docId) return;

    this.doctorService.getDoctorEarnings(docId).subscribe({
      next: (res) => {
        if (res.data) this.earnings.set(res.data);
        this.loading.set(false);
      },
      error: () => {
        this.toast.error('Failed to load earnings data.');
        this.loading.set(false);
      }
    });

    this.apptService.getAllAppointments(1, 200, docId).subscribe({
      next: (res) => this.appointments.set(res.data.items),
      error: () => {}
    });
  }
}