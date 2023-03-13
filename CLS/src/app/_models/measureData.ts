export interface Filter {
    hierarchyId: number;
    measureTypeId: number;
    intervalId: number;
    calendarId: number;
    year: number;
}

export interface Updated {
    by: string;
    longDt: string;
    shortDt: string;
}

export interface Data {
    id: number;
    name: string;
    value?: number;
    explanation?: any;
    action?: any;
    target?: any;
    targetCount?: any;
    targetId?: any;
    unitId: number;
    units: string;
    yellow?: any;
    expression: any;
    evaluated: string;
    calculated: boolean;
    description: string;
    updated: Updated;
}

export interface MeasureDataResponse {
    range: string;
    calendarId: number;
    allow: boolean;
    editValue: boolean;
    locked: boolean;
    confirmed: boolean;
    filter: Filter;
    data: Data[];
    error?: any;
}