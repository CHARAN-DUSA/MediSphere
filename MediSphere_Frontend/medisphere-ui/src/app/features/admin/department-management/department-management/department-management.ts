import { resolveDeptIcon } from '../../../../shared/utils/dept-icon.util';
import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { DepartmentService, Department } from '../../../../core/services/department.service';
import { ToastService } from '../../../../core/services/toast.service';



@Component({
  selector: 'app-department-management',
  standalone: true,
  imports: [MsIconComponent, 
    CommonModule,
    ReactiveFormsModule
  ],
  templateUrl: './department-management.html',
  styleUrls: ['./department-management.css']
})
export class DepartmentManagementComponent implements OnInit {
  readonly resolveDeptIcon = resolveDeptIcon;

  private departmentService = inject(DepartmentService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  loading = signal(false);

  departments = signal<Department[]>([]);

  editingDept = signal<Department | null>(null);

  deptForm = this.fb.group({
    name: ['', Validators.required],
    description: ['', Validators.required],
    iconUrl: ['']
  });

  ngOnInit(): void {
    this.loadDepartments();
  }

  loadDepartments(): void {

    this.loading.set(true);

    this.departmentService.getAll().subscribe({
      next: (response) => {

        this.departments.set(response.data ?? []);

        this.loading.set(false);
      },
      error: () => {

        this.loading.set(false);

        this.toast.error(
          'Failed to load departments.'
        );
      }
    });
  }

  editDepartment(department: Department): void {

    this.editingDept.set(department);

    this.deptForm.patchValue({
      name: department.name,
      description: department.description,
      iconUrl: department.iconUrl
    });
  }

  cancelDeptEdit(): void {

    this.editingDept.set(null);

    this.deptForm.reset();
  }

  saveDepartment(): void {

    if (this.deptForm.invalid) {
      this.deptForm.markAllAsTouched();
      return;
    }

    const formData = this.deptForm.value;

    const editing = this.editingDept();

    if (editing) {

      this.departmentService
        .update(editing.id, formData)
        .subscribe({
          next: () => {

            this.toast.success(
              'Department updated successfully.'
            );

            this.cancelDeptEdit();

            this.loadDepartments();
          },
          error: () => {

            this.toast.error(
              'Failed to update department.'
            );
          }
        });

      return;
    }

    this.departmentService
      .create(formData)
      .subscribe({
        next: () => {

          this.toast.success(
            'Department created successfully.'
          );

          this.deptForm.reset();

          this.loadDepartments();
        },
        error: () => {

          this.toast.error(
            'Failed to create department.'
          );
        }
      });
  }

  deleteDepartment(id: number): void {

    const confirmed = confirm(
      'Delete this department?'
    );

    if (!confirmed) {
      return;
    }

    this.departmentService
      .delete(id)
      .subscribe({
        next: () => {

          this.toast.success(
            'Department deleted successfully.'
          );

          this.loadDepartments();
        },
        error: () => {

          this.toast.error(
            'Failed to delete department.'
          );
        }
      });
  }
}