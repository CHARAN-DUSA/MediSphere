import { MsIconComponent } from '../../../../shared/components/ms-icon/ms-icon.component';
import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { ContentItem } from '../../../../core/models/admin.model';
import { AdminService } from '../../../../core/services/admin.service';
import { ToastService } from '../../../../core/services/toast.service';


@Component({
  selector: 'app-content-management',
  standalone: true,
  imports: [MsIconComponent, 
    CommonModule,
    FormsModule,
    ReactiveFormsModule
  ],
  templateUrl: './content-management.html',
  styleUrls: ['./content-management.css']
})
export class ContentManagementComponent implements OnInit {

  private adminService = inject(AdminService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  loading = signal(false);

  contentItems = signal<ContentItem[]>([]);

  editingContent = signal<ContentItem | null>(null);

  selectedContentType = 'FAQ';

  contentForm = this.fb.group({
    type: ['FAQ', Validators.required],
    title: ['', Validators.required],
    content: ['', Validators.required],
    imageUrl: [''],
    order: [0, Validators.required]
  });

  ngOnInit(): void {
    this.loadContentItems();
  }

  loadContentItems(): void {

    this.loading.set(true);

    this.adminService.getContent(
      this.selectedContentType
    ).subscribe({
      next: (response) => {
        this.contentItems.set(response.data ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Failed to load content.');
      }
    });
  }

  editContentItem(item: ContentItem): void {

    this.editingContent.set(item);

    this.contentForm.patchValue({
      type: item.type,
      title: item.title,
      content: item.content,
      imageUrl: item.imageUrl,
      order: item.order
    });
  }

  cancelContentEdit(): void {

    this.editingContent.set(null);

    this.contentForm.reset({
      type: this.selectedContentType,
      order: 0
    });
  }

  saveContentItem(): void {

    if (this.contentForm.invalid) {
      this.contentForm.markAllAsTouched();
      return;
    }

    const formValue: any = this.contentForm.value;

    const editing = this.editingContent();

    if (editing) {
      formValue.id = editing.id;
    } else {
      formValue.id = 0;
    }

    this.adminService.upsertContent(formValue)
      .subscribe({
        next: () => {

          this.toast.success(
            'Content saved successfully.'
          );

          this.cancelContentEdit();

          this.loadContentItems();
        },

        error: () => {
          this.toast.error(
            'Failed to save content.'
          );
        }
      });
  }

  deleteContentItem(id: number): void {

    if (!confirm('Delete this content item?')) {
      return;
    }

    this.adminService.deleteContent(id)
      .subscribe({
        next: () => {

          this.toast.success(
            'Content deleted successfully.'
          );

          this.loadContentItems();
        },

        error: () => {
          this.toast.error(
            'Failed to delete content.'
          );
        }
      });
  }
}