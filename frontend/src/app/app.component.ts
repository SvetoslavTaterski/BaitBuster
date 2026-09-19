import { Component, inject, signal } from '@angular/core';
import { AnalysisStore, ReportSource } from './analysis.store';
import { IconComponent } from './icon.component';
import { AnalyzeViewComponent } from './views/analyze-view.component';
import { HistoryViewComponent } from './views/history-view.component';
import { ManualViewComponent } from './views/manual-view.component';
import { ModelViewComponent } from './views/model-view.component';
import { RulesViewComponent } from './views/rules-view.component';
import { StatisticsViewComponent } from './views/statistics-view.component';

type Theme = 'light' | 'dark';
const THEME_STORAGE_KEY = 'baitbuster-theme';

export type View = 'analyze' | 'manual' | 'history' | 'statistics' | 'rules' | 'model';

/** Кой изглед кой доклад „притежава" — останалите нямат собствен. */
const OWN_SOURCE: Partial<Record<View, ReportSource>> = {
  analyze: 'file',
  manual: 'raw'
};

/**
 * Обвивката на приложението: странично меню, превключване на изгледите
 * и тема. Самите изгледи са отделни компоненти във views/.
 */
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    IconComponent,
    AnalyzeViewComponent,
    ManualViewComponent,
    HistoryViewComponent,
    StatisticsViewComponent,
    RulesViewComponent,
    ModelViewComponent
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  private readonly store = inject(AnalysisStore);

  readonly activeView = signal<View>('analyze');
  readonly theme = signal<Theme>(this.readInitialTheme());

  constructor() {
    this.applyTheme(this.theme());
  }

  /**
   * Двата изгледа за анализ делят един доклад. При преминаване между тях
   * чужд резултат се изчиства — иначе под полето за текст би стоял докладът
   * на качен файл и обратно. Входът (файл, текст) остава.
   */
  select(view: View): void {
    const ownSource = OWN_SOURCE[view];
    const current = this.store.source();

    if (ownSource !== undefined && current !== null && current !== ownSource)
      this.store.clear();

    this.activeView.set(view);
  }

  /** „История" е заредила запис в store-а — показваме го в изгледа за анализ. */
  onHistoryOpened(): void {
    this.activeView.set('analyze');
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
}
