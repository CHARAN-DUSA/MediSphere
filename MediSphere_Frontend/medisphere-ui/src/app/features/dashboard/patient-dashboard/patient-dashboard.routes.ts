// src/app/features/dashboard/patient-dashboard/patient-dashboard.routes.ts
import { Routes } from '@angular/router';

export const PATIENT_DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'appointments',
    pathMatch: 'full'
  },
  {
    path: 'appointments',
    loadComponent: () =>
      import('../../patient/appointments/patient-appointments/patient-appointments')
        .then(m => m.PatientAppointmentsComponent)
  },
  {
    path: 'queue',
    loadComponent: () =>
      import('../../patient/queue/live-queue/live-queue')
        .then(m => m.LiveQueueComponent)
  },
  {
    path: 'smart',
    loadComponent: () =>
      import('../../patient/smart-finder/smart-recommendations/smart-recommendations')
        .then(m => m.SmartRecommendationsComponent)
  },
  {
    path: 'rewards',
    loadComponent: () =>
      import('../../patient/rewards/patient-rewards/patient-rewards')
        .then(m => m.PatientRewardsComponent)
  },
  {
    path: 'records',
    loadComponent: () =>
      import('../../patient/health-records/medical-records/medical-records')
        .then(m => m.MedicalRecordsComponent)
  },
  {
    path: 'favorites',
    loadComponent: () =>
      import('../../patient/favorites/favorite-doctors/favorite-doctors')
        .then(m => m.FavoriteDoctorsComponent)
  },
  {
    path: 'profile',
    loadComponent: () =>
      import('../../patient/profile/patient-profile/patient-profile')
        .then(m => m.PatientProfileComponent)
  },
  {
    path: 'notifications',
    loadComponent: () =>
      import('../../patient/notifications/patient-notifications/patient-notifications')
        .then(m => m.PatientNotificationsComponent)
  }
];