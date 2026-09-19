import { Component, inject } from '@angular/core';
import { AnalysisStore, GAUGE_CIRCUMFERENCE } from '../analysis.store';
import { severityLabel, verdictIcon, verdictLabel } from '../categories';
import { fmt } from '../format';
import { IconComponent } from '../icon.component';

/**
 * Докладът от анализа: кръгов индикатор, присъда и находките по категория.
 * Един и същ, независимо дали имейлът е дошъл от файл, от поставен текст
 * или от историята — чете директно текущия доклад от AnalysisStore.
 */
@Component({
  selector: 'app-report-dashboard',
  standalone: true,
  imports: [IconComponent],
  templateUrl: './report-dashboard.component.html',
  styleUrl: './report-dashboard.component.css'
})
export class ReportDashboardComponent {
  protected readonly store = inject(AnalysisStore);
  protected readonly gaugeCircumference = GAUGE_CIRCUMFERENCE;
  protected readonly fmt = fmt;
  protected readonly verdictLabel = verdictLabel;
  protected readonly verdictIcon = verdictIcon;
  protected readonly severityLabel = severityLabel;
}
