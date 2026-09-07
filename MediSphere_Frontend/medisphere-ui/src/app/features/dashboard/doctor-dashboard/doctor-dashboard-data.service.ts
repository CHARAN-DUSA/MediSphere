import { Injectable, inject, signal, computed } from '@angular/core';
import { Subscription } from 'rxjs';
import { AppointmentService } from '../../../core/services/appointment.service';
import { DoctorService } from '../../../core/services/doctor.service';
import { ReviewService } from '../../../core/services/review.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { DepartmentService, Department } from '../../../core/services/department.service';
import { QueueService } from '../../../core/services/queue.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { Appointment } from '../../../core/models/appointment.model';
import { Doctor, DoctorSchedule, BlockSlotDto, VacationDto, DoctorEarningsDto } from '../../../core/models/doctor.model';
import { Review } from '../../../core/models/review.model';
import { Notification } from '../../../core/models/notification.model';
import { CreatePrescription, Prescription, PrescriptionMedicine } from '../../../core/models/prescription.model';
import { DoctorConsultation } from '../../../core/models/consultation.model';
import { PrescriptionService } from '../../../core/services/prescription.service';
import { DoctorConsultationService } from '../../../core/services/doctor-consultation.service';
import { MedicalRecord } from '../../../core/models/medical-record.model';

@Injectable()
export class DoctorDashboardDataService
{
  private apptService = inject(AppointmentService);
  private doctorService = inject(DoctorService);
  private reviewService = inject(ReviewService);
  private deptService = inject(DepartmentService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);
  private queueService = inject(QueueService);
  private consultationService = inject(DoctorConsultationService);


  private prescriptionService = inject(PrescriptionService);
  signalRService = inject(SignalRService);

  appointments = signal<Appointment[]>([]);
  doctorProfile = signal<Doctor | null>(null);
  earnings = signal<DoctorEarningsDto | null>(null);
  reviews = signal<Review[]>([]);
  departments = signal<Department[]>([]);
  notifications = signal<Notification[]>([]);
  showNotificationDropdown = signal(false);
  selectedPrescription = signal<Prescription | null>(null);

  unreadNotificationsCount = computed(() => this.notifications().filter(n => !n.isRead).length);
  recentNotifications = computed(() => this.notifications().slice(0, 5));

  liveQueue = signal<Appointment[]>([]);
  currentlyServing = signal<Appointment | null>(null);
  liveQueueWaiting = computed(() => this.liveQueue().filter(a => a.queueStatus === 'Waiting'));

  activeConsultation =
    signal<DoctorConsultation | null>(null);

  consultationLoading = signal(false);

  consultationDiagnosis = '';

  consultationClinicalNotes = '';

  consultationInstructions = '';

  consultationFollowUpDate = '';

  prescriptionMedicines =
    signal<PrescriptionMedicine[]>([
      {
        medicineName: '',
        dosage: '',
        frequency: '',
        duration: '',
        route: '',
        instructions: ''
      }
    ]);

  private subs = new Subscription();
  private initialized = false;

  doctorIdNum = computed(() =>
  {
    const id = this.auth.referenceId();
    return typeof id === 'number' ? id : Number(id);
  });

  todayAppts = computed(() =>
  {
    const todayStr = new Date().toISOString().split('T')[0];
    return this.appointments().filter(a => a.appointmentDate.startsWith(todayStr));
  });

  init(): void
  {
    if (this.initialized) return;
    this.initialized = true;

    this.deptService.getAll().subscribe(r => this.departments.set(r.data));
    this.loadDoctorProfile();
    this.loadAppointments();
    this.loadDoctorEarnings();
    this.initQueueTracking();
    this.initNotifications();
  }

  destroy(): void
  {
    this.subs.unsubscribe();
    const docId = this.auth.referenceId();
    if (docId) this.signalRService.leaveDoctorQueueRoom(docId);
    this.initialized = false;
  }

  initNotifications(): void
  {
    this.loadNotifications();
    const token = this.auth.getToken() || '';
    this.signalRService.initNotificationConnection(token);

    this.subs.add(
      this.signalRService.notificationReceived$.subscribe(notification =>
      {
        this.notifications.update(list =>
        {
          const exists = list.some(n => n.id === notification.id);
          if (exists) return list;
          return [notification, ...list];
        });
      })
    );

    this.subs.add(
      this.signalRService.notificationsUpdated$.subscribe(() => this.loadNotifications())
    );

    this.subs.add(
      this.signalRService.notificationRemoved$.subscribe(id =>
      {
        this.notifications.update(list => list.filter(n => n.id !== id));
      })
    );
  }

  loadNotifications(): void
  {
    this.doctorService.getNotifications().subscribe(r => this.notifications.set(r.data));
  }

  markNotificationAsRead(id: number): void
  {
    this.doctorService.markNotificationAsRead(id).subscribe(() =>
    {
      this.notifications.update(list => list.filter(n => n.id !== id));
    });
  }

