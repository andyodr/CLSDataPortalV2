// export interface Measure {
//     measureId?: string;
//     hierarchyId?: string;
//     measureDefinitionId?: string;
//     active?: string;
//     expression?: string;
//     rollup?: string;
//     owner?: string;
//     lastUpdatedOn?: string;
// }

// Path: CLS\src\app\_models\target.ts
  //--------------------------------------------------------------------------------
  //from GET api/measures/Index with hierarchyId and measureTypeId parameters
  //--------------------------------------------------------------------------------

export interface MeasureApiResponse {
    error: any
    hierarchy: string[]
    allow: boolean
    data: Data[]
  }
  
  export interface Data {
    id: number
    name: string
    owner: any
    hierarchy: Hierarchy[]
  }
  
  export interface Hierarchy {
    id: number
    active: boolean
    expression: boolean
    rollup: boolean
  }

  export interface MeasureApiParams{
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

  export interface MeasureFilter {
    measureTypes: MeasureType[]
    hierarchy: any[]
    intervals: any
    years: any
    error: any
    filter: Filter
    currentCalendarIds: any
  }
  
  export interface MeasureType {
    id: number
    name: string
    description?: string
  }

  //--------------------------------------------------------------------------------
  export interface TableData {
    id: number;
    name: string;
    owner: string;
    hierarchy: {
      id: number;
      active: boolean;
      expression: boolean;
      rollup: boolean;
    }[];
  }