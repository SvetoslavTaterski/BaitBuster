import { Component, computed, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import {
  AnalysisReport, Finding, HistoryDetail, HistoryListItem, ModelInfo, RuleDescription, Statistics
} from './analysis.model';
import { IconComponent, IconName } from './icon.component';

const API_BASE_URL = 'http://localhost:5289';
const GAUGE_RADIUS = 38;
const GAUGE_CIRCUMFERENCE = 2 * Math.PI * GAUGE_RADIUS;

type Theme = 'light' | 'dark';
const THEME_STORAGE_KEY = 'baitbuster-theme';

type View = 'analyze' | 'history' | 'statistics' | 'rules' | 'model';

const CATEGORY_ORDER = ['Headers', 'Urls', 'Content', 'Attachments', 'Ml'];
const CATEGORY_ICONS: Record<string, IconName> = {
  Headers: 'mail',
  Urls: 'link',
  Content: 'alert-triangle',
  Attachments: 'paperclip',
  Ml: 'cpu'
};
const CATEGORY_LABELS: Record<string, string> = {
  Headers: 'Заглавия',
  Urls: 'Линкове',
  Content: 'Съдържание',
  Attachments: 'Прикачени файлове',
  Ml: 'ML класификатор'
};

interface FindingGroup {
  category: string;
  label: string;
  icon: IconName;
  score: number;
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
  displayedScore = signal(0);
  private animationFrameId: number | null = null;

  activeView = signal<View>('analyze');
  viewingHistoryItem = signal(false);
  historyItems = signal<HistoryListItem[]>([]);
  historyLoading = signal(false);
  historyError = signal<string | null>(null);

  modelInfo = signal<ModelInfo | null>(null);
  modelLoading = signal(false);
  modelError = signal<string | null>(null);

  rules = signal<RuleDescription[]>([]);
  rulesLoading = signal(false);
  rulesError = signal<string | null>(null);

  statistics = signal<Statistics | null>(null);
  statisticsLoading = signal(false);
  statisticsError = signal<string | null>(null);

  /** Най-голямата дневна стойност — мащабът, спрямо който се чертаят стълбовете. */
  maxDailyCount = computed(() =>
    Math.max(1, ...(this.statistics()?.lastDays ?? []).map((d) => d.count)));

  maxCategoryCount = computed(() =>
    Math.max(1, ...(this.statistics()?.findingsByCategory ?? []).map((c) => c.count)));

  maxRuleCount = computed(() =>
    Math.max(1, ...(this.statistics()?.topRules ?? []).map((r) => r.count)));

  /** Под този брой примери метриките не са представителни.
   *  Сравнението стои тук, а не в темплейта — „<" в условие на @if
   *  се парсва като начало на HTML таг. */
  smallDataset = computed(() => {
    const info = this.modelInfo();
    return info !== null && info.totalExamples < 500;
  });

  readonly gaugeCircumference = GAUGE_CIRCUMFERENCE;

  gaugeOffset = computed(() => {
    return GAUGE_CIRCUMFERENCE * (1 - this.displayedScore() / 100);
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
      .map((category) => {
        const groupFindings = byCategory.get(category)!;
        return {
          category,
          label: CATEGORY_LABELS[category] ?? category,
          icon: CATEGORY_ICONS[category] ?? 'alert-triangle',
          score: groupFindings.reduce((sum, f) => sum + f.score, 0),
          findings: groupFindings
        };
      });
  });

  /** Категориите са свити по подразбиране — обобщението в заглавния ред
   *  (брой находки и принос към score-а) стига, за да се реши коя да се отвори. */
  expandedCategories = signal<ReadonlySet<string>>(new Set());

  isCategoryExpanded(category: string): boolean {
    return this.expandedCategories().has(category);
  }

  toggleCategory(category: string): void {
    this.expandedCategories.update((current) => {
      const next = new Set(current);
      if (!next.delete(category)) next.add(category);
      return next;
    });
  }

  constructor(private readonly http: HttpClient) {
    this.applyTheme(this.theme());
  }

  private animateScoreTo(target: number): void {
    if (this.animationFrameId !== null) {
      cancelAnimationFrame(this.animationFrameId);
      this.animationFrameId = null;
    }

    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduceMotion) {
      this.displayedScore.set(target);
      return;
    }

    const from = this.displayedScore();
    const duration = 700;
    const start = performance.now();

    const step = (now: number) => {
      const t = Math.min(1, (now - start) / duration);
      const eased = 1 - Math.pow(1 - t, 3);
      this.displayedScore.set(Math.round(from + (target - from) * eased));
      this.animationFrameId = t < 1 ? requestAnimationFrame(step) : null;
    };

    this.animationFrameId = requestAnimationFrame(step);
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
    this.viewingHistoryItem.set(false);
    this.animateScoreTo(0);
  }

  clearFile(fileInput: HTMLInputElement): void {
    fileInput.value = '';
    this.setFile(null);
  }

  analyze(): void {
    const file = this.selectedFile();
    if (!file) return;

    const formData = new FormData();
    formData.append('file', file);

    this.loading.set(true);
    this.error.set(null);
    this.report.set(null);
    this.animateScoreTo(0);

    this.http.post<AnalysisReport>(`${API_BASE_URL}/api/analysis/upload`, formData).subscribe({
      next: (report) => {
        this.report.set(report);
        this.expandedCategories.set(new Set());
        this.loading.set(false);
        this.animateScoreTo(report.riskScore);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(typeof err.error === 'string' ? err.error : 'Възникна грешка при анализа.');
        this.loading.set(false);
      }
    });
  }

  showAnalyze(): void {
    if (this.viewingHistoryItem()) {
      this.viewingHistoryItem.set(false);
      this.selectedFile.set(null);
      this.error.set(null);
      this.report.set(null);
      this.animateScoreTo(0);
    }
    this.activeView.set('analyze');
  }

  showHistory(): void {
    this.activeView.set('history');
    this.historyLoading.set(true);
    this.historyError.set(null);

    this.http.get<HistoryListItem[]>(`${API_BASE_URL}/api/analysis/history`).subscribe({
      next: (items) => {
        this.historyItems.set(items);
        this.historyLoading.set(false);
      },
      error: () => {
        this.historyError.set('Неуспешно зареждане на историята.');
        this.historyLoading.set(false);
      }
    });
  }

  viewHistoryItem(id: number): void {
    this.http.get<HistoryDetail>(`${API_BASE_URL}/api/analysis/history/${id}`).subscribe({
      next: (detail) => {
        this.selectedFile.set(null);
        this.error.set(null);
        this.report.set(detail);
        this.expandedCategories.set(new Set());
        this.animateScoreTo(detail.riskScore);
        this.viewingHistoryItem.set(true);
        this.activeView.set('analyze');
      },
      error: () => {
        this.historyError.set('Неуспешно зареждане на записа.');
      }
    });
  }

  showStatistics(): void {
    this.activeView.set('statistics');
    this.statisticsLoading.set(true);
    this.statisticsError.set(null);

    // За разлика от правилата и модела тук не кешираме — числата се менят
    // с всеки нов анализ, а изгледът трябва да ги отразява.
    this.http.get<Statistics>(`${API_BASE_URL}/api/statistics`).subscribe({
      next: (stats) => {
        this.statistics.set(stats);
        this.statisticsLoading.set(false);
      },
      error: () => {
        this.statisticsError.set('Статистиката не може да бъде заредена.');
        this.statisticsLoading.set(false);
      }
    });
  }

  /** Дял в проценти спрямо общия брой анализи. */
  sharePercent(count: number): number {
    const total = this.statistics()?.totalAnalyses ?? 0;
    return total === 0 ? 0 : Math.round((count / total) * 100);
  }

  barWidth(count: number, max: number): number {
    return Math.round((count / max) * 100);
  }

  formatDay(iso: string): string {
    const date = new Date(iso);
    return `${String(date.getDate()).padStart(2, '0')}.${String(date.getMonth() + 1).padStart(2, '0')}`;
  }

  showRules(): void {
    this.activeView.set('rules');

    if (this.rules().length > 0) return;

    this.rulesLoading.set(true);
    this.rulesError.set(null);

    this.http.get<RuleDescription[]>(`${API_BASE_URL}/api/rules`).subscribe({
      next: (rules) => {
        this.rules.set(rules);
        this.rulesLoading.set(false);
      },
      error: () => {
        this.rulesError.set('Списъкът с правила не може да бъде зареден.');
        this.rulesLoading.set(false);
      }
    });
  }

  categoryLabel(category: string): string {
    return CATEGORY_LABELS[category] ?? category;
  }

  categoryIcon(category: string): IconName {
    return CATEGORY_ICONS[category] ?? 'alert-triangle';
  }

  showModel(): void {
    this.activeView.set('model');

    if (this.modelInfo()) return;

    this.modelLoading.set(true);
    this.modelError.set(null);

    this.http.get<ModelInfo>(`${API_BASE_URL}/api/model/info`).subscribe({
      next: (info) => {
        this.modelInfo.set(info);
        this.modelLoading.set(false);
      },
      error: () => {
        this.modelError.set('Няма обучен модел или информацията не може да бъде заредена.');
        this.modelLoading.set(false);
      }
    });
  }

  /** Дял 0–1 в проценти; „—“ когато няма как да се сметне (нула примера от този клас). */
  formatRate(value: number | null): string {
    return value === null ? '—' : this.formatPercent(value);
  }

  formatSeconds(value: number): string {
    return value < 1 ? `${Math.round(value * 1000)} мс` : `${value.toFixed(1)} с`;
  }

  formatPercent(value: number): string {
    return `${(value * 100).toFixed(1)}%`;
  }

  deleteHistoryItem(id: number, event: Event): void {
    event.stopPropagation();

    if (!confirm('Да изтрия ли този анализ от историята?')) return;

    this.http.delete(`${API_BASE_URL}/api/analysis/history/${id}`).subscribe({
      next: () => {
        this.historyItems.update((items) => items.filter((item) => item.id !== id));
      },
      error: () => {
        this.historyError.set('Неуспешно изтриване на записа.');
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

  findingsWord(count: number): string {
    return count === 1 ? 'находка' : 'находки';
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleString('bg-BG', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  }
}
