// Hand-written camelCase mirrors of the C# DTOs returned by GodModeApiController.
// Keep these in sync with /Diplo.GodMode/Models/*.cs.
// (Phase 2.5 — replace this file with output from `npm run generate:api` once
// we wire up an OpenAPI client generator.)

export interface ItemBase {
    id: number;
    /** Server-side this is a Guid; JSON-serialised as a string. */
    udi: string;
    name: string;
    alias: string;
    isOpen?: boolean;
}

export interface DataTypeMap extends ItemBase {
    dbType: string;
    isUsed: boolean;
    isNestedUsed: boolean;
    updateDate: string;
}

export interface PropertyTypeMap extends ItemBase {
    editorAlias: string;
    description: string | null;
    editorId: number;
    variesBy: string;
    supportsPublishing: boolean;
    storageType: string;
}

export interface TemplateMap extends ItemBase {
    path: string;
    isDefault: boolean;
}

export interface ComponentMap {
    templateId: number;
    templateAlias: string;
    name: string;
    parameters: string;
    tagHelper: boolean;
}

export interface PartialMap {
    templateId: number;
    templateAlias: string;
    name: string;
    path: string;
}

export interface TemplateModel extends ItemBase {
    virtualPath: string;
    createDate: string;
    isMaster: boolean;
    parents: TemplateModel[];
    /** May not be present in a v17 install where master/layout heuristics aren't run. */
    path?: string;
    masterAlias?: string | null;
    filePath?: string;
    hasCorrectMaster?: boolean;
    layout?: string | null;
    partials: PartialMap[];
    viewComponents: ComponentMap[];
}

export interface ContentTypeData extends ItemBase {
    icon: string;
    description: string | null;
    isMaster: boolean;
    hasCompositions: boolean;
    selected: boolean;
}

/**
 * `ContentVariation` is a C# enum. Umbraco's management-API JSON config emits
 * enums as strings ("Nothing" | "Culture" | "Segment" | "CultureAndSegment"),
 * but if a host overrides that we may receive a number — handle both.
 */
export type ContentVariation = "Nothing" | "Culture" | "Segment" | "CultureAndSegment" | number;

export interface ContentTypeMap extends ContentTypeData {
    templates: TemplateMap[];
    properties: PropertyTypeMap[];
    compositionProperties: PropertyTypeMap[];
    allProperties: PropertyTypeMap[];
    compositions: ContentTypeData[];
    propertyGroups: string[];
    hasTemplates: boolean;
    isListView: boolean;
    allowedAtRoot: boolean;
    isComposition: boolean;
    isElement: boolean;
    variesBy: ContentVariation;
    variesByCulture: boolean;
    createDate: string;
}

/** Returned by GET /content. The C# class hierarchy is ContentBasic → ContentItem. */
export interface ContentItem {
    id: number;
    udi: string;
    alias: string;
    name: string;
    icon: string | null;
    parentId: number;
    level: number;
    trashed: boolean;
    path: string;
    createDate: string;
    updateDate: string;
    creatorId: number;
    creatorName: string;
    updaterId: number;
    updaterName: string;
    culture: string | null;
    cultureStates: string | null;
}

export interface MediaMap extends ItemBase {
    ext: string;
    type: string;
    mediaTypeAlias: string;
    mediaTypeIcon: string | null;
    size: number;
    createDate: string;
    updateDate: string;
    path: string;
}

