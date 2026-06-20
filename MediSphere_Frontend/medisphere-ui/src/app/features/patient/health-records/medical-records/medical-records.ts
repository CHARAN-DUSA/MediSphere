import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';
// src/app/features/patient/health-records/medical-records/medical-records.component.ts
import { Component, inject, OnInit, signal } from '@angular/core';
import { NgFor, NgIf, DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MedicalRecord } from '../../../../core/models/medical-record.model';
import { AuthService } from '../../../../core/services/auth.service';
import { PatientService } from '../../../../core/services/patient.service';
import { ToastService } from '../../../../core/services/toast.service';


@Component({
  selector: 'app-medical-records',
  standalone: true,
  imports: [MsIconComponent, NgFor, NgIf, DatePipe, ReactiveFormsModule],
  templateUrl: './medical-records.html',
  styleUrls: ['./medical-records.css']
})
export class MedicalRecordsComponent implements OnInit {
  private patientService = inject(PatientService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  records = signal<MedicalRecord[]>([]);
  uploading = signal(false);
  selectedFile: File | null = null;

uploadForm = this.fb.group({
  description: ['']
});
  ngOnInit() {
    const patientId = this.auth.referenceId();
    this.patientService.getMedicalRecords(patientId).subscribe(r => this.records.set(r.data));
  }

  onFileSelected(event: any) { this.selectedFile = event.target.files[0] || null; }

  onUploadRecord() {

  console.log('Selected File:', this.selectedFile);
  console.log('Form Valid:', this.uploadForm.valid);
  console.log('Description:', this.uploadForm.value.description);

  if (!this.selectedFile || this.uploadForm.invalid) {
    console.log('Upload blocked');
    return;
  }

  console.log('Uploading...');
    this.uploading.set(true);
    this.patientService.uploadMedicalRecord(this.selectedFile, this.uploadForm.value.description!).subscribe({
      next: r => { this.uploading.set(false); this.selectedFile = null; this.uploadForm.reset(); this.toast.success('Record uploaded.'); this.records.update(l => [r.data, ...l]); },
      error: () => this.uploading.set(false)
    });
  }

  deleteRecord(id: number) {
    if (confirm('Delete this document?')) {
      this.patientService.deleteMedicalRecord(id).subscribe(() => {
        this.toast.success('Document deleted.'); this.records.update(l => l.filter(r => r.id !== id));
      });
    }
  }
}