export interface Filter {
    hierarchyId: number;
    measureTypeId: number;
    intervalId: number;
    calendarId: number;
    year: number;
}

export interface measureDataFilter {
    calendarId: number;
    year: number;
    hierarchyId: number;
    measureTypeId: number;
    intervalId: number;
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

//--------------------------------------------
//Interface for Measure Data Model
export interface MeasureData {
    measureDataId?: string;
    measureId?: string;
    calendarId?: string;
    targetId?: string;
    value?: string;
    explanation?: string;
    action?: string;
    userId?: string;
    lastUpdatedOn?: string;
    isProcessed?: string
}

export interface MeasureDataIndexListObject{
    range: string;
    calendarId?: string;
    allow?: boolean;
    editValue?: boolean;
    locked?: boolean;
    confirmed?: boolean;
    filter?: any;
    data?: MeasureData[];
    error?: string;
} 

export interface MeasureDataReceiveObject{
    calendarId: number;
    day: string;
    hierarchyId: number;
    measureTypeId: number;
    //measureDataId { set; get; }      
    //measureValue { set; get; }
    explanation: string;
    action: string;  
    //public ErrorModel error { set; get; }
  }