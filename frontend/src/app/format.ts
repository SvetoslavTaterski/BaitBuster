/**
 * Форматиране за показване. Събрано в един обект, за да го излагат
 * компонентите като `protected readonly fmt = fmt` и да го ползват в
 * шаблоните, без всеки да си дублира същите пет метода.
 */
export const fmt = {
  date(iso: string): string {
    return new Date(iso).toLocaleString('bg-BG', {
      day: '2-digit', month: '2-digit', year: 'numeric',
      hour: '2-digit', minute: '2-digit'
    });
  },

  /** Кратка дата за оси на графики: „16.09". */
  day(iso: string): string {
    const date = new Date(iso);
    return `${String(date.getDate()).padStart(2, '0')}.${String(date.getMonth() + 1).padStart(2, '0')}`;
  },

  percent(value: number): string {
    return `${(value * 100).toFixed(1)}%`;
  },

  /** Дял 0–1 в проценти; „—" когато няма как да се сметне (нула примера от този клас). */
  rate(value: number | null): string {
    return value === null ? '—' : fmt.percent(value);
  },

  seconds(value: number): string {
    return value < 1 ? `${Math.round(value * 1000)} мс` : `${value.toFixed(1)} с`;
  },

  bytes(bytes: number): string {
    return bytes < 1024 ? `${bytes} B` : `${(bytes / 1024).toFixed(1)} KB`;
  },

  findingsWord(count: number): string {
    return count === 1 ? 'находка' : 'находки';
  }
};
