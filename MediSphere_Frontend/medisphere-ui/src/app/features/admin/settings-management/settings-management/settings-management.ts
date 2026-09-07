import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SystemSetting } from '../../../../core/models/admin.model';
import { AdminService } from '../../../../core/services/admin.service';
import { ToastService } from '../../../../core/services/toast.service';



@Component({
  selector: 'app-settings-management',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  templateUrl: './settings-management.html',
  styleUrls: ['./settings-management.css']
})
export class SettingsManagementComponent implements OnInit {

  private adminService = inject(AdminService);
  private toast = inject(ToastService);

  loading = signal(false);

  settings = signal<SystemSetting[]>([]);

  ngOnInit(): void {
    this.loadSettings();
  }

  loadSettings(): void {

    this.loading.set(true);

    this.adminService
      .getSettings()
      .subscribe({
        next: (response) => {

          this.settings.set(
            response.data ?? []
          );

          this.loading.set(false);
        },

        error: () => {

          this.loading.set(false);

          this.toast.error(
            'Failed to load settings.'
          );
        }
      });
  }

  updateSetting(
    setting: SystemSetting
  ): void {

    this.adminService
      .updateSetting(setting)
      .subscribe({
        next: () => {

          this.toast.success(
            `${setting.key} updated successfully.`
          );

          this.loadSettings();
        },

        error: () => {

          this.toast.error(
            'Failed to save setting.'
          );
        }
      });
  }
}