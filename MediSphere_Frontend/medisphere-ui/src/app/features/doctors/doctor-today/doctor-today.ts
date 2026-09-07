import { Component, input, output } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Appointment } from '../../../core/models/appointment.model';
import { MsIconComponent } from '../../../shared/components/ms-icon/ms-icon.component';


@Component({
  selector: 'app-doctor-today',
  standalone: true,
  imports: [MsIconComponent, CommonModule, DatePipe, RouterLink, FormsModule],
  templateUrl: './doctor-today.html',
  styleUrls: ['./doctor-today.css']
})
export class DoctorTodayComponent {
  todayAppts = input<Appointment[]>([]);
  liveQueueWaiting = input<Appointment[]>([]);
  currentlyServing = input<Appointment | null>(null);
  signalRConnected = input<boolean>(false);

  statusUpdate = output<{ id: number; status: string }>();
  openNotes = output<Appointment>();
  callNext = output<void>();
  queueStatusChange = output<{ id: number; status: string }>();
}