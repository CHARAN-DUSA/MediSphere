import { MsIconComponent } from '../ms-icon/ms-icon.component';
import { Component, Input, OnInit, OnDestroy, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SignalRService } from '../../../core/services/signalr.service';
import { AuthService } from '../../../core/services/auth.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-telemedicine-room',
  standalone: true,
  imports: [MsIconComponent, CommonModule, FormsModule],
  templateUrl: './telemedicine-room.html',
  styleUrls: ['./telemedicine-room.css']
})
export class TelemedicineRoomComponent implements OnInit, OnDestroy {
  @Input() meetingId!: string;
  @Input() appointmentId!: number;
  @Input() role!: 'Patient' | 'Doctor';
  @Input() userName!: string;

  prescriptionText: string = '';
  showSimulation = signal(true);
  isSynced = signal(false);
  
  // Media states
  localCamActive = signal(false);
  localMicActive = signal(false);
  remoteMedia = signal({ audio: false, video: false });

  localStream?: MediaStream;
  private jitsiApi: any = null;
  private subs: Subscription = new Subscription();

  constructor(
    private signalRService: SignalRService,
    private authService: AuthService
  ) {
    // React to SignalR status changes
    effect(() => {
      this.isSynced.set(this.signalRService.videoConnected());
    });
  }

  ngOnInit() {
    this.connectSignalR();
    this.tryLoadJitsi();
  }

  ngOnDestroy() {
    this.subs.unsubscribe();
    this.signalRService.stopConnections();
    this.stopLocalCamera();
    if (this.jitsiApi) {
      this.jitsiApi.dispose();
    }
  }

  private connectSignalR() {
    const token = this.authService.getToken() || '';
    this.signalRService.initVideoConnection(token, this.appointmentId);

    // Listen for live prescription sync
    this.subs.add(
      this.signalRService.prescriptionSynced$.subscribe(text => {
        if (this.role === 'Patient') {
          this.prescriptionText = text;
        }
      })
    );

    // Listen for peer media state changes
    this.subs.add(
      this.signalRService.mediaStateChanged$.subscribe(state => {
        this.remoteMedia.set({ audio: state.micActive, video: state.camActive });
      })
    );
  }

  private tryLoadJitsi() {
    // Check if external_api is already in window, otherwise load it
    if ((window as any).JitsiMeetExternalAPI) {
      this.initJitsi();
    } else {
      const script = document.createElement('script');
      script.src = 'https://meet.jit.si/external_api.js';
      script.async = true;
      script.onload = () => this.initJitsi();
      script.onerror = () => {
        console.warn('Jitsi script blocked or unavailable, running inside Sandbox simulator.');
        this.showSimulation.set(true);
        this.initLocalCamera();
      };
      document.body.appendChild(script);
    }
  }

  private initJitsi() {
    this.showSimulation.set(false);
    
    const domain = 'meet.jit.si';
    const options = {
      roomName: `medisphere-${this.meetingId}`,
      width: '100%',
      height: '100%',
      parentNode: document.querySelector('#jitsi-container'),
      userInfo: {
        displayName: this.userName
      },
      configOverwrite: {
        startWithAudioMuted: true,
        startWithVideoMuted: true,
        prejoinPageEnabled: false
      },
      interfaceConfigOverwrite: {
        SHOW_JITSI_WATERMARK: false,
        TOOLBAR_BUTTONS: ['microphone', 'camera', 'desktop', 'fullscreen', 'hangup', 'chat']
      }
    };

    try {
      this.jitsiApi = new (window as any).JitsiMeetExternalAPI(domain, options);

      // Listen to Jitsi events
      this.jitsiApi.addEventListener('audioMuteStatusChanged', (e: any) => {
        const muted = e.muted;
        this.localMicActive.set(!muted);
        this.broadcastMediaState();
      });

      this.jitsiApi.addEventListener('videoMuteStatusChanged', (e: any) => {
        const muted = e.muted;
        this.localCamActive.set(!muted);
        this.broadcastMediaState();
      });

      this.jitsiApi.addEventListener('readyToClose', () => {
        this.endCall();
      });

    } catch (err) {
      console.error('Failed to init Jitsi API', err);
      this.showSimulation.set(true);
      this.initLocalCamera();
    }
  }

  //
  // Simulated Camera Functions (Sandbox)
  //
  private async initLocalCamera() {
    try {
      this.localStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
      this.localCamActive.set(true);
      this.localMicActive.set(true);
      this.broadcastMediaState();
    } catch (err) {
      console.warn('Camera permission denied or not available. Using purely mock media feeds.', err);
    }
  }

  private stopLocalCamera() {
    if (this.localStream) {
      this.localStream.getTracks().forEach(track => track.stop());
    }
  }

  toggleCamera() {
    this.localCamActive.set(!this.localCamActive());
    if (this.localStream) {
      this.localStream.getVideoTracks().forEach(track => track.enabled = this.localCamActive());
    }
    this.broadcastMediaState();
  }

  toggleMicrophone() {
    this.localMicActive.set(!this.localMicActive());
    if (this.localStream) {
      this.localStream.getAudioTracks().forEach(track => track.enabled = this.localMicActive());
    }
    this.broadcastMediaState();
  }

  private broadcastMediaState() {
    this.signalRService.toggleMediaState(
      this.appointmentId,
      this.localCamActive(),
      this.localMicActive()
    );
  }

  onPrescriptionChange(value: string) {
    if (this.role === 'Doctor') {
      this.signalRService.syncLivePrescription(this.appointmentId, value);
    }
  }

  savePrescription() {
    console.log('Finalized prescription details saved:', this.prescriptionText);
    alert('Digital Prescription saved and filed under patient medical logs!');
  }

  endCall() {
    this.stopLocalCamera();
    alert('Consultation call session has ended.');
    // Redirect logic or event emitter
  }
}