  markAllNotificationsAsRead(): void
  {
    this.doctorService.markAllNotificationsAsRead().subscribe(() =>
    {
      this.notifications.set([]);
    });
  }

  toggleNotificationDropdown(): void
  {
    this.showNotificationDropdown.update(v => !v);
  }

  closeNotificationDropdown(): void
  {
    this.showNotificationDropdown.set(false);
  }

  loadDoctorEarnings(): void
  {
    const docId = this.auth.referenceId();
    if (!docId) return;
    this.doctorService.getDoctorEarnings(docId).subscribe({
      next: (res) => { if (res.data) this.earnings.set(res.data); }
    });
  }

  initQueueTracking(): void
  {
    const docId = this.auth.referenceId();
    if (!docId) return;
    const token = this.auth.getToken() || '';
    this.signalRService.initQueueConnection(token);
    this.signalRService.joinDoctorQueueRoom(docId);
    this.loadLiveQueue();
    this.subs.add(
      this.signalRService.queueUpdated$.subscribe(() =>
      {
        this.loadLiveQueue();
      })
    );
  }

  loadLiveQueue(): void
  {
    const docId = this.auth.referenceId();
    if (!docId) return;
    this.queueService.getDoctorQueue(docId).subscribe(res =>
    {
      if (res.data)
      {
        this.liveQueue.set(res.data);
        this.currentlyServing.set(res.data.find(a => a.queueStatus === 'InConsultation') || null);
      }
    });
  }

  callNext(): void
  {
    const docId = this.auth.referenceId();
    this.queueService.callNextPatient(docId).subscribe({
      next: (res) =>
      {
        if (res.data)
        {
          this.toast.success(`Called patient: ${res.data.patientName}`);
          this.loadLiveQueue();
        } else
        {
          this.toast.info('No more patients waiting in queue.');
        }
      },
      error: () => this.toast.error('Failed to call next patient.')
    });
  }

  changeQueueStatus(appointmentId: number, status: string): void
  {
    this.queueService.updateQueueStatus(appointmentId, status).subscribe({
      next: () =>
      {
        this.toast.success(`Queue status updated to ${status}.`);
        this.loadLiveQueue();
      },
      error: () => this.toast.error('Failed to update queue status.')
    });
  }

  loadDoctorProfile(): void
  {
    const docId = this.auth.referenceId();
    this.doctorService.getDoctorById(docId).subscribe(r =>
    {
      if (r.data)
      {
        this.doctorProfile.set(r.data);
        this.reviewService.getDoctorReviews(docId).subscribe(revs => this.reviews.set(revs.data));
      }
    });
  }

  loadAppointments(): void
  {
    const docId = this.auth.referenceId();
    this.apptService.getAllAppointments(1, 100, docId).subscribe(r =>
    {
      this.appointments.set(r.data.items);
      this.loadDoctorEarnings();
    });
  }

  updateStatus(id: number, status: string): void
  {
    if (!status) return;
    this.apptService.updateStatus(id, status).subscribe(() =>
    {
      this.toast.success('Appointment status changed to ' + status + '.');
      this.appointments.update(list => list.map(a => a.id === id ? { ...a, status } : a));
      this.loadDoctorEarnings();
    });
  }

  openMedicalRecord(record: MedicalRecord): void
  {
    const consultation = this.activeConsultation();

    if (!consultation) return;

    const appointmentId = consultation.appointment.id;

    this.consultationService
      .getMedicalRecordFile(record.id, appointmentId)
      .subscribe({
        next: (blob: Blob) =>
        {
          const url = window.URL.createObjectURL(blob);

          window.open(url, '_blank');

          setTimeout(() =>
          {
            window.URL.revokeObjectURL(url);
          }, 60000);
        },
        error: () =>
        {
          this.toast.error('Unable to open the medical record.');
        }
      });
  }
  openConsultationModal(
    appointment: Appointment
  ): void
  {

    this.consultationLoading.set(true);

    this.consultationService
      .getConsultation(appointment.id)
      .subscribe({

        next: response =>
        {

          if (!response.data)
          {
            this.toast.error(
              'Consultation data could not be loaded.'
            );
            this.consultationLoading.set(false);
            return;
          }

          this.activeConsultation.set(
            response.data
          );

          this.consultationDiagnosis = '';

          this.consultationClinicalNotes =
            appointment.notes || '';

          this.consultationInstructions = '';

          this.consultationFollowUpDate = '';

          this.prescriptionMedicines.set([
            {
              medicineName: '',
              dosage: '',
              frequency: '',
              duration: '',
              route: '',
              instructions: ''
            }
          ]);

          this.consultationLoading.set(false);
        },

        error: error =>
        {

          console.error(
            'Failed to load consultation',
            error
          );

          this.consultationLoading.set(false);

          this.toast.error(
            'Unable to open consultation.'
          );

        }

      });
  }

  consultationNotes(id: any, arg1: string, consultationNotes: any)
  {
    throw new Error('Method not implemented.');
  }

