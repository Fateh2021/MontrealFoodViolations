const dateFormatter = new Intl.DateTimeFormat('fr-CA', {
  dateStyle: 'medium',
  timeStyle: 'short'
});

const currencyFormatter = new Intl.NumberFormat('fr-CA', {
  style: 'currency',
  currency: 'CAD',
  maximumFractionDigits: 0
});

const compactCurrencyFormatter = new Intl.NumberFormat('fr-CA', {
  style: 'currency',
  currency: 'CAD',
  maximumFractionDigits: 2
});

const calendarDateFormatter = new Intl.DateTimeFormat('fr-CA', {
  day: 'numeric',
  month: 'long',
  year: 'numeric'
});

const monthYearFormatter = new Intl.DateTimeFormat('fr-CA', {
  month: 'long',
  year: 'numeric'
});

export function formatDate(value) {
  if (!value) return 'Jamais';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Inconnue';
  return dateFormatter.format(date);
}

export function formatCalendarDate(value) {
  if (!value) return '—';
  const match = String(value).match(/^(\d{4})-(\d{2})-(\d{2})/);
  if (!match) return String(value);
  const date = new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
  return calendarDateFormatter.format(date);
}

export function statusClass(value) {
  const text = String(value || '').toLowerCase();
  if (text.includes('ouvert')) return 'status-pill open';
  if (text.includes('ferm')) return 'status-pill closed';
  return 'status-pill';
}

export function formatNumber(value) {
  return new Intl.NumberFormat('fr-CA').format(value ?? 0);
}

export function formatCurrency(value, compact = false) {
  if (value == null || Number.isNaN(Number(value))) return '—';
  return (compact ? currencyFormatter : compactCurrencyFormatter).format(Number(value));
}

export function formatMonthYear(value) {
  const match = String(value || '').match(/^(\d{4})-(\d{2})/);
  if (!match) return '';
  return monthYearFormatter.format(new Date(Number(match[1]), Number(match[2]) - 1, 1));
}

export function highestFine(rows) {
  const amounts = (rows || [])
    .map(row => Number(row?.montant))
    .filter(amount => Number.isFinite(amount) && amount > 0);
  if (amounts.length < 2) return null;
  const max = Math.max(...amounts);
  return max > Math.min(...amounts) ? max : null;
}

export function summarizeBusiness(data) {
  const violations = data?.violations || [];
  const fines = violations.filter(violation => Number(violation.montant) > 0);
  const count = fines.length;
  const total = fines.reduce((sum, violation) => sum + Number(violation.montant), 0);
  const latest = violations
    .map(violation => violation.dateJugement || violation.date)
    .filter(Boolean)
    .sort()
    .at(-1);
  const countLabel = count === 0
    ? 'Aucune amende'
    : count === 1
      ? '1 amende'
      : `${formatNumber(count)} amendes`;
  const month = latest ? formatMonthYear(latest) : '';
  if (!month) return `${countLabel}, ${formatCurrency(total, true)} au total.`;
  return `${countLabel}, ${formatCurrency(total, true)} au total, dernier dossier en ${month}.`;
}
