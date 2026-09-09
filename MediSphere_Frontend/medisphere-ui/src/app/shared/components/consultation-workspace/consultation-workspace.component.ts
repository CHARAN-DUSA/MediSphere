import { Component, Input, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MsIconComponent } from '../ms-icon/ms-icon.component';

import { DoctorConsultationService } from '../../../core/services/doctor-consultation.service';
import { PrescriptionService } from '../../../core/services/prescription.service';
import { AppointmentService } from '../../../core/services/appointment.service';
import { ToastService } from '../../../core/services/toast.service';

import { DoctorConsultation } from '../../../core/models/consultation.model';
import { MedicalRecord } from '../../../core/models/medical-record.model';
import { Prescription, CreatePrescriptionMedicine } from '../../../core/models/prescription.model';

/**
 * Real medical-consultation workspace shown to a Doctor during a telemedicine
 * session: patient overview, full medical history, previous consultations,
 * previous prescriptions, and a structured current-prescription form that is
 * actually persisted through the existing Prescription API.
 *
 * This intentionally reuses the same three backend-facing services the
 * doctor dashboard's in-person consultation modal already uses
 * (DoctorConsultationService, PrescriptionService, AppointmentService) —
 * it does not introduce any new service or endpoint.
 */
@Component({
  selector: 'app-consultation-workspace',
  standalone: true,
  imports: [CommonModule, FormsModule, MsIconComponent],
  templateUrl: './consultation-workspace.component.html',
  styleUrls: ['./consultation-workspace.component.css']
})
export class ConsultationWorkspaceComponent implements OnInit
{
  @Input({ required: true }) appointmentId!: number;

  consultation = signal<DoctorConsultation | null>(null);
  loading = signal(true);
  loadError = signal('');
  activeTab = signal<'overview' | 'history' | 'prescription'>('overview');
  saving = signal(false);
  openingRecordId = signal<number | null>(null);
  selectedPrescription = signal<Prescription | null>(null);

  diagnosis = '';
  clinicalNotes = '';
  instructions = '';
  followUpDate = '';
  medicines: CreatePrescriptionMedicine[] = [this.emptyMedicine()];

  constructor(
    private consultationService: DoctorConsultationService,
    private prescriptionService: PrescriptionService,
    private appointmentService: AppointmentService,
    private toast: ToastService,
    private router: Router
  ) { }

  ngOnInit(): void
  {
    this.load();
  }

  private load(): void
  {
    this.loading.set(true);
    this.loadError.set('');

    this.consultationService.getConsultation(this.appointmentId).subscribe({
      next: (res) =>
      {
        this.consultation.set(res.data);
        this.loading.set(false);

        // Switch to a sensible tab once we know the appointment's state.
        if (this.isReadOnly())
        {
          this.activeTab.set('history');
        }
      },
      error: (err) =>
      {
        this.loadError.set(
          err?.error?.message || 'Unable to load consultation details.'
        );
        this.loading.set(false);
      }
    });
  }

  //
  // Derived state
  //

  get appointment()
  {
    return this.consultation()?.appointment;
  }

  isCompleted(): boolean
  {
    return this.appointment?.status === 'Completed';
  }

  isCancelled(): boolean
  {
    return this.appointment?.status === 'Cancelled';
  }

  isReadOnly(): boolean
  {
    return this.isCompleted() || this.isCancelled();
  }

  setTab(tab: 'overview' | 'history' | 'prescription'): void
  {
    this.activeTab.set(tab);
  }

  //
  // Medical records
  //

  openRecord(record: MedicalRecord): void
  {
    this.openingRecordId.set(record.id);

    this.consultationService.getMedicalRecordFile(record.id, this.appointmentId).subscribe({
      next: (blob: Blob) =>
      {
        const url = window.URL.createObjectURL(blob);
        window.open(url, '_blank');
        setTimeout(() => window.URL.revokeObjectURL(url), 60000);
        this.openingRecordId.set(null);
      },
      error: (err) =>
      {
        this.openingRecordId.set(null);
        this.toast.error(
          err?.error?.message || 'Unable to open this medical record.'
        );
      }
    });
  }

  //
  // Previous prescriptions
  //

  viewPrescription(prescription: Prescription): void
  {
    this.selectedPrescription.set(prescription);
  }

  closePrescriptionDetail(): void
  {
    this.selectedPrescription.set(null);
  }

  //
  // Current prescription form
  //

  private emptyMedicine(): CreatePrescriptionMedicine
  {
    return {
      medicineName: '',
      dosage: '',
      frequency: '',
      duration: '',
      route: '',
      instructions: ''
    };
  }

  addMedicine(): void
  {
    this.medicines.push(this.emptyMedicine());
  }

  removeMedicine(index: number): void
  {
    if (this.medicines.length === 1)
    {
      this.medicines[0] = this.emptyMedicine();
      return;
    }
    this.medicines.splice(index, 1);
  }

  savePrescriptionAndComplete(): void
  {
    if (this.saving() || this.isReadOnly())
    {
      return;
    }

    if (!this.diagnosis.trim())
    {
      this.toast.error('Diagnosis is required.');
      return;
    }

    const validMedicines = this.medicines.filter(m => m.medicineName.trim().length > 0);
    if (validMedicines.length === 0)
    {
      this.toast.error('At least one medicine is required.');
      return;
    }

    this.saving.set(true);

    this.prescriptionService
      .create({
        appointmentId: this.appointmentId,
        diagnosis: this.diagnosis.trim(),
        clinicalNotes: this.clinicalNotes.trim(),
        instructions: this.instructions.trim(),
        followUpDate: this.followUpDate || null,
        medicines: validMedicines
      })
      .subscribe({
        next: () =>
        {
          this.appointmentService
            .updateStatus(this.appointmentId, 'Completed', this.clinicalNotes.trim() || undefined)
            .subscribe({
              next: () =>
              {
                this.toast.success('Prescription saved and consultation completed.');
                this.saving.set(false);
                this.router.navigate(['/doctor/appointments']);
              },
              error: (err) =>
              {
                console.error('Failed to mark appointment completed', err);
                this.toast.error(
                  'Prescription was saved, but marking the consultation as completed failed. Please refresh and try again.'
                );
                this.saving.set(false);
                this.load();
              }
            });
        },
        error: (err) =>
        {
          console.error('Failed to save prescription', err);
          this.toast.error(
            err?.error?.message || 'Failed to save prescription.'
          );
          this.saving.set(false);
        }
      });
  }
}