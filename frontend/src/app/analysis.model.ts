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
