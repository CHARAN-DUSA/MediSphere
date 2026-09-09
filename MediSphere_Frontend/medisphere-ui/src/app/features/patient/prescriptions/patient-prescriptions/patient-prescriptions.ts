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

  printPrescription(p: Prescription): void {
    const win = window.open('', '_blank');
    if (!win) return;

    const medicineRows = p.medicines?.length
      ? p.medicines.map(m => `
          <tr>
            <td>${m.medicineName}</td>
            <td>${m.dosage || '—'}</td>
            <td>${m.frequency || '—'}</td>
            <td>${m.duration || '—'}</td>
            <td>${m.route || '—'}</td>
          </tr>
        `).join('')
      : `<tr><td colspan="5" style="text-align:center; color:#718096;">No medicines recorded.</td></tr>`;

    win.document.write(`
      <!DOCTYPE html>
      <html>
      <head>
        <title>MediSphere Prescription - ${p.patientName}</title>
        <style>
          body { font-family: 'Inter', 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 40px; color: #1a202c; line-height: 1.5; }
          .header { display: flex; justify-content: space-between; align-items: flex-start; border-bottom: 2px solid #0a3d62; padding-bottom: 16px; margin-bottom: 24px; }
          .brand { font-size: 22px; font-weight: 800; color: #0a3d62; }
          .subtitle { font-size: 13px; color: #718096; margin-top: 4px; }
          .rx-meta { text-align: right; font-size: 13px; color: #718096; }
          .section-title { margin-top: 26px; margin-bottom: 8px; font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em; color: #0a3d62; }
          .info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px 24px; font-size: 14px; }
          .info-grid span { color: #718096; display: block; font-size: 12px; }
          table { width: 100%; border-collapse: collapse; margin-top: 8px; }
          th, td { text-align: left; padding: 10px 12px; border-bottom: 1px solid #e2e8f0; font-size: 13px; }
          th { background: #f8fafc; color: #475569; font-weight: 600; }
          .notes-block { font-size: 14px; margin-top: 6px; white-space: pre-wrap; }
          .follow-up-box { background: #f0fdfa; border: 1px solid #00cec9; padding: 12px 16px; border-radius: 8px; margin-top: 24px; font-size: 13px; color: #0a3d62; }
          .footer { margin-top: 40px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0; padding-top: 15px; }
        </style>
      </head>
      <body onload="window.print()">
        <div class="header">
          <div>
            <div class="brand">MediSphere Healthcare Platform</div>
            <div class="subtitle">Official Prescription Record</div>
          </div>
          <div class="rx-meta">
            <div>Prescription #${p.id}</div>
            <div>${p.createdAt ? new Date(p.createdAt).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }) : ''}</div>
          </div>
        </div>

        <div class="section-title">Patient & Consultation</div>
        <div class="info-grid">
          <div><span>Patient</span>${p.patientName}</div>
          <div><span>Doctor</span>${p.doctorName}</div>
          <div><span>Consultation Date</span>${p.appointmentDate ? new Date(p.appointmentDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }) : 'N/A'}</div>
          <div><span>Diagnosis</span>${p.diagnosis || 'N/A'}</div>
        </div>

        <div class="section-title">Medicines</div>
        <table>
          <tr><th>Medicine</th><th>Dosage</th><th>Frequency</th><th>Duration</th><th>Route</th></tr>
          ${medicineRows}
        </table>

        ${p.clinicalNotes ? `
          <div class="section-title">Clinical Notes</div>
          <div class="notes-block">${p.clinicalNotes}</div>
        ` : ''}

        ${p.instructions ? `
          <div class="section-title">Doctor's Instructions</div>
          <div class="notes-block">${p.instructions}</div>
        ` : ''}

        ${p.followUpDate ? `
          <div class="follow-up-box">
            <strong>Follow-up Scheduled:</strong>
            ${new Date(p.followUpDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })}
          </div>
        ` : ''}

        <div class="footer">
          This is an electronically generated prescription issued through MediSphere Healthcare Platform.<br/>
          For assistance, contact support@medisphere.app.
        </div>
      </body>
      </html>
    `);
    win.document.close();
  }
}