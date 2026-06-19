import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';
// src/app/features/patient/queue/live-queue/live-queue.component.ts
import { Component, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';

import { Subscription } from 'rxjs';
import { Appointment } from '../../../../core/models/appointment.model';
import { AppointmentService } from '../../../../core/services/appointment.service';
import { AuthService } from '../../../../core/services/auth.service';
import { LanguageService } from '../../../../core/services/language.service';
import { SignalRService } from '../../../../core/services/signalr.service';

@Component({
  selector: 'app-live-queue',
  standalone: true,
  imports: [MsIconComponent, NgIf, RouterLink],
  templateUrl: './live-queue.html',
  styleUrls: ['./live-queue.css']
})
export class LiveQueueComponent implements OnInit, OnDestroy {
  private apptService = inject(AppointmentService);
  private auth = inject(AuthService);
  private subs = new Subscription();
  signalRService = inject(SignalRService);
  langService = inject(LanguageService);

  appointments = signal<Appointment[]>([]);
  liveQueueUpdate = signal<string>('');

  myActiveAppointment = () => this.appointments().find(a =>
    a.queueToken && a.queueStatus !== 'Completed' && new Date(a.appointmentDate).toDateString() === new Date().toDateString()
  ) ?? null;

  ngOnInit() {
    this.loadAppointments();
    this.subs.add(this.signalRService.queuePositionChanged$.subscribe(({ tokenNumber, status }) => {
      this.liveQueueUpdate.set(`Queue Update: Your token #${tokenNumber} status changed to ${status}`);
      this.loadAppointments();
      setTimeout(() => this.liveQueueUpdate.set(''), 10000);
    }));
  }

  ngOnDestroy() { this.subs.unsubscribe(); }

  private loadAppointments() {
    this.apptService.getMyAppointments(1, 50).subscribe(r => this.appointments.set(r.data.items));
  }
}