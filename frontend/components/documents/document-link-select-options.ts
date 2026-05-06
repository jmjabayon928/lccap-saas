import type { ActionItemSummary } from "@/types/actions";
import type { PlanSectionSummary } from "@/types/plans";

export interface DocumentSectionLinkOption {
  readonly value: string;
  readonly label: string;
}

export interface DocumentActionLinkOption {
  readonly value: string;
  readonly label: string;
}

export function formatSectionOptionLabel(section: Pick<PlanSectionSummary, "id" | "sectionKey" | "title">): string {
  const sk = section.sectionKey?.trim();
  const title = section.title?.trim();
  if (sk && title) {
    return `${sk} — ${title}`;
  }
  if (title) {
    return title;
  }
  if (sk) {
    return sk;
  }
  return section.id;
}

export function formatActionOptionLabel(action: Pick<ActionItemSummary, "id" | "title" | "actionType">): string {
  const title = action.title?.trim() ?? "";
  const actionType = action.actionType.trim();
  if (title && actionType) {
    return `${title} (${actionType})`;
  }
  if (title) {
    return title;
  }
  return action.id;
}

export function buildSectionLinkOptions(
  sections: readonly PlanSectionSummary[]
): readonly DocumentSectionLinkOption[] {
  const sorted = [...sections].sort((a, b) => {
    if (a.sortOrder !== b.sortOrder) {
      return a.sortOrder - b.sortOrder;
    }
    return formatSectionOptionLabel(a).localeCompare(formatSectionOptionLabel(b));
  });
  return sorted.map((s) => ({ value: s.id, label: formatSectionOptionLabel(s) }));
}

export function buildActionLinkOptions(
  actions: readonly ActionItemSummary[]
): readonly DocumentActionLinkOption[] {
  const sorted = [...actions].sort((a, b) =>
    formatActionOptionLabel(a).localeCompare(formatActionOptionLabel(b))
  );
  return sorted.map((a) => ({ value: a.id, label: formatActionOptionLabel(a) }));
}

export function enrichSectionOptionsWithCurrentIfMissing(
  options: readonly DocumentSectionLinkOption[],
  currentId: string | null | undefined
): readonly DocumentSectionLinkOption[] {
  const id = currentId?.trim();
  if (!id) {
    return options;
  }
  if (options.some((o) => o.value === id)) {
    return options;
  }
  return [...options, { value: id, label: `Current linked section: ${id}` }];
}

export function enrichActionOptionsWithCurrentIfMissing(
  options: readonly DocumentActionLinkOption[],
  currentId: string | null | undefined
): readonly DocumentActionLinkOption[] {
  const id = currentId?.trim();
  if (!id) {
    return options;
  }
  if (options.some((o) => o.value === id)) {
    return options;
  }
  return [...options, { value: id, label: `Current linked action: ${id}` }];
}
