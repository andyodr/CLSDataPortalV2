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
    filter: TargetFilter;
    data: TargetDto[];
    error?: any;
  }
  
  export interface TargetDto {
    //id: number;
    id: string;
    name: string;
    value?: number;
    explanation?: string;
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

  export interface TargetFilter {
    intervalId?: number;
    calendarId?: number;
    year?: number;
    measureTypeId: number;
    hierarchyId: number;
  }
  

  export interface TargetApiParams{
    intervalId?: number;
    calendarId?: number;
    year?: number;
    measureTypeId: number;
    hierarchyId: number; 
  }


  export interface TargetPutDto {
    hierarchyId: number
    measureId: number
    measureTypeId: number
    target: number
    yellow: number
    applyToChildren: boolean
    isCurrentUpdate: boolean
    confirmIntervals?: ConfirmIntervals
  }
  
  export interface ConfirmIntervals {
    daily: boolean
    weekly: boolean
    monthly: boolean
    quarterly: boolean
    yearly: boolean
  }

  //--------------------------------------------------------------------------------
  //from GET api/targets/ common for index and filter
  //--------------------------------------------------------------------------------

  //--------------------------------------------------------------------------------
  //from GET api/targets/Filter with no parameters
  //--------------------------------------------------------------------------------


  export interface TargetFilterResponseDto {
    measureTypes: MeasureType[]
    hierarchy: Hierarchy[]
    intervals: any
    years: any
    error: any
    filter: TargetFilter
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

  
 
  