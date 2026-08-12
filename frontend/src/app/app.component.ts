import { Component, computed, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { AnalysisReport, Finding } from './analysis.model';
import { IconComponent, IconName } from './icon.component';

const API_BASE_URL = 'http://localhost:5289';
const GAUGE_RADIUS = 38;
const GAUGE_CIRCUMFERENCE = 2 * Math.PI * GAUGE_RADIUS;

type Theme = 'light' | 'dark';
const THEME_STORAGE_KEY = 'baitbuster-theme';

const CATEGORY_ORDER = ['Headers', 'Urls', 'Content', 'Attachments', 'Ml'];
const CATEGORY_ICONS: Record<string, IconName> = {
  Headers: 'mail',
  Urls: 'link',
  Content: 'alert-triangle',
  Attachments: 'paperclip'
};

interface FindingGroup {
  category: string;
  icon: IconName;
  findings: Finding[];
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [IconComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  selectedFile = signal<File | null>(null);
  isDragging = signal(false);
  loading = signal(false);
  error = signal<string | null>(null);
  theme = signal<Theme>(this.readInitialTheme());
  report = signal<AnalysisReport | null>(null);

  readonly gaugeCircumference = GAUGE_CIRCUMFERENCE;

  gaugeOffset = computed(() => {
    const score = this.report()?.riskScore ?? 0;
    return GAUGE_CIRCUMFERENCE * (1 - score / 100);
  });

  groupedFindings = computed<FindingGroup[]>(() => {
    const findings = this.report()?.findings ?? [];
    const byCategory = new Map<string, Finding[]>();

    for (const f of findings) {
      const list = byCategory.get(f.category) ?? [];
      list.push(f);
      byCategory.set(f.category, list);
    }

    return [...byCategory.keys()]
      .sort((a, b) => CATEGORY_ORDER.indexOf(a) - CATEGORY_ORDER.indexOf(b))
      .map((category) => ({
        category,
        icon: CATEGORY_ICONS[category] ?? 'alert-triangle',
        findings: byCategory.get(category)!
      }));
  });

  constructor(private readonly http: HttpClient) {
    this.applyTheme(this.theme());
  }

  toggleTheme(): void {
    const next: Theme = this.theme() === 'dark' ? 'light' : 'dark';
    this.theme.set(next);
    localStorage.setItem(THEME_STORAGE_KEY, next);
    this.applyTheme(next);
  }

  private readInitialTheme(): Theme {
    const stored = localStorage.getItem(THEME_STORAGE_KEY);
    if (stored === 'light' || stored === 'dark') return stored;
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  private applyTheme(theme: Theme): void {
    document.documentElement.setAttribute('data-theme', theme);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.setFile(input.files?.[0] ?? null);
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
    this.setFile(event.dataTransfer?.files?.[0] ?? null);
  }

  private setFile(file: File | null): void {
    this.selectedFile.set(file);
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

  verdictIcon(verdict: string): IconName {
    switch (verdict) {
      case 'Phishing': return 'shield-x';
      case 'Suspicious': return 'shield-alert';
      default: return 'shield-check';
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

  formatBytes(bytes: number): string {
    return bytes < 1024
      ? `${bytes} B`
      : `${(bytes / 1024).toFixed(1)} KB`;
  }
}
