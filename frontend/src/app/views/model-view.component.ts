import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ModelInfo } from '../analysis.model';
import { ApiService } from '../api.service';
import { fmt } from '../format';
import { IconComponent } from '../icon.component';

/** Метрики и произход на обучения класификатор. */
@Component({
  selector: 'app-model-view',
  standalone: true,
  imports: [IconComponent],
  templateUrl: './model-view.component.html',
  styleUrl: './model-view.component.css'
})
export class ModelViewComponent implements OnInit {
  private readonly api = inject(ApiService);
  protected readonly fmt = fmt;

  readonly info = signal<ModelInfo | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  /** Под този брой примери метриките не са представителни.
   *  Сравнението стои тук, а не в шаблона — „<" в условие на @if
   *  се парсва като начало на HTML таг. */
  readonly smallDataset = computed(() => {
    const info = this.info();
    return info !== null && info.totalExamples < 500;
  });

  ngOnInit(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.modelInfo().subscribe({
      next: (info) => {
        this.info.set(info);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Няма обучен модел или информацията не може да бъде заредена.');
        this.loading.set(false);
      }
    });
  }
}
