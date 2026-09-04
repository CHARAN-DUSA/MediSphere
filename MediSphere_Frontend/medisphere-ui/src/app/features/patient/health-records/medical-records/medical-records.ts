import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';
import { Component, inject, OnInit, signal, OnDestroy } from '@angular/core';
import { NgFor, NgIf, DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
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
export class MedicalRecordsComponent implements OnInit, OnDestroy {
  private patientService = inject(PatientService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);
  private sanitizer = inject(DomSanitizer);

  records = signal<MedicalRecord[]>([]);
  uploading = signal(false);
  selectedFile: File | null = null;

  activeRecord: MedicalRecord | null = null;
  activeFileUrl: SafeResourceUrl | null = null;
  rawBlobUrl: string | null = null;
  loadingViewer = signal(false);
  isPdf = false;
  isImage = false;
  isUnsupported = false;

  recordToDelete = signal<MedicalRecord | null>(null);

  uploadForm = this.fb.group({
    description: ['']
  });

  ngOnInit() {
    const patientId = this.auth.referenceId();
    this.patientService.getMedicalRecords(patientId).subscribe(r => this.records.set(r.data));
  }

  ngOnDestroy() {
    this.cleanupBlobUrl();
  }

  onFileSelected(event: any) { this.selectedFile = event.target.files[0] || null; }

  onUploadRecord() {
    if (!this.selectedFile || this.uploadForm.invalid) {
      return;
    }

    this.uploading.set(true);
    this.patientService.uploadMedicalRecord(this.selectedFile, this.uploadForm.value.description!).subscribe({
      next: r => {
        this.uploading.set(false);
        this.selectedFile = null;
        this.uploadForm.reset();
        this.toast.success('Record uploaded.');
        this.records.update(l => [r.data, ...l]);
      },
      error: () => this.uploading.set(false)
    });
  }

  viewRecord(r: MedicalRecord) {
    this.cleanupBlobUrl();
    this.activeRecord = r;
    this.loadingViewer.set(true);

    const ext = (r.fileName.split('.').pop() || '').toLowerCase();
    this.isPdf = ext === 'pdf';
    this.isImage = ['jpg', 'jpeg', 'png', 'gif', 'webp'].includes(ext);
    this.isUnsupported = !this.isPdf && !this.isImage;

    this.patientService.getMedicalRecordFileBlob(r.id).subscribe({
      next: (blob) => {
        this.rawBlobUrl = URL.createObjectURL(blob);
        this.activeFileUrl = this.sanitizer.bypassSecurityTrustResourceUrl(this.rawBlobUrl);
        this.loadingViewer.set(false);
      },
      error: () => {
        this.loadingViewer.set(false);
        this.toast.error('Unable to load document preview.');
      }
    });
  }

  closeViewer() {
    this.cleanupBlobUrl();
    this.activeRecord = null;
    this.activeFileUrl = null;
    this.isPdf = false;
    this.isImage = false;
    this.isUnsupported = false;
  }

  private cleanupBlobUrl() {
    if (this.rawBlobUrl) {
      URL.revokeObjectURL(this.rawBlobUrl);
      this.rawBlobUrl = null;
    }
  }

  requestDeleteRecord(r: MedicalRecord) {
    this.recordToDelete.set(r);
  }

  closeDeleteModal() {
    this.recordToDelete.set(null);
  }

  confirmDeleteRecord() {
    const record = this.recordToDelete();
    if (!record) return;

    this.patientService.deleteMedicalRecord(record.id).subscribe(() => {
      this.toast.success('Document deleted.');
      this.records.update(l => l.filter(r => r.id !== record.id));
      this.closeDeleteModal();
    });
  }
}