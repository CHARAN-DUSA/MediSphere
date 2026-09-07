import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { NgFor, NgIf, DatePipe } from '@angular/common';
import { Subscription } from 'rxjs';
import { SignalRService } from '../../../core/services/signalr.service';
import { DoctorService } from '../../../core/services/doctor.service';
import { Notification } from '../../../core/models/notification.model';
@Component({
  selector: 'app-doctor-notifications',
  standalone: true,
  imports: [NgFor, NgIf, DatePipe],
  templateUrl: './doctor-notifications.html',
  styleUrls: ['./doctor-notifications.css']
})
export class DoctorNotificationsComponent implements OnInit, OnDestroy {
  private doctorService = inject(DoctorService);
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
    this.doctorService.getNotifications().subscribe(r => this.notifications.set(r.data));
  }

  markAsRead(id: number) {
    this.doctorService.markNotificationAsRead(id).subscribe(() => {
      this.notifications.update(list => list.filter(n => n.id !== id));
    });
  }

  markAllAsRead() {
    this.doctorService.markAllNotificationsAsRead().subscribe(() => {
      this.notifications.set([]);
    });
  }
}
