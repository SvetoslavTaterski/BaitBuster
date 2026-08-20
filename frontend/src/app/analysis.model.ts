export type Severity = 'Info' | 'Low' | 'Medium' | 'High';
export type Verdict = 'Legitimate' | 'Suspicious' | 'Phishing';

export interface Finding {
  ruleId: string;
  category: string;
  severity: Severity;
  score: number;
  description: string;
  evidence: string;
}

export interface AnalysisReport {
  emailSubject: string;
  fromAddress: string;
  analyzedAt: string;
  findings: Finding[];
  riskScore: number;
  verdict: Verdict;
}

export interface HistoryListItem {
  id: number;
  emailSubject: string;
  fromAddress: string;
  riskScore: number;
  verdict: Verdict;
  analyzedAt: string;
  findingsCount: number;
}

export interface HistoryDetail extends AnalysisReport {
  id: number;
}

export interface ModelMetrics {
  accuracy: number;
  precision: number;
  recall: number;
  f1Score: number;
  areaUnderRocCurve: number;
}

export interface ModelInfo {
  trainedAt: string;
  algorithm: string;
  totalExamples: number;
  trainingExamples: number;
  testExamples: number;
  phishingExamples: number;
  legitimateExamples: number;
  metrics: ModelMetrics;
  confusion: ConfusionMatrix | null;
  crossValidation: CrossValidationSummary | null;
  candidates: AlgorithmResult[] | null;
  perSource: SourceAccuracy[] | null;
  dataset: string | null;
}

/** Разпределение на решенията върху тестовата извадка. */
export interface ConfusionMatrix {
  truePositives: number;
  falsePositives: number;
  trueNegatives: number;
  falseNegatives: number;
}

/** Резултат от k-кратна кръстосана проверка. */
export interface CrossValidationSummary {
  folds: number;
  accuracyMean: number;
  accuracyStdDev: number;
  f1Mean: number;
  f1StdDev: number;
  aucMean: number;
  aucStdDev: number;
}

/** Един изпробван алгоритъм от сравнението. */
export interface AlgorithmResult {
  algorithm: string;
  metrics: ModelMetrics;
  trainingSeconds: number;
  selected: boolean;
}

/** Точност върху частта от теста, дошла от един корпус. */
export interface SourceAccuracy {
  source: string;
  testExamples: number;
  accuracy: number;
  recall: number | null;
  falsePositiveRate: number | null;
}
