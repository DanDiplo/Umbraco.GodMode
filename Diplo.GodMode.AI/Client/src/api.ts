import { umbHttpClient } from "@umbraco-cms/backoffice/http-client";
import { GODMODE_AI_API_BASE } from "./constants";

const BEARER_SECURITY = [{ scheme: "bearer", type: "http" }] as const;

export interface GodModeAiExplainSubject {
  subjectType: string;
  title: string;
  data: Record<string, unknown>;
  context?: Record<string, unknown>;
}

export interface GodModeAiExplainResponse {
  summary: string;
  primaryDiagnosis?: string;
  whereToLook?: string;
  likelyCause?: string;
  howToFix?: string[];
  whatItIs: string;
  whyItMatters: string;
  observedDetails: string[];
  risk: string;
  suggestedNextSteps: string[];
}

export async function explainSubject(subject: GodModeAiExplainSubject): Promise<GodModeAiExplainResponse> {
  const { data, error } = await umbHttpClient.post<GodModeAiExplainResponse>({
    url: `${GODMODE_AI_API_BASE}/explain`,
    body: subject,
    security: BEARER_SECURITY
  });

  if (error) {
    throw error;
  }

  return data as GodModeAiExplainResponse;
}
