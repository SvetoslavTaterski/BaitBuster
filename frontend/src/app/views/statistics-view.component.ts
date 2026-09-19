import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Statistics } from '../analysis.model';
import { ApiService } from '../api.service';
import { categoryLabel } from '../categories';
import { fmt } from '../format';

/** Обобщение върху всички запазени анализи. Не се кешира — числата се менят с всеки анализ. */
@Component({
  selector: 'app-statistics-view',
  standalone: true,
  templateUrl: './statistics-view.component.html',
  styleUrl: './statistics-view.component.css'
})
export class StatisticsViewComponent implements OnInit {
  private readonly api = inject(ApiService);
  protected readonly fmt = fmt;
  protected readonly categoryLabel = categoryLabel;

  readonly statistics = signal<Statistics | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  /** Най-големите стойности — мащабът, спрямо който се чертаят стълбовете. */
  readonly maxDailyCount = computed(() =>
    Math.max(1, ...(this.statistics()?.lastDays ?? []).map((d) => d.count)));

  readonly maxCategoryCount = computed(() =>
    Math.max(1, ...(this.statistics()?.findingsByCategory ?? []).map((c) => c.count)));

  readonly maxRuleCount = computed(() =>
    Math.max(1, ...(this.statistics()?.topRules ?? []).map((r) => r.count)));

  ngOnInit(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.statistics().subscribe({
      next: (stats) => {
        this.statistics.set(stats);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Статистиката не може да бъде заредена.');
        this.loading.set(false);
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
}
