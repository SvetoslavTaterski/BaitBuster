import { Component, inject, signal } from '@angular/core';
import { AnalysisStore } from '../analysis.store';
import { ApiService } from '../api.service';
import { fmt } from '../format';
import { IconComponent } from '../icon.component';
import { ReportDashboardComponent } from '../shared/report-dashboard.component';

/** Качване на .eml файл и показване на резултата. */
@Component({
  selector: 'app-analyze-view',
  standalone: true,
  imports: [IconComponent, ReportDashboardComponent],
  templateUrl: './analyze-view.component.html',
  styleUrl: './analyze-view.component.css'
})
export class AnalyzeViewComponent {
  protected readonly store = inject(AnalysisStore);
  private readonly api = inject(ApiService);
  protected readonly fmt = fmt;

  /** Само визуално състояние на dropzone-а — не надживява изгледа. */
  readonly isDragging = signal(false);

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.store.setFile(input.files?.[0] ?? null);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(true);
  }

  onDragLeave(): void {
    this.isDragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(false);
    this.store.setFile(event.dataTransfer?.files?.[0] ?? null);
  }

  /** Нулира и самия <input>, за да може същият файл да се избере отново
   *  — иначе браузърът не задейства change при идентичен избор. */
  clearFile(fileInput: HTMLInputElement): void {
    fileInput.value = '';
    this.store.setFile(null);
  }

  analyze(): void {
    const file = this.store.selectedFile();
    if (!file) return;

    this.store.begin();
    this.api.analyzeFile(file).subscribe({
      next: (report) => this.store.accept(report, 'file'),
      error: (err) => this.store.fail(err)
    });
  }
}
