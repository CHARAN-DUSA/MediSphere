import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';

import { PrescriptionService } from '../../../../core/services/prescription.service';
import { AuthService } from '../../../../core/services/auth.service';
import { Prescription } from '../../../../core/models/prescription.model';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-patient-prescriptions',
  standalone: true,
  imports: [CommonModule, MsIconComponent],
  templateUrl: './patient-prescriptions.html',
  styleUrls: ['./patient-prescriptions.css']
})
export class PatientPrescriptionsComponent implements OnInit {

  private prescriptionService = inject(PrescriptionService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);

  prescriptions = signal<Prescription[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.loadPrescriptions();
  }

  loadPrescriptions(): void {
    const patientId = this.auth.referenceId();

    if (!patientId) {
      this.loading.set(false);
      this.toast.error('Unable to identify your patient account.');
      return;
    }

    this.prescriptionService
      .getPatientHistory(patientId)
      .subscribe({
        next: (response) => {
          this.prescriptions.set(response.data ?? []);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.toast.error('Unable to load your prescriptions.');
        }
      });
  }

  trackByPrescription(
    index: number,
    prescription: Prescription
  ): number {
    return prescription.id;
  }
}