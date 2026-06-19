import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
@Component({
  selector: 'app-doctor-dashboard-home',
  standalone: true,
  imports: [RouterModule],
  template: `
    <div class="dash-home">
      <div class="tab-content ms-card welcome-card">
        <div class="welcome-icon-wrap">
          <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24"
               fill="none" stroke="currentColor" stroke-width="1.6"
               stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
            <path d="M4.8 2.3A.3.3 0 1 0 5 2H4a2 2 0 0 0-2 2v5a6 6 0 0 0 6 6 6 6 0 0 0 6-6V4a2 2 0 0 0-2-2h-1a.3.3 0 1 0 .3.3"/>
            <path d="M8 15v1a6 6 0 0 0 6 6v0a6 6 0 0 0 6-6v-4"/>
            <circle cx="20" cy="10" r="2"/>
          </svg>
        </div>
        <h2 class="welcome-heading">Welcome back, Doctor.</h2>
        <p class="welcome-sub">
          Your patients are waiting. Review your schedule and manage
          today's appointments from your control suite.
        </p>
        <a [routerLink]="['/doctor/appointments']" class="check-btn">
          <svg xmlns="http://www.w3.org/2000/svg" width="17" height="17" viewBox="0 0 24 24"
               fill="none" stroke="currentColor" stroke-width="2"
               stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
            <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
            <line x1="16" y1="2" x2="16" y2="6"/>
            <line x1="8" y1="2" x2="8" y2="6"/>
            <line x1="3" y1="10" x2="21" y2="10"/>
          </svg>
          Check Appointments
        </a>
      </div>
    </div>
  `,
  styles: [`
    .dash-home {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 100%;
      height: 100%;
      box-sizing: border-box;
    }
    .welcome-card {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      text-align: center;
      gap: 18px;
      width: 100%;
      height: 100%;
      box-sizing: border-box;
    }
    .welcome-icon-wrap {
      width: 72px;
      height: 72px;
      border-radius: 18px;
      background: color-mix(in srgb, var(--ms-primary) 10%, transparent);
      display: flex;
      align-items: center;
      justify-content: center;
      color: var(--ms-primary);
      margin-bottom: 6px;
    }
    .welcome-heading {
      margin: 0;
      font-family: var(--font-display);
      font-size: 26px;
      font-weight: 700;
      color: var(--ms-text);
      line-height: 1.2;
    }
    .welcome-sub {
      margin: 0;
      font-size: 14px;
      color: var(--ms-text-muted);
      line-height: 1.75;
      max-width: 400px;
    }
    .check-btn {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      margin-top: 10px;
      padding: 11px 26px;
      background: var(--ms-primary);
      color: #fff;
      border-radius: 8px;
      font-size: 14px;
      font-weight: 600;
      text-decoration: none;
      transition: opacity 0.18s;
    }
    .check-btn:hover {
      opacity: 0.88;
    }
  `]
})
export class DoctorDashboardHomePage { }