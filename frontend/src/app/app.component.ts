import { Component, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { AnalysisReport } from './analysis.model';

const API_BASE_URL = 'http://localhost:5289';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  selectedFile = signal<File | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  report = signal<AnalysisReport | null>(null);

  constructor(private readonly http: HttpClient) {}

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile.set(input.files?.[0] ?? null);
    this.error.set(null);
    this.report.set(null);
  }

  analyze(): void {
    const file = this.selectedFile();
    if (!file) return;

    const formData = new FormData();
    formData.append('file', file);

    this.loading.set(true);
    this.error.set(null);
    this.report.set(null);

    this.http.post<AnalysisReport>(`${API_BASE_URL}/api/analysis/upload`, formData).subscribe({
      next: (report) => {
        this.report.set(report);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(typeof err.error === 'string' ? err.error : 'Възникна грешка при анализа.');
        this.loading.set(false);
      }
    });
  }

  verdictLabel(verdict: string): string {
    switch (verdict) {
      case 'Phishing': return 'Фишинг';
      case 'Suspicious': return 'Подозрителен';
      default: return 'Легитимен';
    }
  }

  severityLabel(severity: string): string {
    switch (severity) {
      case 'High': return 'Висока';
      case 'Medium': return 'Средна';
      case 'Low': return 'Ниска';
      default: return 'Инфо';
    }
  }
}
