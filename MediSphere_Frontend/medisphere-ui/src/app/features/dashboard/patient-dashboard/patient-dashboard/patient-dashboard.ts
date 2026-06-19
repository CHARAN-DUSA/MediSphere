import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';
// src/app/features/dashboard/patient-dashboard/patient-dashboard.component.ts
import { Component, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NgIf, NgFor } from '@angular/common';

import { Subscription } from 'rxjs';
import { Appointment } from '../../../../core/models/appointment.model';
import { AppointmentService } from '../../../../core/services/appointment.service';
import { AuthService } from '../../../../core/services/auth.service';
import { LanguageService } from '../../../../core/services/language.service';
import { PatientService } from '../../../../core/services/patient.service';
import { RewardsService, RewardStatement } from '../../../../core/services/rewards.service';
import { SignalRService } from '../../../../core/services/signalr.service';
import { ToastService } from '../../../../core/services/toast.service';
import { Notification } from '../../../../core/models/notification.model';
@Component({
  selector: 'app-patient-dashboard',
  standalone: true,
  imports: [MsIconComponent, RouterLink, RouterLinkActive, RouterOutlet, NgIf, NgFor],
  templateUrl: './patient-dashboard.html',
  styleUrls: ['./patient-dashboard.css']
})
export class PatientDashboardComponent implements OnInit, OnDestroy {
  private apptService = inject(AppointmentService);
  private patientService = inject(PatientService);
  private rewardsService = inject(RewardsService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  private router = inject(Router);
  signalRService = inject(SignalRService);
  langService = inject(LanguageService);

  showPwaBanner = signal(false);
  private pwaInstallPrompt: any = null;
  private subs = new Subscription();

  appointments = signal<Appointment[]>([]);
  notifications = signal<Notification[]>([]);
  rewardStatement = signal<RewardStatement | null>(null);

  pending = () => this.appointments().filter(a => a.status === 'Pending').length;
  completed = () => this.appointments().filter(a => a.status === 'Completed').length;
  unreadNotificationsCount = () => this.notifications().filter(n => !n.isRead).length;

  navItems = [
    { path: 'appointments', icon: 'event_available', label: 'Appointments' },
    { path: 'queue',        icon: 'format_list_numbered', label: 'Live Queue' },
    { path: 'smart',        icon: 'psychology', label: 'Smart Finder' },
    { path: 'rewards',      icon: 'emoji_events', label: 'Rewards' },
    { path: 'records',      icon: 'folder_open', label: 'Health Records' },
    { path: 'favorites',    icon: 'favorite', label: 'Saved Doctors' },
    { path: 'profile',      icon: 'person', label: 'Profile & Family' },
    { path: 'notifications', icon: 'notifications', label: 'Notifications', badge: true },
  ];

  ngOnInit() {
    this.loadStats();
    this.initSignalR();
    this.setupPwaInstallPrompt();
  }

  ngOnDestroy() {
    this.subs.unsubscribe();
    this.signalRService.stopConnections();
  }

  private initSignalR() {
    const token = this.auth.getToken() || '';
    this.signalRService.initQueueConnection(token);
    this.signalRService.initNotificationConnection(token);
    const patientId = this.auth.referenceId();
    if (patientId) this.signalRService.registerPatientQueueListener(patientId);
    this.subs.add(this.signalRService.queueUpdated$.subscribe(() => {this.apptService.getMyAppointments(1, 50).subscribe(r =>this.appointments.set(r.data.items));}));
    this.subs.add(this.signalRService.notificationReceived$.subscribe(notification => {
      this.notifications.update(list => {
        if (list.some(n => n.id === notification.id)) return list;
        return [notification, ...list];
      });
    }));
    this.subs.add(this.signalRService.notificationsUpdated$.subscribe(() => this.loadNotifications()));
    this.subs.add(this.signalRService.notificationRemoved$.subscribe(id => {
      this.notifications.update(list => list.filter(n => n.id !== id));
    }));
  }

  private setupPwaInstallPrompt() {
    window.addEventListener('beforeinstallprompt', (e: any) => {
      e.preventDefault();
      this.pwaInstallPrompt = e;
      this.showPwaBanner.set(true);
    });
  }

  installPwa() {
    if (this.pwaInstallPrompt) {
      this.pwaInstallPrompt.prompt();
      this.pwaInstallPrompt.userChoice.then(() => {
        this.pwaInstallPrompt = null;
        this.showPwaBanner.set(false);
      });
    }
  }

  loadStats() {
    const patientId = this.auth.referenceId();
    this.apptService.getMyAppointments(1, 50).subscribe(r => this.appointments.set(r.data.items));
    this.loadNotifications();
    this.rewardsService.getMyStatement().subscribe(r => this.rewardStatement.set(r.data));
  }

  loadNotifications() {
    this.patientService.getNotifications().subscribe(r => this.notifications.set(r.data));
  }

  dismissNotification(id: number) {
    this.patientService.markNotificationAsRead(id).subscribe(() => {
      this.notifications.update(list => list.filter(n => n.id !== id));
    });
  }

  setLang(lang: 'en' | 'hi' | 'te') {
    this.langService.setLanguage(lang);
  }

  navigateTo(path: string) {
    this.router.navigate(['/patient', path]);
  }
}