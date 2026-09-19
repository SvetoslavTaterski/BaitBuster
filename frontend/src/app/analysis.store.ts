import { Injectable, computed, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { AnalysisReport, Finding } from './analysis.model';
import { CATEGORY_ORDER, categoryIcon, categoryLabel } from './categories';
import { IconName } from './icon.component';

/** Откъде е дошъл текущо показаният доклад — определя кой изглед има право да го покаже. */
export type ReportSource = 'file' | 'raw' | 'history';

export interface FindingGroup {
  category: string;
  label: string;
  icon: IconName;
  score: number;
  findings: Finding[];
}

const GAUGE_RADIUS = 38;
export const GAUGE_CIRCUMFERENCE = 2 * Math.PI * GAUGE_RADIUS;

/**
 * Състоянието на текущата сесия за анализ: входът (файл или поставен текст),
 * резултатът и откъде е дошъл. Живее извън компонентите, защото се дели
 * между изгледите — „Нов анализ" и „Ръчен вход" показват един и същ доклад,
 * „История" го зарежда, а входът трябва да оцелее при превключване на таб.
 */
@Injectable({ providedIn: 'root' })
export class AnalysisStore {
  readonly selectedFile = signal<File | null>(null);
  readonly rawInput = signal('');

  readonly report = signal<AnalysisReport | null>(null);
  readonly source = signal<ReportSource | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  /** Стойността в кръговия индикатор — брои се плавно до реалния score. */
  readonly displayedScore = signal(0);
  private animationFrameId: number | null = null;

  /** Категориите са свити по подразбиране — обобщението в заглавния ред
   *  (брой находки и принос към score-а) стига, за да се реши коя да се отвори. */
  readonly expandedCategories = signal<ReadonlySet<string>>(new Set());

  /** Докладът, само когато е зареден от историята. */
  readonly historyReport = computed(() =>
    this.source() === 'history' ? this.report() : null);

  readonly gaugeOffset = computed(() =>
    GAUGE_CIRCUMFERENCE * (1 - this.displayedScore() / 100));

  readonly groupedFindings = computed<FindingGroup[]>(() => {
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
          label: categoryLabel(category),
          icon: categoryIcon(category),
          score: groupFindings.reduce((sum, f) => sum + f.score, 0),
          findings: groupFindings
        };
      });
  });

  setFile(file: File | null): void {
    this.selectedFile.set(file);
    this.clear();
  }

  /** Стартира нов анализ: маркира зареждане и чисти предишния резултат. */
  begin(): void {
    this.loading.set(true);
    this.clear();
  }

  /** Показва пристигнал доклад — от файл, от текст или от историята. */
  accept(report: AnalysisReport, source: ReportSource): void {
    this.report.set(report);
    this.source.set(source);
    this.error.set(null);
    this.expandedCategories.set(new Set());
    this.loading.set(false);
    this.animateScoreTo(report.riskScore);
  }

  fail(err: HttpErrorResponse): void {
    this.error.set(typeof err.error === 'string' ? err.error : 'Възникна грешка при анализа.');
    this.loading.set(false);
  }

  /** Нулира резултата и всичко, което зависи от него. Входът остава. */
  clear(): void {
    this.error.set(null);
    this.report.set(null);
    this.source.set(null);
    this.expandedCategories.set(new Set());
    this.animateScoreTo(0);
  }

  isExpanded(category: string): boolean {
    return this.expandedCategories().has(category);
  }

  toggleCategory(category: string): void {
    this.expandedCategories.update((current) => {
      const next = new Set(current);
      if (!next.delete(category)) next.add(category);
      return next;
    });
  }

  private animateScoreTo(target: number): void {
    if (this.animationFrameId !== null) {
      cancelAnimationFrame(this.animationFrameId);
      this.animationFrameId = null;
    }

    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
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
}
