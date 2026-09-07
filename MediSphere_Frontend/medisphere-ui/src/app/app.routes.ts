import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent) },
  { path: 'login', loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },
  { path: 'register', loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent) },
  { path: 'forgot-password', loadComponent: () => import('./features/auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent) },
  { path: 'reset-password', loadComponent: () => import('./features/auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent) },
  { path: 'register-doctor', loadComponent: () => import('./features/auth/register-doctor/register-doctor.component').then(m => m.RegisterDoctorComponent) },
  { path: 'doctors', loadComponent: () => import('./features/doctors/doctor-list/doctor-list.component').then(m => m.DoctorListComponent) },
  { path: 'doctors/:id', loadComponent: () => import('./features/doctors/doctor-detail/doctor-detail.component').then(m => m.DoctorDetailComponent) },
  {
    path: 'appointments',
    canActivate: [authGuard],
    children: [
      { path: 'book/:doctorId', loadComponent: () => import('./features/appointments/book-appointment/book-appointment.component').then(m => m.BookAppointmentComponent) },
      { path: 'history', loadComponent: () => import('./features/appointments/appointment-history/appointment-history.component').then(m => m.AppointmentHistoryComponent) }
    ]
  },
  // {
  //   path: 'dashboard',
  //   canActivate: [authGuard],
  //   loadComponent: () => import('./features/dashboard/patient-dashboard/patient-dashboard.component').then(m => m.PatientDashboardComponent)
  // },
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard('Admin')],
    loadComponent: () =>
      import('./features/admin/admin-panel/admin-panel.component')
        .then(m => m.AdminPanelComponent),
    loadChildren: () =>
      import('./features/admin/admin-panel/admin-panel.routes')
        .then(m => m.ADMIN_PANEL_ROUTES)
  },
  {
    path: 'doctor',
    canActivate: [authGuard, roleGuard('Doctor')],
    loadComponent: () =>
      import('./features/dashboard/doctor-dashboard/doctor-dashboard/doctor-dashboard.component')
        .then(m => m.DoctorDashboardComponent),
    loadChildren: () =>
      import('./features/dashboard/doctor-dashboard/doctor-dashboard/doctor-dashboard.routes')
        .then(m => m.DOCTOR_DASHBOARD_ROUTES)
  },
  {
    path: 'doctor-dashboard',
    redirectTo: 'doctor/dashboard',
    pathMatch: 'full'
  },
  // In your existing app.routes.ts, add:
  {
    path: 'patient',
    canActivate: [authGuard, roleGuard('Patient')],
    loadComponent: () =>
      import('./features/dashboard/patient-dashboard/patient-dashboard/patient-dashboard')
        .then(m => m.PatientDashboardComponent),
    loadChildren: () =>
      import('./features/dashboard/patient-dashboard/patient-dashboard.routes')
        .then(m => m.PATIENT_DASHBOARD_ROUTES)
  },
  {
    path: 'consultation/:appointmentId',
    canActivate: [authGuard],
    loadComponent: () => import('./features/telemedicine/telemedicine-page/telemedicine-page.component').then(m => m.TelemedicinePageComponent)
  },
  { path: '**', redirectTo: '' }
];
