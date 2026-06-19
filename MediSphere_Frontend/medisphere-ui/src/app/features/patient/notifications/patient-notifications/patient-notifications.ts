import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { NgFor, NgIf, DatePipe } from '@angular/common';
import { Subscription } from 'rxjs';
import { PatientService } from '../../../../core/services/patient.service';
import { SignalRService } from '../../../../core/services/signalr.service';
import { Notification } from '../../../../core/models/notification.model';

@Component({
  selector: 'app-patient-notifications',
  standalone: true,
  imports: [NgFor, NgIf, DatePipe],
  templateUrl: './patient-notifications.html',
  styleUrls: ['./patient-notifications.css']
})
export class PatientNotificationsComponent implements OnInit, OnDestroy {
  private patientService = inject(PatientService);
  private signalRService = inject(SignalRService);
  private subs = new Subscription();

  notifications = signal<Notification[]>([]);

  hasUnread = () => this.notifications().length > 0;

  ngOnInit() {
    this.loadNotifications();
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

  ngOnDestroy() {
    this.subs.unsubscribe();
  }

  loadNotifications() {
    this.patientService.getNotifications().subscribe(r => this.notifications.set(r.data));
  }

  markAsRead(id: number) {
    this.patientService.markNotificationAsRead(id).subscribe(() => {
      this.notifications.update(list => list.filter(n => n.id !== id));
    });
  }

  markAllAsRead() {
    this.patientService.markAllNotificationsAsRead().subscribe(() => {
      this.notifications.set([]);
    });
  }
}
