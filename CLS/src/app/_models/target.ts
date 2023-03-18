// export interface Target {
//     targetId?: string;
//     measureId?: string;
//     targetValue?: string;
//     yellowValue?: string;
//     active?: string;
//     lastUpdatedOn?: string;
//     userId?: string;
//     isProcessed?: string
// }


export interface TargetApiResponse {
    range?: string;
    calendarId?: number;
    allow: boolean;
    editValue: boolean;
    locked: boolean;
    confirmed: boolean;
    filter: Filter;
    data: Data[];
    error?: any;
  }
  
  export interface Filter {
    intervalId?: number;
    calendarId?: number;
    year?: number;
    measureTypeId: number;
    hierarchyId: number;
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
    unitId?: number;
    units?: string;
    yellow?: any;
    expression?: any;
    evaluated?: string;
    calculated: boolean;
    description?: string;
    updated: Updated;
  }
  
  export interface Updated {
    by: string;
    longDt: string;
    shortDt: string;
  }

  export interface TargetApiParams{
    intervalId?: number;
    calendarId?: number;
    year?: number;
    measureTypeId: number;
    hierarchyId: number; 
 
  }