const BRASILIA_TIME_ZONE = "America/Sao_Paulo";

export function formatDecimal(value: number, digits = 2): string {
  return value.toLocaleString("pt-BR", {
    minimumFractionDigits: digits,
    maximumFractionDigits: digits
  });
}

export function formatDateTime(value: string): string {
  return parseUtcDate(value).toLocaleString("pt-BR", {
    timeZone: BRASILIA_TIME_ZONE,
    dateStyle: "short",
    timeStyle: "medium"
  });
}

export function formatTime(value: string): string {
  return parseUtcDate(value).toLocaleTimeString("pt-BR", {
    timeZone: BRASILIA_TIME_ZONE
  });
}

function parseUtcDate(value: string): Date {
  if (/[zZ]$|[+-]\d{2}:\d{2}$/.test(value)) {
    return new Date(value);
  }

  return new Date(`${value}Z`);
}
