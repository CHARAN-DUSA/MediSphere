import { Routes } from '@angular/router';

export const DOCTOR_DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('../../../doctors/pages/doctor-dashboard-home.page')
        .then(m => m.DoctorDashboardHomePage)
  },
  {
    path: 'appointments',
    loadComponent: () =>
      import('../../../doctors/pages/doctor-appointments.page')
        .then(m => m.DoctorAppointmentsPage)
  },
  {
    path: 'patients',
    loadComponent: () =>
      import('../../../doctors/pages/doctor-patients.page')
        .then(m => m.DoctorPatientsPage)
  },
  {
    path: 'schedule',
    loadComponent: () =>
      import('../../../doctors/pages/doctor-schedule.page')
        .then(m => m.DoctorSchedulePage)
  },
  {
    path: 'revenue',
    loadComponent: () =>
      import('../../../doctors/pages/doctor-revenue.page')
        .then(m => m.DoctorRevenuePage)
  },
  {
    path: 'reviews',
    loadComponent: () =>
      import('../../../doctors/pages/doctor-reviews.page')
        .then(m => m.DoctorReviewsPage)
  },
  {
    path: 'profile',
    loadComponent: () =>
      import('../../../doctors/pages/doctor-profile.page')
        .then(m => m.DoctorProfilePage)
  },
  {
    path: 'notifications',
    loadComponent: () =>
      import('../../../doctors/doctor-notifications/doctor-notifications')
        .then(m => m.DoctorNotificationsComponent)
  }
];
