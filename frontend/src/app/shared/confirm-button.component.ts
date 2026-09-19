import { Component, HostListener, OnDestroy, input, output, signal } from '@angular/core';
import { IconComponent } from '../icon.component';

/**
 * Бутон „×", който при натискане не изпълнява действието, а се разгъва на
 * място във въпрос с „Да" / „Не". Замества браузърния confirm(), който
 * стърчи на фона на останалия интерфейс и спира целия таб.
 *
 * Спира разпространението на всяко кликване, за да не задейства редa, в
 * който стои (напр. отваряне на записа от историята).
 */
@Component({
  selector: 'app-confirm-button',
  standalone: true,
  imports: [IconComponent],
  template: `
    @if (armed()) {
      <span class="confirm" (click)="$event.stopPropagation()">
        <span class="confirm-question">{{ question() }}</span>
        <button type="button" class="confirm-yes" (click)="confirm($event)">Да</button>
        <button type="button" class="confirm-no" (click)="cancel($event)">Не</button>
      </span>
    } @else {
      <button type="button" class="clear-file-button" (click)="arm($event)" [attr.aria-label]="label()">
        <app-icon name="x" [size]="14" />
      </button>
    }
  `,
  styles: `
    :host {
      display: inline-flex;
      flex-shrink: 0;
    }

    .confirm {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      font-size: 12px;
    }

    .confirm-question {
      color: var(--text-secondary);
      white-space: nowrap;
    }

    .confirm-yes,
    .confirm-no {
      padding: 3px 10px;
      border-radius: var(--radius);
      font-family: inherit;
      font-size: 12px;
      font-weight: 600;
      cursor: pointer;
    }

    .confirm-yes {
      border: none;
      background: var(--fill-danger);
      color: #fff;
    }

    .confirm-no {
      border: 1px solid var(--border-strong);
      background: var(--surface-1);
      color: var(--text-primary);
    }
  `
})
export class ConfirmButtonComponent implements OnDestroy {
  /** Достъпно описание на действието, напр. „Изтрий записа". */
  readonly label = input.required<string>();
  readonly question = input('Изтрий?');
  readonly confirmed = output<void>();

  readonly armed = signal(false);
  private disarmTimer: number | null = null;

  /** Ако потребителят се разколебае и не избере нищо, въпросът се прибира сам. */
  private static readonly AutoDisarmMs = 4000;

  arm(event: Event): void {
    event.stopPropagation();
    this.armed.set(true);
    this.clearTimer();
    this.disarmTimer = window.setTimeout(() => this.armed.set(false), ConfirmButtonComponent.AutoDisarmMs);
  }

  confirm(event: Event): void {
    event.stopPropagation();
    this.disarm();
    this.confirmed.emit();
  }

  cancel(event: Event): void {
    event.stopPropagation();
    this.disarm();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.armed()) this.disarm();
  }

  ngOnDestroy(): void {
    this.clearTimer();
  }

  private disarm(): void {
    this.clearTimer();
    this.armed.set(false);
  }

  private clearTimer(): void {
    if (this.disarmTimer !== null) {
      window.clearTimeout(this.disarmTimer);
      this.disarmTimer = null;
    }
  }
}
