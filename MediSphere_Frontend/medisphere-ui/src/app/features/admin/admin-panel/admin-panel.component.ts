import { Component, OnInit, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { MsIconComponent } from '../../../shared/components/ms-icon/ms-icon.component';

const SIDEBAR_COLLAPSED_KEY = 'ms-admin-sidebar-collapsed';

@Component({
  selector: 'app-admin-panel',
  standalone: true,
  imports: [
    MsIconComponent,
    RouterLink,
    RouterLinkActive,
    RouterOutlet
  ],
  templateUrl: './admin-panel.component.html',
  styleUrls: ['./admin-panel.component.css']
})
export class AdminPanelComponent implements OnInit {

  sidebarCollapsed = signal(false);

  ngOnInit(): void {
    this.sidebarCollapsed.set(
      localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === 'true'
    );
  }

  toggleSidebar(): void {
    this.sidebarCollapsed.update(collapsed => {
      const next = !collapsed;
      localStorage.setItem(
        SIDEBAR_COLLAPSED_KEY,
        String(next)
      );
      return next;
    });
  }

}