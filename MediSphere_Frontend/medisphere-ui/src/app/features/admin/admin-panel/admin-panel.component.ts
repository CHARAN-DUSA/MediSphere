import { Component, OnInit, signal, inject } from '@angular/core';
import { Router, NavigationEnd, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

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
  private router = inject(Router);

  sidebarCollapsed = signal(false);

  ngOnInit(): void {
    const isMobile = window.innerWidth <= 768;
    this.sidebarCollapsed.set(
      isMobile ? true : localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === 'true'
    );

    this.router.events.subscribe(event => {
      if (event instanceof NavigationEnd && window.innerWidth <= 768) {
        this.sidebarCollapsed.set(true);
      }
    });
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