export interface ContentMediaDetail {
    kind: "Content" | "Media" | string;
    id: number;
    key: string;
    name: string;
    contentTypeName: string;
    contentTypeAlias: string;
    path: string;
    ancestors: Array<{
        id: number;
        key?: string | null;
        name: string;
        alias: string;
        level: number;
        isRoot: boolean;
        isCurrent: boolean;
    }>;
    parentId: number;
    level: number;
    trashed: boolean;
    createDate: string;
    updateDate: string;
    state: {
        published?: boolean | null;
        edited?: boolean | null;
        templateId?: number | null;
        publishedVersionId?: number | null;
        publishDate?: string | null;
        availableCultures: string[];
        publishedCultures: string[];
        editedCultures: string[];
    };
    mediaFile?: {
        extension: string;
        fileType: string;
        size: number;
    } | null;
    properties: Array<{
        alias: string;
        name: string;
        editorAlias: string;
        storageType: string;
        mandatory: boolean;
        variations: string;
        valueCount: number;
        hasEditedValue: boolean;
        hasPublishedValue: boolean;
        cultures: string[];
    }>;
    incomingRelations: Array<{
        direction: string;
        relationTypeAlias: string;
        relationTypeName: string;
        relatedId: number;
        relatedKey: string;
        relatedName: string;
        relatedPath: string;
        comment: string;
    }>;
    outgoingRelations: Array<{
        direction: string;
        relationTypeAlias: string;
        relationTypeName: string;
        relatedId: number;
        relatedKey: string;
        relatedName: string;
        relatedPath: string;
        comment: string;
    }>;
    usedBy: ReferenceEdge[];
    uses: ReferenceEdge[];
    auditTrail: Array<{
        auditType: string;
        entityType: string;
        userId: number;
        userName: string;
        comment: string;
        parameters: string;
    }>;
}

export interface MemberModel {
    id: number;
    username: string;
    name: string;
    email: string;
    memberTypeId: number;
    memberTypeName: string;
    memberTypeAlias: string;
    groups: string;
    isApproved: boolean;
    isLockedOut: boolean;
    usesTwoFactor: boolean;
    createDate: string;
    udi: string;
}

export interface MemberGroupModel {
    id: number;
    name: string;
}

export interface UsageModel {
    id: number;
    nodeCount: number;
    description: string | null;
    alias: string;
    icon: string | null;
    guidType: string;
    /** "Content" | "Media" | "Members" | "Content Item" | "Not Classified" */
    type: string;
}

export interface Tag {
    text: string;
    group: string;
    id: number;
    nodeCount: number;
    culture: string | null;
}

export interface ContentBasic {
    id: number;
    udi: string;
    alias: string;
    name: string;
    icon: string | null;
}

export interface ContentTags extends ContentBasic {
    type: string;
    tags: Tag[];
}

export interface TagMapping {
    key: string;
    tag: Tag;
    content: ContentTags[];
    culture: string | null;
}

export interface Lang {
    id: number;
    name: string;
    culture: string;
}

export interface NameValue {
    name: string;
    value: string;
}

export interface TypeMap {
    module: string;
    assembly: string;
    origin: string;
    name: string;
    namespace: string;
    baseType: string;
    loadableName: string;
    isUmbraco: boolean;
    implementationCount: number;
}

export interface TypeDetail extends TypeMap {
    inheritanceChain: TypeMap[];
    interfaces: TypeMap[];
}

export interface RegisteredService {
    name: string | null;
    namespace: string | null;
    fullName: string | null;
    isPublic: boolean;
    implementName: string | null;
    implementNamespace: string | null;
    implementFullName: string | null;
    lifetime: string;
    key: string;
}

export interface ReferenceEdge {
    sourceType: string;
    sourceName: string;
    sourceAlias: string;
    sourceKey: string;
    relation: string;
    targetType: string;
    targetName: string;
    targetAlias: string;
    targetKey: string;
    context: string | null;
}

export interface HealthRiskFinding {
    severity: "High" | "Medium" | "Low" | "Info" | string;
    score: number;
    category: string;
    title: string;
    detail: string;
    entityType: string;
    entityName: string;
    entityAlias: string;
    entityKey: string;
    recommendation: string;
}

export interface ConfigurationDriftFinding {
    severity: "High" | "Medium" | "Low" | "Info" | string;
    score: number;
    category: string;
    entityType: string;
    entityName: string;
    entityAlias: string;
    entityKey: string;
    comparedWith: string[];
    summary: string;
    differingFields: string[];
    recommendation: string;
}

