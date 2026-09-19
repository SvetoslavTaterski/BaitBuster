import { IconName } from './icon.component';

/** Редът, в който категориите се показват в доклада. */
export const CATEGORY_ORDER = ['Headers', 'Urls', 'Content', 'Attachments', 'Ml'];

const CATEGORY_ICONS: Record<string, IconName> = {
  Headers: 'mail',
  Urls: 'link',
  Content: 'alert-triangle',
  Attachments: 'paperclip',
  Ml: 'cpu'
};

const CATEGORY_LABELS: Record<string, string> = {
  Headers: 'Заглавия',
  Urls: 'Линкове',
  Content: 'Съдържание',
  Attachments: 'Прикачени файлове',
  Ml: 'ML класификатор'
};

export function categoryLabel(category: string): string {
  return CATEGORY_LABELS[category] ?? category;
}

export function categoryIcon(category: string): IconName {
  return CATEGORY_ICONS[category] ?? 'alert-triangle';
}

export function verdictLabel(verdict: string): string {
  switch (verdict) {
    case 'Phishing': return 'Фишинг';
    case 'Suspicious': return 'Подозрителен';
    default: return 'Легитимен';
  }
}

export function verdictIcon(verdict: string): IconName {
  switch (verdict) {
    case 'Phishing': return 'shield-x';
    case 'Suspicious': return 'shield-alert';
    default: return 'shield-check';
  }
}

export function severityLabel(severity: string): string {
  switch (severity) {
    case 'High': return 'Висока';
    case 'Medium': return 'Средна';
    case 'Low': return 'Ниска';
    default: return 'Инфо';
  }
}
