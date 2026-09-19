import { Component, OnInit, inject, output, signal } from '@angular/core';
import { HistoryListItem } from '../analysis.model';
import { AnalysisStore } from '../analysis.store';
import { ApiService } from '../api.service';
import { verdictIcon } from '../categories';
import { fmt } from '../format';
import { IconComponent } from '../icon.component';
import { ConfirmButtonComponent } from '../shared/confirm-button.component';

/** Списък на запазените анализи; отваряне и изтриване на запис. */
@Component({
  selector: 'app-history-view',
  standalone: true,
  imports: [IconComponent, ConfirmButtonComponent],
  templateUrl: './history-view.component.html',
  styleUrl: './history-view.component.css'
})
export class HistoryViewComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly store = inject(AnalysisStore);
  protected readonly fmt = fmt;
  protected readonly verdictIcon = verdictIcon;

  /** Записът е зареден в AnalysisStore и приложението трябва да покаже изгледа за анализ. */
  readonly opened = output<void>();

  readonly items = signal<HistoryListItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.history().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Неуспешно зареждане на историята.');
        this.loading.set(false);
      }
    });
  }

  open(id: number): void {
    this.api.historyDetail(id).subscribe({
      next: (detail) => {
        this.store.selectedFile.set(null);
        this.store.accept(detail, 'history');
        this.opened.emit();
      },
      error: () => this.error.set('Неуспешно зареждане на записа.')
    });
  }

  remove(id: number): void {
    this.api.deleteHistory(id).subscribe({
      next: () => this.items.update((items) => items.filter((item) => item.id !== id)),
      error: () => this.error.set('Неуспешно изтриване на записа.')
    });
  }
}
