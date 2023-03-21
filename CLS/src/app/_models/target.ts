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

// Path: CLS\src\app\_models\target.ts
  //--------------------------------------------------------------------------------
  //from GET api/targets/Index with hierarchyId and measureTypeId parameters
  //--------------------------------------------------------------------------------

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

  //--------------------------------------------------------------------------------
  //from GET api/targets/ common for index and filter
  //--------------------------------------------------------------------------------
  export interface Filter {
    intervalId?: number;
    calendarId?: number;
    year?: number;
    measureTypeId: number;
    hierarchyId: number;
  }
  
  //--------------------------------------------------------------------------------
  //from GET api/targets/Filter with no parameters
  //--------------------------------------------------------------------------------


  export interface TargetFilter {
    measureTypes: MeasureType[]
    hierarchy: Hierarchy[]
    intervals: any
    years: any
    error: any
    filter: Filter
    currentCalendarIds: any
  }

  // export interface TargetFilter {
  //   hierarchies: any;
  //   measureTypes: MeasureType[]
  // }
  
  export interface MeasureType {
    id: number
    name: string
    description?: string
  }
  
  export interface Hierarchy {
    hierarchy: string
    id: number
    count: number
    sub: any[]
    found: any
    error: any
  }
  
 
  