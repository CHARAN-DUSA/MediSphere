import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

export type MsIconSize = 'sm' | 'md' | 'lg' | 'xl';

@Component({
  selector: 'ms-icon',
  standalone: true,
  imports: [MatIconModule],
  template: `
    <mat-icon
      class="ms-icon"
      [attr.aria-label]="ariaLabel || null"
      [attr.aria-hidden]="ariaLabel ? null : 'true'"
    >{{ name }}</mat-icon>
  `,
  styles: [`
    :host {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      line-height: 1;
      vertical-align: middle;
      flex-shrink: 0;
    }
    .ms-icon {
      width: 1em;
      height: 1em;
      font-size: inherit;
      overflow: visible;
    }
    :host(.ms-icon-sm) { font-size: 1rem; }
    :host(.ms-icon-md) { font-size: 1.25rem; }
    :host(.ms-icon-lg) { font-size: 1.5rem; }
    :host(.ms-icon-xl) { font-size: 2rem; }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class.ms-icon-sm]': 'size === "sm"',
    '[class.ms-icon-md]': 'size === "md"',
    '[class.ms-icon-lg]': 'size === "lg"',
    '[class.ms-icon-xl]': 'size === "xl"',
  }
})
export class MsIconComponent {
  @Input({ required: true }) name!: string;
  @Input() size: MsIconSize = 'md';
  @Input() ariaLabel?: string;
}
