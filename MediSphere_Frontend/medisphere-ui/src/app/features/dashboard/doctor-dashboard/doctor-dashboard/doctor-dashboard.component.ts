import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';
import { Component, OnInit, OnDestroy, inject, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { DoctorDashboardDataService } from '../doctor-dashboard-data.service';
import { DoctorStatsComponent } from '../../../doctors/doctor-stats/doctor-stats';

@Component({
  selector: 'app-doctor-dashboard',
  standalone: true,
  imports: [MsIconComponent,
    CommonModule,
    FormsModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
    DoctorStatsComponent
  ],
  providers: [DoctorDashboardDataService],
  templateUrl: './doctor-dashboard.component.html',
  styleUrls: ['./doctor-dashboard.component.css']
})
export class DoctorDashboardComponent implements OnInit, OnDestroy
{
  readonly data = inject(DoctorDashboardDataService);

  @HostListener('document:click')
  onDocumentClick()
  {
    this.data.closeNotificationDropdown();
  }

  ngOnInit(): void
  {
    this.data.init();
  }

  ngOnDestroy(): void
  {
    this.data.destroy();
  }
}
