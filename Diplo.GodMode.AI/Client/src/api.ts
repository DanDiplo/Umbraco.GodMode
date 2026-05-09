import { umbHttpClient } from "@umbraco-cms/backoffice/http-client";
import { GODMODE_AI_API_BASE } from "./constants";

const BEARER_SECURITY = [{ scheme: "bearer", type: "http" }] as const;

export interface GodModeAnalysisFinding {
  severity: string;
  score: number;
  category: string;
  title: string;
  detail: string;
  entityType: string;
  entityName: string;
  entityAlias: string;
  affectedEntities: GodModeAffectedEntity[];
  recommendation: string;
}

export interface GodModeAffectedEntity {
  entityType: string;
  name: string;
  alias: string;
  key: string;
  context: string;
}

export interface GodModeAnalysisResult {
  providerAlias: string;
  providerName: string;
  summary: string;
  findings: GodModeAnalysisFinding[];
  suggestedNextSteps: string[];
  generatedUtc: string;
}

export interface GodModeAiExplainSubject {
  subjectType: string;
  title: string;
  data: Record<string, unknown>;
}

export interface GodModeAiExplainResponse {
  summary: string;
  whatItIs: string;
  whyItMatters: string;
  risk: string;
  suggestedNextSteps: string[];
}

export async function analyzeSchemaHealth(): Promise<GodModeAnalysisResult> {
  const { data, error } = await umbHttpClient.post<GodModeAnalysisResult>({
    url: `${GODMODE_AI_API_BASE}/schema-health`,
    security: BEARER_SECURITY
  });

  if (error) {
    throw error;
  }

  return data as GodModeAnalysisResult;
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

export async function createFixPlan(): Promise<GodModeAnalysisResult> {
  const { data, error } = await umbHttpClient.post<GodModeAnalysisResult>({
    url: `${GODMODE_AI_API_BASE}/fix-plan`,
    security: BEARER_SECURITY
  });

  if (error) {
    throw error;
  }

  return data as GodModeAnalysisResult;
}
