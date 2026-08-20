import { Component, Input } from '@angular/core';

export type IconName =
  | 'shield-check'
  | 'shield-alert'
  | 'shield-x'
  | 'mail'
  | 'link'
  | 'alert-triangle'
  | 'upload'
  | 'paperclip'
  | 'clock'
  | 'chart-bar'
  | 'sun'
  | 'moon'
  | 'x'
  | 'cpu'
  | 'chevron-down';

@Component({
  selector: 'app-icon',
  standalone: true,
  template: `
    <svg
      [attr.width]="size"
      [attr.height]="size"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="1.8"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
    >
      @switch (name) {
        @case ('shield-check') {
          <path d="M12,2 L20,6 L20,11 C20,16 16.5,19.5 12,22 C7.5,19.5 4,16 4,11 L4,6 Z" />
          <path d="M8.5 12.5 11 15 15.5 9.5" />
        }
        @case ('shield-alert') {
          <path d="M12,2 L20,6 L20,11 C20,16 16.5,19.5 12,22 C7.5,19.5 4,16 4,11 L4,6 Z" />
          <line x1="12" y1="8" x2="12" y2="13" />
          <circle cx="12" cy="16.3" r="0.6" fill="currentColor" />
        }
        @case ('shield-x') {
          <path d="M12,2 L20,6 L20,11 C20,16 16.5,19.5 12,22 C7.5,19.5 4,16 4,11 L4,6 Z" />
          <line x1="9.3" y1="9.3" x2="14.7" y2="14.7" />
          <line x1="14.7" y1="9.3" x2="9.3" y2="14.7" />
        }
        @case ('mail') {
          <rect x="3" y="5" width="18" height="14" rx="2" />
          <polyline points="3,7 12,13 21,7" />
        }
        @case ('link') {
          <path d="M10 13a5 5 0 0 0 7.07 0l1.93-1.93a5 5 0 0 0-7.07-7.07L10.5 5.5" />
          <path d="M14 11a5 5 0 0 0-7.07 0L4.99 12.93a5 5 0 0 0 7.07 7.07L13.5 18.5" />
        }
        @case ('alert-triangle') {
          <path d="M12 3 2 21 22 21Z" />
          <line x1="12" y1="9.5" x2="12" y2="14" />
          <circle cx="12" cy="17.3" r="0.6" fill="currentColor" />
        }
        @case ('upload') {
          <line x1="12" y1="3" x2="12" y2="13" />
          <polyline points="7,8 12,3 17,8" />
          <path d="M4 17v2a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-2" />
        }
        @case ('paperclip') {
          <path d="M17 7 8 16a3 3 0 0 0 4.24 4.24L20 12.5a5 5 0 0 0-7.07-7.07L6 12.36" />
        }
        @case ('clock') {
          <circle cx="12" cy="12" r="9" />
          <polyline points="12,7 12,12 15.5,14" />
        }
        @case ('chart-bar') {
          <line x1="5" y1="20" x2="5" y2="12" />
          <line x1="12" y1="20" x2="12" y2="6" />
          <line x1="19" y1="20" x2="19" y2="15" />
        }
        @case ('sun') {
          <circle cx="12" cy="12" r="4" />
          <line x1="12" y1="2" x2="12" y2="4.5" />
          <line x1="12" y1="19.5" x2="12" y2="22" />
          <line x1="2" y1="12" x2="4.5" y2="12" />
          <line x1="19.5" y1="12" x2="22" y2="12" />
          <line x1="4.9" y1="4.9" x2="6.6" y2="6.6" />
          <line x1="17.4" y1="17.4" x2="19.1" y2="19.1" />
          <line x1="4.9" y1="19.1" x2="6.6" y2="17.4" />
          <line x1="17.4" y1="6.6" x2="19.1" y2="4.9" />
        }
        @case ('moon') {
          <path d="M20 14.5A8 8 0 1 1 9.5 4 6.5 6.5 0 0 0 20 14.5Z" />
        }
        @case ('x') {
          <line x1="5" y1="5" x2="19" y2="19" />
          <line x1="19" y1="5" x2="5" y2="19" />
        }
        @case ('chevron-down') {
          <polyline points="6,9 12,15 18,9" />
        }
        @case ('cpu') {
          <rect x="6" y="6" width="12" height="12" rx="1.5" />
          <rect x="10" y="10" width="4" height="4" rx="0.5" />
          <line x1="9" y1="3" x2="9" y2="6" />
          <line x1="15" y1="3" x2="15" y2="6" />
          <line x1="9" y1="18" x2="9" y2="21" />
          <line x1="15" y1="18" x2="15" y2="21" />
          <line x1="3" y1="9" x2="6" y2="9" />
          <line x1="3" y1="15" x2="6" y2="15" />
          <line x1="18" y1="9" x2="21" y2="9" />
          <line x1="18" y1="15" x2="21" y2="15" />
        }
      }
    </svg>
  `
})
export class IconComponent {
  @Input() name!: IconName;
  @Input() size = 20;
}