export interface DiagnosticGroup {
    id: number;
    title: string;
    sections: DiagnosticSection[];
}

export interface DiagnosticSection {
    heading: string;
    diagnostics: Diagnostic[];
}

export interface Diagnostic {
    key: string;
    /** Server stringifies values; always a string when present. */
    value: string | null;
}

export interface UmbracoKeyValue {
    key: string;
    value: string | null;
    updated: string;
}

export interface ServerResponse {
    message: string;
    /** Server emits the enum value as a string ("Success" | "Error" | "Warning")
     *  via the public `response` getter — keep both shapes available. */
    response?: "Success" | "Error" | "Warning";
    responseType?: 0 | 1 | 2;
}

export interface UtilityDiagnostics {
    app: {
        environmentName: string;
        contentRootPath: string;
        webRootPath: string;
        processId: number;
        startedAt: string;
        uptime: string;
        godModeVersion: string;
    };
    assets: Array<{
        label: string;
        url: string;
        path: string;
        exists: boolean;
        size: number;
    }>;
    folders: Array<{
        label: string;
        path: string;
        exists: boolean;
        size: number;
        fileCount: number;
    }>;
    cache: {
        settings: Array<{
            label: string;
            path: string;
            value: string;
        }>;
        folders: Array<{
            label: string;
            path: string;
            exists: boolean;
            size: number;
            fileCount: number;
        }>;
    };
    database: Array<{
        label: string;
        table: string;
        count: number;
        exists: boolean;
    }>;
}

export interface DatabaseTableInfo {
    name: string;
    schema: string;
    category: string;
    purpose: string;
    rowCount: number;
    countSucceeded: boolean;
    warning: string;
}

export interface DatabaseTableDetail extends DatabaseTableInfo {
    columns: DatabaseColumnInfo[];
    outgoingRelationships: DatabaseRelationshipInfo[];
    incomingRelationships: DatabaseRelationshipInfo[];
}

export interface DatabaseColumnInfo {
    name: string;
    dataType: string;
    maxLength?: number | null;
    nullable: boolean;
    primaryKey: boolean;
    ordinal: number;
}

export interface DatabaseRelationshipInfo {
    constraintName: string;
    fromTable: string;
    fromColumn: string;
    toTable: string;
    toColumn: string;
}

export interface GodModeLogOverview {
    logFolder: string;
    exists: boolean;
    fileCount: number;
}

export interface GodModeLogEvent {
    id: string;
    timestamp: string | null;
    level: string;
    message: string;
    messageTemplate: string;
    exception: string;
    sourceContext: string;
    requestId: string;
    requestPath: string;
    machineName: string;
    processId?: number | null;
    threadId?: number | null;
    logFile: string;
    properties: Record<string, unknown>;
    rawJson: string;
}

export interface GodModeLogInsight {
    id: string;
    level: string;
    title: string;
    count: number;
    firstSeen: string | null;
    lastSeen: string | null;
    sourceContext: string;
    requestPaths: string[];
    exceptionType: string;
    normalizedMessage: string;
    sample?: GodModeLogEvent | null;
    samples: GodModeLogEvent[];
}

export interface GodModeLogLevelCount {
    level: string;
    count: number;
}

export interface GodModeSavedLogQuery {
    id: string;
    name: string;
    query: string;
}

export interface Page<T> {
    currentPage: number;
    totalPages: number;
    itemsPerPage: number;
    totalItems: number;
    items: T[];
}

export interface GodModeConfigResponse {
    featuresToHide: string[];
    aliasesToIgnore: string[];
    diagnostics: {
        groupsToHide: string[];
        sectionsToHide: string[];
        keysToRedact: string[];
        keyMatchesToRedact: string[];
        redactRevealPasswordEnv: string;
    };
}

