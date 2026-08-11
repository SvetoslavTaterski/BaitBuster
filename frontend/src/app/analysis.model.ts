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
