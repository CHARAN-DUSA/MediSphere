import { MsIconComponent } from '../../../shared/components/ms-icon/ms-icon.component';
import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AppointmentService } from '../../../core/services/appointment.service';
import { AuthService } from '../../../core/services/auth.service';
import { Appointment } from '../../../core/models/appointment.model';
import { TelemedicineRoomComponent } from '../../../shared/components/telemedicine-room/telemedicine-room.component';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-telemedicine-page',
  standalone: true,
  imports: [MsIconComponent, CommonModule, RouterLink, TelemedicineRoomComponent],
  templateUrl:'./telemedicine-page.html',
  styleUrls: ['./telemedicine-page.css']
})
export class TelemedicinePageComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private apptService = inject(AppointmentService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);

  appointment = signal<Appointment | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  userRole = signal<'Patient' | 'Doctor'>('Patient');
  userName = signal<string>('');

  ngOnInit() {
    const apptId = +(this.route.snapshot.paramMap.get('appointmentId') ?? '0');
    if (!apptId) {
      this.error.set('Invalid consultation appointment room code.');
      this.loading.set(false);
      return;
    }

    const role = this.auth.currentRole();
    if (role === 'Doctor') {
      this.userRole.set('Doctor');
    } else {
      this.userRole.set('Patient');
    }

    this.apptService.getAppointmentById(apptId).subscribe({
      next: (res) => {
        if (res.data) {
          this.appointment.set(res.data);
          this.userName.set(
            this.userRole() === 'Doctor' ? res.data.doctorName || 'Doctor' : res.data.patientName || 'Patient'
          );
        } else {
          this.error.set('Could not locate the scheduled consultation session.');
        }
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load appointment details:', err);
        this.error.set('Access denied or appointment details could not be retrieved.');
        this.loading.set(false);
      }
    });
  }

  backLink() {
    return this.userRole() === 'Doctor' ? '/doctor/dashboard' : '/patient/appointments';
  }
}
