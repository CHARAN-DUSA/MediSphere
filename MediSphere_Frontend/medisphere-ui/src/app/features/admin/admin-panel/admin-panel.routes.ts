import { Routes } from '@angular/router';

export const ADMIN_PANEL_ROUTES: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('../dashboard/admin-dashboard/admin-dashboard')
        .then(m => m.AdminDashboardComponent)
  },
  {
    path: 'doctors',
    loadComponent: () =>
      import('../doctor-management/doctor-management/doctor-management')
        .then(m => m.DoctorManagementComponent)
  },
  {
    path: 'patients',
    loadComponent: () =>
      import('../patient-management/patient-management/patient-management')
        .then(m => m.PatientManagementComponent)
  },
  {
    path: 'department-management',
    loadComponent: () =>
      import('../department-management/department-management/department-management')
        .then(m => m.DepartmentManagementComponent)
  },
  {
    path: 'departments',
    redirectTo: 'department-management',
    pathMatch: 'full'
  },
  {
    path: 'reviews',
    loadComponent: () =>
      import('../review-moderation/review-moderation/review-moderation')
        .then(m => m.ReviewModerationComponent)
  },
  {
    path: 'doctor-analytics',
    loadComponent: () =>
      import('../reports/doctor-analytics/doctor-analytics')
        .then(m => m.DoctorAnalyticsComponent)
  },
  {
    path: 'patient-analytics',
    loadComponent: () =>
      import('../reports/patient-analytics/patient-analytics')
        .then(m => m.PatientAnalyticsComponent)
  },
  {
    path: 'appointment-analytics',
    loadComponent: () =>
      import('../reports/appointment-analytics/appointment-analytics')
        .then(m => m.AppointmentAnalyticsComponent)
  },
  {
    path: 'revenue-report',
    loadComponent: () =>
      import('../reports/revenue-report/revenue-report')
        .then(m => m.RevenueReportComponent)
  },
  {
    path: 'payout-analytics',
    loadComponent: () =>
      import('../reports/payout-analytics/payout-analytics')
        .then(m => m.PayoutAnalyticsComponent)
  },
  {
    path: 'settings',
    loadComponent: () =>
      import('../settings-management/settings-management/settings-management')
        .then(m => m.SettingsManagementComponent)
  },
  {
    path: 'content',
    loadComponent: () =>
      import('../content-management/content-management/content-management')
        .then(m => m.ContentManagementComponent)
  },
  {
    path: 'broadcast',
    loadComponent: () =>
      import('../broadcast-management/broadcast-management/broadcast-management')
        .then(m => m.BroadcastManagementComponent)
  },
  // Legacy nested paths → flat URLs
  {
    path: 'analytics/doctors',
    redirectTo: 'doctor-analytics',
    pathMatch: 'full'
  },
  {
    path: 'analytics/patients',
    redirectTo: 'patient-analytics',
    pathMatch: 'full'
  },
  {
    path: 'analytics/appointments',
    redirectTo: 'appointment-analytics',
    pathMatch: 'full'
  },
  {
    path: 'reports/revenue',
    redirectTo: 'revenue-report',
    pathMatch: 'full'
  },
  {
    path: 'reports/payouts',
    redirectTo: 'payout-analytics',
    pathMatch: 'full'
  }
];
