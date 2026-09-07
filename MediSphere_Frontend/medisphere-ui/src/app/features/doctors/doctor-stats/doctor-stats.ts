import { Component, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Appointment } from '../../../core/models/appointment.model';
import { Doctor, DoctorEarningsDto } from '../../../core/models/doctor.model';
import { MsIconComponent } from '../../../shared/components/ms-icon/ms-icon.component';

@Component({
  selector: 'app-doctor-stats',
  standalone: true,
  imports: [MsIconComponent, CommonModule],
  templateUrl: './doctor-stats.html',
  styleUrls: ['./doctor-stats.css']
})
export class DoctorStatsComponent {
  private router = inject(Router);

  doctorProfile = input.required<Doctor>();
  earnings = input<DoctorEarningsDto | null>(null);
  appointments = input<Appointment[]>([]);

  navigateTo(path: string): void {
    this.router.navigate(['/doctor', path]);
  }
}
