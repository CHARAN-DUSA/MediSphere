import { MsIconComponent } from '../../../shared/components/ms-icon/ms-icon.component';
import { Component, inject, OnInit, signal } from '@angular/core';
import { NgFor, NgIf, DatePipe } from '@angular/common';
import { AppointmentService } from '../../../core/services/appointment.service';
import { Appointment } from '../../../core/models/appointment.model';
import { ToastService } from '../../../core/services/toast.service';
import { LoaderComponent } from '../../../shared/components/loader/loader.component';

@Component({
  selector: 'app-appointment-history',
  standalone: true,
  imports: [MsIconComponent, NgFor, NgIf, DatePipe, LoaderComponent],
  templateUrl: './appointment-history.html',
  styleUrls: ['./appointment-history.css']
})
export class AppointmentHistoryComponent implements OnInit {
  private service = inject(AppointmentService);
  private toast = inject(ToastService);
  appointments = signal<Appointment[]>([]);
  loading = signal(false);
  ngOnInit() { this.load(); }
  load() {
    this.loading.set(true);
    this.service.getMyAppointments().subscribe(r => { this.appointments.set(r.data.items); this.loading.set(false); });
  }
  cancel(id: number) {
    if (!confirm('Cancel this appointment?')) return;
    this.service.cancelAppointment(id).subscribe(() => { this.toast.success('Appointment cancelled.'); this.load(); });
  }
}
