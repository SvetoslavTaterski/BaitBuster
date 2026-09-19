import { Component, inject } from '@angular/core';
import { AnalysisStore } from '../analysis.store';
import { ApiService } from '../api.service';
import { ReportDashboardComponent } from '../shared/report-dashboard.component';

/** Поставяне на суров MIME текст вместо файл. */
@Component({
  selector: 'app-manual-view',
  standalone: true,
  imports: [ReportDashboardComponent],
  templateUrl: './manual-view.component.html',
  styleUrl: './manual-view.component.css'
})
export class ManualViewComponent {
  protected readonly store = inject(AnalysisStore);
  private readonly api = inject(ApiService);

  onInput(event: Event): void {
    this.store.rawInput.set((event.target as HTMLTextAreaElement).value);
  }

  clear(): void {
    this.store.rawInput.set('');
    this.store.clear();
  }

  analyze(): void {
    const raw = this.store.rawInput().trim();
    if (raw.length === 0) return;

    this.store.begin();
    this.api.analyzeRaw(raw).subscribe({
      next: (report) => this.store.accept(report, 'raw'),
      error: (err) => this.store.fail(err)
    });
  }
}
