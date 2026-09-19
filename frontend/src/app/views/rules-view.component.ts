import { Component, OnInit, inject, signal } from '@angular/core';
import { RuleDescription, VerdictThresholds } from '../analysis.model';
import { ApiService } from '../api.service';
import { categoryIcon, categoryLabel } from '../categories';
import { IconComponent } from '../icon.component';

/** Всички детекционни правила и праговете за присъда, така както ги вижда бекендът. */
@Component({
  selector: 'app-rules-view',
  standalone: true,
  imports: [IconComponent],
  templateUrl: './rules-view.component.html',
  styleUrl: './rules-view.component.css'
})
export class RulesViewComponent implements OnInit {
  private readonly api = inject(ApiService);
  protected readonly categoryLabel = categoryLabel;
  protected readonly categoryIcon = categoryIcon;

  readonly rules = signal<RuleDescription[]>([]);
  readonly thresholds = signal<VerdictThresholds | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.rules().subscribe({
      next: (response) => {
        this.rules.set(response.rules);
        this.thresholds.set(response.thresholds);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Списъкът с правила не може да бъде зареден.');
        this.loading.set(false);
      }
    });
  }
}
