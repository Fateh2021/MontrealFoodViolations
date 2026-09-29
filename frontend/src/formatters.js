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

export function formatDate(value) {
  if (!value) return 'Jamais';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Inconnue';
  return dateFormatter.format(date);
}

export function formatNumber(value) {
  return new Intl.NumberFormat('fr-CA').format(value ?? 0);
}

export function formatCurrency(value, compact = false) {
  if (value == null || Number.isNaN(Number(value))) return '—';
  return (compact ? currencyFormatter : compactCurrencyFormatter).format(Number(value));
}
