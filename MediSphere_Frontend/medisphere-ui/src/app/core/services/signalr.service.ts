import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Notification } from '../models/notification.model';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubUrl = environment.apiUrl.replace('/api', ''); 
  private queueConnection!: signalR.HubConnection;
  private videoConnection!: signalR.HubConnection;
  private notificationConnection!: signalR.HubConnection;

  // Real-time Event Streams
  queueUpdated$ = new Subject<void>();
  queuePositionChanged$ = new Subject<{ tokenNumber: number; status: string }>();
  consultationStatusChanged$ = new Subject<string>();
  mediaStateChanged$ = new Subject<{ camActive: boolean; micActive: boolean }>();
  prescriptionSynced$ = new Subject<string>();
  notificationReceived$ = new Subject<Notification>();
  notificationsUpdated$ = new Subject<void>();
  notificationRemoved$ = new Subject<number>();

  // Signal State Trackers
  queueConnected = signal(false);
  videoConnected = signal(false);
  notificationConnected = signal(false);

  constructor() {}

  //
  // Queue Tracking WebSocket Actions
  //
  initQueueConnection(token: string) {
    if (this.queueConnection) {
      this.queueConnection.stop();
    }

    this.queueConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.hubUrl}/hubs/queue`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    this.queueConnection.on('QueueUpdated', () => {
      this.queueUpdated$.next();
    });

    this.queueConnection.on('QueuePositionChanged', (tokenNumber: number, status: string) => {
      this.queuePositionChanged$.next({ tokenNumber, status });
    });

    this.queueConnection.start()
      .then(() => {
        console.log('QueueHub connected successfully.');
        this.queueConnected.set(true);
      })
      .catch(err => {
        console.error('QueueHub connection error:', err);
        this.queueConnected.set(false);
      });
  }

  registerPatientQueueListener(patientId: number) {
    if (this.queueConnection) {
      this.queueConnection.on(`QueuePositionChanged_${patientId}`, (tokenNumber: number, status: string) => {
        this.queuePositionChanged$.next({ tokenNumber, status });
      });
    }
  }

  joinDoctorQueueRoom(doctorId: number) {
    if (this.queueConnection && this.queueConnected()) {
      this.queueConnection.invoke('JoinDoctorQueueRoom', doctorId.toString())
        .catch(err => console.error('Join doctor queue room error:', err));
    }
  }

  leaveDoctorQueueRoom(doctorId: number) {
    if (this.queueConnection && this.queueConnected()) {
      this.queueConnection.invoke('LeaveDoctorQueueRoom', doctorId.toString())
        .catch(err => console.error('Leave doctor queue room error:', err));
    }
  }

  joinDepartmentQueueRoom(departmentId: number) {
    if (this.queueConnection && this.queueConnected()) {
      this.queueConnection.invoke('JoinDepartmentQueueRoom', departmentId.toString())
        .catch(err => console.error('Join dept queue room error:', err));
    }
  }

  leaveDepartmentQueueRoom(departmentId: number) {
    if (this.queueConnection && this.queueConnected()) {
      this.queueConnection.invoke('LeaveDepartmentQueueRoom', departmentId.toString())
        .catch(err => console.error('Leave dept queue room error:', err));
    }
  }

  //
  // Inbox Notification Hub
  //
  initNotificationConnection(token: string) {
    if (this.notificationConnection) {
      this.notificationConnection.stop();
    }

    this.notificationConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.hubUrl}/hubs/notifications`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    this.notificationConnection.on('NotificationReceived', (notification: Notification) => {
      this.notificationReceived$.next(notification);
    });

    this.notificationConnection.on('NotificationsUpdated', () => {
      this.notificationsUpdated$.next();
    });

    this.notificationConnection.on('NotificationRemoved', (notificationId: number) => {
      this.notificationRemoved$.next(notificationId);
    });

    this.notificationConnection.start()
      .then(() => {
        console.log('NotificationHub connected successfully.');
        this.notificationConnected.set(true);
      })
      .catch(err => {
        console.error('NotificationHub connection error:', err);
        this.notificationConnected.set(false);
      });
  }

  //
  // Telemedicine / WebRTC Coordination Hub
  //
  initVideoConnection(token: string, appointmentId: number) {
    if (this.videoConnection) {
      this.videoConnection.stop();
    }

    this.videoConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.hubUrl}/hubs/video`, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    this.videoConnection.on('ConsultationStatusChanged', (status: string) => {
      this.consultationStatusChanged$.next(status);
    });

    this.videoConnection.on('MediaStateChanged', (camActive: boolean, micActive: boolean) => {
      this.mediaStateChanged$.next({ camActive, micActive });
    });

    this.videoConnection.on('PrescriptionSynced', (prescriptionJson: string) => {
      this.prescriptionSynced$.next(prescriptionJson);
    });

    this.videoConnection.start()
      .then(() => {
        console.log('VideoHub connected successfully.');
        this.videoConnected.set(true);
        this.videoConnection.invoke('JoinConsultationRoom', appointmentId.toString())
          .catch(err => console.error('Join consultation room error:', err));
      })
      .catch(err => {
        console.error('VideoHub connection error:', err);
        this.videoConnected.set(false);
      });
  }

  toggleMediaState(appointmentId: number, camActive: boolean, micActive: boolean) {
    if (this.videoConnection && this.videoConnected()) {
      this.videoConnection.invoke('ToggleMediaState', appointmentId.toString(), camActive, micActive)
        .catch(err => console.error('Toggle media state failed:', err));
    }
  }

  syncLivePrescription(appointmentId: number, prescriptionJson: string) {
    if (this.videoConnection && this.videoConnected()) {
      this.videoConnection.invoke('SyncLivePrescription', appointmentId.toString(), prescriptionJson)
        .catch(err => console.error('Sync prescription failed:', err));
    }
  }

  stopConnections() {
    if (this.queueConnection) {
      this.queueConnection.stop();
      this.queueConnected.set(false);
    }
    if (this.videoConnection) {
      this.videoConnection.stop();
      this.videoConnected.set(false);
    }
    if (this.notificationConnection) {
      this.notificationConnection.stop();
      this.notificationConnected.set(false);
    }
  }
}
