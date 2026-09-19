import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import {
  AnalysisReport, HistoryDetail, HistoryListItem, ModelInfo, RulesResponse, Statistics
} from './analysis.model';

/**
 * Всички обръщения към бекенда на едно място. Компонентите не знаят
 * адреси и формати на заявки — само какво искат да получат.
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  analyzeFile(file: File): Observable<AnalysisReport> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<AnalysisReport>(`${this.base}/api/analysis/upload`, form);
  }

  analyzeRaw(raw: string): Observable<AnalysisReport> {
    // Endpoint-ът приема суров MIME текст, не JSON. text/plain е измежду
    // безопасните за CORS типове, така че браузърът не праща preflight заявка.
    return this.http.post<AnalysisReport>(`${this.base}/api/analysis/raw`, raw, {
      headers: { 'Content-Type': 'text/plain' }
    });
  }

  history(): Observable<HistoryListItem[]> {
    return this.http.get<HistoryListItem[]>(`${this.base}/api/analysis/history`);
  }

  historyDetail(id: number): Observable<HistoryDetail> {
    return this.http.get<HistoryDetail>(`${this.base}/api/analysis/history/${id}`);
  }

  deleteHistory(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/analysis/history/${id}`);
  }

  statistics(): Observable<Statistics> {
    return this.http.get<Statistics>(`${this.base}/api/statistics`);
  }

  rules(): Observable<RulesResponse> {
    return this.http.get<RulesResponse>(`${this.base}/api/rules`);
  }

  modelInfo(): Observable<ModelInfo> {
    return this.http.get<ModelInfo>(`${this.base}/api/model/info`);
  }
}
