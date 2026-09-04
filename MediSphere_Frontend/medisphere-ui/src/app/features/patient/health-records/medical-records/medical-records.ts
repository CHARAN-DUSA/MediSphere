// src/app/features/patient/health-records/medical-records/medical-records.ts

import {
  Component,
  inject,
  OnInit,
  OnDestroy,
  signal
} from '@angular/core';

import {
  NgFor,
  NgIf,
  DatePipe
} from '@angular/common';

import {
  ReactiveFormsModule,
  FormBuilder
} from '@angular/forms';

import {
  DomSanitizer,
  SafeResourceUrl
} from '@angular/platform-browser';

import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';

import { MedicalRecord } from '../../../../core/models/medical-record.model';

import { AuthService } from '../../../../core/services/auth.service';
import { PatientService } from '../../../../core/services/patient.service';
import { ToastService } from '../../../../core/services/toast.service';


@Component({
  selector: 'app-medical-records',
  standalone: true,

  imports: [
    MsIconComponent,
    NgFor,
    NgIf,
    DatePipe,
    ReactiveFormsModule
  ],

  templateUrl: './medical-records.html',

  styleUrls: [
    './medical-records.css'
  ]
})
export class MedicalRecordsComponent
  implements OnInit, OnDestroy {


  // =========================================================
  // Services
  // =========================================================

  private patientService = inject(PatientService);

  private auth = inject(AuthService);

  private toast = inject(ToastService);

  private fb = inject(FormBuilder);

  private sanitizer = inject(DomSanitizer);



  // =========================================================
  // Records
  // =========================================================

  records = signal<MedicalRecord[]>([]);

  uploading = signal(false);

  selectedFile: File | null = null;



  // =========================================================
  // Document Viewer
  // =========================================================

  activeRecord: MedicalRecord | null = null;

  activeFileUrl: SafeResourceUrl | null = null;

  rawBlobUrl: string | null = null;

  loadingViewer = signal(false);

  isPdf = false;

  isImage = false;

  isUnsupported = false;



  // =========================================================
  // Delete
  // =========================================================

  recordToDelete =
    signal<MedicalRecord | null>(null);



  // =========================================================
  // Upload Form
  // =========================================================

  uploadForm = this.fb.group({
    description: ['']
  });



  // =========================================================
  // Init
  // =========================================================

  ngOnInit(): void {

    const patientId =
      this.auth.referenceId();

    this.patientService
      .getMedicalRecords(patientId)
      .subscribe({

        next: response => {

          this.records.set(
            response.data
          );

        },

        error: () => {

          this.toast.error(
            'Unable to load medical records.'
          );

        }

      });

  }



  // =========================================================
  // Destroy
  // =========================================================

  ngOnDestroy(): void {

    this.cleanupBlobUrl();

  }



  // =========================================================
  // File Selection
  // =========================================================

  onFileSelected(event: Event): void {

    const input =
      event.target as HTMLInputElement;

    this.selectedFile =
      input.files &&
        input.files.length > 0
        ? input.files[0]
        : null;

  }



  // =========================================================
  // Upload
  // =========================================================

  onUploadRecord(): void {

    if (
      !this.selectedFile ||
      this.uploadForm.invalid
    ) {
      return;
    }


    this.uploading.set(true);


    this.patientService
      .uploadMedicalRecord(
        this.selectedFile,
        this.uploadForm.value.description ?? ''
      )
      .subscribe({

        next: response => {

          this.uploading.set(false);

          this.selectedFile = null;

          this.uploadForm.reset();

          this.toast.success(
            'Record uploaded.'
          );


          this.records.update(
            list => [
              response.data,
              ...list
            ]
          );

        },


        error: () => {

          this.uploading.set(false);

          this.toast.error(
            'Unable to upload medical record.'
          );

        }

      });

  }



  // =========================================================
  // View Record
  // =========================================================

  viewRecord(
    record: MedicalRecord
  ): void {

    this.cleanupBlobUrl();

    this.activeRecord = record;

    this.loadingViewer.set(true);


    const extension =
      (
        record.fileName
          .split('.')
          .pop() || ''
      ).toLowerCase();


    this.isPdf =
      extension === 'pdf';


    this.isImage = [
      'jpg',
      'jpeg',
      'png',
      'gif',
      'webp'
    ].includes(extension);


    this.isUnsupported =
      !this.isPdf &&
      !this.isImage;


    this.patientService
      .getMedicalRecordFileBlob(record.id)
      .subscribe({

        next: blob => {

          this.rawBlobUrl =
            URL.createObjectURL(blob);


          this.activeFileUrl =
            this.sanitizer
              .bypassSecurityTrustResourceUrl(
                this.rawBlobUrl
              );


          this.loadingViewer.set(false);

        },


        error: () => {

          this.loadingViewer.set(false);

          this.toast.error(
            'Unable to load document preview.'
          );

        }

      });

  }



  // =========================================================
  // Close Viewer
  // =========================================================

  closeViewer(): void {

    this.cleanupBlobUrl();

    this.activeRecord = null;

    this.activeFileUrl = null;

    this.isPdf = false;

    this.isImage = false;

    this.isUnsupported = false;

  }



  // =========================================================
  // Blob Cleanup
  // =========================================================

  private cleanupBlobUrl(): void {

    if (this.rawBlobUrl) {

      URL.revokeObjectURL(
        this.rawBlobUrl
      );

      this.rawBlobUrl = null;

    }

  }



  // =========================================================
  // Open Delete Confirmation
  // =========================================================

  requestDeleteRecord(
    record: MedicalRecord
  ): void {

    this.recordToDelete.set(
      record
    );

  }



  // =========================================================
  // Close Delete Confirmation
  // =========================================================

  closeDeleteModal(): void {

    this.recordToDelete.set(
      null
    );

  }



  // =========================================================
  // Confirm Delete
  // =========================================================

  confirmDeleteRecord(): void {

    const record =
      this.recordToDelete();


    if (!record) {
      return;
    }


    this.patientService
      .deleteMedicalRecord(record.id)
      .subscribe({

        next: () => {

          this.toast.success(
            'Document deleted.'
          );


          this.records.update(
            list =>
              list.filter(
                item =>
                  item.id !== record.id
              )
          );


          this.closeDeleteModal();

        },


        error: () => {

          this.toast.error(
            'Unable to delete document.'
          );

        }

      });

  }

}