  submitReviewResponse(reviewId: number, text: string): void
  {
    if (!text.trim())
    {
      this.toast.warning('Response text cannot be empty.');
      return;
    }
    this.reviewService.respondToReview(reviewId, text).subscribe({
      next: () =>
      {
        this.toast.success('Your response has been saved.');
        const docId = this.auth.referenceId();
        this.reviewService.getDoctorReviews(docId).subscribe(revs => this.reviews.set(revs.data));
      },
      error: () => this.toast.error('Failed to submit response.')
    });
  }

  onUpdateProfile(dto: unknown): void
  {
    const docId = this.auth.referenceId();
    this.doctorService.updateDoctor(docId, dto as never).subscribe(() =>
    {
      this.toast.success('Profile bio details updated.');
      this.loadDoctorProfile();
    });
  }

  uploadPhoto(file: File): void
  {
    const docId = this.auth.referenceId();
    this.doctorService.uploadProfileImage(docId, file).subscribe(() =>
    {
      this.toast.success('Profile photo uploaded.');
      this.loadDoctorProfile();
    });
  }

  onSaveSchedule(schedules: DoctorSchedule[]): void
  {
    const docId = this.auth.referenceId();
    this.doctorService.updateSchedule(docId, schedules).subscribe(() =>
    {
      this.toast.success('Standard work hours schedule updated.');
    });
  }

  onBlockSlot(dto: BlockSlotDto): void
  {
    const docId = this.auth.referenceId();
    this.doctorService.blockSlot(docId, dto).subscribe(() =>
    {
      this.toast.success('Requested slot blocked successfully.');
      this.loadAppointments();
    });
  }

  onSetVacation(dto: VacationDto): void
  {
    const docId = this.auth.referenceId();
    this.doctorService.setVacation(docId, dto).subscribe(() =>
    {
      this.toast.success('Vacation periods set successfully.');
      this.loadDoctorProfile();
    });
  }
  addPrescriptionMedicine(): void
  {
    this.prescriptionMedicines.update(
      medicines => [
        ...medicines,
        {
          medicineName: '',
          dosage: '',
          frequency: '',
          duration: '',
          route: '',
          instructions: ''
        }
      ]
    );
  }
  removePrescriptionMedicine(
    index: number
  ): void
  {

    this.prescriptionMedicines.update(
      medicines =>
        medicines.filter(
          (_, i) => i !== index
        )
    );

  }
  savePrescriptionAndComplete(): void
  {

    const consultation =
      this.activeConsultation();

    if (!consultation)
    {
      return;
    }

    const medicines =
      this.prescriptionMedicines()
        .filter(
          medicine =>
            medicine.medicineName.trim().length > 0
        );

    if (!this.consultationDiagnosis.trim())
    {
      this.toast.warning(
        'Please enter the diagnosis.'
      );
      return;
    }

    if (medicines.length === 0)
    {
      this.toast.warning(
        'Please add at least one medicine.'
      );
      return;
    }

    const prescription: CreatePrescription = {

      appointmentId:
        consultation.appointment.id,

      diagnosis:
        this.consultationDiagnosis.trim(),

      clinicalNotes:
        this.consultationClinicalNotes.trim(),

      instructions:
        this.consultationInstructions.trim(),

      followUpDate:
        this.consultationFollowUpDate
          ? this.consultationFollowUpDate
          : null,

      medicines: medicines.map(
        medicine => ({
          medicineName:
            medicine.medicineName.trim(),

          dosage:
            medicine.dosage.trim(),

          frequency:
            medicine.frequency.trim(),

          duration:
            medicine.duration.trim(),

          route:
            medicine.route.trim(),

          instructions:
            medicine.instructions.trim()
        })
      )

    };

    this.prescriptionService
      .create(prescription)
      .subscribe({

        next: () =>
        {

          this.apptService
            .updateStatus(
              consultation.appointment.id,
              'Completed',
              this.consultationClinicalNotes
            )
            .subscribe({

              next: () =>
              {

                this.toast.success(
                  'Prescription saved and consultation completed.'
                );

                this.appointments.update(
                  list =>
                    list.map(a =>
                      a.id ===
                        consultation.appointment.id
                        ? {
                          ...a,
                          status: 'Completed',
                          notes:
                            this.consultationClinicalNotes
                        }
                        : a
                    )
                );

                this.loadDoctorEarnings();

                this.activeConsultation.set(null);

              },

              error: error =>
              {

                console.error(
                  'Failed to complete appointment',
                  error
                );

                this.toast.error(
                  'Prescription was saved, but appointment completion failed.'
                );

              }

            });

        },

        error: error =>
        {

          console.error(
            'Failed to save prescription',
            error
          );

          this.toast.error(
            error?.error?.message ||
            'Failed to save prescription.'
          );

        }

      });

  }
  viewPrescription(prescription: Prescription): void
  {
    this.selectedPrescription.set(prescription);
  }
  closePrescription(): void
  {
    this.selectedPrescription.set(null);
  }

}
